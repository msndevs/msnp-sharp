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
using System.Text;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp.Core;

    [Serializable]
    public abstract class P2PHeader
    {
        /// <summary>
        /// Header length
        /// </summary>
        public abstract int HeaderLength
        {
            get;
        }

        /// <summary>
        /// Sequence number
        /// </summary>
        public UInt32 Identifier
        {
            get
            {
                return _identifier;
            }
            set
            {
                _identifier = value;
            }
        }

        /// <summary>
        /// Payload length
        /// </summary>
        public UInt32 MessageSize
        {
            get
            {
                return _messageSize;
            }
            set
            {
                _messageSize = value;
            }
        }

        /// <summary>
        /// Total size
        /// </summary>
        public UInt64 TotalSize
        {
            get
            {
                return _totalSize;
            }
            set
            {
                _totalSize = value;
            }
        }

        public UInt32 SessionId
        {
            get
            {
                return _sessionId;
            }
            set
            {
                _sessionId = value;
            }
        }

        /// <summary>
        /// Acknowledgement identifier
        /// </summary>
        public abstract UInt32 AckIdentifier
        {
            get;
            set;
        }

        public virtual bool IsAcknowledgement
        {
            get
            {
                return (AckIdentifier != 0);
            }
        }

        public abstract P2PHeader CreateAck();
        public abstract int ParseHeader(byte[] data);
        public abstract byte[] GetBytes();

        protected P2PHeader()
        {
        }

        private UInt32 _identifier;
        private UInt64 _totalSize;
        private UInt32 _messageSize;
        private UInt32 _sessionId;
    };

    [Serializable]
    public class P2Pv1Header : P2PHeader
    {
        //private UInt32 sessionId;
        //private UInt32 identifier;
        private UInt64 offset;
        //private UInt64 totalSize;
        //private UInt32 messageSize;
        private P2PFlag flags;
        private UInt32 ackSessionId;
        private UInt32 ackIdentifier;
        private UInt64 ackTotalSize;

        #region Properties

        /// <summary>
        /// The session identifier field. Bytes 0-3 in the binary header.
        /// </summary>
        public new uint SessionId
        {
            get
            {
                return base.SessionId;
            }
            set
            {
                base.SessionId = value;
            }
        }

        /// <summary>
        /// The identifier of this message. Bytes 5-8 in the binary header.
        /// </summary>
        public new uint Identifier
        {
            get
            {
                return base.Identifier;
            }
            set
            {
                base.Identifier = value;
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
        public new ulong TotalSize
        {
            get
            {
                return base.TotalSize;
            }
            set
            {
                base.TotalSize = value;
            }
        }

        /// <summary>
        /// Message length in bytes of the current message. Bytes 25-28 in the binary header.
        /// </summary>
        public new uint MessageSize
        {
            get
            {
                return base.MessageSize;
            }
            set
            {
                base.MessageSize = value;
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
        public override uint AckIdentifier
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

        #endregion

        public override int HeaderLength
        {
            get
            {
                return 48;
            }
        }

        public override P2PHeader CreateAck()
        {
            P2Pv1Header ack = new P2Pv1Header();
            ack.TotalSize = TotalSize;
            ack.Flags = P2PFlag.Acknowledgement;
            ack.AckSessionId = Identifier;
            ack.AckIdentifier = AckSessionId;
            ack.AckTotalSize = TotalSize;
            return ack;
        }

        public override int ParseHeader(byte[] data)
        {
            Stream memStream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(memStream);

            SessionId = (UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            Identifier = (UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            Offset = (UInt64)BitUtility.ToLittleEndian(reader.ReadUInt64());
            TotalSize = (UInt64)BitUtility.ToLittleEndian(reader.ReadUInt64());
            MessageSize = (UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            Flags = (P2PFlag)(UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            AckSessionId = (UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            AckIdentifier = (UInt32)BitUtility.ToLittleEndian(reader.ReadUInt32());
            AckTotalSize = (UInt64)BitUtility.ToLittleEndian(reader.ReadUInt64());

            reader.Close();
            memStream.Close();

            return HeaderLength;
        }

        public override byte[] GetBytes()
        {
            byte[] header = new byte[HeaderLength];
            Stream memStream = new MemoryStream(header);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(BitUtility.ToLittleEndian((UInt32)SessionId));
            writer.Write(BitUtility.ToLittleEndian((UInt32)Identifier));
            writer.Write(BitUtility.ToLittleEndian((UInt64)Offset));
            writer.Write(BitUtility.ToLittleEndian((UInt64)TotalSize));
            writer.Write(BitUtility.ToLittleEndian((UInt32)MessageSize));
            writer.Write(BitUtility.ToLittleEndian((UInt32)Flags));
            writer.Write(BitUtility.ToLittleEndian((UInt32)AckSessionId));
            writer.Write(BitUtility.ToLittleEndian((UInt32)AckIdentifier));
            writer.Write(BitUtility.ToLittleEndian((UInt64)AckTotalSize));

            writer.Close();
            memStream.Close();

            return header;
        }

        public override string ToString()
        {
            return "[P2Pv1Header]\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId     : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier    : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Offset        : {1:x} ({0})\r\n", Offset.ToString(System.Globalization.CultureInfo.InvariantCulture), Offset) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TotalSize     : {1:x} ({0})\r\n", TotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), TotalSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize   : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Flags         : {1:x} ({0})\r\n", (uint)Flags, Convert.ToString(Flags)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckSessionId  : {1:x} ({0})\r\n", AckSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), AckSessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckIdentifier : {1:x} ({0})\r\n", AckIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture), AckIdentifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckTotalSize  : {1:x} ({0})\r\n", AckTotalSize.ToString(System.Globalization.CultureInfo.InvariantCulture), AckTotalSize);
        }
    };

    [Serializable]
    public class P2Pv2Header : P2PHeader
    {
        private OperationCode operationCode;
        //private UInt16 messageSize;
        //private UInt32 identifier;
        private Dictionary<byte, byte[]> knownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN
        private Dictionary<byte, byte[]> unknownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN

        public Dictionary<byte, byte[]> UnknownTLVs
        {
            get
            {
                return unknownTLVs;
            }
        }

        /// <summary>
        /// Header length (dynamic)
        /// </summary>
        /// <remarks>Min: 8, Max: 252. Padding: 4</remarks>
        public override int HeaderLength
        {
            get
            {
                int length = 8;
                if (knownTLVs.Count > 0)
                {
                    // Sum TLV lengths
                    foreach (byte[] val in knownTLVs.Values)
                    {
                        length += 1 + 1 + val.Length;
                    }
                    // 4 bytes padding
                    if ((length % 4) != 0)
                    {
                        length += (4 - (length % 4));
                    }
                }
                return length;
            }
        }

        /// <summary>
        /// Type, Length, Values. Max length (t+l+v): 244. Header length - 8 = TLVs length
        /// </summary>
        public Dictionary<byte, byte[]> KnownTLVs
        {
            get
            {
                return knownTLVs;
            }
        }

        /// <summary>
        /// Operation code
        /// </summary>
        public OperationCode OperationCode
        {
            get
            {
                return operationCode;
            }
            set
            {
                operationCode = value;
            }
        }

        /// <summary>
        /// Payload size
        /// </summary>
        public new uint MessageSize
        {
            get
            {
                return base.MessageSize;
            }
            set
            {
                base.MessageSize = value;
            }
        }

        /// <summary>
        /// Message identifier
        /// </summary>
        public new uint Identifier
        {
            get
            {
                return base.Identifier;
            }
            set
            {
                base.Identifier = value;
            }
        }

        private UInt32 ackIdentifier;
        public override UInt32 AckIdentifier
        {
            get
            {
                return ackIdentifier;
            }
            set
            {
                ackIdentifier = value;

                if (value == 0)
                {
                    knownTLVs.Remove(0x2);
                }
                else
                {
                    knownTLVs[0x2] = BitUtility.GetBytes(value, false);
                }
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


        public override P2PHeader CreateAck()
        {
            P2Pv2Header ack = new P2Pv2Header();
            ack.AckIdentifier = Identifier + MessageSize;
            return ack;
        }

        /// <summary>
        /// Parse header
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Header length</returns>
        public override int ParseHeader(byte[] data)
        {
            MemoryStream mem = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(mem);

            int headerLen = (int)(Byte)reader.ReadByte();
            OperationCode = (OperationCode)(Byte)reader.ReadByte();
            MessageSize = (uint)(UInt16)BitUtility.ToBigEndian(reader.ReadUInt16());
            Identifier = (uint)(UInt32)BitUtility.ToBigEndian(reader.ReadUInt32());
            if (headerLen > 8) //TLVs
            {
                byte[] TLvs = reader.ReadBytes(headerLen - 8);
                int index = 0;
                do
                {
                    byte T = TLvs[index];

                    if (T == 0x0)
                        break; // Skip padding bytes

                    byte L = TLvs[index + 1];
                    byte[] V = new byte[(int)L];
                    Array.Copy(TLvs, index + 2, V, 0, (int)L);
                    ProcessTLVData(T, L, V);
                    index += 2 + L;
                }
                while (index < TLvs.Length);
            }
            reader.Close();
            mem.Close();

            return headerLen;
        }

        protected void ProcessTLVData(byte T, byte L, byte[] V)
        {
            switch (T)
            {
                case 1:
                    P2PDataLayerPacket initialData = new P2PDataLayerPacket();
                    initialData.InnerBody = V;
                    // knownTLVs[T] = V;
                    return;

                case 2:
                    if (L == 4)
                    {
                        AckIdentifier = BitUtility.ToUInt32(V, 0, false);
                        return;
                    }
                    break;
            }

            unknownTLVs[T] = V;
        }

        public override byte[] GetBytes()
        {
            int headerLen = HeaderLength;
            byte[] data = new byte[headerLen];
            MemoryStream memStream = new MemoryStream(data);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write((byte)headerLen);
            writer.Write((byte)OperationCode);
            writer.Write(BitUtility.ToBigEndian((ushort)MessageSize));
            writer.Write(BitUtility.ToBigEndian((uint)Identifier));

            foreach (KeyValuePair<byte, byte[]> keyvalue in knownTLVs)
            {
                writer.Write((byte)keyvalue.Key); // Type
                writer.Write((byte)keyvalue.Value.Length); // Length
                writer.Write(keyvalue.Value, 0, keyvalue.Value.Length); // Value
            }

            writer.Close();
            memStream.Close();

            return data;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Known TLVs ({0})      : ", knownTLVs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (knownTLVs.Count > 0)
            {                
                foreach (KeyValuePair<byte, byte[]> keyvalue in knownTLVs)
                {
                    sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),", keyvalue.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Key));
                    sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),( ", keyvalue.Value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Value.Length));
                    foreach (byte b in keyvalue.Value)
                    {
                        sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "0x{0:x2} ", b));
                    }
                    sb.Append("); ");
                    
                }
            }
            sb.Append("\r\n");

            sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Unknown TLVs ({0})    : ", unknownTLVs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (unknownTLVs.Count > 0)
            {                
                foreach (KeyValuePair<byte, byte[]> keyvalue in unknownTLVs)
                {
                    sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),", keyvalue.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Key));
                    sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),( ", keyvalue.Value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Value.Length));
                    foreach (byte b in keyvalue.Value)
                    {
                        sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "0x{0:x2} ", b));
                    }
                    sb.Append("); ");
                }
            }
            sb.Append("\r\n");

            return "[P2Pv2Header]\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionId           : {1:x} ({0})\r\n", SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionId) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TFCombination       : {1:x} ({0})\r\n", (byte)TFCombination, Convert.ToString(TFCombination)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PackageNumber       : {1:x} ({0})\r\n", PackageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), PackageNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "DataRemaining       : {1:x} ({0})\r\n", DataRemaining.ToString(System.Globalization.CultureInfo.InvariantCulture), DataRemaining) +

                String.Format(System.Globalization.CultureInfo.InvariantCulture, "HeaderLength        : {1:x} ({0})\r\n", HeaderLength.ToString(System.Globalization.CultureInfo.InvariantCulture), HeaderLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "OperationCode       : {1:x} ({0})\r\n", (byte)OperationCode, Convert.ToString(OperationCode)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "MessageSize         : {1:x} ({0})\r\n", MessageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), MessageSize) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Identifier          : {1:x} ({0})\r\n", Identifier.ToString(System.Globalization.CultureInfo.InvariantCulture), Identifier) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AckIdentifier       : {1:x} ({0})\r\n", AckIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture), AckIdentifier) +
                sb.ToString();
        }
    }
};
