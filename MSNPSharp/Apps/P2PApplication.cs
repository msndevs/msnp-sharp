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

    /// <summary>
    /// P2P attribute used in classes, which can be used to implement the class as a P2P application.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the P2PApplicationAttribute class.
        /// </summary>
        /// <param name="appID">Application ID</param>
        /// <param name="eufGuid">GUID for the P2P application</param>
        public P2PApplicationAttribute(uint appID, string eufGuid)
        {
            this.appId = appID;
            this.eufGuid = new Guid(eufGuid);
        }
    }
    #endregion

    #region P2PApplicationStatus

    /// <summary>
    /// List of current P2PApplication statuses.
    /// </summary>
    public enum P2PApplicationStatus
    {
        Waiting,
        Active,
        Finished,
        Aborted,
        Error
    }

    #endregion

    /// <summary>
    /// Class where you can inherit this class to produce P2P applications
    /// </summary>
    public abstract class P2PApplication : IDisposable
    {
        #region Events

        /// <summary>
        /// Called if the transfer has begun.
        /// </summary>
        public event EventHandler<EventArgs> TransferStarted;

        /// <summary>
        /// Called after the transfer has finished.
        /// </summary>
        public event EventHandler<EventArgs> TransferFinished;

        /// <summary>
        /// Called after the transfer has been cancelled by the user.
        /// </summary>
        public event EventHandler<ContactEventArgs> TransferAborted;

        /// <summary>
        /// Called if the transfer has encountered an error.
        /// </summary>
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
            if (ver == P2PVersion.None)
                throw new InvalidOperationException(remote.ToString() + " doesn't support P2P");

            this.version = ver;

            this.local = remote.NSMessageHandler.Owner;
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

        /// <summary>
        /// Prepares the invite message.
        /// </summary>
        /// <param name="slp"></param>
        public virtual void SetupInviteMessage(SLPMessage slp)
        {
            slp.BodyValues["EUF-GUID"] = ApplicationEufGuid.ToString("B").ToUpperInvariant();
            slp.BodyValues["AppID"] = ApplicationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            slp.BodyValues["Context"] = InvitationContext;
        }

        /// <summary>
        /// Checks if the invitation was valid.
        /// </summary>
        /// <param name="invitation"></param>
        /// <returns></returns>
        public virtual bool ValidateInvitation(SLPMessage invitation)
        {
            bool ret = invitation.ToEmailAccount.ToLowerInvariant() == local.Account.ToLowerInvariant();
            if (ret && version == P2PVersion.P2PV2 && invitation.ToEndPoint != Guid.Empty)
            {
                ret = invitation.ToEndPoint == localEP;
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bridge"></param>
        /// <param name="data"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public abstract bool ProcessData(P2PBridge bridge, byte[] data, bool reset);

        /// <summary>
        /// Sends a P2P message.
        /// </summary>
        /// <param name="p2pMessage"></param>
        public void SendMessage(P2PMessage p2pMessage)
        {
            SendMessage(p2pMessage, 0, null);
        }

        /// <summary>
        /// Sends a message with the specified <see cref="P2PMessage"/> and <see cref="AckHandler"/>.
        /// </summary>
        public virtual void SendMessage(P2PMessage p2pMessage, int ackTimeout, AckHandler ackHandler)
        {
            Debug.Assert(p2pMessage.Version == version);

            p2pMessage.Header.SessionId = p2pSession.SessionId;

            // If not an ack, set the footer (p2pv1 only)
            if (p2pMessage.Version == P2PVersion.P2PV1 && (p2pMessage.V1Header.Flags & P2PFlag.Acknowledgement) != P2PFlag.Acknowledgement)
                p2pMessage.Footer = ApplicationId;

            p2pSession.Send(p2pMessage, ackTimeout, ackHandler);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void BridgeIsReady()
        {
        }

        /// <summary>
        /// Begins the P2P transfer, thus calls the <see cref="TransferStarted"/> event.
        /// </summary>
        public virtual void Start()
        {
            OnTransferStarted(EventArgs.Empty);
        }

        /// <summary>
        /// Accepts the P2P session.
        /// </summary>
        public virtual void Accept()
        {
            if (WarnIfP2PSessionDisposed("accept"))
                return;

            p2pSession.Accept();
        }

        /// <summary>
        /// Rejects the P2P session.
        /// </summary>
        public virtual void Decline()
        {
            if (WarnIfP2PSessionDisposed("decline"))
                return;

            p2pSession.Decline();
        }

        /// <summary>
        /// Ends the P2P session.
        /// </summary>
        public virtual void Abort()
        {
            OnTransferAborted(new ContactEventArgs(Local));

            if (WarnIfP2PSessionDisposed("abort"))
                return;

            p2pSession.Close();
        }

        /// <summary>
        /// Frees the resources for this <see cref="P2PApplication"/>.
        /// </summary>
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

        #region IsRegistered/CreateInstance

        internal static bool IsRegistered(Guid eufGuid, uint appId)
        {
            if (eufGuid != Guid.Empty && p2pAppCache.ContainsKey(eufGuid))
            {
                if (appId != 0)
                {
                    foreach (P2PApp app in p2pAppCache[eufGuid])
                    {
                        if (appId == app.AppId)
                            return true;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new P2PApplication instance with the parameters provided.
        /// </summary>
        /// <param name="eufGuid"></param>
        /// <param name="appId"></param>
        /// <param name="withSession"></param>
        internal static P2PApplication CreateInstance(Guid eufGuid, uint appId, P2PSession withSession)
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

        /// <summary>
        /// Registers all P2P applications through an assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Registers a P2P application, which inherits P2PApplication.
        /// </summary>
        /// <param name="appType"></param>
        /// <returns></returns>
        public static bool RegisterApplication(Type appType)
        {
            return RegisterApplication(appType, false);
        }

        /// <summary>
        /// Registers a P2P application which inherits P2PApplication, with option to override the existing class.
        /// </summary>
        /// <param name="appType"></param>
        /// <param name="overrideExisting"></param>
        /// <returns></returns>
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
                                    String.Format("Application has been unregistered! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
                            }

                            p2pAppCache[att.EufGuid].Add(app);
                            added = true;

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                                String.Format("New application has been registered in force mode! EufGuid: {0}, AppId: {1}, AppType: {2}", att.EufGuid, att.AppId, appType), "P2PApplication");
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

        /// <summary>
        /// Unregisters all P2P applications from an assembly. 
        /// </summary>
        /// <param name="assembly"></param>
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

        /// <summary>
        /// Unregisters a P2P application from the registered list of applications.
        /// </summary>
        /// <param name="appType"></param>
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