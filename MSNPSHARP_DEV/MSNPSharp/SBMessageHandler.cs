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
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    #region Event argument classes
    /// <summary>
    /// Used when a new switchboard session is affected.
    /// </summary>
    public class SBEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private SBMessageHandler switchboard;

        /// <summary>
        /// The affected switchboard
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                return switchboard;
            }
            set
            {
                switchboard = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBEventArgs(SBMessageHandler switchboard)
        {
            this.switchboard = switchboard;
        }
    }

    /// <summary>
    /// Used when a new switchboard session is created.
    /// </summary>
    public class SBCreatedEventArgs : EventArgs
    {
        private object initiator;
        private SBMessageHandler switchboard;
        private string account = string.Empty;
        private string name = string.Empty;
        private bool anonymous = false;

        /// <summary>
        /// The affected switchboard
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                return switchboard;
            }
            set
            {
                switchboard = value;
            }
        }


        /// <summary>
        /// The object that requested the switchboard. Null if the switchboard session was initiated by a remote client.
        /// </summary>
        public object Initiator
        {
            get
            {
                return initiator;
            }
        }

        /// <summary>
        /// The account of user that requested the switchboard.
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
        }

        /// <summary>
        ///  The nick name of user that requested the switchboard.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Anonymous request, usually from webchat users.
        /// </summary>
        public bool Anonymous
        {
            get
            {
                return anonymous;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBCreatedEventArgs(SBMessageHandler switchboard, object initiator)
        {
            this.switchboard = switchboard;
            this.initiator = initiator;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBCreatedEventArgs(SBMessageHandler switchboard, object initiator, string account, string name, bool anonymous)
        {
            this.switchboard = switchboard;
            this.initiator = initiator;
            this.account = account;
            this.name = name;
            this.anonymous = anonymous;
        }
    }
    #endregion

    /// <summary>
    /// Handles the messages from the switchboard server.
    /// </summary>
    public class SBMessageHandler : IMessageHandler
    {
        private SBMessageHandler()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected internal SBMessageHandler(NSMessageHandler hander)
        {
            nsMessageHandler = hander;
            SetNewProcessor();
            ResigerHandlersToProcessor(MessageProcessor);
            NSMessageHandler.P2PHandler.AddSwitchboardSession(this);
            NSMessageHandler.ContactOffline += new EventHandler<ContactEventArgs>(ContactOfflineHandler);
        }

        #region Public Events

        /// <summary>
        /// Fired when the owner is the only contact left. If the owner leaves too the connection is automatically closed by the server.
        /// </summary>
        public event EventHandler<EventArgs> AllContactsLeft;

        /// <summary>
        /// Fired when the session is closed, either by the server or by the local client.
        /// </summary>
        public event EventHandler<EventArgs> SessionClosed;

        /// <summary>
        /// Fired when a switchboard connection has been made and the initial handshaking commands are send. This indicates that the session is ready to invite or accept other contacts.
        /// </summary>
        public event EventHandler<EventArgs> SessionEstablished;

        /// <summary>
        /// Fired when a contact joins. In case of a conversation with two people in it this event is called with the remote contact specified in the event argument.
        /// </summary>
        public event EventHandler<ContactConversationEventArgs> ContactJoined;

        /// <summary>
        /// Fired when a contact leaves the conversation.
        /// </summary>
        public event EventHandler<ContactConversationEventArgs> ContactLeft;

        /// <summary>
        /// Fired when a message is received from any of the other contacts in the conversation.
        /// </summary>
        public event EventHandler<TextMessageEventArgs> TextMessageReceived;

        /// <summary>
        /// Fired when a contact sends a emoticon definition.
        /// </summary>
        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;

        /// <summary>
        /// Fired when a contact sends a wink.
        /// </summary>
        public event EventHandler<WinkEventArgs> WinkReceived;

        /// <summary>
        /// Fired when a contact sends a nudge.
        /// </summary>
        public event EventHandler<ContactEventArgs> NudgeReceived;

        /// <summary>
        /// Fired when any of the other contacts is typing a message.
        /// </summary>
        public event EventHandler<ContactEventArgs> UserTyping;

        /// <summary>
        /// Fired when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Fired when the MSN Switchboard Server sends us an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;

        #endregion

        #region Members

        private Dictionary<string, string> rosterName = new Dictionary<string, string>(0);
        private Dictionary<string, string> rosterCapacities = new Dictionary<string, string>(0);
        private Dictionary<string, ContactConversationState> rosterState = new Dictionary<string, ContactConversationState>(0);

        private Dictionary<string, MSGMessage> multiPacketMessages = new Dictionary<string, MSGMessage>();
        private object syncObject = new object();
        private Queue<Contact> invitationQueue = new Queue<Contact>();
        private string sessionHash = String.Empty;
        protected SocketMessageProcessor messageProcessor;
        private NSMessageHandler nsMessageHandler;
        private string sessionId;

        protected bool sessionEstablished;
        protected bool invited;

        #endregion

        #region Properties

        /// <summary>
        /// The nameserver that received the request for the switchboard session
        /// </summary>
        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// Indicates if the local client was invited to the session
        /// </summary>
        public bool Invited
        {
            get
            {
                return invited;
            }
        }

        /// <summary>
        /// Indicates if the session is ready to send/accept commands. E.g. the initial handshaking and identification has been completed.
        /// </summary>
        public bool IsSessionEstablished
        {
            get
            {
                return sessionEstablished;
            }
        }

        protected string SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                sessionId = value;
            }
        }


        /// <summary>
        /// The hash identifier used to define this switchboard session.
        /// </summary>
        internal string SessionHash
        {
            get
            {
                return sessionHash;
            }
            set
            {
                sessionHash = value;
            }
        }

        /// <summary>
        /// Implements the P2P framework. This object is automatically created when a succesfull connection was made to the switchboard.
        /// </summary>
        [Obsolete("Please use Messenger.P2PHandler.", true)]
        public P2PHandler P2PHandler
        {
            get
            {
                throw new MSNPSharpException("Please use Messenger.P2PHandler.");
            }
            set
            {
                throw new MSNPSharpException("Please use Messenger.P2PHandler.");
            }
        }

        #endregion

        #region Invitation

        /// <summary>
        /// Invites the specified contact to the switchboard.
        /// </summary>
        /// <remarks>
        /// If there is not yet a connection established the invitation will be stored in a queue and processed as soon as a connection is established.
        /// </remarks>
        /// <param name="contact">The contact's account to invite.</param>
        public virtual bool Invite(Contact contact)
        {
            return Invite(contact, Guid.Empty);
        }

        /// <summary>
        /// Called when a switchboard session is created on request of a remote client.
        /// </summary>
        /// <param name="sessionHash"></param>
        /// <param name="sessionId"></param>
        public void SetInvitation(string sessionHash, string sessionId)
        {
            SessionId = sessionId;
            SessionHash = sessionHash;
            this.invited = true;

            NSMessageHandler.SetSession(SessionId, SessionHash);
        }

        /// <summary>
        /// Called when a switchboard session is created on request of a local client.
        /// </summary>
        /// <param name="sessionHash"></param>
        public void SetInvitation(string sessionHash)
        {
            SessionHash = sessionHash;
            SessionId = SessionHash.Split('.')[0];

            NSMessageHandler.SetSession(SessionId, SessionHash);

            this.invited = false;
        }

        /// <summary>
        /// Left the conversation then closes the switchboard session by disconnecting from the server. 
        /// </summary>
        public virtual void Close(bool causeByRemote)
        {
            if (MessageProcessor != null)
            {
                if (!causeByRemote)
                {
                    SendSwitchBoardClosedNotifyToNS();
                }

                SocketMessageProcessor processor = MessageProcessor as SocketMessageProcessor;

                try
                {
                    processor.UnregisterHandler(this);
                    processor.SendMessage(new SBMessage("OUT", new string[] { }));
                    processor.Disconnect();
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().ToString());
                }
            }
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Sends a plain text message to all other contacts in the conversation.
        /// </summary>
        /// <remarks>
        /// This method wraps the TextMessage object in a SBMessage object and sends it over the network.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        public virtual void SendTextMessage(TextMessage message)
        {
            //First, we have to check whether the content of the message is not to big for one message
            //There's a maximum of 1600 bytes per message, let's play safe and take 1400 bytes
            UTF8Encoding encoding = new UTF8Encoding();

            if (encoding.GetByteCount(message.Text) > 1400)
            {
                //we'll have to use multi-packets messages
                Guid guid = Guid.NewGuid();
                byte[] text = encoding.GetBytes(message.Text);
                int chunks = Convert.ToInt32(Math.Ceiling((double)text.GetUpperBound(0) / (double)1400));
                for (int i = 0; i < chunks; i++)
                {
                    SBMessage sbMessage = new SBMessage();
                    MSGMessage msgMessage = new MSGMessage();

                    //Clone the message
                    TextMessage chunkMessage = (TextMessage)message.Clone();

                    //Get the part of the message that we are going to send
                    if (text.GetUpperBound(0) - (i * 1400) > 1400)
                        chunkMessage.Text = encoding.GetString(text, i * 1400, 1400);
                    else
                        chunkMessage.Text = encoding.GetString(text, i * 1400, text.GetUpperBound(0) - (i * 1400));

                    //Add the correct headers
                    msgMessage.MimeHeader.Add("Message-ID", "{" + guid.ToString() + "}");

                    if (i == 0)
                        msgMessage.MimeHeader.Add("Chunks", Convert.ToString(chunks));
                    else
                        msgMessage.MimeHeader.Add("Chunk", Convert.ToString(i));

                    sbMessage.InnerMessage = msgMessage;

                    msgMessage.InnerMessage = chunkMessage;

                    //send it over the network
                    MessageProcessor.SendMessage(sbMessage);
                }
            }
            else
            {
                SBMessage sbMessage = new SBMessage();
                MSGMessage msgMessage = new MSGMessage();

                sbMessage.InnerMessage = msgMessage;

                msgMessage.InnerMessage = message;

                // send it over the network
                MessageProcessor.SendMessage(sbMessage);
            }
        }

        /// <summary>
        /// Sends the definition for a list of emoticons to all other contacts in the conversation. The client-programmer must use this function if a text messages uses multiple emoticons in a single message.
        /// </summary>
        /// <remarks>Use this function before sending text messages which include the emoticon text. You can only send one emoticon message before the textmessage. So make sure that all emoticons used in the textmessage are included.</remarks>
        /// <param name="emoticons">A list of emoticon objects.</param>
        /// <param name="icontype">The type of current emoticons.</param>
        public virtual void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            if (emoticons == null)
                throw new ArgumentNullException("emoticons");

            SBMessage sbMessage = new SBMessage();
            MSGMessage msgMessage = new MSGMessage();

            foreach (Emoticon emoticon in emoticons)
            {
                if (!NSMessageHandler.ContactList.Owner.Emoticons.ContainsKey(emoticon.Sha))
                {
                    // Add the emotions to owner's emoticon collection.
                    NSMessageHandler.ContactList.Owner.Emoticons.Add(emoticon.Sha, emoticon);
                }
            }

            EmoticonMessage emoticonMessage = new EmoticonMessage(emoticons, icontype);

            msgMessage.InnerMessage = emoticonMessage;
            sbMessage.InnerMessage = msgMessage;

            // send it over the network
            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Sends a 'user is typing..' message to the switchboard, and is received by all participants.
        /// </summary>
        public virtual void SendTypingMessage()
        {
            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "U";

            MSGMessage msgMessage = new MSGMessage();
            msgMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msmsgscontrol";
            msgMessage.MimeHeader[MimeHeaderStrings.TypingUser] = NSMessageHandler.ContactList.Owner.Mail + "\r\n";


            sbMessage.InnerMessage = msgMessage;

            // send it over the network
            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Sends a 'nudge' message to the switchboard, and is received by all participants.
        /// </summary>
        public virtual void SendNudge()
        {
            SBMessage sbMessage = new SBMessage();

            MSGMessage msgMessage = new MSGMessage();
            msgMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n\r\n\r\n";
            sbMessage.InnerMessage = msgMessage;

            // send it over the network
            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Send a keep-alive message to avoid the switchboard closing. This is useful for bots.
        /// </summary>
        public virtual void SendKeepAliveMessage()
        {
            SBMessage sbMessage = new SBMessage();

            MSGMessage msgMessage = new MSGMessage();
            msgMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-keepalive\r\n";

            sbMessage.InnerMessage = msgMessage;

            // send it over the network
            MessageProcessor.SendMessage(sbMessage);
        }

        #endregion

        #region Protected User Methods

        /// <summary>
        /// Fires the <see cref="AllContactsLeft"/> event.
        /// </summary>		
        protected virtual void OnAllContactsLeft()
        {
            if (AllContactsLeft != null)
            {
                AllContactsLeft(this, new EventArgs());
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, ToString() + " all contacts left, disconnect automately.", GetType().ToString());
            Close(false);
        }

        /// <summary>
        /// Fires the <see cref="ContactJoined"/> event.
        /// </summary>
        /// <param name="contact">The contact who joined the session.</param>
        /// <param name="endPoint">The machine guid where this contact joined from.</param>
        protected virtual void OnContactJoined(Contact contact, Guid endPoint)
        {
            if (ContactJoined != null)
            {
                ContactJoined(this, new ContactConversationEventArgs(contact, endPoint));
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactLeft"/> event.
        /// </summary>
        /// <param name="contact">The contact who left the session.</param>
        /// <param name="endPoint">The machine guid where this contact left from.</param>
        protected virtual void OnContactLeft(Contact contact, Guid endPoint)
        {
            if (ContactLeft != null)
            {
                ContactLeft(this, new ContactConversationEventArgs(contact, endPoint));
            }
        }

        /// <summary>
        /// Fires the <see cref="UserTyping"/> event.
        /// </summary>
        /// <param name="message">The emoticon message.</param>
        /// <param name="contact">The contact who is sending the definition.</param>
        protected virtual void OnEmoticonDefinition(MSGMessage message, Contact contact)
        {
            EmoticonMessage emoticonMessage = new EmoticonMessage();
            emoticonMessage.CreateFromMessage(message);

            if (EmoticonDefinitionReceived != null)
            {
                foreach (Emoticon emoticon in emoticonMessage.Emoticons)
                {
                    EmoticonDefinitionReceived(this, new EmoticonDefinitionEventArgs(contact, emoticon));
                }
            }
        }

        /// <summary>
        /// Fires the <see cref="NudgeReceived"/> event.
        /// </summary>
        /// <param name="contact">The contact who is sending the nudge.</param>
        protected virtual void OnNudgeReceived(Contact contact)
        {
            if (NudgeReceived != null)
            {
                NudgeReceived(this, new ContactEventArgs(contact));
            }
        }

        /// <summary>
        /// Fires the <see cref="UserTyping"/> event.
        /// </summary>
        /// <param name="contact">The contact who is typing.</param>
        protected virtual void OnUserTyping(Contact contact)
        {
            // make sure we don't parse the rest of the message in the next loop											
            if (UserTyping != null)
            {
                // raise the event
                UserTyping(this, new ContactEventArgs(contact));
            }
        }

        protected virtual void OnWinkReceived(MSGMessage message, Contact contact)
        {
            string body = System.Text.Encoding.UTF8.GetString(message.InnerBody);

            Wink obj = new Wink();
            obj.ParseContext(body, false);

            if (WinkReceived != null)
                WinkReceived(this, new WinkEventArgs(contact, obj));
        }

        /// <summary>
        /// Fires the <see cref="TextMessageReceived"/> event.
        /// </summary>
        /// <param name="message">The message send.</param>
        /// <param name="contact">The contact who sended the message.</param>
        protected virtual void OnTextMessageReceived(TextMessage message, Contact contact)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, new TextMessageEventArgs(message, contact));
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        protected virtual void OnSessionClosed()
        {
            ClearAll();  //Session closed, force all contact left this conversation.

            if (SessionClosed != null)
                SessionClosed(this, new EventArgs());
        }

        /// <summary>
        /// Fires the SessionEstablished event and processes invitations in the queue.
        /// </summary>
        protected virtual void OnSessionEstablished()
        {
            sessionEstablished = true;
            if (SessionEstablished != null)
                SessionEstablished(this, new EventArgs());

            // process ant remaining invitations
            ProcessInvitations();
        }

        /// <summary>
        /// Handles all remaining invitations. If no connection is yet established it will do nothing.
        /// </summary>
        protected virtual void ProcessInvitations()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Processing invitations for switchboard...", GetType().Name);

            if (IsSessionEstablished)
            {
                while (invitationQueue.Count > 0)
                    SendInvitationCommand(invitationQueue.Dequeue());

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Invitation send", GetType().Name);
            }
        }

        /// <summary>
        /// Sends the invitation command to the switchboard server.
        /// </summary>
        /// <param name="contact"></param>
        protected virtual void SendInvitationCommand(Contact contact)
        {
            MessageProcessor.SendMessage(new SBMessage("CAL", new string[] { contact.Mail }));
        }

        /// <summary>
        /// Send the first message to the server.
        /// </summary>
        /// <remarks>
        /// Depending on the <see cref="Invited"/> field a ANS command (if true), or a USR command (if false) is send.
        /// </remarks>
        protected virtual void SendInitialMessage()
        {
            string auth = NSMessageHandler.ContactList.Owner.Mail + ";" + NSMessageHandler.MachineGuid.ToString("B");

            if (Invited)
                MessageProcessor.SendMessage(new SBMessage("ANS", new string[] { auth, SessionHash, SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
            else
                MessageProcessor.SendMessage(new SBMessage("USR", new string[] { auth, SessionHash }));
        }

        protected virtual void SendSwitchBoardClosedNotifyToNS()
        {
            NSMessageHandler.SenSwitchBoardClosedNotify(SessionId.ToString());
        }

        protected virtual void SetNewProcessor()
        {
            messageProcessor = new SBMessageProcessor();

            // catch the connect event to start sending the USR command upon initiating
            messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
            messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
        }

        protected virtual void ResigerHandlersToProcessor(IMessageProcessor processor)
        {
            if (processor != null)
            {
                processor.RegisterHandler(this);
                processor.RegisterHandler(NSMessageHandler.P2PHandler);
            }
        }

        protected virtual void SetRosterProperty(string key, string value, RosterProperties property)
        {
            switch (property)
            {
                case RosterProperties.Name:
                    lock (rosterName)
                        rosterName[key.ToLowerInvariant()] = value;
                    break;
                case RosterProperties.ClientCapacityString:
                    lock (rosterCapacities)
                        rosterCapacities[key.ToLowerInvariant()] = value;
                    break;
                case RosterProperties.Status:
                    lock (rosterState)
                        rosterState[key.ToLowerInvariant()] = (ContactConversationState)Enum.Parse(typeof(ContactConversationState), value);
                    break;
            }
        }

        protected virtual bool IsAllContactsInRosterLeft()
        {
            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (rosterState[key] != ContactConversationState.Left)
                        return false;
                }

                return true;
            }
        }

        protected virtual bool Invite(Contact contact, Guid endPoint)
        {
            string fullaccount = contact.Mail.ToLowerInvariant();
            if (endPoint != Guid.Empty)
            {
                fullaccount += ";" + endPoint.ToString("B").ToLowerInvariant();
            }

            if (GetRosterProperty(fullaccount, RosterProperties.Status) != null)
            {
                //Invite repeatly.
                return false;
            }

            //Add "this contact"
            SetRosterProperty(contact.Mail.ToLowerInvariant() + ";" + contact.MachineGuid.ToString("B").ToLowerInvariant(),
                ContactConversationState.Invited.ToString(),
                RosterProperties.Status);

            if (endPoint == Guid.Empty)
            {
                //Add "all contact"
                SetRosterProperty(contact.Mail.ToLowerInvariant(), ContactConversationState.Invited.ToString(), RosterProperties.Status);
                foreach (Guid place in contact.Places.Keys)
                {
                    string currentAccount = contact.Mail.ToLowerInvariant() + ";" + place.ToString("B").ToLowerInvariant();
                    SetRosterProperty(currentAccount, ContactConversationState.Invited.ToString(), RosterProperties.Status);
                }
            }

            invitationQueue.Enqueue(contact);
            ProcessInvitations();
            return true;
        }

        protected virtual object GetRosterProperty(string key, RosterProperties property)
        {
            string value = string.Empty;
            string lowerKey = key.ToLowerInvariant();

            switch (property)
            {
                case RosterProperties.Name:
                    lock (rosterName)
                    {
                        if (!rosterName.ContainsKey(lowerKey))
                            return null;
                        return rosterName[lowerKey];
                    }

                case RosterProperties.ClientCapacities:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;

                        value = rosterCapacities[lowerKey];
                        int cap = 0;
                        if (value.Contains(":"))
                        {
                            int.TryParse(value.Split(':')[0], out cap);
                            return (ClientCapacities)cap;
                        }

                        return ClientCapacities.None;
                    }

                case RosterProperties.ClientCapacitiesEx:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;

                        value = rosterCapacities[lowerKey];
                        int cap = 0;
                        if (value.Contains(":"))
                        {
                            int.TryParse(value.Split(':')[1], out cap);
                            return (ClientCapacitiesEx)cap;
                        }

                        return ClientCapacitiesEx.None;
                    }
                case RosterProperties.ClientCapacityString:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;
                        return rosterCapacities[lowerKey];
                    }
                case RosterProperties.Status:
                    lock (rosterState)
                    {
                        if (!rosterState.ContainsKey(lowerKey))
                            return null;
                        return rosterState[lowerKey];
                    }
            }

            return null;
        }

        protected virtual void ContactOfflineHandler(object sender, ContactEventArgs e)
        {
            Contact contact = e.Contact;

            if (HasContact(contact) && GetRosterUniqueUserCount() == 1)
            {
                Close(true);
            }
        }

        internal virtual int GetRosterUserCount()
        {
            int count = 0;
            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (NSMessageHandler.ContactList.Owner != null)
                    {
                        if (key.Split(';')[0] != NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        internal virtual int GetRosterUniqueUserCount()
        {
            Dictionary<string, string> uniqueUsers = new Dictionary<string, string>(0);

            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (NSMessageHandler.ContactList.Owner != null)
                    {
                        uniqueUsers[key.Split(';')[0]] = string.Empty;
                    }
                }

                return uniqueUsers.Count;
            }
        }

        internal bool HasContact(Contact contact)
        {
            lock (rosterState)
            {
                if (HasContact(contact.Mail))
                    return true;

                if (HasContact(contact.Mail, contact.Guid))
                    return true;

                lock (contact.SyncObject)
                {
                    foreach (Guid place in contact.Places.Keys)
                    {
                        if (HasContact(contact.Mail, place))
                            return true;
                    }
                }

                return false;
            }
        }

        internal bool HasContact(string account)
        {
            lock (rosterState)
                return rosterState.ContainsKey(account.ToLowerInvariant());
        }

        internal bool HasContact(string account, Guid place)
        {
            string fullaccount = account.ToLowerInvariant() + ";" + place.ToString("B").ToLowerInvariant();
            lock (rosterState)
                return rosterState.ContainsKey(fullaccount);
        }

        #endregion

        #region Protected CMDs

        /// <summary>
        /// Called when a ANS command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our identification ANS command.
        /// <code>ANS [Transaction] ['OK']</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnANSReceived(SBMessage message)
        {
            if (message.CommandValues[1].ToString() == "OK")
            {
                // we are now ready to invite other contacts. Notify the client of this.
                OnSessionEstablished();
            }
        }

        /// <summary>
        /// Called when a BYE command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has leaved the session.
        /// This will fire the <see cref="ContactLeft"/> event. Or, if all contacts have left, the <see cref="AllContactsLeft"/> event.
        /// <code>BYE [account[;GUID]] [Client Type]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnBYEReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[0].ToString().ToLowerInvariant();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            if (oldStatus == ContactConversationState.Left || oldStatus == ContactConversationState.None)
                return;

            SetRosterProperty(fullaccount, ContactConversationState.Left.ToString(), RosterProperties.Status);

            Guid endPointId = Guid.Empty;
            string account = fullaccount;

            if (fullaccount.Contains(";"))
            {
                string[] accountGuid = fullaccount.Split(';');
                account = accountGuid[0];
                endPointId = new Guid(accountGuid[1]);
            }

            if (account == NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
            {
                if (IsAllContactsInRosterLeft())
                {
                    OnAllContactsLeft();
                }
                return;
            }

            Contact contact = (message.CommandValues.Count >= 2) ?
                NSMessageHandler.ContactList.GetContact(account, (ClientType)Enum.Parse(typeof(ClientType), message.CommandValues[1].ToString()))
                :
                NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

            OnContactLeft(contact, endPointId); // notify the client programmer

            if (IsAllContactsInRosterLeft())
            {
                OnAllContactsLeft();  //Indicates whe should end the conversation and disconnect.
            }
        }

        /// <summary>
        /// Called when a CAL command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our request to invite a contact.
        /// <code>CAL [Transaction] ['RINGING'] [sessionId]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnCALReceived(SBMessage message)
        {
            // this is not very interesting so do nothing at the moment
        }

        /// <summary>
        /// Called when a IRO command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates contacts in the session that have joined.
        /// <code>IRO [Transaction] [Current] [Total] [account[;GUID]] [DisplayName] [Caps]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnIROReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[3].ToString().ToLowerInvariant();
            string displayName = HttpUtility.UrlDecode(message.CommandValues[4].ToString());
            string capacitiesString = message.CommandValues[5].ToString();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            SetRosterProperty(fullaccount, displayName, RosterProperties.Name);
            SetRosterProperty(fullaccount, capacitiesString, RosterProperties.ClientCapacityString);
            SetRosterProperty(fullaccount, ContactConversationState.Joined.ToString(), RosterProperties.Status);

            string account = fullaccount;
            Guid endpointGuid = Guid.Empty;

            if (fullaccount.Contains(";"))
            {
                account = fullaccount.Split(';')[0];
                endpointGuid = new Guid(fullaccount.Split(';')[1]);
            }

            if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() == account)
                return;

            // Get the contact.
            Contact contact = NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

            // Not in contact list (anonymous). Update it's name and caps.
            if (contact.Lists == MSNLists.None && NSMessageHandler.BotMode)
            {
                if (endpointGuid != Guid.Empty)
                {
                    if (contact.PersonalMessage == null)
                    {
                        PersonalMessage personalMessage = new PersonalMessage("", MediaType.None, new string[] { }, endpointGuid);
                        contact.SetPersonalMessage(personalMessage);
                    }
                    else
                    {
                        PersonalMessage personalMessage = new PersonalMessage(contact.PersonalMessage.Message,
                            contact.PersonalMessage.MediaType, contact.PersonalMessage.CurrentMediaContent, endpointGuid);
                        contact.SetPersonalMessage(personalMessage);
                    }
                }
            }

            if (message.CommandValues.Count >= 5)
                contact.SetName(MSNHttpUtility.UrlDecode(message.CommandValues[4].ToString()));

            if (message.CommandValues.Count >= 6)
            {
                string caps = message.CommandValues[5].ToString();
                if (caps.Contains(":"))
                {
                    contact.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps.Split(':')[0]);
                    contact.ClientCapacitiesEx = (ClientCapacitiesEx)Convert.ToInt64(caps.Split(':')[1]);
                }
                else
                {
                    contact.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps);
                }
            }


            // Notify the client programmer.
            if (oldStatus != ContactConversationState.Joined)
            {
                OnContactJoined(contact, endpointGuid);
            }
        }

        /// <summary>
        /// Called when a JOI command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has joined the session.
        /// This will fire the <see cref="ContactJoined"/> event.
        /// <code>JOI [account[;GUID]] [DisplayName] [Caps]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnJOIReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[0].ToString().ToLowerInvariant();
            string displayName = HttpUtility.UrlDecode(message.CommandValues[1].ToString());
            string capacitiesString = message.CommandValues[2].ToString();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            SetRosterProperty(fullaccount, displayName, RosterProperties.Name);
            SetRosterProperty(fullaccount, capacitiesString, RosterProperties.ClientCapacityString);
            SetRosterProperty(fullaccount, ContactConversationState.Joined.ToString(), RosterProperties.Status);

            string account = fullaccount;
            Guid endpointGuid = Guid.Empty;

            if (fullaccount.Contains(";"))
            {
                account = fullaccount.Split(';')[0];
                endpointGuid = new Guid(fullaccount.Split(';')[1]);
            }

            if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() != account)
            {
                // Get the contact.
                Contact contact = NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

                // Not in contact list (anonymous). Update it's name and caps.
                if (contact.Lists == MSNLists.None && NSMessageHandler.BotMode)
                {
                    if (endpointGuid != Guid.Empty)
                    {
                        if (contact.PersonalMessage == null)
                        {
                            PersonalMessage personalMessage = new PersonalMessage("", MediaType.None, new string[] { }, endpointGuid);
                            contact.SetPersonalMessage(personalMessage);
                        }
                        else
                        {
                            PersonalMessage personalMessage = new PersonalMessage(contact.PersonalMessage.Message,
                                contact.PersonalMessage.MediaType, contact.PersonalMessage.CurrentMediaContent, endpointGuid);
                            contact.SetPersonalMessage(personalMessage);
                        }
                    }
                }

                if (message.CommandValues.Count >= 2)
                    contact.SetName(MSNHttpUtility.UrlDecode(message.CommandValues[1].ToString()));

                if (message.CommandValues.Count >= 3)
                {
                    string caps = message.CommandValues[2].ToString();
                    if (caps.Contains(":"))
                    {
                        contact.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps.Split(':')[0]);
                        contact.ClientCapacitiesEx = (ClientCapacitiesEx)Convert.ToInt64(caps.Split(':')[1]);
                    }
                    else
                    {
                        contact.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps);
                    }
                }


                // Notify the client programmer.
                if (oldStatus != ContactConversationState.Joined)
                {
                    OnContactJoined(contact, endpointGuid);
                }
            }
        }

        /// <summary>
        /// Called when a USR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our identification USR command.
        /// <code>USR [Transaction] ['OK'] [account[;GUID]] [name]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUSRReceived(SBMessage message)
        {
            if (message.CommandValues[1].ToString() == "OK")
            {
                string account = message.CommandValues[2].ToString().ToLowerInvariant();
                if (account.Contains(";"))
                {
                    account = account.Split(';')[0];
                }

                if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() == account)
                {
                    // update the owner's name. Just to be sure.
                    // NSMessageHandler.ContactList.Owner.SetName(message.CommandValues[3].ToString());
                    if (NSMessageHandler != null)
                    {
                        Invite(NSMessageHandler.ContactList.Owner);
                    }
                    // we are now ready to invite other contacts. Notify the client of this.
                    OnSessionEstablished();
                }
            }
        }

        /// <summary>
        /// Called when a MSG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has send us a message. This can be a plain text message,
        /// an invitation, or an application specific message.
        /// <code>MSG [Account] [Name] [Bodysize]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnMSGReceived(MSNMessage message)
        {
            // the MSG command is the most versatile one. These are all the messages
            // between clients. Like normal messages, file transfer invitations, P2P messages, etc.
            Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString(), ClientType.PassportMember);

            // update the name to make sure we have it up-to-date
            //contact.SetName(message.CommandValues[1].ToString());

            // get the corresponding SBMSGMessage object
            MSGMessage sbMSGMessage = new MSGMessage(message);

            //first check if we are dealing with multi-packet-messages
            if (sbMSGMessage.MimeHeader.ContainsKey("Message-ID"))
            {
                //is this the first message?
                if (sbMSGMessage.MimeHeader.ContainsKey("Chunks"))
                {
                    multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/0", sbMSGMessage);
                    return;
                }

                else if (sbMSGMessage.MimeHeader.ContainsKey("Chunk"))
                {
                    //Is this the last message?
                    if (Convert.ToInt32(sbMSGMessage.MimeHeader["Chunk"]) + 1 == Convert.ToInt32(multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"].MimeHeader["Chunks"]))
                    {
                        //Paste all the pieces together
                        MSGMessage completeMessage = multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"];
                        multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/0");

                        int chunksToProcess = Convert.ToInt32(completeMessage.MimeHeader["Chunks"]) - 2;
                        List<byte> completeText = new List<byte>(completeMessage.InnerBody);
                        for (int i = 0; i < chunksToProcess; i++)
                        {
                            MSGMessage part = multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1)];
                            completeText.AddRange(part.InnerBody);

                            //Remove the part from the buffer
                            multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1));
                        }

                        completeText.AddRange(sbMSGMessage.InnerBody);
                        completeMessage.InnerBody = completeText.ToArray();

                        //process the message
                        sbMSGMessage = completeMessage;
                    }
                    else
                    {
                        multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/" + sbMSGMessage.MimeHeader["Chunk"], sbMSGMessage);
                        return;
                    }
                }
                else
                    throw new Exception("Multi-packetmessage with damaged headers received");
            }

            if (sbMSGMessage.MimeHeader.ContainsKey(MimeHeaderStrings.Content_Type))
            {
                switch (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                    case "text/x-mms-emoticon":
                    case "text/x-mms-animemoticon":
                    case "text/x-msnmsgr-datacast":
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, message.ToDebugString(), GetType().Name);
                        break;
                    default:
                        if (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, message.ToDebugString(), GetType().Name);
                        }
                        break;
                }

                switch (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                        // make sure we don't parse the rest of the message in the next loop											
                        OnUserTyping(NSMessageHandler.ContactList.GetContact(sbMSGMessage.MimeHeader["TypingUser"], ClientType.PassportMember));
                        break;

                    case "text/x-mms-emoticon":
                    case "text/x-mms-animemoticon":
                        OnEmoticonDefinition(sbMSGMessage, contact);
                        break;

                    case "text/x-msnmsgr-datacast":
                        if (message.CommandValues[2].Equals("69"))
                            OnNudgeReceived(contact);
                        else if (message.CommandValues[2].Equals("1325"))
                            OnWinkReceived(sbMSGMessage, contact);
                        break;

                    default:
                        if (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            // a normal message has been sent, notify the client programmer
                            TextMessage msg = new TextMessage();
                            msg.CreateFromMessage(sbMSGMessage);
                            OnTextMessageReceived(msg, contact);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a ACK command has been received.
        /// </summary>
        /// <remarks>
        /// <code>ACK [MSGTransid]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnACKReceived(SBMessage message)
        {
        }

        #endregion

        #region Switchboard Handling

        private void ClearAll()
        {
            lock (rosterState)
                rosterState.Clear();
            lock (rosterName)
                rosterName.Clear();
            lock (rosterCapacities)
                rosterCapacities.Clear();
        }

        /// <summary>
        /// Called when the message processor has established a connection. This function will 
        /// begin the login procedure by sending the USR or ANS command.
        /// </summary>
        protected virtual void OnProcessorConnectCallback(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "SB processor connected.", GetType().Name);
            SendInitialMessage();
        }

        /// <summary>
        /// Called when the message processor has disconnected. This function will 
        /// set the IsSessionEstablished to false.
        /// </summary>
        protected virtual void OnProcessorDisconnectCallback(object sender, EventArgs e)
        {
            lock (syncObject)
            {
                if (sessionEstablished)
                    OnSessionClosed();

                sessionEstablished = false;
            }
        }

        /// <summary>
        /// The processor to handle the messages
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            
            set
            {
                ////if (messageProcessor != null)
                ////{
                ////    messageProcessor.ConnectionEstablished -= OnProcessorConnectCallback;
                ////    messageProcessor.ConnectionClosed -= OnProcessorDisconnectCallback;
                ////}

                ////messageProcessor = value as SocketMessageProcessor;

                ////if (messageProcessor != null)
                ////{
                ////    // catch the connect event to start sending the USR command upon initiating
                ////    messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
                ////    messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
                ////}

                throw new InvalidOperationException("This property is read-only.");
            }
        }


        /// <summary>
        /// Handles message from the processor.
        /// </summary>
        /// <remarks>
        /// This is one of the most important functions of the class.
        /// It handles incoming messages and performs actions based on the commands in the messages.
        /// Exceptions which occur in this method are redirected via the <see cref="ExceptionOccurred"/> event.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="message">The network message received from the notification server</param>
        public virtual void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            try
            {
                // We expect at least a SBMessage object
                SBMessage sbMessage = (SBMessage)message;

                switch (sbMessage.Command)
                {
                    case "ACK":
                    case "ANS":
                    case "BYE":
                    case "CAL":
                    case "IRO":
                    case "JOI":
                    case "USR":
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, sbMessage.ToDebugString(), GetType().Name);
                        break;
                }

                switch (sbMessage.Command)
                {
                    case "MSG":
                        OnMSGReceived(sbMessage);
                        return;
                    case "ACK":
                        OnACKReceived(sbMessage);
                        return;

                    case "ANS":
                        OnANSReceived(sbMessage);
                        return;
                    case "BYE":
                        OnBYEReceived(sbMessage);
                        return;
                    case "CAL":
                        OnCALReceived(sbMessage);
                        return;
                    case "IRO":
                        OnIROReceived(sbMessage);
                        return;
                    case "JOI":
                        OnJOIReceived(sbMessage);
                        return;
                    case "USR":
                        OnUSRReceived(sbMessage);
                        return;
                }

                // Check whether it is a numeric error command
                if (sbMessage.Command[0] >= '0' && sbMessage.Command[0] <= '9')
                {
                    try
                    {
                        int errorCode = int.Parse(sbMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
                        OnServerErrorReceived((MSNError)errorCode);
                    }
                    catch (FormatException fe)
                    {
                        throw new MSNPSharpException("Exception occurred while parsing an error code received from the switchboard server", fe);
                    }
                }
                else
                {
                    // It is a unknown command
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "UNKNOWN COMMAND: " + sbMessage.Command + "\r\n" + sbMessage.ToDebugString(), GetType().ToString());
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(e);
                throw; //RethrowToPreserveStackDetails (without e)
            }
        }

        /// <summary>
        /// Fires the ServerErrorReceived event.
        /// </summary>
        protected virtual void OnServerErrorReceived(MSNError serverError)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, new MSNErrorEventArgs(serverError));
        }

        /// <summary>
        /// Fires the <see cref="ExceptionOccurred"/> event.
        /// </summary>
        /// <param name="e">The exception which was thrown</param>
        protected virtual void OnExceptionOccurred(Exception e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, new ExceptionEventArgs(e));
        }

        #endregion

        /// <summary>
        /// Debug string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetType().ToString() + " SessionHash: " + SessionHash;
        }

    }
};