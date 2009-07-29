#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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

namespace MSNPSharp.DataTransfer
{
    using System;
    using System.IO;
    using MSNPSharp.Core;
    using MSNPSharp;

    /// <summary>
    /// Defines the type of P2P message.
    /// </summary>
    [Flags]
    public enum P2PFlag : uint
    {
        /// <summary>
        /// Normal (protocol) message.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Negative Ack
        /// </summary>
        NegativeAck = 0x1,
        /// <summary>
        /// Acknowledgement message.
        /// </summary>
        Acknowledgement = 0x2,
        /// <summary>
        /// Waiting
        /// </summary>
        Waiting = 0x4,
        /// <summary>
        /// Messages notifies a binary error.
        /// </summary>
        Error = 0x8,
        /// <summary>
        /// File
        /// </summary>
        File = 0x10,
        /// <summary>
        /// Messages defines a msn object.
        /// </summary>
        Data = 0x20,
        /// <summary>
        /// Close session
        /// </summary>
        CloseSession = 0x40,
        /// <summary>
        /// Tlp error
        /// </summary>
        TlpError = 0x80,
        /// <summary>
        /// Direct handshake
        /// </summary>
        DirectHandshake = 0x100,
        /// <summary>
        /// Messages for info data, such as INVITE, 200 OK, 500 INTERNAL ERROR
        /// </summary>
        MSNSLPInfo = 0x01000000,
        /// <summary>
        /// Messages defines data for a filetransfer.
        /// </summary>
        FileData = MSNSLPInfo | P2PFlag.Data | P2PFlag.File,
        /// <summary>
        /// Messages defines data for a MSNObject transfer.
        /// </summary>
        MSNObjectData = MSNSLPInfo | P2PFlag.Data
    }

    internal static class P2PConst
    {
        /// <summary>
        /// The guid used in invitations for a filetransfer.
        /// </summary>
        public const string FileTransferGuid = "{5D3E02AB-6190-11D3-BBBB-00C04F795683}";

        /// <summary>
        /// The guid used in invitations for a user display transfer.
        /// </summary>
        public const string UserDisplayGuid = "{A4268EEC-FEC5-49E5-95C3-F126696BDBF6}";

        /// <summary>
        /// The guid used in invitations for a share photo.
        /// </summary>
        public const string SharePhotoGuid = "{41D3E74E-04A2-4B37-96F8-08ACDB610874}";

        /// <summary>
        /// The guid used in invitations for an activity.
        /// </summary>
        public const string ActivityGuid = "{6A13AF9C-5308-4F35-923A-67E8DDA40C2F}";

        /// <summary>
        /// Footer for a msn DisplayImage p2pMessage.
        /// </summary>
        public const uint DisplayImageFooter12 = 12;

        /// <summary>
        /// Footer for a filetransfer p2pMessage.
        /// </summary>
        public const uint FileTransFooter2 = 2;

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        public const uint CustomEmoticonFooter11 = 11;

        /// <summary>
        /// Footer for a msn object p2pMessage.
        /// </summary>
        public const uint DisplayImageFooter1 = 1;

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        public const uint CustomEmoticonFooter1 = 1;
    }

    /// <summary>
    /// Represents a single P2P framework message.
    /// </summary>
    [Serializable()]
    public class P2PMessage : NetworkMessage
    {
        private uint sessionId;
        private uint identifier;
        private ulong offset;
        private ulong totalSize;
        private uint messageSize;
        private P2PFlag flags;
        private uint ackSessionId;
        private uint ackIdentifier;
        private ulong ackTotalSize;
        private uint footer;

        /// <summary>
        /// The session identifier field. Bytes 0-3 in the binary header.
        /// </summary>
        public uint SessionId
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
        /// The identifier of this message. Bytes 5-8 in the binary header.
        /// </summary>
        public uint Identifier
        {
            get
            {
                return identifier;
            }
            set
            {
                identifier = value;
            }
        }

        /// <summary>
        /// The offset in bytes from the begin of the total message. Bytes 9-16 in the binary header.
        /// </summary>
        public ulong Offset
        {
            get
            {
                return offset;
            }
            set
            {
                offset = value;
            }
        }

        /// <summary>
        /// Total message length in bytes.  Bytes 17-24 in the binary header.
        /// </summary>
        public ulong TotalSize
        {
            get
            {
                return totalSize;
            }
            set
            {
                totalSize = value;
            }
        }

        /// <summary>
        /// Message length in bytes of the current message. Bytes 25-28 in the binary header.
        /// </summary>
        public uint MessageSize
        {
            get
            {
                return messageSize;
            }
            set
            {
                messageSize = value;
            }
        }

        /// <summary>
        /// Flag parameter. Bytes 29-32 in the binary header.
        /// </summary>
        public P2PFlag Flags
        {
            get
            {
                return flags;
            }
            set
            {
                flags = value;
            }
        }

        /// <summary>
        /// Acknowledge session identifier. Acknowledgement messages respond with this number in their acknowledge identfier. Bytes 33-36 in the binary header.
        /// </summary>
        public uint AckSessionId
        {
            get
            {
                return ackSessionId;
            }
            set
            {
                ackSessionId = value;
            }
        }

        /// <summary>
        /// Acknowledge identifier. Set when the message is an acknowledgement to a received message. Bytes 37-40 in the binary header.
        /// </summary>
        public uint AckIdentifier
        {
            get
            {
                return ackIdentifier;
            }
            set
            {
                ackIdentifier = value;
            }
        }

        /// <summary>
        /// Acknowledged total message length. Set when the message is an acknowledgement to a received message. Bytes 41-48 in the binary header.
        /// </summary>
        public ulong AckTotalSize
        {
            get
            {
                return ackTotalSize;
            }
            set
            {
                ackTotalSize = value;
            }
        }

        /// <summary>
        /// The footer, or Application Identifier. Bytes 0-3 in the binary footer (BIG ENDIAN).
        /// </summary>
        public uint Footer
        {
            get
            {
                return footer;
            }
            set
            {
                footer = value;
            }
        }

        /// <summary>
        /// Indicates whether the message is an acknowledgement message
        /// </summary>
        public bool IsAcknowledgement
        {
            get
            {
                return AckIdentifier > 0;
            }
        }

        public P2PMessage()
        {
        }

        /// <summary>
        /// Creates an acknowledgement message to this message.
        /// </summary>
        /// <returns></returns>
        public virtual P2PMessage CreateAcknowledgement()
        {
            P2PMessage ack = new P2PMessage();
            ack.TotalSize = (ulong)0;  //ACK dose NOT have totalsize
            ack.Flags = P2PFlag.Acknowledgement;
            ack.AckSessionId = Identifier;
            ack.AckIdentifier = AckSessionId;
            ack.AckTotalSize = TotalSize;
            return ack;
        }

        /// <summary>
        /// Returns debug info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", (uint)Flags, Convert.ToString(Flags)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckSessionId  : {1:x} ({0})\r\n", AckSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), AckSessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckIdentifier : {1:x} ({0})\r\n", AckIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture), AckIdentifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckTotalSize  : {1:x} ({1})\r\n", AckTotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), AckTotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({0})\r\n", (uint)Footer, Convert.ToString(Footer));
            return "[P2PMessage]\r\n" + debugLine;
        }

        #region Protected helper methods

        protected internal static uint ToLittleEndian(uint val)
        {
            if (BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        protected internal static ulong ToLittleEndian(ulong val)
        {
            if (BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        protected internal static uint ToBigEndian(uint val)
        {
            if (!BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        protected internal static ulong ToBigEndian(ulong val)
        {
            if (!BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        protected internal static uint FlipEndian(uint val)
        {
            return (uint)
                 (((val & 0x000000ff) << 24) +
                 ((val & 0x0000ff00) << 8) +
                 ((val & 0x00ff0000) >> 8) +
                 ((val & 0xff000000) >> 24));
        }

        protected internal static ulong FlipEndian(ulong val)
        {
            return (ulong)
                 (((val & 0x00000000000000ff) << 56) +
                 ((val & 0x000000000000ff00) << 40) +
                 ((val & 0x0000000000ff0000) << 24) +
                 ((val & 0x00000000ff000000) << 8) +
                 ((val & 0x000000ff00000000) >> 8) +
                 ((val & 0x0000ff0000000000) >> 24) +
                 ((val & 0x00ff000000000000) >> 40) +
                 ((val & 0xff00000000000000) >> 56));
        }
        #endregion

        /// <summary>
        /// Sets the D as acknowledgement in the ParentMessage.ParentMessage. This should be a SBMessage object.
        /// </summary>
        public override void PrepareMessage()
        {
            base.PrepareMessage();
            if (ParentMessage != null && ParentMessage is MSGMessage)
                if (ParentMessage.ParentMessage != null && ParentMessage.ParentMessage is SBMessage)
                    ((SBMessage)ParentMessage.ParentMessage).Acknowledgement = "D";
        }

        /// <summary>
        /// Parses the given message.
        /// </summary>
        public override void ParseBytes(byte[] data)
        {
            Stream memStream = new System.IO.MemoryStream(data);
            BinaryReader reader = new System.IO.BinaryReader(memStream);

            SessionId = ToLittleEndian(reader.ReadUInt32());
            Identifier = ToLittleEndian(reader.ReadUInt32());
            Offset = ToLittleEndian(reader.ReadUInt64());
            TotalSize = ToLittleEndian(reader.ReadUInt64());
            MessageSize = ToLittleEndian(reader.ReadUInt32());
            Flags = (P2PFlag)ToLittleEndian(reader.ReadUInt32());
            AckSessionId = ToLittleEndian(reader.ReadUInt32());
            AckIdentifier = ToLittleEndian(reader.ReadUInt32());
            AckTotalSize = ToLittleEndian(reader.ReadUInt64());

            // now move to the footer while reading the message contents
            InnerBody = new byte[MessageSize];
            memStream.Read(InnerBody, 0, (int)MessageSize);

            // this is big-endian
            if (data.Length > 48 + MessageSize)
                Footer = (uint)ToBigEndian(reader.ReadUInt32());

            // clean up
            reader.Close();
            memStream.Close();
        }

        /// <summary>
        /// Returns the inner message as a byte array.
        /// </summary>
        /// <remarks>If the inner message is set the GetBytes() method is called upon that inner message. If there is no inner message set, but the InnerBody property contains data then that data is returned.</remarks>
        /// <returns></returns>
        protected virtual byte[] GetInnerBytes()
        {
            // if there is a message we contain get the contents
            if (InnerMessage != null)
                return InnerMessage.GetBytes();
            else if (InnerBody != null)
                return InnerBody;
            else
                return new byte[0];
        }

        /// <summary>
        /// Creates a P2P Message. This sets the MessageSize properly.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            // get the inner contents and set the message size
            byte[] innerBytes = GetInnerBytes();
            MessageSize = (uint)innerBytes.Length;

            // if no total size is specified, then we assume this is the whole message.
            if (TotalSize == 0)
                TotalSize = MessageSize;

            // total size is header (48) + footer (4) + messagesize
            byte[] p2pMessage = new byte[52 + MessageSize];

            Stream memStream = new MemoryStream(p2pMessage, true);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(ToLittleEndian(SessionId));
            writer.Write(ToLittleEndian(Identifier));
            writer.Write(ToLittleEndian(Offset));
            writer.Write(ToLittleEndian(TotalSize));
            writer.Write(ToLittleEndian(MessageSize));
            writer.Write(ToLittleEndian((uint)Flags));
            writer.Write(ToLittleEndian(AckSessionId));
            writer.Write(ToLittleEndian(AckIdentifier));
            writer.Write(ToLittleEndian(AckTotalSize));
            writer.Write(innerBytes);

            writer.Write(ToBigEndian((uint)Footer));

            // clean up
            writer.Close();
            memStream.Close();

            // return the total message
            return p2pMessage;
        }
    }




    /// <summary>
    /// Represents a single P2PDataMessage which is used for the actual data transfer. No negotiation handling.
    /// </summary>
    /// <remarks>
    /// A p2p data message can be identified by looking at the footer in the P2P Message. When this value is > 0 a
    /// data message is send. When this value is 0 a normal, and more complex, MSNSLPMessage is send.
    /// This class is created to provide a fast way of sending messages.
    /// </remarks>
    [Serializable()]
    public class P2PDataMessage : P2PMessage
    {
        /// <summary>
        /// Constructs a P2P data message.
        /// </summary>
        public P2PDataMessage()
        {
            Footer = 0;
        }

        /// <summary>
        /// Writes 4 nul-bytes in the inner body. This message can then be used as a data preparation message.
        /// </summary>
        public void WritePreparationBytes()
        {
            InnerBody = new byte[4] { 0, 0, 0, 0 };
        }

        /// <summary>
        /// Writes data in the inner message buffer.
        /// </summary>
        /// <param name="ioStream"></param>
        /// <param name="maxLength"></param>
        public int WriteBytes(Stream ioStream, int maxLength)
        {
            long readLength = Math.Min(maxLength, ioStream.Length - ioStream.Position);

            InnerBody = new byte[readLength];
            return ioStream.Read(InnerBody, 0, (int)readLength);
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", (uint)Flags, Convert.ToString(Flags)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckSessionId  : {1:x} ({0})\r\n", AckSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), AckSessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckIdentifier : {1:x} ({0})\r\n", AckIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture), AckIdentifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckTotalSize  : {1:x} ({1})\r\n", AckTotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), AckTotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({0})\r\n", (uint)Footer, Convert.ToString(Footer));
            return "[P2PDataMessage]\r\n" + debugLine;
        }

    }


    /// <summary>
    /// A P2P Message which is send in a direct-connection.
    /// </summary>
    /// <remarks>The innerbody contents are used as message contents (data). The InnerMessage object is ignored.</remarks>
    [Serializable()]
    public class P2PDCMessage : P2PDataMessage
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", (uint)Flags, Convert.ToString(Flags)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckSessionId	: {1:x} ({0})\r\n", AckSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), AckSessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckIdentifier : {1:x} ({0})\r\n", AckIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture), AckIdentifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckTotalSize  : {1:x} ({1})\r\n", AckTotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), AckTotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer        : {1:x} ({0})\r\n", (uint)Footer, Convert.ToString(Footer));
            return "[P2PDCMessage]\r\n" + debugLine;
        }


        /// <summary>
        /// Basic constructor.
        /// </summary>
        public P2PDCMessage()
        {
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCMessage(P2PMessage message)
        {
            SessionId = message.SessionId;
            Identifier = message.Identifier;
            Offset = message.Offset;
            TotalSize = message.TotalSize;
            MessageSize = message.MessageSize;
            Flags = message.Flags;
            AckSessionId = message.AckSessionId;
            AckIdentifier = message.AckIdentifier;
            AckTotalSize = message.AckTotalSize;
            InnerMessage = message.InnerMessage;
            InnerBody = message.InnerBody;
        }

        /// <summary>
        /// Writes no footer, but a 4 byte length size in front of the header.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            // get the inner contents and set the message size
            byte[] innerBytes = GetInnerBytes();

            MessageSize = (uint)innerBytes.Length;

            // if no total size is specified, then we assume this is the whole message.
            if (TotalSize == 0)
                TotalSize = MessageSize;

            // total size is size(4) + header (48) + messagesize
            byte[] p2pMessage = new byte[52 + MessageSize];

            Stream memStream = new System.IO.MemoryStream(p2pMessage, true);
            BinaryWriter writer = new System.IO.BinaryWriter(memStream);

            writer.Write(ToLittleEndian((uint)(48 + MessageSize)));
            writer.Write(ToLittleEndian(SessionId));
            writer.Write(ToLittleEndian(Identifier));
            writer.Write(ToLittleEndian(Offset));
            writer.Write(ToLittleEndian(TotalSize));
            writer.Write(ToLittleEndian(MessageSize));
            writer.Write(ToLittleEndian((uint)Flags));
            writer.Write(ToLittleEndian(AckSessionId));
            writer.Write(ToLittleEndian(AckIdentifier));
            writer.Write(ToLittleEndian(AckTotalSize));

            writer.Write(innerBytes);

            // clean up
            writer.Close();
            memStream.Close();

            // return the total message
            return p2pMessage;
        }


        /// <summary>
        /// Parses a data message without the 4-byte length header and without a 4 byte footer.
        /// </summary>
        /// <param name="data"></param>
        public override void ParseBytes(byte[] data)
        {
            Stream memStream = new System.IO.MemoryStream(data);
            BinaryReader reader = new System.IO.BinaryReader(memStream);

            SessionId = ToLittleEndian(reader.ReadUInt32());
            Identifier = ToLittleEndian(reader.ReadUInt32());
            Offset = ToLittleEndian(reader.ReadUInt64());
            TotalSize = ToLittleEndian(reader.ReadUInt64());
            MessageSize = ToLittleEndian(reader.ReadUInt32());
            Flags = (P2PFlag)ToLittleEndian(reader.ReadUInt32());
            AckSessionId = ToLittleEndian(reader.ReadUInt32());
            AckIdentifier = ToLittleEndian(reader.ReadUInt32());
            AckTotalSize = ToLittleEndian(reader.ReadUInt64());

            // now read the message contents
            InnerBody = new byte[MessageSize];
            memStream.Read(InnerBody, 0, (int)MessageSize);

            // there is no footer

            // clean up
            reader.Close();
            memStream.Close();
        }
    }

    /// <summary>
    /// A P2P Message which is send in a direct-connection.
    /// </summary>
    /// <remarks>The innerbody contents are used as message contents (data). The InnerMessage object is ignored.</remarks>
    [Serializable()]
    public class P2PDCHandshakeMessage : P2PDCMessage
    {
        private Guid guid;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <remarks>
        /// Defaults the Flags property to 0x100
        /// </remarks>
        public P2PDCHandshakeMessage()
        {
            Flags = P2PFlag.DirectHandshake;
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCHandshakeMessage(P2PMessage message)
            : base(message)
        {
            Guid = new Guid(
                (int)message.AckSessionId,
                (short)(message.AckIdentifier & 0x0000FFFF),
                (short)((message.AckIdentifier & 0xFFFF0000) >> 16),
                (byte)((message.AckTotalSize & 0x00000000000000FF)),
                (byte)((message.AckTotalSize & 0x000000000000FF00) >> 8),
                (byte)((message.AckTotalSize & 0x0000000000FF0000) >> 16),
                (byte)((message.AckTotalSize & 0x00000000FF000000) >> 24),
                (byte)((message.AckTotalSize & 0x000000FF00000000) >> 32),
                (byte)((message.AckTotalSize & 0x0000FF0000000000) >> 40),
                (byte)((message.AckTotalSize & 0x00FF000000000000) >> 48),
                (byte)((message.AckTotalSize & 0xFF00000000000000) >> 56));
        }

        /// <summary>
        /// Creates an acknowledgement message to a handshake message. This will only set the flag to 0 and
        /// </summary>
        /// <returns></returns>
        public override P2PMessage CreateAcknowledgement()
        {
            // create a copy of this message
            P2PDCHandshakeMessage ackMessage = new P2PDCHandshakeMessage(this);

            // set the identifier to 0 to set our own local identifier
            ackMessage.Identifier = 0;
            return ackMessage;
        }

        /// <summary>
        /// The Guid to use in the handshake message.
        /// </summary>
        public Guid Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }


        /// <summary>
        /// Writes no footer, but a 4 byte length size in front of the header.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {

            // first get the bytes for the handshake
            byte[] handshakeMessage = base.GetBytes();


            byte[] totalMessage = new byte[handshakeMessage.Length + 8];
            byte[] fooMessage = new byte[] { 0x04, 0x00, 0x00, 0x00, 0x66, 0x6f, 0x6f, 0x00 };
            byte[] guidMessage = guid.ToByteArray();
            Array.Copy(fooMessage, 0, totalMessage, 0, 8);
            Array.Copy(handshakeMessage, 0, totalMessage, 8, handshakeMessage.Length);
            Array.Copy(guidMessage, 0, totalMessage, totalMessage.Length - 16, 16);

            // return the total message
            return totalMessage;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId    : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier   : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset       : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize    : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize  : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags        : {1:x} ({0})\r\n", (uint)Flags, Convert.ToString(Flags)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Guid         : {0}\r\n", this.Guid.ToString());
            return "[P2PDCHandshakeMessage]\r\n" + debugLine;
        }
    }
};
