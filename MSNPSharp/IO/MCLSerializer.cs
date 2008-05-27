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
    public abstract class MCLSerializer
    {
        #region Common

        [NonSerialized]
        private bool noCompress;

        [NonSerialized]
        private string fileName;

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

        protected static object LoadFromFile(string filename, bool nocompress,Type targettype)
        {
            object rtnobj = Activator.CreateInstance(targettype);
            if (File.Exists(filename))
            {
                MCLFile file = MCLFileManager.GetFile(filename, nocompress);
                if (file.Content != null)
                {
                    MemoryStream mem = new MemoryStream(file.Content);
                    rtnobj = new XmlSerializer(targettype).Deserialize(mem);
                    mem.Close();
                }
            }

            if (targettype.IsSubclassOf(typeof(MCLSerializer)))   //Subclass of MCLSerializer, set the default NoCompress and FileName properties
            {
                ((MCLSerializer)rtnobj).NoCompress = nocompress;
                ((MCLSerializer)rtnobj).FileName = filename;
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
}
