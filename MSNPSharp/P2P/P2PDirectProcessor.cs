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
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using Org.Mentalis.Network.ProxySocket;

namespace MSNPSharp.P2P
{
    using MSNPSharp.Core;

    public enum DirectConnectionState
    {
        None = 0,
        Closed = 0,
        Foo = 1,
        Handshake = 2,
        HandshakeReply = 3,
        Established = 5
    }

    /// <summary>
    /// Handles the direct connections in P2P sessions.
    /// </summary>
    public class P2PDirectProcessor : IDisposable
    {
        #region Events

        public event EventHandler<EventArgs> DirectNegotiationTimedOut;
        public event EventHandler<EventArgs> HandshakeCompleted;
        public event EventHandler<P2PMessageEventArgs> P2PMessageReceived;

        public event EventHandler<EventArgs> ConnectionEstablished;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ExceptionEventArgs> ConnectingException;
        public event EventHandler<ExceptionEventArgs> ConnectionException;
        public event EventHandler<ObjectEventArgs> SendCompleted;

        #region Event triggers

        protected virtual void OnDirectNegotiationTimedOut(EventArgs e)
        {
            if (DirectNegotiationTimedOut != null)
                DirectNegotiationTimedOut(this, e);
        }

        protected virtual void OnHandshakeCompleted(EventArgs e)
        {
            // Disable timer.
            socketExpireTimer.Enabled = false;

            if (HandshakeCompleted != null)
                HandshakeCompleted(this, e);
        }

        protected virtual void OnP2PMessageReceived(P2PMessageEventArgs e)
        {
            if (P2PMessageReceived != null)
                P2PMessageReceived(this, e);
        }

        protected virtual void OnConnectionEstablished(object sender, EventArgs e)
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(sender, e);

            this.OnConnected();
        }

        protected virtual void OnConnectionClosed(object sender, EventArgs e)
        {
            if (ConnectionClosed != null)
                ConnectionClosed(sender, e);

            DCState = DirectConnectionState.Closed;
        }

        protected virtual void OnConnectingException(object sender, ExceptionEventArgs e)
        {
            if (ConnectingException != null)
                ConnectingException(sender, e);
        }

        protected virtual void OnConnectionException(object sender, ExceptionEventArgs e)
        {
            if (ConnectionException != null)
                ConnectionException(sender, e);
        }

        protected virtual void OnSendCompleted(object sender, ObjectEventArgs e)
        {
            if (SendCompleted != null)
                SendCompleted(sender, e);
        }

        #endregion

        #endregion

        #region Members
        private P2PVersion version = P2PVersion.P2PV1;
        private Guid nonce = Guid.Empty;
        private bool needHash = false;
        private Timer socketExpireTimer = new Timer(15000);
        private ProxySocket socketListener = null;
        private bool isListener = false;
        private Socket dcSocket = null;
        private P2PSession startupSession = null;
        private NSMessageHandler nsMessageHandler = null;
        private DirectConnectionState dcState = DirectConnectionState.Closed;
        #endregion

        #region Properties

        public P2PVersion Version
        {
            get
            {
                return version;
            }
        }

        public DirectConnectionState DCState
        {
            get
            {
                return dcState;
            }
            protected internal set
            {
                if (dcState != value)
                {
                    dcState = value;

                    if (value == DirectConnectionState.Established)
                        OnHandshakeCompleted(EventArgs.Empty);
                }
            }
        }

        public Guid Nonce
        {
            get
            {
                return nonce;
            }
        }

        // Only transport over TCP is supported; no HTTP poll support
        private TcpSocketMessageProcessor processor;
        private TcpSocketMessageProcessor Processor
        {
            get
            {
                return processor;
            }
            set
            {
                if (processor != null)
                {
                    processor.ConnectionEstablished -= OnConnectionEstablished;
                    processor.ConnectionClosed -= OnConnectionClosed;
                    processor.ConnectingException -= OnConnectingException;
                    processor.ConnectionException -= OnConnectionException;
                    processor.SendCompleted -= OnSendCompleted;
                    processor.MessageReceived -= OnMessageReceived;
                }

                processor = value;

                if (processor != null)
                {
                    processor.ConnectionEstablished += OnConnectionEstablished;
                    processor.ConnectionClosed += OnConnectionClosed;
                    processor.ConnectingException += OnConnectingException;
                    processor.ConnectionException += OnConnectionException;
                    processor.SendCompleted += OnSendCompleted;
                    processor.MessageReceived += OnMessageReceived;
                }
            }
        }

        public bool Connected
        {
            get
            {
                return Processor != null && Processor.Connected;
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return Processor.LocalEndPoint;
            }
        }

        /// <summary>
        /// Returns whether this processor was initiated as listening (true) or connecting (false).
        /// </summary>
        public bool IsListener
        {
            get
            {
                return isListener;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                if (dcSocket != null)
                    return dcSocket.RemoteEndPoint;

                return Processor.RemoteEndPoint;
            }
        }

        #endregion

        Guid reply = Guid.Empty;
        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PDirectProcessor(
            ConnectivitySettings connectivitySettings,
            P2PVersion p2pVersion,
            Guid reply, Guid authNonce, bool isNeedHash,
            P2PSession p2pMessageSession,
            NSMessageHandler nsMessageHandler)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object - " + p2pVersion, GetType().Name);

            Processor = new TcpSocketMessageProcessor(connectivitySettings, new P2PDCPool());

            this.version = p2pVersion;
            this.nonce = authNonce;
            this.reply = reply;
            this.needHash = isNeedHash;
            this.startupSession = p2pMessageSession;
            this.nsMessageHandler = nsMessageHandler;
        }

        ~P2PDirectProcessor()
        {
            Dispose(false);
        }


        /// <summary>
        /// Starts listening at the specified port in the connectivity settings.
        /// </summary>
        public void Listen(IPAddress address, int port)
        {
            ProxySocket socket = Processor.GetPreparedSocket(address, port);

            // Begin waiting for the incoming connection
            socket.Listen(1);

            // set this value so we know whether to send a handshake message or not later in the process
            isListener = true;
            socketListener = socket;

            SetupTimer();
            socket.BeginAccept(new AsyncCallback(EndAcceptCallback), socket);
        }

        public void Connect()
        {
            SetupTimer();
            Processor.Connect();
        }

        private void SetupTimer()
        {
            socketExpireTimer.Elapsed += new ElapsedEventHandler(socketExpireTimer_Elapsed);
            socketExpireTimer.AutoReset = false;
            socketExpireTimer.Enabled = true; // After handshake completed, DISABLE timer.
        }

        private void socketExpireTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            socketExpireTimer.Elapsed -= socketExpireTimer_Elapsed;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                "No handshake made in " + (socketExpireTimer.Interval / 1000) + " seconds, disposing...", GetType().Name);

            OnDirectNegotiationTimedOut(EventArgs.Empty);

            Dispose();
        }

        private void StopListening()
        {
            if (socketListener != null)
            {
                try
                {
                    socketListener.Close();
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, GetType().ToString() + " Error: " + ex.Message);
                }

                socketListener = null;
            }
        }

        /// <summary>
        /// Called when an incoming connection has been accepted.
        /// </summary>
        /// <param name="ar"></param>
        protected virtual void EndAcceptCallback(IAsyncResult ar)
        {
            ProxySocket listenSocket = (ProxySocket)ar.AsyncState;
            try
            {
                dcSocket = listenSocket.EndAccept(ar);

                DCState = DirectConnectionState.Foo;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "I have listened on " + dcSocket.LocalEndPoint + " and setup a DC with " + dcSocket.RemoteEndPoint, GetType().Name);

                // Stop listening
                StopListening();

                // Begin accepting messages
                Processor.BeginDataReceive(dcSocket);

                OnConnected();
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, GetType().ToString() + " Error: " + ex.Message);
            }
        }


        /// <summary>
        /// Closes the socket connection.
        /// </summary>
        public void Disconnect()
        {
            Processor.Disconnect();

            StopListening();

            // clean up the socket properly
            if (dcSocket != null)
            {
                try
                {
                    dcSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception dcex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, dcex.Message);
                }
                finally
                {
                    dcSocket.Close();
                }

                dcSocket = null;
            }
        }

        protected void OnConnected()
        {
            if (!IsListener && DCState == DirectConnectionState.Closed)
            {
                // Send foo
                DCState = DirectConnectionState.Foo;
                Processor.Send(new byte[] { 0x04, 0x00, 0x00, 0x00, 0x66, 0x6f, 0x6f, 0x00 });
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "foo0 sent", GetType().Name);

                // Send NONCE
                DCState = DirectConnectionState.Handshake;
                P2PDCHandshakeMessage hm = new P2PDCHandshakeMessage(Version);
                hm.Guid = reply;

                if (Version == P2PVersion.P2PV1)
                {
                    // AckSessionId is set by NONCE
                    startupSession.IncreaseLocalIdentifier();
                    hm.Header.Identifier = startupSession.LocalIdentifier;
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Sending handshake message:\r\n " +
                    hm.ToDebugString(), GetType().Name);

                Processor.Send(hm.GetBytes());
                DCState = DirectConnectionState.HandshakeReply;
            }
        }

        private P2PDCHandshakeMessage VerifyHandshake(byte[] data)
        {
            P2PVersion authVersion = P2PVersion.P2PV1;
            P2PDCHandshakeMessage ret = null;

            if (data.Length == 48)
            {
                authVersion = P2PVersion.P2PV1;
            }
            else if (data.Length == 16)
            {
                authVersion = P2PVersion.P2PV2;
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    "Invalid handshake length, the data was: " + Encoding.ASCII.GetString(data), GetType().Name);

                return null;
            }

            if (authVersion != this.version)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received version is {0}, expected {1}", authVersion, this.version), GetType().Name);

                return null;
            }

            P2PDCHandshakeMessage incomingHandshake = new P2PDCHandshakeMessage(version);
            incomingHandshake.ParseBytes(data);

            Guid incomingGuid = incomingHandshake.Guid;

            if (incomingHandshake.Version == P2PVersion.P2PV1 && (P2PFlag.DirectHandshake != (incomingHandshake.V1Header.Flags & P2PFlag.DirectHandshake)))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                   "Handshake flag not set for v1, the flag was: " + incomingHandshake.V1Header.Flags, GetType().Name);

                return null;
            }

            Guid compareGuid = incomingGuid;
            if (needHash)
            {
                compareGuid = HashedNonceGenerator.HashNonce(compareGuid);
            }

            if (this.nonce == compareGuid)
            {
                ret = new P2PDCHandshakeMessage(version);
                ret.ParseBytes(data); // copy identifiers
                ret.Guid = compareGuid; // set new guid (hashed)
                ret.Header.Identifier = 0; // our id
                return ret; // OK this is our handshake message
            }

            return null;

        }

        /// <summary>
        /// Discards the foo message and sends the message to all handlers as a P2PDCMessage object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnMessageReceived(object sender, ByteEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "Analyzing message in DC state <" + dcState + ">", GetType().Name);

            byte[] data = e.Bytes;

            switch (dcState)
            {
                case DirectConnectionState.Established:
                    {
                        // Convert to a p2pdc message
                        P2PDCMessage dcMessage = new P2PDCMessage(version);
                        dcMessage.ParseBytes(data);

                        OnP2PMessageReceived(new P2PMessageEventArgs(dcMessage));
                    }
                    break;

                case DirectConnectionState.HandshakeReply:
                    {
                        P2PDCHandshakeMessage match = VerifyHandshake(data);

                        if (match == null)
                        {
                            Dispose();
                            return;
                        }

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            "Nonce accepted: " + match.Guid + "; My Nonce: " + this.nonce + "; Need Hash: " + needHash, GetType().Name);

                        DCState = DirectConnectionState.Established;
                    }
                    break;

                case DirectConnectionState.Handshake:
                    {
                        P2PDCHandshakeMessage match = VerifyHandshake(data);

                        if (match == null)
                        {
                            Dispose();
                            return;
                        }

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            "Nonce MATCH: " + match.Guid, GetType().Name);

                        match.Guid = reply;

                        if (version == P2PVersion.P2PV1)
                        {
                            startupSession.IncreaseLocalIdentifier();
                            match.Header.Identifier = startupSession.LocalIdentifier;
                        }

                        // Send Nonce Reply
                        SendMessage(match);

                        DCState = DirectConnectionState.Established;

                    }
                    break;

                case DirectConnectionState.Foo:
                    {
                        string initialData = Encoding.ASCII.GetString(data);

                        if (data.Length == 4 && initialData == "foo\0")
                        {
                            DCState = DirectConnectionState.Handshake;
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "foo0 handled", GetType().Name);
                        }
                        else
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "foo0 expected, but it was: " + initialData, GetType().Name);
                            Dispose();
                            return;
                        }
                    }
                    break;

                case DirectConnectionState.Closed:
                    break;
            }

        }


        /// <summary>
        /// Sends the P2PMessage directly over the socket. Accepts P2PDCMessage and P2PMessage objects.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetworkMessage message)
        {
            // if it is a regular message convert it
            P2PDCMessage p2pMessage = message as P2PDCMessage;
            if (p2pMessage == null)
            {
                p2pMessage = new P2PDCMessage(message as P2PMessage);
            }

            SendMessage(p2pMessage, null);
        }

        public virtual void SendMessage(P2PMessage msg, P2PMessageSessionEventArgs se)
        {
            // if it is a regular message convert it
            P2PDCMessage p2pMessage = msg as P2PDCMessage;
            if (p2pMessage == null)
            {
                p2pMessage = new P2PDCMessage(msg);
            }

            // prepare the message
            p2pMessage.PrepareMessage();

            // this is very bloated!
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + p2pMessage.ToDebugString(), GetType().Name);

            if (dcSocket != null)
                Processor.SendSocketData(dcSocket, p2pMessage.GetBytes(), se);
            else
                Processor.Send(p2pMessage.GetBytes(), se);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
};
