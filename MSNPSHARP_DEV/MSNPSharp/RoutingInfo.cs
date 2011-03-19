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
		/*
		private string senderAccount = string.Empty;
		
		public string SenderAccount
		{
			get{ return senderAccount;}
			private set{ senderAccount = value; }
		}
		*/
		

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
		
		/*
		private string senderGatewayAccount = string.Empty;
		
		public string SenderGatewayAccount
		{
			get{ return senderGatewayAccount;}
			private set{ senderGatewayAccount = value; }
		}
		*/
		
		/*
		private string receiverAccount = string.Empty;
		
		public string ReceiverAccount
		{
			get{ return receiverAccount;}
			private set{ receiverAccount = value; }
		}
		
		private string receiverGatewayAccount = string.Empty;
		
		public string ReceiverGatewayAccount
		{
			get{ return receiverGatewayAccount;}
			private set{ receiverGatewayAccount = value; }
		}
		*/
		
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
            Contact sender = null;
			
			Contact receiverGateway = null;
			Contact receiver = null;
			
            bool fromMyself = false;

            if (multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via) ||
                senderGatewayAccountAddressType != IMAddressInfoType.None ||
                receiverGatewayAccountAddressType != IMAddressInfoType.None)
            {
                string gatewayAccountString = multiMimeMessage.RoutingHeaders.ContainsKey(MIMEHeaderStrings.Via)
                    ? multiMimeMessage.RoutingHeaders[MIMEHeaderStrings.Via].Value
                    :
                    (senderGatewayAccountAddressType != IMAddressInfoType.None ?
                    (((int)senderGatewayAccountAddressType).ToString() + ":" + senderGatewayAccount)
                    :
                    (((int)receiverGatewayAccountAddressType).ToString() + ":" + receiverGatewayAccount));

                IMAddressInfoType gatewayAddressType;
                string gatewayAccount;
                IMAddressInfoType ignoreAddressType;
                string ignoreAccount;

                Contact.ParseFullAccount(gatewayAccountString,
                    out gatewayAddressType, out gatewayAccount,
                    out ignoreAddressType, out ignoreAccount);

                if (gatewayAddressType == IMAddressInfoType.Circle)
                {
                    senderGateway = nsMessageHandler.ContactList.GetCircle(gatewayAccount);
                    if (senderGateway != null)
                    {
                        sender = senderGateway.ContactList.GetContact(senderAccount, senderAccountAddressType);
                    }
                }
                else if (gatewayAddressType == IMAddressInfoType.TemporaryGroup)
                {
                    senderGateway = nsMessageHandler.GetMultiparty(gatewayAccount);
                    if (senderGateway != null)
                    {
                        sender = senderGateway.ContactList.GetContact(senderAccount, senderAccountAddressType);
                    }
                }
                else
                {
                    senderGateway = nsMessageHandler.ContactList.GetContact(gatewayAccount, gatewayAddressType);
                    if (senderGateway != null)
                    {
                        sender = senderGateway.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
                    }
                }
            }
			
			if (sender == null)
            {
                fromMyself = (senderAccount == nsMessageHandler.Owner.Account && senderAccountAddressType == IMAddressInfoType.WindowsLive);
                sender = fromMyself ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContactWithCreate(senderAccount, senderAccountAddressType);
            }
			
			return new RoutingInfo(sender, senderGateway, receiver, receiverGateway, nsMessageHandler);
		}
	}
}

