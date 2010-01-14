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
    public partial class P2PMessageSession : IMessageHandler, IMessageProcessor
    {
        #region Properties

        private uint localBaseIdentifier;
        private uint localIdentifier;
        private uint remoteBaseIdentifier;
        private uint remoteIdentifier;
        private string remoteContact;
        private string localContact;
        private Contact remoteClient;


        private Contact localUser;


        private P2PVersion version = P2PVersion.P2PV1;

        public Contact LocalUser
        {
            get { return localUser; }
            set 
            {
                LocalContact = value.Mail;
                localUser = value;
            }
        }

        public Contact RemoteUser
        {
            get { return remoteClient; }
            set { 
                remoteClient = value;
                RemoteContact = value.Mail;
            }
        }

        public P2PVersion Version
        {
            get { return version; }
        }

        /// <summary>
        /// This is the processor used before a direct connection. Usually a SB processor.
        /// It is a fallback variables in case a direct connection fails.
        /// </summary>
        private IMessageProcessor preDCProcessor;

        /// <summary>
        /// A collection of all transfersessions
        /// </summary>
        private Dictionary<uint, P2PTransferSession> transferSessions = new Dictionary<uint, P2PTransferSession>();
        
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
        protected P2PMessageSession()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PMessageSession(P2PVersion ver)
        {
            version = ver;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object, version = " + ver.ToString(), GetType().Name);
        }

        /// <summary>
        /// Removes references to handlers and the messageprocessor. Also closes running transfer sessions and pending processors establishing connections.
        /// </summary>
        public virtual void CleanUp()
        {
            StopAllPendingProcessors();
            AbortAllTransfers();

            lock (handlers)
                handlers.Clear();

            MessageProcessor = null;

            lock (transferSessions)
                transferSessions.Clear();
        }

        /// <summary>
        /// Aborts all running transfer sessions.
        /// </summary>
        public virtual void AbortAllTransfers()
        {
            List<P2PTransferSession> transferSessions_copy = new List<P2PTransferSession>(transferSessions.Values);
            foreach (P2PTransferSession session in transferSessions_copy)
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

            transferSessions.Add(session.TransferProperties.SessionId, session);
        }

        /// <summary>
        /// Removes the specified transfer session from the collection.
        /// </summary>
        public void RemoveTransferSession(P2PTransferSession session)
        {
            if (session != null)
            {
                lock (transferSessions)
                    transferSessions.Remove(session.TransferProperties.SessionId);
            }
        }

        /// <summary>
        /// Returns the transfer session associated with the specified session identifier.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public P2PTransferSession GetTransferSession(uint sessionId)
        {
            return transferSessions.ContainsKey(sessionId) ? transferSessions[sessionId] : null;
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
        /// Wraps a P2PMessage in a MSGMessage and SBMessage.
        /// </summary>
        /// <returns></returns>
        protected SBMessage WrapMessage(NetworkMessage networkMessage)
        {
            // create wrapper messages
            MSGMessage msgWrapper = new MSGMessage();
            if (Version == P2PVersion.P2PV1)
            {
                msgWrapper.MimeHeader["P2P-Dest"] = RemoteContact;
                msgWrapper.MimeHeader["P2P-Src"] = LocalContact;
            }

            if (Version == P2PVersion.P2PV2)
            {
                if (RemoteUser != null &&
                    LocalUser != null &&
                    RemoteUser.MachineGuid != Guid.Empty &&
                    LocalUser.MachineGuid != Guid.Empty)
                {
                    //Created from local.
                    msgWrapper.MimeHeader["P2P-Dest"] = RemoteUser.Mail + ";" + RemoteUser.MachineGuid.ToString("B");
                    msgWrapper.MimeHeader["P2P-Src"] = LocalUser.Mail + ";" + LocalUser.MachineGuid.ToString("B");
                }
                else
                {
                    //Created from remote
                    msgWrapper.MimeHeader["P2P-Dest"] = RemoteContact;
                    msgWrapper.MimeHeader["P2P-Src"] = LocalContact;
                }
            }

            msgWrapper.MimeHeader[MimeHeaderStrings.Content_Type] = "application/x-msnmsgrp2p";
            msgWrapper.InnerMessage = networkMessage;

            SBMessage sbMessageWrapper = new SBMessage();
            sbMessageWrapper.InnerMessage = msgWrapper;

            return sbMessageWrapper;
        }

        #endregion

        #region Private
        

        
        #endregion

        #region IMessageHandler Members
        private IMessageProcessor messageProcessor;
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

                if (MessageProcessor != null && MessageProcessor.GetType() != typeof(NSMessageProcessor))
                {
                    ValidateProcessor();
                    SendBuffer();
                }
            }
        }


        /// <summary>
        /// Handles P2PMessages. Other messages are ignored.
        /// All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Debug.Assert(p2pMessage != null, "Incoming message is not a P2PMessage", "");

            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                // Keep track of the remote identifier
                RemoteIdentifier = p2pMessage.Header.Identifier;

                // Check whether it is an acknowledgement to data preparation message
                if (p2pMessage.V1Header.Flags == P2PFlag.DirectHandshake && DCHandshakeAck != 0)
                {
                    OnHandshakeCompleted((P2PDirectProcessor)sender);
                    return;
                }

                // check if it's a direct connection handshake
                if (p2pMessage.V1Header.Flags == P2PFlag.DirectHandshake && AutoHandshake == true)
                {
                    // create a handshake message based on the incoming p2p message and send it
                    P2PDCHandshakeMessage dcHsMessage = new P2PDCHandshakeMessage(p2pMessage);
                    sender.SendMessage(dcHsMessage.CreateAcknowledgement());
                    OnHandshakeCompleted((P2PDirectProcessor)sender);
                    return;
                }

                if (p2pMessage.V1Header.Flags == P2PFlag.Error)
                {
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);
                    if (session != null)
                    {
                        session.AbortTransfer();
                    }

                    return;
                }

                // check if it is a content message
                if (p2pMessage.Header.SessionId > 0)
                {
                    // get the session to handle this message
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);

                    if (session != null)
                        session.HandleMessage(this, p2pMessage);

                    return;
                }
            }

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Keep track of the remote identifier
                RemoteIdentifier = p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize;

                // Check if it is a content message
                if (p2pMessage.InnerBody != null && p2pMessage.Header.SessionId > 0) //Data messages.
                {
                    // get the session to handle this message
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);

                    if (session != null)
                        session.HandleMessage(this, p2pMessage);

                    return;
                }
            }

            // It is not a datamessage. Extract the messages one-by-one and dispatch
            // it to all handlers. Usually the MSNSLP handler.
            IMessageHandler[] cpHandlers = handlers.ToArray();
            foreach (IMessageHandler handler in cpHandlers)
                handler.HandleMessage(this, p2pMessage);
        }

        #endregion

        #region IMessageProcessor Members

        private List<IMessageHandler> handlers = new List<IMessageHandler>();

        /// <summary>
        /// Registers a message handler. After registering the handler will receive incoming messages.
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            lock (handlers)
            {
                if (false == handlers.Contains(handler))
                {
                    handlers.Add(handler);
                }
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
            if (p2pMessage.Header.Identifier == 0)
            {
                if (Version == P2PVersion.P2PV1)
                {
                    IncreaseLocalIdentifier();
                    p2pMessage.Header.Identifier = LocalIdentifier;
                }

                if (Version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.Identifier = LocalIdentifier;
                    CorrectLocalIdentifier((int)p2pMessage.V2Header.MessageSize);
                }
            }

            if (Version == P2PVersion.P2PV1 && 0 == p2pMessage.V1Header.AckSessionId)
            {
                p2pMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }

            #region P2P Version 1
            // check whether we have a direct connection (send p2pdc messages) or not (send sb messages)
            int maxSize = DirectConnected ? 1352 : 1202;

            // split up large messages which go to the SB
            if (Version == P2PVersion.P2PV1)
            {
                if (p2pMessage.Header.MessageSize > maxSize)
                {

                    P2PMessage[] messages = p2pMessage.SplitMessage(maxSize);

                    foreach (P2PMessage chunkMessage in messages)
                    {
                        // now send it to propbably a SB processor
                        try
                        {
                            if (MessageProcessor != null &&
                                ((SocketMessageProcessor)MessageProcessor).Connected)
                            {
                                if (DirectConnected)
                                {
                                    P2PDCMessage dcChunkMessage = new P2PDCMessage(chunkMessage);
                                    MessageProcessor.SendMessage(dcChunkMessage);
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + dcChunkMessage.GetType().Name + ":\r\n" + dcChunkMessage.ToDebugString() + "\r\n", GetType().Name);
                                }
                                else
                                {
                                    // wrap the message before sending it to the (probably) SB processor
                                    MessageProcessor.SendMessage(WrapMessage(chunkMessage));
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + chunkMessage.GetType().Name + ":\r\n" + chunkMessage.ToDebugString() + "\r\n", GetType().Name);
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
                    } 

                }
                else
                {
                    try
                    {
                        if (MessageProcessor != null)
                        {
                            if (DirectConnected)
                            {
                                P2PDCMessage dcChunkMessage = new P2PDCMessage(p2pMessage);
                                MessageProcessor.SendMessage(dcChunkMessage);
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + dcChunkMessage.GetType().Name + ":\r\n" + dcChunkMessage.ToDebugString() + "\r\n", GetType().Name);
                            }
                            else
                            {
                                // wrap the message before sending it to the (probably) SB processor
                                MessageProcessor.SendMessage(WrapMessage(p2pMessage));
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + p2pMessage.GetType().Name + ":\r\n" + p2pMessage.ToDebugString() + "\r\n", GetType().Name);
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
            #endregion

            #region P2P Version 2
            if (Version == P2PVersion.P2PV2)
            {
                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > maxSize)
                {
                    CorrectLocalIdentifier(-(int)p2pMessage.V2Header.MessageSize);

                    P2PMessage[] messages = p2pMessage.SplitMessage(maxSize);

                    foreach (P2PMessage chunkMessage in messages)
                    {
                        //chunkMessage.V2Header.Identifier = LocalIdentifier;
                        LocalIdentifier = chunkMessage.V2Header.Identifier + chunkMessage.V2Header.MessageSize;
                        // CorrectLocalIdentifier((int)chunkMessage.V2Header.MessageSize);

                        // now send it to propbably a SB processor
                        try
                        {
                            if (MessageProcessor != null)
                            {
                                if (DirectConnected)
                                {
                                    P2PDCMessage dcChunkMessage = new P2PDCMessage(chunkMessage);
                                    MessageProcessor.SendMessage(dcChunkMessage);
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + dcChunkMessage.GetType().Name + ":\r\n" + dcChunkMessage.ToDebugString() + "\r\n", GetType().Name);
                                }
                                else
                                {
                                    // wrap the message before sending it to the (probably) SB processor
                                    MessageProcessor.SendMessage(WrapMessage(chunkMessage));
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + chunkMessage.GetType().Name + ":\r\n" + chunkMessage.ToDebugString() + "\r\n", GetType().Name);
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
                    }
                }
                else
                {
                    try
                    {
                        if (MessageProcessor != null)
                        {
                            if (DirectConnected)
                            {
                                P2PDCMessage dcChunkMessage = new P2PDCMessage(p2pMessage);
                                MessageProcessor.SendMessage(dcChunkMessage);

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + dcChunkMessage.GetType().Name + ":\r\n" + dcChunkMessage.ToDebugString() + "\r\n", GetType().Name);
                            }
                            else
                            {
                                // wrap the message before sending it to the (probably) SB processor
                                MessageProcessor.SendMessage(WrapMessage(p2pMessage));

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + p2pMessage.GetType().Name + ":\r\n" + p2pMessage.ToDebugString() + "\r\n", GetType().Name);
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
            #endregion
        }

        /// <summary>
        /// Occurs when the processor has been marked as invalid. Due to connection error, or message processor being null.
        /// </summary>
        public event EventHandler<EventArgs> ProcessorInvalid;

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
                    NetworkMessage p2pMessage = sendMessages.Dequeue() as NetworkMessage;

                    if (DirectConnected == true)
                    {
                        MessageProcessor.SendMessage(new P2PDCMessage(p2pMessage as P2PMessage));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + p2pMessage.GetType().Name + ":\r\n" + p2pMessage.ToDebugString() + "\r\n", GetType().Name);
                    }
                    else
                    {
                        MessageProcessor.SendMessage(WrapMessage(p2pMessage));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing " + p2pMessage.GetType().Name + ":\r\n" + p2pMessage.ToDebugString() + "\r\n", GetType().Name);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                InvalidateProcessor();
            }
        }

        

        #endregion
    }
};
