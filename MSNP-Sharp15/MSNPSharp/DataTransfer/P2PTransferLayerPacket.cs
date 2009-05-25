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

    internal enum OperationCode : byte
    {
        None = 0x0,
        NeedACK = 0x2,
        InitSession = 0x3
    }

    public class P2PTransferLayerPacket : NetworkMessage
    {
        private Byte operationCode;
        private UInt32 sequenceNumber;
        private Dictionary<byte, byte[]> knownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN
        private Dictionary<byte, byte[]> unknownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN
        private P2PDataLayerPacket dataPacket;
        private UInt32 footer;
        private bool needACK = false;

        public byte HeaderLength
        {
            get
            {
                byte length = 8;
                if (knownTLVs.Count > 0)
                {
                    // Sum TLV lengths
                    foreach (byte[] val in knownTLVs.Values)
                    {
                        length += (byte)(1 + 1 + val.Length);
                    }
                    // 4 bytes padding
                    if ((length % 4) != 0)
                    {
                        length += (byte)(4 - (length % 4));
                    }
                }
                return length;
            }
        }

        public byte OperationCode
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

        public ushort PayloadLength
        {
            get
            {
                if (DataPacket == null)
                    return 0;

                return (ushort)DataPacket.GetBytes().Length;
            }
        }

        public UInt32 SequenceNumber
        {
            get
            {
                return sequenceNumber;
            }
            set
            {
                sequenceNumber = value;
            }
        }

        public bool NeedACK
        {
            get
            {
                return needACK;
            }
            set
            {
                if (value)
                {
                    OperationCode = (byte)MSNPSharp.DataTransfer.OperationCode.NeedACK;
                }

                needACK = value;
            }
        }

        public bool IsACK
        {
            get
            {
                return AcksequenceNumber > 0;
            }
        }

        private UInt32 acksequenceNumber;
        public UInt32 AcksequenceNumber
        {
            get
            {
                return acksequenceNumber;
            }
            set
            {
                acksequenceNumber = value;

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

        public P2PDataLayerPacket DataPacket
        {
            get
            {
                return dataPacket;
            }
            set
            {
                dataPacket = value;
            }
        }

        public UInt32 Footer
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

        public P2PTransferLayerPacket()
        {
        }

        public P2PTransferLayerPacket(byte[] innerBytes)
        {
            InnerBody = innerBytes;
            ParseBytes(InnerBody);
        }

        public override void PrepareMessage()
        {
            base.PrepareMessage();
        }

        public override byte[] GetBytes()
        {
            if (NeedACK)
            {
                OperationCode = (byte)MSNPSharp.DataTransfer.OperationCode.NeedACK;
            }

            byte headerLen = HeaderLength;

            // Header + Payload + Footer(for SB sessions only)
            byte[] data = new byte[headerLen + PayloadLength + 4];

            MemoryStream memStream = new MemoryStream(data);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write((byte)headerLen);
            writer.Write((byte)OperationCode);
            writer.Write(BitUtility.ToBigEndian((ushort)PayloadLength));
            writer.Write(BitUtility.ToBigEndian((uint)SequenceNumber));

            foreach (KeyValuePair<byte, byte[]> keyvalue in knownTLVs)
            {
                writer.Write((byte)keyvalue.Key); // Type
                writer.Write((byte)keyvalue.Value.Length); // Length
                writer.Write(keyvalue.Value, 0, keyvalue.Value.Length); // Value
            }

            memStream.Seek(headerLen, SeekOrigin.Begin); // Skip padding bytes for TLVs

            if (PayloadLength > 0)
            {
                writer.Write(DataPacket.GetBytes());
            }

            writer.Write(BitUtility.ToBigEndian(Footer));

            writer.Close();
            memStream.Close();

            InnerBody = data;

            return data;
        }

        public override void ParseBytes(byte[] data)
        {
            MemoryStream mem = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(mem);

            byte headerLen = reader.ReadByte();
            OperationCode = (byte)reader.ReadByte();
            ushort payloadLen = BitUtility.ToBigEndian(reader.ReadUInt16());
            SequenceNumber = BitUtility.ToBigEndian(reader.ReadUInt32());

            if (OperationCode == (byte)MSNPSharp.DataTransfer.OperationCode.NeedACK)
            {
                NeedACK = true;
            }

            if (headerLen > 8)  //TLVs
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

            mem.Seek(headerLen, SeekOrigin.Begin); // Skip padding bytes for TLVs

            if (payloadLen > 0)
            {
                DataPacket = new P2PDataLayerPacket(reader.ReadBytes(payloadLen));
            }

            Footer = BitUtility.ToBigEndian(reader.ReadUInt32());

            mem.Close();
            reader.Close();
        }

        protected void ProcessTLVData(byte T, byte L, byte[] V)
        {
            switch (T)
            {
                case 1:
                    P2PDataLayerPacket initialData = new P2PDataLayerPacket();
                    initialData.InnerBody = V;
                    knownTLVs[T] = V;
                    return;

                case 2:
                    if (L == 4)
                    {
                        AcksequenceNumber = BitUtility.ToUInt32(V, 0, false);
                        knownTLVs[T] = V;
                        return;
                    }
                    break;
            }

            unknownTLVs.Add(T, V);
        }

        /// <summary>
        /// Returns debug info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (InnerBody == null)
            {
                GetBytes();
            }

            string dataString = "[No data]\r\n";
            if (DataPacket != null)
            {
                dataString = DataPacket.ToString();
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<byte, byte[]> keyvalue in knownTLVs)
            {
                sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),", keyvalue.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Key));
                sb.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{1:x}({0}),( ", keyvalue.Value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), keyvalue.Value.Length));
                foreach (byte b in keyvalue.Value)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "0x{0:x2} ", b));
                }
                sb.Append("); ");
            }

            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "HeaderLength        : {1:x} ({0})\r\n", HeaderLength.ToString(System.Globalization.CultureInfo.InvariantCulture), HeaderLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "OperationCode       : {1:x} ({0})\r\n", OperationCode.ToString(System.Globalization.CultureInfo.InvariantCulture), OperationCode) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PayloadLength       : {1:x} ({0})\r\n", PayloadLength.ToString(System.Globalization.CultureInfo.InvariantCulture), PayloadLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SequenceNumber      : {1:x} ({0})\r\n", SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), SequenceNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AcksequenceNumber   : {1:x} ({0})\r\n", AcksequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), AcksequenceNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TLV({0})              : {1}\r\n", knownTLVs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), sb.ToString());

            debugLine +=

                "{\r\n" +
                dataString +
                "}\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer              : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
            return debugLine;
        }
    }
};
