#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;

namespace MSNPSharp.IO
{
    /// <summary>
    /// Object serializer/deserializer class.
    /// <remarks>This class was used to save/load an object into/from a hidden mcl file.
    /// Any object needs to be serialized as a hidden mcl file should derive from this class.</remarks>
    /// </summary>
    [Serializable]
    public abstract class MCLSerializer
    {
        #region Common

        [NonSerialized]
        private MclSerialization serializationType;

        [NonSerialized]
        private string fileName;

        [NonSerialized]
        NSMessageHandler nsMessageHandler;

        [NonSerialized]
        private bool useCache;

        private string version = "1.0";

        protected MCLSerializer()
        {
        }

        protected string FileName
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

        protected MclSerialization SerializationType
        {
            get
            {
                return serializationType;
            }
            set
            {
                serializationType = value;
            }
        }

        protected NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
            set
            {
                nsMessageHandler = value;
            }
        }

        protected bool UseCache
        {
            get
            {
                return useCache;
            }
            set
            {
                useCache = value;
            }
        }

        /// <summary>
        /// The version of serialized object in the mcl file.
        /// </summary>
        [XmlAttribute("Version")]
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        protected static MCLSerializer LoadFromFile(string filename, MclSerialization st, Type targettype, NSMessageHandler handler, bool useCache)
        {
            MCLSerializer ret = (MCLSerializer)Activator.CreateInstance(targettype);
            if (Settings.NoSave == false && File.Exists(filename))
            {
                MclFile file = MclFile.Open(filename, FileAccess.Read, st, handler.Credentials.Password, useCache);
                if (file.Content != null)
                {
                    using (MemoryStream ms = new MemoryStream(file.Content))
                    {
                        ret = (MCLSerializer)new XmlSerializer(targettype).Deserialize(ms);
                    }
                }
            }

            ret.SerializationType = st;
            ret.FileName = filename;
            ret.NSMessageHandler = handler;
            ret.UseCache = useCache;

            return ret;
        }

        /// <summary>
        /// Serialize and save the class into a file.
        /// </summary>
        public virtual void Save()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Underlying data saved.", "< " + GetType() + " >");
            Save(FileName);
        }

        /// <summary>
        /// Serialize and save the class into a file.
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(string filename)
        {
            SaveToHiddenMCL(filename);
        }

        private void SaveToHiddenMCL(string filename)
        {
            if (!Settings.NoSave)
            {
                XmlSerializer ser = new XmlSerializer(this.GetType());
                MemoryStream ms = new MemoryStream();
                ser.Serialize(ms, this);
                MclFile file = MclFile.Open(filename, FileAccess.Write, SerializationType, NSMessageHandler.Credentials.Password, UseCache);
                file.Content = ms.ToArray();
                file.SaveAndHide(filename);
                ms.Close();
            }
        }

        #endregion
    }
};
