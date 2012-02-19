#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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

        public bool ProcessP2PMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage p2pMessage)
        {
            // 1) SLP BUFFERING: Combine splitted SLP messages
            if (slpMessagePool.BufferMessage(ref p2pMessage))
            {
                // * Buffering: Not completed yet, we must wait next packets -OR-
                // * Invalid packet received: Don't kill me, just ignore it...
                return true;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("Received P2PMessage from {0}\r\n{1}", bridge.ToString(), p2pMessage.ToDebugString()), GetType().Name);

            // 2) CHECK SLP: Check destination, source, endpoints
            SLPMessage slp = p2pMessage.IsSLPData ? p2pMessage.InnerMessage as SLPMessage : null;
            if (slp != null)
            {
                if (!slpHandler.CheckSLPMessage(bridge, source, sourceGuid, p2pMessage, slp))
                    return true; // HANDLED, This SLP is not for us.
            }

            // 3) FIRST SLP MESSAGE: Create applications/sessions based on invitation
            if (slp != null && slp is SLPRequestMessage &&
                (slp as SLPRequestMessage).Method == "INVITE" &&
                slp.ContentType == "application/x-msnmsgr-sessionreqbody")
            {
                uint appId = slp.BodyValues.ContainsKey("AppID") ? uint.Parse(slp.BodyValues["AppID"].Value) : 0;
                Guid eufGuid = slp.BodyValues.ContainsKey("EUF-GUID") ? new Guid(slp.BodyValues["EUF-GUID"].Value) : Guid.Empty;
                P2PVersion ver = slp.P2PVersion;

                if (P2PApplication.IsRegistered(eufGuid, appId))
                {
                    P2PSession newSession = FindSessionByCallId(slp.CallId, ver);

                    if (newSession == null)
                    {
                        newSession = new P2PSession(slp as SLPRequestMessage, p2pMessage, nsMessageHandler, bridge);
                        newSession.Closed += P2PSessionClosed;

                        if (newSession.Version == P2PVersion.P2PV2)
                            p2pV2Sessions.Add(newSession);
                        else
                            p2pV1Sessions.Add(newSession);
                    }
                    else
                    {
                        // P2PSession exists, bridge changed...
                        if (newSession.Bridge != bridge)
                        {
                            // BRIDGETODO
                        }
                    }

                    return true;
                }

                // Not registered application. Decline it without create a new session...
                slpHandler.SendSLPStatus(bridge, p2pMessage, source, sourceGuid, 603, "Decline");
                return true;
            }

            // 4) FIND SESSION: Search session by SessionId/ExpectedIdentifier
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
                    return true;

                // Session Data
                if (slp == null && session.ProcessP2PData(bridge, p2pMessage))
                    return true;
            }

            return false;
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

            sdgBridge.Dispose();
            slpHandler.Dispose();
        }

        #endregion

        #endregion

        #region Internal & Protected

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

        #region FindSession & P2PSessionClosed

        private P2PSession FindSessionByCallId(Guid callId, P2PVersion version)
        {
            List<P2PSession> sessions = (version == P2PVersion.P2PV2) ? p2pV2Sessions : p2pV1Sessions;

            foreach (P2PSession session in sessions)
            {
                if (session.Invitation.CallId == callId)
                    return session;
            }

            return null;
        }

        private P2PSession FindSessionBySessionId(uint sessionId, P2PVersion version)
        {
            List<P2PSession> sessions = (version == P2PVersion.P2PV2) ? p2pV2Sessions : p2pV1Sessions;

            foreach (P2PSession session in sessions)
            {
                if (session.SessionId == sessionId)
                    return session;
            }

            return null;
        }

        private P2PSession FindSessionByExpectedIdentifier(P2PMessage p2pMessage)
        {
            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                foreach (P2PSession session in p2pV2Sessions)
                {
                    uint expected = session.RemoteIdentifier;

                    if (p2pMessage.Header.Identifier == expected)
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

                    if (p2pMessage.Header.Identifier == expected)
                        return session;
                }
            }

            return null;
        }

        private P2PSession FindSession(P2PMessage msg, SLPMessage slp)
        {
            P2PSession p2pSession = null;
            uint sessionID = (msg != null) ? msg.Header.SessionId : 0;

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
                    P2PVersion p2pVersion = slp.P2PVersion;
                    p2pSession = FindSessionByCallId(slp.CallId, p2pVersion);

                    if (p2pSession != null)
                        return p2pSession;
                }
            }

            // Sometimes we only have a messageID to find the session with...
            if ((sessionID == 0) && (msg.Header.Identifier != 0))
            {
                p2pSession = FindSessionByExpectedIdentifier(msg);

                if (p2pSession != null)
                    return p2pSession;
            }

            if (sessionID != 0)
            {
                p2pSession = FindSessionBySessionId(sessionID, msg.Version);

                if (p2pSession != null)
                    return p2pSession;
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
