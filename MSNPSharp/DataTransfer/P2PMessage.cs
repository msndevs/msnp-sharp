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

using System;
using System.IO;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
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

    public enum AppFlags
    {

#if MSNC12
        /// <summary>
        /// Footer for a msn DisplayImage p2pMessage.
        /// </summary>
        DisplayImageFooter = 0xc,

        /// <summary>
        /// Footer for a filetransfer p2pMessage.
        /// </summary>
        FileTransFooter = 0x2,

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        CustomEmoticonFooter = 0x0b
#else
        /// <summary>
        /// Footer for a msn object p2pMessage.
        /// </summary>
        DisplayImageFooter = 0x1,

        /// <summary>
        /// Footer for a filetransfer p2pMessage.
        /// </summary>
        FileTransFooter = 0x1,

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        CustomEmoticonFooter = 0x1
#endif

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


#if MSNC12
        /// <summary>
        /// The AppID used in invitations for DisplayImage p2p transfer.
        /// </summary>
        public const uint DisplayImageAppID = 12;

        /// <summary>
        /// The AppID used in invitations for CustomEmoticon p2p transfer.
        /// </summary>
        public const uint CustomEmoticonAppID = 11;
#else

        /// <summary>
        /// The AppID used in invitations for DisplayImage p2p transfer.
        /// </summary>
        public const uint DisplayImageAppID = 1;

        /// <summary>
        /// The AppID used in invitations for CustomEmoticon p2p transfer.
        /// </summary>
        public const uint CustomEmoticonAppID = 1;
#endif

        /// <summary>
        /// The AppID(footer) used in invitations for a filetransfer.
        /// </summary>
        public const uint FileTransAppID = 2;
    }


    internal enum OperationCode : byte
    {
        None = 0x0,
        NeedACK = 0x2,
        InitSession = 0x3
    }

    /// <summary>
    /// Represents a single P2P framework message.
    /// </summary>
    [Serializable()]
    public class P2PMessage : NetworkMessage
    {
        protected P2PVersion version;
        private P2PHeader header;
        private uint footer;

        public P2PMessage(P2PVersion ver)
            : this(ver, new byte[0])
        {
        }

        public P2PMessage(P2PVersion ver, byte[] data)
        {
            version = ver;

            if (ver == P2PVersion.P2PV1)
            {
                header = new P2Pv1Header();
            }
            else if (ver == P2PVersion.P2PV2)
            {
                header = new P2Pv2Header();
            }

            if (data.Length > 0)
            {
                ParseBytes(data);
            }
        }

        /// <summary>
        /// The p2p framework currently using.
        /// </summary>
        public P2PVersion Version
        {
            get
            {
                return version;
            }
        }

        public P2PHeader Header
        {
            get
            {
                return header;
            }
        }

        public P2Pv1Header V1Header
        {
            get
            {
                return (P2Pv1Header)header;
            }
        }

        public P2Pv2Header V2Header
        {
            get
            {
                return (P2Pv2Header)header;
            }
        }

        /// <summary>
        /// The footer, or Application Identifier (BIG ENDIAN).
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
        /// Payload data
        /// </summary>
        public new byte[] InnerBody
        {
            get
            {
                return base.InnerBody;
            }
            set
            {
                base.InnerBody = value;

                if (version == P2PVersion.P2PV1)
                {
                    header.MessageSize = (uint)value.Length;
                    header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
                }
                else if (version == P2PVersion.P2PV2)
                {
                    P2PDataLayerPacket dataPacket = new P2PDataLayerPacket();
                    dataPacket.PayloadData = value;

                    header.MessageSize = (uint)dataPacket.GetBytes().Length;
                    header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
                }
            }
        }

        /// <summary>
        /// SLP Message
        /// </summary>
        public new NetworkMessage InnerMessage
        {
            get
            {
                if (base.InnerMessage == null && InnerBody != null && InnerBody.Length > 0)
                    base.InnerMessage = SLPMessage.Parse(InnerBody);

                return base.InnerMessage;
            }
            set
            {
                this.InnerBody = value.GetBytes();
                base.InnerMessage = value;
            }
        }

        #region Remove these later...

        private uint sessionID;
        public uint SessionID
        {
            get
            {
                return sessionID;
            }
            set
            {
                sessionID = value;
            }
        }

        private TFCombination tfCombination;
        public TFCombination TFCombination
        {
            get
            {
                return tfCombination;
            }
            set
            {
                tfCombination = value;
            }
        }

        private ushort packageNumber;
        public ushort PackageNumber
        {
            get
            {
                return packageNumber;
            }
            set
            {
                packageNumber = value;
            }
        }

        private ulong dataRemaining;
        public ulong DataRemaining
        {
            get
            {
                return dataRemaining;
            }
            set
            {
                dataRemaining = value;
            }
        }


        #endregion


        /// <summary>
        /// Indicates whether the message will be acked. If so, send a message returned from CreateAcknowledgement(). 
        /// </summary>
        public bool ShouldAck
        {
            get
            {
                if (Version == P2PVersion.P2PV1 && (V1Header.Offset + Header.MessageSize) != Header.TotalSize)
                    return false;
                if (Header.IsAcknowledgement)
                    return false;
                if (InnerBody != null)
                    return false;
                if (Version == P2PVersion.P2PV1 && (V1Header.Flags & P2PFlag.Waiting) == P2PFlag.Waiting)
                    return false;
                if (Version == P2PVersion.P2PV1 && (V1Header.Flags & P2PFlag.CloseSession) == P2PFlag.CloseSession)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Creates an acknowledgement message to this message.
        /// </summary>
        /// <returns></returns>
        public virtual P2PMessage CreateAcknowledgement()
        {
            P2PMessage ack = new P2PMessage(Version);

            if (Version == P2PVersion.P2PV1)
            {
                ack.Header.TotalSize = Header.TotalSize;
                ack.V1Header.Flags = P2PFlag.Acknowledgement;
                ack.V1Header.AckSessionId = Header.Identifier;
                ack.Header.AckIdentifier = V1Header.AckSessionId;
                ack.V1Header.AckTotalSize = Header.TotalSize;
            }
            else if (Version == P2PVersion.P2PV2)
            {
                // XXX TODO...
                //ack.Header.AckIdentifier = 1; // To calculate ack.Header.MessageSize correctly.
                ack.Header.AckIdentifier = ack.Header.Identifier + ack.Header.MessageSize;
            }

            return ack;
        }

        /// <summary>
        /// Split big P2PMessages to transport over sb or dc.
        /// </summary>
        /// <param name="p2pMessage"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static P2PMessage[] SplitMessage(P2PMessage p2pMessage, int maxSize)
        {
            if (p2pMessage.Header.MessageSize <= maxSize)
                return new P2PMessage[] { p2pMessage };

            ulong offset = 0;
            Random rand = new Random();
            int cnt = ((int)(p2pMessage.Header.MessageSize / maxSize)) + 1;
            List<P2PMessage> chunks = new List<P2PMessage>(cnt);
            byte[] totalMessage = (p2pMessage.InnerBody != null) ?
                p2pMessage.InnerBody : p2pMessage.InnerMessage.GetBytes();

            for (int i = 0; i < cnt; i++)
            {
                P2PMessage chunkMessage = new P2PMessage(p2pMessage.Version);

                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    chunkMessage.V1Header.AckIdentifier = p2pMessage.V1Header.AckIdentifier;
                    chunkMessage.V1Header.AckTotalSize = p2pMessage.V1Header.AckTotalSize;
                    chunkMessage.V1Header.Flags = p2pMessage.V1Header.Flags;
                    chunkMessage.V1Header.Identifier = p2pMessage.V1Header.Identifier;
                    chunkMessage.V1Header.MessageSize = (uint)Math.Min((uint)maxSize, (uint)(p2pMessage.Header.TotalSize - offset));
                    chunkMessage.V1Header.Offset = offset;
                    chunkMessage.V1Header.SessionId = p2pMessage.V1Header.SessionId;
                    chunkMessage.V1Header.TotalSize = p2pMessage.V1Header.TotalSize;

                    chunkMessage.InnerBody = new byte[chunkMessage.Header.MessageSize];
                    Array.Copy(totalMessage, (int)offset, chunkMessage.InnerBody, 0, (int)chunkMessage.Header.MessageSize);

                    chunkMessage.V1Header.AckSessionId = (uint)rand.Next(50000, int.MaxValue);
                    chunkMessage.Footer = p2pMessage.Footer;
                }
                else
                {
                    P2PDataLayerPacket DataPacket = new P2PDataLayerPacket();
                    uint messageSize = (uint)Math.Min((uint)maxSize, (uint)(p2pMessage.Header.TotalSize - offset));

                    chunkMessage.V2Header.OperationCode = p2pMessage.V2Header.OperationCode;

                    chunkMessage.Header.MessageSize = messageSize;
                    DataPacket.PayloadData = new byte[messageSize];
                    Array.Copy(totalMessage, (int)offset, DataPacket.PayloadData, 0, (int)messageSize);

                    chunkMessage.SessionID = p2pMessage.SessionID;

                    chunkMessage.Header.Identifier = (uint)(chunkMessage.Header.Identifier + offset);
                    chunkMessage.TFCombination = p2pMessage.TFCombination;

                    if (i == 0)
                    {
                        chunkMessage.Header.AckIdentifier = p2pMessage.Header.AckIdentifier;
                    }

                    if ((i != 0) &&
                        TFCombination.First == (p2pMessage.TFCombination & TFCombination.First))
                    {
                        chunkMessage.TFCombination = (TFCombination)(p2pMessage.TFCombination - TFCombination.First);
                    }
                }

                chunkMessage.PrepareMessage();
                chunks.Add(chunkMessage);

                offset += chunkMessage.Header.MessageSize;
            }

            return chunks.ToArray();
        }


        /// <summary>
        /// Returns debug info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[P2PMessage]\r\n" +
                header.ToString() +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "FOOTER        : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer) +
                ((InnerMessage != null) ? InnerMessage.ToString() : String.Empty);
        }

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

        public override byte[] GetBytes()
        {
            return GetBytes(true);
        }

        /// <summary>
        /// Creates a P2P Message. This sets the MessageSize and TotalSize properly.
        /// </summary>
        /// <param name="appendFooter"></param>
        /// <returns></returns>
        public byte[] GetBytes(bool appendFooter)
        {
            byte[] innerBytes = (InnerMessage != null)
                ? InnerMessage.GetBytes()
                : (InnerBody != null ? InnerBody : new byte[0]);

            if (version == P2PVersion.P2PV1)
            {
                header.MessageSize = (uint)innerBytes.Length;
                header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
            }
            else if (version == P2PVersion.P2PV2)
            {
                P2PDataLayerPacket dataPacket = new P2PDataLayerPacket();

                dataPacket.SessionID = SessionID;
                dataPacket.TFCombination = TFCombination;
                dataPacket.PackageNumber = PackageNumber;
                dataPacket.DataRemaining = DataRemaining;

                dataPacket.PayloadData = innerBytes; // Set payload
                innerBytes = dataPacket.GetBytes(); // Set inner bytes

                header.MessageSize = (uint)innerBytes.Length;
                header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
            }

            byte[] allData = new byte[header.HeaderLength + innerBytes.Length + (appendFooter ? 4 : 0)];

            MemoryStream stream = new MemoryStream(allData);
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(header.GetBytes());
            writer.Write(innerBytes);

            if (appendFooter)
                writer.Write(BitUtility.ToBigEndian(footer));

            writer.Close();
            stream.Close();

            return allData;
        }


        /// <summary>
        /// Parses the given message.
        /// </summary>
        public override void ParseBytes(byte[] data)
        {
            int headerLen = header.ParseHeader(data);

            Stream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);
            stream.Seek(headerLen, SeekOrigin.Begin);

            if (version == P2PVersion.P2PV1)
            {
                InnerBody = new byte[header.MessageSize];
                reader.Read(InnerBody, 0, (int)header.MessageSize);

                SessionID = V1Header.SessionId;

            }
            else if (version == P2PVersion.P2PV2)
            {
                P2PDataLayerPacket dataPacket =
                    new P2PDataLayerPacket(reader.ReadBytes((int)header.MessageSize));

                SessionID = dataPacket.SessionID;
                DataRemaining = dataPacket.DataRemaining;
                TFCombination = dataPacket.TFCombination;
                PackageNumber = dataPacket.PackageNumber;
                InnerBody = dataPacket.PayloadData;
            }

            if ((data.Length - (headerLen + (InnerBody != null ? InnerBody.Length : 0))) >= 4)
                footer = BitUtility.ToBigEndian(reader.ReadUInt32());

            reader.Close();
            stream.Close();
        }

        /// <summary>
        /// Returns the inner message as a byte array.
        /// </summary>
        /// <remarks>If the inner message is set the GetBytes() method is called upon that inner message. If there is no inner message set, but the InnerBody property contains data then that data is returned.</remarks>
        /// <returns></returns>
        protected virtual byte[] GetInnerBytes()
        {
            if (Version == P2PVersion.P2PV1)
            {
                // if there is a message we contain get the contents
                if (InnerMessage != null)
                    return InnerMessage.GetBytes();
                else if (InnerBody != null)
                    return InnerBody;
                else
                    return new byte[0];
            }
            else
            {
                return new byte[0];
            }
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
        public P2PDataMessage(P2PVersion v)
            : base(v)
        {
            Footer = 1;
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
            return "[P2PDataMessage]\r\n" +
                base.ToString();
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
            return "[P2PDCMessage]\r\n" + base.ToString();
        }

        public P2PDCMessage(P2PVersion ver)
            : base(ver)
        {
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCMessage(P2PMessage message)
            : base(message.Version)
        {
            SessionID = message.SessionID;
            Header.Identifier = message.Header.Identifier;
            V1Header.Offset = message.V1Header.Offset; // V!
            Header.TotalSize = message.Header.TotalSize;
            Header.MessageSize = message.Header.MessageSize;
            V1Header.Flags = message.V1Header.Flags; // 
            V1Header.AckSessionId = message.V1Header.AckSessionId;
            Header.AckIdentifier = message.Header.AckIdentifier;
            V1Header.AckTotalSize = message.V1Header.AckTotalSize;
            InnerMessage = message.InnerMessage;
            InnerBody = message.InnerBody;
        }

        /// <summary>
        /// Writes no footer, but a 4 byte length size in front of the header.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            byte[] dataWithoutFooter = base.GetBytes(false);
            byte[] p2pMessage = new byte[4 + dataWithoutFooter.Length];
            Stream memStream = new MemoryStream(p2pMessage);
            BinaryWriter writer = new BinaryWriter(memStream);
            writer.Write(BitUtility.ToLittleEndian((uint)dataWithoutFooter.Length));
            writer.Write(dataWithoutFooter);
            writer.Close();
            memStream.Close();

            return p2pMessage;
        }


        /// <summary>
        /// Parses a data message without the 4-byte length header and without a 4 byte footer.
        /// </summary>
        /// <param name="data"></param>
        public override void ParseBytes(byte[] data)
        {
            base.ParseBytes(data);
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
            : base(P2PVersion.P2PV1)
        {
            V1Header.Flags = P2PFlag.DirectHandshake;
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCHandshakeMessage(P2PMessage message)
            : base(message)
        {
            Guid = new Guid(
                (int)message.V1Header.AckSessionId,
                (short)(message.Header.AckIdentifier & 0x0000FFFF),
                (short)((message.Header.AckIdentifier & 0xFFFF0000) >> 16),
                (byte)((message.V1Header.AckTotalSize & 0x00000000000000FF)),
                (byte)((message.V1Header.AckTotalSize & 0x000000000000FF00) >> 8),
                (byte)((message.V1Header.AckTotalSize & 0x0000000000FF0000) >> 16),
                (byte)((message.V1Header.AckTotalSize & 0x00000000FF000000) >> 24),
                (byte)((message.V1Header.AckTotalSize & 0x000000FF00000000) >> 32),
                (byte)((message.V1Header.AckTotalSize & 0x0000FF0000000000) >> 40),
                (byte)((message.V1Header.AckTotalSize & 0x00FF000000000000) >> 48),
                (byte)((message.V1Header.AckTotalSize & 0xFF00000000000000) >> 56));
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
            ackMessage.Header.Identifier = 0;
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
        /// Foo+HandshakeMessage+Guid. Writes no footer.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            // Get the bytes for the handshake
            byte[] handshakeMessage = base.GetBytes(); // Calls P2PDCMessage.GetBytes();

            byte[] totalMessage = new byte[handshakeMessage.Length + 8];
            byte[] fooMessage = new byte[] { 0x04, 0x00, 0x00, 0x00, 0x66, 0x6f, 0x6f, 0x00 };
            byte[] guidMessage = guid.ToByteArray();

            Array.Copy(fooMessage, 0, totalMessage, 0, 8);
            Array.Copy(handshakeMessage, 0, totalMessage, 8, handshakeMessage.Length);
            Array.Copy(guidMessage, 0, totalMessage, totalMessage.Length - 16, 16);

            return totalMessage;
        }

        public override string ToString()
        {
            return "[P2PDCHandshakeMessage]\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Guid         : {0}\r\n", this.Guid.ToString()) +
                base.ToString();
        }
    }
};
