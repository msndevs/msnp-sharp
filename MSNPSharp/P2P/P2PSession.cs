#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.P2P
{
    using MSNPSharp;
    using MSNPSharp.Apps;
    using MSNPSharp.Core;

    public enum P2PSessionStatus
    {
        Error,
        WaitingForLocal,
        WaitingForRemote,
        Active,
        Closing,
        Closed
    }

    public class P2PSessionEventArgs : EventArgs
    {
        private P2PSession p2pSession;

        public P2PSession P2PSession
        {
            get
            {
                return p2pSession;
            }
        }

        public P2PSessionEventArgs(P2PSession session)
        {
            this.p2pSession = session;
        }
    }

    public partial class P2PSession : IDisposable
    {
        private static Random random = new Random();

        #region Events

        public event EventHandler<EventArgs> Error;
        public event EventHandler<EventArgs> Activated;
        public event EventHandler<ContactEventArgs> Closing;
        public event EventHandler<ContactEventArgs> Closed;

        #endregion

        #region Members

        private uint sessionId = 0;
        private uint localBaseIdentifier = 0;
        private uint localIdentifier = 0;
        private uint remoteBaseIdentifier = 0;
        private uint remoteIdentifier = 0;

        private Contact localContact = null;
        private Contact remoteContact = null;
        private Guid localContactEndPointID = Guid.Empty;
        private Guid remoteContactEndPointID = Guid.Empty;

        private SLPRequestMessage invitation = null;
        private P2PBridge p2pBridge = null;
        private P2PApplication p2pApplication = null;
        private bool uunTried = false;
        internal ushort dataPacketNumber = 0;

        private P2PVersion version = P2PVersion.P2PV1;
        private P2PSessionStatus status = P2PSessionStatus.Closed;
        private NSMessageHandler nsMessageHandler = null;

        #endregion

        #region Properties

        public P2PVersion Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// Session ID
        /// </summary>
        public uint SessionId
        {
            get
            {
                return sessionId;
            }
        }

        /// <summary>
        /// The base identifier of the local client
        /// </summary>
        public uint LocalBaseIdentifier
        {
            get
            {
                return localBaseIdentifier;
            }
            set
            {
                localBaseIdentifier = value;
            }
        }

        /// <summary>
        /// The identifier of the local contact. This identifier is increased just before a message is send.
        /// </summary>
        public uint LocalIdentifier
        {
            get
            {
                return localIdentifier;
            }
            set
            {
                localIdentifier = value;
            }
        }

        /// <summary>
        /// The base identifier of the remote client
        /// </summary>
        public uint RemoteBaseIdentifier
        {
            get
            {
                return remoteBaseIdentifier;
            }
        }
        /// <summary>
        /// The expected identifier of the remote client for the next message.
        /// </summary>
        public uint RemoteIdentifier
        {
            get
            {
                return remoteIdentifier;
            }
        }

        /// <summary>
        /// Local contact
        /// </summary>
        public Contact Local
        {
            get
            {
                return localContact;
            }
        }

        /// <summary>
        /// Remote contact
        /// </summary>
        public Contact Remote
        {
            get
            {
                return remoteContact;
            }
        }

        public Guid LocalContactEndPointID
        {
            get
            {
                return localContactEndPointID;
            }
        }

        public Guid RemoteContactEndPointID
        {
            get
            {
                return remoteContactEndPointID;
            }
        }

        public string LocalContactEPIDString
        {
            get
            {
                if (version == P2PVersion.P2PV1)
                {
                    return Local.Mail.ToLowerInvariant();
                }

                return Local.Mail.ToLowerInvariant() + ";" + LocalContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        public string RemoteContactEPIDString
        {
            get
            {
                if (version == P2PVersion.P2PV1)
                {
                    return Remote.Mail.ToLowerInvariant();
                }

                return Remote.Mail.ToLowerInvariant() + ";" + RemoteContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        /// <summary>
        /// SLP invitation
        /// </summary>
        public SLPRequestMessage Invitation
        {
            get
            {
                return invitation;
            }
        }

        /// <summary>
        /// P2P Application handling incoming data messages
        /// </summary>
        public P2PApplication Application
        {
            get
            {
                return p2pApplication;
            }
        }

        public P2PBridge Bridge
        {
            get
            {
                return p2pBridge;
            }
        }

        public bool UUNTried
        {
            get
            {
                return uunTried;
            }
            protected internal set
            {
                uunTried = value;
            }
        }

        /// <summary>
        /// Session state
        /// </summary>
        public P2PSessionStatus Status
        {
            get
            {
                return status;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// We are sender. (Initiated locally).
        /// </summary>
        public P2PSession(P2PApplication app)
        {
            p2pApplication = app;
            version = app.P2PVersion;

            localContact = app.Local;
            remoteContact = app.Remote;
            localContactEndPointID = app.Local.MachineGuid;
            remoteContactEndPointID = app.Remote.SelectRandomEPID();

            nsMessageHandler = app.Local.NSMessageHandler;
            sessionId = (uint)random.Next(50000, int.MaxValue);

            // These 2 fields are set when optimal bridge found.
            localBaseIdentifier = 0;
            localIdentifier = 0;

            invitation = new SLPRequestMessage(RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            invitation.Target = RemoteContactEPIDString;
            invitation.Source = LocalContactEPIDString;
            invitation.ContentType = "application/x-msnmsgr-sessionreqbody";
            invitation.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            app.SetupInviteMessage(invitation);

            app.P2PSession = this; // Register events
            remoteContact.DirectBridgeEstablished += RemoteDirectBridgeEstablished;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                String.Format("{0} created (Initiated locally)", SessionId), GetType().Name);

            status = P2PSessionStatus.WaitingForRemote;
            // Next step must be: Invite()
        }

        /// <summary>
        /// We are receiver. (Initiated remotely)
        /// </summary>
        public P2PSession(SLPRequestMessage slp, P2PMessage msg, NSMessageHandler ns, P2PBridge bridge)
        {
            nsMessageHandler = ns;
            invitation = slp;
            version = (slp.FromEndPoint == Guid.Empty || slp.ToEndPoint == Guid.Empty) ? P2PVersion.P2PV1 : P2PVersion.P2PV2;

            if (version == P2PVersion.P2PV1)
            {
                localContact = (slp.ToEmailAccount == ns.ContactList.Owner.Mail) ?
                    ns.ContactList.Owner : ns.ContactList.GetContact(slp.ToEmailAccount, ClientType.PassportMember);

                remoteContact = ns.ContactList.GetContact(slp.FromEmailAccount, ClientType.PassportMember);
            }
            else
            {
                localContact = (slp.ToEmailAccount == ns.ContactList.Owner.Mail) ?
                    ns.ContactList.Owner : ns.ContactList.GetContact(slp.ToEmailAccount, ClientType.PassportMember);
                localContactEndPointID = slp.ToEndPoint;

                remoteContact = ns.ContactList.GetContact(slp.FromEmailAccount, ClientType.PassportMember);
                remoteContactEndPointID = slp.FromEndPoint;
            }

            if (!uint.TryParse(slp.BodyValues["SessionID"].Value, out sessionId))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    "Can't parse SessionID: " + SessionId, GetType().Name);
            }

            p2pBridge = bridge;
            localBaseIdentifier = bridge.localTrackerId;
            localIdentifier = localBaseIdentifier;
            status = P2PSessionStatus.WaitingForLocal;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
              String.Format("{0} created (Initiated remotely)", SessionId), GetType().Name);

            remoteContact.DirectBridgeEstablished += RemoteDirectBridgeEstablished;

            if (msg != null)
            {
                // Set remote baseID
                remoteBaseIdentifier = msg.Header.Identifier;
                remoteIdentifier = remoteBaseIdentifier;

                if (version == P2PVersion.P2PV2)
                    remoteIdentifier += msg.V2Header.MessageSize;
            }

            // Create application based on invitation
            uint appId = slp.BodyValues.ContainsKey("AppID") ? uint.Parse(slp.BodyValues["AppID"].Value) : 0;
            Guid eufGuid = slp.BodyValues.ContainsKey("EUF-GUID") ? new Guid(slp.BodyValues["EUF-GUID"].Value) : Guid.Empty;
            Type appType = P2PApplication.GetApplication(eufGuid, appId);

            if (appType != null)
                p2pApplication = (P2PApplication)Activator.CreateInstance(appType, this);

            if (p2pApplication != null && p2pApplication.ValidateInvitation(invitation))
            {
                if (p2pApplication.AutoAccept)
                {
                    Accept(false);
                }
                else
                {
                    ns.P2PHandler.OnInvitationReceived(new P2PSessionEventArgs(this));
                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    String.Format("{0} app DECLINES this invite:\r\n{1}", SessionId, slp), GetType().Name);

                SLPStatusMessage slpMessage = new SLPStatusMessage(RemoteContactEPIDString, 500, "Internal Error");
                slpMessage.Target = RemoteContactEPIDString;
                slpMessage.Source = LocalContactEPIDString;
                slpMessage.Branch = invitation.Branch;
                slpMessage.CallId = invitation.CallId;
                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                P2PMessage p2pMessage = WrapSLPMessage(slpMessage);
                Send(p2pMessage);
            }
        }

        #endregion

        #region Invite / Accept / Decline / Close / Dispose

        /// <summary>
        /// Sends invitation message if initiated locally. Sets remote identifiers after ack received.
        /// </summary>
        public void Invite()
        {
            if (status != P2PSessionStatus.WaitingForRemote)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Invite called, but we're not waiting for the remote client (State {0})", status), GetType().Name);
            }
            else
            {
                if (UUNTried == false && Remote.DirectBridge == null &&
                    NSMessageHandler.UUNBridge.SuitableFor(this))
                {
                    UUNTried = true;
                    P2PSession.SendDirectInvite(NSMessageHandler, NSMessageHandler.UUNBridge, this);
                }

                // Get id from bridge....
                {
                    MigrateToOptimalBridge();
                    localBaseIdentifier = p2pBridge.localTrackerId;
                    localIdentifier = localBaseIdentifier;
                }


                P2PMessage p2pMessage = WrapSLPMessage(invitation);

                if (version == P2PVersion.P2PV2)
                {
                    if (p2pBridge.Synced == false)
                    {
                        p2pMessage.V2Header.OperationCode = (byte)(OperationCode.SYN | OperationCode.RAK);
                        p2pMessage.V2Header.AppendPeerInfoTLV();

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                        String.Format("{0} invitation sending with SYN+RAK op...", SessionId), GetType().Name);
                    }
                }

                p2pMessage.InnerMessage = invitation;

                Send(p2pMessage, delegate(P2PMessage ack)
                {
                    remoteBaseIdentifier = ack.Header.Identifier;
                    remoteIdentifier = remoteBaseIdentifier;

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                        String.Format("{0} invitation sent with SYN+RAK op and received RemoteBaseIdentifier is: {1}", SessionId, remoteIdentifier), GetType().Name);
                });
            }
        }

        public void Accept(bool sendDCInvite)
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Accept called, but we're not waiting for the local client (State {0})", status), GetType().Name);
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} accepted", SessionId), GetType().Name);

                SLPStatusMessage slpMessage = new SLPStatusMessage(RemoteContactEPIDString, 200, "OK");
                slpMessage.Target = RemoteContactEPIDString;
                slpMessage.Source = LocalContactEPIDString;
                slpMessage.Branch = invitation.Branch;
                slpMessage.CallId = invitation.CallId;
                slpMessage.CSeq = 1;
                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                Send(WrapSLPMessage(slpMessage), delegate(P2PMessage ack)
                {
                    if (sendDCInvite)
                        SendDirectInvite(nsMessageHandler, p2pBridge, this);

                    OnActive(EventArgs.Empty);

                    if (p2pApplication != null)
                    {
                        p2pApplication.Start();
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "Unable to start p2p application (null)", GetType().Name);
                    }

                });
            }
        }

        public void Decline()
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Declined called, but we're not waiting for the local client (State {0})", status), GetType().Name);
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} declined", SessionId), GetType().Name);

                SLPStatusMessage slpMessage = new SLPStatusMessage(RemoteContactEPIDString, 603, "Decline");
                slpMessage.Target = RemoteContactEPIDString;
                slpMessage.Source = LocalContactEPIDString;
                slpMessage.Branch = invitation.Branch;
                slpMessage.CallId = invitation.CallId;
                slpMessage.CSeq = 1;
                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                P2PMessage p2pMessage = new P2PMessage(Version);
                if (version == P2PVersion.P2PV1)
                {
                    p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                }
                else if (version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.OperationCode = (byte)OperationCode.RAK;
                    p2pMessage.V2Header.TFCombination = TFCombination.First;
                    p2pMessage.V2Header.PackageNumber = dataPacketNumber;

                    //dataPacketNumber++;
                }

                p2pMessage.InnerMessage = slpMessage;

                Send(p2pMessage, delegate(P2PMessage ack)
                {
                    Close();
                });
            }
        }

        public void Close()
        {
            if (status == P2PSessionStatus.Closing)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("P2PSession {0} was already closing, forcing unclean closure", SessionId), GetType().Name);

                OnClosed(new ContactEventArgs(Local));
            }
            else
            {
                OnClosing(new ContactEventArgs(Local));

                SLPRequestMessage slpMessage = new SLPRequestMessage(RemoteContactEPIDString, "BYE");
                slpMessage.Target = RemoteContactEPIDString;
                slpMessage.Source = LocalContactEPIDString;
                slpMessage.Branch = invitation.Branch;
                slpMessage.CallId = invitation.CallId;
                slpMessage.MaxForwards = 0;
                slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                P2PMessage p2pMessage = new P2PMessage(Version);
                if (version == P2PVersion.P2PV1)
                {
                    p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                }
                else if (version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.OperationCode = (byte)OperationCode.RAK;
                    p2pMessage.V2Header.TFCombination = TFCombination.First;
                    p2pMessage.V2Header.PackageNumber = dataPacketNumber;

                    //dataPacketNumber++;
                }

                p2pMessage.InnerMessage = slpMessage;

                Send(p2pMessage, delegate(P2PMessage ack)
                {
                    OnClosed(new ContactEventArgs(Local));
                });
            }
        }

        public void Dispose()
        {
            remoteContact.DirectBridgeEstablished -= RemoteDirectBridgeEstablished;

            DisposeApp();
            Migrate(null);
        }

        #endregion

        #region Event Handlers

        protected virtual void OnClosing(ContactEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("P2PSession {0} closing", sessionId), GetType().Name);

            status = P2PSessionStatus.Closing;

            if (Closing != null)
                Closing(this, e);

            DisposeApp();
        }

        protected virtual void OnClosed(ContactEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("P2PSession {0} closed", sessionId), GetType().Name);

            status = P2PSessionStatus.Closed;

            if (timeoutTimer != null)
            {
                timeoutTimer.Dispose();
                timeoutTimer = null;
            }

            if (Closed != null)
                Closed(this, e);

            DisposeApp();
        }

        protected virtual void OnError(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("P2PSession {0} error", sessionId), GetType().Name);

            status = P2PSessionStatus.Error;

            if (Error != null)
                Error(this, e);

            DisposeApp();
        }

        protected virtual void OnActive(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("P2PSession {0} active", sessionId), GetType().Name);

            status = P2PSessionStatus.Active;

            if (Activated != null)
                Activated(this, e);
        }

        private void DisposeApp()
        {
            if (p2pApplication != null)
            {
                p2pApplication.Dispose();
                p2pApplication = null;
            }
        }

        const int timeout = 120000;
        private Timer timeoutTimer;
        private void ResetTimeoutTimer()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Change(timeout, timeout);
                return;
            }

            timeoutTimer = new Timer(new TimerCallback(InactivityClose), this, timeout, timeout);
        }

        private void InactivityClose(object state)
        {
            Close();
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                String.Format("P2PSession {0} timed out through inactivity", sessionId), GetType().Name);
            //Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("Remain ackhandler count: {0}", NSMessageHandler.P2PHandler.AckHandlers), GetType().Name);
        }

        #endregion

        #region Public Methods

        public bool ProcessP2PMessage(P2PBridge bridge, P2PMessage p2pMessage, SLPMessage slp)
        {
            ResetTimeoutTimer();

            // Keep track of the remote identifier
            remoteIdentifier = (p2pMessage.Version == P2PVersion.P2PV2) ?
                p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize :
                p2pMessage.Header.Identifier;

            if (status == P2PSessionStatus.Closed || status == P2PSessionStatus.Error)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                       String.Format("P2PSession {0} received message whilst in '{1}' state", sessionId, status), GetType().Name);

                return false;
            }

            #region SESSION SLP LOOP

            if (slp != null)
            {
                if (slp is SLPRequestMessage)
                {
                    SLPRequestMessage slpRequest = slp as SLPRequestMessage;

                    if (slpRequest.ContentType == "application/x-msnmsgr-sessionclosebody" &&
                        slpRequest.Method == "BYE")
                    {
                        if (p2pMessage.Version == P2PVersion.P2PV1)
                        {
                            P2PMessage byeAck = p2pMessage.CreateAcknowledgement();
                            byeAck.V1Header.Flags = P2PFlag.CloseSession;
                            Send(byeAck);
                        }
                        else if (p2pMessage.Version == P2PVersion.P2PV2)
                        {
                            SLPRequestMessage slpMessage = new SLPRequestMessage(RemoteContactEPIDString, "BYE");
                            slpMessage.Target = RemoteContactEPIDString;
                            slpMessage.Source = LocalContactEPIDString;
                            slpMessage.Branch = invitation.Branch;
                            slpMessage.CallId = invitation.CallId;
                            slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";
                            slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                            Send(WrapSLPMessage(slpMessage));
                        }

                        OnClosed(new ContactEventArgs(Remote));

                        return true;
                    }
                    else
                    {
                        if (slpRequest.ContentType == "application/x-msnmsgr-transreqbody" ||
                            slpRequest.ContentType == "application/x-msnmsgr-transrespbody" ||
                            slpRequest.ContentType == "application/x-msnmsgr-transdestaddrupdate")
                        {
                            ProcessDirectInvite(slpRequest, nsMessageHandler, this); // Direct connection invite
                            return true;
                        }
                    }
                }
                else if (slp is SLPStatusMessage)
                {
                    SLPStatusMessage slpStatus = slp as SLPStatusMessage;

                    if (slpStatus.Code == 200) // OK
                    {
                        if (slpStatus.ContentType == "application/x-msnmsgr-transrespbody")
                        {
                            ProcessDirectInvite(slpStatus, nsMessageHandler, this);
                        }
                        else
                        {
                            OnActive(EventArgs.Empty);
                            p2pApplication.Start();
                        }

                        return true;
                    }
                    else if (slpStatus.Code == 603) // Decline
                    {
                        OnClosed(new ContactEventArgs(Remote));

                        return true;
                    }
                    else if (slpStatus.Code == 500) // Internal Error
                    {

                        return true;
                    }
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                     String.Format("Unhandled SLP Message in session:---->>\r\n{0}", p2pMessage.ToString()), GetType().Name);

                OnError(EventArgs.Empty);
                return true;
            }

            #endregion

            if (p2pApplication == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                      String.Format("P2PSession {0}: Received message for P2P app, but it's either been disposed or not created", sessionId), GetType().Name);

                return false;
            }
            else
            {
                // Application data
                if (p2pMessage.Header.MessageSize > 0 && p2pMessage.Header.SessionId > 0)
                {
                    bool reset = false;
                    byte[] appData = new byte[0];

                    if (p2pMessage.Header.MessageSize == 4 && BitUtility.ToInt32(p2pMessage.InnerBody, 0, true) == 0)
                    {
                        reset = true;
                    }
                    else
                    {
                        appData = new byte[p2pMessage.InnerBody.Length];
                        Buffer.BlockCopy(p2pMessage.InnerBody, 0, appData, 0, appData.Length);
                    }

                    if (p2pMessage.Version == P2PVersion.P2PV2 &&
                        (TFCombination.First == (p2pMessage.V2Header.TFCombination & TFCombination.First)))
                    {
                        reset = true;
                    }

                    return p2pApplication.ProcessData(bridge, appData, reset);
                }

                return false;
            }
        }


        public void Send(P2PMessage msg)
        {
            Send(msg, null);
        }

        public void Send(P2PMessage msg, AckHandler ackHandler)
        {
            ResetTimeoutTimer();

            if (p2pBridge == null)
                MigrateToOptimalBridge();

            p2pBridge.Send(this, Remote, RemoteContactEndPointID, msg, ackHandler);
        }

        private P2PMessage WrapSLPMessage(SLPMessage slpMessage)
        {
            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.TFCombination = TFCombination.First;
                p2pMessage.V2Header.PackageNumber = dataPacketNumber;
                //dataPacketNumber++;
            }
            else if (Version == P2PVersion.P2PV1)
            {
                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }

            return p2pMessage;
        }

        private void RemoteDirectBridgeEstablished(object sender, EventArgs args)
        {
            MigrateToOptimalBridge();
        }

        private void MigrateToOptimalBridge()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PSession {0} choosing optimal bridge", sessionId), GetType().Name);

            if ((remoteContact.DirectBridge != null) && remoteContact.DirectBridge.IsOpen)
            {
                Migrate(remoteContact.DirectBridge);
                return;
            }

            Migrate(nsMessageHandler.P2PHandler.GetBridge(this));
        }

        private void Migrate(P2PBridge newBridge)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                      String.Format("P2PSession {0} migrating from bridge {1} to {2}",
                      sessionId, (p2pBridge != null) ? p2pBridge.ToString() : "null",
                      (newBridge != null) ? newBridge.ToString() : "null"), GetType().Name);

            if (p2pBridge != null)
            {
                p2pBridge.BridgeOpened -= BridgeOpened;
                p2pBridge.BridgeSynced -= BridgeSynced;
                p2pBridge.BridgeClosed -= BridgeClosed;
                p2pBridge.BridgeSent -= BridgeSent;

                p2pBridge.MigrateQueue(this, newBridge);
            }

            p2pBridge = newBridge;

            if (p2pBridge != null)
            {
                p2pBridge.BridgeOpened += BridgeOpened;
                p2pBridge.BridgeSynced += BridgeSynced;
                p2pBridge.BridgeClosed += BridgeClosed;
                p2pBridge.BridgeSent += BridgeSent;

                localBaseIdentifier = p2pBridge.localTrackerId;
                localIdentifier = localBaseIdentifier;

                if ((directNegotiationTimer != null) && !(p2pBridge is SBBridge))
                    DirectNegotiationSuccessful();
            }
        }

        private void BridgeOpened(object sender, EventArgs args)
        {
            if (p2pBridge.Ready(this) && (p2pApplication != null))
                p2pApplication.BridgeIsReady();
        }

        private void BridgeSynced(object sender, EventArgs args)
        {
            localBaseIdentifier = p2pBridge.SyncId;
            localIdentifier = localBaseIdentifier;

            if (p2pBridge.Ready(this) && (p2pApplication != null))
                p2pApplication.BridgeIsReady();
        }

        private void BridgeClosed(object sender, EventArgs args)
        {
            if (remoteContact.Status == PresenceStatus.Offline)
                OnClosed(new ContactEventArgs(remoteContact));
            else
                MigrateToOptimalBridge();
        }

        private void BridgeSent(object sender, P2PMessageEventArgs args)
        {
            if (p2pBridge.Ready(this) && (p2pApplication != null))
                p2pApplication.BridgeIsReady();
        }

        #endregion


        public uint NextLocalIdentifier(int correction)
        {
            uint abs = (uint)Math.Abs(correction);
            bool desc = (correction < 0);
            uint ret = localIdentifier;

            if (Version == P2PVersion.P2PV1)
            {
                if (localIdentifier == localBaseIdentifier)
                {
                    localIdentifier++;
                }

                return localIdentifier++;
            }
            else if (Version == P2PVersion.P2PV2)
            {
                if (desc)
                {
                    localIdentifier -= abs;
                }
                else
                {
                    localIdentifier += abs;
                }
            }

            return ret;
        }

        /// <summary>
        /// Corrects the local identifier with the specified correction.
        /// </summary>
        /// <param name="correction"></param>
        public void CorrectLocalIdentifier(int correction)
        {
            if (correction < 0)
                LocalIdentifier -= (uint)Math.Abs(correction);
            else
                LocalIdentifier += (uint)Math.Abs(correction);
        }

        /// <summary>
        /// The identifier of the local client, increases with each message send
        /// </summary>		
        public void IncreaseLocalIdentifier()
        {
            localIdentifier++;
            if (localIdentifier == localBaseIdentifier)
                localIdentifier++;
        }

        public ushort IncreaseDataPacketNumber()
        {
            return ++dataPacketNumber;
        }

        /// <summary>
        /// The identifier of the remote client, increases with each message received
        /// </summary>
        public void IncreaseRemoteIdentifier()
        {
            remoteIdentifier++;
            if (remoteIdentifier == remoteBaseIdentifier)
                remoteIdentifier++;
        }
    }
};
