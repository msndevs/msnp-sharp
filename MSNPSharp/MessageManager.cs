using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.Utilities
{
    public class MessageManager : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a user message arrived. Use MessageType property to determine what kind of message it is.
        /// </summary>
        public event EventHandler<MessageArrivedEventArgs> MessageArrived;

        #endregion

        #region Fields and Properties

        private Dictionary<ConversationID, Conversation> conversations = new Dictionary<ConversationID, Conversation>(100, new ConversationIDComparer());
        private Dictionary<ConversationID, Contact> pendingConversations = new Dictionary<ConversationID, Contact>(100, new ConversationIDComparer());

        private Messenger messenger = null;

        /// <summary>
        /// The <see cref="Messenger"/> instance this manager connected to.
        /// </summary>
        public Messenger Messenger
        {
            get { return messenger; }
        }

        private object syncObject = new object();

        protected object SyncObject
        {
            get { return syncObject; }
        }


        #endregion

        #region .ctor

        private MessageManager()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messenger">The <see cref="Messenger"/> instance this manager connected to.</param>
        public MessageManager(Messenger messenger)
        {
            this.messenger = messenger;
            Messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(ConversationCreated);
        }

        #endregion

        #region Event handlers

        private void ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            AttatchEvents(e.Conversation);
        }

        private void ConversationEnded(object sender, ConversationEndEventArgs e)
        {
            DetatchEvents(e.Conversation);
            RemoveConversation(e.Conversation);
        }


        private void UserTyping(object sender, ContactEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, MessageType.UserTyping));
        }

        private void TextMessageReceived(object sender, TextMessageEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new TextMessageArrivedEventArgs(id, e.Sender, e.Message));
        }

        private void NudgeReceived(object sender, ContactEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, MessageType.Nudge));
        }

        private void MSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(sender as Conversation);
            if (e.ClientData is Emoticon)
            {
                Emoticon emoticon = e.ClientData as Emoticon;
                OnMessageArrived(new EmoticonArrivedEventArgs(id, e.RemoteContact, emoticon));
            }
        }

        #endregion

        protected virtual void OnMessageArrived(MessageArrivedEventArgs e)
        {
            if (MessageArrived != null)
                MessageArrived(this, e);
        }

        private ConversationID ProcessArrivedConversation(Conversation conversation)
        {
            lock (SyncObject)
            {
                ConversationID id = new ConversationID(conversation);
                bool created = HasConversation(id);
                bool pending = IsPendingConversation(id);
                if (pending)
                {
                    if (!created)
                    {
                        AddConversation(id, conversation);  //We fix this bug.
                    }

                    if (created)
                    {
                        //What happends?!
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[ProcessArrivedConversation Error]: A conversation is both in pending and created status.");
                        RemovePendingConversation(id);
                    }
                }

                if (!pending)
                {
                    if (!created)
                    {
                        AddConversation(id, conversation);
                    }
                }

                return id;
            }
        }

        private bool HasConversation(ConversationID cId)
        {
            lock (syncObject)
                return conversations.ContainsKey(cId);
        }

        private bool IsPendingConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                return pendingConversations.ContainsKey(cId);
            }
        }

        /// <summary>
        /// Add the specific <see cref="Conversation"/> object into conversatoin list. And listen to its message events (i.e. user typing messages, text messages).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="conversation"></param>
        /// <returns>Return true if added successfully, false if the conversation with the specific id already exists.</returns>
        private bool AddConversation(ConversationID id, Conversation conversation)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(id)) 
                    return false;
                conversations[id] = conversation;
                return true;
            }
        }

        private void AddPending(ConversationID cId, Contact remoteOwner)
        {
            lock (SyncObject)
            {
                pendingConversations[cId] = remoteOwner;
            }
        }

        private bool RemovePendingConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (IsPendingConversation(cId))
                {
                    pendingConversations.Remove(cId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool RemoveConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(cId))
                {
                    conversations.Remove(cId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool RemoveConversation(Conversation conversation)
        {
            lock (SyncObject)
            {
                Dictionary<ConversationID, Conversation> cp = new Dictionary<ConversationID, Conversation>(conversations, new ConversationIDComparer());
                foreach (ConversationID id in cp.Keys)
                {
                    if (object.ReferenceEquals(cp[id], conversation))
                    {
                        conversations.Remove(id);
                        return true;
                    }
                }

                return false;
            }
        }

        private void DetatchEvents(Conversation conversation)
        {
            if (conversation != null)
            {
                conversation.TextMessageReceived -= TextMessageReceived;
                conversation.NudgeReceived -= NudgeReceived;
                conversation.UserTyping -= UserTyping;
                conversation.ConversationEnded -= ConversationEnded;
                conversation.MSNObjectDataTransferCompleted -= MSNObjectDataTransferCompleted;
            }
        }

        private void AttatchEvents(Conversation conversation)
        {

            DetatchEvents(conversation);

            conversation.TextMessageReceived += new EventHandler<TextMessageEventArgs>(TextMessageReceived);
            conversation.NudgeReceived += new EventHandler<ContactEventArgs>(NudgeReceived);
            conversation.UserTyping += new EventHandler<ContactEventArgs>(UserTyping);
            conversation.ConversationEnded += new EventHandler<ConversationEndEventArgs>(ConversationEnded);
            conversation.MSNObjectDataTransferCompleted += new EventHandler<MSNObjectDataTransferCompletedEventArgs>(MSNObjectDataTransferCompleted);
        }


        /// <summary>
        /// Test whether the <see cref="Messenger"/> conntected to this manager is still in signed in status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Messenger not sign in.</exception>
        private void CheckMessengerStatus()
        {
            if (!Messenger.Nameserver.IsSignedIn)
            {
                throw new InvalidOperationException("Messenger not sign in. Please sign in first.");
            }
        }

        /// <summary>
        /// Test whether a contact can be the receiver of a user message.
        /// </summary>
        /// <param name="messengerContact"></param>
        /// <param name="messageObject"></param>
        /// <exception cref="MSNPSharpException">The target <see cref="Contact"/> is not a messenger contact.</exception>
        /// <exception cref="NotSupportedException">The message is not compatible to the specific contact.</exception>
        private void CheckContact(Contact messengerContact, MessageObject messageObject)
        {
            if (!messengerContact.IsMessengerUser)
            {
                throw new MSNPSharpException("This is not a MSN contact.");
            }

            if (messengerContact.ClientType == ClientType.EmailMember && (messageObject is EmoticonObject))
            {
                throw new NotSupportedException("A Yahoo Messenger contact cannot receive custom emoticon.");
            }

            if (messengerContact.Status == PresenceStatus.Offline && (messageObject is TextMessageObject) == false)
            {
                throw new NotSupportedException("The specific message cannot send to an offline contact: " + messengerContact);
            }
        }

        /// <summary>
        /// Send a cross network message to Yahoo! Messenger network
        /// </summary>
        /// <param name="yimContact"></param>
        /// <param name="messageObject"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throw when sending a custom emoticon to a Yahoo Messenger contact.</exception>
        private Guid SendYIMMessage(Contact yimContact, MessageObject messageObject)
        {
            if (yimContact.IsMessengerUser && yimContact.ClientType == ClientType.EmailMember)
            {
                if (messageObject is EmoticonObject)
                {
                    throw new InvalidOperationException("Cannot send custom emoticon to a Email messenger contact.");
                }

                try
                {

                    if (messageObject is NudgeObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, NetworkMessageType.Nudge);
                    }

                    if (messageObject is UserTypingObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, NetworkMessageType.Typing);
                    }

                    if (messageObject is TextMessageObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, messageObject.InnerObject as TextMessage);
                    }

                }

                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot send message to email contact: " + yimContact);
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Send the message through the specific <see cref="Conversation"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="messageObject"></param>
        /// <exception cref="InvalidOperationException">Thrown when a conversation is already ended.</exception>
        private void SendConversationMessage(Conversation conversation, MessageObject messageObject)
        {
            if (conversation.Ended)
                throw new InvalidOperationException("Cannot send a message through an ended conversation.");

            try
            {
                if (messageObject is NudgeObject)
                {
                    conversation.SendNudge();
                }

                if (messageObject is UserTypingObject)
                {
                    conversation.SendTypingMessage();
                }

                if (messageObject is TextMessageObject)
                {
                    conversation.SendTextMessage(messageObject.InnerObject as TextMessage);
                }

                if (messageObject is EmoticonObject)
                {
                    conversation.SendEmoticonDefinitions(messageObject.InnerObject as List<Emoticon>, (messageObject as EmoticonObject).Type);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Send a message to a remote contact by all means. This method always send the message through a single chat conversation.
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="messageObject"></param>
        /// <returns>The ID of conversation that send this message</returns>
        private void SendMessage(Contact contact, MessageObject messageObject)
        {
            CheckMessengerStatus();

            //Verify messenger contact.
            CheckContact(contact, messageObject);

            //Process YIM contact.
            if (contact.ClientType == ClientType.EmailMember)
            {
                SendYIMMessage(contact, messageObject);
                return;
            }

            //Process OIM.
            if (contact.Status == PresenceStatus.Offline)
            {
                Messenger.Nameserver.OIMService.SendOIMMessage(contact, (messageObject as TextMessageObject).InnerObject as TextMessage);
                return;
            }
        }

        /// <summary>
        /// Send a message to the spcific conversation.
        /// </summary>
        /// <param name="cId"></param>
        /// <param name="messageObject"></param>
        /// <returns>Guid.Empty if conversatio has ended or not exists.</returns>
        private ConversationID SendMessage(ConversationID cId, MessageObject messageObject)
        {
            if (cId == null)
            {
                throw new ArgumentNullException("cId is null.");
            }

            //If the mesenger is not signed in, this calling will throw an exception.
            CheckMessengerStatus();

            lock (SyncObject)
            {
                bool created = HasConversation(cId);
                bool pending = IsPendingConversation(cId);
                if (cId.NetworkType != ClientType.EmailMember)
                {
                    if (cId.RemoteOwner.Status != PresenceStatus.Offline)
                    {
                        if ((!pending) && created)  //Send message through exisiting conversations.
                        {
                            SendConversationMessage(GetConversation(cId), messageObject);
                        }
                        else
                        {

                            //In the following case, the conversation object is not actually created.
                            //However, if the message is user typing, we just do nothing.
                            if (!(messageObject is UserTypingObject))
                            {
                                cId = CreateNewConversation(cId);
                                CheckContact(cId.RemoteOwner, messageObject);
                                SendConversationMessage(cId.Conversation, messageObject);
                            }
                        }
                    }
                    else
                    {
                        RemovePendingConversation(cId);
                        //Verify messenger contact.
                        CheckContact(cId.RemoteOwner, messageObject);
                        SendMessage(cId.RemoteOwner, messageObject);

                    }
                }

                if (cId.NetworkType == ClientType.EmailMember) //Yahoo!
                {
                    CheckContact(cId.RemoteOwner, messageObject);
                    SendMessage(cId.RemoteOwner, messageObject);
                    RemovePendingConversation(cId);
                }
            }
            return cId;

        }

        private ConversationID CreateNewConversation(ConversationID pendingId)
        {
            bool created = HasConversation(pendingId);
            bool pending = IsPendingConversation(pendingId);
            bool otherNetwork = (pendingId.RemoteOwner.ClientType != ClientType.PassportMember);

            if (pending)
                RemovePendingConversation(pendingId);
            if (created || otherNetwork)
                return pendingId;

            pendingId.SetConversation(Messenger.CreateConversation());
            AddConversation(pendingId, pendingId.Conversation);
            pendingId.Conversation.Invite(pendingId.RemoteOwner);

            return pendingId;

        }

        #region Public methods

        /// <summary>
        /// Get the corresponding conversation from conversation Id.
        /// </summary>
        /// <param name="cId"></param>
        /// <returns>A conversation object will returned if found, null otherwise.</returns>
        public Conversation GetConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(cId))
                    return conversations[cId];
                return null;
            }
        }

        public ConversationID SendTyping(ConversationID conversationID)
        {
            return SendMessage(conversationID, new UserTypingObject());
        }

        public ConversationID SendNudge(ConversationID conversationID)
        {
            return SendMessage(conversationID, new NudgeObject());
        }


        public ConversationID SendTextMessage(ConversationID conversationID, TextMessage message)
        {
            return SendMessage(conversationID, new TextMessageObject(message));
        }


        public ConversationID SendEmoticonDefinitions(ConversationID conversationID, List<Emoticon> emoticons, EmoticonType icontype)
        {
            return SendMessage(conversationID, new EmoticonObject(emoticons, icontype));
        }

        public ConversationID GetID(Contact remoteOwner)
        {
            lock (SyncObject)
            {
                ConversationID id = new ConversationID(remoteOwner);
                bool created = HasConversation(id);
                bool pending = IsPendingConversation(id);
                if (created || pending)
                    return id;

                AddPending(id, remoteOwner);
                return id;
            }
        }

        /// <summary>
        /// Invite another user to a conversation.
        /// </summary>
        /// <param name="conversationID"></param>
        /// <param name="remoteContact"></param>
        /// <returns>The updated conversation Id.</returns>
        /// <exception cref="InvalidOperationException">The remote contact is not a <see cref="ClientType.PassportMember"/></exception>
        public ConversationID InviteContactToConversation(ConversationID conversationID, Contact remoteContact)
        {
            ConversationID cId = conversationID;
            if (remoteContact.IsSibling(cId.RemoteOwner))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connot invite the remote owner into this conversation again.");
                return cId;
            }

            if (remoteContact.ClientType != ClientType.PassportMember)
                throw new InvalidOperationException("The remoteContact: " + remoteContact + " is not a PassportMember.");

            Conversation activeConversation = null;
            if (HasConversation(cId))
            {
                activeConversation = GetConversation(cId);
            }
            else
            {
                //The conversation object not exist, we need to create one first.
                //Then invite the remote owner to the newly created conversation.
                cId = CreateNewConversation(cId);
                activeConversation = cId.Conversation;
            }

            if (activeConversation.Ended)  //If conversation exists, but ended.
            {
                //We dump the old conversation and start the whole process again.
                RemoveConversation(activeConversation);
                RemovePendingConversation(cId);
                return InviteContactToConversation(cId, remoteContact);
            }

            activeConversation.Invite(remoteContact);


            return cId;
        }

        /// <summary>
        /// Invite another user to a conversation.
        /// </summary>
        /// <param name="conversationID"></param>
        /// <param name="contacts"></param>
        /// <returns>The updated conversation Id.</returns>
        /// <exception cref="InvalidOperationException">The remote contact is not a <see cref="ClientType.PassportMember"/></exception>
        public ConversationID InviteContactToConversation(ConversationID conversationID, Contact[] contacts)
        {
            ConversationID cId = conversationID;
            foreach (Contact contact in contacts)
            {
                cId = InviteContactToConversation(cId, contact);
            }

            return cId;
        }

        /// <summary>
        /// End the specific conversation and release the resources it used.
        /// </summary>
        /// <param name="conversationId"></param>
        public void EndConversation(ConversationID conversationId)
        {
            ConversationID cId = conversationId;
            bool created = HasConversation(cId);
            bool pending = IsPendingConversation(cId);

            if (pending)
                RemovePendingConversation(cId);

            if (created)
            {
                if (cId.NetworkType == ClientType.PassportMember)
                {
                    Conversation activeConversation = GetConversation(cId);
                    if (!object.ReferenceEquals(cId.Conversation, activeConversation))
                    {
                        //We end as much conversation as possible.
                        if (cId.Conversation != null)
                            cId.Conversation.End();
                    }
                    if (activeConversation != null)
                        activeConversation.End();

                    //We do not process any overflow conversation.
                    //If these overflow conversation have received messages, new conversation id will be created and added into conversation id dictionary.
                    //else just let them ended and the event habdler attached will be removed.
                }
                RemoveConversation(cId);
            }
        }

        #endregion

        #region IDisposable ≥…‘±

        public void Dispose()
        {
            if (Messenger != null)
            {
                Messenger.ConversationCreated -= ConversationCreated;
            }

        }

        #endregion
    }
}
