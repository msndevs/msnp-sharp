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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    partial class P2PMessageSession
    {
        /// <summary>
        /// Occurs when a direct connection is succesfully established.
        /// </summary>
        public event EventHandler<EventArgs> DirectConnectionEstablished;

        /// <summary>
        /// Occurs when a direct connection attempt has failed.
        /// </summary>
        public event EventHandler<EventArgs> DirectConnectionFailed;



        private P2PDCHandshakeMessage handshakeMessage;
        private bool autoAcknowledgeHandshake = true;
        private bool directConnectionAttempt;
        private bool directConnected;

        

        /// <summary>
        /// Tracked to know when an acknowledgement for the handshake is received.
        /// </summary>
        private uint DCHandshakeAck;

        /// <summary>
        /// The handshake message to send to the receiving client when a direct connection has been established
        /// </summary>
        /// <remarks>
        /// If this property is set to null no handshake message is sent.
        /// </remarks>
        public P2PDCHandshakeMessage HandshakeMessage
        {
            get
            {
                return handshakeMessage;
            }
            set
            {
                handshakeMessage = value;
            }
        }        

        /// <summary>
        /// Defines whether a direct connection handshake is automatically send to the remote client, or replied with an acknowledgement.
        /// Setting this to true means the remote client will start the transfer immediately.
        /// Setting this to false means the client programmer must send a handhsake message and an acknowledgement message after which the transfer will begin.
        /// </summary>
        public bool AutoHandshake
        {
            get
            {
                return autoAcknowledgeHandshake;
            }
            set
            {
                autoAcknowledgeHandshake = value;
            }
        }

        /// <summary>
        /// Defines whether an attempt has been made to create a direct connection
        /// </summary>
        public bool DirectConnectionAttempt
        {
            get
            {
                return directConnectionAttempt;
            }
        }

        /// <summary>
        /// Defines whether the message session runs over a direct session or is routed via the messaging server
        /// </summary>
        public bool DirectConnected
        {
            get
            {
                return directConnected;
            }
        }


        /// <summary>
        /// A list of all direct processors trying to establish a connection.
        /// </summary>
        private List<P2PDirectProcessor> pendingProcessors = new List<P2PDirectProcessor>();

        /// <summary>
        /// Disconnect all processors that are trying to establish a connection.
        /// </summary>
        protected void StopAllPendingProcessors()
        {
            lock (pendingProcessors)
            {
                foreach (P2PDirectProcessor processor in pendingProcessors)
                {
                    processor.Disconnect();
                    processor.UnregisterHandler(this);
                }
                pendingProcessors.Clear();
            }
        }

        /// <summary>
        /// Add the processor to the pending list.
        /// </summary>
        /// <param name="processor"></param>
        protected void AddPendingProcessor(P2PDirectProcessor processor)
        {
            // we want to handle message from this processor
            processor.RegisterHandler(this);

            // inform the session of connected/disconnected events
            processor.ConnectionEstablished += new EventHandler<EventArgs>(OnDirectProcessorConnected);
            processor.ConnectionClosed += new EventHandler<EventArgs>(OnDirectProcessorDisconnected);
            processor.ConnectingException += new EventHandler<ExceptionEventArgs>(OnDirectProcessorException);

            lock (pendingProcessors)
            {
                pendingProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Use the given processor as the direct connection processor. And disconnect all other pending processors.
        /// </summary>
        /// <param name="processor"></param>
        protected void UsePendingProcessor(P2PDirectProcessor processor)
        {
            lock (pendingProcessors)
            {
                if (pendingProcessors.Contains(processor))
                {
                    pendingProcessors.Remove(processor);
                }
            }

            // stop all remaining attempts
            StopAllPendingProcessors();

            // set the direct processor as the main processor
            lock (this)
            {
                directConnected = true;
                directConnectionAttempt = true;
                preDCProcessor = MessageProcessor;
                MessageProcessor = processor;
            }

            if (DirectConnectionEstablished != null)
                DirectConnectionEstablished(this, new EventArgs());
        }

        /// <summary>
        /// Creates a direct connection with the remote client.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public IMessageProcessor CreateDirectConnection(string host, int port)
        {
            // create the P2P Direct processor to handle the file data
            P2PDirectProcessor processor = new P2PDirectProcessor();
            processor.ConnectivitySettings = new ConnectivitySettings();
            processor.ConnectivitySettings.Host = host;
            processor.ConnectivitySettings.Port = port;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Trying to setup direct connection with remote host " + host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture), GetType().Name);

            AddPendingProcessor(processor);

            // try to connect
            processor.Connect();

            return processor;
        }

        /// <summary>
        /// Setups a P2PDirectProcessor to listen for incoming connections.
        /// After a connection has been established the P2PDirectProcessor will become the main MessageProcessor to send messages.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public IMessageProcessor ListenForDirectConnection(IPAddress host, int port)
        {
            P2PDirectProcessor processor = new P2PDirectProcessor();

            // we want this session to listen to incoming messages
            processor.RegisterHandler(this);

            // add to the list of processors trying to establish a connection
            AddPendingProcessor(processor);

            // start to listen
            processor.Listen(host, port);

            return processor;
        }

        /// <summary>
        /// Closes the direct connection with the remote client, if available. A closing p2p message will be send first.
        /// The session will fallback to the previous (SB) message processor.
        /// </summary>
        public void CloseDirectConnection()
        {
            if (DirectConnected == false)
                return;
            else
            {
                CleanUpDirectConnection();
            }
        }

        /// <summary>
        /// Sends the handshake message in a direct connection.
        /// </summary>
        protected virtual void SendHandshakeMessage(IMessageProcessor processor)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Preparing to send handshake message", GetType().Name);

            SocketMessageProcessor smp = (SocketMessageProcessor)processor;

            if (HandshakeMessage == null)
            {
                // don't throw an exception because the file transfer can continue over the switchboard
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Handshake could not be send because none is specified.", GetType().Name);

                // but close the direct connection
                smp.Disconnect();
                return;
            }

            IncreaseLocalIdentifier();
            HandshakeMessage.Header.Identifier = LocalIdentifier;

            HandshakeMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            DCHandshakeAck = HandshakeMessage.V1Header.AckSessionId;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Sending handshake message:\r\n " + HandshakeMessage.ToDebugString(), GetType().Name);

            smp.SendMessage(HandshakeMessage);
        }

        

        /// <summary>
        /// Sets the message processor back to the switchboard message processor.
        /// </summary>
        private void CleanUpDirectConnection()
        {
            lock (this)
            {
                if (DirectConnected == false)
                    return;

                SocketMessageProcessor directProcessor = (SocketMessageProcessor)MessageProcessor;

                directConnected = false;
                directConnectionAttempt = false;
                MessageProcessor = preDCProcessor;

                directProcessor.Disconnect();
            }
        }



        /// <summary>
        /// Sets the current message processor to the processor which has just connected succesfully.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDirectProcessorConnected(object sender, EventArgs e)
        {
            //DCHandshakeProcessor = (IMessageProcessor)sender;
            P2PDirectProcessor p2pdp = (P2PDirectProcessor)sender;

            if (p2pdp.IsListener == false)
            {
                if (AutoHandshake == true && HandshakeMessage != null)
                {
                    SendHandshakeMessage(p2pdp);
                }
            }
        }

        /// <summary>
        /// Cleans up the direct connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDirectProcessorDisconnected(object sender, EventArgs e)
        {
            CleanUpDirectConnection();
        }

        /// <summary>
        /// Called when the direct processor could not connect. It will start the data transfer over the switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDirectProcessorException(object sender, ExceptionEventArgs e)
        {
            lock (this)
            {
                CleanUpDirectConnection();
                directConnectionAttempt = true;
            }

            if (DirectConnectionFailed != null)
                DirectConnectionFailed(this, new EventArgs());
        }

        /// <summary>
        /// Occurs when an acknowledgement to a send handshake has been received, or a handshake is received.
        /// This will start the data transfer, provided the local client is the sender.
        /// </summary>
        protected virtual void OnHandshakeCompleted(P2PDirectProcessor processor)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Handshake accepted", GetType().Name);

            UsePendingProcessor(processor);
        }


    }
};
