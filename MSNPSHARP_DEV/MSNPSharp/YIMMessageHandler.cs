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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using System.Diagnostics;

    /// <summary>
    /// Handler for YIM messages
    /// </summary>
    public class YIMMessageHandler :SBMessageHandler
    {
        protected internal YIMMessageHandler()
            : base()
        {
            sessionEstablished = true;
            invited = true;
        }

        #region Message sending methods

        /// <summary>
        /// Do nothing except fire OnContactJoined event and add the contact to the <see cref="Contact"/> property.
        /// </summary>
        /// <param name="contact"></param>
        public override void Invite(Contact contact)
        {
            OnContactJoined(contact);// (NSMessageHandler.ContactList[contact, ClientType.EmailMember]);
        }

        public override void SendNudge()
        {
            IEnumerator iemu = Contacts.Keys.GetEnumerator();
            iemu.MoveNext();

            YIMMessage nsMessage = new YIMMessage("UUM",
                new string[] { ((Contact)(iemu.Current)).Mail, 
                ((int)ClientType.EmailMember).ToString(), 
                 ((uint)TextMessageType.Nudge).ToString() },
                 NSMessageHandler.Credentials.MsnProtocol);

            MSGMessage msgMessage = new MSGMessage();

            msgMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n\r\n\r\n";

            nsMessage.InnerMessage = msgMessage;
            // send it over the network
            MessageProcessor.SendMessage(nsMessage);
        }

        public override void SendTextMessage(TextMessage message)
        {
            IEnumerator iemu = Contacts.Keys.GetEnumerator();
            iemu.MoveNext();

            YIMMessage nsMessage = new YIMMessage("UUM",
                new string[] { ((Contact)(iemu.Current)).Mail, 
                ((int)ClientType.EmailMember).ToString(), 
                ((uint)TextMessageType.Text).ToString()},
                NSMessageHandler.Credentials.MsnProtocol);

            MSGMessage msgMessage = new MSGMessage();

            nsMessage.InnerMessage = msgMessage;
            msgMessage.InnerMessage = message;
            // send it over the network
            MessageProcessor.SendMessage(nsMessage);
        }

        public override void SendTypingMessage()
        {
            IEnumerator iemu = Contacts.Keys.GetEnumerator();
            iemu.MoveNext();

            YIMMessage nsMessage = new YIMMessage("UUM",
                new string[] { ((Contact)(iemu.Current)).Mail, 
                ((int)ClientType.EmailMember).ToString(), 
                ((uint)TextMessageType.Typing).ToString()},
                NSMessageHandler.Credentials.MsnProtocol);

            MSGMessage msgMessage = new MSGMessage();

            msgMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msmsgscontrol";
            msgMessage.MimeHeader[MimeHeaderStrings.TypingUser] = NSMessageHandler.ContactList.Owner.Mail;

            nsMessage.InnerMessage = msgMessage;
            // send it over the network
            MessageProcessor.SendMessage(nsMessage);
        }

        public override void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            throw new MSNPSharpException("Function not support.You cannot send custom emoticons to a Yahoo Messenger user.");
        }

        #endregion

        public override void Left()
        {
            IEnumerator ienum = Contacts.Keys.GetEnumerator();
            ienum.MoveNext();
            OnContactLeft(NSMessageHandler.ContactList[((Contact)(ienum.Current)).Mail, ClientType.EmailMember]);
            Close();
        }

        public override void Close()
        {
            OnSessionClosed();
            NSMessageHandler.MessageProcessor.UnregisterHandler(this);
        }

        public override void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            try
            {
                NSMessage nsMessage = message as NSMessage;

                switch (nsMessage.Command)
                {
                    case "UBM":
                        {
                            YIMMessage yimMessage = new YIMMessage(nsMessage, NSMessageHandler.Credentials.MsnProtocol);


                            if (yimMessage.CommandValues.Count > 3)
                            {
                                //Verify receiver.
                                if (yimMessage.CommandValues[2].ToString() != NSMessageHandler.ContactList.Owner.Mail ||
                                    yimMessage.CommandValues[3].ToString() != ((int)NSMessageHandler.ContactList.Owner.ClientType).ToString())
                                {
                                    return;
                                }
                            }


                            OnUBMReceived(yimMessage);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                // notify the client of this exception
                OnExceptionOccurred(e);
                throw e;
            }
        }

        internal void ForceJoin(Contact contact)
        {
            OnContactJoined(contact);
        }

        /// <summary>
        /// Called when a UBM command has been received, this message was sent by a Yahoo Messenger client.
        /// </summary>
        /// <remarks>
        /// Indicates that the notification server has send us a UBM. This is usually a message from Yahoo Messenger.
        /// <code>UBM [Remote user account] 32 [Destination user account] [3(nudge) or 2(typing) or 1(text message)] [Length]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUBMReceived(NetworkMessage message)
        {
            YIMMessage yimMessage = message as YIMMessage;

            if (!NSMessageHandler.ContactList.HasContact(yimMessage.CommandValues[0].ToString(), ClientType.EmailMember))
            {
                return;
            }
            Contact contact = NSMessageHandler.ContactList.GetContact(yimMessage.CommandValues[0].ToString(), ClientType.EmailMember);
            if (yimMessage.InnerMessage.MimeHeader.ContainsKey(MimeHeaderStrings.Content_Type))
            {
                switch (yimMessage.InnerMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                        // make sure we don't parse the rest of the message in the next loop											
                        OnUserTyping(NSMessageHandler.ContactList.GetContact(yimMessage.InnerMessage.MimeHeader[MimeHeaderStrings.TypingUser], ClientType.EmailMember));
                        break;

                    case "text/x-msnmsgr-datacast":
                        OnNudgeReceived(contact);
                        break;

                    default:
                        if (yimMessage.InnerMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            TextMessage msg = new TextMessage();
                            msg.CreateFromMessage(yimMessage.InnerMessage);
                            OnTextMessageReceived(msg, contact);
                        }
                        break;
                }
            }
        }

        //Do nothing
        protected override void SendInitialMessage()
        {
        }

        protected override void ProcessInvitations()
        {
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
    }
};
