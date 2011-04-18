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
        private string senderAccount = String.Empty;
        private Guid senderEndPointID = Guid.Empty;
        private IMAddressInfoType senderType = IMAddressInfoType.None;
        private Contact sender; // Default is null, don't set = null for performance reasons.
        private Contact senderGateway;

        private string receiverAccount = String.Empty;
        private Guid receiverEndPointID = Guid.Empty;
        private IMAddressInfoType receiverType = IMAddressInfoType.None;
        private Contact receiver;
        private Contact receiverGateway;

        private NSMessageHandler nsMessageHandler;

        public bool FromOwner
        {
            get
            {
                return Sender == this.NSMessageHandler.Owner;
            }
        }

        public bool ToOwner
        {
            get
            {
                return Receiver == this.NSMessageHandler.Owner;
            }
        }

        public string SenderAccount
        {
            get
            {
                return senderAccount;
            }
            private set
            {
                senderAccount = value;
            }
        }

        public IMAddressInfoType SenderType
        {
            get
            {
                return senderType;
            }
            private set
            {
                senderType = value;
            }
        }

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

        public string ReceiverAccount
        {
            get
            {
                return receiverAccount;
            }
            private set
            {
                receiverAccount = value;
            }
        }

        public IMAddressInfoType ReceiverType
        {
            get
            {
                return receiverType;
            }
            private set
            {
                receiverType = value;
            }
        }

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

            return GetGateway(gatewayAccount, gatewayAddressType, nsMessageHandler);
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
            return mimeAccountValue.HasAttribute("epid") ?
                new Guid(mimeAccountValue["epid"]) : Guid.Empty;
        }

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

            Contact sender = null;
            Contact senderGateway = null;

            Contact receiver = null;
            Contact receiverGateway = null;

            if (multiMimeMessage.From.HasAttribute("via"))
                senderGateway = GetGatewayFromAccountString(multiMimeMessage.From["via"], nsMessageHandler);

            if (senderGateway == null && multiMimeMessage.RoutingHeaders.ContainsKey(MIMERoutingHeaders.Via)) //The gateway is sender gateway
                senderGateway = GetGatewayFromAccountString(multiMimeMessage.RoutingHeaders[MIMERoutingHeaders.Via], nsMessageHandler);

            if (multiMimeMessage.To.HasAttribute("via"))
                receiverGateway = GetGatewayFromAccountString(multiMimeMessage.To["via"], nsMessageHandler);

            bool fromMyself = (senderAccount == nsMessageHandler.Owner.Account && senderAccountAddressType == IMAddressInfoType.WindowsLive);

            if (!Contact.IsSpecialGatewayType(senderAccountAddressType))
            {
                if (senderGateway == null)
                    sender = fromMyself ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
                else
                {
                    // For facebook, we might need to use GetContact instead of GetContactWithCreate, 
                    // that's the official client's behavior. Actually we will get all status notification from
                    // our facebook contacts, however, WLM only display those guys who are also our windows live
                    // contact. For now, those facebook contact doesn't add us as an WLM contact will also show up
                    // in MSNPSharp, their name is the same with the account - all are numbers. I think it doesn't
                    // harm so far, so keep it like this is reasonable, but might change in the future.
                    sender = senderGateway.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
                }
            }
            else
            {
                sender = GetGateway(senderAccount, senderAccountAddressType, nsMessageHandler);
            }

            bool sentToMe = (receiverAccount == nsMessageHandler.Owner.Account && receiverAccountAddressType == IMAddressInfoType.WindowsLive);

            if (!Contact.IsSpecialGatewayType(receiverAccountAddressType))
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

            RoutingInfo routingInfo = new RoutingInfo(sender, senderGateway, receiver, receiverGateway, nsMessageHandler);
            routingInfo.SenderEndPointID = GetEPID(multiMimeMessage.From);
            routingInfo.SenderAccount = senderAccount;
            routingInfo.SenderType = senderAccountAddressType;

            routingInfo.ReceiverEndPointID = GetEPID(multiMimeMessage.To);
            routingInfo.ReceiverAccount = receiverAccount;
            routingInfo.ReceiverType = receiverAccountAddressType;

            return routingInfo;
        }
    }
};
