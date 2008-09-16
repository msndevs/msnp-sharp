using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace MSNPSharp.IO
{
    /// <summary>
    /// Object serializer/deserializer class
    /// <remarks>This class was used to save/load an object into/from a hidden mcl file.
    /// Any object needs to be serialized as a hidden mcl file should derive from this class.</remarks>
    /// </summary>
    [Serializable]
    public abstract class MCLSerializer
    {
        #region Common

        [NonSerialized]
        private bool noCompress;

        [NonSerialized]
        private string fileName;

        [NonSerialized]
        NSMessageHandler nsMessageHandler;

        private string version = "1.0";

        public MCLSerializer()
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

        protected bool NoCompress
        {
            get
            {
                return noCompress;
            }
            set
            {
                noCompress = value;
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

        protected static object LoadFromFile(string filename, bool nocompress, Type targettype, NSMessageHandler handler)
        {
            object rtnobj = Activator.CreateInstance(targettype);
            if (Settings.NoSave == false && File.Exists(filename))
            {
                MCLFile file = MCLFileManager.GetFile(filename, nocompress);
                if (file.Content != null)
                {
                    MemoryStream mem = new MemoryStream(file.Content);
                    rtnobj = new XmlSerializer(targettype).Deserialize(mem);
                    mem.Close();
                }
            }

            // Subclass of MCLSerializer, set the default properties
            if (targettype.IsSubclassOf(typeof(MCLSerializer)))
            {
                ((MCLSerializer)rtnobj).NoCompress = nocompress;
                ((MCLSerializer)rtnobj).FileName = filename;
                ((MCLSerializer)rtnobj).NSMessageHandler = handler;
            }
            return rtnobj;
        }

        /// <summary>
        /// Serialize and save the class into a file.
        /// </summary>
        public virtual void Save()
        {
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
            if (Settings.NoSave)
                return;

            XmlSerializer ser = new XmlSerializer(this.GetType());
            MemoryStream ms = new MemoryStream();
            ser.Serialize(ms, this);
            MCLFile file = MCLFileManager.GetFile(filename, noCompress);
            file.Content = ms.ToArray();
            MCLFileManager.Save(file, true);
            ms.Close();
        }

        #endregion
    }
};
