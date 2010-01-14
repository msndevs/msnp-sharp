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
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
using Org.Mentalis.Network.ProxySocket;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp.Core;


    /// <summary>
    /// Handles the direct connections in P2P sessions.
    /// </summary>
    public class P2PDirectProcessor : SocketMessageProcessor
    {
        private Timer socketExpireTimer = new Timer(5000);
        private ProxySocket socketListener = null;

        private void socketExpireTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (socketListener != null)
            {
                try
                {
                    socketListener.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, GetType().ToString() + " Error: " + ex.Message);
                }
                finally
                {
                    socketListener.Close();
                }

                // clean up the socket properly
                if (dcSocket == null || !IsSocketConnected(dcSocket))
                {
                    base.Disconnect();

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
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PDirectProcessor()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);

            MessagePool = new P2PDCPool();
        }

        /// <summary>
        /// Starts listening at the specified port in the connectivity settings.
        /// </summary>
        public void Listen(IPAddress address, int port)
        {
            ProxySocket socket = GetPreparedSocket();

            // begin waiting for the incoming connection			
            socket.Bind(new IPEndPoint(address, port));

            socket.Listen(100);

            // set this value so we know whether to send a handshake message or not later in the process
            isListener = true;
            socketListener = socket;

            socketExpireTimer.Elapsed += new ElapsedEventHandler(socketExpireTimer_Elapsed);
            socketExpireTimer.AutoReset = false;
            socketExpireTimer.Enabled = true;

            socket.BeginAccept(new AsyncCallback(EndAcceptCallback), socket);
        }

        /// <summary>
        /// Returns whether this processor was initiated as listening (true) or connecting (false).
        /// </summary>
        private bool isListener;

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
        /// Called when an incoming connection has been accepted.
        /// </summary>
        /// <param name="ar"></param>
        protected virtual void EndAcceptCallback(IAsyncResult ar)
        {
            ProxySocket listenSocket = (ProxySocket)ar.AsyncState;
            try
            {
                dcSocket = listenSocket.EndAccept(ar);

                // begin accepting messages
                BeginDataReceive(dcSocket);

                OnConnected();
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, GetType().ToString() + " Error: " + ex.Message);
            }
        }

        private Socket dcSocket;

        /// <summary>
        /// Closes the socket connection.
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();

            // clean up the socket properly
            if (dcSocket != null && IsSocketConnected(dcSocket))
            {
                dcSocket.Shutdown(SocketShutdown.Both);
                dcSocket.Close();
                dcSocket = null;
            }
        }

        /// <summary>
        /// Discards the foo message and sends the message to all handlers as a P2PDCMessage object.
        /// </summary>
        /// <param name="data"></param>
        protected override void OnMessageReceived(byte[] data)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "analyzing message", "P2PDirect In");

            // check if it is the 'foo' message
            if (data.Length == 4)
                return;

            // convert to a p2pdc message
            P2PDCMessage dcMessage = new P2PDCMessage(P2PVersion.P2PV1);
            dcMessage.ParseBytes(data);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, dcMessage.ToDebugString(), "P2PDirect In");

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
            if ((message is P2PDCMessage) == false)
            {
                message = new P2PDCMessage((P2PMessage)message);
            }
            // otherwise we just assume it is a P2PDCMessage

            // prepare the message
            message.PrepareMessage();

            // this is very bloated!
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + message.ToDebugString(), GetType().Name);

            if (dcSocket != null)
                SendSocketData(dcSocket, message.GetBytes());
            else
                SendSocketData(message.GetBytes());
        }

    }
};
