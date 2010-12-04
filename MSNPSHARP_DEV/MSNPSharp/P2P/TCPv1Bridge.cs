using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.P2P
{
    using MSNPSharp.Core;

    public class DestinationAddressUpdatedEventHandler : EventArgs
    {
        private IPEndPoint ipEndPoint;

        public IPEndPoint IPEndPoint
        {
            get
            {
                return ipEndPoint;
            }
        }

        public DestinationAddressUpdatedEventHandler(IPEndPoint ipep)
        {
            this.ipEndPoint = ipep;
        }
    }

    public class TCPv1Bridge : P2PBridge
    {
        public event EventHandler<DestinationAddressUpdatedEventHandler> DestinationAddressUpdated;

        private NSMessageHandler nsMessageHandler = null;
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

        private Contact remote = null;
        public override Contact Remote
        {
            get
            {
                if (remote != null)
                    return remote;

                if (startupSession != null)
                    return startupSession.Remote;

                foreach (P2PSession s in SendQueues.Keys)
                {
                    return s.Remote;
                }

                return null;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (directConnection != null)
                    return directConnection.RemoteEndPoint as IPEndPoint;

                return null;
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (directConnection != null)
                    return directConnection.LocalEndPoint as IPEndPoint;

                return null;
            }
        }

        public TCPv1Bridge(
            ConnectivitySettings connectivitySettings,
            P2PVersion p2pVersion,
            Guid authNonce, bool isNeedHash,
            P2PSession p2pSession,
            NSMessageHandler ns,
            Contact remote)
            : base(0)
        {
            this.startupSession = p2pSession;
            this.nsMessageHandler = ns;
            this.remote = remote;

            directConnection = new P2PDirectProcessor(connectivitySettings, p2pVersion, authNonce, isNeedHash, p2pSession, nsMessageHandler);
            directConnection.HandshakeCompleted += new EventHandler<EventArgs>(directConnection_HandshakeCompleted);
            directConnection.P2PMessageReceived += new EventHandler<P2PMessageEventArgs>(directConnection_P2PMessageReceived);
            directConnection.SendCompleted += new EventHandler<MSNPSharp.Core.ObjectEventArgs>(directConnection_SendCompleted);

            directConnection.DirectNegotiationTimedOut += new EventHandler<EventArgs>(directConnection_DirectNegotiationTimedOut);
            directConnection.ConnectionClosed += new EventHandler<EventArgs>(directConnection_ConnectionClosed);
            directConnection.ConnectingException += new EventHandler<ExceptionEventArgs>(directConnection_ConnectingException);
            directConnection.ConnectionException += new EventHandler<ExceptionEventArgs>(directConnection_ConnectionException);
        }

        protected internal virtual void OnDestinationAddressUpdated(DestinationAddressUpdatedEventHandler e)
        {
            if (directConnection != null && directConnection.RemoteEndPoint != null)
            {
                IPEndPoint ipep = (IPEndPoint)directConnection.RemoteEndPoint;

                if (ipep.Address.Equals(e.IPEndPoint.Address) == false || ipep.Port != e.IPEndPoint.Port)
                {
                    directConnection.Disconnect();
                    OnBridgeClosed(EventArgs.Empty);
                }
            }

            if (DestinationAddressUpdated != null)
                DestinationAddressUpdated(this, e);
        }

        private void directConnection_HandshakeCompleted(object sender, EventArgs e)
        {
            OnBridgeOpened(e);
        }

        private void directConnection_DirectNegotiationTimedOut(object sender, EventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        private void directConnection_ConnectionException(object sender, ExceptionEventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);

        }

        private void directConnection_ConnectingException(object sender, ExceptionEventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);

        }

        private void directConnection_ConnectionClosed(object sender, EventArgs e)
        {
            OnBridgeClosed(EventArgs.Empty);
        }

        public void Listen(IPAddress address, int port)
        {
            directConnection.Listen(address, port);
        }

        public void Connect()
        {
            directConnection.Connect();
        }

        private void directConnection_P2PMessageReceived(object sender, P2PMessageEventArgs e)
        {
            remotePacketNo = e.P2PMessage.Header.Identifier;

            nsMessageHandler.P2PHandler.ProcessP2PMessage(this, Remote, Remote.SelectRandomEPID(), e.P2PMessage);
        }

        protected override void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            directConnection.SendMessage(msg, new P2PMessageSessionEventArgs(msg, session));
        }

        private void directConnection_SendCompleted(object sender, ObjectEventArgs e)
        {
            OnBridgeSent(e.Object as P2PMessageSessionEventArgs);
        }
    }
};
