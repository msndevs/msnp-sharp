#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Used in events where a P2PMessageSession object is created, or in another way affected.
    /// </summary>
    public class P2PSessionAffectedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PMessageSession session;

        /// <summary>
        /// The affected session
        /// </summary>
        public P2PMessageSession Session
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="affectedSession"></param>
        public P2PSessionAffectedEventArgs(P2PMessageSession affectedSession)
        {
            session = affectedSession;
        }
    }

    /// <summary>
    /// Handles incoming P2P messages from the switchboardserver.
    /// </summary>
    public class P2PHandler : IMessageHandler
    {
        #region Members

        private NSMessageHandler nsMessageHandler;
        private P2PMessagePool p2pMessagePool = new P2PMessagePool();
        private List<P2PMessageSession> messageSessions = new List<P2PMessageSession>();
        private List<SBMessageHandler> switchboardSessions = new List<SBMessageHandler>(0);

        #endregion

        /// <summary>
        /// Protected constructor.
        /// </summary>
        protected internal P2PHandler(NSMessageHandler nsHandler)
        {
            NSMessageHandler = nsHandler;
        }

        #region Properties

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
                // de-register from the previous ns handler
                if (nsMessageHandler != null)
                {
                    nsMessageHandler.SBCreated -= (nsMessageHandler_SBCreated);
                    nsMessageHandler.ContactOffline -= (nsMessageHandler_ContactOffline);
                }

                nsMessageHandler = value;

                // register new ns handler
                if (nsMessageHandler != null)
                {
                    nsMessageHandler.SBCreated += (nsMessageHandler_SBCreated);
                    nsMessageHandler.ContactOffline += (nsMessageHandler_ContactOffline);
                }
            }
        }

        /// <summary>
        /// The message processor that will send the created P2P messages to the remote contact.
        /// </summary>
        [Obsolete("This is always null. Don't use!", true)]
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>
        /// A list of all current p2p message sessions. Multiple threads can access this resource so make sure to lock this.
        /// </summary>
        public List<P2PMessageSession> MessageSessions
        {
            get
            {
                return messageSessions;
            }
        }

        /// <summary>
        /// A collection of all available switchboard sessions
        /// </summary>
        internal List<SBMessageHandler> SwitchboardSessions
        {
            get
            {
                return switchboardSessions;
            }
        }

        #endregion

        /// <summary>
        /// Aborts and cleans up all running messagesessions and their transfersessions.
        /// </summary>
        public void Clear()
        {
            lock (messageSessions)
            {
                foreach (P2PMessageSession session in messageSessions)
                {
                    session.AbortAllTransfers();
                }
            }

            lock (messageSessions)
                messageSessions.Clear();

            p2pMessagePool = new P2PMessagePool();
        }

        #region Events

        /// <summary>
        /// Occurs when a P2P session is created.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionCreated;

        /// <summary>
        /// Occurs when a P2P session is closed.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionClosed;

        /// <summary>
        /// Fires the SessionCreated event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionCreated(P2PMessageSession session)
        {
            if (SessionCreated != null)
                SessionCreated(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionClosed(P2PMessageSession session)
        {
            if (SessionClosed != null)
                SessionClosed(this, new P2PSessionAffectedEventArgs(session));
        }

        #endregion

        /// <summary>
        /// Gets a reference to a p2p message session with the specified remote contact.
        /// In case a session does not exist a new session will be created and returned.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        public virtual P2PMessageSession GetSession(Contact localContact, Contact remoteContact)
        {
            // check for existing session
            P2PMessageSession existingSession = GetSessionFromRemote(remoteContact);

            if (existingSession != null)
            {
                if (existingSession.MessageProcessor != null && existingSession.MessageProcessor is SocketMessageProcessor)
                {
                    if (((SocketMessageProcessor)existingSession.MessageProcessor).Connected)
                    {
                        return existingSession;
                    }
                    else
                    {
                        lock (messageSessions)
                        {
                            messageSessions.Remove(existingSession);
                        }
                    }
                }
            }

            // no session available, create a new session
            P2PMessageSession newSession = CreateSessionFromLocal(localContact, remoteContact);
            lock (messageSessions)
                messageSessions.Add(newSession);

            // fire event
            OnSessionCreated(newSession);

            return newSession;
        }

        /// <summary>
        /// Creates a p2p session. The session is at the moment of return pure fictive; no actual messages
        /// have been sent to the remote client. The session will use the P2PHandler's messageprocessor as it's default messageprocessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected virtual P2PMessageSession CreateSessionFromLocal(Contact localContact, Contact remoteContact)
        {
            P2PMessageSession session =
                ((localContact.ClientCapacitiesEx & ClientCapacitiesEx.CanP2PV2) > 0 && (remoteContact.ClientCapacitiesEx & ClientCapacitiesEx.CanP2PV2) > 0)
                ? new P2PMessageSession(P2PVersion.P2PV2) : new P2PMessageSession(P2PVersion.P2PV1);


            // set the parameters
            session.RemoteUser = remoteContact;
            session.LocalUser = localContact;
            session.RemoteContact = (session.Version == P2PVersion.P2PV1) ? remoteContact.Mail : remoteContact.Mail + ";" + remoteContact.MachineGuid.ToString("B");
            session.LocalContact = (session.Version == P2PVersion.P2PV1) ? localContact.Mail : localContact.Mail + ";" + localContact.MachineGuid.ToString("B");

            session.ProcessorInvalid += session_ProcessorInvalid;

            // generate a local base identifier.
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));

            // uses -1 because the first message must be the localbaseidentifier and the identifier
            // is automatically increased
            session.LocalIdentifier = (uint)session.LocalBaseIdentifier;// - (ulong)session.LocalInitialCorrection);//session.LocalBaseIdentifier - 4;

            return session;
        }

        /// <summary>
        /// Gets a switchboard session with the specified remote contact present in the session. Null is returned if no such session is found.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected SBMessageHandler GetSwitchboardSession(Contact remoteContact)
        {
            foreach (SBMessageHandler handler in switchboardSessions)
            {
                if (handler.Contacts.Count == 1 && handler.Contacts.ContainsKey(remoteContact) && handler.IsSessionEstablished)
                    return handler;
            }
            return null;
        }

        /// <summary>
        /// Gets the p2p message session for which the remote identifier equals the identifier passed as a parameter.
        /// This is typically called when an incoming message is processed.
        /// </summary>
        /// <param name="remoteContact">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionFromRemote(Contact remoteContact)
        {
            lock (messageSessions)
            {
                foreach (P2PMessageSession session in messageSessions)
                {
                    if (session.RemoteUser == remoteContact)
                        return session;
                }
            }
            return null;
        }


        /// <summary>
        /// Creates a session based on a message received from the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        protected P2PMessageSession CreateSessionFromRemote(P2PMessage receivedMessage)
        {
            P2PMessageSession session = new P2PMessageSession(receivedMessage.Version);

            // generate a local base identifier.
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));
            session.LocalIdentifier = (uint)(session.LocalBaseIdentifier);

            session.ProcessorInvalid += new EventHandler<EventArgs>(session_ProcessorInvalid);

            // setup the remote identifier
            session.RemoteBaseIdentifier = receivedMessage.Header.Identifier;
            session.RemoteIdentifier = receivedMessage.Header.Identifier;

            if (receivedMessage.Version == P2PVersion.P2PV2)
            {
                session.RemoteIdentifier += receivedMessage.V2Header.MessageSize;
            }

            return session;
        }

        /// <summary>
        /// After the first acknowledgement we must set the identifier of the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        protected P2PMessageSession SetSessionIdentifiersAfterAck(P2PMessage receivedMessage)
        {
            P2PMessageSession session = (receivedMessage.Version == P2PVersion.P2PV1)
                ? GetSessionFromLocal(receivedMessage.V1Header.AckSessionId)
                : null; // We only do things step by step.

            if (session == null)
                throw new MSNPSharpException("P2PHandler: an acknowledgement for the creation of a P2P session was received, but no local created session could be found with the specified identifier.");

            // set the remote identifiers. 
            session.RemoteBaseIdentifier = receivedMessage.Header.Identifier;
            session.RemoteIdentifier = (uint)(receivedMessage.Header.Identifier);// - (ulong)session.RemoteInitialCorrection);

            return session;
        }

        /// <summary>
        /// Gets the p2p message session for which the local identifier equals the identifier passed as a parameter.
        /// This is typically called when a message is created.
        /// </summary>
        /// <param name="identifier">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionFromLocal(uint identifier)
        {
            lock (messageSessions)
            {
                foreach (P2PMessageSession session in messageSessions)
                {
                    if (session.LocalIdentifier == identifier)
                        return session;
                }
            }
            return null;
        }



        #region HandleMessage

        /// <summary>
        /// Handles incoming sb messages. Other messages are ignored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            SBMessage sbMessage = message as SBMessage;

            if (sbMessage == null)
                return;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming msg message", GetType().Name);

            // Clean the MessageSessions, or it will lead to memory leak.
            if (sbMessage.Command == "BYE")
            {
                string account = sbMessage.CommandValues[0].ToString();
                lock (messageSessions)
                {
                    List<P2PMessageSession> list = new List<P2PMessageSession>(messageSessions);
                    foreach (P2PMessageSession p2psession in list)
                    {
                        if (p2psession.RemoteContact.ToLowerInvariant() == account.ToLowerInvariant())
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

            // create a MSGMessage from the sb message
            MSGMessage msgMessage = new MSGMessage();
            try
            {
                msgMessage.CreateFromMessage(sbMessage);
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString(), GetType().Name);
            }

            // check if it's a valid p2p message (chunk messages has no content type)
            if (!msgMessage.MimeHeader.ContainsKey(MimeHeaderStrings.Content_Type) ||
                msgMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToString() != "application/x-msnmsgrp2p")
            {
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming p2p message", GetType().Name);

            P2PVersion version = P2PVersion.P2PV1;
            string remoteAccount = (sbMessage.CommandValues.Count > 0) ? sbMessage.CommandValues[0].ToString() : String.Empty;
            string localAccount = NSMessageHandler.ContactList.Owner.Mail;
            Guid remoteMachineGuid = Guid.Empty;
            Guid localMachineGuid = Guid.Empty;

            if (msgMessage.MimeHeader.ContainsKey("P2P-Dest"))
            {
                if (msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                {
                    version = P2PVersion.P2PV2;
                    remoteAccount = msgMessage.MimeHeader["P2P-Src"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    remoteMachineGuid = new Guid(msgMessage.MimeHeader["P2P-Src"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                    localAccount = msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    localMachineGuid = new Guid(msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);

                    Trace.WriteLine("P2Pv2 incoming message found. P2P-Dest: " + msgMessage.MimeHeader["P2P-Dest"].ToString());
                }
                else
                {
                    localAccount = msgMessage.MimeHeader["P2P-Dest"].ToString();
                    remoteAccount = msgMessage.MimeHeader.ContainsKey("P2P-Src")
                        ? msgMessage.MimeHeader["P2P-Src"].ToString()
                        : sbMessage.CommandValues[0].ToString(); // CommandValues.Count=0, Clone issue????

                    Trace.WriteLine("P2Pv1 incoming message found. P2P-Dest: " + msgMessage.MimeHeader["P2P-Dest"].ToString());
                }
            }

            // Check destination
            if (version == P2PVersion.P2PV2 &&
                localMachineGuid != Guid.Empty &&
                localMachineGuid != NSMessageHandler.MachineGuid)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "The destination of p2pv2 message received is not owner.\r\n" +
                    "Destination GUID: " + localMachineGuid.ToString("B") + "\r\n" +
                    "Owner GUID: " + NSMessageHandler.MachineGuid.ToString("B"));
                return; // This message is not for me
            }

            // Create a P2P Message from the msg message
            P2PMessage p2pMessage = new P2PMessage(version);
            p2pMessage.CreateFromMessage(msgMessage);

            if (Settings.TraceSwitch.TraceVerbose)
            {
                ulong dataRemaining = 0;

                if (version == P2PVersion.P2PV1)
                {
                    dataRemaining = p2pMessage.V1Header.TotalSize - (p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize);
                }
                else if (version == P2PVersion.P2PV2)
                {
                    dataRemaining = p2pMessage.V2Header.DataRemaining;
                }

                Trace.WriteLine("Incoming p2p message: DataRemaining: " + dataRemaining + "\r\n" +
                    p2pMessage.ToDebugString(), GetType().Name);
            }

            // Buffer splitted P2P SLP messages.
            if (p2pMessagePool.BufferMessage(ref p2pMessage))
            {
                // - Buffering: Not completed yet, wait next packets...
                // - Invalid packet: Just ignore it...
                return;
            }

            SLPMessage slp = p2pMessage.IsSLPData ? p2pMessage.InnerMessage as SLPMessage : null;
            if (slp != null)
            {
                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    remoteAccount = slp.FromMail;
                    localAccount = slp.ToMail;
                }
                else
                {
                    remoteAccount = slp.FromMail.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    remoteMachineGuid = new Guid(slp.FromMail.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                    localAccount = slp.ToMail.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    localMachineGuid = new Guid(slp.ToMail.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                }
            }

            if (false == NSMessageHandler.ContactList.HasContact(remoteAccount, ClientType.PassportMember))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "P2P remote contact not in contact list: " + remoteAccount + " Type: " + ClientType.PassportMember.ToString());
                return;
            }

            // Find P2P SESSION
            Contact remoteContact = NSMessageHandler.ContactList.GetContact(remoteAccount, ClientType.PassportMember);
            P2PMessageSession session = GetSessionFromRemote(remoteContact);

            if (session == null)
            {
                if (version == P2PVersion.P2PV1)
                {
                    if (p2pMessage.V1Header.IsAcknowledgement)
                    {
                        // if it is an acknowledgement then the local client initiated the session.
                        // this means the session alread exists, but the remote identifier are not yet set.
                        session = SetSessionIdentifiersAfterAck(p2pMessage);
                    }
                    else
                    {
                        // there is no session available at all. the remote client sends the first message
                        // in the session. So create a new session to handle following messages.
                        session = CreateSessionFromRemote(p2pMessage);
                    }
                }

                if (version == P2PVersion.P2PV2)
                {
                    // there is no session available at all. the remote client sends the first message
                    // in the session. So create a new session to handle following messages.

                    if (slp != null)
                    {
                        SLPRequestMessage req = slp as SLPRequestMessage;

                        if (req != null &&
                            req.Method == "INVITE" &&
                            req.ContentType == "application/x-msnmsgr-sessionreqbody")
                        {
                            session = CreateSessionFromRemote(p2pMessage);
                        }
                    }
                }

                if (session == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "P2PHandler get session failed.");
                    return;
                }

                session.LocalUser = NSMessageHandler.ContactList.Owner;
                session.RemoteUser = remoteContact;
                session.RemoteContact = remoteMachineGuid == Guid.Empty ? remoteAccount : (remoteAccount + ";" + remoteMachineGuid.ToString("B"));
                session.LocalContact = localMachineGuid == Guid.Empty ? localAccount : (localAccount + ";" + localMachineGuid.ToString("B"));

                // add the session to the session list
                lock (messageSessions)
                {
                    messageSessions.Add(session);
                }

                // set the default message processor
                session.MessageProcessor = sender;

                // notify the client programmer
                OnSessionCreated(session);

            }

            Debug.Assert(session != null, "Session is null", "P2PHandler");

            // Send an acknowledgement after the last message
            if (version == P2PVersion.P2PV1)
            {
                if (p2pMessage.Header.IsAcknowledgement == false &&
                    p2pMessage.V1Header.Offset + p2pMessage.Header.MessageSize == p2pMessage.Header.TotalSize)
                {
                    P2PMessage ack = p2pMessage.CreateAcknowledgement();
                    session.SendMessage(ack);
                }
            }
            if (version == P2PVersion.P2PV2)
            {
                //Case 1: 0x18 0x03 Invite
                if (p2pMessage.Header.IsAcknowledgement == false &&
                    p2pMessage.V2Header.OperationCode > 0)
                {
                    P2PMessage ack = p2pMessage.CreateAcknowledgement();
                    session.SendMessage(ack);
                }
            }

            // now handle the message
            session.HandleMessage(sender, p2pMessage);

        }

        #endregion


        /// <summary>
        /// Requests a new switchboard processor.
        /// </summary>
        /// <remarks>
        /// This is done by delegating the request to the nameserver handler. The supplied contact is also direct invited to the newly created switchboard session.
        /// </remarks>
        /// <param name="remoteContact"></param>
        protected virtual SBMessageHandler RequestSwitchboard(Contact remoteContact)
        {
            if (NSMessageHandler == null)
                throw new MSNPSharpException("P2PHandler could not request a new switchboard session because the NSMessageHandler property is null.");

            SBMessageHandler handler = new SBMessageHandler();
            NSMessageHandler.RequestSwitchboard(handler, this);
            handler.NSMessageHandler = NSMessageHandler;

            handler.Invite(remoteContact);
            return handler;
        }

        /// <summary>
        /// Add a switchboard handler to the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void AddSwitchboardSession(SBMessageHandler session)
        {
            lock (SwitchboardSessions)
            {
                if (SwitchboardSessions.Contains(session) == false)
                    SwitchboardSessions.Add(session);
            }
        }

        /// <summary>
        /// Removes a switchboard handler from the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void RemoveSwitchboardSession(SBMessageHandler session)
        {
            int remainCount = 0;
            lock (SwitchboardSessions)
            {
                SwitchboardSessions.Remove(session);
                remainCount = SwitchboardSessions.Count;
            }
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A " + session.GetType().ToString() + " has been removed.");
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "There is/are " + remainCount.ToString() + " switchboard(s) remain(s) unclosed.");
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
            e.Switchboard.ContactJoined += new EventHandler<ContactEventArgs>(Switchboard_ContactJoined);
            e.Switchboard.ContactLeft += new EventHandler<ContactEventArgs>(Switchboard_ContactLeft);
            e.Switchboard.SessionClosed += new EventHandler<EventArgs>(Switchboard_SessionClosed);
            // keep track of this switchboard
            AddSwitchboardSession(sbHandler);
        }

        /// <summary>
        /// Removes the messageprocessor from the specified messagesession, because it is invalid.
        /// </summary>
        protected virtual void RemoveSessionProcessor(P2PMessageSession session)
        {
            session.MessageProcessor = null;

            return;
        }

        /// <summary>
        /// Sets the specified messageprocessor as the default messageprocessor for the message session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="processor"></param>
        protected virtual void AddSessionProcessor(P2PMessageSession session, IMessageProcessor processor)
        {
            System.Threading.Thread.CurrentThread.Join(1000);
            session.MessageProcessor = processor;

            return;
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
            //MSNP18: owner in the switchboard, so there're 2 contacts.
            bool canremove = (handler.Contacts.Count > 2);

            if (canremove)
            {
                lock (messageSessions)
                {
                    // in a conversation with multiple contacts we don't want to send p2p messages.
                    foreach (P2PMessageSession session in messageSessions)
                    {
                        if (session.MessageProcessor == handler.MessageProcessor)
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
            bool canadd = (handler.Contacts.Count == 2);

            if (canadd)
            {
                P2PMessageSession session = GetSessionFromRemote(e.Contact);

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
            P2PMessageSession session = GetSessionFromRemote(e.Contact);
            if (session != null && session.MessageProcessor == handler.MessageProcessor)
            {
                // invalidate the processor
                RemoveSessionProcessor(session);
            }
        }

        /// <summary>
        /// Cleans up p2p resources associated with the offline contact.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsMessageHandler_ContactOffline(object sender, ContactEventArgs e)
        {
            // destroy all message sessions that dealt with this contact
            P2PMessageSession session = GetSessionFromRemote(e.Contact);
            if (session != null)
            {
                CloseMessageSession(session);
            }
        }

        /// <summary>
        /// Closes a message session.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void CloseMessageSession(P2PMessageSession session)
        {
            // make sure there are no sessions and references left
            session.AbortAllTransfers();
            session.CleanUp();

            // call the event
            OnSessionClosed(session);

            // and remove the session from the list
            lock (messageSessions)
            {
                messageSessions.Remove(session);
            }
        }

        /// <summary>
        /// Requests a new switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void session_ProcessorInvalid(object sender, EventArgs e)
        {
            P2PMessageSession session = (P2PMessageSession)sender;

            // create a new switchboard to fill the hole
            SBMessageHandler sbHandler = GetSwitchboardSession(session.RemoteUser);

            // if the contact is offline, there is no need to request a new switchboard. close the session.
            if (session.RemoteUser.Status == PresenceStatus.Offline)
            {
                CloseMessageSession(session);
                return;
            }

            // check whether the switchboard handler is valid and has a valid processor.
            // if that is the case, use that processor. Otherwise request a new session.
            if (sbHandler == null || sbHandler.MessageProcessor == null ||
                ((SocketMessageProcessor)sbHandler.MessageProcessor).Connected == false ||
                (sbHandler.Contacts.ContainsKey(session.RemoteUser) == false))
            {
                RequestSwitchboard(session.RemoteUser);
            }
            else
            {
                session.MessageProcessor = sbHandler.MessageProcessor;
            }
        }

        /// <summary>
        /// Removes the session from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            RemoveSwitchboardSession((SBMessageHandler)sender);
        }
    }
};
