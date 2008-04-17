#region Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com>
/*
Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com> All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.Compression;

namespace MSNPSharp.IO
{
    internal struct MCLFileStruct
    {
        public Int32 len;
        public byte[] content;
    }

    public sealed class MCLFile
    {
        string fileName = "";
        int length = 0;
        byte[] uncompressData;

        public MCLFile(string filename)
        {
            fileName = filename;
            MCLFileStruct file = GetStruct(filename);
            if (file.content != null)
            {
                length = file.len;
                uncompressData = Decompress(file.content, length);
            }
        }

        #region Public method
        public void Save(string filename)
        {
            SaveImpl(filename, uncompressData);
        }

        public void Save()
        {
            Save(fileName);
        }

        /// <summary>
        /// Save the file and set its hidden attribute to true
        /// </summary>
        /// <param name="filename"></param>
        public void SaveAndHide(string filename)
        {
            SaveImpl(filename, FillFileStruct(uncompressData));
            File.SetAttributes(filename, FileAttributes.Hidden);

        }

        /// <summary>
        /// Save the file and set its hidden attribute to true
        /// </summary>
        public void SaveAndHide()
        {
            SaveAndHide(fileName);
        } 
        #endregion

        #region Properties

        /// <summary>
        /// The length of data before it was compressed
        /// </summary>
        public int Length
        {
            get { return length; }
        }

        /// <summary>
        /// The data of file
        /// </summary>
        public byte[] Content
        {
            get { return uncompressData; }
            set { uncompressData = value; }
        } 
        #endregion

        #region Private
        private void SaveImpl(string filename, byte[] content)
        {
            if (content == null)
                return;
            if (File.Exists(filename))
                File.SetAttributes(filename, FileAttributes.Normal);
            byte[] ext = new byte[3] { (byte)'m', (byte)'c', (byte)'l' };
            byte[] byt = new byte[content.Length + 3];
            Array.Copy(ext, byt, 3);
            Array.Copy(content, 0, byt, 3, content.Length);
            File.WriteAllBytes(filename, byt);
        }


        private byte[] Compress(byte[] buffer)
        {
            string str = Encoding.UTF8.GetString(buffer);
            MemoryStream destms = new MemoryStream();
            GZipStream zipsm = new GZipStream(destms, CompressionMode.Compress, true);
            zipsm.Write(buffer, 0, buffer.Length);
            zipsm.Close();
            byte[] byt = destms.ToArray();
            
            return byt;
        }

        private byte[] Decompress(byte[] compresseddata, int originallength)
        {
            byte[] decompressdata = new byte[originallength];
            MemoryStream ms = new MemoryStream(compresseddata);
            ms.Position = 0;
            GZipStream zipsm = new GZipStream(ms, CompressionMode.Decompress);
            int re=1;

            re = zipsm.Read(decompressdata, 0, originallength);

            zipsm.Close();
            return decompressdata;
        }

        /// <summary>
        /// Compress the data and add the length bits before compressed data
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private byte[] FillFileStruct(byte[] content)
        {
            MCLFileStruct mclstruct;
            mclstruct.len = content.Length;
            mclstruct.content = Compress(content);

            IntPtr structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(mclstruct.len));
            byte[] byt = new byte[Marshal.SizeOf(mclstruct.len) + mclstruct.content.Length];
            Marshal.StructureToPtr(mclstruct.len, structPtr, false);
            Marshal.Copy(structPtr, byt, 0, Marshal.SizeOf(mclstruct.len));
            Array.Copy(mclstruct.content, 0, byt, Marshal.SizeOf(mclstruct.len), mclstruct.content.Length);
            Marshal.FreeHGlobal(structPtr);  //Delete the pointer
            return byt;
        }

        /// <summary>
        /// Decompress the file and fill the MCLFileStruct struct
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private MCLFileStruct GetStruct(string filename)
        {
            if (!File.Exists(fileName))
                return new MCLFileStruct();
            byte[] bytstruct = File.ReadAllBytes(fileName);
            MCLFileStruct mclfile = new MCLFileStruct();
            try
            {

                IntPtr structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(mclfile.len));
                Marshal.Copy(bytstruct, 3, structPtr, Marshal.SizeOf(mclfile.len));
                mclfile.len = Marshal.ReadInt32(structPtr);
                byte[] content = new byte[bytstruct.Length - 3 - Marshal.SizeOf(mclfile.len)];
                Array.Copy(bytstruct, 3 + Marshal.SizeOf(mclfile.len), content, 0, content.Length);
                Marshal.FreeHGlobal(structPtr);
                mclfile.content = content;
            }
            catch (Exception)
            {
                return new MCLFileStruct();
            }
            return mclfile;
        } 
        #endregion
    }
}
