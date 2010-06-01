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

        private Dictionary<Guid, Conversation> conversations = new Dictionary<Guid, Conversation>(100);

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
            Conversation conversation = e.Conversation;

            if (!HasConversation(conversation))
            {
                if (!AddConversation(GenerateID(), conversation))
                {
                    throw new MSNPSharpException("AddConversation failed.");
                }
            }
        }

        private void ConversationEnded(object sender, ConversationEndEventArgs e)
        {
            if (!RemoveConversation(GetID(e.Conversation)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Remove conversation from MessageManager failed.", GetType().ToString());
            }
        }


        private void UserTyping(object sender, ContactEventArgs e)
        {
            Guid id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, MessageType.UserTyping));
        }

        private void TextMessageReceived(object sender, TextMessageEventArgs e)
        {
            Guid id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new TextMessageArrivedEventArgs(id, e.Sender, e.Message));
        }

        private void NudgeReceived(object sender, ContactEventArgs e)
        {
            Guid id = ProcessArrivedConversation(sender as Conversation);
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, MessageType.Nudge));
        }

        private void MSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            Guid id = ProcessArrivedConversation(sender as Conversation);
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

        private Guid ProcessArrivedConversation(Conversation conversation)
        {
            Guid returnId = GetID(conversation);
            if (returnId == Guid.Empty)
            {
                throw new MSNPSharpException("Get conversation Id failed.");
            }

            bool isMultipleUser = conversation.IsMultipleUserConversation;
            Conversation originalConversation = GetConversationByDefaultContact(conversation.RemoteOwner, isMultipleUser);
            Guid returnId2 = GetID(originalConversation);

            if (returnId != returnId2)  //The arrived conversation is an overflow conversation.
            {
                //In this case, there is a conversation in manager can logically replace the arrived one.
                //We do not want to let the upper layer programmer know this overflow,
                //so return the earlier conversation.

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Overflow conversation detected: Id1: " + returnId.ToString("B") + ", return id: " + returnId2.ToString("B"));

                return returnId2;
            }

            //If there's no over flow, go ahead.
            return returnId;
        }

        private Guid GetID(Conversation conversation)
        {
            lock (SyncObject)
            {
                foreach (Guid id in conversations.Keys)
                {
                    if (object.ReferenceEquals(conversations[id], conversation))
                    {
                        return id;
                    }
                }
            }

            return Guid.Empty;
        }

        private bool HasConversation(Conversation conversation)
        {
            if (GetID(conversation) == Guid.Empty)
                return false;

            return true;
        }

        /// <summary>
        /// Generate a <see cref="Guid"/> for a specific <see cref="Conversation"/>.
        /// </summary>
        /// <returns></returns>
        private Guid GenerateID()
        {
            Guid uid = Guid.NewGuid();
            int randLimit = 100000;

            lock (SyncObject)
            {
                while (conversations.ContainsKey(uid))
                {
                    uid = Guid.NewGuid();
                    randLimit--;

                    if (randLimit == 0)
                    {
                        throw new MSNPSharpException("Cannot get a new Switchboard ID for this MessageManager.");
                    }
                }

                return uid;
            }
        }

        /// <summary>
        /// Add the specific <see cref="Conversation"/> object into conversatoin list. And listen to its message events (i.e. user typing messages, text messages).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="conversation"></param>
        /// <returns>Return true if added successfully, false if the conversation with the specific id already exists.</returns>
        private bool AddConversation(Guid id, Conversation conversation)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(id)) return false;
                conversations[id] = conversation;
                AttatchEvents(conversation);
                return true;
            }
        }

        private bool RemoveConversation(Guid id)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(id))
                {
                    DetatchEvents(conversations[id]);
                    conversations.Remove(id);
                    return true;
                }
                else
                {
                    return false;
                }
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

        private Conversation GetConversationByDefaultContact(Contact contact, bool isMultipleUserConversation)
        {
            lock (SyncObject)
            {
                foreach (Conversation conversation in conversations.Values)
                {
                    if (conversation.RemoteOwner.IsSibling(contact))
                    {
                        if (conversation.IsMultipleUserConversation == isMultipleUserConversation)
                        {
                            return conversation;
                        }
                    }
                }

                return null;
            }
        }

        //private Guid CreateNewConversation(Contact remoteOwner)
        //{
        //    lock (SyncObject)
        //    {
        //        //Guid id = GenerateID();
        //        //Conversation conversation = new Conversation(Messenger);
        //        //if (AddConversation(id, conversation))
        //        //{
        //        //    conversation.Invite(remoteOwner);
        //        //    return id;
        //        //}

        //        //throw new MSNPSharpException("Create conversation failed.");
        //    }

        //    Conversation conversation = Messenger.CreateConversation();
        //    conversation.Invite(remoteOwner);
        //}

        /// <summary>
        /// Send a message to a remote contact by all means. This method always send the message through a single chat conversation.
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="messageObject"></param>
        /// <returns>The ID of conversation that send this message</returns>
        private Guid SendMessage(Contact contact, MessageObject messageObject)
        {
            CheckMessengerStatus();

            //Verify messenger contact.
            CheckContact(contact, messageObject);

            //Process YIM contact.
            if (contact.ClientType == ClientType.EmailMember)
            {
                return SendYIMMessage(contact, messageObject);
            }

            //Process OIM.
            if (contact.Status == PresenceStatus.Offline)
            {
                Messenger.Nameserver.OIMService.SendOIMMessage(contact, (messageObject as TextMessageObject).InnerObject as TextMessage);
                return Guid.Empty;
            }

            //We always find the single chat conversation. 
            //If it does not exists, we will create a new one even if the multiple chat conversation exists.
            Conversation targetConversation = GetConversationByDefaultContact(contact, false);
            if (targetConversation == null)
            {
                targetConversation = Messenger.CreateConversation();
                targetConversation.Invite(contact);
            }

            SendConversationMessage(targetConversation, messageObject);

            return GetID(targetConversation);
        }

        /// <summary>
        /// Send a message to the spcific conversation.
        /// </summary>
        /// <param name="messageObject"></param>
        /// <param name="conversationID"></param>
        /// <returns>Guid.Empty if conversatio has ended or not exists.</returns>
        private Guid SendMessage(Guid conversationID, MessageObject messageObject)
        {
            //If the mesenger is not signed in, this calling will throw an exception.
            CheckMessengerStatus();

            if (conversationID == Guid.Empty)
            {
                throw new ArgumentException("Invalid conversationID: " + conversationID.ToString("B"));
            }

            Conversation targetConversation = GetConversation(conversationID);

            if (targetConversation != null)  //Send message through exisiting conversations.
            {
                SendConversationMessage(targetConversation, messageObject);
                return GetID(targetConversation);
            }
            else
            {
                return Guid.Empty; //Conversation ended or not exists.
            }

        }

        #region Public methods


        public Conversation GetConversation(Guid id)
        {
            lock (SyncObject)
            {
                if (conversations.ContainsKey(id))
                    return conversations[id];
                return null;
            }
        }

        public Guid SendTyping(Contact contact)
        {
            return SendMessage(contact, new UserTypingObject());
        }

        public Guid SendTyping(Guid conversationID)
        {
            return SendMessage(conversationID, new UserTypingObject());
        }

        public Guid SendNudge(Contact contact)
        {
            return SendMessage(contact, new NudgeObject());
        }

        public Guid SendNudge(Guid conversationID)
        {
            return SendMessage(conversationID, new NudgeObject());
        }

        public Guid SendTextMessage(Contact contact, TextMessage message)
        {
            return SendMessage(contact, new TextMessageObject(message));
        }

        public Guid SendTextMessage(Guid conversationID, TextMessage message)
        {
            return SendMessage(conversationID, new TextMessageObject(message));
        }

        public Guid SendEmoticonDefinitions(Contact contact, List<Emoticon> emoticons, EmoticonType icontype)
        {
            return SendMessage(contact, new EmoticonObject(emoticons, icontype));
        }

        public Guid SendEmoticonDefinitions(Guid conversationID, List<Emoticon> emoticons, EmoticonType icontype)
        {
            return SendMessage(conversationID, new EmoticonObject(emoticons, icontype));
        }

        /// <summary>
        /// Test whether the two single chat conversation is logically equal.
        /// Two logically equal conversations can replace by each other.
        /// </summary>
        /// <param name="activeConversationId"></param>
        /// <param name="expireConversationRemoteOwner">The remote owner of an Ended conversation.</param>
        /// <returns></returns>
        public bool LogicallyEquals(Guid activeConversationId, Contact expireConversationRemoteOwner)
        {
            Conversation activeConversation = GetConversation(activeConversationId);
            if (activeConversation == null)
                return false;

            return activeConversation.RemoteOwner.IsSibling(expireConversationRemoteOwner) && activeConversation.IsMultipleUserConversation == false;
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
