#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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

    /// <summary>
    /// Handler for YIM messages
    /// </summary>
    public class YIMMessageHandler : SBMessageHandler
    {
        protected YIMMessageHandler()
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
        public override void Invite(string contact)
        {
            Contacts.Add(contact, NSMessageHandler.ContactList[contact, ClientType.EmailMember]);
            OnContactJoined(NSMessageHandler.ContactList[contact, ClientType.EmailMember]);
        }

        public override void SendNudge()
        {
            IEnumerator iemu = Contacts.Keys.GetEnumerator();
            iemu.MoveNext();

            YIMMessage nsMessage = new YIMMessage("UUM", new string[] { (string)(iemu.Current), "32", "3" });
            MSGMessage msgMessage = new MSGMessage();

            msgMessage.MimeHeader["Content-Type"] = "text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n\r\n\r\n";

            nsMessage.InnerMessage = msgMessage;
            // send it over the network
            MessageProcessor.SendMessage(nsMessage);
        }

        public override void SendTextMessage(TextMessage message)
        {
            IEnumerator iemu = Contacts.Keys.GetEnumerator();
            iemu.MoveNext();

            YIMMessage nsMessage = new YIMMessage("UUM", new string[] { (string)(iemu.Current), "32", "1" });
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

            YIMMessage nsMessage = new YIMMessage("UUM", new string[] { (string)(iemu.Current), "32", "2" });
            MSGMessage msgMessage = new MSGMessage();

            msgMessage.MimeHeader["Content-Type"] = "text/x-msmsgscontrol";
            msgMessage.MimeHeader["TypingUser"] = NSMessageHandler.Owner.Mail;

            nsMessage.InnerMessage = msgMessage;
            // send it over the network
            MessageProcessor.SendMessage(nsMessage);
        }

        public override void SendEmoticonDefinitions(ArrayList emoticons, EmoticonType icontype)
        {
            throw new MSNPSharpException("Function not support.You cannot send custom emoticons to a Yahoo Messenger user.");
        }

        #endregion

        public override void Close()
        {
            IEnumerator ienum = Contacts.Keys.GetEnumerator();
            ienum.MoveNext();
            OnContactLeft(NSMessageHandler.ContactList[(string)(ienum.Current), ClientType.EmailMember]);
            OnSessionClosed();
            NSMessageHandler.MessageProcessor.UnregisterHandler(this);
            lock (NSMessageHandler.SwitchBoards)
                NSMessageHandler.SwitchBoards.Remove(this);

        }

        public override void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            try
            {
                if (message.InnerMessage == null)
                    return;
                YIMMessage nsMessage = (YIMMessage)(message.InnerMessage.ParentMessage);

                switch (nsMessage.Command)
                {
                    case "UBM":
                        {
                            OnUBMReceived(nsMessage);
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


        protected virtual void OnUBMReceived(YIMMessage message)
        {
            MSGMessage sbMSGMessage = message.InnerMessage;

            if (!NSMessageHandler.ContactList.HasContact(message.CommandValues[0].ToString(), ClientType.EmailMember))
            {
                return;
            }
            Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString(), ClientType.EmailMember);
            if (sbMSGMessage.MimeHeader.ContainsKey("Content-Type"))
            {
                switch (sbMSGMessage.MimeHeader["Content-Type"].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                        // make sure we don't parse the rest of the message in the next loop											
                        OnUserTyping(NSMessageHandler.ContactList.GetContact(sbMSGMessage.MimeHeader["TypingUser"]));
                        break;

                    /*case "text/x-msmsgsinvite":
                        break;
					
                    case "application/x-msnmsgrp2p":
                        break;*/

                    case "text/x-msnmsgr-datacast":
                        OnNudgeReceived(contact);
                        break;

                    default:
                        if (sbMSGMessage.MimeHeader["Content-Type"].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            TextMessage msg = new TextMessage();
                            msg.CreateFromMessage(sbMSGMessage);
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
    }
};
