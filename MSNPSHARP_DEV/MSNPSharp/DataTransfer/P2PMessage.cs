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
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    #region P2PFlag

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

    #endregion

    #region P2PConst

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

    #endregion

    #region OperationCode

    public enum OperationCode : byte
    {
        None = 0x0,
        TransferPrepare = 0x2,
        InitSession = 0x3
    }

    #endregion

    #region P2PMessage

    /// <summary>
    /// Represents a single P2P framework message.
    /// </summary>
    [Serializable]
    public class P2PMessage : NetworkMessage
    {
        private P2PVersion version;
        private P2PHeader header;
        private uint footer = 0;

        public P2PMessage(P2PVersion ver)
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
        }

        public P2PMessage(P2PMessage message)
            : this(message.Version)
        {
            Header.SessionId = message.Header.SessionId;
            Header.Identifier = message.Header.Identifier;
            Header.TotalSize = message.Header.TotalSize;
            Header.MessageSize = message.Header.MessageSize;
            Header.AckIdentifier = message.Header.AckIdentifier;

            if (message.Version == P2PVersion.P2PV1)
            {
                V1Header.Offset = message.V1Header.Offset;
                V1Header.Flags = message.V1Header.Flags;
                V1Header.AckSessionId = message.V1Header.AckSessionId;
                V1Header.AckTotalSize = message.V1Header.AckTotalSize;
            }
            else if (message.Version == P2PVersion.P2PV2)
            {
                V2Header.OperationCode = message.V2Header.OperationCode;
                V2Header.TFCombination = message.V2Header.TFCombination;
                V2Header.PackageNumber = message.V2Header.PackageNumber;
                V2Header.DataRemaining = message.V2Header.DataRemaining;

                if (message.V2Header.HeaderTLVs.Count > 0)
                {
                    foreach (KeyValuePair<byte, byte[]> keyvalue in message.V2Header.HeaderTLVs)
                    {
                        V2Header.HeaderTLVs[keyvalue.Key] = keyvalue.Value;
                    }
                }
                if (message.V2Header.DataPacketTLVs.Count > 0)
                {
                    foreach (KeyValuePair<byte, byte[]> keyvalue in message.V2Header.DataPacketTLVs)
                    {
                        V2Header.DataPacketTLVs[keyvalue.Key] = keyvalue.Value;
                    }
                }
            }

            if (message.InnerMessage != null)
                InnerMessage = message.InnerMessage;

            if (message.InnerBody != null)
                InnerBody = message.InnerBody;

            Footer = message.Footer;
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

            private set
            {
                if ((Version == P2PVersion.P2PV1 && value is P2Pv1Header)
                    || (Version == P2PVersion.P2PV2 && value is P2Pv2Header))
                {
                    header = value;
                }
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
                base.InnerMessage = null; // Data changed, re-parse SLP message

                if (version == P2PVersion.P2PV1)
                {
                    header.MessageSize = (uint)value.Length;
                    header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
                }
                else if (version == P2PVersion.P2PV2)
                {
                    if (value.Length > 0)
                    {
                        header.MessageSize = (uint)value.Length; // DataPacketHeaderLength depends on MessageSize
                        header.MessageSize += (uint)V2Header.DataPacketHeaderLength;
                        header.TotalSize = Math.Max(header.TotalSize, header.MessageSize);
                    }
                    else
                    {
                        header.MessageSize = 0;
                        header.TotalSize = 0;
                    }
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
                base.InnerMessage = null; // Data changed, re-parse SLP message
            }
        }


        public bool IsSLPData
        {
            get
            {
                if (Header.MessageSize > 0 && Header.SessionId == 0)
                {
                    if ((Version == P2PVersion.P2PV1 && (V1Header.Flags == P2PFlag.Normal || V1Header.Flags == P2PFlag.MSNSLPInfo))
                        ||
                        (Version == P2PVersion.P2PV2 && (V2Header.TFCombination == TFCombination.None || V2Header.TFCombination == TFCombination.First)))
                    {
                        return true;
                    }

                }
                return false;
            }
        }

        /// <summary>
        /// Creates an acknowledgement message to this message.
        /// </summary>
        /// <returns></returns>
        public virtual P2PMessage CreateAcknowledgement()
        {
            P2PMessage ack = new P2PMessage(Version);
            ack.Header = Header.CreateAck();

            if (Version == P2PVersion.P2PV1)
            {
                ack.Footer = Footer;                    //Keep the same as the message to acknowladge.

            }

            return ack;
        }

        /// <summary>
        /// Split big P2PMessages to transport over sb or dc.
        /// </summary>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public P2PMessage[] SplitMessage(int maxSize)
        {
            uint payloadMessageSize = 0;

            if (Version == P2PVersion.P2PV1)
            {
                payloadMessageSize = V1Header.MessageSize;
            }

            if (Version == P2PVersion.P2PV2)
            {
                payloadMessageSize = (uint)V2Header.MessageSize - (uint)V2Header.DataPacketHeaderLength;
            }

            if (payloadMessageSize <= maxSize)
                return new P2PMessage[] { this };


            Random rand = new Random();
            List<P2PMessage> chunks = new List<P2PMessage>();
            byte[] totalMessage = (InnerBody != null)
                ? InnerBody
                : InnerMessage.GetBytes();

            long offset = 0;

            if (Version == P2PVersion.P2PV1)
            {
                while (offset < totalMessage.LongLength)
                {
                    P2PMessage chunkMessage = new P2PMessage(Version);
                    uint messageSize = (uint)Math.Min((uint)maxSize, (totalMessage.LongLength - offset));
                    byte[] chunk = new byte[messageSize];
                    Array.Copy(totalMessage, (int)offset, chunk, 0, (int)messageSize);

                    chunkMessage.V1Header.Flags = V1Header.Flags;
                    chunkMessage.V1Header.AckIdentifier = V1Header.AckIdentifier;
                    chunkMessage.V1Header.AckTotalSize = V1Header.AckTotalSize;
                    chunkMessage.V1Header.Identifier = V1Header.Identifier;
                    chunkMessage.V1Header.SessionId = V1Header.SessionId;
                    chunkMessage.V1Header.TotalSize = V1Header.TotalSize;
                    chunkMessage.V1Header.Offset = (ulong)offset;
                    chunkMessage.V1Header.MessageSize = messageSize;
                    chunkMessage.InnerBody = chunk;

                    chunkMessage.V1Header.AckSessionId = (uint)rand.Next(50000, int.MaxValue);
                    chunkMessage.Footer = Footer;

                    chunkMessage.PrepareMessage();
                    chunks.Add(chunkMessage);

                    offset += messageSize;
                }
            }



            if (Version == P2PVersion.P2PV2)
            {
                uint nextId = Header.Identifier;
                long dataRemain = (long)V2Header.DataRemaining;
                while (offset < totalMessage.LongLength)
                {
                    P2PMessage chunkMessage = new P2PMessage(Version);
                    int maxDataSize = maxSize;

                    if (offset == 0 && V2Header.HeaderTLVs.Count > 0)
                    {
                        foreach (KeyValuePair<byte, byte[]> keyvalue in V2Header.HeaderTLVs)
                        {
                            chunkMessage.V2Header.HeaderTLVs[keyvalue.Key] = keyvalue.Value;
                        }

                        maxDataSize = maxSize - chunkMessage.V2Header.HeaderLength;
                    }


                    uint dataSize = (uint)Math.Min((uint)maxDataSize, (totalMessage.LongLength - offset));

                    byte[] chunk = new byte[dataSize];
                    Array.Copy(totalMessage, (int)offset, chunk, 0, (int)dataSize);

                    chunkMessage.V2Header.OperationCode = V2Header.OperationCode;
                    chunkMessage.V2Header.SessionId = V2Header.SessionId;
                    chunkMessage.V2Header.TFCombination = V2Header.TFCombination;
                    chunkMessage.V2Header.PackageNumber = V2Header.PackageNumber;

                    if (totalMessage.LongLength + dataRemain - (dataSize + offset) > 0)
                    {
                        chunkMessage.V2Header.DataRemaining = (ulong)(totalMessage.LongLength + dataRemain - (dataSize + offset));
                    }

                    if ((offset != 0) &&
                        TFCombination.First == (V2Header.TFCombination & TFCombination.First))
                    {
                        chunkMessage.V2Header.TFCombination = (TFCombination)(V2Header.TFCombination - TFCombination.First);
                    }

                    chunkMessage.InnerBody = chunk;
                    chunkMessage.Header.Identifier = nextId;
                    nextId += chunkMessage.Header.MessageSize;

                    chunkMessage.PrepareMessage();
                    chunks.Add(chunkMessage);

                    offset += dataSize;
                }
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
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "FOOTER              : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "DATA                : {0}\r\n", ((InnerMessage != null) ? InnerMessage.ToString() : "{Binary data}" /* DumpBytes(InnerBody) */));
        }

        public static string DumpBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            uint hexChars = 0;

            for (int i = 0; i < data.Length; i++)
            {
                string str = string.Format("0x{0:x2} ", data[i]).ToLower();

                hexChars++;

                sb.Append(str);

                if ((hexChars > 0) && (hexChars % 10 == 0))
                    sb.AppendLine();
            }

            return sb.ToString();
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
            InnerBody = GetInnerBytes();

            byte[] allData = new byte[header.HeaderLength + header.MessageSize + (appendFooter ? 4 : 0)];

            MemoryStream stream = new MemoryStream(allData);
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(header.GetBytes());
            writer.Write(InnerBody);

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
            int headerAndBodyHeaderLen = header.ParseHeader(data);
            byte[] bodyAndFooter = new byte[data.Length - headerAndBodyHeaderLen];
            Array.Copy(data, headerAndBodyHeaderLen, bodyAndFooter, 0, bodyAndFooter.Length);

            Stream stream = new MemoryStream(bodyAndFooter);
            BinaryReader reader = new BinaryReader(stream);
            int innerBodyLen = 0;

            if (header.MessageSize > 0)
            {
                if (version == P2PVersion.P2PV1)
                {
                    InnerBody = reader.ReadBytes((int)header.MessageSize);
                    innerBodyLen = InnerBody.Length;
                }
                else if (version == P2PVersion.P2PV2)
                {
                    InnerBody = reader.ReadBytes((int)(header.MessageSize - V2Header.DataPacketHeaderLength));
                    innerBodyLen = InnerBody.Length;
                }
            }
            else
            {
                InnerBody = new byte[0];
            }

            if ((data.Length - headerAndBodyHeaderLen + innerBodyLen) >= 4)
            {
                footer = BitUtility.ToBigEndian(reader.ReadUInt32());
            }

            reader.Close();
            stream.Close();
        }

        /// <summary>
        /// Returns the inner message as a byte array.
        /// </summary>
        /// <remarks>
        /// If the inner message is set the GetBytes() method is called upon that inner message.
        /// If there is no inner message set, but the InnerBody property contains data then
        /// that data is returned.
        /// </remarks>
        /// <returns></returns>
        protected virtual byte[] GetInnerBytes()
        {

            return (InnerBody != null)
                ? InnerBody
                : (InnerMessage != null ? InnerMessage.GetBytes() : new byte[0]);
        }
    };

    #endregion

    #region P2PDataMessage

    /// <summary>
    /// Represents a single P2PDataMessage which is used for the actual data transfer. No negotiation handling.
    /// </summary>
    /// <remarks>
    /// A p2p data message can be identified by looking at the footer in the P2P Message.
    /// When this value is > 0 a data message is send. When this value is 0 a normal, and more complex, MSNSLPMessage is send.
    /// This class is created to provide a fast way of sending messages.
    /// </remarks>
    [Serializable]
    public class P2PDataMessage : P2PMessage
    {
        /// <summary>
        /// Constructs a P2P data message.
        /// </summary>
        public P2PDataMessage(P2PVersion v)
            : base(v)
        {
        }

        public P2PDataMessage(P2PMessage copy)
            : base(copy)
        {
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

        public override string ToString()
        {
            return "[P2PDataMessage]\r\n" + base.ToString();
        }
    };

    #endregion

    #region P2PDCMessage

    /// <summary>
    /// A P2P Message which is send in a direct-connection.
    /// </summary>
    /// <remarks>
    /// The innerbody contents are used as message contents (data).
    /// The InnerMessage object and footer is ignored.
    /// </remarks>
    [Serializable]
    public class P2PDCMessage : P2PDataMessage
    {
        public P2PDCMessage(P2PVersion ver)
            : base(ver)
        {
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCMessage(P2PMessage message)
            : base(message)
        {
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

        public override string ToString()
        {
            return "[P2PDCMessage]\r\n" + base.ToString();
        }
    };

    #endregion

    #region P2PDCHandshakeMessage

    /// <summary>
    /// A P2P Message which is send in a direct-connection. (P2Pv1?)
    /// </summary>
    /// <remarks>The innerbody contents are used as message contents (data).
    /// The InnerMessage object is ignored.
    /// </remarks>
    [Serializable]
    public class P2PDCHandshakeMessage : P2PDCMessage
    {
        private Guid guid;

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <remarks>
        /// Defaults the Flags property to 0x100
        /// </remarks>
        public P2PDCHandshakeMessage(P2PVersion ver)
            : base(ver)
        {
            if (ver == P2PVersion.P2PV1)
                V1Header.Flags = P2PFlag.DirectHandshake;
            else if (ver == P2PVersion.P2PV2)
                V2Header.OperationCode = 0x0; //v!
        }

        /// <summary>
        /// Copy constructor. Creates a shallow copy of the properties of the P2PMessage.
        /// </summary>
        /// <param name="message"></param>
        public P2PDCHandshakeMessage(P2PMessage message)
            : base(message)
        {
            if (message.Version == P2PVersion.P2PV1)
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
            else if (message.Version == P2PVersion.P2PV2)
            {
                throw new NotSupportedException("Not support direct connection in p2pv2.");
            }
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

    #endregion

};
