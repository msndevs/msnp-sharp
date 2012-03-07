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
        protected internal virtual void OnContactStatusChanged(ContactStatusChangedEventArgs e)
        {
            if (ContactStatusChanged != null)
                ContactStatusChanged(this, e);
        }

        /// <summary>
        /// Occurs when any contact goes from offline status to another status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactOnline;
        protected internal virtual void OnContactOffline(ContactStatusChangedEventArgs e)
        {
            if (ContactOffline != null)
                ContactOffline(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact.ToString() + " goes to " + e.NewStatus + " from " + e.OldStatus + (e.Via == null ? String.Empty : " via=" + e.Via.ToString()) + "\r\n", GetType().Name);
        }

        /// <summary>
        /// Occurs when any contact goes from any status to offline status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactOffline;
        protected internal virtual void OnContactOnline(ContactStatusChangedEventArgs e)
        {
            if (ContactOnline != null)
                ContactOnline(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact.ToString() + " goes to " + e.NewStatus + " from " + e.OldStatus + (e.Via == null ? String.Empty : " via=" + e.Via.ToString()) + "\r\n", GetType().Name);
        }

        /// <summary>
        /// Occurs when a user is typing.
        /// </summary>
        public event EventHandler<TypingArrivedEventArgs> TypingMessageReceived;
        protected virtual void OnTypingMessageReceived(TypingArrivedEventArgs e)
        {
            if (TypingMessageReceived != null)
                TypingMessageReceived(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "TYPING: " + e.OriginalSender.ToString() + (e.Sender == e.OriginalSender ? String.Empty : ";by=" + e.Sender.ToString()));
        }

        /// <summary>
        /// Occurs when we receive a nudge message by a user.
        /// </summary>
        public event EventHandler<NudgeArrivedEventArgs> NudgeReceived;
        protected virtual void OnNudgeReceived(NudgeArrivedEventArgs e)
        {
            if (NudgeReceived != null)
                NudgeReceived(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "NUDGE: " + e.OriginalSender + (e.Sender == e.OriginalSender ? String.Empty : ";by=" + e.Sender.ToString()));
        }

        /// <summary>
        /// Occurs when we receive a text message from a user.
        /// </summary>
        public event EventHandler<TextMessageArrivedEventArgs> TextMessageReceived;
        protected virtual void OnTextMessageReceived(TextMessageArrivedEventArgs e)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "TEXT MESSAGE: " + e.OriginalSender.ToString() + (e.Sender == e.OriginalSender ? String.Empty : ";by=" + e.Sender.ToString()) + "\r\n" + e.TextMessage.ToDebugString());
        }


        /// <summary>
        /// Fired when a contact sends a emoticon definition.
        /// </summary>
        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;
        protected virtual void OnEmoticonDefinitionReceived(EmoticonDefinitionEventArgs e)
        {
            if (EmoticonDefinitionReceived != null)
                EmoticonDefinitionReceived(this, e);
        }

        /// <summary>
        /// Fired when a contact sends a wink definition.
        /// </summary>
        public event EventHandler<WinkEventArgs> WinkDefinitionReceived;
        protected virtual void OnWinkDefinitionReceived(WinkEventArgs e)
        {
            if (WinkDefinitionReceived != null)
                WinkDefinitionReceived(this, e);
        }

        /// <summary>
        /// Occurs when a multiparty chat created remotely. Owner is joined automatically by the library.
        /// </summary>
        public event EventHandler<MultipartyCreatedEventArgs> MultipartyCreatedRemotely;
        protected virtual void OnMultipartyCreatedRemotely(MultipartyCreatedEventArgs e)
        {
            if (MultipartyCreatedRemotely != null)
                MultipartyCreatedRemotely(this, e);
        }

        /// <summary>
        /// Occurs when a contact joined the group chat.
        /// </summary>
        public event EventHandler<GroupChatParticipationEventArgs> JoinedGroupChat;
        protected virtual void OnJoinedGroupChat(GroupChatParticipationEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                e.Contact + " joined group chat " + e.Via.ToString(), GetType().Name);

            if (JoinedGroupChat != null)
                JoinedGroupChat(this, e);
        }

        /// <summary>
        /// Occurs when a contact left the group chat.
        /// </summary>
        public event EventHandler<GroupChatParticipationEventArgs> LeftGroupChat;
        protected virtual void OnLeftGroupChat(GroupChatParticipationEventArgs e)
        {
            if (LeftGroupChat != null)
                LeftGroupChat(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               e.Contact + " left group chat " + e.Via.ToString(), GetType().Name);
        }

        /// <summary>
        /// Occurs after the user on another end point closed the IM window.
        /// </summary>
        public event EventHandler<CloseIMWindowEventArgs> RemoteEndPointCloseIMWindow;
        protected virtual void OnRemoteEndPointCloseIMWindow(CloseIMWindowEventArgs e)
        {
            if (RemoteEndPointCloseIMWindow != null)
                RemoteEndPointCloseIMWindow(this, e);

            if (e.Sender != null && e.SenderEndPoint != null)
            {
                string partiesString = string.Empty;
                foreach (Contact party in e.Parties)
                {
                    partiesString += party.ToString() + "\r\n";
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                  "User at End Point: " + e.SenderEndPoint.ToString() + " has closed the IM window.\r\n" +
                                  "Parties in the conversation: \r\n" +
                                  partiesString);
            }
        }

        #endregion

        #region MULTIPARTY

        #region CreateMultiparty

        internal class MultipartyObject
        {
            public event EventHandler<MultipartyCreatedEventArgs> MultipartyCreatedLocally;

            public int TransactionID;
            public List<string> InviteQueueHash;
            public Contact MultiParty;

            public MultipartyObject(int transId, List<string> inviteQueueHash, Contact multiParty,
                EventHandler<MultipartyCreatedEventArgs> onCreated)
            {
                TransactionID = transId;
                InviteQueueHash = new List<string>(inviteQueueHash);
                MultiParty = multiParty;

                if (onCreated != null)
                    MultipartyCreatedLocally += onCreated;
            }

            internal void OnMultipartyCreatedLocally(object sender, MultipartyCreatedEventArgs e)
            {
                if (MultipartyCreatedLocally != null)
                {
                    MultipartyCreatedLocally(sender, e);

                    MultipartyCreatedLocally -= OnMultipartyCreatedLocally;
                }
            }
        };

        /// <summary>
        /// Creates a new multiparty (Group chat)
        /// </summary>
        /// <param name="inviteQueue">Contacts to be invited (don't add yourself)</param>
        /// <param name="onCreated">The handler to be executed when multiparty created (must be provided)</param>
        /// <exception cref="ArgumentNullException">inviteQueue or event handler is null</exception>
        /// <exception cref="InvalidOperationException">At least 2 contacts is required except you and contacts must support multiparty</exception>
        /// <returns>Transaction ID</returns>
        public int CreateMultiparty(List<Contact> inviteQueue, EventHandler<MultipartyCreatedEventArgs> onCreated)
        {
            if (inviteQueue == null || inviteQueue.Count == 0)
                throw new ArgumentNullException("inviteQueue");

            if (onCreated == null)
                throw new ArgumentNullException("onCreated");

            List<string> newQueue = new List<string>();

            foreach (Contact c in inviteQueue)
            {
                if (c != null &&
                    !c.IsSibling(Owner) &&
                    !newQueue.Contains(c.SiblingString) &&
                    c.SupportsMultiparty)
                {
                    newQueue.Add(c.SiblingString);
                }
            }

            if (newQueue.Count < 2)
                throw new InvalidOperationException("At least 2 contacts is required except you and contacts must support multiparty.");


            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
            int transId = nsmp.IncreaseTransactionID();

            lock (multiParties)
                multiParties[transId] = new MultipartyObject(transId, newQueue, null, onCreated);

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
                lock (multiParties)
                {
                    foreach (MultipartyObject group in multiParties.Values)
                    {
                        if (group.MultiParty != null && group.MultiParty.Account == tempGroupAddress)
                            return group.MultiParty;
                    }
                }
            }

            return null;
        }

        internal MultipartyObject GetMultipartyObject(string tempGroupAddress)
        {
            if (!String.IsNullOrEmpty(tempGroupAddress))
            {
                lock (multiParties)
                {
                    foreach (MultipartyObject group in multiParties.Values)
                    {
                        if (group.MultiParty != null && group.MultiParty.Account == tempGroupAddress)
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

            lock (multiParties)
            {
                int delTransId = 0;
                foreach (MultipartyObject g in multiParties.Values)
                {
                    if (g.MultiParty != null && g.MultiParty.Account == group.Account)
                    {
                        delTransId = g.TransactionID;
                        break;
                    }
                }
                if (delTransId != 0)
                    multiParties.Remove(delTransId);
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

            if (remoteContact.Via != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.Via.ClientType).ToString() + ":" + remoteContact.Via.Account;
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = MessageTypes.ControlTyping;
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

            if (remoteContact.Via != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.Via.ClientType).ToString() + ":" + remoteContact.Via.Account;
            }

            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = MessageTypes.Nudge;
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

            if (remoteContact.Via != null)
            {
                mmMessage.RoutingHeaders[MIMERoutingHeaders.To]["via"] =
                    ((int)remoteContact.Via.ClientType).ToString() + ":" + remoteContact.Via.Account;
            }


            mmMessage.ContentKeyVersion = "2.0";

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = MessageTypes.Text;
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
            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = MessageTypes.Text;
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

            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = MessageTypes.CustomEmoticon;
            mmMessage.ContentHeaders[MIMEContentHeaders.ContentType] = icontype == EmoticonType.AnimEmoticon ? "text/x-mms-animemoticon" : "text/x-mms-emoticon";
            mmMessage.InnerBody = emoticonMessage.GetBytes();

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.InnerMessage = mmMessage;
            MessageProcessor.SendMessage(sdgPayload);
        }

        #endregion

        #endregion

        #region PRESENCE

        #region SignoutFrom

        internal void SignoutFrom(Guid endPointID)
        {
            if (messageProcessor == null || messageProcessor.Connected == false)
                return;

            if (endPointID == MachineGuid)
            {
                messageProcessor.Disconnect();
                return;
            }

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
        }

        #endregion

        #region SetScreenName & SetPersonalMessage

        /// <summary>
        /// Sets the contactlist owner's screenname. After receiving confirmation from the server
        /// this will set the Owner object's name which will in turn raise the NameChange event.
        /// </summary>
        internal void SetScreenName(string newName)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (string.IsNullOrEmpty(newName))
            {
                newName = Owner.Account;
            }

            PersonalMessage pm = Owner.PersonalMessage;

            pm.FriendlyName = newName;

            SetPersonalMessage(pm);
        }

        /// <summary>
        /// Sets personal message.
        /// </summary>
        internal void SetPersonalMessage(PersonalMessage newPSM)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (Owner.Status != PresenceStatus.Offline)
            {
                SetPresenceStatus(
                    Owner.Status,
                    Owner.LocalEndPointIMCapabilities, Owner.LocalEndPointIMCapabilitiesEx,
                    Owner.LocalEndPointPECapabilities, Owner.LocalEndPointPECapabilitiesEx,
                    Owner.EpName, newPSM, true);
            }
        }

        /// <summary>
        /// Sets the scene image and scheme context.
        /// </summary>
        internal void SetSceneData(SceneImage scimg, Color sccolor)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            PersonalMessage pm = Owner.PersonalMessage;

            pm.ColorScheme = sccolor;
            pm.Scene = scimg.IsDefaultImage ? String.Empty : scimg.ContextPlain;

            SetPresenceStatus(Owner.Status,
                Owner.LocalEndPointIMCapabilities, Owner.LocalEndPointIMCapabilitiesEx,
                Owner.LocalEndPointPECapabilities, Owner.LocalEndPointPECapabilitiesEx,
                Owner.EpName, pm, true);
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

            bool setAll = (Owner.Status == PresenceStatus.Offline);

            if (setAll || forcePEservice ||
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
                if (setAll || forcePEservice ||
                    newStatus != Owner.Status)
                {
                    XmlElement service = xmlDoc.CreateElement("s");
                    service.SetAttribute("n", ServiceShortNames.IM.ToString());
                    service.InnerXml =
                        "<Status>" + ParseStatus(newStatus) + "</Status>" +
                        "<CurrentMedia>" + MSNHttpUtility.XmlEncode(newPSM.CurrentMedia) + "</CurrentMedia>";

                    userElement.AppendChild(service);

                    // Don't call Owner.Status = newStatus.
                }

                // s.PE (UserTileLocation, FriendlyName, PSM, Scene, ColorScheme)
                if (setAll ||
                    forcePEservice)
                {
                    XmlElement service = xmlDoc.CreateElement("s");
                    service.SetAttribute("n", ServiceShortNames.PE.ToString());
                    service.InnerXml = newPSM.Payload;
                    userElement.AppendChild(service);

                    // Don't set owner.PersonalMessage here. It is replaced (with a new reference) when NFY PUT received.
                }

                // sep.IM (Capabilities)
                if (setAll ||
                    newLocalIMCaps != Owner.LocalEndPointIMCapabilities ||
                    newLocalIMCapsex != Owner.LocalEndPointIMCapabilitiesEx)
                {
                    ClientCapabilities localIMCaps = setAll ? ClientCapabilities.DefaultIM : newLocalIMCaps;
                    ClientCapabilitiesEx localIMCapsEx = setAll ? ClientCapabilitiesEx.DefaultIM : newLocalIMCapsex;

                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.IM.ToString());
                    XmlElement capabilities = xmlDoc.CreateElement("Capabilities");
                    capabilities.InnerText = ((long)localIMCaps).ToString() + ":" + ((long)localIMCapsEx).ToString();
                    sep.AppendChild(capabilities);
                    userElement.AppendChild(sep);

                    // Don't call Owner.LocalEndPointIMCapabilities. It is recursive call to this method.
                }

                // sep.PE (Capabilities)
                if (setAll ||
                    newLocalPECaps != Owner.LocalEndPointPECapabilities ||
                    newLocalPECapsex != Owner.LocalEndPointPECapabilitiesEx)
                {
                    ClientCapabilities localPECaps = setAll ? ClientCapabilities.DefaultPE : newLocalPECaps;
                    ClientCapabilitiesEx localPECapsEx = setAll ? ClientCapabilitiesEx.DefaultPE : newLocalPECapsex;

                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.PE.ToString());
                    XmlElement VER = xmlDoc.CreateElement("VER");
                    VER.InnerText = Credentials.ClientInfo.MessengerClientName + ":" + Credentials.ClientInfo.MessengerClientBuildVer;
                    sep.AppendChild(VER);
                    XmlElement TYP = xmlDoc.CreateElement("TYP");
                    TYP.InnerText = "1";
                    sep.AppendChild(TYP);
                    XmlElement capabilities = xmlDoc.CreateElement("Capabilities");
                    capabilities.InnerText = ((long)localPECaps).ToString() + ":" + ((long)localPECapsEx).ToString();
                    sep.AppendChild(capabilities);
                    userElement.AppendChild(sep);

                    // Don't call Owner.LocalEndPointPECapabilities. It is recursive call to this method.
                }

                // sep.PD (EpName, State)
                if (setAll ||
                    newEPName != Owner.EpName ||
                    newStatus != Owner.Status)
                {
                    XmlElement sep = xmlDoc.CreateElement("sep");
                    sep.SetAttribute("n", ServiceShortNames.PD.ToString());
                    XmlElement clientType = xmlDoc.CreateElement("ClientType");
                    clientType.InnerText = "1";
                    sep.AppendChild(clientType);
                    XmlElement epName = xmlDoc.CreateElement("EpName");
                    epName.InnerText = MSNHttpUtility.XmlEncode(newEPName);
                    sep.AppendChild(epName);
                    XmlElement idle = xmlDoc.CreateElement("Idle");
                    idle.InnerText = ((newStatus == PresenceStatus.Idle) ? "true" : "false");
                    sep.AppendChild(idle);
                    XmlElement state = xmlDoc.CreateElement("State");
                    state.InnerText = ParseStatus(newStatus);
                    sep.AppendChild(state);
                    userElement.AppendChild(sep);

                    // Don't set Owner.EpName. It is recursive call to this method.
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
            if (multiParties.ContainsKey(message.TransactionID))
            {
                if (ok == false || message.InnerBody == null || message.InnerBody.Length == 0)
                {
                    lock (multiParties)
                        multiParties.Remove(message.TransactionID);

                    return;
                }

                MultiMimeMessage mmMessage = new MultiMimeMessage(message.InnerBody);
                string[] tempGroup = mmMessage.From.Value.Split(':');
                IMAddressInfoType addressType = (IMAddressInfoType)int.Parse(tempGroup[0]);

                if (addressType == IMAddressInfoType.TemporaryGroup)
                {
                    Contact group = new Contact(tempGroup[1].ToLowerInvariant(), IMAddressInfoType.TemporaryGroup, this);
                    group.ContactList = new ContactList(new Guid(tempGroup[1].ToLowerInvariant().Split('@')[0]), null, group, this);

                    MultipartyObject mpo = multiParties[message.TransactionID];
                    mpo.TransactionID = message.TransactionID;
                    mpo.MultiParty = group;

                    JoinMultiparty(group);

                    List<string> copy = new List<string>(mpo.InviteQueueHash);

                    foreach (string siblingHash in copy)
                    {
                        string[] addressTypeAndAccount = siblingHash.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        Contact contact = ContactList.GetContactWithCreate(addressTypeAndAccount[1], (IMAddressInfoType)Enum.Parse(typeof(IMAddressInfoType), addressTypeAndAccount[0].ToString()));

                        InviteContactToMultiparty(contact, group);
                    }

                    mpo.OnMultipartyCreatedLocally(this, new MultipartyCreatedEventArgs(group));

                    group.SetStatus(PresenceStatus.Online);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "MultipartyCreated: " + group.Account);
                }
                else
                {
                    lock (multiParties)
                        multiParties.Remove(message.TransactionID);
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

        private void OnNFYPUTReceived(MultiMimeMessage multiMimeMessage, RoutingInfo routingInfo)
        {
            switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType].Value)
            {
                #region user xml
                case "application/user+xml":
                    {

                        if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Sync")
                        {
                            if (routingInfo.SenderGateway != null && routingInfo.SenderGateway.ClientType == IMAddressInfoType.Circle)
                            {
                                JoinMultiparty(routingInfo.SenderGateway);
                            }

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

                                                        if (routingInfo.FromOwner && IsSignedIn == false)
                                                        {
                                                            // We have already signed in another place, but not here...
                                                            // Don't set status... This place will set the status later.
                                                            return;
                                                        }

                                                        PresenceStatus oldStatus = routingInfo.Sender.Status;
                                                        PresenceStatus newStatus = ParseStatus(node.InnerText);
                                                        routingInfo.Sender.SetStatus(newStatus);

                                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));
                                                        OnContactOnline(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));

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
                                            PersonalMessage personalMessage = new PersonalMessage(service.ChildNodes);

                                            if (!String.IsNullOrEmpty(personalMessage.Payload) &&
                                                routingInfo.Sender.PersonalMessage != personalMessage)
                                            {
                                                // FriendlyName
                                                if (!String.IsNullOrEmpty(personalMessage.FriendlyName))
                                                {
                                                    //Only Windows Live Messenger Contact has friendly name.
                                                    routingInfo.Sender.SetName(personalMessage.FriendlyName);
                                                }

                                                // UserTileLocation
                                                if (!String.IsNullOrEmpty(personalMessage.UserTileLocation) && routingInfo.Sender.UserTileLocation != personalMessage.UserTileLocation)
                                                {
                                                    routingInfo.Sender.UserTileLocation = personalMessage.UserTileLocation;
                                                    routingInfo.Sender.FireDisplayImageContextChangedEvent(personalMessage.UserTileLocation);
                                                }

                                                // Scene
                                                if (!String.IsNullOrEmpty(personalMessage.Scene))
                                                {
                                                    if (routingInfo.Sender.SceneContext != personalMessage.Scene)
                                                    {
                                                        routingInfo.Sender.SceneContext = personalMessage.Scene;
                                                        routingInfo.Sender.FireSceneImageContextChangedEvent(personalMessage.Scene);
                                                    }
                                                }

                                                // ColorScheme
                                                if (personalMessage.ColorScheme != Color.Empty)
                                                {
                                                    if (routingInfo.Sender.ColorScheme != personalMessage.ColorScheme)
                                                    {
                                                        routingInfo.Sender.ColorScheme = personalMessage.ColorScheme;
                                                        routingInfo.Sender.OnColorSchemeChanged();
                                                    }
                                                }

                                                // This must be final... 
                                                routingInfo.Sender.PersonalMessage = personalMessage;

                                            }
                                            break;
                                        }

                                    case ServiceShortNames.PF:
                                        {
                                            // Profile Annotation, it is AB.Me.annotations/Live.Profile.Expression.LastChanged
                                            // <user><s n="PF" ts="2011-04-16T06:00:58Z"></s></user>
                                            if (routingInfo.FromOwner)
                                            {
                                                DateTime ts = WebServiceDateTimeConverter.ConvertToDateTime(service.Attributes["ts"].Value);
                                            }
                                            break;
                                        }
                                }
                            }
                        }

                        if (serviceEndPoints.Count > 0)
                        {
                            foreach (XmlNode serviceEndPoint in serviceEndPoints)
                            {
                                ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), serviceEndPoint.Attributes["n"].Value);
                                Guid epid = serviceEndPoint.Attributes["epid"] == null ? Guid.Empty : new Guid(serviceEndPoint.Attributes["epid"].Value);

                                if (!routingInfo.Sender.EndPointData.ContainsKey(epid))
                                {
                                    lock (routingInfo.Sender.SyncObject)
                                        routingInfo.Sender.EndPointData.Add(epid, routingInfo.FromOwner ? new PrivateEndPointData(routingInfo.Sender.Account, epid) : new EndPointData(routingInfo.Sender.Account, epid));
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

                                                        routingInfo.Sender.EndPointData[epid].IMCapabilities = cap;
                                                        routingInfo.Sender.EndPointData[epid].IMCapabilitiesEx = capEx;

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

                                                        routingInfo.Sender.EndPointData[epid].PECapabilities = cap;
                                                        routingInfo.Sender.EndPointData[epid].PECapabilitiesEx = capEx;

                                                        break;
                                                }
                                            }

                                            routingInfo.Sender.SetChangedPlace(new PlaceChangedEventArgs(routingInfo.Sender.EndPointData[epid], PlaceChangedReason.SignedIn));


                                            break;
                                        }

                                    case ServiceShortNames.PD:
                                        {
                                            PrivateEndPointData privateEndPoint = routingInfo.Sender.EndPointData[epid] as PrivateEndPointData;

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
                        if (routingInfo.SenderType == IMAddressInfoType.Circle)
                        {
                            Contact circle = ContactList.GetCircle(routingInfo.SenderAccount);

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
                        else if (routingInfo.SenderType == IMAddressInfoType.TemporaryGroup)
                        {
                            MultipartyObject mpo = GetMultipartyObject(routingInfo.SenderAccount);
                            Contact group = null;

                            if (mpo == null)
                            {
                                // Created remotely.
                                NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
                                int transId = nsmp.IncreaseTransactionID();

                                group = new Contact(routingInfo.SenderAccount, IMAddressInfoType.TemporaryGroup, this);
                                group.ContactList = new ContactList(new Guid(routingInfo.SenderAccount.Split('@')[0]), null, group, this);

                                mpo = new MultipartyObject(transId, new List<string>(), group, null);

                                lock (multiParties)
                                    multiParties[transId] = mpo;

                                OnMultipartyCreatedRemotely(new MultipartyCreatedEventArgs(group));

                                group.SetStatus(PresenceStatus.Online);
                            }
                            else
                            {
                                group = mpo.MultiParty;
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
                                    Contact part = group.ContactList.GetContactWithCreate(memberAccount, addressType);
                                    Contact real = ContactList.GetContactWithCreate(memberAccount, addressType);
                                    part.SetStatus(real.Status);
                                    OnJoinedGroupChat(new GroupChatParticipationEventArgs(part, group));

                                    if (mpo.InviteQueueHash.Contains(part.SiblingString))
                                        mpo.InviteQueueHash.Remove(part.SiblingString);
                                }
                            }
                        }
                    }
                    break;
                #endregion

                #region network xml
                case "application/network+xml":
                    {
                        if (routingInfo.Sender.ClientType == IMAddressInfoType.RemoteNetwork &&
                            routingInfo.Sender.Account == RemoteNetworkGateways.FaceBookGatewayAccount)
                        {
                            string status = Encoding.UTF8.GetString(multiMimeMessage.InnerBody);

                            PresenceStatus oldStatus = routingInfo.Sender.Status;
                            PresenceStatus newStatus = PresenceStatus.Unknown;

                            if (status.Contains("SignedIn"))
                                newStatus = PresenceStatus.Online;
                            else if (status.Contains("SignedOut"))
                                newStatus = PresenceStatus.Offline;

                            if (newStatus != PresenceStatus.Unknown)
                            {
                                routingInfo.Sender.SetStatus(newStatus);

                                // The contact changed status
                                OnContactStatusChanged(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));

                                if (newStatus == PresenceStatus.Online)
                                    OnContactOnline(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));
                                else
                                    OnContactOffline(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));
                            }
                        }
                    }
                    break;
                #endregion
            }
        }

        private void OnNFYDELReceived(MultiMimeMessage multiMimeMessage, RoutingInfo routingInfo)
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
                                            if (routingInfo.Sender.EndPointData.ContainsKey(epid))
                                            {
                                                routingInfo.Sender.SetChangedPlace(new PlaceChangedEventArgs(routingInfo.Sender.EndPointData[epid], PlaceChangedReason.SignedOut));
                                            }

                                            if (routingInfo.FromOwner && epid == MachineGuid &&
                                                messageProcessor != null)
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
                                            PresenceStatus oldStatus = routingInfo.Sender.Status;
                                            PresenceStatus newStatus = PresenceStatus.Offline;
                                            routingInfo.Sender.SetStatus(newStatus);

                                            OnContactStatusChanged(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));
                                            OnContactOffline(new ContactStatusChangedEventArgs(routingInfo.Sender, routingInfo.SenderGateway, oldStatus, newStatus));
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

                        if (routingInfo.SenderType == IMAddressInfoType.Circle)
                        {
                            circle = ContactList.GetCircle(routingInfo.SenderAccount);

                            if (circle == null)
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                    "[OnNFYReceived] Cannot complete the operation since circle not found: " + multiMimeMessage.From.ToString());

                                return;
                            }
                        }
                        else if (routingInfo.SenderType == IMAddressInfoType.TemporaryGroup)
                        {
                            group = GetMultiparty(routingInfo.SenderAccount);

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
                                    OnContactStatusChanged(new ContactStatusChangedEventArgs(goesOfflineGroup, routingInfo.SenderGateway, oldStatus, newStatus));

                                    // the contact goes offline
                                    OnContactOffline(new ContactStatusChangedEventArgs(goesOfflineGroup, routingInfo.SenderGateway, oldStatus, newStatus));

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


            RoutingInfo routingInfo = RoutingInfo.FromMultiMimeMessage(multiMimeMessage, this);
            if (routingInfo == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnNFYReceived] Get Rounting Info Error.");
                return;
            }

            if (command == "PUT")
            {
                OnNFYPUTReceived(multiMimeMessage, routingInfo);
            }
            else if (command == "DEL")
            {
                OnNFYDELReceived(multiMimeMessage, routingInfo);
            }
        }

        #endregion

        #region OnSDGReceived

        //This is actually another OnMSGxxx

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
            if (message.InnerBody == null || (message.InnerMessage is MultiMimeMessage) == false)
            {
                // This is not an SDG MultiMimeMessage message
                return;
            }

            MultiMimeMessage multiMimeMessage = message.InnerMessage as MultiMimeMessage;

            #region Get the Routing Info

            RoutingInfo routingInfo = RoutingInfo.FromMultiMimeMessage(multiMimeMessage, this);
            if (routingInfo == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnSDGReceived] Get Rounting Info Error.");
                return;
            }

            Contact sender = null; // via=fb, circle or temporary group
            Contact originalSender = null; // invidiual sender, 1 on 1 chat

            if (routingInfo.ReceiverType == IMAddressInfoType.Circle ||
                routingInfo.ReceiverType == IMAddressInfoType.TemporaryGroup)
            {
                sender = routingInfo.Receiver;

                if (sender == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnSDGReceived] Error: Cannot find group: " + multiMimeMessage.To.ToString());

                    return;
                }

                originalSender = routingInfo.Sender;
            }

            // External Network
            if (originalSender == null && routingInfo.SenderGateway != null)
            {
                originalSender = routingInfo.SenderGateway;
                sender = routingInfo.Sender;
            }

            if (originalSender == null)
            {
                sender = routingInfo.Sender;
                originalSender = sender;
            }

            #endregion

            if (multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.MessageType))
            {
                switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString())
                {
                    default:
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "[OnSDGReceived] UNHANDLED MESSAGE TYPE: \r\n" + multiMimeMessage.ContentHeaders[MIMEContentHeaders.MessageType].ToString() +
                            "\r\n\r\nMessage Body: \r\n\r\n" + multiMimeMessage.ToDebugString());
                        break;

                    case MessageTypes.Nudge:
                        OnNudgeReceived(new NudgeArrivedEventArgs(sender, originalSender, routingInfo));
                        break;

                    case MessageTypes.ControlTyping:
                        OnTypingMessageReceived(new TypingArrivedEventArgs(sender, originalSender, routingInfo));
                        break;

                    case MessageTypes.Text:
                        OnSDGTextMessageReceived(multiMimeMessage, sender, originalSender, routingInfo);
                        break;

                    case MessageTypes.CustomEmoticon:
                        OnSDGCustomEmoticonReceived(multiMimeMessage, sender, originalSender, routingInfo);
                        break;

                    case MessageTypes.Wink:
                        OnSDGWinkReceived(multiMimeMessage, sender, originalSender, routingInfo);
                        break;

                    case MessageTypes.SignalP2P:
                        OnSDGP2PSignalReceived(multiMimeMessage, sender, originalSender, routingInfo);
                        break;

                    case MessageTypes.SignalCloseIMWindow:
                        OnSDGCloseIMWindowReceived(multiMimeMessage, routingInfo);
                        break;

                    case MessageTypes.Data:
                        OnSDGDataMessageReceived(multiMimeMessage, sender, originalSender, routingInfo);
                        break;
                }
            }
        }

        #region Process SDG Messages

        private void OnSDGCloseIMWindowReceived(MultiMimeMessage multiMimeMessage, RoutingInfo routingInfo)
        {
            string partiesString = (multiMimeMessage.InnerMessage as TextPayloadMessage).Text;
            if (string.IsNullOrEmpty(partiesString))
                return;
            string[] parties = partiesString.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            IMAddressInfoType addressInfo = IMAddressInfoType.None;
            string account = string.Empty;

            List<Contact> partiesList = new List<Contact>(0);

            for (int i = 0; i < parties.Length; i++)
            {
                Contact.ParseFullAccount(parties[i], out addressInfo, out account);

                Contact party = ContactList.GetContact(account, addressInfo);
                if (party != null)
                    partiesList.Add(party);
            }

            EndPointData senderEndPoint = null;
            EndPointData receiverEndPoint = null;

            if (routingInfo.Sender != null)
            {
                if (routingInfo.Sender.EndPointData.ContainsKey(routingInfo.SenderEndPointID))
                    senderEndPoint = routingInfo.Sender.EndPointData[routingInfo.SenderEndPointID];
            }

            if (routingInfo.Receiver != null)
            {
                if (routingInfo.Receiver.EndPointData.ContainsKey(routingInfo.ReceiverEndPointID))
                    receiverEndPoint = routingInfo.Receiver.EndPointData[routingInfo.ReceiverEndPointID];
            }

            OnRemoteEndPointCloseIMWindow(new CloseIMWindowEventArgs(routingInfo.Sender, senderEndPoint,
                                                                     routingInfo.Receiver, receiverEndPoint,
                                                                     partiesList.ToArray()));
        }

        private void OnSDGWinkReceived(MultiMimeMessage multiMimeMessage, Contact sender, Contact originalSender, RoutingInfo routingInfo)
        {
            Wink wink = new Wink();
            wink.SetContext((multiMimeMessage.InnerMessage as TextPayloadMessage).Text);

            OnWinkDefinitionReceived(new WinkEventArgs(originalSender, wink, routingInfo));
        }

        private void OnSDGCustomEmoticonReceived(MultiMimeMessage multiMimeMessage, Contact sender, Contact originalSender, RoutingInfo routingInfo)
        {
            EmoticonMessage emoticonMessage = multiMimeMessage.InnerMessage as EmoticonMessage;

            foreach (Emoticon emoticon in emoticonMessage.Emoticons)
            {
                OnEmoticonDefinitionReceived(new EmoticonDefinitionEventArgs(originalSender, originalSender, routingInfo, emoticon));
            }
        }

        private void OnSDGTextMessageReceived(MultiMimeMessage multiMimeMessage, Contact sender, Contact by, RoutingInfo routingInfo)
        {
            TextMessage txtMessage = multiMimeMessage.InnerMessage as TextMessage;

            OnTextMessageReceived(new TextMessageArrivedEventArgs(sender, txtMessage, by, routingInfo));
        }

        private void OnSDGP2PSignalReceived(MultiMimeMessage multiMimeMessage, Contact sender, Contact by, RoutingInfo routingInfo)
        {
            //SLPMessage slpMessage = SLPMessage.Parse(multiMimeMessage.InnerBody);
            SLPMessage slpMessage = multiMimeMessage.InnerMessage as SLPMessage;

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

        private MultiMimeMessage ParseSDGDataMessage(MultiMimeMessage multiMimeMessage, Contact sender, Contact by)
        {
            return multiMimeMessage;
        }

        private void OnSDGDataMessageReceived(MultiMimeMessage multiMimeMessage, Contact sender, Contact by, RoutingInfo routingInfo)
        {
            Guid senderEPID = routingInfo.SenderEndPointID;
            P2PVersion p2pVer = senderEPID == Guid.Empty ? P2PVersion.P2PV1 : P2PVersion.P2PV2;

            string[] offsets = multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.BridgingOffsets) ?
                multiMimeMessage.ContentHeaders[MIMEContentHeaders.BridgingOffsets].ToString().Split(',') :
                new string[] { "0" };
            List<long> offsetList = new List<long>();
            foreach (string os in offsets)
            {
                offsetList.Add(long.Parse(os));
            }

            P2PMessage p2pData = new P2PMessage(p2pVer);
            P2PMessage[] p2pDatas = p2pData.CreateFromOffsets(offsetList.ToArray(), multiMimeMessage.InnerBody);

            if (multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.Pipe))
            {
                SDGBridge.PackageNo = ushort.Parse(multiMimeMessage.ContentHeaders[MIMEContentHeaders.Pipe]);
            }

            foreach (P2PMessage m in p2pDatas)
            {
                SDGBridge.ProcessP2PMessage(sender, senderEPID, m);
            }
        }

        #endregion

        #endregion

        #endregion
    }
};
