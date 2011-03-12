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

    public class UUNBridge : P2PBridge
    {
        private NSMessageHandler nsHandler;
        private Dictionary<int, P2PMessageSessionEventArgs> p2pAckMessages = new Dictionary<int, P2PMessageSessionEventArgs>();

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
                return 7500;
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
            if (session != null)
            {
                ClientCapabilities caps = session.Remote.EndPointData[session.RemoteContactEndPointID].PECapabilities;
                return (ClientCapabilities.SupportsDirectBootstrapping == (caps & ClientCapabilities.SupportsDirectBootstrapping));
            }
            return false;

            // Don't call base.SuitableFor(). Base call checks Remote and generates InvalidOperationException();
            // See below. 
        }

        [Obsolete("This is not valid for UUNBridge. See SuitableFor() method.", true)]
        public override Contact Remote
        {
            get
            {
                throw new InvalidOperationException("This is not valid for UUNBridge. See SuitableFor() method.");
            }
        }

        public UUNBridge(NSMessageHandler nsHandler)
            : base(0)
        {
            this.nsHandler = nsHandler;
        }

        protected internal void ProcessUUN(int transId, bool ok)
        {
            if (p2pAckMessages.ContainsKey(transId))
            {
                P2PMessageSessionEventArgs p2pe = p2pAckMessages[transId];
                p2pAckMessages.Remove(transId);

                OnBridgeSent(p2pe);
            }
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            SLPMessage slp = msg.InnerMessage as SLPMessage;
            if (slp != null)
            {
                string target = (msg.Version == P2PVersion.P2PV1) ?
                    remote.Account.ToLowerInvariant()
                    :
                    remote.Account.ToLowerInvariant() + ";" + remoteGuid.ToString("B");

                NSMessageProcessor nsmp = (NSMessageProcessor)nsHandler.MessageProcessor;
                int transId = nsmp.IncreaseTransactionID();

                NSPayLoadMessage uunCommand = new NSPayLoadMessage(
                    "UUN",
                    new string[] { target, "3" },
                    Encoding.UTF8.GetString(slp.GetBytes(false)));

                uunCommand.TransactionID = transId;

                p2pAckMessages[transId] = new P2PMessageSessionEventArgs(msg, session);

                nsmp.SendMessage(uunCommand, uunCommand.TransactionID);
            }
        }
    }
};
