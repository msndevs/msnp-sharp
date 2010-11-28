#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
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

    public class SBBridge : P2PBridge
    {
#if DEBUG
        private const int sbQueueSize = 2; // Step by step :)
#else
        private const int sbQueueSize = 9;
#endif
        private Conversation sbHandler = null;

        private Dictionary<int, P2PMessageSessionEventArgs> p2pAckMessages = new Dictionary<int, P2PMessageSessionEventArgs>();

        public override bool IsOpen
        {
            get
            {
                return (
                    (sbHandler != null) &&
                    (sbHandler.Switchboard != null) &&
                    (sbHandler.Switchboard.GetRosterUniqueUserCount() > 0) &&
                    (sbHandler.Switchboard.IsSessionEstablished));
            }
        }

        public override int MaxDataSize
        {
            get
            {
                return 1202;
            }
        }

        public override Contact Remote
        {
            get
            {
                if (sbHandler != null)
                {
                    foreach (Contact c in sbHandler.Contacts)
                    {
                        if (c.NSMessageHandler.ContactList.Owner != c)
                            return c;
                    }

                    foreach (Contact c in sbHandler.PendingInviteContacts)
                    {
                        if (c.NSMessageHandler.ContactList.Owner != c)
                            return c;
                    }
                }

                foreach (P2PSession s in SendQueues.Keys)
                {
                    return s.Remote;
                }

                return null;
            }
        }

        public Conversation Switchboard
        {
            get
            {
                return sbHandler;
            }
        }

        public SBBridge(P2PSession p2pSess)
            : base(sbQueueSize)
        {
            SendQueues.Add(p2pSess, new P2PSendQueue());
        }

        public SBBridge(Conversation sb)
            : base(sbQueueSize)
        {
            this.sbHandler = sb;

            AttachSBEvents();
        }

        private void AttachSBEvents()
        {
            sbHandler.P2PMessageReceived += (sb_P2PMessageReceived);
            sbHandler.MessageAcknowledgementReceived += (sb_MessageAcknowledgementReceived);

            sbHandler.ContactJoined += (sb_ContactJoined);
            sbHandler.ContactLeft += (sb_ContactLeft);
            sbHandler.AllContactsLeft += (sb_AllContactsLeft);
            sbHandler.ServerErrorReceived += (sb_ServerErrorReceived);
        }

        private void DetachSBEvents()
        {
            sbHandler.P2PMessageReceived -= (sb_P2PMessageReceived);
            sbHandler.MessageAcknowledgementReceived -= (sb_MessageAcknowledgementReceived);

            sbHandler.ContactJoined -= (sb_ContactJoined);
            sbHandler.ContactLeft -= (sb_ContactLeft);
            sbHandler.AllContactsLeft -= (sb_AllContactsLeft);
            sbHandler.ServerErrorReceived -= (sb_ServerErrorReceived);
        }

        private void ClearSwitchboard()
        {
            if (sbHandler == null)
                return;

            DetachSBEvents();

            sbHandler = null;
            p2pAckMessages.Clear();

            OnBridgeClosed(EventArgs.Empty);
        }

        void sb_AllContactsLeft(object sender, EventArgs e)
        {
            ClearSwitchboard();
        }

        void sb_ServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            ClearSwitchboard();
        }

        void sb_ContactLeft(object sender, ContactConversationEventArgs e)
        {
            ClearSwitchboard();
        }

        void sb_ContactJoined(object sender, ContactConversationEventArgs e)
        {
            if (sbHandler.Contacts.Count > 1)
            {
                ClearSwitchboard();
            }
            else
            {
                OnBridgeOpened(EventArgs.Empty);
            }
        }

        void sb_P2PMessageReceived(object sender, P2PMessageEventArgs e)
        {
            sbHandler.Messenger.Nameserver.P2PHandler.ProcessP2PMessage(this, Remote, Remote.SelectRandomEPID(), e.P2PMessage);
        }

        public override void Send(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg, AckHandler ackHandler)
        {
            if (sbHandler == null)
            {
                sbHandler = session.NSMessageHandler.__internalMessenger.CreateConversation();

                AttachSBEvents();

                sbHandler.Invite(remote);
            }

            base.Send(session, remote, remoteGuid, msg, ackHandler);
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            string target = remote.Mail.ToLowerInvariant();
            string source = remote.NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant();

            if (msg.Version == P2PVersion.P2PV2)
            {
                target += ";" + remoteGuid.ToString("B");
                source += ";" + NSMessageHandler.MachineGuid.ToString("B");
            }

            int ackId = ((SBMessageProcessor)sbHandler.Switchboard.MessageProcessor).IncreaseTransactionID();

            MimeMessage mimeMessage = new P2PMimeMessage(target, source, msg);
            SBMessage sbMessage = new SBMessage();
            sbMessage.TransactionID = ackId;
            sbMessage.Acknowledgement = "D";
            sbMessage.InnerMessage = mimeMessage;

            p2pAckMessages[ackId] = new P2PMessageSessionEventArgs(msg, session);

            sbHandler.Switchboard.MessageProcessor.SendMessage(sbMessage);
        }

        void sb_MessageAcknowledgementReceived(object sender, SBMessageDeliverResultEventArgs e)
        {
            if (p2pAckMessages.ContainsKey(e.MessageID))
            {
                P2PMessageSessionEventArgs p2pe = p2pAckMessages[e.MessageID];
                p2pAckMessages.Remove(e.MessageID);

                if (e.Success)
                {
                    p2pAckMessages.Remove(e.MessageID);
                    OnBridgeSent(p2pe);
                }
                else
                {
                    p2pAckMessages.Remove(e.MessageID);

                    // Try Again???
                    // sbHandler.Switchboard.MessageProcessor.SendMessage(p2pAckMessages[ackId].P2PMessage);
                }
            }
        }
    }
};
