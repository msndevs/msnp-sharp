#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace MSNPSharp.Core
{
    /// <summary>
    /// Represents a single NOT or IPG message.
    /// </summary>
    /// <remarks>
    /// These messages are receid from, and send to, a nameserver. NOT messages are rececived for MSN-Calendar or MSN-Alert notifications.
    /// IPG commands are received/send to exchange pager (sms) messages.
    ///	</remarks>
    [Serializable()]
    public class NotificationMessage : MSNMessage
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationMessage()
        {
        }

        /// <summary>
        /// Constructs a NotificationMessage from the inner body contents of the specified message object.
        /// This will also set the InnerMessage property of the message object to the newly created NotificationMessage.
        /// </summary>
        public NotificationMessage(NetworkMessage message)
        {
            ParseBytes(message.InnerBody);
            message.InnerMessage = this;
        }
        /*	Example notification
         * <NOTIFICATION ver="1" siteid="111100200" siteurl="http://calendar.msn.com" id="1">\r\n
          <TO pid="0x00060000:0x81ee5a43" name="example@passport.com" />\r\n
          <MSG pri="" id="1">\r\n
            <ACTION url="/calendar/isapi.dll?request=action&operation=modify&objectID=1&uicode1=modifyreminder&locale=2052"/>\r\n
            <SUBSCR url="/calendar/isapi.dll?request=action&operation=modify&objectID=1&uicode1=modifyreminder&locale=2052"/><CAT id="111100201" />\r\n
            <BODY lang="2052" icon="/En/img/calendar.png">\r\n
              <TEXT>goto club 7. 2002 21:15 - 22:15 </TEXT>\r\n
            </BODY>\r\n
          </MSG>\r\n
        </NOTIFICATION>\r\n
        */

        #region Private


        /// <summary>
        /// </summary>
        NotificationType notificationType;

        private int id;
        private int siteId;
        private string siteUrl;

        private string receiverAccount;
        private string receiverOfflineMail;
        private string receiverMemberIdLow;
        private string receiverMemberIdHigh;

        private string senderAccount;
        private string senderMemberIdLow;
        private string senderMemberIdHigh;

        private string sendDevice;

        private int messageId;

        private string pri;

        private string actionUrl;
        private string subcriptionUrl;

        private string catId = "110110001";

        private string language;

        private string iconUrl;

        private string text;
        private string offlineText;

        #endregion

        #region Public

        /// <summary>
        /// Creates a xml message based on the data in the object. It is used before the message is send to the server.
        /// </summary>
        protected virtual XmlDocument CreateXmlMessage()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("NOTIFICATION");
            root.Attributes.Append(doc.CreateAttribute("ver")).Value = ((int)notificationType).ToString();
            root.Attributes.Append(doc.CreateAttribute("id")).Value = id.ToString();
            if (siteId > 0)
                root.Attributes.Append(doc.CreateAttribute("siteid")).Value = siteId.ToString();
            if (siteUrl.Length > 0)
                root.Attributes.Append(doc.CreateAttribute("siteurl")).Value = siteUrl;

            XmlElement to = doc.CreateElement("TO");
            if (receiverMemberIdLow.Length > 0 && receiverMemberIdHigh.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("pid")).Value = receiverMemberIdLow.ToString() + ":" + receiverMemberIdHigh.ToString();
            if (receiverAccount.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("name")).Value = receiverAccount;
            if (receiverOfflineMail.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("email")).Value = receiverOfflineMail;
            if (sendDevice.Length > 0)
            {
                XmlElement via = doc.CreateElement("VIA");
                via.Attributes.Append(doc.CreateAttribute("agent")).Value = sendDevice;
                to.AppendChild(via);
            }
            root.AppendChild(to);

            XmlElement from = doc.CreateElement("FROM");
            if (senderMemberIdLow.Length > 0 && senderMemberIdHigh.Length > 0)
                from.Attributes.Append(doc.CreateAttribute("pid")).Value = senderMemberIdLow.ToString() + ":" + senderMemberIdHigh.ToString();
            if (senderAccount.Length > 0)
                from.Attributes.Append(doc.CreateAttribute("name")).Value = senderAccount;
            root.AppendChild(from);

            XmlElement msg = doc.CreateElement("MSG");
            if (pri.Length > 0)
                msg.Attributes.Append(doc.CreateAttribute("pri")).Value = pri.ToString();
            if (messageId > 0)
                msg.Attributes.Append(doc.CreateAttribute("id")).Value = messageId.ToString();

            if (actionUrl.Length > 0)
            {
                XmlElement action = doc.CreateElement("ACTION");
                action.Attributes.Append(doc.CreateAttribute("url")).Value = actionUrl;
                msg.AppendChild(action);
            }
            if (subcriptionUrl.Length > 0)
            {
                XmlElement subscr = doc.CreateElement("SUBSCR");
                subscr.Attributes.Append(doc.CreateAttribute("url")).Value = subcriptionUrl;
                msg.AppendChild(subscr);
            }
            if (catId.Length > 0)
            {
                XmlElement cat = doc.CreateElement("CAT");
                cat.Attributes.Append(doc.CreateAttribute("id")).Value = catId.ToString();
                msg.AppendChild(cat);
            }

            XmlElement body = doc.CreateElement("BODY");
            if (language.Length > 0)
                body.Attributes.Append(doc.CreateAttribute("id")).Value = language;
            if (iconUrl.Length > 0)
                body.Attributes.Append(doc.CreateAttribute("icon")).Value = iconUrl;
            if (text.Length > 0)
            {
                XmlElement textEl = doc.CreateElement("TEXT");
                textEl.AppendChild(doc.CreateTextNode(text));
                body.AppendChild(textEl);
            }
            if (offlineText.Length > 0)
            {
                XmlElement emailTextEl = doc.CreateElement("EMAILTEXT");
                emailTextEl.AppendChild(doc.CreateTextNode(offlineText));
                body.AppendChild(emailTextEl);
            }
            msg.AppendChild(body);

            root.AppendChild(msg);

            doc.AppendChild(root);

            return doc;

        }

        /// <summary>
        /// Returns the command message as a byte array. This can be directly send over a networkconnection.
        /// </summary>
        /// <remarks>
        /// Remember to set the transaction ID before calling this method.
        /// Uses UTF8 Encoding.				
        /// </remarks>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            return new byte[] { 0x00 };
            //throw new MSNPSharpException("You can't send notification messages yourself. It is only possible to retrieve them.");
        }

        /// <summary>
        /// Parses incoming byte data send from the network.
        /// </summary>
        /// <param name="data">The raw message as received from the server</param>
        public override void ParseBytes(byte[] data)
        {
            if (data != null)
            {
                // retrieve the innerbody
                XmlDocument xmlDoc = new XmlDocument();

                TextReader reader = new StreamReader(new MemoryStream(data), new System.Text.UTF8Encoding(false));

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, reader.ReadToEnd(), GetType().Name);

                reader = new StreamReader(new MemoryStream(data), new System.Text.UTF8Encoding(false));
                xmlDoc.Load(reader);

                // Root node: NOTIFICATION
                XmlNode node = xmlDoc.SelectSingleNode("//NOTIFICATION");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("ver") != null)
                    {
                        notificationType = (NotificationType)int.Parse(node.Attributes.GetNamedItem("ver").Value);
                    }
                    if (node.Attributes.GetNamedItem("id") != null)
                        id = int.Parse(node.Attributes.GetNamedItem("id").Value);
                    if (node.Attributes.GetNamedItem("siteid") != null)
                        siteId = int.Parse(node.Attributes.GetNamedItem("siteid").Value);
                    if (node.Attributes.GetNamedItem("siteurl") != null)
                        siteUrl = node.Attributes.GetNamedItem("siteurl").Value;
                }

                // TO element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/TO");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pid") != null)
                    {
                        string[] values = node.Attributes.GetNamedItem("pid").Value.Split(':');
                        receiverMemberIdLow = values[0];
                        receiverMemberIdHigh = values[1];
                    }
                    if (node.Attributes.GetNamedItem("name") != null)
                        receiverAccount = node.Attributes.GetNamedItem("name").Value;
                    if (node.Attributes.GetNamedItem("email") != null)
                        receiverOfflineMail = node.Attributes.GetNamedItem("email").Value;
                }

                // VIA element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/TO/VIA");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("agent") != null)
                        sendDevice = node.Attributes.GetNamedItem("agent").Value;
                }

                // FROM element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/FROM");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pid") != null)
                    {
                        string[] values = node.Attributes.GetNamedItem("pid").Value.Split(':');
                        senderMemberIdLow = values[0];
                        senderMemberIdHigh = values[1];
                    }
                    if (node.Attributes.GetNamedItem("name") != null)
                        senderAccount = node.Attributes.GetNamedItem("name").Value;
                }

                // MSG element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pri") != null)
                        pri = node.Attributes.GetNamedItem("pri").Value;
                    if (node.Attributes.GetNamedItem("id") != null)
                        messageId = int.Parse(node.Attributes.GetNamedItem("id").Value);

                }

                // ACTION element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/ACTION");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("url") != null)
                        actionUrl = node.Attributes.GetNamedItem("url").Value;
                }

                // SUBSCR element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/SUBSCR");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("url") != null)
                        subcriptionUrl = node.Attributes.GetNamedItem("url").Value;
                }

                // CAT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/CAT");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("id") != null)
                        catId = node.Attributes.GetNamedItem("id").Value;
                }

                // BODY element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("lang") != null)
                        language = node.Attributes.GetNamedItem("lang").Value;
                    if (node.Attributes.GetNamedItem("icon") != null)
                        iconUrl = node.Attributes.GetNamedItem("icon").Value;
                }

                // TEXT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY/TEXT");
                if (node != null)
                {
                    text = node.Value;
                }

                // EMAILTEXT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY/EMAILTEXT");
                if (node != null)
                {
                    offlineText = node.Value;
                }


            }
            else
                throw new MSNPSharpException("NotificationMessage expected payload data, but not InnerBody is present.");
        }




        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return System.Text.UTF8Encoding.UTF8.GetString(this.GetBytes());
        }

        #endregion
    }
};
