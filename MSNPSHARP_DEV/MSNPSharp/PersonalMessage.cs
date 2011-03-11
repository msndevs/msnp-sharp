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
        private string signatureSound;

        private MediaType mediaType = MediaType.None;
        private string currentMedia = string.Empty;
        private string appName;
        private string format;
        private string[] content;

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
            }
        }

        public string Rum
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

        public string Ddp
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
        }

        public MediaType MediaType
        {
            get
            {
                return mediaType;
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
                StringBuilder pload = new StringBuilder();

                if (!String.IsNullOrEmpty(userTileLocation))
                {
                    pload.Append("<UserTileLocation>");
                    pload.Append(MSNHttpUtility.XmlEncode(userTileLocation));
                    pload.Append("</UserTileLocation>");
                }

                if (!String.IsNullOrEmpty(friendlyName))
                {
                    pload.Append("<FriendlyName>");
                    pload.Append(MSNHttpUtility.XmlEncode(friendlyName));
                    pload.Append("</FriendlyName>");
                }

                if (!String.IsNullOrEmpty(rum))
                {
                    pload.Append("<RUM>");
                    pload.Append(MSNHttpUtility.XmlEncode(rum));
                    pload.Append("</RUM>");
                }

                if (!String.IsNullOrEmpty(personalMessage))
                {
                    pload.Append("<PSM>");
                    pload.Append(MSNHttpUtility.XmlEncode(personalMessage));
                    pload.Append("</PSM>");
                }

                if (!String.IsNullOrEmpty(ddp))
                {
                    pload.Append("<DDP>");
                    pload.Append(MSNHttpUtility.XmlEncode(ddp));
                    pload.Append("</DDP>");
                }

                if (colorScheme != Color.Empty)
                {
                    pload.Append("<ColorScheme>");
                    pload.Append(MSNHttpUtility.XmlEncode(colorScheme.ToArgb().ToString()));
                    pload.Append("</ColorScheme>");
                }

                if (!String.IsNullOrEmpty(scene))
                {
                    pload.Append("<Scene>");
                    pload.Append(MSNHttpUtility.XmlEncode(scene));
                    pload.Append("</Scene>");
                }

                if (!String.IsNullOrEmpty(signatureSound))
                {
                    pload.Append("<SignatureSound>");
                    pload.Append(MSNHttpUtility.XmlEncode(signatureSound));
                    pload.Append("</SignatureSound>");
                }

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
                if (!String.IsNullOrEmpty(currentmedia))
                {
                    pload.Append("<CurrentMedia>");
                    pload.Append(MSNHttpUtility.XmlEncode(currentmedia));
                    pload.Append("</CurrentMedia>");
                }

                return pload.ToString();
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

        private void Handle(XmlNodeList nodeList)
        {
            if (nodeList == null || nodeList.Count == 0)
                return;

            foreach (XmlNode node in nodeList)
            {
                if (!String.IsNullOrEmpty(node.InnerText))
                {
                    switch (node.Name)
                    {
                        case "UserTileLocation":
                            userTileLocation = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "FriendlyName":
                            friendlyName = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "RUM":
                            rum = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "PSM":
                            personalMessage = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "DDP":
                            ddp = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "ColorScheme":
                            colorScheme = ColorTranslator.FromOle(int.Parse(node.InnerText));
                            break;

                        case "Scene":
                            scene = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "SignatureSound":
                            signatureSound = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
                            break;

                        case "CurrentMedia":

                            currentMedia = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);

                            if (currentMedia.Length > 0)
                            {
                                string[] vals = currentMedia.Split(new string[] { @"\0" }, StringSplitOptions.None);

                                if (vals[0] != "")
                                    appName = vals[0];

                                switch (vals[1])
                                {
                                    case "Music":
                                        mediaType = MediaType.Music;
                                        break;
                                    case "Games":
                                        mediaType = MediaType.Games;
                                        break;
                                    case "Office":
                                        mediaType = MediaType.Office;
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

                                format = vals[3];

                                int size = vals.Length - 4;

                                content = new String[size];

                                for (int i = 0; i < size; i++)
                                    content[i] = vals[i + 4];
                            }
                            break;


                    }
                }
            }
        }
    }
};