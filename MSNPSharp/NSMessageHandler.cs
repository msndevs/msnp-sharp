﻿#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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

        #region Public Events

        /// <summary>
        /// Occurs when the user finished the authentication step, the owner was created.
        /// </summary>
        public event EventHandler<EventArgs> OwnerVerified;

        /// <summary>
        /// Occurs when the authentication and authorization with the server has finished.
        /// The client is now connected to the messenger network.
        /// </summary>
        public event EventHandler<EventArgs> SignedIn;

        /// <summary>
        /// Occurs when the message processor has disconnected, and thus the user is no longer signed in.
        /// </summary>
        public event EventHandler<SignedOffEventArgs> SignedOff;


        /// <summary>
        /// Occurs when an answer is received after sending a ping to the MSN server via the SendPing() method.
        /// </summary>
        public event EventHandler<PingAnswerEventArgs> PingAnswer;

        /// <summary>
        /// Occurs when the server notifies the client with the status of the owner's mailbox.
        /// </summary>
        public event EventHandler<MailboxStatusEventArgs> MailboxStatusReceived;

        /// <summary>
        /// Occurs when new mail is received.
        /// </summary>
        public event EventHandler<NewMailEventArgs> NewMailReceived;

        /// <summary>
        /// Occurs when unread mail is read or mail is moved.
        /// </summary>
        public event EventHandler<MailChangedEventArgs> MailboxChanged;

        /// <summary>
        /// Occurs when the server sends an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;

        /// <summary>
        /// Occurs when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Occurs when the user could not be signed in due to authentication errors. Most likely due
        /// to an invalid account or password. Note that this event will also raise the more general
        /// <see cref="ExceptionOccurred"/> event.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> AuthenticationError;

        #endregion

        #region Members

        private Credentials credentials = new Credentials(MsnProtocol.MSNP21);
        private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint externalEndPoint;
        private NSMessageProcessor messageProcessor;
        private P2PHandler p2pHandler;
        private int hopCount = 0;

        private ContactGroupList contactGroups;
        private ContactList contactList;
        private ContactManager contactManager;
        private MessageManager messageManager;
        private bool autoSynchronize = true;
        private bool botMode = false;
        private int canSendPing = 1;

        private bool isSignedIn = false;
        private MSNTicket msnTicket = MSNTicket.Empty;

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
                    messageProcessor.SendCompleted -= OnProcessorSendCompletedCallback;
                }

                messageProcessor = (NSMessageProcessor) value;

                if (messageProcessor != null)
                {
                    // catch the connect event so we can start sending the USR command upon initiating
                    messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
                    // and make sure we respond on closing
                    messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
                    // track transid
                    messageProcessor.SendCompleted += OnProcessorSendCompletedCallback;
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
        public virtual void SendPing()
        {
            if (Interlocked.CompareExchange(ref canSendPing, 0, 1) == 1)
            {
                MessageProcessor.SendMessage(new NSMessage("PNG"));
            }
        }

        #endregion

        #region SetScreenName & SetPersonalMessage

        /// <summary>
        /// Sets the contactlist owner's screenname. After receiving confirmation from the server
        /// this will set the Owner object's name which will in turn raise the NameChange event.
        /// </summary>
        internal void SetScreenName(string newName)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if(string.IsNullOrEmpty(newName))
            {
                newName = Owner.Account;
            }

            PersonalMessage pm = Owner.PersonalMessage;

            pm.FriendlyName = newName;

            if (Owner.Status != PresenceStatus.Offline)
            {
                SetPresenceStatus(
                            Owner.Status,
                            Owner.LocalEndPointIMCapabilities, Owner.LocalEndPointIMCapabilitiesEx,
                            Owner.LocalEndPointPECapabilities, Owner.LocalEndPointPECapabilitiesEx,
                            Owner.EpName, pm, true);
            }
        }

        /// <summary>
        /// Sets personal message.
        /// </summary>
        internal void SetPersonalMessage(PersonalMessage newPSM)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (Owner.Status != PresenceStatus.Offline)
            {
                SetPresenceStatus(
                    Owner.Status,
                    Owner.LocalEndPointIMCapabilities, Owner.LocalEndPointIMCapabilitiesEx,
                    Owner.LocalEndPointPECapabilities, Owner.LocalEndPointPECapabilitiesEx,
                    Owner.EpName, newPSM, true);
            }

        }

        /// <summary>
        /// Sets the scene image and scheme context.
        /// </summary>
        internal void SetSceneData(SceneImage scimg, Color sccolor)
        {
            if (Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            PersonalMessage pm = Owner.PersonalMessage;

            pm.ColorScheme = sccolor;
            pm.Scene = scimg.IsDefaultImage ? String.Empty : scimg.ContextPlain;

            SetPresenceStatus(Owner.Status,
                Owner.LocalEndPointIMCapabilities, Owner.LocalEndPointIMCapabilitiesEx,
                Owner.LocalEndPointPECapabilities, Owner.LocalEndPointPECapabilitiesEx,
                Owner.EpName, pm, true);
        }

        #endregion

        #region Circle

        internal void SendBlockCircleNSCommands(Guid circleId, string hostDomain)
        {
            SendCircleNotifyRML(circleId, hostDomain, RoleLists.Allow, true);
            SendCircleNotifyADL(circleId, hostDomain, RoleLists.Hide, true);
        }

        internal void SendUnBlockCircleNSCommands(Guid circleId, string hostDomain)
        {
            SendCircleNotifyRML(circleId, hostDomain, RoleLists.Hide, true);
            SendCircleNotifyADL(circleId, hostDomain, RoleLists.Allow, true);
        }

        internal int SendCircleNotifyADL(Guid circleId, string hostDomain, RoleLists lists, bool blockUnBlock)
        {
            string payload = "<ml" + (blockUnBlock == true ? "" : " l=\"1\"") + "><d n=\""
            + hostDomain + "\"><c n=\"" + circleId.ToString("D") + "\" l=\"" +
            ((int)lists).ToString() + "\" t=\"" +
            ((int)IMAddressInfoType.Circle).ToString() + "\"/></d></ml>";

            NSPayLoadMessage nsMessage = new NSPayLoadMessage("ADL", payload);
            MessageProcessor.SendMessage(nsMessage);
            return nsMessage.TransactionID;
        }

        internal int SendCircleNotifyRML(Guid circleId, string hostDomain, RoleLists lists, bool blockUnBlock)
        {
            string payload = "<ml" + (blockUnBlock == true ? "" : " l=\"1\"") + "><d n=\""
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

        protected virtual void OnProcessorSendCompletedCallback(object sender, ObjectEventArgs e)
        {
            SDGBridge.FireSendCompleted((int)e.Object);
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

                            OnAuthenticationErrorOccurred(e);
                        }
                    );
                }
                catch (Exception exception)
                {
                    if (messageProcessor != null)
                        messageProcessor.Disconnect();

                    OnAuthenticationErrorOccurred(new ExceptionEventArgs(exception));
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
                }

                Owner.PassportVerified = message.CommandValues[2].Equals("1");
            }
        }

        /// <summary>
        /// Fires the <see cref="SignedIn"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnSignedIn(EventArgs e)
        {
            isSignedIn = true;

            Owner.EndPointData[NSMessageHandler.MachineGuid] = new PrivateEndPointData(Owner.Account, NSMessageHandler.MachineGuid);

            if (ContactService.Deltas != null)
                Owner.SyncProfileToDeltas();


            if (SignedIn != null)
                SignedIn(this, e);

            SendPing(); //Ping the server for the first time. Then client programmer should handle the answer.
        }

        /// <summary>
        /// Fires the <see cref="SignedOff"/> event.
        /// </summary>
        /// <param name="e"></param>
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

                // disconnect first
                processor.Disconnect();

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

                // set new connectivity settings
                ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);

                if (!isGateway)
                {
                    newSettings.Host = hostAndPort[0];
                    newSettings.Port = int.Parse(hostAndPort[1], System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    newSettings.Host = ConnectivitySettings.DefaultHost;
                    newSettings.Port = ConnectivitySettings.DefaultPort;
                }

                processor.ConnectivitySettings = newSettings;

                // and reconnect. The login procedure will now start over again
                processor.Connect();
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

                    CirclePersonalMembershipRole role = CirclePersonalMembershipRole.Member;
                    if (node != null)
                    {
                        role = (CirclePersonalMembershipRole)Enum.Parse(typeof(CirclePersonalMembershipRole), node.InnerText);
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

                OnMailNotificationReceived(new NewMailEventArgs(
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

                OnMailChanged(new MailChangedEventArgs(
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

        /// <summary>
        /// Fires the <see cref="MailboxChanged"/> event.
        /// </summary>
        /// <remarks>Called when the owner has removed or moved e-mail.</remarks>
        /// <param name="e"></param>
        protected virtual void OnMailChanged(MailChangedEventArgs e)
        {
            if (MailboxChanged != null)
            {
                MailboxChanged(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="MailboxStatusReceived"/> event.
        /// </summary>
        /// <remarks>Called when the server sends the status of the owner's mailbox.</remarks>
        /// <param name="e"></param>
        protected virtual void OnMailboxStatusReceived(MailboxStatusEventArgs e)
        {
            if (MailboxStatusReceived != null)
            {
                MailboxStatusReceived(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="NewMailReceived"/> event.
        /// </summary>
        /// <remarks>Called when the owner has received new e-mail, or e-mail has been removed / moved.</remarks>
        /// <param name="e"></param>
        protected virtual void OnMailNotificationReceived(NewMailEventArgs e)
        {
            if (NewMailReceived != null)
            {
                NewMailReceived(this, e);
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
                if (ContactService.ProcessADL(message.TransactionID))
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

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + " has removed you from his/her " + list.ToString(), GetType().Name);

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
            if (PingAnswer != null)
            {
                // get the number of seconds till the next ping and fire the event
                // with the correct parameters.
                int seconds = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);
                PingAnswer(this, new PingAnswerEventArgs(seconds));

                Interlocked.CompareExchange(ref canSendPing, 1, 0);
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

        #region Command handler

        /// <summary>
        /// Clears all resources associated with a nameserver session.
        /// </summary>
        /// <remarks>
        /// Called after we the processor has disconnected. This will clear the contactlist and free other resources.
        /// </remarks>
        protected virtual bool Clear()
        {
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
            Interlocked.Exchange(ref canSendPing, 1);

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

        /// <summary>
        /// Fires the <see cref="ServerErrorReceived"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnServerErrorReceived(MSNErrorEventArgs e)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ExceptionOccurred"/> event.
        /// </summary>
        /// <param name="e">The exception event args</param>
        protected virtual void OnExceptionOccurred(ExceptionEventArgs e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Exception.Message + "\r\n\r\nStackTrace: \r\n" + e.Exception.StackTrace + "\r\n\r\n", GetType().Name);
        }

        /// <summary>
        /// Fires the <see cref="AuthenticationError"/> event.
        /// </summary>
        /// <param name="e">The exception event args</param>
        protected virtual void OnAuthenticationErrorOccurred(ExceptionEventArgs e)
        {
            if (AuthenticationError != null)
                AuthenticationError(this, e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString(), GetType().Name);
        }

        /// <summary>
        /// Fires the <see cref="OwnerVerified"/> event.
        /// </summary>
        protected virtual void OnOwnerVerified(EventArgs e)
        {
            if (OwnerVerified != null)
                OwnerVerified(this, e);
        }

        #endregion
    }
};