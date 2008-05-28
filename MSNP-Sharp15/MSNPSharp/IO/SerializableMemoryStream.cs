using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace MSNPSharp.IO
{
    [XmlRoot("Stream"), Serializable]
    public class SerializableMemoryStream : MemoryStream,IXmlSerializable
    {
        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;
            reader.Read();
            XmlSerializer valueSerializer = new XmlSerializer(typeof(byte[]));
            byte[] byt = (byte[])valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            Write(byt, 0, byt.Length);
            Flush();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer valueSerializer = new XmlSerializer(typeof(byte[]));
            // I just can't imagine what will happen if the stream size exceed 1 mega byte
            if (ToArray() != null)
            {
                valueSerializer.Serialize(writer, ToArray());
            }
        }

        #endregion
    }
}
