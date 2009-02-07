using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    public delegate void P2PAckHandler(P2PMessage ackMsg);

    public enum P2PSessionStatus
    {
        Error,
        WaitingForLocal,
        WaitingForRemote,
        Active,
        Closing,
        Closed
    }

    public partial class P2PSession : IDisposable, IMessageHandler, IMessageProcessor
    {
        static Random random = new Random();

        private uint localBaseIdentifier;
        private uint localIdentifier;
        private uint remoteBaseIdentifier;
        private uint remoteIdentifier;

        private Contact local;
        private Contact remote;
        private uint sessionId;
        private Timer timeoutTimer;
        private P2PApplication app;
        private MSNSLPMessage invite;
        private NSMessageHandler nsMessageHandler;
        private P2PSessionStatus status = P2PSessionStatus.Closed;

        /// <summary>
        /// This is the processor used before a direct connection. Usually a SB processor.
        /// It is a fallback variables in case a direct connection fails.
        /// </summary>
        private IMessageProcessor preDCProcessor;

        /// <summary>
        /// A collection of all transfersessions
        /// </summary>
        private Hashtable transferSessions = new Hashtable();
        
        public event EventHandler<EventArgs> Error;
        public event EventHandler<EventArgs> Activated;
        public event EventHandler<ContactEventArgs> Closing;
        public event EventHandler<ContactEventArgs> Closed;        
        public event EventHandler<ContactEventArgs> Waiting;

        public P2PApplication Application
        {
            get
            {
                return app;
            }
        }

        public P2PSessionStatus Status
        {
            get
            {
                return status;
            }
        }

        public MSNSLPMessage Invite
        {
            get
            {
                return invite;
            }
        }

        public uint SessionId
        {
            get
            {
                return sessionId;
            }
        }        

        public Contact Local
        {
            get
            {
                return local;
            }
        }
        
        public Contact Remote
        {
            get
            {
                return remote;
            }
        }

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
        /// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            System.Diagnostics.Debug.Assert(p2pMessage != null, "Incoming message is not a P2PMessage", "");
            ResetTimeoutTimer();

            // check whether it is an acknowledgement to data preparation message
            if (p2pMessage.Flags == P2PFlag.DirectHandshake && DCHandshakeAck != 0)
            {
                OnHandshakeCompleted((P2PDirectProcessor)sender);
                return;
            }

            // check if it's a direct connection handshake
            if (p2pMessage.Flags == P2PFlag.DirectHandshake && AutoHandshake)
            {
                // create a handshake message based on the incoming p2p message and send it				
                P2PDCHandshakeMessage dcHsMessage = new P2PDCHandshakeMessage(p2pMessage);
                sender.SendMessage(dcHsMessage.CreateAcknowledgement());
                OnHandshakeCompleted((P2PDirectProcessor)sender);
                return;
            }

            if (p2pMessage.Flags == P2PFlag.Error)
            {
                P2PTransferSession session = (P2PTransferSession)transferSessions[p2pMessage.SessionId];
                if (session != null)
                {
                    session.AbortTransfer();
                }

                return;
            }



            if ((p2pMessage.Flags & P2PFlag.Waiting) == P2PFlag.Waiting)
            {
                return;
            }

            remoteIdentifier = p2pMessage.Identifier;

            if (p2pMessage.InnerMessage != null)
            {
                MSNSLPMessage slp = p2pMessage.InnerMessage as MSNSLPMessage;
                bool isStatusMessage = slp.StartLine.StartsWith("MSNSLP/1.0");

                if (!isStatusMessage)
                {
                    SendMessage(p2pMessage.CreateAcknowledgement());

                    if (slp.StartLine.Contains("200"))
                    {
                        if (slp.ContentType == "application/x-msnmsgr-transrespbody")
                        {
                            //XXX TODO ProcessDirectInvite(msg);
                        }
                        else
                        {
                            OnActive(EventArgs.Empty);
                            app.Start();
                        }
                        return;
                    }
                    else if (slp.StartLine.Contains("603")) // Decline
                    {
                        OnClosed(new ContactEventArgs(Remote));
                        return;
                    }
                }
                else if (isStatusMessage)
                {
                    if ((slp.ContentType == "application/x-msnmsgr-sessionclosebody") && (slp.StartLine.Contains("BYE")))
                    {
                        P2PMessage byeAck = p2pMessage.CreateAcknowledgement();
                        byeAck.Flags = P2PFlag.CloseSession;
                        SendMessage(byeAck);

                        OnClosed(new ContactEventArgs(Remote));

                        return;
                    }
                    else
                    {
                        SendMessage(p2pMessage.CreateAcknowledgement());

                        if ((slp.ContentType == "application/x-msnmsgr-transreqbody") || (slp.ContentType == "application/x-msnmsgr-transrespbody"))
                        {
                            // Direct connection invite XXX TODO
                            // ProcessDirectInvite(msg);
                            return;
                        }
                        else if (slp.StartLine.Contains("ACK"))
                            return;
                    }
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Unhandled SLP Message:\r\n{0}", p2pMessage.ToDebugString()), GetType().Name);
                    OnError(EventArgs.Empty);
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
            SendMessage((P2PMessage)message, null);
        }


        public void SendMessage(P2PMessage p2pMessage, P2PAckHandler ackHandler)
        {
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
            
            ResetTimeoutTimer();
            P2PTransfers.RegisterP2PAckHandler(p2pMessage, ackHandler);

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



        #endregion

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
            if (session != null)
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

        private P2PMessagePool p2pMessagePool = new P2PMessagePool();

        /// <summary>
        /// Wraps a P2PMessage in a MSGMessage and SBMessage.
        /// </summary>
        /// <returns></returns>
        protected SBMessage WrapMessage(NetworkMessage networkMessage)
        {
            // create wrapper messages
            MSGMessage msgWrapper = new MSGMessage();
            msgWrapper.MimeHeader["P2P-Dest"] = Remote.Mail;
#if MSNC9
            //msgWrapper.MimeHeader["P2P-Src"] = LocalContact;
#endif
            msgWrapper.MimeHeader["Content-Type"] = "application/x-msnmsgrp2p";
            msgWrapper.InnerMessage = networkMessage;

            SBMessage sbMessageWrapper = new SBMessage();
            sbMessageWrapper.InnerMessage = msgWrapper;

            return sbMessageWrapper;
        }

        
        /// <summary>
        /// We are sender
        /// </summary>
        /// <param name="app"></param>
        public P2PSession(P2PApplication app)
        {
            this.app = app;
            local = app.Local;
            remote = app.Remote;
            nsMessageHandler = app.Local.NSMessageHandler;

            sessionId = (uint)random.Next(50000, int.MaxValue);
            localBaseIdentifier = (uint)random.Next(50000, int.MaxValue);
            localIdentifier = localBaseIdentifier;

            app.Session = this;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} created (locally)", SessionId), GetType().Name);

            MSNSLPMessage slpMessage = new MSNSLPMessage();
            slpMessage.StartLine = "INVITE MSNMSGR:" + remote.Mail + " MSNSLP/1.0";
            slpMessage.To = "<msnmsgr:" + remote.Mail + ">";
            slpMessage.From = "<msnmsgr:" + local.Mail + ">";
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";

            slpMessage.BodyValues["EUF-GUID"] = app.ApplicationEufGuid.ToString("B").ToUpperInvariant();
            slpMessage.BodyValues["SessionID"] = SessionId.ToString();
            slpMessage.BodyValues["SChannelState"] = "0";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["AppID"] = app.ApplicationId.ToString();
            slpMessage.BodyValues["Context"] = app.InvitationContext;

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.Flags = P2PFlag.MSNSLPInfo;
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            OnWaiting(new ContactEventArgs(remote));

            SendMessage(p2pMessage, delegate(P2PMessage ack)
            {
                RemoteBaseIdentifier = ack.Identifier;
                RemoteIdentifier = RemoteBaseIdentifier;
            });
        }

        /// <summary>
        /// We are receiver
        /// </summary>
        /// <param name="invite"></param>
        /// <param name="msg"></param>
        public P2PSession(MSNSLPMessage invite, P2PMessage msg)
        {
            this.invite = invite;
            nsMessageHandler = invite.NSMessageHandler;
            local = (invite.ToMail == nsMessageHandler.Owner.Mail) ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContact(invite.ToMail, ClientType.PassportMember);
            remote = nsMessageHandler.ContactList.GetContact(invite.FromMail, ClientType.PassportMember);

            localBaseIdentifier = (uint)random.Next(50000, int.MaxValue);
            localIdentifier = localBaseIdentifier;
            remoteBaseIdentifier = (msg == null) ? 0 : msg.Identifier;
            remoteIdentifier = remoteBaseIdentifier;

            uint.TryParse(invite.BodyValues["SessionID"].Value, out sessionId);
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} created (remotely)", SessionId), GetType().Name);

            // Send Base ID
            if (msg != null)
            {
                SendMessage(msg.CreateAcknowledgement(), delegate(P2PMessage ack)
                {
                    Debug.Assert(ack.Flags == P2PFlag.Acknowledgement, "not ACK");
                });
            }


            Type appType = P2PApplications.GetApplication(new Guid(invite.BodyValues["EUF-GUID"].Value), uint.Parse(invite.BodyValues["AppID"].Value));

            if (appType == null)
            {

                return;
            }

            app = Activator.CreateInstance(appType, this) as P2PApplication;

            if (app.ValidateInvitation(invite))
            {
                status = P2PSessionStatus.WaitingForLocal;

                if (app.AutoAccept)
                    Accept();
                else
                    OnWaiting(new ContactEventArgs(local));
            }
            else
            {
                OnError(EventArgs.Empty);

                MSNSLPMessage slpMessage = new MSNSLPMessage();
                slpMessage.StartLine = "MSNSLP/1.0 500 Internal Error";
                slpMessage.To = "<msnmsgr:" + remote.Mail + ">";
                slpMessage.From = "<msnmsgr:" + local.Mail + ">";
                slpMessage.Branch = invite.Branch;
                slpMessage.CallId = invite.CallId;

                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                slpMessage.BodyValues["SChannelState"] = "0";

                P2PMessage p2pMessage = new P2PMessage();
                p2pMessage.Flags = P2PFlag.MSNSLPInfo;
                p2pMessage.InnerMessage = slpMessage;
                p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                SendMessage(p2pMessage, delegate
                {
                    Close();
                });
            }
        }






        public void Accept()
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Accept called, but we're not waiting for the local client (State {0})", status), GetType().Name);
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} accepted", SessionId), GetType().Name);

            MSNSLPMessage slpMessage = new MSNSLPMessage();
            slpMessage.StartLine = "MSNSLP/1.0 200 OK";
            slpMessage.To = "<msnmsgr:" + remote.Mail + ">";
            slpMessage.From = "<msnmsgr:" + local.Mail + ">";
            slpMessage.Branch = invite.Branch;
            slpMessage.CSeq = 1;
            slpMessage.CallId = invite.CallId;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.Flags = P2PFlag.MSNSLPInfo;
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            SendMessage(p2pMessage, delegate
            {
                OnActive(EventArgs.Empty);

                if (app != null)
                {
                    app.Start();
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unable to start p2p application (null)", GetType().Name);
                }
            });
        }

        public void Decline()
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Declined called, but we're not waiting for the local client (State {0})", status), GetType().Name);
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} declined", SessionId), GetType().Name);

            MSNSLPMessage slpMessage = new MSNSLPMessage();
            slpMessage.StartLine = "MSNSLP/1.0 603 Decline";
            slpMessage.To = "<msnmsgr:" + remote.Mail + ">";
            slpMessage.From = "<msnmsgr:" + local.Mail + ">";
            slpMessage.Branch = invite.Branch;
            slpMessage.CSeq = 1;
            slpMessage.CallId = invite.CallId;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.Flags = P2PFlag.MSNSLPInfo;
            p2pMessage.InnerMessage = slpMessage;
            p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

            SendMessage(p2pMessage, delegate
            {
                Close();
            });
        }

        public void Close()
        {
            if (status == P2PSessionStatus.Closing)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("P2PSession {0} was already closing, forcing unclean closure", SessionId), GetType().Name);
                OnClosed(new ContactEventArgs(Local));
            }
            else
            {
                OnClosing(new ContactEventArgs(Local));

                MSNSLPMessage slpMessage = new MSNSLPMessage();
                slpMessage.StartLine = "BYE MSNMSGR:" + remote.Mail + " MSNSLP/1.0";
                slpMessage.To = "<msnmsgr:" + remote.Mail + ">";
                slpMessage.From = "<msnmsgr:" + local.Mail + ">";
                slpMessage.Branch = invite.Branch;
                slpMessage.CallId = invite.CallId;
                slpMessage.MaxForwards = 0;
                slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";

                P2PMessage p2pMessage = new P2PMessage();
                p2pMessage.Flags = P2PFlag.MSNSLPInfo;
                p2pMessage.InnerMessage = slpMessage;
                p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                SendMessage(p2pMessage, delegate
                {
                    OnClosed(new ContactEventArgs(Local));
                });
            }
        }


        public void Dispose()
        {
            CleanUp();
            DisposeApp();
        }



        protected virtual void OnClosing(ContactEventArgs e)
        {
            status = P2PSessionStatus.Closing;

            if (Closing != null)
                Closing(this, e);

            DisposeApp();
        }

        protected virtual void OnClosed(ContactEventArgs e)
        {
            status = P2PSessionStatus.Closed;

            if (timeoutTimer != null)
            {
                timeoutTimer.Dispose();
                timeoutTimer = null;
            }

            if (Closed != null)
                Closed(this, e);

            DisposeApp();
        }

        protected virtual void OnError(EventArgs e)
        {
            status = P2PSessionStatus.Error;

            if (Error != null)
                Error(this, e);

            DisposeApp();
        }

        protected virtual void OnWaiting(ContactEventArgs e)
        {
            status = (e.Contact == Local) ? P2PSessionStatus.WaitingForLocal : P2PSessionStatus.WaitingForRemote;

            if (Waiting != null)
                Waiting(this, e);
        }

        protected virtual void OnActive(EventArgs e)
        {

            status = P2PSessionStatus.Active;

            if (Activated != null)
                Activated(this, e);
        }

        private void DisposeApp()
        {
            if (app != null)
            {
                app.Dispose();
                app = null;
            }
        }

        
        const int timeout = 120000;
        private void ResetTimeoutTimer()
        {
            if (timeoutTimer != null)
            {
                timeoutTimer.Change(timeout, timeout);
                return;
            }

            timeoutTimer = new Timer(new TimerCallback(InactivityClose), this, timeout, timeout);
        }

        private void InactivityClose(object state)
        {
            Close();
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("P2PSession {0} timed out through inactivity", SessionId), GetType().Name);
        }


    }
};
