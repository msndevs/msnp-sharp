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
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    internal class RoutingInfo
    {
        private Guid senderEndPointID = Guid.Empty;

        public Guid SenderEndPointID
        {
            get
            {
                return senderEndPointID;
            }
            private set
            {
                senderEndPointID = value;
            }
        }

        private Guid receiverEndPointID = Guid.Empty;

        public Guid ReceiverEndPointID
        {
            get
            {
                return receiverEndPointID;
            }
            private set
            {
                receiverEndPointID = value;
            }
        }


        private Contact sender = null;

        public Contact Sender
        {
            get
            {
                return sender;
            }
            private set
            {
                sender = value;
            }
        }


        private Contact senderGateway = null;

        public Contact SenderGateway
        {
            get
            {
                return senderGateway;
            }
            private set
            {
                senderGateway = value;
            }
        }


        private Contact receiver = null;

        public Contact Receiver
        {
            get
            {
                return receiver;
            }
            private set
            {
                receiver = value;
            }
        }

        private Contact receiverGateway = null;

        public Contact ReceiverGateway
        {
            get
            {
                return receiverGateway;
            }
            private set
            {
                receiverGateway = value;
            }
        }

        public bool FromOwner
        {
            get
            {
                return Sender == this.NSMessageHandler.Owner;
            }
        }

        private NSMessageHandler nsMessageHandler = null;
        protected NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
            set
            {
                nsMessageHandler = value;
            }
        }

        private Contact messageGateway = null;

        public Contact MessageGateway
        {
            get { return messageGateway; }
            private set { messageGateway = value; }
        }


        protected RoutingInfo(Contact sender, Contact senderGateway, Contact receiver, Contact receiverGateway, NSMessageHandler nsMessageHandler)
        {
            Sender = sender;
            SenderGateway = senderGateway;

            Receiver = receiver;
            ReceiverGateway = receiverGateway;

            this.NSMessageHandler = nsMessageHandler;
        }

        private static Contact GetGatewayFromAccountString(string accountString, NSMessageHandler nsMessageHandler)
        {
            IMAddressInfoType gatewayAddressType = IMAddressInfoType.None;
            string gatewayAccount = string.Empty;
            IMAddressInfoType ignoreAddressType = IMAddressInfoType.None;
            string ignoreAccount = string.Empty;

            Contact.ParseFullAccount(accountString,
                out gatewayAddressType, out gatewayAccount,
                out ignoreAddressType, out ignoreAccount);

            Contact gateWay = GetGateway(gatewayAccount, gatewayAddressType, nsMessageHandler);
            return gateWay;
        }

        private static Contact GetGateway(string gatewayAccount, IMAddressInfoType gatewayType, NSMessageHandler nsMessageHandler)
        {
            Contact gateWay = null;

            if (gatewayType == IMAddressInfoType.Circle)
            {
                gateWay = nsMessageHandler.ContactList.GetCircle(gatewayAccount);
            }
            else if (gatewayType == IMAddressInfoType.TemporaryGroup)
            {
                gateWay = nsMessageHandler.GetMultiparty(gatewayAccount);
            }
            else
            {
                gateWay = nsMessageHandler.ContactList.GetContact(gatewayAccount, gatewayType);
            }

            return gateWay;
        }
        private static Guid GetEPID(MimeValue mimeAccountValue)
        {
            if (mimeAccountValue.HasAttribute("epid"))
                return new Guid(mimeAccountValue["epid"]);
            return Guid.Empty;
        }

        private static bool IsSpecialGatewayType(string account, IMAddressInfoType type)
        {
            if (type == IMAddressInfoType.Circle ||
                type == IMAddressInfoType.TemporaryGroup)
                return true;

            if (type == IMAddressInfoType.RemoteNetwork &&
                (account == RemoteNetworkGateways.FaceBookGatewayAccount ||
                account == RemoteNetworkGateways.LinkedInGateway
                ))
                return true;

            return false;
        }

        //#region OnNFYReceived

        ///// <summary>
        ///// Called when a NFY command has been received.
        ///// <code>
        ///// NFY [TransactionID] [Operation: PUT|DEL] [Payload Length]\r\n[Payload Data]
        ///// </code>
        ///// </summary>
        ///// <param name="message"></param>
        //protected virtual void OnNFYReceived(NSMessage message)
        //{
        //    NetworkMessage networkMessage = message as NetworkMessage;
        //    if (networkMessage.InnerBody == null || networkMessage.InnerBody.Length == 0)
        //        return;

        //    //PUT or DEL
        //    string command = message.CommandValues[0].ToString();

        //    MultiMimeMessage multiMimeMessage = new MultiMimeMessage(networkMessage.InnerBody);

        //    if (!(multiMimeMessage.ContentHeaders.ContainsKey(MIMEContentHeaders.ContentType)))
        //        return;

        //    IMAddressInfoType fromAccountAddressType;
        //    string fromAccount;
        //    IMAddressInfoType fromViaAccountAddressType;
        //    string fromViaAccount;

        //    IMAddressInfoType toAccountAddressType;
        //    string toAccount;
        //    IMAddressInfoType toViaAccountAddressType;
        //    string toViaAccount;

        //    if ((false == Contact.ParseFullAccount(multiMimeMessage.From.ToString(),
        //        out fromAccountAddressType, out fromAccount,
        //        out fromViaAccountAddressType, out fromViaAccount))
        //        ||
        //        (false == Contact.ParseFullAccount(multiMimeMessage.To.ToString(),
        //        out toAccountAddressType, out toAccount,
        //        out toViaAccountAddressType, out toViaAccount)))
        //    {
        //        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
        //            "[OnNFYReceived] Cannot parse from or to: " + multiMimeMessage.From.ToString() + "|" + multiMimeMessage.To.ToString());

        //        return;
        //    }

        //    Contact viaHeaderContact = null;
        //    Contact fromContact = null;
        //    bool fromIsMe = false;

        //    if (multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via) ||
        //        fromViaAccountAddressType != IMAddressInfoType.None ||
        //        toViaAccountAddressType != IMAddressInfoType.None)
        //    {
        //        string viaFull = multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via)
        //            ? multiMimeMessage.RoutingHeaders[MIMEHeaderStrings.Via].Value
        //            :
        //            (fromViaAccountAddressType != IMAddressInfoType.None ?
        //            (((int)fromViaAccountAddressType).ToString() + ":" + fromViaAccount)
        //            :
        //            (((int)toViaAccountAddressType).ToString() + ":" + toViaAccount));

        //        IMAddressInfoType viaHeaderAddressType;
        //        string viaHeaderAccount;
        //        IMAddressInfoType ignoreAddressType;
        //        string ignoreAccount;

        //        Contact.ParseFullAccount(viaFull,
        //            out viaHeaderAddressType, out viaHeaderAccount,
        //            out ignoreAddressType, out ignoreAccount);

        //        if (viaHeaderAddressType == IMAddressInfoType.Circle)
        //        {
        //            viaHeaderContact = ContactList.GetCircle(viaHeaderAccount);
        //            if (viaHeaderContact != null)
        //            {
        //                fromContact = viaHeaderContact.ContactList.GetContact(fromAccount, fromAccountAddressType);
        //            }
        //        }
        //        else if (viaHeaderAddressType == IMAddressInfoType.TemporaryGroup)
        //        {
        //            viaHeaderContact = GetMultiparty(viaHeaderAccount);
        //            if (viaHeaderContact != null)
        //            {
        //                fromContact = viaHeaderContact.ContactList.GetContact(fromAccount, fromAccountAddressType);
        //            }
        //        }
        //        else
        //        {
        //            viaHeaderContact = ContactList.GetContact(viaHeaderAccount, viaHeaderAddressType);
        //            if (viaHeaderContact != null)
        //            {
        //                fromContact = viaHeaderContact.ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
        //            }
        //        }
        //    }

        //    if (fromContact == null)
        //    {
        //        fromIsMe = (fromAccount == Owner.Account && fromAccountAddressType == IMAddressInfoType.WindowsLive);
        //        fromContact = fromIsMe ? Owner : ContactList.GetContactWithCreate(fromAccount, fromAccountAddressType);
        //    }

        //    fromContact.ViaContact = viaHeaderContact;

        //    if (command == "PUT")
        //    {
        //        switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType].Value)
        //        {
        //            #region user xml
        //            case "application/user+xml":
        //                {
        //                    if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Sync")
        //                    {
        //                        if (viaHeaderContact != null && viaHeaderContact.ClientType == IMAddressInfoType.Circle)
        //                        {
        //                            JoinMultiparty(viaHeaderContact);
        //                        }

        //                        //Sync the contact in contact list with the contact in gateway.
        //                        // TODO: Set the NSMessagehandler.ContactList contact to the gateway
        //                        // TODO: triger the ContactOnline event for the gateway contact.

        //                        //Just wait for my fix.
        //                    }

        //                    if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
        //                        return;  //No xml content.

        //                    if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
        //                    {
        //                        //This is an initial NFY
        //                    }

        //                    XmlDocument xmlDoc = new XmlDocument();
        //                    xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));

        //                    XmlNodeList services = xmlDoc.SelectNodes("//user/s");
        //                    XmlNodeList serviceEndPoints = xmlDoc.SelectNodes("//user/sep");

        //                    if (services.Count > 0)
        //                    {
        //                        foreach (XmlNode service in services)
        //                        {
        //                            ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);
        //                            switch (serviceEnum)
        //                            {
        //                                case ServiceShortNames.IM:
        //                                    {
        //                                        foreach (XmlNode node in service.ChildNodes)
        //                                        {
        //                                            switch (node.Name)
        //                                            {
        //                                                case "Status":

        //                                                    if (fromIsMe && IsSignedIn == false)
        //                                                    {
        //                                                        // We have already signed in another place, but not here...
        //                                                        // Don't set status... This place will set the status later.
        //                                                        return;
        //                                                    }

        //                                                    PresenceStatus oldStatus = fromContact.Status;
        //                                                    PresenceStatus newStatus = ParseStatus(node.InnerText);
        //                                                    fromContact.SetStatus(newStatus);

        //                                                    OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
        //                                                    OnContactOnline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));

        //                                                    break;

        //                                                case "CurrentMedia":
        //                                                    //MSNP21TODO: UBX implementation

        //                                                    break;
        //                                            }
        //                                        }
        //                                        break;
        //                                    }

        //                                case ServiceShortNames.PE:
        //                                    {
        //                                        // Create a new reference to fire PersonalMessageChanged event.
        //                                        PersonalMessage pm = new PersonalMessage(service.ChildNodes);

        //                                        if (!String.IsNullOrEmpty(pm.Payload) &&
        //                                            fromContact.PersonalMessage != pm)
        //                                        {
        //                                            fromContact.PersonalMessage = pm;

        //                                            // FriendlyName
        //                                            fromContact.SetName(String.IsNullOrEmpty(pm.FriendlyName) ? fromContact.Account : pm.FriendlyName);

        //                                            // UserTileLocation
        //                                            if (!String.IsNullOrEmpty(pm.UserTileLocation))
        //                                                fromContact.UserTileLocation = pm.UserTileLocation;

        //                                            // Scene
        //                                            if (!String.IsNullOrEmpty(pm.Scene))
        //                                            {
        //                                                if (fromContact.SceneContext != pm.Scene)
        //                                                {
        //                                                    fromContact.SceneContext = pm.Scene;
        //                                                    fromContact.FireSceneImageContextChangedEvent(pm.Scene);
        //                                                }
        //                                            }

        //                                            // ColorScheme
        //                                            if (pm.ColorScheme != Color.Empty)
        //                                            {
        //                                                if (fromContact.ColorScheme != pm.ColorScheme)
        //                                                {
        //                                                    fromContact.ColorScheme = pm.ColorScheme;
        //                                                    fromContact.OnColorSchemeChanged();
        //                                                }
        //                                            }

        //                                        }
        //                                    }
        //                                    break;
        //                            }
        //                        }
        //                    }

        //                    if (serviceEndPoints.Count > 0)
        //                    {
        //                        foreach (XmlNode serviceEndPoint in serviceEndPoints)
        //                        {
        //                            ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), serviceEndPoint.Attributes["n"].Value);
        //                            Guid epid = serviceEndPoint.Attributes["epid"] == null ? Guid.Empty : new Guid(serviceEndPoint.Attributes["epid"].Value);

        //                            if (!fromContact.EndPointData.ContainsKey(epid))
        //                            {
        //                                lock (fromContact.SyncObject)
        //                                    fromContact.EndPointData.Add(epid, fromIsMe ? new PrivateEndPointData(fromContact.Account, epid) : new EndPointData(fromContact.Account, epid));
        //                            }

        //                            switch (serviceEnum)
        //                            {
        //                                case ServiceShortNames.IM:
        //                                    {
        //                                        foreach (XmlNode node in serviceEndPoint.ChildNodes)
        //                                        {
        //                                            switch (node.Name)
        //                                            {
        //                                                case "Capabilities":

        //                                                    ClientCapabilities cap = ClientCapabilities.None;
        //                                                    ClientCapabilitiesEx capEx = ClientCapabilitiesEx.None;

        //                                                    string[] caps = node.InnerText.Split(':');
        //                                                    if (caps.Length > 1)
        //                                                    {
        //                                                        capEx = (ClientCapabilitiesEx)long.Parse(caps[1]);
        //                                                    }
        //                                                    cap = (ClientCapabilities)long.Parse(caps[0]);

        //                                                    fromContact.EndPointData[epid].IMCapabilities = cap;
        //                                                    fromContact.EndPointData[epid].IMCapabilitiesEx = capEx;

        //                                                    break;
        //                                            }
        //                                        }
        //                                        break;
        //                                    }

        //                                case ServiceShortNames.PE:
        //                                    {
        //                                        foreach (XmlNode node in serviceEndPoint.ChildNodes)
        //                                        {
        //                                            switch (node.Name)
        //                                            {
        //                                                case "Capabilities":

        //                                                    ClientCapabilities cap = ClientCapabilities.None;
        //                                                    ClientCapabilitiesEx capEx = ClientCapabilitiesEx.None;

        //                                                    string[] caps = node.InnerText.Split(':');
        //                                                    if (caps.Length > 1)
        //                                                    {
        //                                                        capEx = (ClientCapabilitiesEx)long.Parse(caps[1]);
        //                                                    }
        //                                                    cap = (ClientCapabilities)long.Parse(caps[0]);

        //                                                    fromContact.EndPointData[epid].PECapabilities = cap;
        //                                                    fromContact.EndPointData[epid].PECapabilitiesEx = capEx;

        //                                                    break;
        //                                            }
        //                                        }

        //                                        fromContact.SetChangedPlace(new PlaceChangedEventArgs(fromContact.EndPointData[epid], PlaceChangedReason.SignedIn));


        //                                        break;
        //                                    }

        //                                case ServiceShortNames.PD:
        //                                    {
        //                                        PrivateEndPointData privateEndPoint = fromContact.EndPointData[epid] as PrivateEndPointData;

        //                                        foreach (XmlNode node in serviceEndPoint.ChildNodes)
        //                                        {
        //                                            switch (node.Name)
        //                                            {
        //                                                case "ClientType":
        //                                                    privateEndPoint.ClientType = node.InnerText;
        //                                                    break;

        //                                                case "EpName":
        //                                                    privateEndPoint.Name = node.InnerText;
        //                                                    break;

        //                                                case "Idle":
        //                                                    privateEndPoint.Idle = bool.Parse(node.InnerText);
        //                                                    break;

        //                                                case "State":
        //                                                    privateEndPoint.State = ParseStatus(node.InnerText);
        //                                                    break;
        //                                            }
        //                                        }

        //                                        Owner.SetChangedPlace(new PlaceChangedEventArgs(privateEndPoint, PlaceChangedReason.SignedIn));

        //                                        break;
        //                                    }
        //                            }
        //                        }
        //                    }

        //                }
        //                break;
        //            #endregion

        //            #region circles xml
        //            case "application/circles+xml":
        //                {
        //                    if (fromAccountAddressType == IMAddressInfoType.Circle)
        //                    {
        //                        Contact circle = ContactList.GetCircle(fromAccount);

        //                        if (circle == null)
        //                        {
        //                            Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
        //                                "[OnNFYReceived] Cannot complete the operation since circle not found: " + multiMimeMessage.From.ToString());

        //                            return;
        //                        }

        //                        if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0 ||
        //                            "<circle></circle>" == Encoding.UTF8.GetString(multiMimeMessage.InnerBody))
        //                        {
        //                            // No xml content and full notify... Circle goes online...
        //                            if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
        //                            {
        //                                PresenceStatus oldStatus = circle.Status;
        //                                PresenceStatus newStatus = PresenceStatus.Online;
        //                                circle.SetStatus(newStatus);

        //                                // The contact changed status
        //                                OnContactStatusChanged(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));

        //                                // The contact goes online
        //                                OnContactOnline(new ContactStatusChangedEventArgs(circle, oldStatus, newStatus));
        //                            }
        //                            return;
        //                        }

        //                        XmlDocument xmlDoc = new XmlDocument();
        //                        xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));
        //                        XmlNodeList ids = xmlDoc.SelectNodes("//circle/roster/user/id");

        //                        if (ids.Count == 0)
        //                        {
        //                            return;  //I hate indent.
        //                        }

        //                        foreach (XmlNode node in ids)
        //                        {
        //                            IMAddressInfoType accountAddressType;
        //                            string account;
        //                            IMAddressInfoType viaAccountAddressType;
        //                            string viaAccount;
        //                            string fullAccount = node.InnerText;

        //                            if (false == Contact.ParseFullAccount(fullAccount,
        //                                out accountAddressType, out account,
        //                                out viaAccountAddressType, out viaAccount))
        //                            {
        //                                continue;
        //                            }

        //                            if (account == Owner.Account)
        //                                continue;

        //                            if (circle.ContactList.HasContact(account, accountAddressType))
        //                            {
        //                                Contact contact = circle.ContactList.GetContact(account, accountAddressType);
        //                                OnJoinedGroupChat(new GroupChatParticipationEventArgs(contact, circle));
        //                            }
        //                        }
        //                    }
        //                    else if (fromAccountAddressType == IMAddressInfoType.TemporaryGroup)
        //                    {
        //                        Contact group = GetMultiparty(fromAccount);

        //                        if (group == null)
        //                        {
        //                            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;
        //                            int transId = nsmp.IncreaseTransactionID();

        //                            group = new Contact(fromAccount, IMAddressInfoType.TemporaryGroup, this);
        //                            group.TransactionID = transId;
        //                            group.ContactList = new ContactList(new Guid(fromAccount.Split('@')[0]), Owner, this);

        //                            lock (multiparties)
        //                                multiparties[transId] = group;

        //                            OnMultipartyCreated(new MultipartyCreatedEventArgs(group));
        //                        }

        //                        if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
        //                        {
        //                            // No xml content and full notify... Circle goes online...
        //                            if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
        //                            {
        //                                PresenceStatus oldStatus = group.Status;
        //                                PresenceStatus newStatus = PresenceStatus.Online;
        //                                group.SetStatus(newStatus);

        //                                // The contact changed status
        //                                OnContactStatusChanged(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

        //                                // The contact goes online
        //                                OnContactOnline(new ContactStatusChangedEventArgs(group, oldStatus, newStatus));

        //                            }
        //                            return;
        //                        }

        //                        // Join multiparty if state is Pending
        //                        XmlDocument xmlDoc = new XmlDocument();
        //                        xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));
        //                        XmlNodeList rosters = xmlDoc.SelectNodes("//circle/roster/user");
        //                        foreach (XmlNode roster in rosters)
        //                        {
        //                            string state = (roster["state"] == null) ? string.Empty : roster["state"].InnerText;
        //                            string[] fullAccount = roster["id"].InnerText.Split(':');
        //                            IMAddressInfoType addressType = (IMAddressInfoType)int.Parse(fullAccount[0]);
        //                            string memberAccount = fullAccount[1].ToLowerInvariant();

        //                            // Me contact
        //                            if ("pending" == state.ToLowerInvariant() &&
        //                                addressType == Owner.ClientType &&
        //                                memberAccount == Owner.Account)
        //                            {
        //                                JoinMultiparty(group);
        //                            }
        //                            else
        //                            {
        //                                Contact contact = group.ContactList.GetContactWithCreate(memberAccount, addressType);
        //                                OnJoinedGroupChat(new GroupChatParticipationEventArgs(contact, group));
        //                            }
        //                        }
        //                    }
        //                }
        //                break;
        //            #endregion

        //            #region network xml
        //            case "application/network+xml":
        //                {
        //                    if (fromAccountAddressType == IMAddressInfoType.RemoteNetwork &&
        //                        fromAccount == RemoteNetworkGateways.FaceBookGatewayAccount)
        //                    {
        //                        string status = Encoding.UTF8.GetString(multiMimeMessage.InnerBody);

        //                        PresenceStatus oldStatus = fromContact.Status;
        //                        PresenceStatus newStatus = PresenceStatus.Unknown;

        //                        if (status.Contains("SignedIn"))
        //                            newStatus = PresenceStatus.Online;
        //                        else if (status.Contains("SignedOut"))
        //                            newStatus = PresenceStatus.Offline;

        //                        if (newStatus != PresenceStatus.Unknown)
        //                        {
        //                            fromContact.SetStatus(newStatus);

        //                            // The contact changed status
        //                            OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));

        //                            if (newStatus == PresenceStatus.Online)
        //                                OnContactOnline(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));
        //                            else
        //                                OnContactOffline(new ContactStatusChangedEventArgs(fromContact, oldStatus, newStatus));
        //                        }
        //                    }
        //                }
        //                break;
        //            #endregion
        //        }
        //    }
        //    else if (command == "DEL")
        //    {
        //        switch (multiMimeMessage.ContentHeaders[MIMEContentHeaders.ContentType].Value)
        //        {
        //            #region user xml
        //            case "application/user+xml":
        //                {
        //                    if (multiMimeMessage.InnerBody == null || multiMimeMessage.InnerBody.Length == 0)
        //                        return;  //No xml content.

        //                    if (multiMimeMessage.ContentHeaders[MIMEHeaderStrings.NotifType].Value == "Full")
        //                    {
        //                        //This is an initial NFY
        //                    }

        //                    XmlDocument xmlDoc = new XmlDocument();
        //                    xmlDoc.LoadXml(Encoding.UTF8.GetString(multiMimeMessage.InnerBody));

        //                    XmlNodeList services = xmlDoc.SelectNodes("//user/s");
        //                    XmlNodeList serviceEndPoints = xmlDoc.SelectNodes("//user/sep");

        //                    if (serviceEndPoints.Count > 0)
        //                    {
        //                        foreach (XmlNode serviceEndPoint in serviceEndPoints)
        //                        {
        //                            ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), serviceEndPoint.Attributes["n"].Value);
        //                            Guid epid = serviceEndPoint.Attributes["epid"] == null ? Guid.Empty : new Guid(serviceEndPoint.Attributes["epid"].Value);

        //                            switch (serviceEnum)
        //                            {
        //                                case ServiceShortNames.IM:
        //                                case ServiceShortNames.PD:
        //                                    {
        //                                        if (fromContact.EndPointData.ContainsKey(epid))
        //                                        {
        //                                            fromContact.SetChangedPlace(new PlaceChangedEventArgs(fromContact.EndPointData[epid], PlaceChangedReason.SignedOut));
        //                                        }

        //                                        if (fromIsMe && epid == MachineGuid)
        //                                        {
        //                                            SignoutFrom(epid);
        //                                        }

        //                                        break;
        //                                    }
        //                            }
        //                        }
        //                    }

        //                    if (services.Count > 0)
        //                    {
        //                        foreach (XmlNode service in services)
        //                        {
        //                            ServiceShortNames serviceEnum = (ServiceShortNames)Enum.Parse(typeof(ServiceShortNames), service.Attributes["n"].Value);

        //                            switch (serviceEnum)
        //                            {
        //                                case ServiceShortNames.IM:
        //                                    {
        //                                        PresenceStatus oldStatus = fromContact.Status;
        //                                        PresenceStatus newStatus = PresenceStatus.Offline;
        //                                        fromContact.SetStatus(newStatus);

        //                                        OnContactStatusChanged(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
        //                                        OnContactOffline(new ContactStatusChangedEventArgs(fromContact, viaHeaderContact, oldStatus, newStatus));
        //                                        break;
        //                                    }
        //                            }
        //                        }
        //                    }
        //                }
        //                break;

        //            #endregion

        //            #region circles xml

        //            case "application/circles+xml":
        //                {
        //                    Contact circle = null;
        //                    Contact group = null;

        //                    if (fromAccountAddressType == IMAddressInfoType.Circle)
        //                    {
        //                        circle = ContactList.GetCircle(fromAccount);

        //                        if (circle == null)
        //                        {
        //                            Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
        //                                "[OnNFYReceived] Cannot complete the operation since circle not found: " + multiMimeMessage.From.ToString());

        //                            return;
        //                        }
        //                    }
        //                    else if (fromAccountAddressType == IMAddressInfoType.TemporaryGroup)
        //                    {
        //                        group = GetMultiparty(fromAccount);

        //                        if (group == null)
        //                        {
        //                            Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
        //                                "[OnNFYReceived] temp group not found: " + multiMimeMessage.From.ToString());

        //                            return;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
        //                                "[OnNFYReceived] sender is not circle nor temp group: " + multiMimeMessage.From.ToString());

        //                        return;
        //                    }

        //                    if (multiMimeMessage.ContentHeaders.ContainsKey(MIMEHeaderStrings.Uri))
        //                    {
        //                        string xpathUri = multiMimeMessage.ContentHeaders[MIMEHeaderStrings.Uri].ToString();
        //                        if (xpathUri.Contains("/circle/roster(IM)/user"))
        //                        {
        //                            string typeAccount = xpathUri.Substring("/circle/roster(IM)/user".Length);
        //                            typeAccount = typeAccount.Substring(typeAccount.IndexOf("(") + 1);
        //                            typeAccount = typeAccount.Substring(0, typeAccount.IndexOf(")"));

        //                            string[] member = typeAccount.Split(':');
        //                            string memberAccount = member[1];
        //                            IMAddressInfoType memberNetwork = (IMAddressInfoType)int.Parse(member[0]);

        //                            Contact c = null;

        //                            if (circle != null)
        //                            {
        //                                if (!circle.ContactList.HasContact(memberAccount, memberNetwork))
        //                                    return;

        //                                c = circle.ContactList.GetContact(memberAccount, memberNetwork);
        //                                OnLeftGroupChat(new GroupChatParticipationEventArgs(c, circle));
        //                            }

        //                            if (group != null)
        //                            {
        //                                if (!group.ContactList.HasContact(memberAccount, memberNetwork))
        //                                    return;

        //                                c = group.ContactList.GetContact(memberAccount, memberNetwork);
        //                                group.ContactList.Remove(memberAccount, memberNetwork);

        //                                OnLeftGroupChat(new GroupChatParticipationEventArgs(c, group));
        //                            }
        //                        }
        //                        else
        //                        {
        //                            Contact goesOfflineGroup = null;
        //                            if (circle != null)
        //                                goesOfflineGroup = circle;
        //                            else if (group != null)
        //                                goesOfflineGroup = group;

        //                            // Group goes offline...
        //                            if (goesOfflineGroup != null)
        //                            {
        //                                PresenceStatus oldStatus = goesOfflineGroup.Status;
        //                                PresenceStatus newStatus = PresenceStatus.Offline;
        //                                goesOfflineGroup.SetStatus(newStatus);

        //                                // the contact changed status
        //                                OnContactStatusChanged(new ContactStatusChangedEventArgs(goesOfflineGroup, oldStatus, newStatus));

        //                                // the contact goes offline
        //                                OnContactOffline(new ContactStatusChangedEventArgs(goesOfflineGroup, oldStatus, newStatus));

        //                            }
        //                        }
        //                    }

        //                }
        //                break;
        //            #endregion

        //            #region network xml
        //            case "application/network+xml":
        //                {


        //                }
        //                break;
        //            #endregion
        //        }
        //    }
        //}

        //#endregion

        internal static RoutingInfo FromMultiMimeMessage(MultiMimeMessage multiMimeMessage, NSMessageHandler nsMessageHandler)
        {
            IMAddressInfoType senderAccountAddressType;
            string senderAccount = string.Empty;
            IMAddressInfoType senderGatewayAccountAddressType;
            string senderGatewayAccount = string.Empty;

            IMAddressInfoType receiverAccountAddressType;
            string receiverAccount = string.Empty;
            IMAddressInfoType receiverGatewayAccountAddressType;
            string receiverGatewayAccount = string.Empty;

            Contact messageGateway = null;

            if ((false == Contact.ParseFullAccount(multiMimeMessage.From.ToString(),
                out senderAccountAddressType, out senderAccount,
                out senderGatewayAccountAddressType, out senderGatewayAccount))
                ||
                (false == Contact.ParseFullAccount(multiMimeMessage.To.ToString(),
                out receiverAccountAddressType, out receiverAccount,
                out receiverGatewayAccountAddressType, out receiverGatewayAccount)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "Cannot parse sender or receiver from message: " + multiMimeMessage.From.ToString() + "|" + multiMimeMessage.To.ToString());

                return null;
            }

            Contact senderGateway = null;
            if (multiMimeMessage.From.HasAttribute("via"))
                senderGateway = GetGatewayFromAccountString(multiMimeMessage.From["via"], nsMessageHandler);

            if (multiMimeMessage.RoutingHeaders.ContainsKey(MIMERoutingHeaders.Via) && messageGateway == null) //The gateway is sender gateway
            {
                messageGateway = GetGatewayFromAccountString(multiMimeMessage.RoutingHeaders[MIMERoutingHeaders.Via], nsMessageHandler);
            }
            Contact sender = null;

            Contact receiverGateway = null;
            if (multiMimeMessage.To.HasAttribute("via"))
                receiverGateway = GetGatewayFromAccountString(multiMimeMessage.To["via"], nsMessageHandler);

            Contact receiver = null;

            bool fromMyself = false;
            bool sentToMe = false;

           

            if (sender == null)
            {
                fromMyself = (senderAccount == nsMessageHandler.Owner.Account && senderAccountAddressType == IMAddressInfoType.WindowsLive);

                if (!IsSpecialGatewayType(senderAccount, senderAccountAddressType))
                {
                    if (senderGateway == null)
                        sender = fromMyself ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
                    else
                    {
                        sender = senderGateway.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
                    }
                }
                else
                {
                    sender = GetGateway(senderAccount, senderAccountAddressType, nsMessageHandler);
                }
            }

            if (receiver == null)
            {
                sentToMe = (receiverAccount == nsMessageHandler.Owner.Account && receiverAccountAddressType == IMAddressInfoType.WindowsLive);

                if (!IsSpecialGatewayType(receiverAccount, receiverAccountAddressType))
                {
                    if (receiverGateway == null)
                        receiver = sentToMe ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(receiverAccount, receiverAccountAddressType);
                    else
                    {
                        receiver = receiverGateway.ContactList.GetContactWithCreate(receiverAccount, receiverAccountAddressType);
                    }
                }
                else
                {
                    receiver = GetGateway(receiverAccount, receiverAccountAddressType, nsMessageHandler);
                }
            }

            RoutingInfo routingInfo = new RoutingInfo(sender, senderGateway, receiver, receiverGateway, nsMessageHandler);
            routingInfo.SenderEndPointID = GetEPID(multiMimeMessage.From);
            routingInfo.ReceiverEndPointID = GetEPID(multiMimeMessage.To);

            routingInfo.MessageGateway = messageGateway;

            return routingInfo;
        }
    }
};
