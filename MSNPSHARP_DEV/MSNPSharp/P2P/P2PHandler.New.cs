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
        /// <summary>
        /// Occurs when interaction with user has required for a file transfer, activity.
        /// Emoticons, display pictures and other msn objects are automatically accepted.
        /// </summary>
        public event EventHandler<P2PSessionEventArgs> InvitationReceived;

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
        }

        public void RegisterAckHandler(P2PMessage msg, AckHandler handler)
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

        public void ProcessP2PMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage p2pMessage)
        {
            // SLP BUFFERING: Combine splitted SLP messages
            if (slpMessagePool.BufferMessage(ref p2pMessage))
            {
                // * Buffering: Not completed yet, we must wait next packets -OR-
                // * Invalid packet received: Don't kill me, just ignore it...
                return;
            }

            SLPMessage slp = p2pMessage.IsSLPData ? p2pMessage.InnerMessage as SLPMessage : null;

            // RAK: Ack this message (if it isn't an ack itself and isn't an SLP message)
            // SLP are handled by p2p sessions.
            if (p2pMessage.Header.RequireAck && slp == null)
            {
                //P2PSession session2 = FindSession(p2pMessage, slp);
                //bridge.Send(null, source, sourceGuid, p2pMessage.CreateAcknowledgement());
            }

            // CHECK SLP: Check destination, source, endpoints
            if (slp != null)
            {
                if (!CheckSLPMessage(bridge, source, sourceGuid, p2pMessage, slp))
                    return;
            }

            // HANDLE ACK: ACK/NAK to our RAK message
            if (p2pMessage.Header.IsAcknowledgement || p2pMessage.Header.IsNegativeAck)
            {
                HandleAck(p2pMessage);
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("Received MSG\r\n{0}", p2pMessage.ToDebugString()), GetType().Name);

            // FIND SESSION: Search session by SessionId/ExpectedIdentifier
            P2PSession session = FindSession(p2pMessage, slp);

            if (session != null)
            {
                if (!session.ProcessP2PMessage(bridge, p2pMessage, slp))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        String.Format("P2PSession {0} could not process message\r\n{1}", session.SessionId, p2pMessage.ToDebugString()), GetType().Name);

                    // SendStatus(bridge, p2pMessage, session.Remote, session.RemoteContactEndPointID, 500, "Internal Error");
                }
                return;
            }

            // FIRST SLP MESSAGE: Create applications/sessions based on invitation
            if (slp != null)
            {
                if (ProcessSLPMessage(bridge, source, sourceGuid, p2pMessage, slp))
                    return;
            }

            // UNHANDLED P2P MESSAGE
            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                String.Format("*******Unhandled P2P message!*******\r\n{0}", p2pMessage.ToDebugString()), GetType().Name);
        }

        public bool ProcessSLPMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg, SLPMessage slp)
        {
            SLPRequestMessage req = slp as SLPRequestMessage;

            if (req != null && req.Method == "INVITE")
            {
                if (req.ContentType == "application/x-msnmsgr-sessionreqbody")
                {
                    // Start a new session
                    P2PSession session = new P2PSession(req, msg, nsMessageHandler);
                    session.Closed += P2PSessionClosed;

                    if (session.Version == P2PVersion.P2PV2)
                        p2pV2Sessions.Add(session);
                    else
                        p2pV1Sessions.Add(session);

                    return true;
                }
            }

            return false;
        }


        public P2PSession FindSession(P2PMessage msg, SLPMessage slp)
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

            if (sessionID == 0)
                return null;

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

            return null;
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

        protected internal virtual void OnInvitationReceived(P2PSessionEventArgs e)
        {
            if (InvitationReceived != null)
                InvitationReceived(this, e);
        }

        public void Dispose()
        {
            slpMessagePool = new P2PMessagePool();


        }

        private void HandleAck(P2PMessage p2pMessage)
        {
            KeyValuePair<P2PMessage, AckHandler>? pair = null;
            uint ackNakId = 0;

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

        private bool CheckSLPMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg, SLPMessage slp)
        {
            string src = source.Mail.ToLowerInvariant();
            string target = nsMessageHandler.ContactList.Owner.Mail.ToLowerInvariant();

            if (msg.Version == P2PVersion.P2PV2)
            {
                src += ";" + sourceGuid.ToString("B").ToLowerInvariant();
                target += ";" + NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();
            }

            if (slp.Source.ToLowerInvariant() != src)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received message from '{0}', differing from source '{1}'", slp.Source, src), GetType().Name);

                return false;
            }
            else if (slp.Target.ToLowerInvariant() != target)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received P2P message intended for '{0}', not us '{1}'", slp.Target, target), GetType().Name);

                if (slp.Source == target)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        "We received a message from ourselves?", GetType().Name);
                }
                else
                {
                    bridge.Send(null, source, sourceGuid, msg.CreateAcknowledgement());
                    SendStatus(bridge, msg, source, sourceGuid, 404, "Not Found");
                }

                return false;
            }

            return true;
        }

        private void SendStatus(P2PBridge bridge, P2PMessage msg, Contact dest, Guid destGuid, int code, string phrase)
        {
            string target = dest.Mail.ToLowerInvariant();

            if (msg.Version == P2PVersion.P2PV2)
            {
                target += ";" + destGuid.ToString("B");
            }

            SLPMessage slp = new SLPStatusMessage(target, code, phrase);

            if (msg.IsSLPData)
            {
                SLPMessage msgSLP = msg.InnerMessage as SLPMessage;
                slp.Branch = msgSLP.Branch;
                slp.CallId = msgSLP.CallId;
                slp.Source = msgSLP.Target;
                slp.ContentType = msgSLP.ContentType;
            }
            else
                slp.ContentType = "null";

            P2PMessage response = new P2PMessage(msg.Version);
            response.InnerMessage = slp;

            if (msg.Version == P2PVersion.P2PV1)
            {
                response.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }
            else if (msg.Version == P2PVersion.P2PV2)
            {
                response.V2Header.OperationCode = (byte)OperationCode.None;
                response.V2Header.TFCombination = TFCombination.First;
            }

            //RegisterAckHandler(msg, delegate {});
            bridge.Send(null, dest, destGuid, response);
        }



        internal P2PBridge GetBridge(P2PSession session)
        {
            foreach (P2PBridge existing in bridges)
                if (existing.SuitableFor(session))
                    return existing;

            P2PBridge bridge = new SBBridge(session);
            bridge.BridgeClosed += BridgeClosed;

            bridges.Add(bridge);

            return bridge;
        }

        internal P2PBridge GetBridge(Conversation sb)
        {
            foreach (P2PBridge existing in bridges)
            {
                if (!(existing is SBBridge))
                    continue;

                if ((existing as SBBridge).Switchboard == sb)
                    return existing;
            }

            P2PBridge bridge = new SBBridge(sb);
            bridge.BridgeClosed += BridgeClosed;
            bridges.Add(bridge);

            return bridge;
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

    }
};