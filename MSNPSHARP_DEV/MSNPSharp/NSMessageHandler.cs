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
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.P2P;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNDirectoryService;

    /// <summary>
    /// Handles the protocol messages from the notification server
    /// and implements protocol version MSNP21.
    /// </summary>
    public partial class NSMessageHandler : IMessageHandler
    {
        /// <summary>
        /// Machine Guid
        /// </summary>
        public static readonly Guid MachineGuid = Guid.NewGuid();

        #region AdlState

        private class AdlState
        {
            /// <summary>
            /// The indicator of whether the initial contact ADL has been processed.
            /// If the contact ADL was not processed, ignore the circle ADL.
            /// </summary>
            internal bool ContactADLProcessed;
            internal int ServiceADL;
            internal Scenario IgnoredSenario;
            internal Dictionary<int, NSPayLoadMessage> InitialADLs = new Dictionary<int, NSPayLoadMessage>();

            internal AdlState()
            {
                Reset();
            }

            internal void Reset()
            {
                lock (this)
                {
                    ContactADLProcessed = false;
                    ServiceADL = 0;
                    IgnoredSenario = Scenario.None;

                    lock (InitialADLs)
                        InitialADLs.Clear();
                }
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when the message processor of the nameserver changed.
        /// </summary>
        public event EventHandler<MessageProcessorChangedEventArgs> MessageProcessorChanged;
        protected virtual void OnMessageProcessorChanged(MessageProcessorChangedEventArgs e)
        {
            if (MessageProcessorChanged != null)
                MessageProcessorChanged(this, e);
        }

        /// <summary>
        /// Occurs when the user finished the authentication step, the owner was created.
        /// </summary>
        public event EventHandler<EventArgs> OwnerVerified;
        protected virtual void OnOwnerVerified(EventArgs e)
        {
            if (OwnerVerified != null)
                OwnerVerified(this, e);
        }

        /// <summary>
        /// Occurs when the authentication and authorization with the server has finished.
        /// The client is now connected to the messenger network.
        /// </summary>
        public event EventHandler<EventArgs> SignedIn;
        protected virtual void OnSignedIn(EventArgs e)
        {
            isSignedIn = true;

            Owner.EndPointData[NSMessageHandler.MachineGuid] = new PrivateEndPointData(Owner.Account, NSMessageHandler.MachineGuid);

            if (ContactService.Deltas != null)
                Owner.SyncProfileToDeltas();

            if (SignedIn != null)
                SignedIn(this, e);
        }

        /// <summary>
        /// Occurs when the message processor has disconnected, and thus the user is no longer signed in.
        /// </summary>
        public event EventHandler<SignedOffEventArgs> SignedOff;
        protected virtual void OnSignedOff(SignedOffEventArgs e)
        {
            if (Owner != null)
                Owner.SetStatus(PresenceStatus.Offline);

            Clear();

            if (messageProcessor != null)
                messageProcessor.Disconnect();

            if (SignedOff != null)
                SignedOff(this, e);
        }

        /// <summary>
        /// Occurs when the server notifies the client with the status of the owner's mailbox.
        /// </summary>
        public event EventHandler<MailboxStatusEventArgs> MailboxStatusReceived;
        protected virtual void OnMailboxStatusReceived(MailboxStatusEventArgs e)
        {
            if (MailboxStatusReceived != null)
            {
                MailboxStatusReceived(this, e);
            }
        }

        /// <summary>
        /// Occurs when the owner has received new e-mail, or e-mail has been removed / moved.
        /// </summary>
        public event EventHandler<NewMailEventArgs> NewMailReceived;
        protected virtual void OnNewMailReceived(NewMailEventArgs e)
        {
            if (NewMailReceived != null)
            {
                NewMailReceived(this, e);
            }
        }

        /// <summary>
        /// Occurs when unread mail is read or mail is moved.
        /// </summary>
        public event EventHandler<MailChangedEventArgs> MailboxChanged;
        protected virtual void OnMailboxChanged(MailChangedEventArgs e)
        {
            if (MailboxChanged != null)
            {
                MailboxChanged(this, e);
            }
        }

        /// <summary>
        /// Occurs when the server sends an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;
        protected virtual void OnServerErrorReceived(MSNErrorEventArgs e)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, e);
        }

        /// <summary>
        /// Occurs when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;
        protected virtual void OnExceptionOccurred(ExceptionEventArgs e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Exception.Message + "\r\n\r\nStackTrace: \r\n" + e.Exception.StackTrace + "\r\n\r\n", GetType().Name);
        }

        /// <summary>
        /// Occurs when the user could not be signed in due to authentication errors. Most likely due
        /// to an invalid account or password. Note that this event will also raise the more general
        /// <see cref="ExceptionOccurred"/> event.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> AuthenticationError;
        protected virtual void OnAuthenticationError(ExceptionEventArgs e)
        {
            if (AuthenticationError != null)
                AuthenticationError(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString(), GetType().Name);
        }

        #endregion

        #region Members

        private Credentials credentials = new Credentials(MsnProtocol.MSNP21);
        private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private NSMessageProcessor messageProcessor;
        private IPEndPoint externalEndPoint;
        private P2PHandler p2pHandler;
        private int hopCount = 0;


        private ContactGroupList contactGroups;
        private ContactList contactList;
        private ContactManager contactManager;
        private MessageManager messageManager;
        private bool autoSynchronize = true;
        private bool botMode = false;

        private bool isSignedIn = false;
        private MSNTicket msnTicket = MSNTicket.Empty;
        private AdlState adlState = new AdlState();
        private System.Timers.Timer pong = null;

        private ContactService contactService;
        private MSNStorageService storageService;
        private WhatsUpService whatsUpService;
        private MSNDirectoryService dirService;

        private List<Regex> censorWords = new List<Regex>(0);
        private Dictionary<int, MultipartyObject> multiParties = new Dictionary<int, MultipartyObject>();

        protected internal NSMessageHandler()
        {
            contactGroups = new ContactGroupList(this);
            contactList = new ContactList(this);
            contactManager = new ContactManager(this);
            messageManager = new MessageManager(this);

            contactService = new ContactService(this);
            storageService = new MSNStorageService(this);
            whatsUpService = new WhatsUpService(this);
            dirService = new MSNDirectoryService(this);

            p2pHandler = new P2PHandler(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The library runs as a bot. Not to auto synchronize the addressbook when login.
        /// </summary>
        public bool BotMode
        {
            get
            {
                return botMode;
            }
            set
            {
                botMode = value;
                AutoSynchronize = !value;
            }
        }

        public bool AutoSynchronize
        {
            get
            {
                return autoSynchronize;
            }
            set
            {
                autoSynchronize = value;
            }
        }

        /// <summary>
        /// Defines whether the user is signed in the messenger network
        /// </summary>
        public bool IsSignedIn
        {
            get
            {
                return isSignedIn;
            }
        }

        /// <summary>
        /// Keep track whether a address book synchronization has been completed so we can warn the client programmer
        /// </summary>
        public bool AddressBookSynchronized
        {
            get
            {
                return ContactService.AddressBookSynchronized;
            }
        }

        /// <summary>
        /// The end point as perceived by the server. This is set after the owner's profile is received.
        /// </summary>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                return externalEndPoint;
            }
        }

        /// <summary>
        /// The client's local end-point. This can differ from the external endpoint through the use of
        /// routers. This value is used to determine how to set-up a direct connection.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                IPAddress[] addrList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

                for (int i = 0; i < addrList.Length; i++)
                {
                    if (addrList[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localEndPoint.Address = addrList[i];
                        break;
                    }
                }
                return localEndPoint;
            }
        }

        /// <summary>
        /// A collection of all contacts which are on any of the lists of the person who logged into the messenger network
        /// </summary>
        public ContactList ContactList
        {
            get
            {
                return contactList;
            }
        }

        /// <summary>
        /// Censors that cannot contain in text messages.
        /// </summary>
        public List<Regex> CensorWords
        {
            get
            {
                return censorWords;
            }
        }

        /// <summary>
        /// A collection of all contactgroups which are defined by the user who logged into the messenger network.
        /// </summary>
        public ContactGroupList ContactGroups
        {
            get
            {
                return contactGroups;
            }
        }

        /// <summary>
        /// A collection of all circles which are defined by the user who logged into the messenger network.
        /// </summary>
        public Dictionary<string, Contact> CircleList
        {
            get
            {
                return contactList[IMAddressInfoType.Circle];
            }
        }

        /// <summary>
        /// These credentials are used for user authentication and client identification
        /// </summary>
        public Credentials Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                credentials = value;
            }
        }

        /// <summary>
        /// If WebProxy is set the Webproxy is used for the
        /// authentication with Passport.com
        /// </summary>
        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return (MessageProcessor as NSMessageProcessor).ConnectivitySettings;
            }
        }

        /// <summary>
        /// A service that provide contact operations.
        /// </summary>
        internal ContactService ContactService
        {
            get
            {
                return contactService;
            }
        }

        /// <summary>
        /// Storage Service for get/update display name, personal status, display picture etc.
        /// </summary>
        internal MSNStorageService StorageService
        {
            get
            {
                return storageService;
            }
        }

        internal WhatsUpService WhatsUpService
        {
            get
            {
                return whatsUpService;
            }
        }

        internal MSNDirectoryService DirectoryService
        {
            get
            {
                return dirService;
            }
        }

        /// <summary>
        /// The local user logged into the network. It will remain null until user successfully login.
        /// </summary>
        internal Owner Owner
        {
            get
            {
                return ContactList.Owner;
            }
        }

        /// <summary>
        /// The processor to handle the messages
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                // de-register from the previous message processor
                if (messageProcessor != null)
                {
                    messageProcessor.ConnectionEstablished -= OnProcessorConnectCallback;
                    messageProcessor.ConnectionClosed -= OnProcessorDisconnectCallback;
                    // messageProcessor.SendCompleted -= OnProcessorSendCompletedCallback;

                    messageProcessor.UnregisterHandler(this);
                }

                NSMessageProcessor oldProcessor = messageProcessor;
                NSMessageProcessor newProcessor = (NSMessageProcessor)value;
                OnMessageProcessorChanged(new MessageProcessorChangedEventArgs(oldProcessor, newProcessor));

                messageProcessor = newProcessor;

                if (messageProcessor != null)
                {
                    // catch the connect event so we can start sending the USR command upon initiating
                    messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
                    // and make sure we respond on closing
                    messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
                    // track transid
                    // messageProcessor.SendCompleted += OnProcessorSendCompletedCallback;

                    messageProcessor.RegisterHandler(this);
                }
            }
        }

        /// <summary>
        /// The synchronizer of sibling contacts.
        /// </summary>
        internal ContactManager ContactManager
        {
            get
            {
                return contactManager;
            }
        }

        internal MessageManager MessageManager
        {
            get
            {
                return messageManager;
            }
        }

        /// <summary>
        /// The handler that handles all incoming P2P framework messages.
        /// </summary>
        /// <remarks>
        /// The handler is defined at the name server niveau which implies there is a single
        /// p2p handler instance for every logged in account. All switchboard sessions route their messages
        /// to this p2p handler. This enables the feature to start a p2p session in one switchboard session,
        /// and continue, or close it, in another switchboard session.
        /// </remarks>
        public P2PHandler P2PHandler
        {
            get
            {
                return p2pHandler;
            }
            internal protected set
            {
                p2pHandler = value;
            }
        }

        internal SDGBridge SDGBridge
        {
            get
            {
                return p2pHandler.SDGBridge;
            }
        }

        internal MSNTicket MSNTicket
        {
            get
            {
                return msnTicket;
            }
            set
            {
                msnTicket = value;
            }
        }

        #endregion

        #region Public Methods

        #region SendPing

        /// <summary>
        /// Sends PNG (ping) command.
        /// </summary>
        private void SendPing()
        {
            if (pong != null && messageProcessor != null && messageProcessor.Connected)
            {
                MessageProcessor.SendMessage(new NSMessage("PNG"));
            }
        }

        #endregion

        #region Circle

        internal int SendCircleNotifyRML(Guid circleId, string hostDomain, RoleLists lists)
        {
            string payload = "<ml><d n=\""
            + hostDomain + "\"><c n=\"" + circleId.ToString("D") + "\" l=\"" +
            ((int)lists).ToString() + "\" t=\"" +
            ((int)IMAddressInfoType.Circle).ToString() + "\"/></d></ml>";

            NSPayLoadMessage nsMessage = new NSPayLoadMessage("RML", payload);
            MessageProcessor.SendMessage(nsMessage);
            return nsMessage.TransactionID;
        }

        internal void SendSHAAMessage(string circleTicket)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(circleTicket);
            string nonce = Convert.ToBase64String(utf8ByteArray);
            MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "SHA", "A", nonce }));
        }

        #endregion

        #endregion

        #region Command Handlers

        #region Login

        /// <summary>
        /// Called when the message processor has disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnProcessorDisconnectCallback(object sender, EventArgs e)
        {
            if (Clear())
                OnSignedOff(new SignedOffEventArgs(SignedOffReason.None));
        }

        /// <summary>
        /// Called when the message processor has established a connection. This function will 
        /// begin the login procedure by sending the VER command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnProcessorConnectCallback(object sender, EventArgs e)
        {
            // Check for valid credentials
            if (Credentials == null)
                throw new MSNPSharpException("No Credentials passed in the NSMessageHandler");

            SendInitialMessage();
        }

        /// <summary>
        /// Send the first message to the server.
        /// </summary>
        protected virtual void SendInitialMessage()
        {
            (MessageProcessor as NSMessageProcessor).ResetTransactionID();

            // 1) VER: MSN Protocol used
            MessageProcessor.SendMessage(new NSMessage("VER", new string[] { "MSNP21", "CVR0" }));

            // 2) CVR: Send client information
            MsnProtocol msnProtocol = MsnProtocol.MSNP21;
            Credentials oldcred = Credentials;
            Credentials = new Credentials(oldcred.Account, oldcred.Password, msnProtocol);
            string base64Hop = (hopCount == 0 || hopCount == int.MaxValue) ? "0" : Convert.ToBase64String(Encoding.ASCII.GetBytes("Version: 1\r\nXfrCount: " + hopCount.ToString(CultureInfo.InvariantCulture)));

            MessageProcessor.SendMessage(new NSMessage("CVR",
                new string[] { 
                    "0x040c", //The LCIDs in .net framework are different from Windows API: "0x" + CultureInfo.CurrentCulture.LCID.ToString("x4")
                    "winnt",
                    "6.1.1",
                    "i386",
                    Credentials.ClientInfo.MessengerClientName, 
                    Credentials.ClientInfo.MessengerClientBuildVer,
                    Credentials.ClientInfo.MessengerClientBrand,
                    Credentials.Account,
                    base64Hop
                })
            );

            // 3) USR: Begin login procedure
            MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "SSO", "I", Credentials.Account }));
        }

        /// <summary>
        /// Called when a VER command has been received. 
        /// </summary>
        /// <remarks>
        /// Indicates that the server has approved our version of the protocol. This function will send the CVR command.
        /// <code>VER [Transaction] [Protocol1] ([Protocol2]) [Clientversion]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnVERReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a CVR command has been received. 
        /// </summary>
        /// <remarks>
        /// Indicates that the server has approved our client details. This function will send the USR command. 
        /// <code>CVR [Transaction] [Recommended version] [Recommended version] [Minimum version] [Download url] [Info url]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnCVRReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a USR command has been received. 
        /// </summary>
        /// <remarks>
        /// 
        /// <code>USR [Transaction] [SSO|OK] [Account] [Policy|Verified] [Nonce]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUSRReceived(NSMessage message)
        {
            //single-sign-on stuff
            if ((string)message.CommandValues[0] == "SSO")
            {
                string policy = (string)message.CommandValues[2];
                string nonce = (string)message.CommandValues[3];

                try
                {
                    SingleSignOnManager.Authenticate(
                        this,
                        policy,
                        delegate(object sender, EventArgs e)
                        {
                            if (messageProcessor != null && messageProcessor.Connected)
                            {
                                MBI mbi = new MBI();
                                string response =
                                    MSNTicket.SSOTickets[SSOTicketType.Clear].Ticket + " " +
                                    mbi.Encrypt(MSNTicket.SSOTickets[SSOTicketType.Clear].BinarySecret, nonce);

                                MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "SSO", "S", response, MachineGuid.ToString("B") }));
                            }
                        },
                        delegate(object sender, ExceptionEventArgs e)
                        {
                            if (messageProcessor != null)
                                messageProcessor.Disconnect();

                            OnAuthenticationError(e);
                        }
                    );
                }
                catch (Exception exception)
                {
                    if (messageProcessor != null)
                        messageProcessor.Disconnect();

                    OnAuthenticationError(new ExceptionEventArgs(exception));
                    return;
                }
            }
            else if ((string)message.CommandValues[0] == "OK")
            {
                // we sucesfully logged in
                if (Owner == null)
                {
                    // set the owner's name and CID
                    ContactList.SetOwner(new Owner(WebServiceConstants.MessengerIndividualAddressBookId, message.CommandValues[1].ToString(), msnTicket.OwnerCID, this));

                    Owner.GetCoreProfile(
                        delegate(object sender1, EventArgs arg)
                        {
                            OnOwnerVerified(EventArgs.Empty);
                        },
                        delegate(object sender2, ExceptionEventArgs exceptionArgs)
                        {
                            OnOwnerVerified(EventArgs.Empty);
                        }
                        );

                    pong = new System.Timers.Timer(1000);
                    pong.Elapsed += new System.Timers.ElapsedEventHandler(pong_Elapsed);
                    SendPing();
                }

                Owner.PassportVerified = message.CommandValues[2].Equals("1");
            }
        }

        private void pong_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendPing();
        }

        #endregion

        #region Status


        /// <summary>
        /// Translates MSNStatus enumeration to messenger's textual status presentation.
        /// </summary>
        /// <param name="status">MSNStatus enum object representing the status to translate</param>
        /// <returns>The corresponding textual value</returns>
        internal protected static string ParseStatus(PresenceStatus status)
        {
            switch (status)
            {
                case PresenceStatus.Online:
                    return "NLN";

                case PresenceStatus.Busy:
                case PresenceStatus.Phone:
                    return "BSY";

                case PresenceStatus.Idle:
                    return "IDL";

                case PresenceStatus.BRB:
                case PresenceStatus.Away:
                case PresenceStatus.Lunch:
                    return "AWY";

                case PresenceStatus.Offline:
                    return "FLN";

                case PresenceStatus.Hidden:
                    return "HDN";

                default:
                    break;
            }

            return "Unknown status";
        }

        /// <summary>
        /// Translates messenger's textual status to the corresponding value of the Status enum.
        /// </summary>
        /// <param name="status">Textual MSN status received from server</param>
        /// <returns>The corresponding enum value</returns>
        internal protected static PresenceStatus ParseStatus(string status)
        {
            switch (status)
            {
                case "NLN":
                    return PresenceStatus.Online;

                case "BSY":
                case "PHN":
                    return PresenceStatus.Busy;

                case "IDL":
                    return PresenceStatus.Idle;

                case "BRB":
                case "AWY":
                case "LUN":
                    return PresenceStatus.Away;

                case "FLN":
                    return PresenceStatus.Offline;

                case "HDN":
                    return PresenceStatus.Hidden;

                default:
                    break;
            }

            return PresenceStatus.Unknown;
        }

        /// <summary>
        /// Called when an OUT command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has signed off the user.
        /// <code>OUT [Reason]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnOUTReceived(NSMessage message)
        {
            if (message.CommandValues.Count == 1)
            {
                switch (message.CommandValues[0].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "OTH":
                        OnSignedOff(new SignedOffEventArgs(SignedOffReason.OtherClient));
                        break;
                    case "SSD":
                        OnSignedOff(new SignedOffEventArgs(SignedOffReason.ServerDown));
                        break;
                    default:
                        OnSignedOff(new SignedOffEventArgs(SignedOffReason.None));
                        break;
                }
            }
            else
                OnSignedOff(new SignedOffEventArgs(SignedOffReason.None));
        }





        #endregion

        #region Nameserver

        /// <summary>
        /// Called when a XFR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the notification server has send us the location that we must switch to a new notification server.
        /// <code>XFR 0 [NS] [*.gateway.edge.messenger.live.com:Port] [G] [D] [HopString]</code>
        /// <code>XFR [Transaction] [NS] [IP:Port] [U] [D] [HopString]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnXFRReceived(NSMessage message)
        {
            if ((string)message.CommandValues[0] == "NS")
            {
                // switch to a new notification server. That means reconnecting our current message processor.
                NSMessageProcessor processor = (NSMessageProcessor)MessageProcessor;
                // set new connectivity settings
                ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
                // disconnect first
                processor.Disconnect();
                processor = null;

                string[] hostAndPort = ((string)message.CommandValues[1]).Split(new char[] { ':' });
                bool isGateway = message.CommandValues[2].ToString() == "G";

                // Get hop count. Version: 1\r\nXfrCount: 2\r\n
                if (message.CommandValues.Count >= 5)
                {
                    string hopString = Encoding.ASCII.GetString(Convert.FromBase64String((string)message.CommandValues[4]));
                    Match m = Regex.Match(hopString, @"(\d+)").NextMatch();
                    if (m.Success)
                    {
                        hopCount = Convert.ToInt32(m.Value);
                    }
                }

                if (Settings.DisableHttpPolling && isGateway)
                {
                    newSettings.HttpPoll = false;
                    newSettings.Host = ConnectivitySettings.DefaultHost;
                    newSettings.Port = ConnectivitySettings.DefaultPort;
                }
                else
                {
                    newSettings.HttpPoll = isGateway;
                    newSettings.Host = hostAndPort[0];
                    newSettings.Port = int.Parse(hostAndPort[1], System.Globalization.CultureInfo.InvariantCulture);
                }

                // Register events and handler,
                NSMessageProcessor mp = new NSMessageProcessor(newSettings);
                this.MessageProcessor = mp;
                mp.Connect(); // then reconnect. The login procedure will now start over again.
            }
        }

        protected virtual void OnSBSReceived(NSMessage message)
        {
        }

        #endregion

        #region Mail and Notification

        /// <summary>
        /// Called when a NOT command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a notification message has been received.
        /// <code>NOT [body length]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnNOTReceived(NSMessage message)
        {
            // build a notification message
            NotificationMessage notification = new NotificationMessage(message);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Notification received : " + notification.BodyPayload);

            if (notification.NotificationTypeSpecified == false &&
                notification.Id == 0 &&
                notification.SiteId == 45705 &&
                notification.MessageId == 0 &&
                notification.SendDevice == "messenger" &&
                notification.ReceiverAccount.ToLowerInvariant() == Owner.Account)
            {
                string xmlString = notification.BodyPayload;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);

                XmlNode node = xmlDoc.SelectSingleNode(@"//NotificationData");

                if (node == null)
                    return;

                node = xmlDoc.SelectSingleNode(@"//NotificationData/Service");
                if (node != null)
                {
                    if (node.InnerText != "ABCHInternal")
                        return;

                    node = xmlDoc.SelectSingleNode(@"//NotificationData/CID");
                    if (node == null)
                        return;
                    if (node.InnerText != Owner.CID.ToString())
                        return;

                    ContactService.ServerNotificationRequest(PartnerScenario.ABChangeNotifyAlert, null, null);
                }
                else
                {
                    node = xmlDoc.SelectSingleNode(@"//NotificationData/CircleId");
                    if (node == null)
                        return;

                    string abId = node.InnerText;
                    node = xmlDoc.SelectSingleNode(@"//NotificationData/Role");

                    RoleId role = RoleId.Member;
                    if (node != null)
                    {
                        role = (RoleId)Enum.Parse(typeof(RoleId), node.InnerText);
                    }

                    object[] param = new object[] { abId, role };
                    ContactService.ServerNotificationRequest(PartnerScenario.CircleIdAlert, param, null);
                }
            }
        }

        /// <summary>
        /// Called when a MSG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the notification server has send us a MSG.
        /// This is usually a MSG from the 'HOTMAIL' user (account is also 'HOTMAIL') which includes notifications
        /// about the contact list owner's profile, new mail, number of unread mails in the inbox, offline messages, etc.
        /// <code>MSG [Account] [Name] [Length]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnMSGReceived(NSMessage message)
        {
            MimeMessage mimeMessage = message.InnerMessage as MimeMessage;
            string mime = mimeMessage.MimeHeader[MIMEContentHeaders.ContentType].ToString();

            if (mime.IndexOf("text/x-msmsgsprofile") >= 0)
            {
                StrDictionary hdr = mimeMessage.MimeHeader;

                int clientPort = 0;

                if (hdr.ContainsKey("ClientPort"))
                {
                    clientPort = int.Parse(mimeMessage.MimeHeader["ClientPort"].Replace('.', ' '), System.Globalization.CultureInfo.InvariantCulture);
                    clientPort = ((clientPort & 255) * 256) + ((clientPort & 65280) / 256);
                }

                IPAddress ip = hdr.ContainsKey("ClientIP") ? IPAddress.Parse(hdr["ClientIP"]) : IPAddress.None;

                if (IPAddress.None != ip)
                {
                    // set the external end point. This can be used in file transfer connectivity determing
                    externalEndPoint = new IPEndPoint(ip, clientPort);
                }

                Owner.UpdateProfile(hdr);
                ContactService.SynchronizeContactList();
            }
            else if (mime.IndexOf("x-msmsgsemailnotification") >= 0)
            {
                MimeMessage mimeEmailNotificationMessage = mimeMessage.InnerMessage as MimeMessage;

                OnNewMailReceived(new NewMailEventArgs(
                    mimeMessage.MimeHeader[MIMEHeaderStrings.From],
                    mimeEmailNotificationMessage.MimeHeader["Message-URL"],
                    mimeEmailNotificationMessage.MimeHeader["Post-URL"],
                    mimeEmailNotificationMessage.MimeHeader["Subject"],
                    mimeEmailNotificationMessage.MimeHeader["Dest-Folder"],
                    mimeEmailNotificationMessage.MimeHeader["From-Addr"],
                    mimeEmailNotificationMessage.MimeHeader.ContainsKey("id") ? int.Parse(mimeEmailNotificationMessage.MimeHeader["id"], System.Globalization.CultureInfo.InvariantCulture) : 0
                ));
            }
            else if (mime.IndexOf("x-msmsgsactivemailnotification") >= 0)
            {
                //Now this is the unread OIM info, not the new mail.
                MimeMessage mimeActiveEmailNotificationMessage = mimeMessage.InnerMessage as MimeMessage;

                OnMailboxChanged(new MailChangedEventArgs(
                    mimeActiveEmailNotificationMessage.MimeHeader["Src-Folder"],
                    mimeActiveEmailNotificationMessage.MimeHeader["Dest-Folder"],
                    mimeActiveEmailNotificationMessage.MimeHeader.ContainsKey("Message-Delta") ? int.Parse(mimeActiveEmailNotificationMessage.MimeHeader["Message-Delta"], System.Globalization.CultureInfo.InvariantCulture) : 0
                ));
            }
            else if (mime.IndexOf("x-msmsgsinitialemailnotification") >= 0)
            {
                MimeMessage mimeInitialEmailNotificationMessage = mimeMessage.InnerMessage as MimeMessage;

                OnMailboxStatusReceived(new MailboxStatusEventArgs(
                    mimeInitialEmailNotificationMessage.MimeHeader.ContainsKey("Inbox-Unread") ? int.Parse(mimeInitialEmailNotificationMessage.MimeHeader["Inbox-Unread"], System.Globalization.CultureInfo.InvariantCulture) : 0,
                    mimeInitialEmailNotificationMessage.MimeHeader.ContainsKey("Folders-Unread") ? int.Parse(mimeInitialEmailNotificationMessage.MimeHeader["Folders-Unread"], System.Globalization.CultureInfo.InvariantCulture) : 0,
                    mimeInitialEmailNotificationMessage.MimeHeader["Inbox-URL"],
                    mimeInitialEmailNotificationMessage.MimeHeader["Folders-URL"],
                    mimeInitialEmailNotificationMessage.MimeHeader["Post-URL"]
                ));
            }
            else if (mime.IndexOf("x-msmsgsinitialmdatanotification") >= 0 || mime.IndexOf("x-msmsgsoimnotification") >= 0)
            {
                MimeMessage innerMimeMessage = mimeMessage.InnerMessage as MimeMessage;


                /*
                 * <MD>
                 *     <E>
                 *         <I>884</I>     Inbox total
                 *         <IU>0</IU>     Inbox unread mail
                 *         <O>222</O>     Sent + Junk + Drafts
                 *         <OU>15</OU>    Junk unread mail
                 *     </E>
                 *     <Q>
                 *         <QTM>409600</QTM>
                 *         <QNM>204800</QNM>
                 *     </Q>
                 *     <M>
                 *         <!-- OIM Nodes -->
                 *     </M>
                 *     <M>
                 *         <!-- OIM Nodes -->
                 *     </M>
                 * </MD>
                 */

                string xmlstr = innerMimeMessage.MimeHeader["Mail-Data"];
                try
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.LoadXml(xmlstr);
                    XmlNodeList mdnodelist = xdoc.GetElementsByTagName("MD");
                    if (mdnodelist.Count > 0)
                    {
                        foreach (XmlNode node in mdnodelist[0])
                        {
                            if (node.Name.ToLowerInvariant() == "e" && node.HasChildNodes)
                            {
                                int iu = 0;
                                int ou = 0;
                                foreach (XmlNode cnode in node.ChildNodes)
                                {
                                    if (cnode.Name.ToLowerInvariant() == "iu")
                                    {
                                        int.TryParse(cnode.InnerText, out iu);
                                        break;
                                    }
                                }

                                foreach (XmlNode cnode in node.ChildNodes)
                                {
                                    if (cnode.Name.ToLowerInvariant() == "ou")
                                    {
                                        int.TryParse(cnode.InnerText, out ou);
                                        break;
                                    }
                                }

                                OnMailboxStatusReceived(new MailboxStatusEventArgs(
                                        iu,
                                        ou,
                                        innerMimeMessage.MimeHeader["Inbox-URL"],
                                        innerMimeMessage.MimeHeader["Folders-URL"],
                                        innerMimeMessage.MimeHeader["Post-URL"]
                                    ));
                                break;
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                }
            }
        }



        #endregion

        #region Contact, Circle and Group

        /// <summary>
        /// Called when a ADL command has been received.
        /// ADL [TransactionID] [OK]
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnADLReceived(NSMessage message)
        {
            if (message.TransactionID != 0 &&
                message.CommandValues[0].ToString() == "OK")
            {
                if (ProcessADL(message.TransactionID))
                {
                }
            }
            else
            {
                NetworkMessage networkMessage = message as NetworkMessage;
                if (networkMessage.InnerBody != null) //Payload ADL command
                {
                    #region NORMAL USER
                    if (AutoSynchronize)
                    {
                        // ALL CHANGES WILL BE MADE BY msRequest()
                        ContactService.msRequest(PartnerScenario.MessengerPendingList, null);
                    }
                    #endregion
                    #region BOT MODE
                    else
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(new MemoryStream(networkMessage.InnerBody));
                        XmlNodeList domains = xmlDoc.GetElementsByTagName("d");
                        string domain = String.Empty;
                        foreach (XmlNode domainNode in domains)
                        {
                            domain = domainNode.Attributes["n"].Value;
                            XmlNode contactNode = domainNode.FirstChild;
                            do
                            {
                                string account = contactNode.Attributes["n"].Value + "@" + domain;
                                IMAddressInfoType type = (IMAddressInfoType)int.Parse(contactNode.Attributes["t"].Value);
                                int list = int.Parse(contactNode.Attributes["l"].Value);
                                string displayName = account;
                                try
                                {
                                    displayName = contactNode.Attributes["f"].Value;
                                }
                                catch (Exception)
                                {
                                }

                                if (list == 8 /*RoleLists.Reverse*/)
                                {
                                    Contact contact = ContactList.GetContact(account, displayName, type);
                                    if (!contact.OnPendingList)
                                    {
                                        contact.Lists |= RoleLists.Pending;
                                        ContactService.OnFriendshipRequested(new ContactEventArgs(contact));
                                    }

                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "ADL received, FriendshipRequested event fired. Contact is in list: " + contact.Lists.ToString());

                                }

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + ":" + type + " was added to your " + list.ToString(), GetType().Name);

                            } while (contactNode.NextSibling != null);
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// Called when a RML command has been received.
        /// </summary>
        /// <remarks>Indicates that someone was removed from a list by local user (RML [Trans-ID] OK)
        /// or local user was removed from someone's reverse list (RML 0 [Trans-ID]\r\n[payload]).</remarks>
        /// <param name="nsMessage"></param>
        protected virtual void OnRMLReceived(NSMessage nsMessage)
        {
            NetworkMessage networkMessage = nsMessage as NetworkMessage;
            if (networkMessage.InnerBody != null)   //Payload RML command.
            {
                if (AutoSynchronize)
                {
                    ContactService.msRequest(PartnerScenario.Initial, null);
                }

                if (Settings.TraceSwitch.TraceVerbose)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(new MemoryStream(networkMessage.InnerBody));
                    XmlNodeList domains = xmlDoc.GetElementsByTagName("d");
                    string domain = String.Empty;
                    foreach (XmlNode domainNode in domains)
                    {
                        domain = domainNode.Attributes["n"].Value;
                        XmlNode contactNode = domainNode.FirstChild;
                        do
                        {
                            string account = contactNode.Attributes["n"].Value + "@" + domain;
                            IMAddressInfoType type = (IMAddressInfoType)int.Parse(contactNode.Attributes["t"].Value);
                            RoleLists list = (RoleLists)int.Parse(contactNode.Attributes["l"].Value);
                            account = account.ToLower(CultureInfo.InvariantCulture);

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + ":" + type + " has removed you from his/her " + list.ToString(), GetType().Name);

                        } while (contactNode.NextSibling != null);
                    }
                }
            }
        }

        #endregion

        #region Challenge and Ping

        /// <summary>
        /// Called when a CHL (challenge) command message has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnCHLReceived(NSMessage message)
        {
            if (Credentials == null)
                throw new MSNPSharpException("No credentials available for the NSMSNP15 handler. No challenge answer could be send.");

            string payload = QRYFactory.CreateQRY(Credentials.ClientID, Credentials.ClientCode, message.CommandValues[0].ToString());
            MessageProcessor.SendMessage(new NSPayLoadMessage("QRY", new string[] { Credentials.ClientID }, payload));
        }

        /// <summary>
        /// Called when a QRY (challenge) command message has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnQRYReceived(NSMessage message)
        {

        }

        /// <summary>
        /// Called when a QNG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates a ping answer. The number of seconds indicates the timespan in which another ping must be send.
        /// <code>QNG [Seconds]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnQNGReceived(NSMessage message)
        {
            if (pong != null)
            {
                int seconds = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);
                if (seconds > 1)
                {
                    try
                    {
                        pong.Interval = 1000 * (seconds - 1);
                        pong.Enabled = true;
                    }
                    catch (ObjectDisposedException)
                    {
                        pong = null;
                    }
                }
            }
        }


        #endregion

        #region Privacy

        /// <summary>
        /// Called when a GCF command has been received. 
        /// </summary>
        /// <remarks>Indicates that the server has send bad words for messaging.</remarks>
        /// <param name="message"></param>
        protected virtual void OnGCFReceived(NSMessage message)
        {
            NetworkMessage networkMessage = message as NetworkMessage;
            if (networkMessage.InnerBody != null)
            {
                censorWords.Clear();
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(new MemoryStream(networkMessage.InnerBody));
                XmlNodeList imtexts = xmlDoc.GetElementsByTagName("imtext");
                foreach (XmlNode imtextNode in imtexts)
                {
                    string censor = Encoding.UTF8.GetString(Convert.FromBase64String(imtextNode.Attributes["value"].Value));
                    censorWords.Add(new Regex(censor));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Censor: " + censor, GetType().Name);
                }
            }
        }

        #endregion

        #endregion

        #region ADL Processor

        internal void SetDefaults()
        {
            // Set display name, personal status and photo
            PersonalMessage pm = Owner.PersonalMessage;

            // If we don't have a live profile yet, get it from saved deltas file.
            {
                string psmMessageSaved = ContactService.Deltas.Profile.PersonalMessage;
                string mydispNameSaved = String.IsNullOrEmpty(ContactService.Deltas.Profile.DisplayName)
                    ? Owner.NickName : ContactService.Deltas.Profile.DisplayName;

                if (String.IsNullOrEmpty(pm.Message))
                    pm.Message = psmMessageSaved;

                if (String.IsNullOrEmpty(pm.FriendlyName))
                    pm.FriendlyName = mydispNameSaved;
            }

            Color colorScheme = ColorTranslator.FromOle(ContactService.Deltas.Profile.ColorScheme);
            Owner.SetColorScheme(colorScheme);
            pm.ColorScheme = colorScheme;

            SceneImage sceneImage = Owner.SceneImage;
            if (sceneImage != null && !sceneImage.IsDefaultImage)
            {
                pm.Scene = sceneImage.ContextPlain;
            }

            Owner.CreateDefaultDisplayImage(ContactService.Deltas.Profile.Photo.DisplayImage);
            pm.UserTileLocation = Owner.DisplayImage.IsDefaultImage ? string.Empty : Owner.DisplayImage.ContextPlain;
            Owner.PersonalMessage = pm;

            if (AutoSynchronize)
            {
                SendInitialServiceADL();
            }
            else
            {
                OnSignedIn(EventArgs.Empty);
            }
        }

        private void SendInitialServiceADL()
        {
            NSMessageProcessor nsmp = MessageProcessor as NSMessageProcessor;

            if (nsmp == null)
                return;

            if (adlState.ServiceADL == 0)
            {
                adlState.ServiceADL = nsmp.IncreaseTransactionID();

                string[] ownerAccount = Owner.Account.Split('@');
                string payload = "<ml><d n=\"" + ownerAccount[1] + "\"><c n=\"" + ownerAccount[0] + "\" t=\"1\"><s l=\"3\" n=\"IM\" /><s l=\"3\" n=\"PE\" /><s l=\"3\" n=\"PD\" /><s l=\"3\" n=\"PF\"/></c></d></ml>";

                NSPayLoadMessage nsPayload = new NSPayLoadMessage("ADL", payload);
                nsPayload.TransactionID = adlState.ServiceADL;
                nsmp.SendMessage(nsPayload, nsPayload.TransactionID);
            }
        }

        /// <summary>
        /// Send the initial ADL command to NS server. 
        /// </summary>
        /// <param name="scene">
        /// A <see cref="Scenario"/>
        /// </param>
        /// <remarks>
        /// The first ADL command MUST be a contact ADL. If you send a circle ADL instead,
        /// you will receive 201 server error for the following circle PUT command.
        /// </remarks>
        internal void SendInitialADL(Scenario scene)
        {
            if (scene == Scenario.None)
                return;

            NSMessageProcessor nsmp = (NSMessageProcessor)MessageProcessor;

            if (nsmp == null)
                return;

            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>();

            #region Process Contacts

            if ((scene & Scenario.SendInitialContactsADL) != Scenario.None)
            {
                // Combine initial ADL for Contacts
                hashlist = new Dictionary<string, RoleLists>(ContactList.Count);
                lock (ContactList.SyncRoot)
                {
                    foreach (Contact contact in ContactList.All)
                    {
                        if (contact.ADLCount == 0)
                            continue;

                        contact.ADLCount--;

                        string ch = contact.Hash;
                        RoleLists l = RoleLists.None;

                        if (contact.OnForwardList)
                            l |= RoleLists.Forward;

                        if (contact.OnAllowedList)
                            l |= RoleLists.Allow;

                        if (contact.AppearOffline)
                            l |= RoleLists.Hide;

                        if (l != RoleLists.None && !hashlist.ContainsKey(ch))
                            hashlist.Add(ch, l);
                    }
                }
                string[] adls = ContactList.GenerateMailListForAdl(hashlist, true);

                if (adls.Length > 0)
                {
                    foreach (string payload in adls)
                    {
                        NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                        message.TransactionID = nsmp.IncreaseTransactionID();
                        adlState.InitialADLs.Add(message.TransactionID, message);
                    }
                }
                scene |= adlState.IgnoredSenario;
                adlState.ContactADLProcessed = true;
            }

            #endregion

            #region Process Circles

            if ((scene & Scenario.SendInitialCirclesADL) != Scenario.None)
            {
                if (adlState.ContactADLProcessed)
                {
                    // Combine initial ADL for Circles
                    if (CircleList.Count > 0)
                    {
                        hashlist = new Dictionary<string, RoleLists>(CircleList.Count);
                        lock (ContactList.SyncRoot)
                        {
                            foreach (Contact circle in CircleList.Values)
                            {
                                if (circle.ADLCount == 0)
                                    continue;

                                circle.ADLCount--;
                                string ch = circle.Hash;
                                RoleLists l = circle.Lists;
                                hashlist.Add(ch, l);
                            }
                        }

                        string[] circleadls = ContactList.GenerateMailListForAdl(hashlist, true);

                        if (circleadls.Length > 0)
                        {
                            foreach (string payload in circleadls)
                            {
                                NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                                message.TransactionID = nsmp.IncreaseTransactionID();
                                adlState.InitialADLs.Add(message.TransactionID, message);
                            }
                        }
                    }
                }
                else
                {
                    adlState.IgnoredSenario |= Scenario.SendInitialCirclesADL;
                }
            }

            #endregion

            // Send All Initial ADLs...
            Dictionary<int, NSPayLoadMessage> initialADLsCopy = null;
            lock (adlState.InitialADLs)
            {
                initialADLsCopy = new Dictionary<int, NSPayLoadMessage>(adlState.InitialADLs);
            }
            foreach (NSPayLoadMessage nsPayload in initialADLsCopy.Values)
            {
                nsmp.SendMessage(nsPayload, nsPayload.TransactionID);
            }
        }

        private bool ProcessADL(int transid)
        {
            if (transid == adlState.ServiceADL)
            {
                SendInitialADL(Scenario.SendInitialContactsADL | Scenario.SendInitialCirclesADL);
                return true;
            }
            else if (adlState.InitialADLs.ContainsKey(transid))
            {
                lock (adlState.InitialADLs)
                {
                    adlState.InitialADLs.Remove(transid);
                }

                if (adlState.InitialADLs.Count <= 0)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "All initial ADLs have processed.", GetType().Name);

                    if (!AddressBookSynchronized)
                    {
                        if (AutoSynchronize)
                        {
                            foreach (Contact contact in ContactList.All)
                            {
                                // Added by other place, this place hasn't synchronized this contact yet.
                                if (contact.OnForwardList && contact.OnPendingList)
                                {
                                    contact.OnPendingList = false;
                                }
                                else if (contact.OnPendingList || contact.FriendshipStatus == RoleId.Pending)
                                {
                                    // FriendshipRequested (1/2): Before SignedIn
                                    ContactService.OnFriendshipRequested(new ContactEventArgs(contact));
                                }
                            }
                        }

                        ContactService.OnSynchronizationCompleted(EventArgs.Empty);
                    }

                    if (AutoSynchronize)
                    {
                        OnSignedIn(EventArgs.Empty);
                    }
                }
                return true;
            }
            return false;
        }

        #endregion


        #region Command handler

        /// <summary>
        /// Clears all resources associated with a nameserver session.
        /// </summary>
        /// <remarks>
        /// Called after we the processor has disconnected. This will clear the contactlist and free other resources.
        /// </remarks>
        protected virtual bool Clear()
        {
            // 0. Remove pong
            if (pong != null)
            {
                pong.Elapsed -= pong_Elapsed;
                try
                {
                    if (pong.Enabled)
                        pong.Enabled = false;

                    pong.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    pong = null;
                }
            }

            // 1. Cancel transfers
            p2pHandler.Dispose();

            // 2. Cancel web services. MSNTicket must be here.
            msnTicket = MSNTicket.Empty;
            ContactService.Clear();
            StorageService.Clear();
            WhatsUpService.Clear();
            DirectoryService.Clear();

            // 3. isSignedIn must be here... 
            // a) ContactService.Clear() merges and saves addressbook if isSignedIn=true.
            // b) Owner.ClientCapabilities = ClientCapabilities.None doesn't send CHG command if isSignedIn=false.
            bool signInStatus = IsSignedIn;
            isSignedIn = false;
            externalEndPoint = null;
            adlState.Reset();

            // 4. Clear contact lists and circle list.
            ContactList.Reset();
            CircleList.Clear();
            ContactGroups.Clear();

            //5. Reset contact manager.
            ContactManager.Reset();

            //6. Reset censor words
            CensorWords.Clear();

            //7. Clear multiparties
            lock (multiParties)
                multiParties.Clear();

            return signInStatus;
        }

        protected virtual NetworkMessage ParseTextPayloadMessage(NSMessage message)
        {
            TextPayloadMessage txtPayLoad = new TextPayloadMessage(string.Empty);
            txtPayLoad.CreateFromParentMessage(message);
            return message;
        }

        protected virtual NetworkMessage ParseMSGMessage(NSMessage message)
        {
            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.CreateFromParentMessage(message);

            string mime = mimeMessage.MimeHeader[MIMEContentHeaders.ContentType].ToString();

            if (mime.IndexOf("text/x-msmsgsprofile") >= 0)
            {
                //This is profile, the content is nothing.
            }
            else
            {
                MimeMessage innerMimeMessage = new MimeMessage(false);
                innerMimeMessage.CreateFromParentMessage(mimeMessage);
            }

            return message;
        }

        protected virtual NetworkMessage ParseNetworkMessage(NetworkMessage message)
        {
            NSMessage nsMessage = (NSMessage)message;

            if (nsMessage.InnerBody != null)
            {
                switch (nsMessage.Command)
                {
                    case "MSG":
                        ParseMSGMessage(nsMessage);
                        break;
                    case "SDG":
                        ParseSDGMessage(nsMessage);
                        break;
                    default:
                        ParseTextPayloadMessage(nsMessage);
                        break;
                }
            }

            return nsMessage;
        }

        protected virtual bool ProcessNetworkMessage(NetworkMessage message)
        {
            NSMessage nsMessage = (NSMessage)message;
            bool isUnknownMessage = false;

            switch (nsMessage.Command)
            {
                // Most used CMDs
                case "NFY":
                    OnNFYReceived(nsMessage);
                    break;
                case "SDG":
                    OnSDGReceived(nsMessage);
                    break;
                case "MSG":
                    OnMSGReceived(nsMessage);
                    break;
                case "QNG":
                    OnQNGReceived(nsMessage);
                    break;
                case "ADL":
                    OnADLReceived(nsMessage);
                    break;
                case "RML":
                    OnRMLReceived(nsMessage);
                    break;
                case "PUT":
                    OnPUTReceived(nsMessage);
                    break;
                case "DEL":
                    OnDELReceived(nsMessage);
                    break;

                // Other CMDs               
                case "CHL":
                    OnCHLReceived(nsMessage);
                    break;
                case "NOT":
                    OnNOTReceived(nsMessage);
                    break;
                case "OUT":
                    OnOUTReceived(nsMessage);
                    break;
                case "QRY":
                    OnQRYReceived(nsMessage);
                    break;
                case "SBS":
                    OnSBSReceived(nsMessage);
                    break;
                case "VER":
                    OnVERReceived(nsMessage);
                    break;
                case "CVR":
                    OnCVRReceived(nsMessage);
                    break;
                case "USR":
                    OnUSRReceived(nsMessage);
                    break;
                case "XFR":
                    OnXFRReceived(nsMessage);
                    break;
                case "GCF":
                    OnGCFReceived(nsMessage);
                    break;

                // Error codes
                case "801":
                    On801Received(nsMessage);
                    break;
                default:
                    isUnknownMessage = true;
                    break;
            }

            return !isUnknownMessage;
        }

        /// <summary>
        /// Handles message from the processor.
        /// </summary>
        /// <remarks>
        /// This is one of the most important functions of the class.
        /// It handles incoming messages and performs actions based on the commands in the messages.
        /// Many commands will affect the data objects in MSNPSharp, like <see cref="Contact"/> and <see cref="ContactGroup"/>.
        /// For example contacts are renamed, contactgroups are added and status is set.
        /// Exceptions which occur in this method are redirected via the <see cref="ExceptionOccurred"/> event.
        /// </remarks>
        /// <param name="sender">The message processor that dispatched the message.</param>
        /// <param name="message">The network message received from the notification server</param>
        public virtual void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            try
            {
                // We expect at least a NSMessage object
                NSMessage nsMessage = (NSMessage)message;
                bool processed = false;

                ParseNetworkMessage(message);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Incoming NS command: " + message.ToDebugString() + "\r\n", GetType().Name);

                processed = ProcessNetworkMessage(message);

                if (processed)
                    return;

                // Check whether it is a numeric error command
                if (nsMessage.Command[0] >= '0' && nsMessage.Command[0] <= '9' && processed == false)
                {
                    MSNError msnError = 0;
                    string description = string.Empty;
                    try
                    {
                        int errorCode = int.Parse(nsMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
                        msnError = (MSNError)errorCode;

                        if (nsMessage.InnerBody != null && nsMessage.InnerBody.Length > 0)
                        {
                            description = Encoding.UTF8.GetString(nsMessage.InnerBody);
                        }
                        else
                        {
                            description = msnError.ToString();
                        }
                    }
                    catch (Exception fe)
                    {
                        throw new MSNPSharpException("Exception Occurred when parsing an error code received from the server", fe);
                    }

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "A server error occurred\r\nError Code: " + nsMessage.Command
                        + "\r\nError Description: " + msnError.ToString());

                    OnServerErrorReceived(new MSNErrorEventArgs(msnError, description));
                }
                else
                {
                    // It is a unknown command
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "UNKNOWN COMMAND: " + nsMessage.Command + "\r\n" + nsMessage.ToDebugString(), GetType().ToString());
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(new ExceptionEventArgs(e));
                throw; //RethrowToPreserveStackDetails (without e)
            }
        }

        #endregion
    }
};