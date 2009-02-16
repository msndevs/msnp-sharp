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

        #region Events

        public event EventHandler<EventArgs> Error;
        public event EventHandler<EventArgs> Activated;
        public event EventHandler<ContactEventArgs> Closing;
        public event EventHandler<ContactEventArgs> Closed;
        public event EventHandler<ContactEventArgs> Waiting;

        /// <summary>
        /// Occurs when the processor has been marked as invalid.
        /// Due to connection error, or message processor being null.
        /// </summary>
        public event EventHandler<EventArgs> ProcessorInvalid;

        #endregion

        #region Members

        private uint localBaseIdentifier;
        private uint localIdentifier;
        private uint remoteBaseIdentifier;
        private uint remoteIdentifier;
        private uint sessionId;
        private Contact local;
        private Contact remote;
        private SLPMessage invite;
        private P2PApplication p2pApplication;
        private NSMessageHandler nsMessageHandler;
        private IMessageProcessor messageProcessor; // SB or DC
        private IMessageProcessor preDCProcessor; // Processor used before a DC. Usually a SB processor. It is a fallback variables in case a direct connection fails.
        private P2PSessionStatus status = P2PSessionStatus.Closed;

        #endregion

        #region Properties

        /// <summary>
        /// The base identifier of the local client
        /// </summary>
        public uint LocalBaseIdentifier
        {
            get
            {
                return localBaseIdentifier;
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
        }

        /// <summary>
        /// Session ID
        /// </summary>
        public uint SessionId
        {
            get
            {
                return sessionId;
            }
        }

        /// <summary>
        /// Local contact
        /// </summary>
        public Contact Local
        {
            get
            {
                return local;
            }
        }

        /// <summary>
        /// Remote contact
        /// </summary>
        public Contact Remote
        {
            get
            {
                return remote;
            }
        }

        /// <summary>
        /// SLP invitation
        /// </summary>
        public SLPMessage Invite
        {
            get
            {
                return invite;
            }
        }

        /// <summary>
        /// P2P Application handling incoming data messages
        /// </summary>
        public P2PApplication Application
        {
            get
            {
                return p2pApplication;
            }
        }

        /// <summary>
        /// Session state
        /// </summary>
        public P2PSessionStatus Status
        {
            get
            {
                return status;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

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

                if (value != null && value.GetType() != typeof(NSMessageProcessor))
                {
                    ValidateProcessor();
                    SendBuffer();
                }
            }
        }

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// We are sender. Sends invitation message automatically and sets remote identifiers after ack received.
        /// </summary>
        public P2PSession(P2PApplication app)
        {
            this.p2pApplication = app;
            this.ProcessorInvalid += P2PSession_ProcessorInvalid;

            local = app.Local;
            remote = app.Remote;
            nsMessageHandler = app.Local.NSMessageHandler;

            sessionId = (uint)random.Next(50000, int.MaxValue);
            localBaseIdentifier = (uint)random.Next(50000, int.MaxValue);
            localIdentifier = localBaseIdentifier;

            app.Session = this;


            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} created (Initiated locally)", SessionId), GetType().Name);

            invite = new SLPRequestMessage(remote.Mail, "INVITE");
            invite.ToMail = remote.Mail;
            invite.FromMail = local.Mail;
            invite.ContentType = "application/x-msnmsgr-sessionreqbody";
            invite.BodyValues["EUF-GUID"] = app.ApplicationEufGuid.ToString("B").ToUpperInvariant();
            invite.BodyValues["AppID"] = app.ApplicationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            invite.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            invite.BodyValues["Context"] = app.InvitationContext;
            invite.BodyValues["SChannelState"] = "0";
            //invite.BodyValues["Capabilities-Flags"] = "1";

            P2PMessage p2pMessage = new P2PMessage();
            p2pMessage.Flags = (remote.ClientCapacities > ClientCapacities.CanHandleMSNC8) ? P2PFlag.MSNSLPInfo : P2PFlag.Normal;
            p2pMessage.InnerMessage = invite;
            p2pMessage.MessageSize = (uint)invite.GetBytes().Length;

            OnWaiting(new ContactEventArgs(Remote));

            SendMessage(p2pMessage, delegate(P2PMessage ack)
            {
                remoteBaseIdentifier = ack.Identifier;
                remoteIdentifier = RemoteBaseIdentifier;
            });
        }

        /// <summary>
        /// We are receiver.
        /// </summary>
        public P2PSession(SLPMessage invite, P2PMessage msg, NSMessageHandler nsMessageHandler)
        {
            this.invite = invite;
            this.nsMessageHandler = nsMessageHandler;
            this.ProcessorInvalid += P2PSession_ProcessorInvalid;

            local = (invite.ToMail == nsMessageHandler.Owner.Mail) ? nsMessageHandler.Owner : nsMessageHandler.ContactList.GetContact(invite.ToMail, ClientType.PassportMember);
            remote = nsMessageHandler.ContactList.GetContact(invite.FromMail, ClientType.PassportMember);

            localBaseIdentifier = (uint)random.Next(50000, int.MaxValue);
            localIdentifier = localBaseIdentifier;
            remoteBaseIdentifier = (msg == null) ? 0 : msg.Identifier;
            remoteIdentifier = remoteBaseIdentifier;

            if (!uint.TryParse(invite.BodyValues["SessionID"].Value, out sessionId))
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Can't parse SessionID: " + SessionId, GetType().Name);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} created (Initiated remotely)", SessionId), GetType().Name);

            // Send Base ID
            if (msg != null)
            {
                SendMessage(msg.CreateAcknowledgement(), delegate
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("BaseID {0} sent for session", SessionId), GetType().Name);
                });
            }

            // Create application based on invitation
            uint appId = uint.Parse(invite.BodyValues["AppID"].Value);
            Guid eufGuid = new Guid(invite.BodyValues["EUF-GUID"].Value);
            Type appType = P2PApplication.GetApplication(eufGuid, appId);
            if (appType == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Unknown app for EUF-GUID: {0}, AppID: {1}", eufGuid, appId), GetType().Name);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("[SLPMessage]\r\n{1}", invite.ToString()), GetType().Name);
            }
            else
            {
                p2pApplication = Activator.CreateInstance(appType, this) as P2PApplication;

                if (p2pApplication.ValidateInvitation(invite))
                {
                    status = P2PSessionStatus.WaitingForLocal;

                    if (p2pApplication.AutoAccept)
                        Accept();
                    else
                        OnWaiting(new ContactEventArgs(Local));
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} app rejects invite\r\n{1}", SessionId, Invite), GetType().Name);

                    OnError(EventArgs.Empty);

                    SLPStatusMessage slpMessage = new SLPStatusMessage(remote.Mail, 500, "Internal Error");
                    slpMessage.ToMail = remote.Mail;
                    slpMessage.FromMail = local.Mail;
                    slpMessage.Branch = invite.Branch;
                    slpMessage.CallId = invite.CallId;
                    slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                    slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    slpMessage.BodyValues["SChannelState"] = "0";

                    P2PMessage p2pMessage = new P2PMessage();
                    p2pMessage.Flags = (remote.ClientCapacities > ClientCapacities.CanHandleMSNC8) ? P2PFlag.MSNSLPInfo : P2PFlag.Normal;
                    p2pMessage.InnerMessage = slpMessage;
                    p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                    SendMessage(p2pMessage, delegate
                    {
                        Close();
                    });
                }
            }
        }

        #endregion

        #region Public Methods

        public void Accept()
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Accept called, but we're not waiting for the local client (State {0})", status), GetType().Name);
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} accepted", SessionId), GetType().Name);

                SLPStatusMessage slpMessage = new SLPStatusMessage(remote.Mail, 200, "OK");
                slpMessage.ToMail = remote.Mail;
                slpMessage.FromMail = local.Mail;
                slpMessage.Branch = invite.Branch;
                slpMessage.CSeq = 1;
                slpMessage.CallId = invite.CallId;
                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                P2PMessage p2pMessage = new P2PMessage();
                p2pMessage.Flags = (remote.ClientCapacities > ClientCapacities.CanHandleMSNC8) ? P2PFlag.MSNSLPInfo : P2PFlag.Normal;
                p2pMessage.InnerMessage = slpMessage;
                p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                SendMessage(p2pMessage, delegate
                {
                    OnActive(EventArgs.Empty);

                    if (p2pApplication != null)
                    {
                        p2pApplication.Start();
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unable to start p2p application (null)", GetType().Name);
                    }
                });
            }
        }

        public void Decline()
        {
            if (status != P2PSessionStatus.WaitingForLocal)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Declined called, but we're not waiting for the local client (State {0})", status), GetType().Name);
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("{0} declined", SessionId), GetType().Name);

                SLPStatusMessage slpMessage = new SLPStatusMessage(remote.Mail, 603, "Decline");
                slpMessage.ToMail = remote.Mail;
                slpMessage.FromMail = local.Mail;
                slpMessage.Branch = invite.Branch;
                slpMessage.CSeq = 1;
                slpMessage.CallId = invite.CallId;
                slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                slpMessage.BodyValues["SessionID"] = SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                P2PMessage p2pMessage = new P2PMessage();
                p2pMessage.Flags = (remote.ClientCapacities > ClientCapacities.CanHandleMSNC8) ? P2PFlag.MSNSLPInfo : P2PFlag.Normal;
                p2pMessage.InnerMessage = slpMessage;
                p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                SendMessage(p2pMessage, delegate
                {
                    Close();
                });
            }
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

                SLPRequestMessage slpMessage = new SLPRequestMessage(remote.Mail, "BYE");
                slpMessage.ToMail = remote.Mail;
                slpMessage.FromMail = local.Mail;
                slpMessage.Branch = invite.Branch;
                slpMessage.CallId = invite.CallId;
                slpMessage.MaxForwards = 0;
                slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";

                P2PMessage p2pMessage = new P2PMessage();
                p2pMessage.Flags = (remote.ClientCapacities > ClientCapacities.CanHandleMSNC8) ? P2PFlag.MSNSLPInfo : P2PFlag.Normal;
                p2pMessage.InnerMessage = slpMessage;
                p2pMessage.MessageSize = (uint)slpMessage.GetBytes().Length;

                SendMessage(p2pMessage, delegate
                {
                    OnClosed(new ContactEventArgs(Local));
                });
            }
        }

        /// <summary>
        /// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;
            Debug.Assert(p2pMessage != null, "Incoming message is not a P2PMessage");

            ResetTimeoutTimer();

            remoteIdentifier = p2pMessage.Identifier;

            // Waiting for what?
            if ((p2pMessage.Flags & P2PFlag.Waiting) == P2PFlag.Waiting)
            {
                return;
            }

            if (status == P2PSessionStatus.Closed || status == P2PSessionStatus.Error)
            {
                return;
            }

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
                return;
            }

            // Check if it is a content message
            if (p2pMessage.SessionId > 0)
            {
                if (p2pApplication == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("P2PSession {0}: Received message for P2P app, but it's either been disposed or not created", SessionId), GetType().Name);
                }
                else
                {
                    p2pApplication.HandleMessage(this, p2pMessage);
                }
                return;
            }


            // Check slp
            SLPMessage slp = SLPMessage.Parse(p2pMessage.InnerBody);
            if (slp != null)
            {
                if (slp is SLPRequestMessage)
                {
                    SLPRequestMessage req = slp as SLPRequestMessage;

                    if ((req.ContentType == "application/x-msnmsgr-sessionclosebody") && (req.Method == "BYE"))
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

                        if ((req.ContentType == "application/x-msnmsgr-transreqbody")
                            || (req.ContentType == "application/x-msnmsgr-transrespbody"))
                        {
                            // Direct connection invite
                            OnDCRequest(slp);
                            return;
                        }
                        else if (req.Method == "ACK")
                            return;
                    }
                }
                else if (slp is SLPStatusMessage)
                {
                    SLPStatusMessage sta = slp as SLPStatusMessage;

                    SendMessage(p2pMessage.CreateAcknowledgement());

                    if (sta.Code == 200) // OK
                    {
                        if (sta.ContentType == "application/x-msnmsgr-transrespbody")
                            OnDCResponse(sta);
                        else
                        {
                            OnActive(EventArgs.Empty);
                            p2pApplication.Start();
                        }
                        return;
                    }
                    else if (sta.Code == 603) // Decline
                    {
                        OnClosed(new ContactEventArgs(Remote));
                        return;
                    }
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Unhandled SLP Message:\r\n{0}", p2pMessage.ToString()), GetType().Name);
                OnError(EventArgs.Empty);
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
        /// <param name="p2pMessage">The P2PMessage to send to the remote contact.</param>
        public void SendMessage(NetworkMessage p2pMessage)
        {
            SendMessage((P2PMessage)p2pMessage, null);
        }


        public void SendMessage(P2PMessage p2pMessage, P2PAckHandler ackHandler)
        {
            ResetTimeoutTimer();

            p2pMessage.SessionId = SessionId;

            if (p2pMessage.Identifier == 0)
            {
                IncreaseLocalIdentifier();
                p2pMessage.Identifier = LocalIdentifier;
            }

            if (p2pMessage.AckSessionId == 0)
            {
                p2pMessage.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }

            NSMessageHandler.Messenger.P2PHandler.RegisterP2PAckHandler(p2pMessage, ackHandler);

            int maxSize = DirectConnected ? 1352 : 1202;
            P2PMessage[] chunks = p2pMessage.SplitMessage(maxSize);

            foreach (P2PMessage chunk in chunks)
            {
                // now send it to propbably a SB processor
                try
                {
                    if (MessageProcessor != null && ((SocketMessageProcessor)MessageProcessor).Connected)
                    {
                        if (DirectConnected)
                        {
                            MessageProcessor.SendMessage(new P2PDCMessage(chunk));
                        }
                        else
                        {
                            // wrap the message before sending it to the (probably) SB processor
                            MessageProcessor.SendMessage(WrapMessage(chunk));
                        }
                    }
                    else
                    {
                        InvalidateProcessor();
                        BufferMessage(chunk);
                    }
                }
                catch (System.Net.Sockets.SocketException)
                {
                    InvalidateProcessor();
                    BufferMessage(chunk);
                }
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
            if (p2pApplication != null)
            {
                p2pApplication.Dispose();
                p2pApplication = null;
            }
        }


        const int timeout = 120000;
        private Timer timeoutTimer;
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
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("Remain ackhandler count: {0}", NSMessageHandler.Messenger.P2PHandler.ackHandlers.Count), GetType().Name);
        }

        private void P2PSession_ProcessorInvalid(object sender, EventArgs e)
        {
            P2PSession session = (P2PSession)sender;
            SBMessageHandler sbHandler = session.NSMessageHandler.Messenger.P2PHandler.GetSwitchboardSession(session.Remote.Mail);

            // if the contact is offline, there is no need to request a new switchboard. close the session.
            if (PresenceStatus.Offline == NSMessageHandler.ContactList[session.Remote.Mail, ClientType.PassportMember].Status)
            {
                session.Close();
                return;
            }

            // check whether the switchboard handler is valid and has a valid processor.
            // if that is the case, use that processor. Otherwise request a new session.
            if (sbHandler == null || sbHandler.MessageProcessor == null ||
                ((SocketMessageProcessor)sbHandler.MessageProcessor).Connected == false ||
                (sbHandler.Contacts.ContainsKey(session.Remote.Mail) == false))
            {
                sbHandler = session.NSMessageHandler.Messenger.P2PHandler.RequestSwitchboard(session.Remote.Mail);
            }
            else
            {
                session.MessageProcessor = sbHandler.MessageProcessor;
            }
        }

        #endregion














        /// <summary>
        /// A collection of all transfersessions
        /// </summary>
        private Hashtable transferSessions = new Hashtable();
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
            if (processorValid)
            {
                processorValid = false;
                OnProcessorInvalid();
            }
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
        public SBMessage WrapMessage(NetworkMessage networkMessage)
        {
            // create wrapper messages
            MSGMessage msgWrapper = new MSGMessage();
            msgWrapper.MimeHeader["P2P-Dest"] = Remote.Mail;
#if MSNC12
            //msgWrapper.MimeHeader["P2P-Src"] = LocalContact;
#endif
            msgWrapper.MimeHeader["Content-Type"] = "application/x-msnmsgrp2p";
            msgWrapper.InnerMessage = networkMessage;

            SBMessage sbMessageWrapper = new SBMessage();
            sbMessageWrapper.InnerMessage = msgWrapper;

            return sbMessageWrapper;
        }












    }
};
