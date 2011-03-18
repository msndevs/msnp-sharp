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
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    using MSNPSharp;
    using MSNPSharp.Apps;
    using MSNPSharp.Core;

    public delegate void AckHandler(P2PMessage ack);

    /// <summary>
    /// New P2P handler
    /// </summary>
    public class P2PHandler : IDisposable
    {
        #region Events

        /// <summary>
        /// Occurs when interaction with user has required for a file transfer, activity.
        /// Emoticons, display pictures and other msn objects are automatically accepted.
        /// </summary>
        public event EventHandler<P2PSessionEventArgs> InvitationReceived;

        protected internal virtual void OnInvitationReceived(P2PSessionEventArgs e)
        {
            if (InvitationReceived != null)
                InvitationReceived(this, e);
        }

        #endregion

        #region Members

        private SDGBridge sdgBridge;
        private SLPHandler slpHandler;
        private NSMessageHandler nsMessageHandler = null;
        private P2PMessagePool slpMessagePool = new P2PMessagePool();
        private List<P2PBridge> bridges = new List<P2PBridge>();
        private List<P2PSession> p2pV1Sessions = new List<P2PSession>();
        private List<P2PSession> p2pV2Sessions = new List<P2PSession>();
        private Dictionary<uint, KeyValuePair<P2PMessage, AckHandler>> ackHandlersV1 = new Dictionary<uint, KeyValuePair<P2PMessage, AckHandler>>();
        private Dictionary<uint, KeyValuePair<P2PMessage, AckHandler>> ackHandlersV2 = new Dictionary<uint, KeyValuePair<P2PMessage, AckHandler>>();

        protected internal P2PHandler(NSMessageHandler nsHandler)
        {
            this.nsMessageHandler = nsHandler;
            this.sdgBridge = new SDGBridge(nsHandler);
            this.slpHandler = new SLPHandler(nsHandler);            
        }

        #endregion

        #region Properties

        public SDGBridge SDGBridge
        {
            get
            {
                return sdgBridge;
            }
        }

        #endregion


        #region Public

        #region RequestMsnObject & SendFile & AddTransfer

        public ObjectTransfer RequestMsnObject(Contact remoteContact, MSNObject msnObject)
        {
            ObjectTransfer objectTransferApp = new ObjectTransfer(msnObject, remoteContact);

            AddTransfer(objectTransferApp);

            return objectTransferApp;
        }

        public FileTransfer SendFile(Contact remoteContact, string filename, FileStream fileStream)
        {
            FileTransfer fileTransferApp = new FileTransfer(remoteContact, fileStream, Path.GetFileName(filename));

            AddTransfer(fileTransferApp);

            return fileTransferApp;
        }

        public P2PSession AddTransfer(P2PApplication app)
        {
            P2PSession session = new P2PSession(app);
            session.Closed += P2PSessionClosed;

            if (app.P2PVersion == P2PVersion.P2PV2)
                p2pV2Sessions.Add(session);
            else
                p2pV1Sessions.Add(session);

            session.Invite();

            return session;
        }

        #endregion

        #region ProcessP2PMessage

        public void ProcessP2PMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage p2pMessage)
        {
            // 1) HANDLE RAK: RAKs are session independent and mustn't be quoted on bridges.
            bool requireAck = HandleRAK(bridge, source, sourceGuid, p2pMessage);

            // 2) SLP BUFFERING: Combine splitted SLP messages
            if (slpMessagePool.BufferMessage(ref p2pMessage))
            {
                // * Buffering: Not completed yet, we must wait next packets -OR-
                // * Invalid packet received: Don't kill me, just ignore it...
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("Received P2PMessage from {0}\r\n{1}", bridge.ToString(), p2pMessage.ToDebugString()), GetType().Name);

            // 3) CHECK SLP: Check destination, source, endpoints
            SLPMessage slp = p2pMessage.IsSLPData ? p2pMessage.InnerMessage as SLPMessage : null;
            if (slp != null)
            {
                if (!slpHandler.CheckSLPMessage(bridge, source, sourceGuid, p2pMessage, slp))
                    return;
            }

            // 4) HANDLE ACK: ACK/NAK to our RAK message
            if (HandleACK(p2pMessage))
            {
                return;
            }

            // 5) FIRST SLP MESSAGE: Create applications/sessions based on invitation
            if (slp != null && slp is SLPRequestMessage &&
                (slp as SLPRequestMessage).Method == "INVITE" &&
                slp.ContentType == "application/x-msnmsgr-sessionreqbody")
            {
                uint appId = slp.BodyValues.ContainsKey("AppID") ? uint.Parse(slp.BodyValues["AppID"].Value) : 0;
                Guid eufGuid = slp.BodyValues.ContainsKey("EUF-GUID") ? new Guid(slp.BodyValues["EUF-GUID"].Value) : Guid.Empty;
                P2PVersion ver = slp.ToEndPoint == Guid.Empty ? P2PVersion.P2PV1 : P2PVersion.P2PV2;

                if (P2PApplication.IsRegistered(eufGuid, appId))
                {
                    bool sessionExists = false;
                    P2PSession newSession = null;

                    if (ver == P2PVersion.P2PV2)
                    {
                        foreach (P2PSession s in p2pV2Sessions)
                        {
                            if (s.Invitation.CallId == slp.CallId)
                            {
                                newSession = s;
                                sessionExists = true;
                                break;
                            }
                        }

                        if (sessionExists == false)
                        {
                            newSession = new P2PSession(slp as SLPRequestMessage, p2pMessage, nsMessageHandler, bridge);
                            newSession.Closed += P2PSessionClosed;
                            p2pV2Sessions.Add(newSession);
                        }
                    }
                    else
                    {
                        foreach (P2PSession s in p2pV1Sessions)
                        {
                            if (s.Invitation.CallId == slp.CallId)
                            {
                                newSession = s;
                                sessionExists = true;
                                break;
                            }
                        }

                        if (sessionExists == false)
                        {
                            newSession = new P2PSession(slp as SLPRequestMessage, p2pMessage, nsMessageHandler, bridge);
                            newSession.Closed += P2PSessionClosed;
                            p2pV1Sessions.Add(newSession);
                        }
                    }

                    return;
                }

                // Not registered application. Decline it without create a new session...
                slpHandler.SendSLPStatus(bridge, p2pMessage, source, sourceGuid, 603, "Decline");
                return;
            }

            // 6) FIND SESSION: Search session by SessionId/ExpectedIdentifier
            P2PSession session = FindSession(p2pMessage, slp);
            if (session != null)
            {
                // ResetTimeoutTimer();

                // Keep track of theremoteIdentifier

                // Keep track of the remote identifier
                session.remoteIdentifier = (p2pMessage.Version == P2PVersion.P2PV2) ?
                    p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize :
                    p2pMessage.Header.Identifier;

                // Session SLP
                if (slp != null && slpHandler.HandleP2PSessionSignal(bridge, p2pMessage, slp, session))
                    return;

                // Session Data
                if (slp == null && session.ProcessP2PData(bridge, p2pMessage))
                    return;
            }


            
            if (!requireAck)
            {
                // UNHANDLED P2P MESSAGE
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("*******Unhandled P2P message!******* PING sent:\r\n{0}", p2pMessage.ToDebugString()), GetType().Name);

                P2PMessage ping = new P2PMessage(p2pMessage.Version);
                bridge.Send(null, source, sourceGuid, ping, null);
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            lock (slpMessagePool)
                slpMessagePool.Clear();

            lock (p2pV1Sessions)
                p2pV1Sessions.Clear();

            lock (p2pV2Sessions)
                p2pV2Sessions.Clear();

            lock (bridges)
                bridges.Clear();

            lock (ackHandlersV1)
                ackHandlersV1.Clear();

            lock (ackHandlersV2)
                ackHandlersV2.Clear();

        }

        #endregion

        #endregion

        #region Internal & Protected

        #region RegisterAckHandler

        internal void RegisterAckHandler(P2PMessage msg, AckHandler handler)
        {
            if (msg.Version == P2PVersion.P2PV2)
            {
                msg.V2Header.OperationCode |= (byte)OperationCode.RAK;
                ackHandlersV2[msg.V2Header.Identifier + msg.Header.MessageSize] = new KeyValuePair<P2PMessage, AckHandler>(msg, handler);
            }
            else if (msg.Version == P2PVersion.P2PV1)
            {
                ackHandlersV1[msg.V1Header.AckSessionId] = new KeyValuePair<P2PMessage, AckHandler>(msg, handler);
            }
        }

        #endregion

        #region GetBridge & BridgeClosed

        internal P2PBridge GetBridge(P2PSession session)
        {
            

            foreach (P2PBridge existing in bridges)
                if (existing.SuitableFor(session))
                    return existing;


            return nsMessageHandler.SDGBridge;

            /*MSNP21TODO
            P2PBridge bridge = new SBBridge(session);
            bridge.BridgeClosed += BridgeClosed;

            bridges.Add(bridge);

            return bridge;
             * */
            return null;
        }

        private void BridgeClosed(object sender, EventArgs args)
        {
            P2PBridge bridge = sender as P2PBridge;

            if (!bridges.Contains(bridge))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Closed bridge not found in list", GetType().Name);
                return;
            }

            bridges.Remove(bridge);
        }

        #endregion

        #endregion

        #region Private

        #region HandleAck

        private bool HandleACK(P2PMessage p2pMessage)
        {
            bool isAckOrNak = false;

            if (p2pMessage.Header.IsAcknowledgement || p2pMessage.Header.IsNegativeAck)
            {
                KeyValuePair<P2PMessage, AckHandler>? pair = null;
                uint ackNakId = 0;
                isAckOrNak = true;

                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    lock (ackHandlersV1)
                    {
                        if (ackHandlersV1.ContainsKey(p2pMessage.Header.AckIdentifier))
                        {
                            ackNakId = p2pMessage.Header.AckIdentifier;
                            pair = ackHandlersV1[ackNakId];
                            ackHandlersV1.Remove(ackNakId);
                        }
                    }
                }
                else if (p2pMessage.Version == P2PVersion.P2PV2)
                {
                    lock (ackHandlersV2)
                    {
                        if (ackHandlersV2.ContainsKey(p2pMessage.Header.AckIdentifier))
                        {
                            ackNakId = p2pMessage.Header.AckIdentifier;
                            pair = ackHandlersV2[ackNakId];
                            ackHandlersV2.Remove(ackNakId);
                        }
                        else if (ackHandlersV2.ContainsKey(p2pMessage.V2Header.NakIdentifier))
                        {
                            ackNakId = p2pMessage.V2Header.NakIdentifier;
                            pair = ackHandlersV2[ackNakId];
                            ackHandlersV2.Remove(ackNakId);
                        }
                    }
                }

                if (ackNakId == 0)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        String.Format("!!!!!! No AckHandler registered for ack/nak {0}:\r\n{1}", p2pMessage.Header.AckIdentifier, p2pMessage.ToDebugString()), GetType().Name);
                }
                else
                {
                    if (pair.HasValue && pair.Value.Value != null)
                    {
                        pair.Value.Value(p2pMessage);
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            String.Format("!!!!!! No AckHandler pair for ack {0}\r\n{1}", ackNakId, p2pMessage.ToDebugString()), GetType().Name);
                    }
                }
            }

            return isAckOrNak;
        }

        #endregion

        #region HandleRAK

        private bool HandleRAK(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg)
        {
            bool requireAck = false;

            if (msg.Header.RequireAck)
            {
                requireAck = true;

                P2PMessage ack = msg.CreateAcknowledgement();
                ack.Header.Identifier = bridge.localTrackerId;

                if (ack.Header.RequireAck)
                {
                    // SYN
                    bridge.Send(null, source, sourceGuid, ack, delegate(P2PMessage sync)
                    {
                        bridge.SyncId = sync.Header.AckIdentifier;

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            String.Format("SYNC completed for: {0}", bridge), GetType().Name);
                    });
                }
                else
                {
                    // ACK
                    bridge.Send(null, source, sourceGuid, ack, null);
                }
            }

            return requireAck;
        }

        #endregion


        #region FindSession & P2PSessionClosed

        private P2PSession FindSession(P2PMessage msg, SLPMessage slp)
        {
            uint sessionID = msg.Header.SessionId;

            if ((sessionID == 0) && (slp != null))
            {
                if (slp.BodyValues.ContainsKey("SessionID"))
                {
                    if (!uint.TryParse(slp.BodyValues["SessionID"].Value, out sessionID))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                           "Unable to parse SLP message SessionID", GetType().Name);

                        sessionID = 0;
                    }
                }

                if (sessionID == 0)
                {
                    // We don't get a session ID in BYE requests
                    // So we need to find the session by its call ID
                    if (msg.Version == P2PVersion.P2PV2)
                    {
                        foreach (P2PSession session in p2pV2Sessions)
                        {
                            if (session.Invitation.CallId == slp.CallId)
                                return session;
                        }
                    }
                    else
                    {
                        foreach (P2PSession session in p2pV1Sessions)
                        {
                            if (session.Invitation.CallId == slp.CallId)
                                return session;
                        }
                    }
                }
            }

            // Sometimes we only have a messageID to find the session with...
            if ((sessionID == 0) && (msg.Header.Identifier != 0))
            {
                if (msg.Version == P2PVersion.P2PV2)
                {
                    foreach (P2PSession session in p2pV2Sessions)
                    {
                        uint expected = session.RemoteIdentifier;

                        if (msg.Header.Identifier == expected)
                            return session;
                    }
                }
                else
                {
                    foreach (P2PSession session in p2pV1Sessions)
                    {
                        uint expected = session.RemoteIdentifier + 1;
                        if (expected == session.RemoteBaseIdentifier)
                            expected++;

                        if (msg.Header.Identifier == expected)
                            return session;
                    }
                }
            }

            if (sessionID != 0)
            {
                if (msg.Version == P2PVersion.P2PV2)
                {
                    foreach (P2PSession session in p2pV2Sessions)
                    {
                        if (session.SessionId == sessionID)
                            return session;
                    }
                }
                else
                {
                    foreach (P2PSession session in p2pV1Sessions)
                    {
                        if (session.SessionId == sessionID)
                            return session;
                    }
                }
            }

            return null;
        }

        private void P2PSessionClosed(object sender, ContactEventArgs args)
        {
            P2PSession session = sender as P2PSession;

            session.Closed -= P2PSessionClosed;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PSession {0} closed, removing", session.SessionId), GetType().Name);

            if (session.Version == P2PVersion.P2PV2)
                p2pV2Sessions.Remove(session);
            else
                p2pV1Sessions.Remove(session);

            session.Dispose();
        }

        #endregion

        #endregion
    }
};