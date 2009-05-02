using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.Core;
using System.IO;

namespace MSNPSharp.DataTransfer
{
    public class P2PDataLayerPacket : NetworkMessage
    {
        private byte headerLength;
        private byte tFCombination;
        private ushort packageNumber;
        private UInt32 sessionID;
        private UInt64 dataRemaining;
        private byte[] payloadData;
        private UInt32 footer;

        public byte HeaderLength
        {
            get { return headerLength; }
        }

        public byte TFCombination
        {
            get { return tFCombination; }
            set { tFCombination = value; }
        }

        public ushort PackageNumber
        {
            get { return packageNumber; }
            set { packageNumber = value; }
        }

        public UInt32 SessionID
        {
            get { return sessionID; }
            set { sessionID = value; }
        }

        public UInt64 DataRemaining
        {
            get { return dataRemaining; }
            set { dataRemaining = value; }
        }

        public byte[] PayloadData
        {
            get { return payloadData; }
            set { payloadData = value; }
        }

        public UInt32 Footer
        {
            get { return footer; }
            set { footer = value; }
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
            if (TFCombination == 4 || 
                TFCombination == 5 ||
                TFCombination == 6 ||
                TFCombination == 7)
            {
                headerLength = 0x14;
            }

            ushort payloadLength = 0;
            if (PayloadData != null)
            {
                payloadLength = (ushort)PayloadData.Length;
            }

            byte[] data = new byte[HeaderLength + payloadLength + 4];


            MemoryStream memStream = new MemoryStream(data, true);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(HeaderLength);
            writer.Write(TFCombination);
            writer.Write(P2PMessage.ToBigEndian(PackageNumber));
            writer.Write(P2PMessage.ToBigEndian(SessionID));

            if (HeaderLength == 0x14)
            {
                writer.Write((byte)0x1);
                writer.Write((byte)0x8);
                writer.Write(DataRemaining);
            }

            memStream.Seek(HeaderLength, SeekOrigin.Begin);

            if (PayloadData != null)
            {
                writer.Write(PayloadData);
            }

            writer.Write(P2PMessage.ToBigEndian(Footer));

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
            if (HeaderLength - 8 > 0)  //TLVs
            {
                byte[] TLvs = new byte[HeaderLength - 8];
                TLvs = reader.ReadBytes(TLvs.Length);
                int index = 0;
                while (TLvs.Length - (index + 1) >= 4)
                {
                    byte T = TLvs[index];
                    byte L = TLvs[index + 1];
                    byte[] V = new byte[(int)L];
                    Array.Copy(TLvs, index + 3, V, 0, (int)L);
                    index = index + 2 + L;
                    ProcessTLVData(T, L, V);
                }
            }

            if (InnerBody.Length - HeaderLength - 4 > 0)
            {
                PayloadData = reader.ReadBytes(InnerBody.Length - HeaderLength - 4);
            }

            Footer = P2PMessage.ToBigEndian(reader.ReadUInt32());

        }

        protected void ProcessTLVData(byte T, byte L, byte[] V)
        {
            MemoryStream mem = new MemoryStream(V);
            BinaryReader reader = new BinaryReader(mem);

            switch (T)
            {
                case 1:
                    if (L == 8)
                    {
                        DataRemaining = P2PMessage.ToBigEndian(reader.ReadUInt64());
                    }
                    break;
                case 2:
                    break;
            }
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

            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "HeaderLength    : {1:x} ({0})\r\n", HeaderLength.ToString(System.Globalization.CultureInfo.InvariantCulture), HeaderLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "TFCombination   : {1:x} ({0})\r\n", TFCombination.ToString(System.Globalization.CultureInfo.InvariantCulture), TFCombination) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PackageNumber   : {1:x} ({0})\r\n", PackageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), PackageNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SessionID       : {1:x} ({0})\r\n", SessionID.ToString(System.Globalization.CultureInfo.InvariantCulture), SessionID) +
                "{\r\n" +
                Encoding.UTF8.GetString(payloadBytes) +
                "}\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer              : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
            return debugLine;
        }
    }
}
