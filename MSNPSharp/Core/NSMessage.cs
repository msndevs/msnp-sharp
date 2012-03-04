#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp.Core
{
    using MSNPSharp;
    using MSNPSharp.P2P;

    [Serializable()]
    public class NSMessage : MSNMessage, ICloneable
    {
        public NSMessage()
            : base()
        {
        }

        public NSMessage(string command, ArrayList commandValues)
            : base(command, commandValues)
        {
        }

        public NSMessage(string command, string[] commandValues)
            : base(command, new ArrayList(commandValues))
        {
        }

        public NSMessage(string command)
            : base()
        {
            Command = command;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            switch (Command)
            {
                case "PNG":
                    return System.Text.Encoding.UTF8.GetBytes("PNG\r\n");

                default:
                    return base.GetBytes();

            }
        }

        public override void ParseBytes(byte[] data)
        {
            base.ParseBytes(data);

            if (InnerBody != null)
            {
                switch (Command)
                {
                    case "MSG":
                        ParseMSGMessage(this);
                        break;
                    case "SDG":
                        ParseSDGMessage(this);
                        break;
                    default:
                        ParseTextPayloadMessage(this);
                        break;
                }
            }
        }

        private NetworkMessage ParseMSGMessage(NSMessage message)
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.CreateFromParentMessage(message);

            string mime = mimeMessage.MimeHeader[MIMEContentHeaders.ContentType].ToString();

            if (mime.IndexOf("text/x-msmsgsprofile") >= 0)
            {
                //This is profile, the content is nothing.
            }
            else
            {
                MimeMessage innerMimeMessage = new MimeMessage(false);
                innerMimeMessage.CreateFromParentMessage(mimeMessage);
            }

            return message;
        }

        private NetworkMessage ParseTextPayloadMessage(NSMessage message)
        {
            TextPayloadMessage txtPayLoad = new TextPayloadMessage(string.Empty);
            txtPayLoad.CreateFromParentMessage(message);
            return message;
        }

        private MultiMimeMessage ParseSDGCustomEmoticonMessage(MultiMimeMessage multiMimeMessage)
        {
            EmoticonMessage emoticonMessage = new EmoticonMessage();
            emoticonMessage.CreateFromParentMessage(multiMimeMessage);

            emoticonMessage.EmoticonType = multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType] == "text/x-mms-animemoticon" ?
                EmoticonType.AnimEmoticon : EmoticonType.StaticEmoticon;

            return multiMimeMessage;
        }

        private NetworkMessage ParseSDGMessage(NSMessage nsMessage)
        {
            MultiMimeMessage multiMimeMessage = new MultiMimeMessage();
            multiMimeMessage.CreateFromParentMessage(nsMessage);

            if (multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.MessageType))
            {
                switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString())
                {
                    default:
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "[ParseSDGMessage] Cannot parse this type of SDG message: \r\n" + multiMimeMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString() +
                            "\r\n\r\nMessage Body: \r\n\r\n" + multiMimeMessage.ToDebugString());
                        break;

                    case MessageTypes.Nudge:
                    case MessageTypes.ControlTyping:
                    case MessageTypes.Wink:
                    case MessageTypes.SignalCloseIMWindow:
                        // Pure Text body, nothing to parse.
                        ParseSDGTextPayloadMessage(multiMimeMessage);
                        break;

                    case MessageTypes.Text:
                        // Set the TextMessage as its InnerMessage.
                        ParseSDGTextMessage(multiMimeMessage);
                        break;

                    case MessageTypes.CustomEmoticon:
                        // Set the EmoticonMessage as its InnerMessage.
                        ParseSDGCustomEmoticonMessage(multiMimeMessage);
                        break;

                    case MessageTypes.SignalP2P:
                        // Add the SLPMessage as its InnerMessage.
                        ParseSDGP2PSignalMessage(multiMimeMessage);
                        break;

                    case MessageTypes.Data:
                        //OnSDGDataMessageReceived(multiMimeMessage, sender, by, routingInfo);
                        break;
                }
            }

            return nsMessage;
        }

        private MultiMimeMessage ParseSDGTextPayloadMessage(MultiMimeMessage multiMimeMessage)
        {
            TextPayloadMessage textPayloadMessage = new TextPayloadMessage();
            textPayloadMessage.CreateFromParentMessage(multiMimeMessage);
            return multiMimeMessage;
        }

        private MultiMimeMessage ParseSDGTextMessage(MultiMimeMessage multiMimeMessage)
        {
            TextMessage txtMessage = new TextMessage();
            txtMessage.CreateFromParentMessage(multiMimeMessage);

            return multiMimeMessage;
        }

        private MultiMimeMessage ParseSDGP2PSignalMessage(MultiMimeMessage multiMimeMessage)
        {
            SLPMessage slpMessage = SLPMessage.Parse(multiMimeMessage.InnerBody);
            slpMessage.CreateFromParentMessage(multiMimeMessage);

            return multiMimeMessage;
        }

        #region ICloneable

        object ICloneable.Clone()
        {
            NSMessage messageClone = new NSMessage();
            messageClone.ParseBytes(GetBytes());

            if (messageClone.InnerBody == null && InnerBody != null)
            {
                messageClone.InnerBody = new byte[InnerBody.Length];
                Buffer.BlockCopy(InnerBody, 0, messageClone.InnerBody, 0, InnerBody.Length);
            }

            return messageClone;
        }

        #endregion
    }
};
