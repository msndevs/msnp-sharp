using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.Core
{
    public static class BitUtility
    {
        public static short ToInt16(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToInt16(data, startIndex);
            }
            else
            {
                return BitConverter.ToInt16(GetSwappedByteArray(data, startIndex, 2), 0);
            }
        }

        public static ushort ToUInt16(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToUInt16(data, startIndex);
            }
            else
            {
                return BitConverter.ToUInt16(GetSwappedByteArray(data, startIndex, 2), 0);
            }
        }

        public static int ToInt32(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToInt32(data, startIndex);
            }
            else
            {
                return BitConverter.ToInt32(GetSwappedByteArray(data, startIndex, 4), 0);
            }
        }

        public static uint ToUInt32(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToUInt32(data, startIndex);
            }
            else
            {
                return BitConverter.ToUInt32(GetSwappedByteArray(data, startIndex, 4), 0);
            }
        }

        public static long ToInt64(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToInt64(data, startIndex);
            }
            else
            {
                return BitConverter.ToInt64(GetSwappedByteArray(data, startIndex, 8), 0);
            }
        }

        public static ulong ToUInt64(byte[] data, int startIndex, bool dataIsLittleEndian)
        {
            if (BitConverter.IsLittleEndian == dataIsLittleEndian)
            {
                return BitConverter.ToUInt64(data, startIndex);
            }
            else
            {
                return BitConverter.ToUInt64(GetSwappedByteArray(data, startIndex, 8), 0);
            }
        }


        public static byte[] GetBytes(short val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 2);
        }

        public static byte[] GetBytes(ushort val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 2);
        }

        public static byte[] GetBytes(int val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 4);
        }

        public static byte[] GetBytes(uint val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 4);
        }

        public static byte[] GetBytes(long val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 8);
        }

        public static byte[] GetBytes(ulong val, bool littleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(val);

            if (BitConverter.IsLittleEndian == littleEndian)
                return bytes;

            return GetSwappedByteArray(bytes, 0, 8);
        }

        private static byte[] GetSwappedByteArray(byte[] data, int startIndex, int length)
        {
            byte[] swap = new byte[length];
            int endIndex = 0;
            while (--length >= 0)
            {
                swap[length] = data[startIndex + endIndex];
                endIndex++;
            }
            return swap;
        }




        public static ushort ToLittleEndian(ushort val)
        {
            if (BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        public static uint ToLittleEndian(uint val)
        {
            if (BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        public static ulong ToLittleEndian(ulong val)
        {
            if (BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        public static ushort ToBigEndian(ushort val)
        {
            if (!BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        public static uint ToBigEndian(uint val)
        {
            if (!BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        public static ulong ToBigEndian(ulong val)
        {
            if (!BitConverter.IsLittleEndian)
                return val;

            return FlipEndian(val);
        }

        private static ushort FlipEndian(ushort val)
        {
            return (ushort)
                 (((val & 0x00ff) << 8) +
                 ((val & 0xff00) >> 8));
        }


        private static uint FlipEndian(uint val)
        {
            return (uint)
                 (((val & 0x000000ff) << 24) +
                 ((val & 0x0000ff00) << 8) +
                 ((val & 0x00ff0000) >> 8) +
                 ((val & 0xff000000) >> 24));
        }

        private static ulong FlipEndian(ulong val)
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
    }
};
