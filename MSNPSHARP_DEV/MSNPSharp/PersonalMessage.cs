#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Web;
using System.Xml;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using System.Globalization;

    [Serializable]
    public class PersonalMessage
    {
        private string userTileLocation = string.Empty;
        private string friendlyName = string.Empty;
        private string rum = string.Empty;
        private string personalMessage = string.Empty;
        private string ddp = string.Empty;
        private string scene = string.Empty;
        private Color colorScheme = Color.Empty;
        private string signatureSound = string.Empty;

        private MediaType mediaType = MediaType.None;
        private string currentMedia = string.Empty;
        private string appName = string.Empty;
        private string format = string.Empty;
        private string[] content;

        public PersonalMessage()
        {
        }

        public PersonalMessage(string personalmsg)
        {
            Message = personalmsg;
        }

        public PersonalMessage(string personalmsg, MediaType mediatype, string[] currentmediacontent)
        {
            Message = personalmsg;
            mediaType = mediatype;
            content = currentmediacontent;
            Format = "{0}";
        }

        public PersonalMessage(string personalmsg, MediaType mediatype, string[] currentmediacontent, string contentformat)
        {
            Message = personalmsg;
            mediaType = mediatype;
            content = currentmediacontent;
            Format = contentformat;
        }

        internal PersonalMessage(XmlNodeList nodeList)
        {
            try
            {
                Handle(nodeList);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLineIf(Settings.TraceSwitch.TraceError, exception.Message, GetType().Name);
            }
        }

        public string UserTileLocation
        {
            get
            {
                return userTileLocation;
            }
            set
            {
                userTileLocation = value;
            }
        }

        public string FriendlyName
        {
            get
            {
                return friendlyName;
            }
            set
            {
                friendlyName = value;
            }
        }

        public string Message
        {
            get
            {
                return personalMessage;
            }
            set
            {
                personalMessage = value;
                if(MediaType != MediaType.None)
                    MediaType = MediaType.None;
            }
        }

        public string RUM
        {
            get
            {
                return rum;
            }
            set
            {
                rum = value;
            }
        }

        public string DDP
        {
            get
            {
                return ddp;
            }
            set
            {
                ddp = value;
            }
        }

        public string Scene
        {
            get
            {
                return scene;
            }
            set
            {
                scene = value;
            }
        }


        public Color ColorScheme
        {
            get
            {
                return colorScheme;
            }
            set
            {
                colorScheme = value;
            }
        }

        public string SignatureSound
        {
            get
            {
                return signatureSound;
            }

            private set
            {
                signatureSound = value;
            }
        }

        public MediaType MediaType
        {
            get
            {
                return mediaType;
            }

            private set
            {
                mediaType = value;
            }
        }

        public string AppName
        {
            get
            {
                return appName;
            }
        }

        public string Format
        {
            get
            {
                return format;
            }

            set
            {
                format = value;
            }
        }

        //This is used in conjunction with format
        public string[] CurrentMediaContent
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
            }
        }
        public string CurrentMedia
        {
            get
            {
                string currentmedia = String.Empty;

                if (mediaType != MediaType.None)
                {
                    foreach (string media in content)
                    {
                        currentmedia = currentmedia + media + @"\0";
                    }

                    if (String.IsNullOrEmpty(Format))
                        Format = "{0}";

                    currentmedia = @"\0" + mediaType.ToString() + @"\01\0" + Format + @"\0" + currentmedia;
                }

                return currentmedia;
            }
        }

        public string Payload
        {
            get
            {
                // I think we need a serializer for PersonalMessage.
                XmlDocument xdoc = new XmlDocument();
                XmlNode rootNode = xdoc.CreateElement("root");
                xdoc.AppendChild(rootNode);

                XmlNode userTileLocationNode = xdoc.CreateElement("UserTileLocation");
                userTileLocationNode.InnerText = UserTileLocation;
                rootNode.AppendChild(userTileLocationNode);

                XmlNode friendlyNameNode = xdoc.CreateElement("FriendlyName");
                friendlyNameNode.InnerText = FriendlyName;
                rootNode.AppendChild(friendlyNameNode);

                XmlNode rumNode = xdoc.CreateElement("RUM");
                rumNode.InnerText = RUM;
                rootNode.AppendChild(rumNode);

                XmlNode personalMessageNode = xdoc.CreateElement("PSM");
                personalMessageNode.InnerText = Message;
                rootNode.AppendChild(personalMessageNode);

                XmlNode ddpNode = xdoc.CreateElement("DDP");
                ddpNode.InnerText = DDP;
                rootNode.AppendChild(ddpNode);

                XmlNode colorSchemeNode = xdoc.CreateElement("ColorScheme");
                colorSchemeNode.InnerText = ColorScheme.ToArgb().ToString(CultureInfo.InvariantCulture);
                rootNode.AppendChild(colorSchemeNode);

                XmlNode sceneNode = xdoc.CreateElement("Scene");
                sceneNode.InnerText = Scene;
                rootNode.AppendChild(sceneNode);

                XmlNode signatureSoundNode = xdoc.CreateElement("SignatureSound");
                signatureSoundNode.InnerText = HttpUtility.UrlEncode(SignatureSound);
                rootNode.AppendChild(signatureSoundNode);

                XmlNode currentMediaNode = xdoc.CreateElement("CurrentMedia");
                currentMediaNode.InnerText = CurrentMedia;
                rootNode.AppendChild(currentMediaNode);

                return rootNode.InnerXml;
            }
        }

        public string ToDebugString()
        {
            return Payload;
        }

        public override string ToString()
        {
            return Payload;
        }

        public void SetListeningAlbum(string artist, string song, string album)
        {
            this.mediaType = MediaType.Music;
            this.content = new string[] { artist, song, album, String.Empty };
        }

        private void Handle(XmlNodeList nodeList)
        {
            if (nodeList == null || nodeList.Count == 0)
                return;

            foreach (XmlNode node in nodeList)
            {
                if (!String.IsNullOrEmpty(node.InnerText))
                {
                    // REMARK: All the MSNObject must use MSNObject.GetDecodeString(string) to get the decoded conext.
                    switch (node.Name)
                    {
                        case "UserTileLocation":
                            UserTileLocation = MSNObject.GetDecodeString(node.InnerText);
                            break;

                        case "FriendlyName":
                            FriendlyName = node.InnerText;
                            break;

                        case "RUM":
                            RUM = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "PSM":
                            Message = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "DDP":
                            DDP = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "ColorScheme":
                            ColorScheme = ColorTranslator.FromOle(int.Parse(node.InnerText));
                            break;

                        case "Scene":
                            Scene = MSNObject.GetDecodeString(node.InnerText);
                            break;

                        case "SignatureSound":
                            SignatureSound = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "CurrentMedia":

                            string mediaString = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);

                            if (mediaString.Length > 0)
                            {
                                string[] vals = mediaString.Split(new string[] { @"\0" }, StringSplitOptions.None);

                                if (!String.IsNullOrEmpty(vals[0]))
                                    appName = vals[0];

                                switch (vals[1])
                                {
                                    case "Music":
                                        MediaType = MediaType.Music;
                                        break;
                                    case "Games":
                                        MediaType = MediaType.Games;
                                        break;
                                    case "Office":
                                        MediaType = MediaType.Office;
                                        break;
                                }

                                /*
                                0 
                                1 Music
                                2 1
                                3 {0} - {1}
                                4 Evanescence
                                5 Call Me When You're Sober
                                6 Album
                                7 WMContentID
                                8 
                                */
                                //vals[2] = Enabled/Disabled

                                Format = vals[3];

                                int size = vals.Length - 4;

                                CurrentMediaContent = new String[size];

                                for (int i = 0; i < size; i++)
                                    CurrentMediaContent[i] = vals[i + 4];
                            }
                            break;
                    }
                }
            }
        }

        public static bool operator ==(PersonalMessage psm1, PersonalMessage psm2)
        {
            if (((object)psm1) == null && ((object)psm2) == null)
                return true;

            if (((object)psm1) == null || ((object)psm2) == null)
                return false;

            return psm1.Payload.Equals(psm2.Payload);
        }

        public static bool operator !=(PersonalMessage psm1, PersonalMessage psm2)
        {
            return !(psm1 == psm2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            return this.Payload == ((PersonalMessage)obj).Payload;
        }

        public override int GetHashCode()
        {
            return Payload.GetHashCode();
        }
    }
};
