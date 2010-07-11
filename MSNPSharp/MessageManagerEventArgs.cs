using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.Utilities
{

    public class MessageArrivedEventArgs : EventArgs
    {
        private ConversationID conversationID = null;

        /// <summary>
        /// The identifier of a <see cref="Conversation"/> in <see cref="MessageManager"/>.
        /// </summary>
        public ConversationID ConversationID
        {
            get { return conversationID; }
        }

        private Contact sender = null;

        /// <summary>
        /// The sender of message.
        /// </summary>
        public Contact Sender
        {
            get { return sender; }
        }

        private NetworkMessageType messageType = NetworkMessageType.None;

        /// <summary>
        /// The type of message received.
        /// </summary>
        public NetworkMessageType MessageType
        {
            get { return messageType; }
        }


        public MessageArrivedEventArgs(ConversationID conversationId, Contact sender, NetworkMessageType type)
        {
            conversationID = conversationId;
            this.sender = sender;
            messageType = type;
        }
    }

    public class TextMessageArrivedEventArgs : MessageArrivedEventArgs
    {
        private TextMessage textMessage = null;

        /// <summary>
        /// The text message received.
        /// </summary>
        public TextMessage TextMessage
        {
            get { return textMessage; }
        }


        public TextMessageArrivedEventArgs(ConversationID conversationId, Contact sender, TextMessage textMessage)
            : base(conversationId, sender, NetworkMessageType.Text)
        {
            this.textMessage = textMessage;
        }
    }

    public class EmoticonArrivedEventArgs : MessageArrivedEventArgs
    {
        private Emoticon emoticon = null;

        /// <summary>
        /// The emoicon data received.
        /// </summary>
        public Emoticon Emoticon
        {
            get { return emoticon; }
        }

        public EmoticonArrivedEventArgs(ConversationID conversationId, Contact sender, Emoticon emoticon)
            : base(conversationId, sender, NetworkMessageType.Emoticon)
        {
            this.emoticon = emoticon;
        }

    }
}
