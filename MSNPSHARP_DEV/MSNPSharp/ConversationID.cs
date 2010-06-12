
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.Utilities
{
    public class ConversationID
    {
        private Conversation conversation = null;
        private Contact remoteOwner = null;

        private string hashString = string.Empty;
        private string compareString = string.Empty;
        private int hashCode = 0;

        public Contact RemoteOwner
        {
            get { return remoteOwner; }
        }

        private object syncObject = new object();

        internal void SetConversation(Conversation conv)
        {
            conversation = conv;
            if (conversation != null)
            {
                if (conversation.RemoteOwner == null)
                    throw new ArgumentException("Invailid conversation.");
                remoteOwner = conversation.RemoteOwner;
            }
        }

        public ClientType NetworkType
        {
            get
            {
                if (!remoteOwner.IsMessengerUser)
                    return ClientType.None;

                return remoteOwner.ClientType;
            }
        }

        public ConversationID(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException("conversation is null.");

            SetConversation(conversation);

            hashString = ToString();
            hashCode = hashString.GetHashCode();


            conversation.ConversationEnded += delegate
            {
                lock (syncObject)
                {
                    SetConversation(null);
                }
            };

            conversation.RemoteOwnerChanged += delegate(object sender, ConversationRemoteOwnerChangedEventArgs args)
            {
                lock (syncObject)
                {
                    try
                    {
                        SetConversation(conversation);
                    }
                    catch (Exception)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Remote owner changed to null.");
                    }
                }
            };
        }

        public ConversationID(Contact remote)
        {
            if (remote == null)
                throw new ArgumentNullException("remote is null.");

            remoteOwner = remote;
            hashString = ToString();
            hashCode = hashString.GetHashCode();
        }

        public static bool operator ==(ConversationID id1, object other)
        {
            if (((object)id1 == null) && other == null)
                return true;

            if (((object)id1) == null || other == null)
                return false;

            if (other is ConversationID)
            {
                return id1.Equals(other);
            }

            if (other is Conversation)
            {
                if (id1.conversation == null) return false;
                return object.ReferenceEquals(id1.conversation, other);
            }


            return false;
        }

        public static bool operator !=(ConversationID id1, object other)
        {
            return !(id1 == other);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return false;

            if (!(obj is ConversationID))
                return false;

            return ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            string remoteOwnerString = (remoteOwner == null ? "null" : remoteOwner.Mail.ToLowerInvariant());
            string conversationString = string.Empty;
            if (conversation.IsMultipleUserConversation)
            {
                conversationString = conversation.Switchboard.SessionHash;
            }
            return string.Join(";", new string[] { NetworkType.ToString().ToLowerInvariant(), remoteOwnerString, conversationString });
        }
    }
}
