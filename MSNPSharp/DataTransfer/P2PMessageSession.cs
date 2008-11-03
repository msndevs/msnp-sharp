#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// P2PMessageSession routes all messages in the p2p framework between the local client and a single remote client.
    /// </summary>
    /// <remarks>
    /// A single message session can hold multiple p2p transfer sessions. This for example occurs when a contact sends
    /// two files directly after each other in the same switchboard session.
    /// This class keeps track of the message identifiers, dispatches messages to registered message handlers and routes
    /// data messages to the correct <see cref="P2PTransferSession"/> objects. Usually this class is a handler of a switchboard processor.
    /// A common handler for this class is <see cref="MSNSLPHandler"/>.
    /// </remarks>
    public class P2PMessageSession : IMessageHandler, IMessageProcessor
    {
        #region Properties
        /// <summary>
        /// </summary>
        private uint localBaseIdentifier;
        /// <summary>
        /// </summary>
        private uint localIdentifier;
        /// <summary>
        /// </summary>
        private uint remoteBaseIdentifier;
        /// <summary>
        /// </summary>
        private uint remoteIdentifier;

        /// <summary>
        /// </summary>
        private P2PDCHandshakeMessage handshakeMessage;

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
        /// </summary>
        private bool autoAcknowledgeHandshake = true;

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
        /// </summary>		
        private bool directConnected = false;

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
        /// </summary>		
        private bool directConnectionAttempt = false;

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
        /// Occurs when a direct connection is succesfully established.
        /// </summary>
        public event EventHandler DirectConnectionEstablished;

        /// <summary>
        /// Occurs when a direct connection attempt has failed.
        /// </summary>
        public event EventHandler DirectConnectionFailed;


        /// <summary>
        /// The base identifier of the local client
        /// </summary>
        public uint LocalBaseIdentifier
        {
            get
            {
                return localBaseIdentifier;
            }
            set
            {
                localBaseIdentifier = value;
            }
        }

        /// <summary>
        /// The identifier of the local contact. This identifier is increased just before a message is send.
        /// </summary>
        public uint LocalIdentifier
        {
            get
            {
                return localIdentifier;
            }
            set
            {
                localIdentifier = value;
            }
        }

        /// <summary>
        /// The base identifier of the remote client
        /// </summary>
        public uint RemoteBaseIdentifier
        {
            get
            {
                return remoteBaseIdentifier;
            }
            set
            {
                remoteBaseIdentifier = value;
            }
        }
        /// <summary>
        /// The expected identifier of the remote client for the next message.
        /// </summary>
        public uint RemoteIdentifier
        {
            get
            {
                return remoteIdentifier;
            }
            set
            {
                remoteIdentifier = value;
            }
        }

        /// <summary>
        /// </summary>
        private string remoteContact;
        /// <summary>
        /// </summary>
        private string localContact;

        /// <summary>
        /// The account of the local contact.
        /// </summary>
        public string LocalContact
        {
            get
            {
                return localContact;
            }
            set
            {
                localContact = value;
            }
        }

        /// <summary>
        /// The account of the remote contact.
        /// </summary>
        public string RemoteContact
        {
            get
            {
                return remoteContact;
            }
            set
            {
                remoteContact = value;
            }
        }

        #endregion

        #region Public
        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PMessageSession()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        /// <summary>
        /// Removes references to handlers and the messageprocessor. Also closes running transfer sessions and pending processors establishing connections.
        /// </summary>
        public virtual void CleanUp()
        {
            StopAllPendingProcessors();
            AbortAllTransfers();

            handlers.Clear();

            MessageProcessor = null;

            transferSessions.Clear();
        }

        /// <summary>
        /// A list of all direct processors trying to establish a connection.
        /// </summary>
        private ArrayList pendingProcessors = new ArrayList();

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
            processor.ConnectionEstablished += new EventHandler(OnDirectProcessorConnected);
            processor.ConnectionClosed += new EventHandler(OnDirectProcessorDisconnected);
            processor.ConnectingException += new ProcessorExceptionEventHandler(OnDirectProcessorException);

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
        /// Aborts all running transfer sessions.
        /// </summary>
        public virtual void AbortAllTransfers()
        {
            Hashtable transferSessions_copy = new Hashtable(transferSessions);
            foreach (P2PTransferSession session in transferSessions_copy.Values)
            {
                session.AbortTransfer();
            }
        }

        /// <summary>
        /// Corrects the local identifier with the specified correction.
        /// </summary>
        /// <param name="correction"></param>
        public void CorrectLocalIdentifier(int correction)
        {
            if (correction < 0)
                LocalIdentifier -= (uint)Math.Abs(correction);
            else
                LocalIdentifier += (uint)Math.Abs(correction);
        }

        /// <summary>
        /// The identifier of the local client, increases with each message send
        /// </summary>		
        public void IncreaseLocalIdentifier()
        {
            localIdentifier++;
            if (localIdentifier == localBaseIdentifier)
                localIdentifier++;
        }

        /// <summary>
        /// The identifier of the remote client, increases with each message received
        /// </summary>
        public void IncreaseRemoteIdentifier()
        {
            remoteIdentifier++;
            if (remoteIdentifier == remoteBaseIdentifier)
                remoteIdentifier++;
        }

        /// <summary>
        /// Adds the specified transfer session to the collection and sets the transfer session's message processor to be the
        /// message processor of the p2p message session. This is usally a SB message processor. 
        /// </summary>
        /// <param name="session"></param>
        public void AddTransferSession(P2PTransferSession session)
        {
            session.MessageProcessor = this;

            lock (handlers)
            {
                foreach (IMessageHandler handler in handlers)
                {
                    session.RegisterHandler(handler);
                }
            }

            transferSessions.Add(session.SessionId, session);
        }

        /// <summary>
        /// Removes the specified transfer session from the collection.
        /// </summary>
        public void RemoveTransferSession(P2PTransferSession session)
        {
            transferSessions.Remove(session.SessionId);
        }

        /// <summary>
        /// Returns the transfer session associated with the specified session identifier.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public P2PTransferSession GetTransferSession(uint sessionId)
        {
            return (P2PTransferSession)transferSessions[sessionId];
        }

        /// <summary>
        /// Searches through all handlers and returns the first object with the specified type, or null if not found.
        /// </summary>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        public object GetHandler(Type handlerType)
        {
            foreach (IMessageHandler handler in handlers)
            {
                if (handler.GetType() == handlerType)
                    return handler;
            }
            return null;
        }

        #endregion

        #region Protected

        /// <summary>
        /// Keeps track of clustered p2p messages
        /// </summary>
        protected P2PMessagePool P2PMessagePool
        {
            get
            {
                return p2pMessagePool;
            }
            set
            {
                p2pMessagePool = value;
            }
        }

        /// <summary>
        /// </summary>
        private P2PMessagePool p2pMessagePool = new P2PMessagePool();

        /// <summary>
        /// Wraps a P2PMessage in a MSGMessage and SBMessage.
        /// </summary>
        /// <returns></returns>
        protected SBMessage WrapMessage(NetworkMessage networkMessage)
        {
            // create wrapper messages
            MSGMessage msgWrapper = new MSGMessage();
            msgWrapper.MimeHeader["P2P-Dest"] = RemoteContact;
            msgWrapper.MimeHeader["Content-Type"] = "application/x-msnmsgrp2p";
            msgWrapper.InnerMessage = networkMessage;

            SBMessage sbMessageWrapper = new SBMessage();
            sbMessageWrapper.InnerMessage = msgWrapper;

            return sbMessageWrapper;
        }

        /// <summary>
        /// Sends the handshake message in a direct connection.
        /// </summary>
        protected virtual void SendHandshakeMessage(IMessageProcessor processor)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Preparing to send handshake message", GetType().Name);

            if (HandshakeMessage == null)
            {
                // don't throw an exception because the file transfer can continue over the switchboard
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Handshake could not be send because none is specified.", GetType().Name);

                // but close the direct connection
                ((SocketMessageProcessor)processor).Disconnect();
                return;
            }

            IncreaseLocalIdentifier();
            HandshakeMessage.Identifier = LocalIdentifier;

            HandshakeMessage.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            DCHandshakeAck = HandshakeMessage.AckSessionId;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Sending handshake message:\r\n " + HandshakeMessage.ToDebugString(), GetType().Name);

            ((SocketMessageProcessor)processor).SendMessage(HandshakeMessage);
        }

        #endregion

        #region Private
        /// <summary>
        /// A collection of all transfersessions
        /// </summary>
        private Hashtable transferSessions = new Hashtable();

        /// <summary>
        /// This is the processor used before a direct connection. Usually a SB processor.
        /// It is a fallback variables in case a direct connection fails.
        /// </summary>
        private IMessageProcessor preDCProcessor = null;

        /// <summary>
        /// Tracked to know when an acknowledgement for the handshake is received.
        /// </summary>
        private uint DCHandshakeAck = 0;

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

            if (((P2PDirectProcessor)sender).IsListener == false)
            {
                if (AutoHandshake == true && HandshakeMessage != null)
                {
                    SendHandshakeMessage((P2PDirectProcessor)sender);
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
        #endregion

        #region IMessageHandler Members
        private IMessageProcessor messageProcessor = null;
        /// <summary>
        /// The message processor that sends the P2P messages to the remote contact.
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                messageProcessor = value;
                if (messageProcessor != null && messageProcessor.GetType() != typeof(NSMessageProcessor))
                {
                    ValidateProcessor();
                    SendBuffer();
                }
            }
        }


        /// <summary>
        /// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            System.Diagnostics.Debug.Assert(message is P2PMessage, "Incoming message is not a P2PMessage", "");

            P2PMessage p2pMessage = (P2PMessage)message;

            // check whether it is an acknowledgement to data preparation message
            if (p2pMessage.Flags == 0x100 && DCHandshakeAck != 0)
            {
                OnHandshakeCompleted((P2PDirectProcessor)sender);
                return;
            }

            // check if it's a direct connection handshake
            if (p2pMessage.Flags == 0x100 && AutoHandshake == true)
            {
                // create a handshake message based on the incoming p2p message and send it				
                P2PDCHandshakeMessage dcHsMessage = new P2PDCHandshakeMessage(p2pMessage);
                sender.SendMessage(dcHsMessage.CreateAcknowledgement());
                OnHandshakeCompleted((P2PDirectProcessor)sender);
                return;
            }

            if (p2pMessage.Flags == 0x08)
            {

                P2PTransferSession session = (P2PTransferSession)transferSessions[p2pMessage.SessionId];
                if (session != null)
                {
                    session.AbortTransfer();
                }

                return;
            }

            // check if it is a content message
            if (p2pMessage.SessionId > 0)
            {
                // get the session to handle this message				
                P2PTransferSession session = (P2PTransferSession)transferSessions[p2pMessage.SessionId];
                if (session != null)
                    session.HandleMessage(this, p2pMessage);
                return;
            }

            // it is not a datamessage.
            // fill up the buffer with this message and extract the messages one-by-one and dispatch
            // it to all handlers. Usually the MSNSLP handler.
            p2pMessagePool.BufferMessage(p2pMessage);

            while (p2pMessagePool.MessageAvailable)
            {
                // keep track of the remote identifier			
                IncreaseRemoteIdentifier();

                p2pMessage = p2pMessagePool.GetNextMessage();

                lock (handlers)
                {
                    // the message is not a datamessage, send it to the handlers
                    foreach (IMessageHandler handler in handlers)
                        handler.HandleMessage(this, message);
                }
            }
        }

        #endregion

        #region IMessageProcessor Members
        /// <summary>
        /// Sends incoming p2p messages to the remote contact.		
        /// </summary>
        /// <remarks>
        /// Before the message is send a couple of things are checked. If there is no identifier available, the local identifier will be increased by one and set as the message identifier.
        /// Second, if the acknowledgement identifier is not set it will be set to a random value. After this the method will check for the total length of the message. If the total length
        /// is too large, the message will be splitted into multiple messages. The maximum size for p2p messages over a switchboard is 1202 bytes. The maximum size for p2p messages over a
        /// direct connection is 1352 bytes. As a result the length of the splitted messages will be 1202 or 1352 bytes or smaller, depending on the availability of a direct connection.
        /// 
        /// If a direct connection is available the message is wrapped in a <see cref="P2PDCMessage"/> object and send over the direct connection. Otherwise it will be send over a switchboard session.
        /// If there is no switchboard session available, or it has become invalid, a new switchboard session will be requested by asking this to the nameserver handler.
        /// Messages will be buffered until a switchboard session, or a direct connection, becomes available. Upon a new connection the buffered messages are directly send to the remote contact
        /// over the new connection.
        /// </remarks>
        /// <param name="message">The P2PMessage to send to the remote contact.</param>
        public void SendMessage(NetworkMessage message)
        {
            P2PMessage p2pMessage = (P2PMessage)message;

            // check whether it's already set. This is important to check for acknowledge messages.
            if (p2pMessage.Identifier == 0)
            {
                IncreaseLocalIdentifier();
                p2pMessage.Identifier = LocalIdentifier;
            }
            if (p2pMessage.AckSessionId == 0)
            {
                p2pMessage.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }

            // maximum length by default is 1202 bytes.
            int maxSize = 1202;

            // check whether we have a direct connection (send p2pdc messages) or not (send sb messages)
            if (DirectConnected == true)
            {
                maxSize = 1352;
            }

            // split up large messages which go to the SB
            if (p2pMessage.MessageSize > maxSize)
            {
                byte[] totalMessage = null;
                if (p2pMessage.InnerBody != null)
                    totalMessage = p2pMessage.InnerBody;
                else if (p2pMessage.InnerMessage != null)
                    totalMessage = p2pMessage.InnerMessage.GetBytes();
                else
                    throw new MSNPSharpException("An error occured while splitting a large p2p message into smaller messages: Both the InnerBody and InnerMessage are null.");

                uint bytesSend = 0;
                int cnt = ((int)(p2pMessage.MessageSize / maxSize)) + 1;
                for (int i = 0; i < cnt; i++)
                {
                    P2PMessage chunkMessage = new P2PMessage();

                    // copy the values from the original message
                    chunkMessage.AckIdentifier = p2pMessage.AckIdentifier;
                    chunkMessage.AckTotalSize = p2pMessage.AckTotalSize;
                    chunkMessage.Flags = p2pMessage.Flags;
                    chunkMessage.Footer = p2pMessage.Footer;
                    chunkMessage.Identifier = p2pMessage.Identifier;
                    chunkMessage.MessageSize = (uint)Math.Min((uint)maxSize, (uint)(p2pMessage.MessageSize - bytesSend));
                    chunkMessage.Offset = bytesSend;
                    chunkMessage.SessionId = p2pMessage.SessionId;
                    chunkMessage.TotalSize = p2pMessage.MessageSize;

                    chunkMessage.InnerBody = new byte[chunkMessage.MessageSize];
                    Array.Copy(totalMessage, (int)chunkMessage.Offset, chunkMessage.InnerBody, 0, (int)chunkMessage.MessageSize);


                    chunkMessage.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);

                    chunkMessage.PrepareMessage();

                    // now send it to propbably a SB processor
                    try
                    {
                        if (MessageProcessor != null && ((SocketMessageProcessor)MessageProcessor).Connected == true)
                        {
                            if (DirectConnected == true)
                            {
                                MessageProcessor.SendMessage(new P2PDCMessage(chunkMessage));
                            }
                            else
                            {
                                // wrap the message before sending it to the (probably) SB processor
                                MessageProcessor.SendMessage(WrapMessage(chunkMessage));
                            }
                        }

                        else
                        {
                            InvalidateProcessor();
                            BufferMessage(chunkMessage);
                        }
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        InvalidateProcessor();
                        BufferMessage(chunkMessage);
                    }

                    bytesSend += chunkMessage.MessageSize;
                }
            }
            else
            {
                try
                {
                    if (MessageProcessor != null)
                    {
                        if (DirectConnected == true)
                        {
                            MessageProcessor.SendMessage(new P2PDCMessage(p2pMessage));
                        }
                        else
                        {
                            // wrap the message before sending it to the (probably) SB processor
                            MessageProcessor.SendMessage(WrapMessage(p2pMessage));
                        }
                    }
                    else
                    {
                        InvalidateProcessor();
                        BufferMessage(p2pMessage);
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    InvalidateProcessor();
                    BufferMessage(p2pMessage);
                }
            }
        }

        /// <summary>
        /// Occurs when the processor has been marked as invalid. Due to connection error, or message processor being null.
        /// </summary>
        public event EventHandler ProcessorInvalid;

        /// <summary>
        /// Keeps track of unsend messages
        /// </summary>
        private Queue sendMessages = new Queue();

        /// <summary>
        /// 
        /// </summary>
        private bool processorValid = true;

        /// <summary>
        /// Indicates whether the processor is invalid
        /// </summary>
        public bool ProcessorValid
        {
            get
            {
                return processorValid;
            }
        }

        /// <summary>
        /// Sets the processor as invalid, and requests the p2phandler for a new request.
        /// </summary>
        protected virtual void InvalidateProcessor()
        {
            if (processorValid == false)
                return;

            processorValid = false;
            OnProcessorInvalid();

        }

        /// <summary>
        /// Sets the processor as valid.
        /// </summary>
        protected virtual void ValidateProcessor()
        {
            processorValid = true;
        }

        /// <summary>
        /// Fires the ProcessorInvalid event.
        /// </summary>
        protected virtual void OnProcessorInvalid()
        {
            if (ProcessorInvalid != null)
                ProcessorInvalid(this, new EventArgs());
        }

        /// <summary>
        /// Buffer messages that can not be send because of an invalid message processor.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void BufferMessage(NetworkMessage message)
        {
            if (sendMessages.Count >= 100)
                System.Threading.Thread.CurrentThread.Join(200);

            sendMessages.Enqueue(message);
        }

        /// <summary>
        /// Try to resend any messages that were stored in the buffer.
        /// </summary>
        protected virtual void SendBuffer()
        {
            if (MessageProcessor == null)
                return;

            try
            {
                while (sendMessages.Count > 0)
                {
                    if (DirectConnected == true)
                        MessageProcessor.SendMessage(new P2PDCMessage((P2PMessage)sendMessages.Dequeue()));
                    else
                        MessageProcessor.SendMessage(WrapMessage((NetworkMessage)sendMessages.Dequeue()));
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                InvalidateProcessor();
            }
        }

        private ArrayList handlers = new ArrayList();

        /// <summary>
        /// Registers a message handler. After registering the handler will receive incoming messages.
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            lock (handlers)
            {
                if (handlers.Contains(handler) == true)
                    return;
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// Unregisters a message handler. After registering the handler will no longer receive incoming messages.
        /// </summary>
        /// <param name="handler"></param>
        public void UnregisterHandler(IMessageHandler handler)
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }

        #endregion
    }
};
