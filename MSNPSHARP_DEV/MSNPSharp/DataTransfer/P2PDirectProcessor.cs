#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using Org.Mentalis.Network.ProxySocket;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp.Core;


    [Serializable]
    public class P2PHandshakeMessageEventArgs : EventArgs
    {
        private P2PDCHandshakeMessage handshakeMessage = null;

        public P2PDCHandshakeMessage HandshakeMessage
        {
            get
            {
                return handshakeMessage;
            }
        }

        public P2PHandshakeMessageEventArgs(P2PDCHandshakeMessage handshakeMessage)
        {
            this.handshakeMessage = handshakeMessage;
        }
    }

    /// <summary>
    /// Handles the direct connections in P2P sessions.
    /// </summary>
    public class P2PDirectProcessor : SocketMessageProcessor, IDisposable
    {
        public event EventHandler<P2PHandshakeMessageEventArgs> HandshakeCompleted;


        private P2PVersion version = P2PVersion.P2PV1;
        private Guid authNonce = Guid.Empty;
        private bool isAuthNonceHashed = false;
        private Timer socketExpireTimer = new Timer(12000);
        private ProxySocket socketListener = null;
        private Socket dcSocket = null;
        private bool isListener = false;
        private bool fooHandled = false;
        private bool authenticated = false;

        private void socketExpireTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            socketExpireTimer.Elapsed -= socketExpireTimer_Elapsed;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                "I was waiting for " + (socketExpireTimer.Interval / 1000) + " seconds, but no one has connected!", GetType().Name);

            Dispose();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PDirectProcessor(ConnectivitySettings connectivitySettings, P2PVersion p2pVersion, Guid authNonce, bool isHashedNonce)
            : base(connectivitySettings)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object - " + p2pVersion, GetType().Name);

            this.version = p2pVersion;
            this.authNonce = authNonce;
            this.isAuthNonceHashed = isHashedNonce;
            this.MessagePool = new P2PDCPool();
        }

        ~P2PDirectProcessor()
        {
            Dispose(false);
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
            set
            {
                isListener = value;
            }
        }

        /// <summary>
        /// Starts listening at the specified port in the connectivity settings.
        /// </summary>
        public void Listen(IPAddress address, int port)
        {
            ProxySocket socket = GetPreparedSocket(address, port);

            // Begin waiting for the incoming connection
            socket.Listen(1);

            // set this value so we know whether to send a handshake message or not later in the process
            isListener = true;
            socketListener = socket;

            socketExpireTimer.Elapsed += new ElapsedEventHandler(socketExpireTimer_Elapsed);
            socketExpireTimer.AutoReset = false;
            socketExpireTimer.Enabled = true; // After accepted, DISABLE timer.
            socket.BeginAccept(new AsyncCallback(EndAcceptCallback), socket);
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

                // Disable timer. Otherwise, data transfer will be broken after 12 secs.
                // Huge datas can't transmit within 12 secs :) 
                socketExpireTimer.Enabled = false;

                // Stop listening
                StopListening();

                // Begin accepting messages
                BeginDataReceive(dcSocket);

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
        public override void Disconnect()
        {
            base.Disconnect();

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


        protected override void OnConnected()
        {
            if (!IsListener)
            {
                // Send foo0: datalen + data
                SendSocketData(new byte[] { 0x04, 0x00, 0x00, 0x00, 0x66, 0x6f, 0x6f, 0x00 });
            }
            base.OnConnected();
        }

        /// <summary>
        /// Discards the foo message and sends the message to all handlers as a P2PDCMessage object.
        /// </summary>
        /// <param name="data"></param>
        protected override void OnMessageReceived(byte[] data)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "analyzing message", GetType().Name);

            // Foo state
            if (!fooHandled)
            {
                string initialData = Encoding.ASCII.GetString(data);

                if (data.Length == 4 && initialData == "foo\0")
                {
                    fooHandled = true;
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "foo0 handled", GetType().Name);
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "foo0 expected, but it was: " + initialData, GetType().Name);
                    Dispose();
                }

                return;
            }


            // Auth state
            if (!authenticated)
            {
                P2PDCHandshakeMessage hm = new P2PDCHandshakeMessage(version);
                hm.ParseBytes(data);

                Guid incomingGuid = hm.Guid;

                if (isAuthNonceHashed)
                {
                    incomingGuid = HashedNonceGenerator.HashNonce(incomingGuid);
                    isAuthNonceHashed = false;
                }

                if (authNonce == incomingGuid)
                {
                    authenticated = true;

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "auth key:" + authNonce, GetType().Name);

                    OnHandshakeCompleted(new P2PHandshakeMessageEventArgs(hm));
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        String.Format("Received nonce is {0}, expected {1}", incomingGuid, authNonce), GetType().Name);

                    Dispose();
                }
                return;
            }

            // Data state: convert to a p2pdc message
            P2PDCMessage dcMessage = new P2PDCMessage(version);
            dcMessage.ParseBytes(data);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, dcMessage.ToDebugString(), GetType().Name);

            lock (MessageHandlers)
            {
                foreach (IMessageHandler handler in MessageHandlers)
                {
                    handler.HandleMessage(this, dcMessage);
                }
            }
        }

        /// <summary>
        /// Sends the P2PMessage directly over the socket. Accepts P2PDCMessage and P2PMessage objects.
        /// </summary>
        /// <param name="message"></param>
        public override void SendMessage(NetworkMessage message)
        {
            // if it is a regular message convert it
            P2PDCMessage p2pMessage = message as P2PDCMessage;
            if (p2pMessage == null)
            {
                p2pMessage = new P2PDCMessage((P2PMessage)message);
            }


            // prepare the message
            p2pMessage.PrepareMessage();

            // this is very bloated!
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + p2pMessage.ToDebugString(), GetType().Name);

            if (dcSocket != null)
                SendSocketData(dcSocket, p2pMessage.GetBytes());
            else
                SendSocketData(p2pMessage.GetBytes());
        }

        protected virtual void OnHandshakeCompleted(P2PHandshakeMessageEventArgs e)
        {
            if (HandshakeCompleted != null)
                HandshakeCompleted(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }

            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
};
