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
    using MSNPSharp.Apps;

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
        /// Fired when a contact sends a emoticon definition.
        /// </summary>
        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;

        /// <summary>
        /// Fired when a contact sends a wink definition.
        /// </summary>
        public event EventHandler<WinkEventArgs> WinkDefinitionReceived;

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

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "TYPING: " + e.Sender.ToString() + (e.Sender == e.OriginalSender ? String.Empty : ";via=" + e.OriginalSender.ToString()));

        }

        protected virtual void OnNudgeReceived(NudgeArrivedEventArgs e)
        {
            if (NudgeReceived != null)
                NudgeReceived(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "NUDGE: " + e.Sender.ToString() + (e.Sender == e.OriginalSender ? String.Empty : ";via=" + e.OriginalSender.ToString()));
        }

        protected virtual void OnTextMessageReceived(TextMessageArrivedEventArgs e)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "TEXT MESSAGE: " + e.Sender.ToString() + (e.Sender == e.OriginalSender ? String.Empty : ";via=" + e.OriginalSender.ToString()) + "\r\n" + e.TextMessage.ToDebugString());
        }

        protected virtual void OnEmoticonDefinitionReceived(EmoticonDefinitionEventArgs e)
        {
            if (EmoticonDefinitionReceived != null)
                EmoticonDefinitionReceived(this, e);
        }

        protected virtual void OnWinkDefinitionReceived(WinkEventArgs e)
        {
            if (WinkDefinitionReceived != null)
                WinkDefinitionReceived(this, e);
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

        #region MULTIPARTY

        #region CreateMultiparty

        public int CreateMultiparty()
        {
            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
            int transId = nsmp.IncreaseTransactionID();

            lock (multiparties)
                multiparties[transId] = null;

            string to = ((int)IMAddressInfoType.TemporaryGroup).ToString() + ":" + Guid.Empty.ToString("D").ToLowerInvariant() + "@" + Contact.DefaultHostDomain;
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;
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
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;
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
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

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
                string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;
                MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
                mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

                mmMessage.ContentKey = MIMEContentHeaders.Publication;
                mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/circle";
                mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/circles+xml";

                string xml = "<circle><roster><id>IM</id><user><id>1:" + Owner.Account + "</id></user></roster></circle>";

                mmMessage.InnerBody = Encoding.UTF8.GetBytes(xml);

                NSMessage putPayload = new NSMessage("PUT");
                putPayload.InnerMessage = mmMessage;
                MessageProcessor.SendMessage(putPayload);

                OnJoinedGroupChat(new GroupChatParticipationEventArgs(Owner, group));
            }
        }

        #endregion

        #endregion

        #region MESSAGING

        #region SendTypingMessage

        protected internal virtual void SendTypingMessage(Contact remoteContact)
        {
            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

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

        protected internal virtual void SendNudge(Contact remoteContact)
        {
            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

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

        protected internal virtual void SendTextMessage(Contact remoteContact, TextMessage textMessage)
        {
            textMessage.PrepareMessage();

            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();

            if (remoteContact.ClientType == IMAddressInfoType.Circle)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To][MIMERoutingHeaders.Path] = "IM";
            }
            else if (remoteContact.Online)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.ServiceChannel] = "IM/Online";
            }
            else
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

        protected internal virtual void SendOIMMessage(Contact remoteContact, TextMessage textMessage)
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
        protected internal virtual void SendMobileMessage(Contact receiver, string text)
        {
            TextMessage txtMsg = new TextMessage(text);

            string to = ((int)receiver.ClientType).ToString() + ":" + ((receiver.ClientType == IMAddressInfoType.Telephone) ? "tel:" + receiver.Account : receiver.Account);
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();
            mmMessage.RoutingHeaders[MIMERoutingHeaders.ServiceChannel] = "IM/Mobile";

            mmMessage.ContentKeyVersion = "2.0";
            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Text";
            mmMessage.ContentHeaders[MIMEContentHeaders.MSIMFormat] = txtMsg.GetStyleString();

            mmMessage.InnerBody = Encoding.UTF8.GetBytes(txtMsg.Text);

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        #endregion

        #region SendEmoticonDefinitions

        protected internal virtual void SendEmoticonDefinitions(Contact remoteContact, List<Emoticon> emoticons, EmoticonType icontype)
        {
            EmoticonMessage emoticonMessage = new EmoticonMessage(emoticons, icontype);

            string to = ((int)remoteContact.ClientType).ToString() + ":" + remoteContact.Account;
            string from = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = MachineGuid.ToString("B").ToLowerInvariant();

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "CustomEmoticon";
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = icontype == EmoticonType.AnimEmoticon ? "text/x-mms-animemoticon" : "text/x-mms-emoticon";
            mmMessage.InnerBody = emoticonMessage.GetBytes();

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        #endregion

        #endregion

        #region SignoutFrom

        internal void SignoutFrom(Guid endPointID)
        {
            string me = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

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

            if (endPointID == MachineGuid && messageProcessor.Connected)
                messageProcessor.Disconnect();
        }

        #endregion

        #region SetPresenceStatus

        /// <summary>
        /// Set the status of the contact list owner (the client).
        /// </summary>
        /// <remarks>You can only set the status _after_ SignedIn event. Otherwise you won't receive online notifications from other clients or the connection is closed by the server.</remarks>
        internal void SetPresenceStatus(
            PresenceStatus newStatus,
            ClientCapabilities newLocalIMCaps, ClientCapabilitiesEx newLocalIMCapsex,
            ClientCapabilities newLocalPECaps, ClientCapabilitiesEx newLocalPECapsex,
            string newEPName,
            PersonalMessage newPSM,
            bool forcePEservice)
        {
            if (IsSignedIn == false)
                throw new MSNPSharpException("Can't set status. You must wait for the SignedIn event before you can set an initial status.");

            if (newStatus == PresenceStatus.Offline)
            {
                SignoutFrom(MachineGuid);
                return;
            }

            bool SETALL = (Owner.Status == PresenceStatus.Offline);

            if (SETALL || forcePEservice ||
                newStatus != Owner.Status ||
                newLocalIMCaps != Owner.LocalEndPointIMCapabilities ||
                newLocalIMCapsex != Owner.LocalEndPointIMCapabilitiesEx ||
                newLocalPECaps != Owner.LocalEndPointPECapabilities ||
                newLocalPECapsex != Owner.LocalEndPointPECapabilitiesEx ||
                newEPName != Owner.EpName)
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement userElement = xmlDoc.CreateElement("user");

                // s.IM (Status, CurrentMedia)
                if (SETALL || forcePEservice ||
                    newStatus != Owner.Status)
                {
                    XmlElement service = xmlDoc.CreateElement("s");
                    service.SetAttribute("n", ServiceShortNames.IM.ToString());
                    service.InnerXml =
                        "<Status>" + ParseStatus(newStatus) + "</Status>" +
                        "<CurrentMedia>" + MSNHttpUtility.XmlEncode(newPSM.CurrentMedia) + "</CurrentMedia>";

                    userElement.AppendChild(service);
                }

                // s.PE (UserTileLocation, FriendlyName, PSM, Scene, ColorScheme)
                if (SETALL ||
                    forcePEservice)
                {
                    XmlElement service = xmlDoc.CreateElement("s");
                    service.SetAttribute("n", ServiceShortNames.PE.ToString());
                    service.InnerXml = newPSM.Payload;
                    userElement.AppendChild(service);

                    // Don't set owner.PersonalMessage here. It is replaced (with a new reference) when NFY PUT received.
                }

                // sep.IM (Capabilities)
                if (SETALL ||
                    newLocalIMCaps != Owner.LocalEndPointIMCapabilities ||
                    newLocalIMCapsex != Owner.LocalEndPointIMCapabilitiesEx)
                {
                    ClientCapabilities localIMCaps = SETALL ? ClientCapabilities.DefaultIM : newLocalIMCaps;
                    ClientCapabilitiesEx localIMCapsEx = SETALL ? ClientCapabilitiesEx.DefaultIM : newLocalIMCapsex;
                    if (BotMode)
                    {
                        localIMCaps |= ClientCapabilities.IsBot;
                    }

                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.IM.ToString());
                    XmlElement Capabilities = xmlDoc.CreateElement("Capabilities");
                    Capabilities.InnerText = ((long)localIMCaps).ToString() + ":" + ((long)localIMCapsEx).ToString();
                    sep.AppendChild(Capabilities);
                    userElement.AppendChild(sep);
                }

                // sep.PE (Capabilities)
                if (SETALL ||
                    newLocalPECaps != Owner.LocalEndPointPECapabilities ||
                    newLocalPECapsex != Owner.LocalEndPointPECapabilitiesEx)
                {
                    ClientCapabilities localPECaps = SETALL ? ClientCapabilities.DefaultPE : newLocalPECaps;
                    ClientCapabilitiesEx localPECapsEx = SETALL ? ClientCapabilitiesEx.DefaultPE : newLocalPECapsex;

                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.PE.ToString());
                    XmlElement VER = xmlDoc.CreateElement("VER");
                    VER.InnerText = Credentials.ClientInfo.MessengerClientName + ":" + Credentials.ClientInfo.MessengerClientBuildVer;
                    sep.AppendChild(VER);
                    XmlElement TYP = xmlDoc.CreateElement("TYP");
                    TYP.InnerText = "1";
                    sep.AppendChild(TYP);
                    XmlElement Capabilities = xmlDoc.CreateElement("Capabilities");
                    Capabilities.InnerText = ((long)localPECaps).ToString() + ":" + ((long)localPECapsEx).ToString();
                    sep.AppendChild(Capabilities);
                    userElement.AppendChild(sep);
                }

                // sep.PD (EpName, State)
                if (SETALL ||
                    newEPName != Owner.EpName ||
                    newStatus != Owner.Status)
                {
                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.PD.ToString());
                    XmlElement ClientType = xmlDoc.CreateElement("ClientType");
                    ClientType.InnerText = "1";
                    sep.AppendChild(ClientType);
                    XmlElement EpName = xmlDoc.CreateElement("EpName");
                    EpName.InnerText = MSNHttpUtility.XmlEncode(newEPName);
                    sep.AppendChild(EpName);
                    XmlElement Idle = xmlDoc.CreateElement("Idle");
                    Idle.InnerText = ((newStatus == PresenceStatus.Idle) ? "true" : "false");
                    sep.AppendChild(Idle);
                    XmlElement State = xmlDoc.CreateElement("State");
                    State.InnerText = ParseStatus(newStatus);
                    sep.AppendChild(State);
                    userElement.AppendChild(sep);
                }

                if (userElement.HasChildNodes)
                {
                    string xml = userElement.OuterXml;
                    string me = ((int)Owner.ClientType).ToString() + ":" + Owner.Account;

                    MultiMimeMessage mmMessage = new MultiMimeMessage(me, me);
                    mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();

                    mmMessage.Stream = 1;
                    mmMessage.ReliabilityHeaders[MIMEReliabilityHeaders.Flags] = "ACK";

                    mmMessage.ContentKey = MIMEContentHeaders.Publication;
                    mmMessage.ContentHeaders[MIMEContentHeaders.URI] = "/user";
                    mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = "application/user+xml";

                    mmMessage.InnerBody = System.Text.Encoding.UTF8.GetBytes(xml);

                    NSMessage nsMessage = new NSMessage("PUT");
                    nsMessage.InnerMessage = mmMessage;
                    MessageProcessor.SendMessage(nsMessage);
                }
            }
        }


        #endregion

        #region COMMAND HANDLERS (PUT, DEL, NFY, SDG)

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
                    group.ContactList = new ContactList(new Guid(tempGroup[1].ToLowerInvariant().Split('@')[0]), Owner, this);

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

        #region OnDELReceived

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

            MultiMimeMessage multiMimeMessage = new MultiMimeMessage(networkMessage.InnerBody);

            if (!(multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.ContentType)))
                return;

            IMAddressInfoType fromAccountAddressType;
            string fromAccount;
            IMAddressInfoType fromViaAccountAddressType;
            string fromViaAccount;

            IMAddressInfoType toAccountAddressType;
            string toAccount;
            IMAddressInfoType toViaAccountAddressType;
            string toViaAccount;

            if ((false == Contact.ParseFullAccount(multiMimeMessage.From.ToString(),
                out fromAccountAddressType, out fromAccount,
                out fromViaAccountAddressType, out fromViaAccount))
                ||
                (false == Contact.ParseFullAccount(multiMimeMessage.To.ToString(),
                out toAccountAddressType, out toAccount,
                out toViaAccountAddressType, out toViaAccount)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "[OnNFYReceived] Cannot parse from or to: " + multiMimeMessage.From.ToString() + "|" + multiMimeMessage.To.ToString());

                return;
            }

            Contact viaHeaderContact = null;
            Contact fromContact = null;
            bool fromIsMe = false;

            if (multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via) ||
                fromViaAccountAddressType != IMAddressInfoType.None ||
                toViaAccountAddressType != IMAddressInfoType.None)
            {
                string viaFull = multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via)
                    ? multiMimeMessage.RoutingHeaders[MIMEHeaderStrings.Via].Value
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
                fromIsMe = (fromAccount == Owner.Account && fromAccountAddressType == IMAddressInfoType.WindowsLive);
                fromContact = fromIsMe ? Owner : ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
            }

            fromContact.ViaContact = viaHeaderContact;

            if (command == "PUT")
            {
                switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {

                            if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Sync")
                            {
                                //Sync the contact in contact list with the contact in gateway.
                                // TODO: Set the NSMessagehandler.ContactList contact to the gateway
                                // TODO: triger the ContactOnline event for the gateway contact.

                                //Just wait for my fix.
                            }
                            if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                            {
                                //This is an initial NFY
                            }

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));

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

                                                            OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
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
                                                // Create a new reference to fire PersonalMessageChanged event.
                                                PersonalMessage pm = new PersonalMessage(service.ChildNodes);

                                                if (!String.IsNullOrEmpty(pm.Payload) &&
                                                    fromContact.PersonalMessage != pm)
                                                {
                                                    fromContact.PersonalMessage = pm;

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
                                foreach (XmlNode serviceEndPoint in serviceEndPoints)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), serviceEndPoint.Attributes["n"].Value);
                                    Guid epid = serviceEndPoint.Attributes["epid"] == null ? Guid.Empty : new Guid(serviceEndPoint.Attributes["epid"].Value);

                                    if (!fromContact.EndPointData.ContainsKey(epid))
                                    {
                                        lock (fromContact.SyncObject)
                                            fromContact.EndPointData.Add(epid, fromIsMe ? new PrivateEndPointData(fromContact.Account, epid) : new EndPointData(fromContact.Account, epid));
                                    }

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {
                                                foreach (XmlNode node in serviceEndPoint.ChildNodes)
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

                                                            fromContact.EndPointData[epid].IMCapabilities = cap;
                                                            fromContact.EndPointData[epid].IMCapabilitiesEx = capEx;

                                                            break;
                                                    }
                                                }
                                                break;
                                            }

                                        case ServiceShortNames.PE:
                                            {
                                                foreach (XmlNode node in serviceEndPoint.ChildNodes)
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

                                                            fromContact.EndPointData[epid].PECapabilities = cap;
                                                            fromContact.EndPointData[epid].PECapabilitiesEx = capEx;

                                                            break;
                                                    }
                                                }

                                                fromContact.SetChangedPlace(new PlaceChangedEventArgs(fromContact.EndPointData[epid], PlaceChangedReason.SignedIn));


                                                break;
                                            }

                                        case ServiceShortNames.PD:
                                            {
                                                PrivateEndPointData privateEndPoint = fromContact.EndPointData[epid] as PrivateEndPointData;

                                                foreach (XmlNode node in serviceEndPoint.ChildNodes)
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

                                                Owner.SetChangedPlace(new PlaceChangedEventArgs(privateEndPoint, PlaceChangedReason.SignedIn));

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
                                        "[OnNFYReceived] Cannot complete the operation since circle not found: " + multiMimeMessage.From.ToString());

                                    return;
                                }

                                if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0 ||
                                    "<circle></circle>" == Encoding.UTF8.GetString(multiMimeMessage.InnerBody))
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
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
                                xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));
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

                                    if (account == Owner.Account)
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
                                    group.ContactList = new ContactList(new Guid(fromAccount.Split('@')[0]), Owner, this);

                                    lock (multiparties)
                                        multiparties[transId] = group;

                                    OnMultipartyCreated(new MultipartyCreatedEventArgs(group));
                                }

                                if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
                                {
                                    // No xml content and full notify... Circle goes online...
                                    if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
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
                                xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));
                                XmlNodeList rosters = xmlDoc.SelectNodes("//circle/roster/user");
                                foreach (XmlNode roster in rosters)
                                {
                                    string state = (roster["state"] == null) ? string.Empty : roster["state"].InnerText;
                                    string[] fullAccount = roster["id"].InnerText.Split(':');
                                    IMAddressInfoType addressType = (IMAddressInfoType)int.Parse(fullAccount[0]);
                                    string memberAccount = fullAccount[1].ToLowerInvariant();

                                    // Me contact
                                    if ("pending" == state.ToLowerInvariant() &&
                                        addressType == Owner.ClientType &&
                                        memberAccount == Owner.Account)
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
                                string status = Encoding.UTF8.GetString(multiMimeMessage.InnerBody);

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
                switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType].Value)
                {
                    #region user xml
                    case "application/user+xml":
                        {
                            if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
                                return;  //No xml content.

                            if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
                            {
                                //This is an initial NFY
                            }

                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));

                            XmlNodeList services = xmlDoc.SelectNodes("//user/s");
                            XmlNodeList serviceEndPoints = xmlDoc.SelectNodes("//user/sep");

                            if (serviceEndPoints.Count > 0)
                            {
                                foreach (XmlNode serviceEndPoint in serviceEndPoints)
                                {
                                    ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), serviceEndPoint.Attributes["n"].Value);
                                    Guid epid = serviceEndPoint.Attributes["epid"] == null ? Guid.Empty : new Guid(serviceEndPoint.Attributes["epid"].Value);

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                        case ServiceShortNames.PD:
                                            {
                                                if (fromContact.EndPointData.ContainsKey(epid))
                                                {
                                                    fromContact.SetChangedPlace(new PlaceChangedEventArgs(fromContact.EndPointData[epid], PlaceChangedReason.SignedOut));
                                                }

                                                if (fromIsMe && epid == MachineGuid)
                                                {
                                                    SignoutFrom(epid);
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

                                    switch (serviceEnum)
                                    {
                                        case ServiceShortNames.IM:
                                            {
                                                PresenceStatus oldStatus = fromContact.Status;
                                                PresenceStatus newStatus = PresenceStatus.Offline;
                                                fromContact.SetStatus(newStatus);

                                                OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
                                                OnContactOffline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
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
                                        "[OnNFYReceived] Cannot complete the operation since circle not found: " + multiMimeMessage.From.ToString());

                                    return;
                                }
                            }
                            else if (fromAccountAddressType == IMAddressInfoType.TemporaryGroup)
                            {
                                group = GetMultiparty(fromAccount);

                                if (group == null)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] temp group not found: " + multiMimeMessage.From.ToString());

                                    return;
                                }
                            }
                            else
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "[OnNFYReceived] sender is not circle nor temp group: " + multiMimeMessage.From.ToString());

                                return;
                            }

                            if (multiMimeMessage.ContentHeaders.ContainsKey(MIMEHeaderStrings.Uri))
                            {
                                string xpathUri = multiMimeMessage.ContentHeaders[MIMEHeaderStrings.Uri].ToString();
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

            Contact sender = null; // via=fb, circle or temporary group
            Contact by = null; // invidiual sender, 1 on 1 chat

            if (toAccountAddressType == IMAddressInfoType.Circle)
            {
                sender = ContactList.GetCircle(toAccount);

                if (sender == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnSDGReceived] Error: Cannot find circle: " + mmMessage.To.ToString());

                    return;
                }

                if (sender.ContactList != null)
                {
                    by = sender.ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
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

                if (sender.ContactList != null)
                {
                    by = sender.ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
                }
            }

            // External Network
            if (by == null && fromViaAccountAddressType != IMAddressInfoType.None && !String.IsNullOrEmpty(fromViaAccount))
            {
                by = ContactList.GetContact(fromViaAccount, fromViaAccountAddressType);

                if (by != null && by.ContactList != null)
                {
                    sender = by.ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
                }
            }

            if (by == null)
            {
                sender = ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
                by = sender;
            }

            if (mmMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.MessageType))
            {
                if ("nudge" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    OnNudgeReceived(new NudgeArrivedEventArgs(sender, by));
                }
                else if ("control/typing" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    OnTypingMessageReceived(new TypingArrivedEventArgs(sender, by));
                }
                else if ("text" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    TextMessage txtMessage = new TextMessage(Encoding.UTF8.GetString(mmMessage.InnerBody));
                    StrDictionary strDic = new StrDictionary();
                    foreach (string key in mmMessage.ContentHeaders.Keys)
                    {
                        strDic.Add(key, mmMessage.ContentHeaders[key].ToString());
                    }
                    txtMessage.ParseHeader(strDic);

                    OnTextMessageReceived(new TextMessageArrivedEventArgs(sender, txtMessage, by));
                }
                else if ("customemoticon" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    EmoticonMessage emoticonMessage = new EmoticonMessage();
                    emoticonMessage.EmoticonType = mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] == "text/x-mms-animemoticon" ?
                        EmoticonType.AnimEmoticon : EmoticonType.StaticEmoticon;

                    emoticonMessage.ParseBytes(mmMessage.InnerBody);

                    foreach (Emoticon emoticon in emoticonMessage.Emoticons)
                    {
                        OnEmoticonDefinitionReceived(new EmoticonDefinitionEventArgs(sender, emoticon));
                    }
                }
                else if ("wink" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    Wink wink = new Wink();
                    wink.SetContext(Encoding.UTF8.GetString(mmMessage.InnerBody));

                    OnWinkDefinitionReceived(new WinkEventArgs(sender, wink));
                }
                else if ("signal/p2p" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    SLPMessage slpMessage = SLPMessage.Parse(mmMessage.InnerBody);
                    if (slpMessage != null)
                    {
                        if (slpMessage.ContentType == "application/x-msnmsgr-transreqbody" ||
                            slpMessage.ContentType == "application/x-msnmsgr-transrespbody" ||
                            slpMessage.ContentType == "application/x-msnmsgr-transdestaddrupdate")
                        {
                            P2PSession.ProcessDirectInvite(slpMessage, this, null);
                        }
                    }
                }
                else if ("data" == mmMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString().ToLowerInvariant())
                {
                    P2PVersion toVer = mmMessage.To.HasAttribute(MIMERoutingHeaders.EPID) ? P2PVersion.P2PV2 : P2PVersion.P2PV1;
                    Guid ep = (toVer == P2PVersion.P2PV1) ? Guid.Empty : new Guid(mmMessage.From[MIMERoutingHeaders.EPID]);
                    string[] offsets = mmMessage.ContentHeaders.ContainsKey("Bridging-Offsets") ?
                        mmMessage.ContentHeaders["Bridging-Offsets"].ToString().Split(',') :
                        new string[] { "0" };
                    List<long> offsetList = new List<long>();
                    foreach (string os in offsets)
                    {
                        offsetList.Add(long.Parse(os));
                    }

                    P2PMessage p2pData = new P2PMessage(toVer);
                    P2PMessage[] p2pDatas = p2pData.CreateFromOffsets(offsetList.ToArray(), mmMessage.InnerBody);

                    if (mmMessage.ContentHeaders.ContainsKey("Pipe"))
                    {
                        SDGBridge.packageNumber = ushort.Parse(mmMessage.ContentHeaders["Pipe"]);
                    }

                    foreach (P2PMessage m in p2pDatas)
                    {
                        P2PBridge p2pBridge = (by.DirectBridge != null && by.DirectBridge.IsOpen)
                            ? by.DirectBridge : SDGBridge;

                        P2PHandler.ProcessP2PMessage(p2pBridge, sender, ep, m);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
};
