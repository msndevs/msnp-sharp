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

    public class P2PDataLayerPacket : NetworkMessage
    {
        // Header (8 bytes = 1+1+2+4)
        private byte headerLength;
        private byte tFCombination;
        private ushort packageNumber;
        private UInt32 sessionID;
        // TLVs = Header length - 8 
        private Dictionary<byte, byte[]> typeAndValues = new Dictionary<byte, byte[]>();
        // Payload (InnerBody)
        private byte[] payloadData;
        // Footer
        private UInt32 footer;

        #region Header

        public byte HeaderLength
        {
            get
            {
                return headerLength;
            }
        }

        public byte TFCombination
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

        #endregion

        public UInt64 DataRemaining
        {
            get
            {
                if (typeAndValues.ContainsKey(0x1))
                    return BitConverter.ToUInt64(typeAndValues[0x1], 0);

                return 0;
            }
            set
            {
                if (value == 0)
                    typeAndValues.Remove(0x1);
                else
                    typeAndValues[0x1] = BitConverter.GetBytes(value);
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

        public P2PDataLayerPacket()
        {
        }

        public P2PDataLayerPacket(byte[] innerBytes)
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
            headerLength = 0x08;

            foreach (byte[] val in typeAndValues.Values)
            {
                headerLength += (byte)(1 + 1 + val.Length); //T+L+V
            }
            // 4 bytes padding
            if ((headerLength % 4) != 0)
            {
                headerLength += (byte)(4 - (headerLength % 4));
            }

            ushort payloadLength = 0;
            if (PayloadData != null)
            {
                payloadLength = (ushort)PayloadData.Length;
            }

            byte[] data = new byte[HeaderLength + payloadLength];


            MemoryStream memStream = new MemoryStream(data, true);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(HeaderLength);
            writer.Write(TFCombination);
            writer.Write(P2PMessage.ToBigEndian(PackageNumber));
            writer.Write(P2PMessage.ToBigEndian(SessionID));

            foreach (KeyValuePair<byte, byte[]> keyvalue in typeAndValues)
            {
                writer.Write(keyvalue.Key); // Type
                writer.Write((byte)keyvalue.Value.Length); // Length
                writer.Write(keyvalue.Value, 0, keyvalue.Value.Length); // Value
            }

            memStream.Seek(HeaderLength, SeekOrigin.Begin);

            if (PayloadData != null)
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
            headerLength = reader.ReadByte();
            TFCombination = reader.ReadByte();
            PackageNumber = P2PMessage.ToBigEndian(reader.ReadUInt16());
            SessionID = P2PMessage.ToBigEndian(reader.ReadUInt32());
            if (HeaderLength > 8) //TLVs
            {
                byte[] TLvs = reader.ReadBytes(HeaderLength - 8);
                int index = 0;
                do
                {
                    byte T = TLvs[index];

                    if (T == 0x0)
                        break; // Skip padding bytes

                    byte L = TLvs[index + 1];
                    byte[] V = new byte[(int)L];
                    Array.Copy(TLvs, index + 2, V, 0, (int)L);
                    typeAndValues[T] = V;
                    index += 2 + L;
                }
                while (index < TLvs.Length);
            }

            if (InnerBody.Length - HeaderLength > 0)
            {
                PayloadData = reader.ReadBytes(InnerBody.Length - HeaderLength);
            }

            //Footer = P2PMessage.ToBigEndian(reader.ReadUInt32());

        }

        /// <summary>
        /// Returns debug info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes("[No payload data.]\r\n");
            if (PayloadData != null)
            {
                payloadBytes = PayloadData;
            }


            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<byte, byte[]> keyvalue in typeAndValues)
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
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TFCombination   : {1:x} ({0})\r\n", TFCombination.ToString(System.Globalization.CultureInfo.InvariantCulture), TFCombination) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PackageNumber   : {1:x} ({0})\r\n", PackageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), PackageNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionID       : {1:x} ({0})\r\n", SessionID.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionID) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TLV({0})          : {1}\r\n", typeAndValues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), sb.ToString());

            debugLine +=
            "{\r\n" +
                Encoding.UTF8.GetString(payloadBytes) +
            "}\r\n"; //+

            //String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer          : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
            return debugLine;
        }
    }
};
