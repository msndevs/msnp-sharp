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
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    public class MSNObjectDataTransferCompletedEventArgs : EventArgs
    {
        private MSNObject clientData;
        private bool aborted;

        /// <summary>
        /// Transfer failed.
        /// </summary>
        public bool Aborted
        {
            get { return aborted; }
        }

        /// <summary>
        /// The target msnobject.
        /// </summary>
        public MSNObject ClientData
        {
            get { return clientData; }
        }

        protected MSNObjectDataTransferCompletedEventArgs()
            : base()
        {
        }

        public MSNObjectDataTransferCompletedEventArgs(MSNObject clientdata, bool abort)
        {
            if (clientdata == null)
                throw new ArgumentNullException("clientdata");

            clientData = clientdata;
            aborted = abort;
        }
    }

    public class ConversationEndEventArgs : EventArgs
    {
        private Conversation conversation = null;

        public Conversation Conversation
        {
            get { return conversation; }
        }

        protected ConversationEndEventArgs()
            : base()
        {
        }

        public ConversationEndEventArgs(Conversation convers)
        {
            conversation = convers;
        }
    }

    internal abstract class MessageObject
    {
        protected object innerObject = null;

        public object InnerObject
        {
            get { return innerObject; }
        }
    }

    internal class UserTypingObject : MessageObject
    {

    }

    internal class NudgeObject : MessageObject { }
    internal class TextMessageObject : MessageObject
    {
        public TextMessageObject(TextMessage message)
        {
            innerObject = message;
        }
    }

    internal class EmoticonObject : MessageObject
    {
        private EmoticonType type;

        public EmoticonType Type
        {
            get { return type; }
        }
        public EmoticonObject(List<Emoticon> iconlist, EmoticonType icontype)
        {
            innerObject = iconlist;
            type = icontype;
        }
    }

    /// <summary>
    /// A facade to the underlying switchboard and YIM session.
    /// </summary>
    /// <remarks>
    /// Conversation implements a few features for the ease of the application programmer. It provides
    /// directly basic common functionality. However, if you need to perform more advanced actions, or catch
    /// other events you have to directly use the underlying switchboard handler, or switchboard processor.
    /// Conversation automatically requests emoticons used by remote contacts.
    /// </remarks>
    public class Conversation
    {
        #region Private

        #region Members

        private Messenger _messenger = null;
        private SBMessageHandler _switchboard = new SBMessageHandler();
        private YIMMessageHandler _yimHandler = new YIMMessageHandler();
        private bool expired = false;
        private bool sbInitialized = false;
        private bool yimInitialized = false;
        private bool ended = false;
        private bool ending = false;

        private bool autoRequestEmoticons = true;
        private bool autoKeepAlive = false;

        private List<Contact> _leftContacts = new List<Contact>(0);
        private List<Contact> _contacts = new List<Contact>(0);

        private Queue<MessageObject> _messageQueues = new Queue<MessageObject>(0);
        private ConversationType _type = ConversationType.None;
        private int keepalivePeriod = 30000;

        private Timer keepaliveTimer = null; 
        #endregion

        private void transferSession_TransferAborted(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon aborted", GetType().Name);
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs((sender as P2PTransferSession).ClientData as MSNObject, true));
        }

        private void transferSession_TransferFinished(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon received", GetType().Name);
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs((sender as P2PTransferSession).ClientData as MSNObject, false));

        }

        private void AddContact(Contact contact)
        {
            lock (_contacts)
            {
                if (_contacts.Contains(contact))
                    return;
                _contacts.Add(contact);
            }
        }

        private static void KeepAlive(object state)
        {
            Conversation convers = state as Conversation;
            if (convers.AutoKeepAlive)
            {
                if ((convers.Type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
                {
                    if (convers.sbInitialized && !convers.Expired && !convers.Ended)
                    {
                        convers._switchboard.SendKeepAliveMessage();
                    }
                }
            }
        }

        private void IniCommonSettings()
        {
            //Must call after _switchboard and _yimHandler have been initialized.
            AttachEvents(_switchboard);
            AttachEvents(_yimHandler);
        }

        private void LeftContactEnqueue(Contact leftcontact)
        {
            lock (_leftContacts)
            {
                if (!_leftContacts.Contains(leftcontact))
                    _leftContacts.Add(leftcontact);
            }
        }

        private void MessageEnqueue(MessageObject message)
        {
            lock (_leftContacts)
            {
                lock (_messageQueues)
                {
                    foreach (Contact contact in _leftContacts)
                    {
                        _messageQueues.Enqueue(message);
                    }
                }
            }
        }

        private void SwitchBoardReInvite()
        {
            List<Contact> leftcontacts = new List<Contact>(_leftContacts);
            foreach (Contact contact in leftcontacts)
            {
                if (contact.Status == PresenceStatus.Offline)
                {
                    lock (_leftContacts)
                    {
                        _leftContacts.Remove(contact);
                    }

                    foreach (MessageObject msgobj in _messageQueues)
                    {
                        if (msgobj is TextMessageObject && _switchboard.NSMessageHandler != null)
                        {
                            _switchboard.NSMessageHandler.OIMService.SendOIMMessage(contact, msgobj.InnerObject as TextMessage);

                        }
                    }
                }
            }

            leftcontacts = new List<Contact>(_leftContacts);
            foreach (Contact contact in leftcontacts)
            {
                _switchboard.Invite(contact);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard " + _switchboard.SessionHash + " inviting user: " + contact.Mail);
            }
        }

        private void AttachEvents(SBMessageHandler switchboard)
        {
            switchboard.AllContactsLeft += new EventHandler<EventArgs>(OnAllContactsLeft);
            switchboard.ContactJoined += new EventHandler<ContactEventArgs>(OnContactJoined);
            switchboard.ContactLeft += new EventHandler<ContactEventArgs>(OnContactLeft);
            switchboard.EmoticonDefinitionReceived += new EventHandler<EmoticonDefinitionEventArgs>(OnEmoticonDefinitionReceived);
            switchboard.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(OnExceptionOccurred);
            switchboard.NudgeReceived += new EventHandler<ContactEventArgs>(OnNudgeReceived);
            switchboard.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(OnServerErrorReceived);
            switchboard.SessionClosed += new EventHandler<EventArgs>(OnSessionClosed);
            switchboard.SessionEstablished += new EventHandler<EventArgs>(OnSessionEstablished);
            switchboard.TextMessageReceived += new EventHandler<TextMessageEventArgs>(OnTextMessageReceived);
            switchboard.UserTyping += new EventHandler<ContactEventArgs>(OnUserTyping);
            switchboard.WinkReceived += new EventHandler<WinkEventArgs>(OnWinkReceived);
        }

        private void DetachEvents(SBMessageHandler switchboard)
        {
            switchboard.AllContactsLeft -= (OnAllContactsLeft);
            switchboard.ContactJoined -= (OnContactJoined);
            switchboard.ContactLeft -= (OnContactLeft);
            switchboard.EmoticonDefinitionReceived -= (OnEmoticonDefinitionReceived);
            switchboard.ExceptionOccurred -= (OnExceptionOccurred);
            switchboard.NudgeReceived -= (OnNudgeReceived);
            switchboard.ServerErrorReceived -= (OnServerErrorReceived);
            switchboard.SessionClosed -= (OnSessionClosed);
            switchboard.SessionEstablished -= (OnSessionEstablished);
            switchboard.TextMessageReceived -= (OnTextMessageReceived);
            switchboard.UserTyping -= (OnUserTyping);
            switchboard.WinkReceived -= (OnWinkReceived);
        }


        private void SwitchBoardEnd(object param)
        {
            try
            {
                if ((bool)param == false)
                {
                    Switchboard.Left();
                }
                
            }
            catch (Exception)
            {
            }

            sbInitialized = false;
        }

        #endregion


        #region Protected

        protected void OnMSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            if (MSNObjectDataTransferCompleted != null)
                MSNObjectDataTransferCompleted(sender, e);
        }


        protected virtual void OnConversationEnded(Conversation conversation)
        {
            if (ConversationEnded != null)
            {
                ConversationEnded(this, new ConversationEndEventArgs(conversation));
            }
        }

        #region Event operation


        protected virtual void OnWinkReceived(object sender, WinkEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (WinkReceived != null)
                WinkReceived(this, e);
        }

        protected virtual void OnUserTyping(object sender, ContactEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (UserTyping != null)
                UserTyping(this, e);
        }

        protected virtual void OnTextMessageReceived(object sender, TextMessageEventArgs e)
        {
            _type |= ConversationType.Chat;

            if (TextMessageReceived != null)
                TextMessageReceived(this, e);
        }

        protected virtual void OnSessionEstablished(object sender, EventArgs e)
        {
            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                SwitchBoardReInvite();
            }

            if (SessionEstablished != null)
                SessionEstablished(this, e);
        }

        protected virtual void OnSessionClosed(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(SBMessageHandler))
            {
                sbInitialized = false;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, sender.GetType().ToString() + " session :" + _switchboard.SessionHash + " closed.");

            if (SessionClosed != null)
                SessionClosed(this, e);

            if (ending)
            {
                DetachEvents(_switchboard);
                DetachEvents(_yimHandler);
                ending = false;
            }
        }

        protected virtual void OnServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, e);
        }

        protected virtual void OnNudgeReceived(object sender, ContactEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (NudgeReceived != null)
                NudgeReceived(this, e);
        }

        protected virtual void OnExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, e);
        }

        protected virtual void OnEmoticonDefinitionReceived(object sender, EmoticonDefinitionEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (AutoRequestEmoticons == false)
                return;

            MSNObject existing = MSNObjectCatalog.GetInstance().Get(e.Emoticon.CalculateChecksum());
            if (existing == null)
            {
                e.Sender.Emoticons[e.Emoticon.Sha] = e.Emoticon;

                // create a session and send the invitation
                P2PMessageSession session = Messenger.Nameserver.P2PHandler.GetSession(Messenger.Nameserver.ContactList.Owner, e.Sender);

                object handlerObject = session.GetHandler(typeof(MSNSLPHandler));
                if (handlerObject != null)
                {
                    MSNSLPHandler msnslpHandler = (MSNSLPHandler)handlerObject;

                    P2PTransferSession transferSession = msnslpHandler.SendInvitation(session.LocalUser, session.RemoteUser, e.Emoticon);
                    transferSession.DataStream = e.Emoticon.OpenStream();
                    transferSession.ClientData = e.Emoticon;

                    transferSession.TransferAborted += new EventHandler<EventArgs>(transferSession_TransferAborted);
                    transferSession.TransferFinished += new EventHandler<EventArgs>(transferSession_TransferFinished);

                    MSNObjectCatalog.GetInstance().Add(e.Emoticon);
                }
                else
                    throw new MSNPSharpException("No MSNSLPHandler was attached to the p2p message session. An emoticon invitation message could not be send.");
            }
            else
            {
                //If exists, fire the event.
                OnMSNObjectDataTransferCompleted(sender, new MSNObjectDataTransferCompletedEventArgs(existing, false));
            }


            if (EmoticonDefinitionReceived != null)
                EmoticonDefinitionReceived(this, e);
        }

        protected virtual void OnContactLeft(object sender, ContactEventArgs e)
        {
            LeftContactEnqueue(e.Contact);
            if (ContactLeft != null)
                ContactLeft(this, e);
        }

        protected virtual void OnContactJoined(object sender, ContactEventArgs e)
        {
            AddContact(e.Contact);
            lock (_leftContacts)
            {
                if (_leftContacts.Contains(e.Contact))
                    _leftContacts.Remove(e.Contact);
                if (_leftContacts.Count == 0)
                {
                    lock (_messageQueues)
                    {
                        while (_messageQueues.Count > 0)
                        {
                            MessageObject msgobj = _messageQueues.Dequeue();
                            if (msgobj is TextMessageObject)
                            {
                                SendTextMessage(msgobj.InnerObject as TextMessage);
                            }

                            if (msgobj is NudgeObject)
                            {
                                SendNudge();
                            }

                            if (msgobj is EmoticonObject)
                            {
                                SendEmoticonDefinitions(msgobj.InnerObject as List<Emoticon>, (msgobj as EmoticonObject).Type);
                            }
                        }
                    }
                }
            }

            if (ContactJoined != null)
                ContactJoined(this, e);
        }

        protected virtual void OnAllContactsLeft(object sender, EventArgs e)
        {
            if (AllContactsLeft != null)
                AllContactsLeft(this, e);
            EndSwitchBoardSession(true);
        }

        /// <summary>
        /// Create a new conversation which contains the same users as the expired one.
        /// </summary>
        /// <returns>A new conversation.</returns>
        /// <exception cref="InvalidOperationException">The current conversation is not expired.</exception>
        protected virtual Conversation ReCreate()
        {
            if (!Expired)
                return this;


            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                Messenger.Nameserver.RequestSwitchboard(_switchboard, Messenger);
                sbInitialized = true;
            }

            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                yimInitialized = true;  //For further use.
            }

            expired = false;

            foreach (Contact contact in _leftContacts)
            {
                Invite(contact);
            }

            return this;
        }


        #endregion

        #endregion

        #region Internal

        #endregion

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
        /// Occurs when a switchboard connection has been made and the initial handshaking commands are send. This indicates that the session is ready to invite or accept other contacts.
        /// </summary>
        public event EventHandler<EventArgs> SessionEstablished;

        /// <summary>
        /// Fired when a contact joins. In case of a conversation with two people in it this event is called with the remote contact specified in the event argument.
        /// </summary>
        public event EventHandler<ContactEventArgs> ContactJoined;
        /// <summary>
        /// Fired when a contact leaves the conversation.
        /// </summary>
        public event EventHandler<ContactEventArgs> ContactLeft;

        /// <summary>
        /// Fired when a message is received from any of the other contacts in the conversation.
        /// </summary>
        public event EventHandler<TextMessageEventArgs> TextMessageReceived;

        /// <summary>
        /// Fired when a contact sends a emoticon definition.
        /// </summary>
        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;

        public event EventHandler<WinkEventArgs> WinkReceived;

        /// <summary>
        /// Fired when a contact sends a nudge
        /// </summary>
        public event EventHandler<ContactEventArgs> NudgeReceived;

        /// <summary>
        /// Fired when any of the other contacts is typing a message.
        /// </summary>
        public event EventHandler<ContactEventArgs> UserTyping;

        /// <summary>
        /// Occurs when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Occurs when the MSN Switchboard Server sends us an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;


        #endregion

        #region Public
        /// <summary>
        /// Fired when the data transfer for a MSNObject finished or aborted.
        /// </summary>
        public event EventHandler<MSNObjectDataTransferCompletedEventArgs> MSNObjectDataTransferCompleted;

        /// <summary>
        /// Occurs when a new conversation is ended (all contacts in the conversation have left or <see cref="Conversation.End()"/> is called).
        /// </summary>
        public event EventHandler<ConversationEndEventArgs> ConversationEnded;

        #region Properties

        /// <summary>
        /// Contacts once or currently in the conversation.
        /// </summary>
        public List<Contact> Contacts
        {
            get { return _contacts; }
        }

        /// <summary>
        /// Indicates the type of current available switchboard in this conversation.
        /// </summary>
        public ConversationType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Indicates whether all contacts in conversation have left.<br/>
        /// A YIM conversation will never expired. <br/>
        /// A switchboard conversation expired after all contacts left.
        /// </summary>
        public bool Expired
        {
            get { return expired; }
        }

        /// <summary>
        /// Indicates whether the conversation is ended by user.<br/>
        /// If a conversation is ended, it can't be used to send any message.
        /// </summary>
        public bool Ended
        {
            get { return ended; }
        }

        /// <summary>
        /// Indicates whether emoticons from remote contacts are automatically retrieved
        /// </summary>
        public bool AutoRequestEmoticons
        {
            get
            {
                return autoRequestEmoticons;
            }
            set
            {
                autoRequestEmoticons = value;
            }
        }

        /// <summary>
        /// Indicates whether the conversation will never expired until the owner close it. <br/>
        /// If true, <see cref="Conversation.SessionClosed"/> will never fired and a keep-alive message will send to the switchboard every <see cref="KeepAliveMessagePeriod"/> seconds.
        /// </summary>
        /// <exception cref="InvalidOperationException">Setting this property on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Setting this property for a YIM conversation or an expired conversation.</exception>
        public bool AutoKeepAlive
        {
            get
            {
                if ((_type & ConversationType.YIM) == ConversationType.YIM)
                    return true;

                return autoKeepAlive;
            }

            set
            {
                if (Ended)
                {
                    throw new InvalidOperationException("Conversation ended.");
                }

                if (Expired || (_type & ConversationType.YIM) == ConversationType.YIM)
                {
                    //YIM handlers, expired handlers. Should I throw an exception here?
                    throw new NotSupportedException("Cannot set keep-alive property to true for this conversation type.");
                }


                if (value && autoKeepAlive != value)
                {
                    if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard || _type == ConversationType.None)
                    {
                        autoKeepAlive = value;
                        keepaliveTimer = new Timer(new TimerCallback(KeepAlive), this, keepalivePeriod, keepalivePeriod);
                        return;
                    }

                }

                if (!value && autoKeepAlive != value)
                {
                    autoKeepAlive = value;
                    keepaliveTimer.Dispose();
                    keepaliveTimer = null;
                }
            }
        }

        /// <summary>
        /// The period between two keep-alive messages sent (In second).
        /// </summary>
        public int KeepAliveMessagePeriod
        {
            get { return keepalivePeriod / 1000; }
            set
            {
                keepalivePeriod = value * 1000;
                if (keepaliveTimer != null)
                    keepaliveTimer.Change(keepalivePeriod, keepalivePeriod);
                else
                    keepaliveTimer = new Timer(new TimerCallback(KeepAlive), this, keepalivePeriod, keepalivePeriod);
            }
        }

        /// <summary>
        /// Messenger that created the conversation
        /// </summary>
        public Messenger Messenger
        {
            get
            {
                return _messenger;
            }
            set
            {
                _messenger = value;
            }
        }

        /// <summary>
        /// The switchboard processor. Sends and receives messages over the switchboard connection
        /// </summary>
        public SocketMessageProcessor SwitchboardProcessor
        {
            get
            {
                return (SocketMessageProcessor)_switchboard.MessageProcessor;
            }
            set
            {
                _switchboard.MessageProcessor = value;
            }
        }

        /// <summary>
        /// The switchboard handler. Handles incoming/outgoing messages.<br/>
        /// If the conversation ended, this property will be null.
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                if ((_type & ConversationType.SwitchBoard) != ConversationType.SwitchBoard)
                {
                    return null;
                }
                else
                {
                    return _switchboard;
                }
            }
        }

        /// <summary>
        /// Yahoo! Message handler.<br/>
        /// If the conversation ended, this property will be null.
        /// </summary>
        public YIMMessageHandler YIMHandler
        {
            get
            {
                if ((_type & ConversationType.YIM) != ConversationType.YIM)
                {
                    return null;
                }
                else
                {
                    return _yimHandler;
                }
            }
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        /// <param name="sbHandler">The switchboard to interface to.</param>		
        public Conversation(Messenger parent, SBMessageHandler sbHandler)
        {
            if (sbHandler is YIMMessageHandler)
            {
                _yimHandler = sbHandler as YIMMessageHandler;
                sbInitialized = false;
                yimInitialized = true;
                _type = ConversationType.YIM;
            }
            else
            {
                _switchboard = sbHandler;
                sbInitialized = true;
                yimInitialized = false;
                _type = ConversationType.SwitchBoard;
            }

            _messenger = parent;
            IniCommonSettings();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        public Conversation(Messenger parent)
        {
            _messenger = parent;
            sbInitialized = false;
            yimInitialized = false;

            _type = ConversationType.None;
            IniCommonSettings();
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contactMail">Contact account</param>
        /// <param name="type">Contact type</param>
        /// <exception cref="InvalidOperationException">Operating on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Inviting mutiple YIM users into a YIM conversation, invite YIM users to a switchboard conversation, or passport members are invited into YIM conversation.</exception>
        public void Invite(string contactMail, ClientType type)
        {
            if (Ended)
            {
                throw new InvalidOperationException("Conversation ended.");
            }

            if (Expired)
            {
                ReCreate();
            }

            if ((_type & ConversationType.YIM) == ConversationType.YIM && type != ClientType.EmailMember)
            {
                throw new NotSupportedException("Only Yahoo messenger users can be invited in a YIM conversation.");
            }


            if ((_type & ConversationType.YIM) == ConversationType.YIM && type == ClientType.EmailMember)
            {
                if (_contacts.Count > 1)
                    throw new NotSupportedException("Mutiple user not supported in YIM conversation.");
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard &&
                (type != ClientType.PassportMember && type != ClientType.LCS))
            {
                throw new NotSupportedException("Only Passport members can be invited in a switchboard conversation.");
            }

            if (_type == ConversationType.None)
            {
                switch (type)
                {
                    case ClientType.EmailMember:
                        _type = ConversationType.YIM;
                        break;

                    case ClientType.LCS:
                    case ClientType.PassportMember:
                        _type = ConversationType.SwitchBoard;
                        break;
                }
            }

            if (!Messenger.ContactList.HasContact(contactMail, type))
            {
                throw new MSNPSharpException("Contact not on your contact list.");
            }

            Contact contact = Messenger.ContactList.GetContact(contactMail, type);

            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                _yimHandler.NSMessageHandler = Messenger.Nameserver;
                _yimHandler.MessageProcessor = Messenger.Nameserver.MessageProcessor;

                lock (Messenger.Nameserver.P2PHandler.SwitchboardSessions)
                {
                    if (!Messenger.Nameserver.P2PHandler.SwitchboardSessions.Contains(_yimHandler))
                    {
                        Messenger.Nameserver.P2PHandler.SwitchboardSessions.Add(_yimHandler);
                    }
                }

                Messenger.Nameserver.MessageProcessor.RegisterHandler(_yimHandler);
                yimInitialized = true;
                if (!_yimHandler.Contacts.ContainsKey(contact))
                {
                    _yimHandler.Invite(contact);
                }
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "YIM hanlder inviting user: " + contactMail);
                return;
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (Switchboard.NSMessageHandler == null)
                    Switchboard.NSMessageHandler = Messenger.Nameserver;

                foreach (Contact acc in _switchboard.Contacts.Keys)
                {
                    if (acc != Messenger.Nameserver.ContactList.Owner)  //MSNP18: owner in switch
                    {
                        _type |= ConversationType.MutipleUsers;
                        break;
                    }
                }

                lock (Messenger.Nameserver.P2PHandler.SwitchboardSessions)
                {
                    if (!Messenger.Nameserver.P2PHandler.SwitchboardSessions.Contains(_switchboard))
                    {
                        Messenger.Nameserver.P2PHandler.SwitchboardSessions.Add(_switchboard);
                    }
                }

                if (sbInitialized == false)
                {
                    LeftContactEnqueue(contact);  //Enqueue the contact if user send message before it join.
                    Messenger.Nameserver.RequestSwitchboard(_switchboard, this);

                    sbInitialized = true;
                    return;
                }

                if (Switchboard.Contacts.ContainsKey(contact) &&
                    (Switchboard.Contacts[contact] == ContactConversationState.Left ||
                    Switchboard.Contacts[contact] == ContactConversationState.Invited))
                {
                    LeftContactEnqueue(contact); //Enqueue the contact if user send message before it join.
                }

                Switchboard.Invite(contact);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard inviting user: " + contactMail);
            }
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contact">The remote contact to invite.</param>
        /// <exception cref="InvalidOperationException">Operating on an expired conversation will get this exception.</exception>
        /// <exception cref="NotSupportedException">Inviting mutiple YIM users into a YIM conversation, invite YIM users to a switchboard conversation, or passport members are invited into YIM conversation.</exception>
        public void Invite(Contact contact)
        {
            Invite(contact.Mail, contact.ClientType);
        }

        /// <summary>
        /// End this conversation.
        /// </summary>
        public void End()
        {
            ending = true;
            EndSwitchBoardSession(false);
            ended = true;

            OnConversationEnded(this);
        }

        public void EndSwitchBoardSession(bool remoteDisconnect)
        {
            if (sbInitialized)
            {
                Thread endthread = new Thread(new ParameterizedThreadStart(SwitchBoardEnd));  //Avoid blocking the UI thread.
                endthread.Start(remoteDisconnect);
            }

            if (yimInitialized)
            {
                YIMHandler.Left();
                yimInitialized = false;
            }

            expired = true;
            if (keepaliveTimer != null)
                keepaliveTimer.Dispose();

        }

        /// <summary>
        /// Whether the specified contact is in the conversation.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public bool HasContact(Contact contact)
        {
            if (_type == ConversationType.None)
                return false;
            if ((_type & ConversationType.YIM) == ConversationType.YIM && contact.ClientType == ClientType.EmailMember)
            {
                if (_yimHandler.Contacts.ContainsKey(contact))
                    return true;
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard &&
                (contact.ClientType == ClientType.PassportMember || contact.ClientType == ClientType.LCS))
            {
                if (_switchboard.Contacts.ContainsKey(contact))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sends a plain text message to all other contacts in the conversation.
        /// </summary>
        /// <remarks>
        /// This method wraps the TextMessage object in a SBMessage object and sends it over the network.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendTextMessage(TextMessage message)
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                throw new InvalidOperationException("Conversation ended.");
            }

            if (Expired)
            {
                ReCreate();
            }

            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                _yimHandler.SendTextMessage(message);
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (_leftContacts.Count > 0)
                {
                    MessageEnqueue(new TextMessageObject(message));
                    SwitchBoardReInvite();
                }
                else
                {
                    _switchboard.SendTextMessage(message);
                }
            }
        }

        /// <summary>
        /// Sends a 'user is typing..' message to the switchboard, and is received by all participants.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendTypingMessage()
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                throw new InvalidOperationException("Conversation ended.");
            }

            if (Expired)
            {
                ReCreate();
            }


            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                _yimHandler.SendTypingMessage();
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (_leftContacts.Count > 0)
                {
                    MessageEnqueue(new UserTypingObject());
                    SwitchBoardReInvite();
                }
                else
                {
                    _switchboard.SendTypingMessage();
                }
            }
        }

        /// <summary>
        /// Sends a 'nudge' message to the switchboard, and is received by all participants.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendNudge()
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                throw new InvalidOperationException("Conversation ended.");
            }

            if (Expired)
            {
                ReCreate();
            }


            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                _yimHandler.SendNudge();
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (_leftContacts.Count > 0)
                {
                    MessageEnqueue(new NudgeObject());
                    SwitchBoardReInvite();
                }
                else
                {
                    _switchboard.SendNudge();
                }
            }
        }

        /// <summary>
        /// Sends the definition for a list of emoticons to all other contacts in the conversation. The client-programmer must use this function if a text messages uses multiple emoticons in a single message.
        /// </summary>
        /// <remarks>Use this function before sending text messages which include the emoticon text. You can only send one emoticon message before the textmessage. So make sure that all emoticons used in the textmessage are included.</remarks>
        /// <param name="emoticons">A list of emoticon objects.</param>
        /// <param name="icontype">The type of current emoticons.</param>
        /// <exception cref="InvalidOperationException">Operating on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Sending custom emoticons from a YIM conversation.</exception>
        public void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            _type |= ConversationType.Chat;
            if (Ended)
            {
                throw new InvalidOperationException("Conversation ended.");
            }

            if (Expired)
            {
                ReCreate();
            }

            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                throw new NotSupportedException("YIM conversation not support sending custom emoticons.");
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (_leftContacts.Count > 0)
                {
                    MessageEnqueue(new EmoticonObject(emoticons, icontype));
                    SwitchBoardReInvite();
                }
                else
                {
                    _switchboard.SendEmoticonDefinitions(emoticons, icontype);
                }
            }
        }

        #endregion


    }
};
