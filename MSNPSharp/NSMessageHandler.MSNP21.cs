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
        /// Occurs when any contact goed from any status to offline status.
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
        }

        /// <summary>
        /// Fires the <see cref="ContactOnline"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnContactOnline(ContactStatusChangedEventArgs e)
        {
            if (ContactOnline != null)
                ContactOnline(this, e);
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
            mmMessage.RoutingHeaders["From"]["epid"] = ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders["To"]["path"] = "IM";
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders["Message-Type"] = "Control/Typing";
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
            mmMessage.RoutingHeaders["From"]["epid"] = ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders["To"]["path"] = "IM";
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders["Message-Type"] = "Nudge";
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
            mmMessage.RoutingHeaders["From"]["epid"] = ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders["To"]["path"] = "IM";
            }

            if (!remoteContact.Online)
            {
                mmMessage.RoutingHeaders["Service-Channel"] = "IM/Offline";
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders["Message-Type"] = "Text";
            mmMessage.ContentHeaders[MimeHeaderStrings.X_MMS_IM_Format] = textMessage.GetStyleString();
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
                mmMessage.RoutingHeaders["From"]["epid"] = ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();
                mmMessage.RoutingHeaders["Service-Channel"] = "IM/Mobile";

                mmMessage.ContentKeyVersion = "2.0";
                mmMessage.ContentHeaders["Message-Type"] = "Text";
                mmMessage.ContentHeaders[MimeHeaderStrings.X_MMS_IM_Format] = txtMsg.GetStyleString();

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

        #region MULTIPARTY

        #region CreateMultiparty

        public int CreateMultiparty()
        {
            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
            int transId = nsmp.IncreaseTransactionID();

            lock (multiparties)
                multiparties[transId] = null;

            string to = ((int)IMAddressInfoType.TemporaryGroup).ToString() + ":" + Guid.Empty.ToString("D").ToLowerInvariant() + "@" + Circle.DefaultHostDomain;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders["From"]["epid"] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKey = "Publication";
            mmMessage.ContentHeaders["Uri"] = "/circle";
            mmMessage.ContentHeaders["Content-Type"] = "application/multiparty+xml";

            mmMessage.InnerBody = new byte[0];

            NSMessage putPayload = new NSMessage("PUT");
            putPayload.InnerMessage = mmMessage;
            nsmp.SendMessage(putPayload, transId);

            return transId;
        }

        #endregion

        #region GetMultiparty

        public TemporaryGroup GetMultiparty(string tempGroupAddress)
        {
            if (!String.IsNullOrEmpty(tempGroupAddress))
            {
                lock (multiparties)
                {
                    foreach (TemporaryGroup group in multiparties.Values)
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

        public void InviteContactToMultiparty(Contact contact, TemporaryGroup group)
        {
            string to = ((int)group.ClientType).ToString() + ":" + group.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders["From"]["epid"] = MachineGuid.ToString("B").ToLowerInvariant();
            mmMessage.RoutingHeaders["To"]["path"] = "IM";

            mmMessage.ContentKey = "Publication";
            mmMessage.ContentHeaders["Uri"] = "/circle";
            mmMessage.ContentHeaders["Content-Type"] = "application/multiparty+xml";

            string xml = "<circle><roster><id>IM</id><user><id>" + ((int)contact.ClientType).ToString() + ":" + contact.Account + "</id></user></roster></circle>";
            mmMessage.InnerBody = Encoding.UTF8.GetBytes(xml);

            NSMessage putPayload = new NSMessage("PUT");
            putPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(putPayload);
        }

        #region LeaveMultiparty

        public void LeaveMultiparty(TemporaryGroup group)
        {
            string to = ((int)group.ClientType).ToString() + ":" + group.Account;
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders["From"]["epid"] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKey = "Publication";
            mmMessage.ContentHeaders["Uri"] = "/circle/roster(IM)/user(" + from + ")";
            mmMessage.ContentHeaders["Content-Type"] = "application/circles+xml";

            mmMessage.InnerBody = new byte[0];

            NSMessage putPayload = new NSMessage("DEL");
            putPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(putPayload);

            lock (multiparties)
            {
                foreach (TemporaryGroup g in multiparties.Values)
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
            if (group is Circle || group is TemporaryGroup)
            {
                string to = ((int)group.ClientType).ToString() + ":" + group.Account;
                string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Account;
                MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
                mmMessage.RoutingHeaders["From"]["epid"] = MachineGuid.ToString("B").ToLowerInvariant();

                mmMessage.ContentKey = "Publication";
                mmMessage.ContentHeaders["Uri"] = "/circle";
                mmMessage.ContentHeaders["Content-Type"] = "application/circles+xml";

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
                    TemporaryGroup group = new TemporaryGroup(tempGroup[1].ToLowerInvariant(), this, message.TransactionID);
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

            if (!(mmm.ContentHeaders.ContainsKey(MimeHeaderStrings.Content_Type)))
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

            if (mmm.RoutingHeaders.ContainsKey(MimeHeaderStrings.Via))
            {
                string[] via = mmm.RoutingHeaders[MimeHeaderStrings.Via].Value.Split(':');
                IMAddressInfoType viaHeaderAddressType = (IMAddressInfoType)int.Parse(via[0]);
                string viaHeaderAccount = via[1].ToLowerInvariant();

                if (viaHeaderAddressType == IMAddressInfoType.Circle)
                {
                    viaHeaderContact = ContactList.GetCircle(viaHeaderAccount);
                    if (viaHeaderContact != null)
                    {
                        fromContact = (viaHeaderContact as Circle).ContactList.GetContact(fromAccount, fromAccountAddressType);
                    }
                }
                else if (viaHeaderAddressType == IMAddressInfoType.TemporaryGroup)
                {
                    viaHeaderContact = GetMultiparty(viaHeaderAccount);
                    if (viaHeaderContact != null)
                    {
                        fromContact = (viaHeaderContact as TemporaryGroup).ContactList.GetContact(fromAccount, fromAccountAddressType);
                    }
                }
            }

            if (fromContact == null)
            {
                fromContact = ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
            }

            if (command == "PUT")
            {
                switch (mmm.ContentHeaders[MimeHeaderStrings.Content_Type].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {

                            if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (mmm.ContentHeaders[MimeHeaderStrings.NotifType].Value == "Full")
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
                                                                fromContact.EndPointData.Add(epid, new EndPointData(fromContact.Account, epid));

                                                            fromContact.EndPointData[epid].ClientCapabilities = cap;
                                                            fromContact.EndPointData[epid].ClientCapabilitiesEx = capEx;

                                                            break;
                                                    }
                                                }
                                                break;
                                            }

                                        case ServiceShortNames.PD:
                                            {
                                                Dictionary<Guid, PrivateEndPointData> epList = new Dictionary<Guid, PrivateEndPointData>(0);
                                                PrivateEndPointData privateEndPoint = new PrivateEndPointData(ContactList.Owner.Account, epid);

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

                                                epList[epid] = privateEndPoint;
                                                TriggerPlaceChangeEvent(epList);
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
                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {
                                                foreach (XmlNode node in service.ChildNodes)
                                                {
                                                    switch (node.Name)
                                                    {
                                                        case "Status":

                                                            PresenceStatus oldStatus = fromContact.Status;
                                                            PresenceStatus newStatus = ParseStatus(node.InnerText);
                                                            fromContact.SetStatus(newStatus);

                                                            // The contact changed status
                                                            OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                            // The contact goes online
                                                            OnContactOnline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                                                fromContact.ToString() + " goes to " + newStatus + " from " + oldStatus + (viaHeaderContact == null ? String.Empty : " via=" + viaHeaderContact.ToString()) + "\r\n", GetType().Name);

                                                            break;

                                                        case "CurrentMedia":
                                                            //MSNP21TODO: UBX implementation

                                                            break;
                                                    }
                                                }
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
                                Circle circle = ContactList.GetCircle(fromAccount);

                                if (circle == null)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] Cannot complete the operation since circle not found: " + mmm.From.ToString());

                                    return;
                                }

                                if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (mmm.ContentHeaders[MimeHeaderStrings.NotifType].Value == "Full")
                                    {
                                        PresenceStatus oldStatus = circle.Status;
                                        PresenceStatus newStatus = PresenceStatus.Online;
                                        circle.SetStatus(newStatus);

                                        // The contact changed status
                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));

                                        // The contact goes online
                                        OnContactOnline(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                            circle.ToString() + " goes to " + newStatus + " from " + oldStatus, GetType().Name);

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
                                TemporaryGroup group = GetMultiparty(fromAccount);

                                if (group == null)
                                {
                                    NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
                                    int transId = nsmp.IncreaseTransactionID();

                                    group = new TemporaryGroup(fromAccount, this, transId);

                                    lock (multiparties)
                                        multiparties[transId] = group;

                                    OnMultipartyCreated(new MultipartyCreatedEventArgs(group));
                                }

                                if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (mmm.ContentHeaders[MimeHeaderStrings.NotifType].Value == "Full")
                                    {
                                        PresenceStatus oldStatus = group.Status;
                                        PresenceStatus newStatus = PresenceStatus.Online;
                                        group.SetStatus(newStatus);

                                        // The contact changed status
                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

                                        // The contact goes online
                                        OnContactOnline(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                            group.ToString() + " goes to " + newStatus + " from " + oldStatus, GetType().Name);
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
                                // SigningIn,SignedIn, MSNP21TODO
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                       Encoding.UTF8.GetString(mmm.InnerBody));
                            }

                        }
                        break;
                    #endregion
                }
            }
            else if (command == "DEL")
            {
                switch (mmm.ContentHeaders[MimeHeaderStrings.Content_Type].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {

                            if (mmm.InnerBody == null || mmm.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (mmm.ContentHeaders[MimeHeaderStrings.NotifType].Value == "Full")
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
                                                if (fromContact.EndPointData.ContainsKey(epid))
                                                    fromContact.EndPointData.Remove(epid);


                                                PresenceStatus oldStatus = fromContact.Status;
                                                PresenceStatus newStatus = PresenceStatus.Offline;
                                                fromContact.SetStatus(newStatus);

                                                // the contact changed status
                                                OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                // the contact goes offline
                                                OnContactOffline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

                                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                                    fromContact.ToString() + " goes to " + newStatus + " from " + oldStatus + (viaHeaderContact == null ? String.Empty : " via=" + viaHeaderContact.ToString()) + "\r\n", GetType().Name);


                                                break;
                                            }

                                        case ServiceShortNames.PD:
                                            {
                                                if (ContactList.Owner.EndPointData.ContainsKey(epid))
                                                {
                                                    TriggerPlaceChangeEvent(new Dictionary<Guid, PrivateEndPointData>(0));
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
                            Circle circle = null;
                            TemporaryGroup group = null;

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

                            if (mmm.ContentHeaders.ContainsKey(MimeHeaderStrings.Uri))
                            {
                                string xpathUri = mmm.ContentHeaders[MimeHeaderStrings.Uri].ToString();
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

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                            goesOfflineGroup.ToString() + " goes to " + newStatus + " from " + oldStatus, GetType().Name);
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

            if (mmMessage.ContentHeaders.ContainsKey(MimeHeaderStrings.Message_Type))
            {
                if ("nudge" == mmMessage.ContentHeaders[MimeHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    OnNudgeReceived(new NudgeArrivedEventArgs(sender, fromContact));

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "NUDGE: " + sender.ToString() + (sender == fromContact ? String.Empty : ";via=" + fromContact.ToString()));
                }
                else if ("control/typing" == mmMessage.ContentHeaders[MimeHeaderStrings.Message_Type].ToString().ToLowerInvariant())
                {
                    OnTypingMessageReceived(new TypingArrivedEventArgs(sender, fromContact));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "TYPING: " + sender.ToString() + (sender == fromContact ? String.Empty : ";via=" + fromContact.ToString()));
                }
                else if ("text" == mmMessage.ContentHeaders[MimeHeaderStrings.Message_Type].ToString().ToLowerInvariant())
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
            }
        }

        #endregion
    }
};
