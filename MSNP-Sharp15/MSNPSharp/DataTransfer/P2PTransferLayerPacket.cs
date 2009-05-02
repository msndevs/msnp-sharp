using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.Core;
using System.IO;

namespace MSNPSharp.DataTransfer
{
    public class P2PTransferLayerPacket : NetworkMessage
    {
        private byte headerLength;
        private byte operationCode;
        private UInt32 sequenceNumber;
        private P2PDataLayerPacket dataPacket;
        private UInt32 footer;

        private bool needACK = false;
        private UInt32 acksequenceNumber;


        public byte HeaderLength
        {
            get { return headerLength; }
        }

        public byte OperationCode
        {
            get { return operationCode; }
            set { operationCode = value; }
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
            get { return sequenceNumber; }
            set { sequenceNumber = value; }
        }

        public bool NeedACK
        {
            get
            {
                OperationCode = 0x0;
                return needACK;
            }
            set
            {
                OperationCode = 0x2;
                needACK = value;
            }
        }

        public UInt32 AcksequenceNumber
        {
            get { return acksequenceNumber; }
            set { acksequenceNumber = value; }
        }

        public P2PDataLayerPacket DataPacket
        {
            get { return dataPacket; }
            set { dataPacket = value; }
        }

        public UInt32 Footer
        {
            get { return footer; }
            set { footer = value; }
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
            headerLength = 0x08;
            if (AcksequenceNumber != 0)
            {
                headerLength = 0x10;
            }

            if (NeedACK)
            {
                OperationCode = 0x2;
            }

            byte[] data = new byte[HeaderLength + PayloadLength + 4];


            MemoryStream memStream = new MemoryStream(data, true);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(HeaderLength);
            writer.Write(OperationCode);
            writer.Write(P2PMessage.ToBigEndian(PayloadLength));
            writer.Write(P2PMessage.ToBigEndian(SequenceNumber));

            if (AcksequenceNumber != 0)
            {
                writer.Write((byte)0x2);
                writer.Write((byte)0x4);
                writer.Write(P2PMessage.ToBigEndian(AcksequenceNumber));
            }

            memStream.Seek(HeaderLength, SeekOrigin.Begin);

            if (PayloadLength > 0)
            {
                writer.Write(DataPacket.GetBytes());
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
            OperationCode = reader.ReadByte();

            if (OperationCode == 0x2)
            {
                NeedACK = true;
            }

            ushort payloadLen = P2PMessage.ToBigEndian(reader.ReadUInt16());
            SequenceNumber = P2PMessage.ToBigEndian(reader.ReadUInt32());

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

            if (PayloadLength > 0)
            {
                DataPacket = new P2PDataLayerPacket(reader.ReadBytes(payloadLen));
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
                    break;
                case 2:
                    if (L == 4)
                    {
                        AcksequenceNumber = P2PMessage.ToBigEndian(reader.ReadUInt32());
                    }
                    break;
            }
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

            string debugLine =
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "HeaderLength        : {1:x} ({0})\r\n", HeaderLength.ToString(System.Globalization.CultureInfo.InvariantCulture), HeaderLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "OperationCode       : {1:x} ({0})\r\n", OperationCode.ToString(System.Globalization.CultureInfo.InvariantCulture), OperationCode) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "PayloadLength       : {1:x} ({0})\r\n", PayloadLength.ToString(System.Globalization.CultureInfo.InvariantCulture), PayloadLength) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "SequenceNumber      : {1:x} ({0})\r\n", SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), SequenceNumber) +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "AcksequenceNumber   : {1:x} ({0})\r\n", AcksequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), AcksequenceNumber) +
                "{\r\n" +
                dataString +
                "}\r\n" +
                String.Format(System.Globalization.CultureInfo.InvariantCulture, "Footer              : {1:x} ({1})\r\n", Footer.ToString(System.Globalization.CultureInfo.InvariantCulture), Footer);
            return debugLine;
        }
    }
}
