#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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

namespace MSNPSharp.Apps
{
    using MSNPSharp;
    using MSNPSharp.P2P;
    using MSNPSharp.Core;

    #region P2PApplicationAttribute

    [AttributeUsageAttribute(AttributeTargets.Class)]
    public class P2PApplicationAttribute : Attribute
    {
        private uint appId;
        private Guid eufGuid;

        public uint AppId
        {
            get
            {
                return appId;
            }
        }

        public Guid EufGuid
        {
            get
            {
                return eufGuid;
            }
        }

        public P2PApplicationAttribute(uint appID, string eufGuid)
        {
            this.appId = appID;
            this.eufGuid = new Guid(eufGuid);
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

        protected internal uint applicationId = uint.MinValue;
        protected internal Guid applicationEufGuid = Guid.Empty;
        protected internal Guid localEP = Guid.Empty;
        protected internal Guid remoteEP = Guid.Empty;

        private P2PApplicationStatus status = P2PApplicationStatus.Waiting;
        private P2PVersion version = P2PVersion.P2PV1;
        private NSMessageHandler nsMessageHandler;
        private P2PSession p2pSession;
        private Contact remote;
        private Contact local;

        #endregion

        #region Ctor

        protected P2PApplication(P2PSession p2pSess)
            : this(p2pSess.Version, p2pSess.Remote, p2pSess.RemoteContactEndPointID)
        {
            P2PSession = p2pSess; // Register events

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                "Application associated with session: " + p2pSess.SessionId, GetType().Name);
        }

        protected P2PApplication(P2PVersion ver, Contact remote, Guid remoteEP)
        {
            this.version = ver;

            this.local = remote.NSMessageHandler.ContactList.Owner;
            this.localEP = NSMessageHandler.MachineGuid;

            this.remote = remote;
            this.remoteEP = remoteEP;

            this.nsMessageHandler = remote.NSMessageHandler;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                "Application created: " + ver, GetType().Name);
        }


        #endregion

        #region Properties

        public P2PVersion P2PVersion
        {
            get
            {
                return version;
            }
        }

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

        public Contact Local
        {
            get
            {
                return local;
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

        public P2PSession P2PSession
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

        public virtual void SetupInviteMessage(SLPMessage slp)
        {
            slp.BodyValues["EUF-GUID"] = ApplicationEufGuid.ToString("B").ToUpperInvariant();
            slp.BodyValues["AppID"] = ApplicationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            slp.BodyValues["Context"] = InvitationContext;
        }

        public virtual bool ValidateInvitation(SLPMessage invitation)
        {
            bool ret = invitation.ToEmailAccount.ToLowerInvariant() == local.Mail.ToLowerInvariant();
            if (ret && version == P2PVersion.P2PV2)
            {
                ret = invitation.ToEndPoint == localEP;
            }
            return ret;
        }

        public abstract bool ProcessData(P2PBridge bridge, byte[] data, bool reset);

        public void SendMessage(P2PMessage p2pMessage)
        {
            SendMessage(p2pMessage, null);
        }

        public virtual void SendMessage(P2PMessage p2pMessage, AckHandler ackHandler)
        {
            Debug.Assert(p2pMessage.Version == version);

            p2pMessage.Header.SessionId = p2pSession.SessionId;

            // If not an ack, set the footer (p2pv1 only)
            if (p2pMessage.Version == P2PVersion.P2PV1 && (p2pMessage.V1Header.Flags & P2PFlag.Acknowledgement) != P2PFlag.Acknowledgement)
                p2pMessage.Footer = ApplicationId;

            p2pSession.Send(p2pMessage, ackHandler);
        }

        public virtual void BridgeIsReady()
        {
        }

        public virtual void Start()
        {
            OnTransferStarted(EventArgs.Empty);
        }

        public virtual void Accept(bool sendDCInvite)
        {
            if (WarnIfP2PSessionDisposed("accept"))
                return;

            p2pSession.Accept(sendDCInvite);
        }

        public virtual void Decline()
        {
            if (WarnIfP2PSessionDisposed("decline"))
                return;

            p2pSession.Decline();
        }

        public virtual void Abort()
        {
            OnTransferAborted(new ContactEventArgs(Local));

            if (WarnIfP2PSessionDisposed("abort"))
                return;

            p2pSession.Close();
        }

        public virtual void Dispose()
        {
            P2PSession = null; // Unregister events

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

        private bool WarnIfP2PSessionDisposed(string actionRequested)
        {
            if (p2pSession == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("P2P session disposed, so you cannot {0}. Application status: {1}", actionRequested, ApplicationStatus), GetType().Name);

                return true;
            }
            return false;
        }

        #endregion

        #region Static

        private static Dictionary<Guid, List<P2PApp>> p2pAppCache = new Dictionary<Guid, List<P2PApp>>(4);

        [Serializable]
        private struct P2PApp
        {
            public Guid EufGuid;
            public UInt32 AppId;
            public Type AppType;

            public P2PApp(Guid euf, UInt32 id, Type type)
            {
                this.EufGuid = euf;
                this.AppId = id;
                this.AppType = type;
            }
        };

        static P2PApplication()
        {
            try
            {
                int added = RegisterApplication(Assembly.GetExecutingAssembly());
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    String.Format("Registered {0} built-in p2p applications", added), "P2PApplication");
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "Error registering built-in p2p applications: " + e.Message, "P2PApplication");
            }
        }

        #region CreateInstance

        public static P2PApplication CreateInstance(Guid eufGuid, uint appId, P2PSession withSession)
        {
            if (withSession != null && eufGuid != Guid.Empty && p2pAppCache.ContainsKey(eufGuid))
            {
                if (appId != 0)
                {
                    foreach (P2PApp app in p2pAppCache[eufGuid])
                    {
                        if (appId == app.AppId)
                            return (P2PApplication)Activator.CreateInstance(app.AppType, withSession);
                    }
                }

                return (P2PApplication)Activator.CreateInstance(p2pAppCache[eufGuid][0].AppType, withSession);
            }

            return null;
        }

        #endregion

        #region RegisterApplication/UnregisterApplication

        public static int RegisterApplication(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(P2PApplicationAttribute), false).Length > 0)
                    if (RegisterApplication(type))
                        count++;
            }

            return count;
        }

        public static bool RegisterApplication(Type appType)
        {
            return RegisterApplication(appType, false);
        }

        public static bool RegisterApplication(Type appType, bool overrideExisting)
        {
            if (appType == null)
                throw new ArgumentNullException("appType");

            if (!appType.IsSubclassOf(typeof(P2PApplication)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    String.Format("Type {0} can't be registered! It must be derived from P2PApplication", appType.Name), "P2PApplication");

                return false;
            }

            bool added = false;
            bool hasAttribute = false;

            foreach (P2PApplicationAttribute att in appType.GetCustomAttributes(typeof(P2PApplicationAttribute), false))
            {
                hasAttribute = true;

                lock (p2pAppCache)
                {
                    if (!p2pAppCache.ContainsKey(att.EufGuid))
                        p2pAppCache[att.EufGuid] = new List<P2PApp>(1);

                    P2PApp app = new P2PApp(att.EufGuid, att.AppId, appType);

                    if (p2pAppCache[att.EufGuid].Contains(app))
                    {
                        if (overrideExisting)
                        {
                            while (p2pAppCache[att.EufGuid].Remove(app))
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                    String.Format("Application has unregistered! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                            }

                            p2pAppCache[att.EufGuid].Add(app);
                            added = true;

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                                String.Format("New application registered in force mode! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                        }
                        else
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                String.Format("Application has already registered! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                        }
                    }
                    else
                    {
                        p2pAppCache[att.EufGuid].Add(app);
                        added = true;

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            String.Format("New application registered! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                    }
                }
            }

            if (hasAttribute == false)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    String.Format("Type {0} can't be registered! It must have [P2PApplicationAttribute(appID, eufGUID)].", appType.Name), "P2PApplication");
            }

            return added;
        }

        public static int UnregisterApplication(Assembly assembly)
        {
            int count = 0;

            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(P2PApplicationAttribute), false).Length > 0)
                    if (UnregisterApplication(type))
                        count++;
            }

            return count;
        }

        public static bool UnregisterApplication(Type appType)
        {
            bool unregistered = false;

            if (appType == null)
                throw new ArgumentNullException("appType");

            if (!appType.IsSubclassOf(typeof(P2PApplication)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    String.Format("Type {0} can't be unregistered! It must be derived from P2PApplication", appType.Name), "P2PApplication");

                return false;
            }

            foreach (P2PApplicationAttribute att in appType.GetCustomAttributes(typeof(P2PApplicationAttribute), false))
            {
                lock (p2pAppCache)
                {
                    if (p2pAppCache.ContainsKey(att.EufGuid))
                    {
                        P2PApp app = new P2PApp(att.EufGuid, att.AppId, appType);

                        if (p2pAppCache[att.EufGuid].Contains(app))
                        {
                            while (p2pAppCache[att.EufGuid].Remove(app))
                            {
                                unregistered = true;

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                    String.Format("Application has unregistered! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                            }
                        }
                    }
                }
            }

            return unregistered;
        }

        #endregion

        #region FindApplicationId/FindApplicationEufGuid

        private static uint FindApplicationId(P2PApplication p2pApp)
        {
            foreach (List<P2PApp> apps in p2pAppCache.Values)
            {
                foreach (P2PApp app in apps)
                    if (app.AppType == p2pApp.GetType())
                        return app.AppId;
            }

            return 0;
        }

        private static Guid FindApplicationEufGuid(P2PApplication p2pApp)
        {
            foreach (List<P2PApp> apps in p2pAppCache.Values)
            {
                foreach (P2PApp app in apps)
                    if (app.AppType == p2pApp.GetType())
                        return app.EufGuid;
            }

            return Guid.Empty;
        }
        #endregion

        #endregion
    }
};