namespace MSNPSharpClient
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;

    [Serializable]
    [XmlRoot("Settings")]
    public class XmlSettings
    {
        public const string FileName = "./settings.xml";

        private string username = "testmsnpsharp@live.cn";
        private string password = "tstmsnpsharp";
        private string bot = "false";
        private string lastStatus = "Online";

        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        public string Bot
        {
            get
            {
                return bot;
            }
            set
            {
                bot = value;
            }
        }

        public string LastStatus
        {
            get
            {
                return lastStatus;
            }
            set
            {
                lastStatus = value;
            }
        }

        public static XmlSettings Load()
        {
            Stream stream = null;
            XmlSettings xmlSettings = new XmlSettings();
            try
            {
                stream = File.Open(FileName, FileMode.Open, FileAccess.Read);
                xmlSettings = (XmlSettings)new XmlSerializer(typeof(XmlSettings)).Deserialize(stream);
            }
            catch (FileNotFoundException)
            {
                return xmlSettings;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return xmlSettings;
        }

        public void Save()
        {
            Stream stream = null;
            try
            {
                stream = File.Open(FileName, FileMode.Create, FileAccess.Write);
                new XmlSerializer(typeof(XmlSettings)).Serialize(stream, this);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
        }
    }
};