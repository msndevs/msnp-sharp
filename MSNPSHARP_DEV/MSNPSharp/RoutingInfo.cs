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
            get { return senderEndPointID; }
            private set { senderEndPointID = value; }
        }

        private Guid receiverEndPointID = Guid.Empty;

        public Guid ReceiverEndPointID
        {
            get { return receiverEndPointID; }
            private set { receiverEndPointID = value; }
        }


		private Contact sender = null;
		
		public Contact Sender
		{
			get{ return sender;}
			private set{ sender = value; }
		}
		
		
		private Contact senderGateway = null;
		
		public Contact SenderGateway
		{
			get{ return senderGateway;}
			private set{ senderGateway = value; }
		}
		
		
		private Contact receiver = null;
		
		public Contact Receiver
		{
			get{ return receiver;}
			private set{ receiver = value; }
		}
		
		private Contact receiverGateway = null;
		
		public Contact ReceiverGateway
		{
			get{ return receiverGateway;}
			private set{ receiverGateway = value; }
		}
		
		public bool FromOwner
		{
			get{ return Sender == this.NSMessageHandler.Owner; }
		}
		
		private NSMessageHandler nsMessageHandler = null;
		protected NSMessageHandler NSMessageHandler
		{
			get{ return nsMessageHandler; }
			set{ nsMessageHandler = value; }
		}
		
		protected RoutingInfo(Contact sender,  Contact senderGateway, Contact receiver, Contact receiverGateway, NSMessageHandler nsMessageHandler)
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

            Contact senderGateway = null;
            if (multiMimeMessage.From.HasAttribute("via"))
                senderGateway = GetGatewayFromAccountString(multiMimeMessage.From["via"], nsMessageHandler);

            if (multiMimeMessage.RoutingHeaders.ContainsKey(MIMERoutingHeaders.Via) && senderGateway == null) //The gateway is sender gateway
            {
                senderGateway = GetGatewayFromAccountString(multiMimeMessage.RoutingHeaders[MIMERoutingHeaders.Via], nsMessageHandler);
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
                sender = fromMyself ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
            }

            if (receiver == null)
            {
                sentToMe = (receiverAccount == nsMessageHandler.Owner.Account && receiverAccountAddressType == IMAddressInfoType.WindowsLive);
                receiver = sentToMe ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(receiverAccount, receiverAccountAddressType);
            }
			
			RoutingInfo routingInfo = new RoutingInfo(sender, senderGateway, receiver, receiverGateway, nsMessageHandler);
            routingInfo.SenderEndPointID = GetEPID(multiMimeMessage.From);
            routingInfo.ReceiverEndPointID = GetEPID(multiMimeMessage.To);


            return routingInfo;
		}
	}
}

