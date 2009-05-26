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

    [Flags]
    public enum TFCombination : byte
    {
        None = 0,
        First = 1,
        Unknown2 = 2,
        MsnObject = 4,
        FileTransfer = 6,
    }

    public class P2PDataLayerPacket : NetworkMessage
    {
        private TFCombination tFCombination;
        private ushort packageNumber;
        private UInt32 sessionID;
        private Dictionary<byte, byte[]> knownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN
        private Dictionary<byte, byte[]> unknownTLVs = new Dictionary<byte, byte[]>(); // BIG ENDIAN
        private byte[] payloadData;

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

        public TFCombination TFCombination
        {
            get
            {
                return tFCombination;
            }
            set
            {
                tFCombination = value;
            }
        }

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

        public UInt32 SessionID
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

        private UInt64 dataRemaining;
        public UInt64 DataRemaining
        {
            get
            {
                return dataRemaining;
            }
            set
            {
                dataRemaining = value;

                if (value == 0)
                {
                    knownTLVs.Remove(0x1);
                }
                else
                {
                    knownTLVs[0x1] = BitUtility.GetBytes(value, false);
                }
            }
        }

        public byte[] PayloadData
        {
            get
            {
                return payloadData;
            }
            set
            {
                payloadData = value;
            }
        }

        public P2PDataLayerPacket()
        {
        }

        public P2PDataLayerPacket(byte[] innerBytes)
        {
            InnerBody = innerBytes;

            if (InnerBody.Length > 0)
                ParseBytes(InnerBody);
        }

        public override void PrepareMessage()
        {
            base.PrepareMessage();
        }

        public override byte[] GetBytes()
        {
            byte headerLen = HeaderLength;
            ushort payloadLength = 0;
            if (PayloadData != null)
            {
                payloadLength = (ushort)PayloadData.Length;
            }

            byte[] data = new byte[headerLen + payloadLength];
            MemoryStream memStream = new MemoryStream(data);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write((byte)headerLen);
            writer.Write((byte)TFCombination);
            writer.Write(BitUtility.ToBigEndian((ushort)PackageNumber));
            writer.Write(BitUtility.ToBigEndian((uint)SessionID));

            foreach (KeyValuePair<byte, byte[]> keyvalue in knownTLVs)
            {
                writer.Write((byte)keyvalue.Key); // Type
                writer.Write((byte)keyvalue.Value.Length); // Length
                writer.Write(keyvalue.Value, 0, keyvalue.Value.Length); // Value
            }

            memStream.Seek(headerLen, SeekOrigin.Begin); // Skip padding bytes for TLVs

            if (payloadLength > 0)
            {
                writer.Write(PayloadData);
            }

            writer.Close();
            memStream.Close();

            InnerBody = data;

            return data;
        }

        public override void ParseBytes(byte[] data)
        {
            MemoryStream mem = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(mem);

            int headerLen = (int)(Byte)reader.ReadByte();
            TFCombination = (TFCombination)(Byte)reader.ReadByte();
            PackageNumber = (ushort)(UInt16)BitUtility.ToBigEndian(reader.ReadUInt16());
            SessionID = (uint)(UInt32)BitUtility.ToBigEndian(reader.ReadUInt32());
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

            mem.Seek(headerLen, SeekOrigin.Begin); // Skip padding bytes for TLVs

            if (InnerBody.Length > headerLen)
            {
                PayloadData = reader.ReadBytes(InnerBody.Length - headerLen);
            }

            reader.Close();
            mem.Close();
        }

        protected void ProcessTLVData(byte T, byte L, byte[] V)
        {
            switch (T)
            {
                case 1:
                    if (L == 8)
                    {
                        DataRemaining = BitUtility.ToUInt64(V, 0, false);
                        knownTLVs[T] = V;
                        return;
                    }
                    break;

                case 2:
                    return;
            }

            unknownTLVs.Add(T, V);
        }

        /// <summary>
        /// Returns debug info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
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
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "HeaderLength    : {1:x} ({0})\r\n", HeaderLength.ToString(System.Globalization.CultureInfo.InvariantCulture), HeaderLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TFCombination   : {1:x} ({0})\r\n", (byte)TFCombination, Convert.ToString(TFCombination)) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PackageNumber   : {1:x} ({0})\r\n", PackageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), PackageNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionID       : {1:x} ({0})\r\n", SessionID.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionID) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "DataRemaining   : {1:x} ({0})\r\n", DataRemaining.ToString(System.Globalization.CultureInfo.InvariantCulture), DataRemaining) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TLV({0})          : {1}\r\n", knownTLVs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), sb.ToString());

            byte[] payloadBytes = Encoding.UTF8.GetBytes("[No payload data]\r\n");
            if (PayloadData != null)
            {
                payloadBytes = PayloadData;
            }

            debugLine +=
            "{\r\n" +
                Encoding.UTF8.GetString(payloadBytes) +
            "}\r\n";

            return debugLine;
        }
    }
};
