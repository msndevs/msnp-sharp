using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.Utilities
{
    /// <summary>
    /// The type of user messages.
    /// </summary>
    public enum MessageType
    {
        None,
        /// <summary>
        /// A plain text message.
        /// </summary>
        TextMessage,
        /// <summary>
        /// Indicates the remote user is typing.
        /// </summary>
        UserTyping,
        /// <summary>
        /// The nudge message
        /// </summary>
        Nudge,
        /// <summary>
        /// The emoticon data.
        /// </summary>
        Emoticon,
        /// <summary>
        /// The object data.
        /// </summary>
        MSNObject
    }

    public class MessageArrivedEventArgs : EventArgs
    {
        private Guid conversationID = Guid.Empty;

        /// <summary>
        /// The identifier of a <see cref="Conversation"/> in <see cref="MessageManager"/>.
        /// </summary>
        public Guid ConversationID
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

        private MessageType messageType = MessageType.None;

        /// <summary>
        /// The type of message received.
        /// </summary>
        public MessageType MessageType
        {
            get { return messageType; }
        }


        public MessageArrivedEventArgs(Guid conversationId, Contact sender, MessageType type)
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


        public TextMessageArrivedEventArgs(Guid conversationId, Contact sender, TextMessage textMessage)
            :base(conversationId,sender, MessageType.TextMessage)
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

        public EmoticonArrivedEventArgs(Guid conversationId, Contact sender, Emoticon emoticon)
            : base(conversationId, sender, MessageType.Emoticon)
        {
            this.emoticon = emoticon;
        }

    }
}
