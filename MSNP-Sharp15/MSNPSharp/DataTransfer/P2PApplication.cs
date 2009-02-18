#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    #region P2PApplicationAttribute

    [AttributeUsage(AttributeTargets.Class)]
    public class P2PApplicationAttribute : Attribute
    {
        private uint appId;
        private string eufGuid;

        public uint AppId
        {
            get
            {
                return appId;
            }
        }

        public string EufGuid
        {
            get
            {
                return eufGuid;
            }
        }

        public P2PApplicationAttribute(uint appID, string eufGuid)
        {
            this.appId = appID;
            this.eufGuid = eufGuid;
        }
    }
    #endregion

    #region P2PApplicationStatus

    public enum P2PApplicationStatus
    {
        Waiting,
        Active,
        Finished,
        Aborted,
        Error
    }

    #endregion

    public abstract class P2PApplication : IDisposable
    {
        #region Events

        public event EventHandler<EventArgs> TransferStarted;
        public event EventHandler<EventArgs> TransferFinished;
        public event EventHandler<ContactEventArgs> TransferAborted;
        public event EventHandler<EventArgs> TransferError;

        #endregion

        #region Members

        protected uint applicationId = uint.MinValue;
        protected Guid applicationEufGuid = Guid.Empty;
        protected P2PFlag dataFlags = P2PFlag.Normal;

        private P2PApplicationStatus status = P2PApplicationStatus.Waiting;
        private NSMessageHandler nsMessageHandler;
        private P2PSession p2pSession;
        private Contact remote;

        #endregion

        #region Ctor

        protected P2PApplication(Contact remote)
        {
            this.remote = remote;
            this.nsMessageHandler = remote.NSMessageHandler;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application created", GetType().Name);
        }

        protected P2PApplication(P2PSession session)
            : this(session.Remote)
        {
            Session = session;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application associated with session: " + session.SessionId, GetType().Name);
        }

        #endregion

        #region Properties

        public abstract string InvitationContext
        {
            get;
        }

        public virtual bool AutoAccept
        {
            get
            {
                return false;
            }
        }

        public virtual uint ApplicationId
        {
            get
            {
                if (applicationId == 0)
                    applicationId = FindApplicationId(this);

                return applicationId;
            }
        }

        public virtual Guid ApplicationEufGuid
        {
            get
            {
                if (applicationEufGuid == Guid.Empty)
                    applicationEufGuid = FindApplicationEufGuid(this);

                return applicationEufGuid;
            }
        }

        public P2PApplicationStatus ApplicationStatus
        {
            get
            {
                return status;
            }
        }

        public Owner Local
        {
            get
            {
                return NSMessageHandler.Owner;
            }
        }

        public Contact Remote
        {
            get
            {
                return remote;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public P2PSession Session
        {
            get
            {
                return p2pSession;
            }
            set
            {
                if (p2pSession != null)
                {
                    p2pSession.Closing -= P2PSessionClosing;
                    p2pSession.Closed -= P2PSessionClosed;
                    p2pSession.Error -= P2PSessionError;
                }

                p2pSession = value;

                if (p2pSession != null)
                {
                    p2pSession.Closing += P2PSessionClosing;
                    p2pSession.Closed += P2PSessionClosed;
                    p2pSession.Error += P2PSessionError;
                }
            }
        }

        #endregion

        #region Methods

        public abstract void HandleMessage(IMessageProcessor bridge, P2PMessage p2pMessage);

        public void SendMessage(P2PMessage p2pMessage)
        {
            SendMessage(p2pMessage, null);
        }

        public virtual void SendMessage(P2PMessage p2pMessage, P2PAckHandler ackHandler)
        {
            p2pMessage.SessionId = p2pSession.SessionId;

            if (p2pMessage.Identifier == 0)
            {
                p2pSession.IncreaseLocalIdentifier();
                p2pMessage.Identifier = p2pSession.LocalIdentifier;
            }

            // If not an ack, set the footer
            if ((p2pMessage.Flags & P2PFlag.Acknowledgement) != P2PFlag.Acknowledgement)
                p2pMessage.Footer = ApplicationId;

            p2pSession.SendMessage(p2pMessage, ackHandler);
        }

        /// <summary>
        /// Validate invitation for this application.
        /// </summary>
        /// <param name="invitation"></param>
        /// <returns></returns>
        public virtual bool ValidateInvitation(SLPMessage invitation)
        {
            return (invitation.ToMail == Local.Mail);
        }

        public virtual void Start()
        {
            OnTransferStarted(EventArgs.Empty);
        }

        public virtual void Accept()
        {
            p2pSession.Accept();
        }

        public virtual void Decline()
        {
            p2pSession.Decline();
        }

        public virtual void Abort()
        {
            OnTransferAborted(new ContactEventArgs(Local));
            p2pSession.Close();
        }

        public virtual void Dispose()
        {
            Session = null;

            if ((status != P2PApplicationStatus.Aborted) &&
                (status != P2PApplicationStatus.Finished) &&
                (status != P2PApplicationStatus.Error))
            {
                OnTransferError(EventArgs.Empty);
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application disposed", GetType().Name);
        }

        #endregion

        #region Protected

        protected virtual void OnTransferStarted(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Transfer started", GetType().Name);

            status = P2PApplicationStatus.Active;

            if (TransferStarted != null)
                TransferStarted(this, e);
        }

        protected virtual void OnTransferFinished(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Transfer finished", GetType().Name);

            status = P2PApplicationStatus.Finished;

            if (TransferFinished != null)
                TransferFinished(this, e);
        }

        protected virtual void OnTransferAborted(ContactEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Transfer aborted ({0})", e.Contact == Local ? "locally" : "remotely"), GetType().Name);

            status = P2PApplicationStatus.Aborted;

            if (TransferAborted != null)
                TransferAborted(this, e);
        }

        protected virtual void OnTransferError(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Transfer error", GetType().Name);

            status = P2PApplicationStatus.Error;

            if (TransferError != null)
                TransferError(this, e);
        }

        #endregion

        #region P2PSession Handling

        private void P2PSessionClosing(object sender, ContactEventArgs e)
        {
            if (e.Contact != Local)
            {
                if (status == P2PApplicationStatus.Waiting || status == P2PApplicationStatus.Active)
                {
                    OnTransferAborted(e);
                }
            }
        }

        private void P2PSessionClosed(object sender, ContactEventArgs e)
        {
            if (status == P2PApplicationStatus.Waiting || status == P2PApplicationStatus.Active)
            {
                OnTransferAborted(e);
            }
        }

        private void P2PSessionError(object sender, EventArgs e)
        {
            if (status != P2PApplicationStatus.Error)
            {
                OnTransferError(e);
            }
        }

        #endregion

        #region Static

        private static List<P2PApp> p2pApps = new List<P2PApp>(4);
        private struct P2PApp
        {
            public UInt32 AppId;
            public Type AppType;
            public Guid EufGuid;
        }

        static P2PApplication()
        {
            try
            {
                AddApplication(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error loading built-in p2p applications: " + e.Message, "P2PApplication");
            }
        }

        #region Add/Find Application

        public static void AddApplication(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(P2PApplicationAttribute), false).Length > 0)
                    AddApplication(type);
            }
        }

        public static void AddApplication(Type type)
        {
            foreach (P2PApplicationAttribute att in type.GetCustomAttributes(typeof(P2PApplicationAttribute), false))
            {
                P2PApp app = new P2PApp();
                app.AppType = type;
                app.AppId = att.AppId;
                app.EufGuid = new Guid(att.EufGuid);

                lock (p2pApps)
                    p2pApps.Add(app);
            }
        }

        internal static Type GetApplication(Guid eufGuid, uint appId)
        {
            if (appId != 0 && eufGuid != Guid.Empty)
            {
                foreach (P2PApp app in p2pApps)
                {
                    if (app.EufGuid == eufGuid && app.AppId == appId)
                        return app.AppType;
                }
            }

            foreach (P2PApp app in p2pApps)
            {
                if (app.EufGuid == eufGuid)
                    return app.AppType;
                else if (app.AppId == appId)
                    return app.AppType;
            }

            return null;
        }

        private static uint FindApplicationId(P2PApplication p2pApp)
        {
            foreach (P2PApp app in p2pApps)
            {
                if (app.AppType == p2pApp.GetType())
                    return app.AppId;
            }

            return 0;
        }

        private static Guid FindApplicationEufGuid(P2PApplication p2pApp)
        {
            foreach (P2PApp app in p2pApps)
            {
                if (app.AppType == p2pApp.GetType())
                    return app.EufGuid;
            }

            return Guid.Empty;
        }
        #endregion


        #endregion
    }
};
