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
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    using MSNPSharp.Core;

    public class SDGBridge : P2PBridge
    {
        private NSMessageHandler nsHandler;

        public override bool IsOpen
        {
            get
            {
                return ((nsHandler != null) && nsHandler.IsSignedIn);
            }
        }

        public override int MaxDataSize
        {
            get
            {
                return 1000;
            }
        }

        public override bool Synced
        {
            get
            {
                return true;
            }
        }

        protected internal override uint SyncId
        {
            get
            {
                return uint.MaxValue;
            }
            set
            {
            }
        }

        public override bool SuitableFor(P2PSession session)
        {
            return true;
        }

        [Obsolete("This is not valid for SDGBridge. See SuitableFor() method.", true)]
        public override Contact Remote
        {
            get
            {
                throw new InvalidOperationException("This is not valid for SDGBridge. See SuitableFor() method.");
            }
        }

        public SDGBridge(NSMessageHandler nsHandler)
            : base(0)
        {
            this.nsHandler = nsHandler;
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            string target = (msg.Version == P2PVersion.P2PV1) ?
                remote.Account.ToLowerInvariant()
                :
                remote.Account.ToLowerInvariant() + ";" + remoteGuid.ToString("B");

            NSMessageProcessor nsmp = (NSMessageProcessor)nsHandler.MessageProcessor;
            int transId = nsmp.IncreaseTransactionID();


            string to = ((int)remote.ClientType).ToString() + ":" + remote.Account;
            string from = ((int)nsHandler.Owner.ClientType).ToString() + ":" + nsHandler.Owner.Account;

            MultiMimeMessage mmMessage = new MultiMimeMessage(to, from);
            mmMessage.RoutingHeaders[MIMERoutingHeaders.From][MIMERoutingHeaders.EPID] = NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();
            mmMessage.RoutingHeaders[MIMERoutingHeaders.ServiceChannel] = "PE";
            mmMessage.RoutingHeaders[MIMERoutingHeaders.Options] = "0";

            mmMessage.ContentKeyVersion = "2.0";
            mmMessage.ContentHeaders[MIMEContentHeaders.MessageType] = "Data";
            mmMessage.ContentHeaders[MIMEHeaderStrings.Content_Type] = "application/x-msnmsgrp2p";
            mmMessage.ContentHeaders[MIMEHeaderStrings.Content_Transfer_Encoding] = "binary";
            mmMessage.ContentHeaders["Bridging-Offsets"] = "0";

            mmMessage.InnerBody = msg.GetBytes();

            NSMessage sdgPayload = new NSMessage("SDG");
            sdgPayload.TransactionID = transId;
            sdgPayload.InnerMessage = mmMessage;
            nsmp.SendMessage(sdgPayload, sdgPayload.TransactionID);
        }
    }
};
