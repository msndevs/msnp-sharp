#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MSNPSharp.IO
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MCLFileStruct
    {
        public byte[] content;
    }

    /// <summary>
    /// File class used to save userdata.
    /// </summary>
    public sealed class MCLFile
    {
        public static readonly byte[] MclBytes = new byte[] { (byte)'m', (byte)'c', (byte)'l' };
        private string fileName = String.Empty;
        private bool noCompression = false;
        private byte[] uncompressData;

        /// <summary>
        /// Opens filename and fills the <see cref="Content"/> with uncompressed data.
        /// </summary>
        /// <param name="filename">Name of file</param>
        /// <param name="nocompress">Use of compression when SAVING file.</param>
        public MCLFile(string filename, bool nocompress)
        {
            fileName = filename;
            noCompression = nocompress;
            uncompressData = GetStruct().content;
        }

        #region Public method
        public void Save(string filename)
        {
            SaveImpl(filename, FillFileStruct(uncompressData));
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
        /// Name of file
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        /// <summary>
        /// Uncompressed (XML) data of file
        /// </summary>
        public byte[] Content
        {
            get
            {
                return uncompressData;
            }
            set
            {
                uncompressData = value;
            }
        }

        /// <summary>
        /// Don't use compression when SAVING.
        /// </summary>
        public bool NoCompression
        {
            get
            {
                return noCompression;
            }
        }

        #endregion

        #region Private

        private void SaveImpl(string filename, byte[] content)
        {
            fileName = filename;
            if (content == null)
                return;

            if (File.Exists(filename))
                File.SetAttributes(filename, FileAttributes.Normal);

            if (!noCompression)
            {
                byte[] byt = new byte[content.Length + MclBytes.Length];
                Array.Copy(MclBytes, byt, MclBytes.Length);
                Array.Copy(content, 0, byt, MclBytes.Length, content.Length);
                File.WriteAllBytes(filename, byt);
            }
            else
            {
                File.WriteAllBytes(filename, content);
            }
        }


        private byte[] Compress(byte[] buffer)
        {
            MemoryStream destms = new MemoryStream();
            GZipStream zipsm = new GZipStream(destms, CompressionMode.Compress, true);
            zipsm.Write(buffer, 0, buffer.Length);
            zipsm.Close();
            return destms.ToArray();
        }

        private byte[] Decompress(byte[] compresseddata)
        {
            MemoryStream destms = new MemoryStream();
            MemoryStream ms = new MemoryStream(compresseddata);
            ms.Position = 0;

            int read;
            byte[] decompressdata = new byte[8192];
            GZipStream zipsm = new GZipStream(ms, CompressionMode.Decompress, true);

            while ((read = zipsm.Read(decompressdata, 0, decompressdata.Length)) > 0)
            {
                destms.Write(decompressdata, 0, read);
            }

            zipsm.Close();
            return destms.ToArray();
        }

        /// <summary>
        /// Compress the data if NoCompression is set to false.
        /// </summary>
        /// <param name="content">Uncompressed data</param>
        /// <returns></returns>
        private byte[] FillFileStruct(byte[] content)
        {
            if (noCompression)
                return content;

            MCLFileStruct mclstruct;
            mclstruct.content = (content != null) ? Compress(content) : null;

            return mclstruct.content;
        }

        /// <summary>
        /// Decompress the file and fill the MCLFileStruct struct
        /// </summary>
        /// <returns></returns>
        private MCLFileStruct GetStruct()
        {
            MCLFileStruct mclfile = new MCLFileStruct();
            if (File.Exists(fileName))
            {
                FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read);
                try
                {
                    byte[] mcl = new byte[MclBytes.Length];
                    if (MclBytes.Length == fs.Read(mcl, 0, mcl.Length))
                    {
                        MemoryStream ms = new MemoryStream();
                        bool ismcl = true;
                        for (int i = 0; i < MclBytes.Length; i++)
                        {
                            if (MclBytes[i] != mcl[i])
                            {
                                ismcl = false;
                                break;
                            }
                        }

                        if (!ismcl)
                        {
                            Debug.Assert(mcl[0] == (byte)'<' && mcl[1] == (byte)'?' && mcl[2] == (byte)'x', "Not valid xml file");
                            ms.Write(mcl, 0, mcl.Length);
                        }

                        int read;
                        byte[] tmp = new byte[8192];
                        while ((read = fs.Read(tmp, 0, tmp.Length)) > 0)
                        {
                            ms.Write(tmp, 0, read);
                        }

                        mclfile.content = ismcl ? Decompress(ms.ToArray()) : ms.ToArray();

                        ms.Close();
                    }
                }
                catch (Exception exception)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, exception.Message, GetType().Name);
                    return new MCLFileStruct();
                }
                finally
                {
                    fs.Close();
                }
            }
            return mclfile;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Content);
        }
        #endregion
    }
};
