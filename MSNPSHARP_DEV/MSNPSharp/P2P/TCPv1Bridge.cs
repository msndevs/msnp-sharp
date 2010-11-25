using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.P2P
{
    public class TCPv1Bridge : P2PBridge
    {
        private P2PSession startupSession = null;
        private P2PDirectProcessor directConnection = null;

        public override bool IsOpen
        {
            get
            {
                return (directConnection != null && 
                    directConnection.DCState == DirectConnectionState.Established);
            }
        }

        public override int MaxDataSize
        {
            get
            {
                return 1352;
            }
        }

        public override Contact Remote
        {
            get
            {
                if (startupSession != null)
                {
                    return startupSession.Remote;
                }

                foreach (P2PSession s in SendQueues.Keys)
                {
                    return s.Remote;
                }

                return null;
            }
        }

        public TCPv1Bridge(
            ConnectivitySettings connectivitySettings,
            P2PVersion p2pVersion,
            Guid authNonce, bool isNeedHash,
            P2PSession p2pSession)
            : base(0)
        {
            startupSession = p2pSession;

            directConnection = new P2PDirectProcessor(connectivitySettings, p2pVersion, authNonce, isNeedHash, p2pSession);
            directConnection.HandshakeCompleted += new EventHandler<EventArgs>(directConnection_HandshakeCompleted);
            directConnection.P2PMessageReceived += new EventHandler<P2PMessageEventArgs>(directConnection_P2PMessageReceived);
        }

        public void Listen(IPAddress address, int port)
        {
            directConnection.Listen(address, port);
        }

        public void Connect()
        {
            directConnection.Connect();
        }

        private void directConnection_HandshakeCompleted(object sender, EventArgs e)
        {
            OnBridgeOpened(e);
        }

        private void directConnection_P2PMessageReceived(object sender, P2PMessageEventArgs e)
        {
            startupSession.NSMessageHandler.P2PHandler.ProcessP2PMessage(
                this, Remote, Remote.SelectRandomEPID(), e.P2PMessage);
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            directConnection.SendMessage(msg);
            try
            {
                OnBridgeSent(new P2PMessageSessionEventArgs(msg, session));
            }
            catch
            {
            }
        }
    }
};
