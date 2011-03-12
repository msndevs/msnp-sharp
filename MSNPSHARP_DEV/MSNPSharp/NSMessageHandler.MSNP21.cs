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
using System.Net;
using System.Xml;
using System.Web;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.P2P;

    partial class NSMessageHandler
    {
        #region Public Events

        /// <summary>
        /// Occurs when any contact changes status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactStatusChanged;
        /// <summary>
        /// Occurs when any contact goes from offline status to another status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactOnline;
        /// <summary>
        /// Occurs when any contact goes from any status to offline status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactOffline;

        /// <summary>
        /// Occurs when a user is typing.
        /// </summary>
        public event EventHandler<TypingArrivedEventArgs> TypingMessageReceived;
        /// <summary>
        /// Occurs when we receive a nudge message by a user.
        /// </summary>
        public event EventHandler<NudgeArrivedEventArgs> NudgeReceived;
        /// <summary>
        /// Occurs when we receive a text message from a user.
        /// </summary>
        public event EventHandler<TextMessageArrivedEventArgs> TextMessageReceived;

        /// <summary>
        /// Occurs when we receive a emoticon from a user.
        /// </summary>
        public event EventHandler<EmoticonArrivedEventArgs> EmoticonReceived;


        /// <summary>
        /// Occurs when a multiparty chat created (locally or remotely).
        /// Joined automatically if invited remotely.
        /// </summary>
        public event EventHandler<MultipartyCreatedEventArgs> MultipartyCreated;
        /// <summary>
        /// Occurs when a contact joined the group chat.
        /// </summary>
        public event EventHandler<GroupChatParticipationEventArgs> JoinedGroupChat;
        /// <summary>
        /// Occurs when a contact left the group chat.
        /// </summary>
        public event EventHandler<GroupChatParticipationEventArgs> LeftGroupChat;

        /// <summary>
        /// Fires the <see cref="ContactStatusChanged"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnContactStatusChanged(ContactStatusChangedEventArgs e)
        {
            if (ContactStatusChanged != null)
                ContactStatusChanged(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ContactOffline"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnContactOffline(ContactStatusChangedEventArgs e)
        {
            if (ContactOffline != null)
                ContactOffline(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact.ToString() + " goes to " + e.NewStatus + " from " + e.OldStatus + (e.Via == null ? String.Empty : " via=" + e.Via.ToString()) + "\r\n", GetType().Name);

        }

        /// <summary>
        /// Fires the <see cref="ContactOnline"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnContactOnline(ContactStatusChangedEventArgs e)
        {
            if (ContactOnline != null)
                ContactOnline(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact.ToString() + " goes to " + e.NewStatus + " from " + e.OldStatus + (e.Via == null ? String.Empty : " via=" + e.Via.ToString()) + "\r\n", GetType().Name);
        }

        protected virtual void OnTypingMessageReceived(TypingArrivedEventArgs e)
        {
            if (TypingMessageReceived != null)
                TypingMessageReceived(this, e);
        }

        protected virtual void OnNudgeReceived(NudgeArrivedEventArgs e)
        {
            if (NudgeReceived != null)
                NudgeReceived(this, e);
        }

        protected virtual void OnTextMessageReceived(TextMessageArrivedEventArgs e)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, e);
        }

        protected virtual void OnEmoticonReceived(EmoticonArrivedEventArgs e)
        {
            if (EmoticonReceived != null)
                EmoticonReceived(this, e);
        }

        protected virtual void OnMultipartyCreated(MultipartyCreatedEventArgs e)
        {
            if (MultipartyCreated != null)
                MultipartyCreated(this, e);
        }

        protected virtual void OnJoinedGroupChat(GroupChatParticipationEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact + " joined group chat " + e.Via.ToString(), GetType().Name);

            if (JoinedGroupChat != null)
                JoinedGroupChat(this, e);
        }

        protected virtual void OnLeftGroupChat(GroupChatParticipationEventArgs e)
        {
            if (LeftGroupChat != null)
                LeftGroupChat(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               e.Contact + " left group chat " + e.Via.ToString(), GetType().Name);
        }

        #endregion

        #region SendTypingMessage

        public void SendTypingMessage(Contact remoteContact)
        {
            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To][MIMERoutingHeaders.Path] = "IM";
            }

            if (remoteContact.ViaContact != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.ViaContact.ClientType).ToString() + ":" + remoteContact.ViaContact.Account;
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Control/Typing";
            mmMessage.InnerBody = new byte[0];

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        #endregion

        #region SendNudge

        public void SendNudge(Contact remoteContact)
        {
            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To][MIMERoutingHeaders.Path] = "IM";
            }

            if (remoteContact.ViaContact != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.ViaContact.ClientType).ToString() + ":" + remoteContact.ViaContact.Account;
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Nudge";
            mmMessage.InnerBody = Encoding.ASCII.GetBytes("\r\n");

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        #endregion

        #region SendTextMessage

        public void SendTextMessage(Contact remoteContact, TextMessage textMessage)
        {
            textMessage.PrepareMessage();

            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To][MIMERoutingHeaders.Path] = "IM";
            }

            if (!remoteContact.Online)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.ServiceChannel] = "IM/Offline";
            }

            if (remoteContact.ViaContact != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.ViaContact.ClientType).ToString() + ":" + remoteContact.ViaContact.Account;
            }


            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Text";
            mmMessage.ContentHeaders[MIMEHeaderStrings.X_MMS_IM_Format] = textMessage.GetStyleString();
            mmMessage.InnerBody = Encoding.UTF8.GetBytes(textMessage.Text);

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        public void SendOIMMessage(Contact remoteContact, TextMessage textMessage)
        {
            SendTextMessage(remoteContact, textMessage);
        }

        #endregion

        #region SendMobileMessage

        /// <summary>
        /// Sends a mobile message to the specified remote contact. This only works when 
        /// the remote contact has it's mobile device enabled and has MSN-direct enabled.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="text"></param>
        public virtual void SendMobileMessage(Contact receiver, string text)
        {
            TextMessage txtMsg = new TextMessage(text);

            if (receiver.MobileAccess || receiver.ClientType == IMAddressInfoType.Telephone)
            {
                string to = ((int)receiver.ClientType).ToString() + ":" + ((receiver.ClientType == IMAddressInfoType.Telephone) ? "tel:" + receiver.Account : receiver.Account);
                string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

                MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
                mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();
                mmMessage.RoutingHeaders[MIMERoutingHeaders.ServiceChannel] = "IM/Mobile";

                mmMessage.ContentKeyVersion = "2.0";
                mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Text";
                mmMessage.ContentHeaders[MIMEContentHeaders.MSIMFormat] = txtMsg.GetStyleString();

                mmMessage.InnerBody = Encoding.UTF8.GetBytes(txtMsg.Text);

                NSMessage sdgPayload = new NSMessage("SDG");
                sdgPayload.InnerMessage = mmMessage;
                MessageProcessor.SendMessage(sdgPayload);
            }
            else
            {
                SendTextMessage(receiver, txtMsg);
            }
        }

        #endregion

        public void SendEmoticonDefinitions(Contact remoteContact, List<Emoticon> emoticons, EmoticonType icontype)
        {
            if (emoticons == null)
                throw new ArgumentNullException("emoticons");

            foreach (Emoticon emoticon in emoticons)
            {
                if (!ContactList.Owner.Emoticons.ContainsKey(emoticon.Sha))
                {
                    // Add the emotions to owner's emoticon collection.
                    ContactList.Owner.Emoticons.Add(emoticon.Sha, emoticon);
                }
            }

            EmoticonMessage emoticonMessage = new EmoticonMessage(emoticons, icontype);

            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "CustomEmoticon";
            mmMessage.ContentHeaders[MIMEHeaderStrings.Content_Type] = "text/x-mms-animemoticon";
            mmMessage.InnerBody = emoticonMessage.GetBytes();

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }


        #region MULTIPARTY

        #region CreateMultiparty

        public int CreateMultiparty()
        {
            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
            int transId = nsmp.IncreaseTransactionID();

            lock (multiparties)
                multiparties[transId] = null;

            string to = ((int)IMAddressInfoType.TemporaryGroup).ToString() + ":" + Guid.Empty.ToString("D").ToLowerInvariant() + "@" + Contact.DefaultHostDomain;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKey = MIMEContentHeaders.Publication;
            mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/circle";
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/multiparty+xml";

            mmMessage.InnerBody = new byte[0];

            NSMessage putPayload = new NSMessage("PUT");
            putPayload.InnerMessage = mmMessage;
            nsmp.SendMessage(putPayload, transId);

            return transId;
        }

        #endregion

        #region GetMultiparty

        public Contact GetMultiparty(string tempGroupAddress)
        {
            if (!String.IsNullOrEmpty(tempGroupAddress))
            {
                lock (multiparties)
                {
                    foreach (Contact group in multiparties.Values)
                    {
                        if (group != null && group.Account == tempGroupAddress)
                            return group;
                    }
                }
            }

            return null;
        }

        #endregion

        #region InviteContactToMultiparty

        public void InviteContactToMultiparty(Contact contact, Contact group)
        {
            string to = ((int)group.ClientType).ToString() + ":" + group.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();
            mmMessage.RoutingHeaders[MIMERoutingHeaders.To][MIMERoutingHeaders.Path] = "IM";

            mmMessage.ContentKey = MIMEContentHeaders.Publication;
            mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/circle";
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/multiparty+xml";

            string xml = "<circle><roster><id>IM</id><user><id>" + ((int)contact.ClientType).ToString() + ":" + contact.Account + "</id></user></roster></circle>";
            mmMessage.InnerBody = Encoding.UTF8.GetBytes(xml);

            NSMessage putPayload = new NSMessage("PUT");
            putPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(putPayload);
        }

        #region LeaveMultiparty

        public void LeaveMultiparty(Contact group)
        {
            string to = ((int)group.ClientType).ToString() + ":" + group.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKey = MIMEContentHeaders.Publication;
            mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/circle/roster(IM)/user(" + from + ")";
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/circles+xml";

            mmMessage.InnerBody = new byte[0];

            NSMessage delPayload = new NSMessage("DEL");
            delPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(delPayload);

            lock (multiparties)
            {
                foreach (Contact g in multiparties.Values)
                {
                    if (g != null && g.Account == group.Account)
                    {
                        multiparties.Remove(g.TransactionID);
                        break;
                    }
                }
            }
        }

        #endregion

        #endregion

        #region JoinMultiparty

        internal void JoinMultiparty(Contact group)
        {
            if (group.ClientType == IMAddressInfoType.Circle || group.ClientType == IMAddressInfoType.TemporaryGroup)
            {
                string to = ((int)group.ClientType).ToString() + ":" + group.Account;
                string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
                MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
                mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

                mmMessage.ContentKey = MIMEContentHeaders.Publication;
                mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/circle";
                mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/circles+xml";

                string xml = "<circle><roster><id>IM</id><user><id>1:" + ContactList.Owner.Account + "</id></user></roster></circle>";

                mmMessage.InnerBody = Encoding.UTF8.GetBytes(xml);

                NSMessage putPayload = new NSMessage("PUT");
                putPayload.InnerMessage = mmMessage;
                MessageProcessor.SendMessage(putPayload);

                OnJoinedGroupChat(new GroupChatParticipationEventArgs(ContactList.Owner, group));
            }
        }

        #endregion

        #endregion

        #region OnPUTReceived

        /// <summary>
        /// Called when a PUT command message has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnPUTReceived(NSMessage message)
        {
            bool ok = message.CommandValues.Count > 0 && message.CommandValues[0].ToString() == "OK";
            if (multiparties.ContainsKey(message.TransactionID))
            {
                if (ok == false || message.InnerBody == null || message.InnerBody.Length == 0)
                {
                    lock (multiparties)
                        multiparties.Remove(message.TransactionID);

                    return;
                }

                MultiMimeMessage mmMessage = new MultiMimeMessage(message.InnerBody);
                string[] tempGroup = mmMessage.From.Value.Split(':');
                IMAddressInfoType addressType = (IMAddressInfoType)int.Parse(tempGroup[0]);

                if (addressType == IMAddressInfoType.TemporaryGroup)
                {
                    Contact group = new Contact(tempGroup[1].ToLowerInvariant(), IMAddressInfoType.TemporaryGroup, this);
                    group.TransactionID = message.TransactionID;
                    group.ContactList = new ContactList(new Guid(tempGroup[1].ToLowerInvariant().Split('@')[0]),ContactList.Owner,this);
                    
                    multiparties[message.TransactionID] = group;

                    JoinMultiparty(group);

                    OnMultipartyCreated(new MultipartyCreatedEventArgs(group));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "MultipartyCreated: " + group.Account);
                }
                else
                {
                    lock (multiparties)
                        multiparties.Remove(message.TransactionID);
                }
            }
        }

        #endregion

        #region SignoutFrom

        internal void SignoutFrom(Guid endPointID)
        {
            string me = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(me, me);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKey = MIMEContentHeaders.Publication;
            mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/user";
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/user+xml";

            string xml = "<user><sep n=\"IM\" epid=\"" + endPointID.ToString("B").ToLowerInvariant() + "\"/></user>";

            mmMessage.InnerBody = Encoding.UTF8.GetBytes(xml);

            NSMessage delPayload = new NSMessage("DEL");
            delPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(delPayload);
        }

        #endregion

        #region OnDEL

        /// <summary>
        /// Called when a DEL command message has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnDELReceived(NSMessage message)
        {
            bool ok = message.CommandValues.Count > 0 && message.CommandValues[0].ToString() == "OK";
            if (ok)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DEL command accepted", GetType().Name);
            }
        }

        #endregion

        #region OnNFYReceived

        /// <summary>
        /// Called when a NFY command has been received.
        /// <code>
        /// NFY [TransactionID] [Operation: PUT|DEL] [Payload Length]\r\n[Payload Data]
        /// </code>
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnNFYReceived(NSMessage message)
        {
            NetworkMessage networkMessage = message as NetworkMessage;
            if (networkMessage.InnerBody == null || networkMessage.InnerBody.Length == 0)
                return;

            //PUT or DEL
            string command = message.CommandValues[0].ToString();

            MultiMimeMessage mmm = new MultiMimeMessage(networkMessage.InnerBody);

            if (!(mmm.ContentHeaders.ContainsKey(MIMEHeaderStrings.Content_Type)))
                return;

            IMAddressInfoType fromAccountAddressType;
            string fromAccount;
            IMAddressInfoType fromViaAccountAddressType;
            string fromViaAccount;

            IMAddressInfoType toAccountAddressType;
            string toAccount;
            IMAddressInfoType toViaAccountAddressType;
            string toViaAccount;

            if ((false == Contact.ParseFullAccount(mmm.From.ToString(),
                out fromAccountAddressType, out fromAccount,
                out fromViaAccountAddressType, out fromViaAccount))
                ||
                (false == Contact.ParseFullAccount(mmm.To.ToString(),
                out toAccountAddressType, out toAccount,
                out toViaAccountAddressType, out toViaAccount)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "[OnNFYReceived] Cannot parse from or to: " + mmm.From.ToString() + "|" + mmm.To.ToString());

                return;
            }

            Contact viaHeaderContact = null;
            Contact fromContact = null;
            bool fromIsMe = false;

            if (mmm.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via) ||
                fromViaAccountAddressType != IMAddressInfoType.None ||
                toViaAccountAddressType != IMAddressInfoType.None)
            {
                string viaFull = mmm.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via)
                    ? mmm.RoutingHeaders[MIMEHeaderStrings.Via].Value
                    :
                    (fromViaAccountAddressType != IMAddressInfoType.None ?
                    (((int)fromViaAccountAddressType).ToString() + ":" + fromViaAccount)
                    :
                    (((int)toViaAccountAddressType).ToString() + ":" + toViaAccount));

                IMAddressInfoType viaHeaderAddressType;
                string viaHeaderAccount;
                IMAddressInfoType ignoreAddressType;
                string ignoreAccount;

                Contact.ParseFullAccount(viaFull,
                    out viaHeaderAddressType, out viaHeaderAccount,
                    out ignoreAddressType, out ignoreAccount);

                if (viaHeaderAddressType == IMAddressInfoType.Circle)
                {
                    viaHeaderContact = ContactList.GetCircle(viaHeaderAccount);
                    if (viaHeaderContact != null)
                    {
                        fromContact = viaHeaderContact.ContactList.GetContact(fromAccount, fromAccountAddressType);
                    }
                }
                else if (viaHeaderAddressType == IMAddressInfoType.TemporaryGroup)
                {
                    viaHeaderContact = GetMultiparty(viaHeaderAccount);
                    if (viaHeaderContact != null)
                    {
                        fromContact = viaHeaderContact.ContactList.GetContact(fromAccount, fromAccountAddressType);
                    }
                }
                else
                {
                    viaHeaderContact = ContactList.GetContact(viaHeaderAccount, viaHeaderAddressType);
                    if (viaHeaderContact != null)
                    {
                        fromContact = viaHeaderContact.ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
                    }
                }
            }

            if (fromContact == null)
            {
                fromIsMe = (fromAccount == ContactList.Owner.Account && fromAccountAddressType == IMAddressInfoType.WindowsLive);
                fromContact = fromIsMe ? ContactList.Owner : ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
            }

            fromContact.ViaContact = viaHeaderContact;

            if (command == "PUT")
            {
                switch (mmm.ContentHeaders[MIMEHeaderStrings.Content_Type].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {

                            if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (mmm.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                            {
                                //This is an initial NFY
                            }

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(Encoding.UTF8.GetString(mmm.InnerBody));

                            XmlNodeList services = xmlDoc.SelectNodes("//user/s");
                            XmlNodeList serviceEndPoints = xmlDoc.SelectNodes("//user/sep");

                            if (services.Count > 0)
                            {
                                foreach (XmlNode service in services)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);
                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {
                                                foreach (XmlNode node in service.ChildNodes)
                                                {
                                                    switch (node.Name)
                                                    {
                                                        case "Status":

                                                            if (fromIsMe && IsSignedIn == false)
                                                            {
                                                                // We have already signed in another place, but not here...
                                                                // Don't set status... This place will set the status later.
                                                                return;
                                                            }

                                                            PresenceStatus oldStatus = fromContact.Status;
                                                            PresenceStatus newStatus = ParseStatus(node.InnerText);
                                                            fromContact.SetStatus(newStatus);

                                                            // The contact changed status
                                                            OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                            // The contact goes online
                                                            OnContactOnline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                            break;

                                                        case "CurrentMedia":
                                                            //MSNP21TODO: UBX implementation

                                                            break;
                                                    }
                                                }
                                                break;
                                            }

                                        case ServiceShortNames.PE:
                                            {
                                                PersonalMessage pm = new PersonalMessage(service.ChildNodes);

                                                if (!String.IsNullOrEmpty(pm.Payload))
                                                {
                                                    fromContact.SetPersonalMessage(pm);

                                                    // FriendlyName
                                                    fromContact.SetName(String.IsNullOrEmpty(pm.FriendlyName) ? fromContact.Account : pm.FriendlyName);

                                                    // UserTileLocation
                                                    if (!String.IsNullOrEmpty(pm.UserTileLocation))
                                                        fromContact.UserTileLocation = pm.UserTileLocation;

                                                    // Scene
                                                    if (!String.IsNullOrEmpty(pm.Scene))
                                                    {
                                                        if (fromContact.SceneContext != pm.Scene)
                                                        {
                                                            fromContact.SceneContext = pm.Scene;
                                                            fromContact.FireSceneImageContextChangedEvent(pm.Scene);
                                                        }
                                                    }

                                                    // ColorScheme
                                                    if (pm.ColorScheme != Color.Empty)
                                                    {
                                                        if (fromContact.ColorScheme != pm.ColorScheme)
                                                        {
                                                            fromContact.ColorScheme = pm.ColorScheme;
                                                            fromContact.OnColorSchemeChanged();
                                                        }
                                                    }

                                                }
                                            }
                                            break;
                                    }
                                }
                            }

                            if (serviceEndPoints.Count > 0)
                            {
                                foreach (XmlNode service in serviceEndPoints)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);
                                    Guid epid = service.Attributes["epid"] == null ? Guid.Empty : new Guid(service.Attributes["epid"].Value);

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                        case ServiceShortNames.PE:
                                            {
                                                foreach (XmlNode node in service.ChildNodes)
                                                {
                                                    switch (node.Name)
                                                    {
                                                        case "Capabilities":

                                                            ClientCapabilities cap = ClientCapabilities.None;
                                                            ClientCapabilitiesEx capEx = ClientCapabilitiesEx.None;

                                                            string[] caps = node.InnerText.Split(':');
                                                            if (caps.Length > 1)
                                                            {
                                                                capEx = (ClientCapabilitiesEx)long.Parse(caps[1]);
                                                            }
                                                            cap = (ClientCapabilities)long.Parse(caps[0]);

                                                            if (!fromContact.EndPointData.ContainsKey(epid))
                                                                fromContact.EndPointData.Add(epid, fromIsMe ? new PrivateEndPointData(fromContact.Account, epid) : new EndPointData(fromContact.Account, epid));

                                                            if (serviceEnum == ServiceShortNames.IM)
                                                            {
                                                                fromContact.EndPointData[epid].IMCapabilities = cap;
                                                                fromContact.EndPointData[epid].IMCapabilitiesEx = capEx;
                                                            }
                                                            else if (serviceEnum == ServiceShortNames.PE)
                                                            {
                                                                fromContact.EndPointData[epid].PECapabilities = cap;
                                                                fromContact.EndPointData[epid].PECapabilitiesEx = capEx;
                                                            }
                                                            break;
                                                    }
                                                }
                                                break;
                                            }

                                        case ServiceShortNames.PD:
                                            {                                        
                                                PrivateEndPointData privateEndPoint = ContactList.Owner.EndPointData.ContainsKey(epid) ?
                                                    ContactList.Owner.EndPointData[epid] as PrivateEndPointData : new PrivateEndPointData(ContactList.Owner.Account, epid);

                                                foreach (XmlNode node in service.ChildNodes)
                                                {
                                                    switch (node.Name)
                                                    {
                                                        case "ClientType":
                                                            privateEndPoint.ClientType = node.InnerText;
                                                            break;

                                                        case "EpName":
                                                            privateEndPoint.Name = node.InnerText;
                                                            break;

                                                        case "Idle":
                                                            privateEndPoint.Idle = bool.Parse(node.InnerText);
                                                            break;

                                                        case "State":
                                                            privateEndPoint.State = ParseStatus(node.InnerText);
                                                            break;
                                                    }
                                                }

                                                ContactList.Owner.SetChangedPlace(privateEndPoint, PlaceChangedReason.SignedIn);
                                                break;
                                            }
                                    }
                                }
                            }

                        }
                        break;
                    #endregion

                    #region circles xml
                    case "application/circles+xml":
                        {
                            if (fromAccountAddressType == IMAddressInfoType.Circle)
                            {
                                Contact circle = ContactList.GetCircle(fromAccount);

                                if (circle == null)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] Cannot complete the operation since circle not found: " + mmm.From.ToString());

                                    return;
                                }

                                if (mmm.InnerBody == null || mmm.InnerBody.Length == 0 ||
                                    "<circle></circle>" == Encoding.UTF8.GetString(mmm.InnerBody))
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (mmm.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                                    {
                                        PresenceStatus oldStatus = circle.Status;
                                        PresenceStatus newStatus = PresenceStatus.Online;
                                        circle.SetStatus(newStatus);

                                        // The contact changed status
                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));

                                        // The contact goes online
                                        OnContactOnline(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));

                                        if (circle.AppearOnline && circle.OnForwardList &&
                                            (oldStatus == PresenceStatus.Offline || oldStatus == PresenceStatus.Hidden))
                                        {
                                            //JoinMultiparty(circle);
                                        }
                                    }
                                    return;
                                }

                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(Encoding.UTF8.GetString(mmm.InnerBody));
                                XmlNodeList ids = xmlDoc.SelectNodes("//circle/roster/user/id");

                                if (ids.Count == 0)
                                {
                                    return;  //I hate indent.
                                }

                                foreach (XmlNode node in ids)
                                {
                                    IMAddressInfoType accountAddressType;
                                    string account;
                                    IMAddressInfoType viaAccountAddressType;
                                    string viaAccount;
                                    string fullAccount = node.InnerText;

                                    if (false == Contact.ParseFullAccount(fullAccount,
                                        out accountAddressType, out account,
                                        out viaAccountAddressType, out viaAccount))
                                    {
                                        continue;
                                    }

                                    if (account == ContactList.Owner.Account)
                                        continue;

                                    if (circle.ContactList.HasContact(account, accountAddressType))
                                    {
                                        Contact contact = circle.ContactList.GetContact(account, accountAddressType);
                                        OnJoinedGroupChat(new GroupChatParticipationEventArgs(contact, circle));
                                    }
                                }
                            }
                            else if (fromAccountAddressType == IMAddressInfoType.TemporaryGroup)
                            {
                                Contact group = GetMultiparty(fromAccount);

                                if (group == null)
                                {
                                    NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
                                    int transId = nsmp.IncreaseTransactionID();

                                    group = new Contact(fromAccount, IMAddressInfoType.TemporaryGroup, this);
                                    group.TransactionID = transId;
                                    group.ContactList = new ContactList(new Guid(fromAccount.Split('@')[0]), ContactList.Owner, this);

                                    lock (multiparties)
                                        multiparties[transId] = group;

                                    OnMultipartyCreated(new MultipartyCreatedEventArgs(group));
                                }

                                if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (mmm.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                                    {
                                        PresenceStatus oldStatus = group.Status;
                                        PresenceStatus newStatus = PresenceStatus.Online;
                                        group.SetStatus(newStatus);

                                        // The contact changed status
                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

                                        // The contact goes online
                                        OnContactOnline(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

                                    }
                                    return;
                                }

                                // Join multiparty if state is Pending
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.LoadXml(Encoding.UTF8.GetString(mmm.InnerBody));
                                XmlNodeList rosters = xmlDoc.SelectNodes("//circle/roster/user");
                                foreach (XmlNode roster in rosters)
                                {
                                    string state = (roster["state"] == null) ? string.Empty : roster["state"].InnerText;
                                    string[] fullAccount = roster["id"].InnerText.Split(':');
                                    IMAddressInfoType addressType = (IMAddressInfoType)int.Parse(fullAccount[0]);
                                    string memberAccount = fullAccount[1].ToLowerInvariant();

                                    // Me contact
                                    if ("pending" == state.ToLowerInvariant() &&
                                        addressType == ContactList.Owner.ClientType &&
                                        memberAccount == ContactList.Owner.Account)
                                    {
                                        JoinMultiparty(group);
                                    }
                                    else
                                    {
                                        Contact contact = group.ContactList.GetContactWithCreate(memberAccount, addressType);
                                        OnJoinedGroupChat(new GroupChatParticipationEventArgs(contact, group));
                                    }
                                }
                            }
                        }
                        break;
                    #endregion

                    #region network xml
                    case "application/network+xml":
                        {
                            if (fromAccountAddressType == IMAddressInfoType.RemoteNetwork &&
                                fromAccount == RemoteNetworkGateways.FaceBookGatewayAccount)
                            {
                                string status = Encoding.UTF8.GetString(mmm.InnerBody);

                                PresenceStatus oldStatus = fromContact.Status;
                                PresenceStatus newStatus = PresenceStatus.Unknown;

                                if (status.Contains("SignedIn"))
                                    newStatus = PresenceStatus.Online;
                                else if (status.Contains("SignedOut"))
                                    newStatus = PresenceStatus.Offline;

                                if (newStatus != PresenceStatus.Unknown)
                                {
                                    fromContact.SetStatus(newStatus);

                                    // The contact changed status
                                    OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));

                                    if (newStatus == PresenceStatus.Online)
                                        OnContactOnline(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));
                                    else
                                        OnContactOffline(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));
                                }
                            }
                        }
                        break;
                    #endregion
                }
            }
            else if (command == "DEL")
            {
                switch (mmm.ContentHeaders[MIMEHeaderStrings.Content_Type].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {

                            if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (mmm.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                            {
                                //This is an initial NFY
                            }

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(Encoding.UTF8.GetString(mmm.InnerBody));

                            XmlNodeList services = xmlDoc.SelectNodes("//user/s");
                            XmlNodeList serviceEndPoints = xmlDoc.SelectNodes("//user/sep");

                            if (serviceEndPoints.Count > 0)
                            {
                                foreach (XmlNode service in serviceEndPoints)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);
                                    Guid epid = service.Attributes["epid"] == null ? Guid.Empty : new Guid(service.Attributes["epid"].Value);

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {
                                                if (fromIsMe)
                                                {
                                                    if (ContactList.Owner.EndPointData.ContainsKey(epid))
                                                    {
                                                        ContactList.Owner.SetChangedPlace(ContactList.Owner.EndPointData[epid] as PrivateEndPointData, PlaceChangedReason.SignedOut);
                                                    }
                                                }
                                                else
                                                {
                                                    if (fromContact.EndPointData.ContainsKey(epid))
                                                        fromContact.EndPointData.Remove(epid);

                                                    PresenceStatus oldStatus = fromContact.Status;
                                                    PresenceStatus newStatus = PresenceStatus.Offline;
                                                    fromContact.SetStatus(newStatus);

                                                    // the contact changed status
                                                    OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                    // the contact goes offline
                                                    OnContactOffline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
                                                }
                                                break;
                                            }

                                        case ServiceShortNames.PD:
                                            {
                                                if (ContactList.Owner.EndPointData.ContainsKey(epid))
                                                {
                                                    ContactList.Owner.SetChangedPlace(ContactList.Owner.EndPointData[epid] as PrivateEndPointData, PlaceChangedReason.SignedOut);
                                                }
                                                if (epid == MachineGuid)
                                                {
                                                    ContactList.Owner.Status = PresenceStatus.Offline;
                                                }
                                                break;
                                            }
                                    }
                                }
                            }

                            if (services.Count > 0)
                            {
                                foreach (XmlNode service in services)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);
                                    Guid epid = service.Attributes["epid"] == null ? Guid.Empty : new Guid(service.Attributes["epid"].Value);

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {

                                                break;
                                            }
                                    }
                                }
                            }

                        }
                        break;

                    #endregion

                    #region circles xml

                    case "application/circles+xml":
                        {
                            Contact circle = null;
                            Contact group = null;

                            if (fromAccountAddressType == IMAddressInfoType.Circle)
                            {
                                circle = ContactList.GetCircle(fromAccount);

                                if (circle == null)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] Cannot complete the operation since circle not found: " + mmm.From.ToString());

                                    return;
                                }
                            }
                            else if (fromAccountAddressType == IMAddressInfoType.TemporaryGroup)
                            {
                                group = GetMultiparty(fromAccount);

                                if (group == null)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] temp group not found: " + mmm.From.ToString());

                                    return;
                                }
                            }
                            else
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] sender is not circle nor temp group: " + mmm.From.ToString());

                                return;
                            }

                            if (mmm.ContentHeaders.ContainsKey(MIMEHeaderStrings.Uri))
                            {
                                string xpathUri = mmm.ContentHeaders[MIMEHeaderStrings.Uri].ToString();
                                if (xpathUri.Contains("/circle/roster(IM)/user"))
                                {
                                    string typeAccount = xpathUri.Substring("/circle/roster(IM)/user".Length);
                                    typeAccount = typeAccount.Substring(typeAccount.IndexOf("(") + 1);
                                    typeAccount = typeAccount.Substring(0, typeAccount.IndexOf(")"));

                                    string[] member = typeAccount.Split(':');
                                    string memberAccount = member[1];
                                    IMAddressInfoType memberNetwork = (IMAddressInfoType)int.Parse(member[0]);

                                    Contact c = null;

                                    if (circle != null)
                                    {
                                        if (!circle.ContactList.HasContact(memberAccount, memberNetwork))
                                            return;

                                        c = circle.ContactList.GetContact(memberAccount, memberNetwork);
                                        OnLeftGroupChat(new GroupChatParticipationEventArgs(c, circle));
                                    }

                                    if (group != null)
                                    {
                                        if (!group.ContactList.HasContact(memberAccount, memberNetwork))
                                            return;

                                        c = group.ContactList.GetContact(memberAccount, memberNetwork);
                                        group.ContactList.Remove(memberAccount, memberNetwork);

                                        OnLeftGroupChat(new GroupChatParticipationEventArgs(c, group));
                                    }
                                }
                                else
                                {
                                    Contact goesOfflineGroup = null;
                                    if (circle != null)
                                        goesOfflineGroup = circle;
                                    else if (group != null)
                                        goesOfflineGroup = group;

                                    // Group goes offline...
                                    if (goesOfflineGroup != null)
                                    {
                                        PresenceStatus oldStatus = goesOfflineGroup.Status;
                                        PresenceStatus newStatus = PresenceStatus.Offline;
                                        goesOfflineGroup.SetStatus(newStatus);

                                        // the contact changed status
                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(goesOfflineGroup, oldStatus, newStatus));

                                        // the contact goes offline
                                        OnContactOffline(new ContactStatusChangedEventArgs(goesOfflineGroup, oldStatus, newStatus));

                                    }
                                }
                            }

                        }
                        break;
                    #endregion

                    #region network xml
                    case "application/network+xml":
                        {


                        }
                        break;
                    #endregion
                }
            }
        }

        #endregion

        #region OnSDGReceived

        /// <summary>
        /// Called when a SDG command has been received.
        /// <remarks>Indicates that someone send us a message.</remarks>
        /// <code>
        /// SDG 0 [Payload Length]\r\n[Payload Data]
        /// </code>
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnSDGReceived(NSMessage message)
        {
            NetworkMessage networkMessage = message as NetworkMessage;
            if (networkMessage.InnerBody == null || networkMessage.InnerBody.Length == 0)
                return;

            MultiMimeMessage mmMessage = new MultiMimeMessage(networkMessage.InnerBody);

            IMAddressInfoType fromAccountAddressType;
            string fromAccount;
            IMAddressInfoType fromViaAccountAddressType;
            string fromViaAccount;

            IMAddressInfoType toAccountAddressType;
            string toAccount;
            IMAddressInfoType toViaAccountAddressType;
            string toViaAccount;

            if ((false == Contact.ParseFullAccount(mmMessage.From.ToString(),
                out fromAccountAddressType, out fromAccount,
                out fromViaAccountAddressType, out fromViaAccount))
                ||
                (false == Contact.ParseFullAccount(mmMessage.To.ToString(),
                out toAccountAddressType, out toAccount,
                out toViaAccountAddressType, out toViaAccount)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "[OnSDGReceived] Cannot parse from or to: " + mmMessage.From.ToString() + "|" + mmMessage.To.ToString());

                return;
            }

            Contact sender = null;

            if (toAccountAddressType == IMAddressInfoType.Circle)
            {
                sender = ContactList.GetCircle(toAccount);

                if (sender == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnSDGReceived] Error: Cannot find circle: " + mmMessage.To.ToString());

                    return;
                }
            }
            else if (toAccountAddressType == IMAddressInfoType.TemporaryGroup)
            {
                sender = GetMultiparty(toAccount);

                if (sender == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnSDGReceived] Error: Cannot find temp group: " + mmMessage.To.ToString());

                    return;
                }
            }

            Contact fromContact = ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);

            if (sender == null)
            {
                sender = fromContact;
            }

            if (mmMessage.ContentHeaders.ContainsKey(MIMEHeaderStrings.Message_Type))
            {
                if ("nudge" == mmMessage.ContentHeaders[MIMEHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    OnNudgeReceived(new NudgeArrivedEventArgs(sender, fromContact));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "NUDGE: " + sender.ToString() + (sender == fromContact ? String.Empty : ";via=" + fromContact.ToString()));
                }
                else if ("control/typing" == mmMessage.ContentHeaders[MIMEHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    OnTypingMessageReceived(new TypingArrivedEventArgs(sender, fromContact));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "TYPING: " + sender.ToString() + (sender == fromContact ? String.Empty : ";via=" + fromContact.ToString()));
                }
                else if ("text" == mmMessage.ContentHeaders[MIMEHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    TextMessage txtMessage = new TextMessage(Encoding.UTF8.GetString(mmMessage.InnerBody));
                    StrDictionary strDic = new StrDictionary();
                    foreach (string key in mmMessage.ContentHeaders.Keys)
                    {
                        strDic.Add(key, mmMessage.ContentHeaders[key].ToString());
                    }
                    txtMessage.ParseHeader(strDic);

                    OnTextMessageReceived(new TextMessageArrivedEventArgs(sender, txtMessage, fromContact));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "TEXT MESSAGE: " + sender.ToString() + (sender == fromContact ? String.Empty : ";via=" + fromContact.ToString()) + "\r\n" + txtMessage.ToDebugString());
                }
                else if ("signal/p2p" == mmMessage.ContentHeaders[MIMEHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    SLPMessage slp = SLPMessage.Parse(mmMessage.InnerBody);
                    HandleSIP(slp);
                }
                else if ("data" == mmMessage.ContentHeaders[MIMEHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    P2PVersion toVer = mmMessage.To.HasAttribute(MIMERoutingHeaders.EPID) ? P2PVersion.P2PV2 : P2PVersion.P2PV1;

                    P2PMessage p2pData = new P2PMessage(toVer);
                    p2pData.ParseBytes(mmMessage.InnerBody);
                    HandleP2PData(p2pData);
                }
            }
        }

        #endregion
    }
};
