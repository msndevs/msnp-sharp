using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.Core;
using System.IO;

namespace MSNPSharp.DataTransfer
{
    public class P2PDataLayerPacket : NetworkMessage
    {
        private byte tFCombination;
        private ushort packageNumber;
        private UInt32 sessionID;
        private UInt64 dataRemaining;
        private byte[] payloadData;
        private UInt32 footer;

        public UInt64 DataRemaining
        {
            get { return dataRemaining; }
            set { dataRemaining = value; }
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
            byte headerlen = 0x08;
            if (TFCombination == 4 || 
                TFCombination == 5 ||
                TFCombination == 6 ||
                TFCombination == 7)
            {
                headerlen = 0x14;
            }

            byte[] data = new byte[headerlen + PayloadData.Length];


            MemoryStream memStream = new MemoryStream(data, true);
            BinaryWriter writer = new BinaryWriter(memStream);

            writer.Write(headerlen);
            writer.Write(TFCombination);
            writer.Write(P2PMessage.ToBigEndian(PackageNumber));
            writer.Write(P2PMessage.ToBigEndian(SessionID));

            if (headerlen == 0x14)
            {
                writer.Write((byte)0x1);
                writer.Write((byte)0x8);
                writer.Write(DataRemaining);
            }

            memStream.Seek(headerlen + 1, SeekOrigin.Begin);
            writer.Write(PayloadData);
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
            byte headerlen = reader.ReadByte();
            TFCombination = reader.ReadByte();
            PackageNumber = P2PMessage.ToBigEndian(reader.ReadUInt16());
            SessionID = P2PMessage.ToBigEndian(reader.ReadUInt32());
            if (headerlen - 8 > 0)  //TLVs
            {
                byte[] TLvs = new byte[headerlen - 8];
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

            if (InnerBody.Length - headerlen - 4 > 0)
            {
                PayloadData = reader.ReadBytes(InnerBody.Length - headerlen - 4);
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
    }
}
