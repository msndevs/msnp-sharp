#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    #region Event args

    public class P2PSessionEventArgs : EventArgs
    {
        private P2PSession session;

        public P2PSession Session
        {
            get
            {
                return session;
            }
            set
            {
                session = value;
            }
        }

        public P2PSessionEventArgs(P2PSession affectedSession)
        {
            session = affectedSession;
        }
    }

    #endregion

    partial class P2PHandler
    {
        internal Dictionary<uint, KeyValuePair<P2PMessage, P2PAckHandler>> ackHandlers = new Dictionary<uint, KeyValuePair<P2PMessage, P2PAckHandler>>();
        private Dictionary<uint, MemoryStream> messageStreams = new Dictionary<uint, MemoryStream>();
        private List<IMessageProcessor> bridges = new List<IMessageProcessor>();
        private List<P2PSession> sessions = new List<P2PSession>();
        private NSMessageHandler nsMessageHandler;

        /// <summary>
        /// The nameserver handler. This object is used to request new switchboard sessions.
        /// </summary>
        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
            set
            {
                nsMessageHandler = value;
                nsMessageHandler.SBCreated += nsMessageHandler_SBCreated;
                nsMessageHandler.ContactOffline += nsMessageHandler_ContactOffline;
            }
        }

        /// <summary>
        /// Registers events of the new switchboard in order to act on these.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsMessageHandler_SBCreated(object sender, SBCreatedEventArgs e)
        {

            SBMessageHandler sbHandler = e.Switchboard;
            sbHandler.MessageProcessor.RegisterHandler(this);

            // act on these events to ensure messages are properly sent to the right switchboards
            e.Switchboard.ContactJoined += Switchboard_ContactJoined;
            e.Switchboard.ContactLeft += Switchboard_ContactLeft;
            e.Switchboard.SessionClosed += Switchboard_SessionClosed;
            // keep track of this switchboard
            AddSwitchboardSession(sbHandler);
        }

        /// <summary>
        /// Cleans up p2p resources associated with the offline contact.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsMessageHandler_ContactOffline(object sender, ContactEventArgs e)
        {
            // destroy all message sessions that dealt with this contact
            lock (sessions)
            {
                foreach (P2PSession session in sessions)
                {
                    if (session.Remote.Mail == e.Contact.Mail)
                        session.Close();
                }
            }
        }

        /// <summary>
        /// Updates the internal switchboard collection to reflect the changes.
        /// </summary>
        /// <remarks>
        /// Conversations with more than one contact are not found suitable for p2p transfers.
        /// If multiple contacts are present, then any message sessions associated with the switchboard are unplugged.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
        {
            SBMessageHandler handler = (SBMessageHandler)sender;

            // MSNP18: Owner in the switchboard, so there're 2 contacts.
            bool canremove = (handler.NSMessageHandler.Credentials.MsnProtocol >= MsnProtocol.MSNP18 && handler.Contacts.ContainsKey(handler.NSMessageHandler.Owner.Mail))
                ? (handler.Contacts.Count > 2)
                : (handler.Contacts.Count > 1);

            if (canremove)
            {
                lock (sessions)
                {
                    // In a conversation with multiple contacts we don't want to send p2p messages.
                    foreach (P2PSession session in sessions)
                    {
                        if (session.MessageProcessor == handler.MessageProcessor &&
                            handler.Contacts.ContainsKey(handler.NSMessageHandler.Owner.Mail))
                        {
                            // set this to null to prevent sending messages to this processor.
                            // it will invalidate the processor when trying to send messages and thus
                            // force the p2p handler to create a new switchboard
                            RemoveSessionProcessor(session);
                        }
                    }
                }
            }

            // MSNP18: owner in the switchboard, so there're 2 contacts.
            bool canadd = (handler.NSMessageHandler.Credentials.MsnProtocol >= MsnProtocol.MSNP18 && handler.Contacts.ContainsKey(handler.NSMessageHandler.Owner.Mail))
                ? (handler.Contacts.Count == 2)
                : (handler.Contacts.Count == 1);

            if (canadd)
            {
                P2PSession session = GetSessionFromRemote(e.Contact.Mail);

                // check whether the session exists and is valid
                // if so, we set this switchboard as new output gateway for the message session.
                if (session != null && session.ProcessorValid == false)
                {
                    AddSessionProcessor(session, handler.MessageProcessor);
                }
            }
        }

        /// <summary>
        /// Removes the switchboard processor from the corresponding p2p message session, if necessary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_ContactLeft(object sender, ContactEventArgs e)
        {
            SBMessageHandler handler = (SBMessageHandler)sender;
            P2PSession session = GetSessionFromRemote(e.Contact.Mail);
            if (session != null && session.MessageProcessor == handler.MessageProcessor)
            {
                // invalidate the processor
                RemoveSessionProcessor(session);
            }
        }



        public IEnumerable<P2PSession> Sessions
        {
            get
            {
                return sessions;
            }
        }

        public P2PSession RequestMSNObject(Contact remote, MSNObject msnObject)
        {
            return AddTransfer(new P2PObjectTransferApplication(msnObject, remote));
        }

        public P2PSession SendFile(Contact remote, string filename)
        {
            return SendFile(remote, filename, new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public P2PSession SendFile(Contact remote, string filename, Stream data)
        {
            return AddTransfer(new P2PFileTransferApplication(remote, data, Path.GetFileName(filename)));
        }

        public P2PSession AddTransfer(P2PApplication app)
        {
            P2PSession session = new P2PSession(app);
            session.Closed += P2PSessionClosed;

            lock (sessions)
                sessions.Add(session);

            return session;
        }

        internal void RegisterP2PAckHandler(P2PMessage msg, P2PAckHandler handler)
        {
            ackHandlers[msg.AckSessionId] = new KeyValuePair<P2PMessage, P2PAckHandler>(msg, handler);
        }

        public P2PSession FindSession(P2PMessage p2pMessage, SLPMessage slp)
        {
            uint sessionID = p2pMessage.SessionId;

            if (sessionID == 0 && slp != null)
            {
                // Get from slp
                if (slp.BodyValues.ContainsKey("SessionID"))
                {
                    if (!uint.TryParse(slp.BodyValues["SessionID"].Value, out sessionID))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unable to parse SLP message SessionID", GetType().Name);
                        sessionID = 0;
                    }
                }

                // BYE request
                if (sessionID == 0)
                {
                    foreach (P2PSession session in sessions)
                    {
                        if (session.Invite.CallId == slp.CallId)
                            return session;
                    }
                }
            }

            // Sometimes we only have a message ID, the waiting flag (4)
            if (sessionID == 0 && p2pMessage.Identifier != 0)
            {
                foreach (P2PSession session in sessions)
                {
                    uint expected = session.RemoteIdentifier + 1;
                    if (expected == session.RemoteBaseIdentifier)
                        expected++;

                    if (p2pMessage.Identifier == expected)
                        return session;
                }
            }

            if (sessionID == 0)
                return null;

            foreach (P2PSession session in sessions)
            {
                if (session.SessionId == sessionID)
                    return session;
            }

            return null;
        }

        private void P2PSessionClosed(object sender, EventArgs args)
        {
            P2PSession session = sender as P2PSession;
            session.Closed -= P2PSessionClosed;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} closed, removing", session.SessionId), GetType().Name);

            lock (sessions)
                sessions.Remove(session);

            session.Dispose();
        }


        bool CheckSLPMessage(IMessageProcessor bridge, string source, P2PMessage msg, SLPMessage slp)
        {
            if (slp.FromMail != source)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Received message from '{0}', differing from source '{1}'", slp.FromMail, source), GetType().Name);
                return false;
            }
            else if (slp.ToMail != NSMessageHandler.Owner.Mail)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Received P2P message intended for {0}, not us\r\n{1}", slp.ToMail, msg.ToString()), GetType().Name);
                bridge.SendMessage(msg.CreateAcknowledgement());
                SendStatus(bridge, msg, source, 404, "Not Found");
                return false;
            }

            return true;
        }

        void SendStatus(IMessageProcessor bridge, P2PMessage msg, string dest, int code, string phrase)
        {
            SLPStatusMessage slp = new SLPStatusMessage(dest, code, phrase);

            if (msg.InnerBody != null)
            {
                SLPMessage s = SLPMessage.Parse(msg.InnerBody);
                slp.Branch = s.Branch;
                slp.CallId = s.CallId;
                slp.FromMail = s.ToMail;
                slp.ContentType = s.ContentType;
            }
            else
                slp.ContentType = "null";

            P2PMessage response = new P2PMessage();
            response.InnerMessage = slp;
            response.MessageSize = (uint)slp.GetBytes().Length;
            response.Flags = P2PFlag.MSNSLPInfo;

            RegisterP2PAckHandler(response, delegate
            {
            });
            bridge.SendMessage(msg);
        }


        /// <summary>
        /// Handles incoming sb messages. Other messages are ignored.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            SBMessage sbMessage = message as SBMessage;

            Debug.Assert(sbMessage != null, "Incoming message is not a SBMessage");

            if (sbMessage.Command == "BYE")
            {
                string source = sbMessage.CommandValues[0].ToString();
                // Clean the MessageSessions, or it will lead to memory leak.
                lock (messageSessions)
                {
                    ArrayList list = new ArrayList(messageSessions);
                    foreach (P2PMessageSession p2psession in list)
                    {
                        if (p2psession.RemoteContact == source)
                        {
                            messageSessions.Remove(p2psession);
                            p2psession.CleanUp();
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "P2PMessageSession removed: " + p2psession.RemoteContact + ", remain session count: " + messageSessions.Count.ToString());
                        }
                    }
                }
                return;
            }

            if (sbMessage.Command != "MSG")
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "No MSG: " + sbMessage.Command + " instead", GetType().Name);
                return;
            }

            // Create a MSGMessage from the sb message
            MSGMessage msgMessage = new MSGMessage();
            try
            {
                msgMessage.CreateFromMessage(sbMessage);
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString(), GetType().Name);
            }

            // check if it's a valid p2p message
            if (!msgMessage.MimeHeader.ContainsKey("Content-Type") ||
                msgMessage.MimeHeader["Content-Type"].ToString() != "application/x-msnmsgrp2p")
            {
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming p2p message " + NSMessageHandler.Owner.Mail, GetType().Name);

            // create a P2P Message from the msg message
            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.CreateFromMessage(msgMessage);

            if (HandleSplitMessage(ref p2pMessage))
                return;

            if (p2pMessage.ShouldAck)
                sender.SendMessage(p2pMessage.CreateAcknowledgement());

            SLPMessage slp = null;
            if (p2pMessage.InnerBody != null && p2pMessage.InnerBody.Length != 0)
            {
                slp = SLPMessage.Parse(p2pMessage.InnerBody);
                if (slp != null && !CheckSLPMessage(sender, slp.FromMail, p2pMessage, slp))
                    return;
            }

            if (p2pMessage.AckIdentifier != 0)
            {
                // This is an ack
                if (ackHandlers.ContainsKey(p2pMessage.AckIdentifier))
                {
                    // An AckHandler has been registered for this ack
                    KeyValuePair<P2PMessage, P2PAckHandler> pair = ackHandlers[p2pMessage.AckIdentifier];

                    if (pair.Value != null)
                        pair.Value(p2pMessage);
                    else
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "No AckHandler registered for ack: " + p2pMessage.AckIdentifier, GetType().Name);

                    ackHandlers.Remove(p2pMessage.AckIdentifier);
                }
                else
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "No AckHandler pair for ack: " + p2pMessage.AckIdentifier, GetType().Name);

                return;
            }


            P2PSession session = FindSession(p2pMessage, slp);

            if (session != null)
            {
                session.HandleMessage(sender, p2pMessage);
                return;
            }

            if (slp != null)
            {
                if (ProcessSLPMessage(sender, slp.FromMail, p2pMessage, slp))
                    return;
            }


            if ((p2pMessage.Flags & P2PFlag.Waiting) == P2PFlag.Waiting)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Received P2P waiting message", GetType().Name);
                return;
            }

            if (slp != null)
                return;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unhandled P2P message!\r\n" + p2pMessage.ToString(), GetType().Name);
        }

        public bool ProcessSLPMessage(IMessageProcessor bridge, string source, P2PMessage msg, SLPMessage slp)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "SLPMessage incoming:\r\n" + slp.ToDebugString(), GetType().Name);

            if (slp is SLPRequestMessage)
            {
                SLPRequestMessage req = slp as SLPRequestMessage;

                if (req.Method == "INVITE")
                {
                    if (req.ContentType == "application/x-msnmsgr-sessionreqbody")
                    {
                        P2PSession session = new P2PSession(req, msg, NSMessageHandler);
                        session.Closed += P2PSessionClosed;

                        lock (sessions)
                            sessions.Add(session);

                        return true;
                    }
                }
            }

            return false;
        }

        private bool HandleSplitMessage(ref P2PMessage msg)
        {
            if ((msg.Flags & P2PFlag.Data) == P2PFlag.Data ||
                (msg.InnerBody.Length == 0) || (msg.Offset == msg.TotalSize))
                return false;


            if (!messageStreams.ContainsKey(msg.Identifier))
                messageStreams.Add(msg.Identifier, new MemoryStream());

            messageStreams[msg.Identifier].Write(msg.InnerBody, 0, msg.InnerBody.Length);

            if ((msg.Offset + msg.MessageSize) >= msg.TotalSize)
            {
                msg.InnerBody = messageStreams[msg.Identifier].ToArray();
                msg.Offset = 0;
                msg.MessageSize = (uint)msg.TotalSize;

                messageStreams[msg.Identifier].Close();
                messageStreams.Remove(msg.Identifier);

                return false;
            }

            return true;
        }


        /// <summary>
        /// Requests a new switchboard processor.
        /// </summary>
        /// <remarks>
        /// This is done by delegating the request to the nameserver handler. The supplied contact is also direct invited to the newly created switchboard session.
        /// </remarks>
        /// <param name="remoteContact"></param>
        protected internal virtual SBMessageHandler RequestSwitchboard(string remoteContact)
        {
            Contact remote = NSMessageHandler.ContactList.GetContact(remoteContact, ClientType.PassportMember);

            SBMessageHandler handler = Factory.CreateSwitchboardHandler();
            NSMessageHandler.RequestSwitchboard(handler, this);
            handler.NSMessageHandler = NSMessageHandler;
            handler.Invite(remote.Mail);

            return handler;
        }

        /// <summary>
        /// Gets a switchboard session with the specified remote contact present in the session. Null is returned if no such session is found.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected internal SBMessageHandler GetSwitchboardSession(string remoteContact)
        {
            foreach (SBMessageHandler handler in switchboardSessions)
            {
                if (handler.Contacts.Count == 1 &&
                    handler.Contacts.ContainsKey(remoteContact) &&
                    handler.IsSessionEstablished)
                    return handler;
            }
            return null;
        }

    }
};
