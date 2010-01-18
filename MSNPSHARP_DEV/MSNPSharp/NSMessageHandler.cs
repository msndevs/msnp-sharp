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
using System.Web;
using System.Xml;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Handles the protocol messages from the notification server
    /// and implements protocol version MSNP18.
    /// </summary>
    public partial class NSMessageHandler : IMessageHandler
    {
        public static readonly Guid MachineGuid = Guid.NewGuid();

        #region Members

        private Credentials credentials = new Credentials(MsnProtocol.MSNP18);
        private SocketMessageProcessor messageProcessor;
        private ConnectivitySettings connectivitySettings;
        private IPEndPoint externalEndPoint;
        private P2PHandler p2pHandler;

        private CircleList circleList;
        private ContactGroupList contactGroups;
        private ContactList contactList;
        private ContactManager manager;
        private bool autoSynchronize = true;
        private bool botMode = false;

        private bool isSignedIn;
        private MSNTicket msnticket = MSNTicket.Empty;
        private Queue pendingSwitchboards = new Queue();

        private ContactService contactService;
        private OIMService oimService;
        private MSNStorageService storageService;
        private WhatsUpService whatsUpService;
        private ClientCapacities defaultClientCapacities = ClientCapacities.CanMultiPacketMSG | ClientCapacities.SupportP2PUUNBootstrap | ClientCapacities.CanReceiveWinks | ClientCapacities.CanHandleMSNC10;
        private ClientCapacitiesEx defaultClientCapacitiesEx = ClientCapacitiesEx.CanP2PV2 | ClientCapacitiesEx.RTCVideoEnabled;

        private List<Regex> censorWords = new List<Regex>(0);

        protected internal NSMessageHandler()
        {
            circleList = new CircleList(this);
            contactGroups = new ContactGroupList(this);
            contactList = new ContactList(this);
            manager = new ContactManager(this);

            contactService = new ContactService(this);
            oimService = new OIMService(this);
            storageService = new MSNStorageService(this);
            whatsUpService = new WhatsUpService(this);

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
        /// The owner of the contactlist. This is the identity that logged into the messenger network.
        /// </summary>
        [Obsolete("Obsoleted in 3.1, please use NSMessageHandler.ContactList.Owner instead.\r\n" +
            "The Owner property's behavior changed a little." + 
            "It will remain null until user successfully login." +
            "You may need to change your code if you see this notic." +
            "For more information and example, please refer to the example client."
            , true)]
        public Owner Owner
        {
            get
            {
                return ContactList.Owner;
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
        public CircleList CircleList
        {
            get
            {
                return circleList;
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
                return connectivitySettings;
            }
            set
            {
                connectivitySettings = value;
            }
        }

        /// <summary>
        /// A service that provide contact operations.
        /// </summary>
        public ContactService ContactService
        {
            get
            {
                return contactService;
            }
        }

        /// <summary>
        /// Offline message service.
        /// </summary>
        public OIMService OIMService
        {
            get
            {
                return oimService;
            }
        }

        /// <summary>
        /// Storage Service for get/update display name, personal status, display picture etc.
        /// </summary>
        public MSNStorageService StorageService
        {
            get
            {
                return storageService;
            }
        }

        public WhatsUpService WhatsUpService
        {
            get
            {
                return whatsUpService;
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
                }

                messageProcessor = value as SocketMessageProcessor;

                if (messageProcessor != null)
                {
                    // catch the connect event so we can start sending the USR command upon initiating
                    messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
                    // and make sure we respond on closing
                    messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
                }
            }
        }

        /// <summary>
        /// The synchronizer of sibling contacts.
        /// </summary>
        internal ContactManager Manager
        {
            get
            {
                return manager;
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

        internal MSNTicket MSNTicket
        {
            get
            {
                return msnticket;
            }
            set
            {
                msnticket = value;
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when an exception is thrown while handling the incoming or outgoing messages
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Occurs when the user could not be signed in due to authentication errors. Most likely due to an invalid account or password. Note that this event will also raise the more general <see cref="ExceptionOccurred"/> event.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> AuthenticationError;

        /// <summary>
        /// Occurs when an answer is received after sending a ping to the MSN server via the SendPing() method
        /// </summary>
        public event EventHandler<PingAnswerEventArgs> PingAnswer;

        /// <summary>
        /// Occurs when any contact changes status.
        /// </summary>
        public event EventHandler<ContactStatusChangedEventArgs> ContactStatusChanged;

        /// <summary>
        /// Occurs when any contact goes from offline status to another status.
        /// </summary>
        public event EventHandler<ContactEventArgs> ContactOnline;

        /// <summary>
        /// Occurs when any contact goed from any status to offline status.
        /// </summary>
        public event EventHandler<ContactEventArgs> ContactOffline;

        /// <summary>
        /// Occurs when any circle changes status.
        /// </summary>
        public event EventHandler<CircleStatusChangedEventArgs> CircleStatusChanged;

        /// <summary>
        /// Occurs when any circle goes from offline status to another status.
        /// </summary>
        public event EventHandler<CircleEventArgs> CircleOnline;

        /// <summary>
        /// Occurs when any circle goes from any status to offline status.
        /// </summary>
        public event EventHandler<CircleEventArgs> CircleOffline;

        /// <summary>
        /// Occurs when any circle member goes from offline status to another status.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleMemberOnline;

        /// <summary>
        ///  Occurs when any circle member goes from any status to offline status.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleMemberOffline;

        /// <summary>
        /// Occurs when any circle member changes status.
        /// </summary>
        public event EventHandler<CircleMemberStatusChanged> CircleMemberStatusChanged;

        /// <summary>
        /// Occurs when a member left the circle conversation.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> LeftCircleConversation;

        /// <summary>
        /// Occurs when a member joined the circle conversation.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> JoinedCircleConversation;

        /// <summary>
        /// Occurs when the authentication and authorzation with the server has finished. The client is now connected to the messenger network.
        /// </summary>
        public event EventHandler<EventArgs> SignedIn;

        /// <summary>
        /// Occurs when the message processor has disconnected, and thus the user is no longer signed in.
        /// </summary>
        public event EventHandler<SignedOffEventArgs> SignedOff;

        /// <summary>
        /// Occurs when a switchboard session has been created
        /// </summary>
        public event EventHandler<SBCreatedEventArgs> SBCreated;

        /// <summary>
        /// Occurs when the server notifies the client with the status of the owner's mailbox.
        /// </summary>
        public event EventHandler<MailboxStatusEventArgs> MailboxStatusReceived;

        /// <summary>
        /// Occurs when new mail is received by the Owner.
        /// </summary>
        public event EventHandler<NewMailEventArgs> NewMailReceived;

        /// <summary>
        /// Occurs when unread mail is read or mail is moved by the Owner.
        /// </summary>
        public event EventHandler<MailChangedEventArgs> MailboxChanged;

        /// <summary>
        /// Occurs when the server sends an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;

        /// <summary>
        /// Occurs when we receive a mobile message.
        /// </summary>
        public event EventHandler<TextMessageEventArgs> MobileMessageReceived;

        /// <summary>
        /// Occurs when a circle member is typing.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleTypingMessageReceived;

        /// <summary>
        /// Occurs when we receive a nudge message sent by a circle member.
        /// </summary>
        public event EventHandler<CircleMemberEventArgs> CircleNudgeReceived;

        /// <summary>
        /// Occurs when we receive a text message sent from a circle.
        /// </summary>
        public event EventHandler<CircleTextMessageEventArgs> CircleTextMessageReceived;

        #endregion

        #region Public Methods

        #region Mobile

        /// <summary>
        /// Sets the telephonenumber for the contact list owner's homephone.
        /// </summary>
        internal void SetPhoneNumberHome(string number)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (number.Length > 30)
                throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHH", HttpUtility.UrlEncode(number) }));
        }

        /// <summary>
        /// Sets the telephonenumber for the contact list owner's workphone.
        /// </summary>
        internal void SetPhoneNumberWork(string number)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (number.Length > 30)
                throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHW", HttpUtility.UrlEncode(number) }));
        }

        /// <summary>
        /// Sets the telephonenumber for the contact list owner's mobile phone.
        /// </summary>
        internal void SetPhoneNumberMobile(string number)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (number.Length > 30)
                throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHM", HttpUtility.UrlEncode(number) }));
        }

        /// <summary>
        /// Sets whether the contact list owner allows remote contacts to send messages to it's mobile device.
        /// </summary>
        internal void SetMobileAccess(bool enabled)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MOB", enabled ? "Y" : "N" }));
        }

        /// <summary>
        /// Sets whether the contact list owner has a mobile device enabled.
        /// </summary>
        internal void SetMobileDevice(bool enabled)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MBE", enabled ? "Y" : "N" }));
        }

        /// <summary>
        /// Sends a mobile message to the specified remote contact. This only works when the remote contact has it's mobile device enabled and has MSN-direct enabled.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="text"></param>
        public virtual void SendMobileMessage(Contact receiver, string text)
        {
            SendMobileMessage(receiver, text, String.Empty, String.Empty);
        }

        /// <summary>
        /// Sends a mobile message to the specified remote contact. This only works when the remote contact has it's mobile device enabled and has MSN-direct enabled.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="text"></param>
        /// <param name="callbackNumber"></param>
        /// <param name="callbackDevice"></param>
        public virtual void SendMobileMessage(Contact receiver, string text, string callbackNumber, string callbackDevice)
        {
            if (receiver.MobileAccess || receiver.ClientType == ClientType.PhoneMember)
            {
                string to = (receiver.ClientType == ClientType.PhoneMember) ? "tel:" + receiver.Mail : receiver.Mail;

                TextMessage txtMsg = new TextMessage(text);
                MSGMessage msgMessage = new MSGMessage();
                msgMessage.InnerMessage = txtMsg;
                msgMessage.MimeHeader["Dest-Agent"] = "mobile";

                YIMMessage nsMessage = new YIMMessage("UUM",
                    new string[] { to, ((int)receiver.ClientType).ToString(), "1" },
                    Credentials.MsnProtocol);

                nsMessage.InnerMessage = msgMessage;
                MessageProcessor.SendMessage(nsMessage);

            }
            else
            {
                throw new MSNPSharpException("The specified contact has no mobile device enabled nor phone member.");
            }
        }


        #endregion

        #region RequestSwitchboard & SendPing

        // List<SBMessageHandler> SwitchBoards = new List<SBMessageHandler>(0);

        /// <summary>
        /// Sends a request to the server to start a new switchboard session.
        /// </summary>
        public virtual SBMessageHandler RequestSwitchboard(object initiator)
        {
            // create a switchboard object
            SBMessageHandler handler = new SBMessageHandler();

            RequestSwitchboard(handler, initiator);

            return handler;
        }

        /// <summary>
        /// Sends a request to the server to start a new switchboard session. The specified switchboard handler will be associated with the new switchboard session.
        /// </summary>
        /// <param name="switchboardHandler">The switchboard handler to use. A switchboard processor will be created and connected to this handler.</param>
        /// <param name="initiator">The object that initiated the request for the switchboard.</param>
        /// <returns></returns>
        public virtual void RequestSwitchboard(SBMessageHandler switchboardHandler, object initiator)
        {
            pendingSwitchboards.Enqueue(new SwitchboardQueueItem(switchboardHandler, initiator));
            MessageProcessor.SendMessage(new NSMessage("XFR", new string[] { "SB" }));
        }

        /// <summary>
        /// Sends PNG (ping) command.
        /// </summary>
        public virtual void SendPing()
        {
            MessageProcessor.SendMessage(new NSMessage("PNG"));
        }

        #endregion

        #region RequestScreenName & SetScreenName & SetPersonalMessage

        /// <summary>
        /// Send the server a request for the contact's screen name.
        /// </summary>
        /// <remarks>
        /// When the server replies with the screen name the Name property of the <see cref="Contact"/> will
        /// be updated.
        /// </remarks>
        /// <param name="contact"></param>
        internal void RequestScreenName(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            MessageProcessor.SendMessage(new NSMessage("SBP", new string[] { contact.Guid.ToString(), "MFN", MSNHttpUtility.UrlEncode(contact.Name) }));
        }

        /// <summary>
        /// Sets the contactlist owner's screenname. After receiving confirmation from the server
        /// this will set the Owner object's name which will in turn raise the NameChange event.
        /// </summary>
        internal void SetScreenName(string newName)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            if (ContactList.Owner.PassportVerified)
            {
                MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MFN", MSNHttpUtility.UrlEncode(newName) }));
            }


            StorageService.UpdateProfile(newName, ContactList.Owner.PersonalMessage != null && ContactList.Owner.PersonalMessage.Message != null ? ContactList.Owner.PersonalMessage.Message : String.Empty);

        }

        /// <summary>
        /// Sets personal message.
        /// </summary>
        /// <param name="pmsg"></param>
        internal void SetPersonalMessage(PersonalMessage pmsg)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            MessageProcessor.SendMessage(new NSPayLoadMessage("UUX", pmsg.Payload));

            StorageService.UpdateProfile(ContactList.Owner.Name, pmsg.Message);
        }

        internal void SetEndPointCapabilities()
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            string xmlstr = "<EndpointData><Capabilities>" +
                ((long)ContactList.Owner.ClientCapacities).ToString() + ":" + ((long)ContactList.Owner.ClientCapacitiesEx).ToString()
            + "</Capabilities></EndpointData>";

            MessageProcessor.SendMessage(new NSPayLoadMessage("UUX", xmlstr));
        }

        #endregion

        #region SetPrivacyMode & SetNotifyPrivacyMode & SetPresenceStatus

        /// <summary>
        /// Set the contactlist owner's privacy mode.
        /// </summary>
        /// <param name="privacy">New privacy setting</param>
        internal void SetPrivacyMode(PrivacyMode privacy)
        {
            if (privacy == PrivacyMode.AllExceptBlocked)
                MessageProcessor.SendMessage(new NSMessage("BLP", new string[] { "AL" }));

            if (privacy == PrivacyMode.NoneButAllowed)
                MessageProcessor.SendMessage(new NSMessage("BLP", new string[] { "BL" }));

        }

        /// <summary>
        /// Set the contactlist owner's notification mode on contact service.
        /// </summary>
        /// <param name="privacy">New notify privacy setting</param>
        internal void SetNotifyPrivacyMode(NotifyPrivacy privacy)
        {
            ContactList.Owner.SetNotifyPrivacy(privacy);
            if (AutoSynchronize)
            {
                ContactService.UpdateMe();
            }
        }

        /// <summary>
        /// Set the status of the contact list owner (the client).
        /// </summary>
        /// <remarks>You can only set the status _after_ SignedIn event. Otherwise you won't receive online notifications from other clients or the connection is closed by the server.</remarks>
        /// <param name="status"></param>
        internal void SetPresenceStatus(PresenceStatus status)
        {
            // check whether we are allowed to send a CHG command
            if (IsSignedIn == false)
                throw new MSNPSharpException("Can't set status. You must wait for the SignedIn event before you can set an initial status.");

            string context = String.Empty;
            bool isSetDefault = false;

            if (ContactList.Owner.DisplayImage != null)
                context = ContactList.Owner.DisplayImage.Context;

            if (status == PresenceStatus.Offline)
            {
                messageProcessor.Disconnect();
            }
            else if (status != ContactList.Owner.Status)
            {
                string capacities = String.Empty;

                if (ContactList.Owner.ClientCapacities == ClientCapacities.None)
                {
                    isSetDefault = true;

                    //don't set the same status or it will result in disconnection
                    ContactList.Owner.ClientCapacities = defaultClientCapacities;

                    if (BotMode)
                    {
                        ContactList.Owner.ClientCapacities |= ClientCapacities.IsBot;
                    }

                    ContactList.Owner.ClientCapacitiesEx = defaultClientCapacitiesEx;

                    SetEndPointCapabilities();
                    SetPresenceStatusUUX(status);

                    if (AutoSynchronize)
                    {
                        // Send BLP
                        SetPrivacyMode(ContactList.Owner.Privacy);
                    }

                    SetPersonalMessage(ContactList.Owner.PersonalMessage);

                    // Set screen name
                    SetScreenName(ContactList.Owner.Name);
                }

                ClientCapacitiesEx capsext = ContactList.Owner.ClientCapacitiesEx;
                capacities = ((long)ContactList.Owner.ClientCapacities).ToString() + ":" + ((long)capsext).ToString();

                if (!isSetDefault)
                {
                    //Well, only update the status after receiving the CHG command is right. However,
                    //we need to send UUX before CHG.

                    SetPresenceStatusUUX(status);
                }

                MessageProcessor.SendMessage(new NSMessage("CHG", new string[] { ParseStatus(status), capacities, context }));
            }
        }

        internal void SetPresenceStatusUUX(PresenceStatus status)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            MessageProcessor.SendMessage(new NSPayLoadMessage("UUX",
                "<PrivateEndpointData>" +
                "<EpName>" + MSNHttpUtility.XmlEncode(ContactList.Owner.EpName) + "</EpName>" +
                "<Idle>" + ((status == PresenceStatus.Idle) ? "true" : "false") + "</Idle>" +
                "<ClientType>1</ClientType>" +
                "<State>" + ParseStatus(status) + "</State>" +
                "</PrivateEndpointData>")
            );
        }

        internal void BroadCastStatus()
        {
            // check whether we are allowed to send a CHG command
            if (IsSignedIn == false)
                throw new MSNPSharpException("Can't set status. You must wait for the SignedIn event before you can set an initial status.");

            string context = String.Empty;

            if (ContactList.Owner.DisplayImage != null)
                context = ContactList.Owner.DisplayImage.Context;

            ClientCapacitiesEx capsext = ContactList.Owner.ClientCapacitiesEx;
            string capacities = ((long)ContactList.Owner.ClientCapacities).ToString() + ":" + ((long)capsext).ToString();

            MessageProcessor.SendMessage(new NSMessage("CHG", new string[] { ParseStatus(ContactList.Owner.Status), capacities, context }));
        }


        #endregion

        #region Circle

        internal void SendBlockCircleNSCommands(Guid circleId, string hostDomain)
        {
            SendCircleNotifyRML(circleId, hostDomain, MSNLists.AllowedList, true);
            SendCircleNotifyADL(circleId, hostDomain, MSNLists.BlockedList, true);
        }

        internal void SendUnBlockCircleNSCommands(Guid circleId, string hostDomain)
        {
            SendCircleNotifyRML(circleId, hostDomain, MSNLists.BlockedList, true);
            SendCircleNotifyADL(circleId, hostDomain, MSNLists.AllowedList, true);
        }

        /// <summary>
        /// Send a PUT command notifying the server we join to a circle(gruop) conversation.
        /// </summary>
        /// <param name="circleId"></param>
        /// <param name="hostDomain"></param>
        internal void JoinCircleConversation(Guid circleId, string hostDomain)
        {
            //Send PUT
            string from = ((int)ContactList.Owner.ClientType).ToString() + ":" + ContactList.Owner.Mail + ";epid=" + ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();
            string to = ((int)ClientType.CircleMember).ToString() + ":" + circleId.ToString().ToLowerInvariant() + "@" + hostDomain;

            string routingInfo = CircleString.RoutingScheme.Replace(CircleString.ToReplacementTag, to);
            routingInfo = routingInfo.Replace(CircleString.FromReplacementTag, from);

            string reliabilityInfo = CircleString.ReliabilityScheme.Replace(CircleString.StreamReplacementTag, "0");
            reliabilityInfo = reliabilityInfo.Replace(CircleString.SegmentReplacementTag, "0");

            string messageInfo = CircleString.PUTCircleReplyMessageScheme;
            string replyXML = CircleString.PUTPayloadXMLScheme.Replace(CircleString.OwnerReplacementTag, ContactList.Owner.Mail);
            messageInfo = messageInfo.Replace(CircleString.ContentLengthReplacementTag, replyXML.Length.ToString());
            messageInfo = messageInfo.Replace(CircleString.XMLReplacementTag, replyXML);

            string putCommandString = CircleString.CircleMessageScheme;
            putCommandString = putCommandString.Replace(CircleString.RoutingSchemeReplacementTag, routingInfo);
            putCommandString = putCommandString.Replace(CircleString.ReliabilitySchemeReplacementTag, reliabilityInfo);
            putCommandString = putCommandString.Replace(CircleString.MessageSchemeReplacementTag, messageInfo);

            NSPayLoadMessage nsMessage = new NSPayLoadMessage("PUT", putCommandString);
            MessageProcessor.SendMessage(nsMessage);
        }

        internal int SendCircleNotifyADL(Guid circleId, string hostDomain, MSNLists lists, bool blockUnBlock)
        {
            string payload = "<ml" + (blockUnBlock == true ? "" : " l=\"1\"") + "><d n=\""
            + hostDomain + "\"><c n=\"" + circleId.ToString("D") + "\" l=\"" +
            ((int)lists).ToString() + "\" t=\"" +
            ((int)ClientType.CircleMember).ToString() + "\"/></d></ml>";

            NSPayLoadMessage nsMessage = new NSPayLoadMessage("ADL", payload);
            MessageProcessor.SendMessage(nsMessage);
            return nsMessage.TransactionID;
        }

        internal int SendCircleNotifyRML(Guid circleId, string hostDomain, MSNLists lists, bool blockUnBlock)
        {
            string payload = "<ml" + (blockUnBlock == true ? "" : " l=\"1\"") + "><d n=\""
            + hostDomain + "\"><c n=\"" + circleId.ToString("D") + "\" l=\"" +
            ((int)lists).ToString() + "\" t=\"" +
            ((int)ClientType.CircleMember).ToString() + "\"/></d></ml>";

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
            if (IsSignedIn)
                OnSignedOff(new SignedOffEventArgs(SignedOffReason.None));

            Clear();
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

            Clear();
            SendInitialMessage();
        }

        /// <summary>
        /// Send the first message to the server.
        /// </summary>
        protected virtual void SendInitialMessage()
        {
            // VER: MSN Protocol used

            (MessageProcessor as NSMessageProcessor).ResetTransactionID();

            MessageProcessor.SendMessage(new NSMessage("VER", new string[] { "MSNP18", "CVR0" }));
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
            MsnProtocol msnProtocol = (MsnProtocol)Convert.ToInt32(message.CommandValues[1].ToString().Substring("MSNP".Length, 2));
            Credentials oldcred = Credentials;
            Credentials = new Credentials(oldcred.Account, oldcred.Password, msnProtocol);

            // CVR: Send client information back
            MessageProcessor.SendMessage(new NSMessage("CVR",
                new string[] { 
                    "0x040c", //The LCIDs in .net framework are different from Windows API: "0x" + CultureInfo.CurrentCulture.LCID.ToString("x4")
                    "winnt",
                    "5.1",
                    "i386",
                    Credentials.ClientInfo.MessengerClientName, 
                    Credentials.ClientInfo.MessengerClientBuildVer,
                    Credentials.ClientInfo.MessengerClientBrand,
                    Credentials.Account
                })
           );
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
            // USR: Begin login procedure
            MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "SSO", "I", Credentials.Account }));
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
            if ((string)message.CommandValues[1] == "SSO")
            {
                string policy = (string)message.CommandValues[3];
                string nonce = (string)message.CommandValues[4];

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
            else if ((string)message.CommandValues[1] == "OK")
            {
                // we sucesfully logged in, set the owner's name
                if (ContactList.Owner == null)
                {
                    ContactList.SetOwner(new Owner(WebServiceConstants.MessengerIndividualAddressBookId, message.CommandValues[2].ToString(), this));
                }
                ContactList.Owner.PassportVerified = message.CommandValues[3].Equals("1");
            }
        }

        /// <summary>
        /// Fires the <see cref="SignedIn"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnSignedIn(EventArgs e)
        {
            isSignedIn = true;

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
            ContactList.Owner.SetStatus(PresenceStatus.Offline);
            Clear();

            if (SignedOff != null)
                SignedOff(this, e);

            if (messageProcessor != null)
                messageProcessor.Disconnect();
        }

        /// <summary>
        /// Fires the <see cref="ContactStatusChanged"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnContactStatusChanged(ContactStatusChangedEventArgs e)
        {
            if (ContactStatusChanged != null)
                ContactStatusChanged(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ContactOffline"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnContactOffline(ContactEventArgs e)
        {
            if (ContactOffline != null)
                ContactOffline(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ContactOnline"/> event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnContactOnline(ContactEventArgs e)
        {
            if (ContactOnline != null)
                ContactOnline(this, e);
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
        /// Called when a UBX command has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnUBXReceived(NSMessage message)
        {
            //check the payload length
            if (message.CommandValues[1].ToString() == "0")
                return;

            string fullaccount = message.CommandValues[0].ToString(); // 1:username@hotmail.com;via=9:guid@live.com

            string account = string.Empty;
            ClientType type = ClientType.PassportMember;
            bool isCircleMember = false;
            Circle circle = null;

            if (fullaccount.Contains(CircleString.ViaCircleGroupSplitter))
            {
                string[] usernameAndCircle = fullaccount.Split(';');
                type = (ClientType)int.Parse(usernameAndCircle[0].Split(':')[0]);
                account = usernameAndCircle[0].Split(':')[1].ToLowerInvariant();

                string circleMail = usernameAndCircle[1].Substring("via=9:".Length);

                circle = CircleList[circleMail];
                if (circle == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnUBXReceived] Cannot retrieve circle for user: " + fullaccount);
                    return;
                }
                else
                {
                    if (!circle.HasMember(fullaccount, AccountParseOption.ParseAsFullCircleAccount))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnUBXReceived] Cannot retrieve user for from circle: " + fullaccount);
                        return;
                    }
                }

                isCircleMember = true;
            }
            else
            {
                type = (ClientType)int.Parse(fullaccount.Split(':')[0]);
                account = fullaccount.Split(':')[1].ToLowerInvariant();
            }

            if (message.InnerBody != null &&
                account.ToLowerInvariant() == ContactList.Owner.Mail.ToLowerInvariant() && 
                type == ClientType.PassportMember &&
                (!isCircleMember))
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(new MemoryStream(message.InnerBody));
                    XmlNodeList privateendpoints = xmlDoc.GetElementsByTagName("PrivateEndpointData");

                    if (privateendpoints.Count > 0)
                    {
                        Guid lastSignedInPlace = NSMessageHandler.MachineGuid; // For AutoLogoff
                        Dictionary<Guid, string> newPlaces = new Dictionary<Guid, string>(privateendpoints.Count);
                        foreach (XmlNode pepdNode in privateendpoints)
                        {
                            Guid id = new Guid(pepdNode.Attributes["id"].Value);
                            String epname = (pepdNode["EpName"] == null) ? String.Empty : pepdNode["EpName"].InnerText;

                            newPlaces[id] = epname;
                            lastSignedInPlace = id;

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Place: " + epname + " " + id, GetType().Name);
                        }

                        if (newPlaces.Count > 1 && ContactList.Owner.MPOPMode == MPOP.AutoLogoff)
                        {
                            foreach (KeyValuePair<Guid, string> keyvalue in newPlaces)
                            {
                                if (keyvalue.Key != NSMessageHandler.MachineGuid &&
                                    keyvalue.Key != lastSignedInPlace)
                                {
                                    ContactList.Owner.SignoutFrom(keyvalue.Key);
                                }
                            }
                        }

                        ContactList.Owner.Places.Clear();
                        ContactList.Owner.Places = newPlaces;

                        if (ContactList.Owner.MPOPMode == MPOP.AutoLogoff &&
                            newPlaces.Count > 1 &&
                            lastSignedInPlace != NSMessageHandler.MachineGuid)
                        {
                            // No owner.Status = PresenceStatus.Offline, because we haven't signed in yet.
                            ContactList.Owner.SignoutFrom(NSMessageHandler.MachineGuid);
                            return;
                        }
                    }
                }
                catch (Exception xmlex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnUBXReceived] Xml parse error: " + xmlex.Message);
                }

                ContactList.Owner.SetPersonalMessage(new PersonalMessage(message));
            }
            else
            {
                
                Contact contact = null;

                if (!isCircleMember)
                {
                    contact = ContactList.GetContact(account, type);
                }
                else
                {
                    contact = circle.ContactList.GetContact(account, type);
                }

                if (contact == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Set contact info failed by UBX command: Cannot get contact: " + fullaccount);

                    return;
                }

                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(new MemoryStream(message.InnerBody));

                    XmlNode friendlyNameNode = xmlDoc.SelectSingleNode(@"//Data/FriendlyName");
                    if (friendlyNameNode != null)
                    {
                        contact.SetName(friendlyNameNode.InnerXml == "" ? contact.Mail : friendlyNameNode.InnerXml);
                    }
                }
                catch (Exception xmlex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[OnUBXReceived] Xml parse error: " + xmlex.Message);
                }

                contact.SetPersonalMessage(new PersonalMessage(message));
            }
        }

        /// <summary>
        /// Called when a NLN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact on the forward list went online.
        /// <code>NLN [status] [clienttype:account] [name] [clientcapacities:48] [displayimage] (MSNP18)</code>
        /// <code>NLN [status] [account] [clienttype] [name] [clientcapacities:0] [displayimage] (MSNP16)</code>
        /// <code>NLN [status] [account] [clienttype] [name] [clientcapacities] [displayimage] (MSNP15)</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnNLNReceived(NSMessage message)
        {
            PresenceStatus newstatus = ParseStatus(message.CommandValues[0].ToString());

            ClientType type;
            string account;
            string fullaccount = message.CommandValues[1].ToString(); // 1:username@hotmail.com;via=9:guid@live.com
            Contact contact = null;
            Circle circle = null;
            ClientCapacities newcaps = ClientCapacities.None;
            ClientCapacitiesEx newcapsex = ClientCapacitiesEx.None;

            if (message.CommandValues.Count >= 4)
            {
                if (message.CommandValues[3].ToString().Contains(":"))
                {
                    newcaps = (ClientCapacities)Convert.ToInt64(message.CommandValues[3].ToString().Split(':')[0]);
                    newcapsex = (ClientCapacitiesEx)Convert.ToInt64(message.CommandValues[3].ToString().Split(':')[1]);
                }
                else
                {
                    newcaps = (ClientCapacities)Convert.ToInt64(message.CommandValues[3].ToString());
                }

            }

            if (fullaccount.Contains(CircleString.ViaCircleGroupSplitter))
            {
                #region Circle Status, or Circle Member status

                string[] usernameAndCircle = fullaccount.Split(';');
                type = (ClientType)int.Parse(usernameAndCircle[0].Split(':')[0]);
                account = usernameAndCircle[0].Split(':')[1].ToLowerInvariant();

                string circleMail = usernameAndCircle[1].Substring("via=9:".Length);
                circle = CircleList[circleMail];

                string capabilityString = message.CommandValues[3].ToString();

                if (capabilityString != "0:0")  //This is NOT a circle's presence status.
                {
                    if (circle == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnNLNReceived] Cannot update status for user, circle not found: " + fullaccount);
                        return;
                    }

                    if (!circle.HasMember(fullaccount, AccountParseOption.ParseAsFullCircleAccount))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnNLNReceived] User not found in the specific circle: " + fullaccount + ", a contact was created by NSCommand.");
                    }

                    contact = circle.ContactList.GetContact(account, type);

                    contact.SetName(HttpUtility.UrlDecode(message.CommandValues[2].ToString()));
                    contact.ClientCapacities = newcaps;
                    contact.ClientCapacitiesEx = newcapsex;

                    PresenceStatus oldStatus = contact.Status;

                    if (oldStatus != newstatus)
                    {
                        contact.SetStatus(newstatus);

                        // The contact changed status
                        OnCircleMemberStatusChanged(new CircleMemberStatusChanged(circle, contact, oldStatus));

                        // The contact goes online
                        OnCircleMemberOnline(new CircleMemberEventArgs(circle, contact));
                    }
                }
                else
                {
                    if (account == ContactList.Owner.Mail.ToLowerInvariant())
                    {
                        if (circle == null)
                            return;

                        PresenceStatus oldCircleStatus = circle.Status;
                        string[] guidDomain = circleMail.Split('@');

                        if (guidDomain.Length != 0 && (oldCircleStatus == PresenceStatus.Offline || oldCircleStatus == PresenceStatus.Hidden))
                        {
                            //This is a PUT command send from server when we login.
                            JoinCircleConversation(new Guid(guidDomain[0]), guidDomain[1]);
                        }

                        circle.SetStatus(newstatus);

                        OnCircleStatusChanged(new CircleStatusChangedEventArgs(circle, oldCircleStatus));

                        OnCircleOnline(new CircleEventArgs(circle));

                        return;
                    }
                }

                #endregion
            }
            else
            {
                type = (ClientType)int.Parse(fullaccount.Split(':')[0]);
                account = fullaccount.Split(':')[1].ToLowerInvariant();
                contact = ContactList.GetContact(account, type);

                #region Contact Status

                if (contact != null)
                {
                    string newname = (message.CommandValues.Count >= 3) ? message.CommandValues[2].ToString() : String.Empty;
                    string newdp = message.CommandValues.Count >= 5 ? message.CommandValues[4].ToString() : String.Empty;


                    if (IsSignedIn && account == ContactList.Owner.Mail.ToLowerInvariant() && type == ClientType.PassportMember)
                    {
                        SetPresenceStatus(newstatus);
                        return;
                    }

                    contact.SetName(HttpUtility.UrlDecode(newname));
                    contact.ClientCapacities = newcaps;
                    contact.ClientCapacitiesEx = newcapsex;

                    if (contact != ContactList.Owner && !String.IsNullOrEmpty(newdp) && newdp != "0")
                    {
                        DisplayImage userDisplay = new DisplayImage();
                        userDisplay.Context = newdp;
                        contact.DisplayImage = userDisplay;
                    }

                    PresenceStatus oldStatus = contact.Status;
                    contact.SetStatus(newstatus);

                    // The contact changed status
                    OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, oldStatus));

                    // The contact goes online
                    OnContactOnline(new ContactEventArgs(contact));
                }
                #endregion
            }
        }

        /// <summary>
        /// Called when a FLN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a user went offline.
        /// <code>FLN [clienttype:account] [caps:0] [networkpng] (MSNP18)</code>
        /// <code>FLN [account] [clienttype] [caps:0] [networkpng] (MSNP16)</code>
        /// <code>FLN [account] [clienttype] [caps] [networkpng] (MSNP15)</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnFLNReceived(NSMessage message)
        {
            ClientType type;
            string account;
            string fullaccount = message.CommandValues[0].ToString(); // 1:username@hotmail.com;via=9:guid@live.com
            Contact contact = null;
            Circle circle = null;
            ClientCapacities oldcaps = ClientCapacities.None;
            ClientCapacitiesEx oldcapsex = ClientCapacitiesEx.None;

            if (message.CommandValues.Count >= 2)
            {
                if (message.CommandValues[1].ToString().Contains(":"))
                {
                    oldcaps = (ClientCapacities)Convert.ToInt64(message.CommandValues[1].ToString().Split(':')[0]);
                    oldcapsex = (ClientCapacitiesEx)Convert.ToInt64(message.CommandValues[1].ToString().Split(':')[1]);
                }
                else
                {
                    oldcaps = (ClientCapacities)Convert.ToInt64(message.CommandValues[1].ToString());
                }
            }


            if (fullaccount.Contains(CircleString.ViaCircleGroupSplitter))
            {
                #region Circle and CircleMemberStatus

                string[] usernameAndCircle = fullaccount.Split(';');
                type = (ClientType)int.Parse(usernameAndCircle[0].Split(':')[0]);
                account = usernameAndCircle[0].Split(':')[1].ToLowerInvariant();

                string circleMail = usernameAndCircle[1].Substring("via=9:".Length);
                circle = CircleList[circleMail];

                if (circle == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                "[OnFLNReceived] Cannot update status for user since circle not found: " + fullaccount);
                    return;
                }

                if (account == ContactList.Owner.Mail.ToLowerInvariant())  //Circle status
                {
                    string capabilityString = message.CommandValues[1].ToString();
                    if (capabilityString == "0:0")  //This is a circle's presence status.
                    {

                        PresenceStatus oldCircleStatus = circle.Status;
                        circle.SetStatus(PresenceStatus.Offline);

                        if (CircleStatusChanged != null)
                            CircleStatusChanged(this, new CircleStatusChangedEventArgs(circle, oldCircleStatus));

                        if (CircleOffline != null)
                            CircleOffline(this, new CircleEventArgs(circle));
                    }

                    return;
                }
                else
                {
                    if (!circle.HasMember(fullaccount, AccountParseOption.ParseAsFullCircleAccount))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                    "[OnFLNReceived] Cannot update status for user since user not found in the specific circle: " + fullaccount);
                        return;
                    }

                    contact = circle.ContactList.GetContact(account, type);
                    PresenceStatus oldStatus = contact.Status;

                    if (oldStatus != PresenceStatus.Offline)
                    {
                        contact.SetStatus(PresenceStatus.Offline);

                        // The contact changed status
                        OnCircleMemberStatusChanged(new CircleMemberStatusChanged(circle, contact, oldStatus));

                        // The contact goes online
                        OnCircleMemberOffline(new CircleMemberEventArgs(circle, contact));
                    }

                    return;
                } 

                #endregion
            }
            else
            {
                type = (ClientType)int.Parse(fullaccount.Split(':')[0]);
                account = fullaccount.Split(':')[1].ToLowerInvariant();

                contact = (account == ContactList.Owner.Mail.ToLowerInvariant() && type == ClientType.PassportMember)
                ? ContactList.Owner : ContactList.GetContact(account, type);

                #region Contact Staus

                if (contact != null)
                {
                    

                    PresenceStatus oldStatus = contact.Status;
                    contact.SetStatus(PresenceStatus.Offline);

                    // the contact changed status
                    OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, oldStatus));

                    // the contact goes offline
                    OnContactOffline(new ContactEventArgs(contact));
                }

                #endregion
            }
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

        /// <summary>
        /// Called when a UBN command has been received.
        /// </summary>
        /// <remarks>
        /// <code>UBN [account;{GUID}] [1:xml data,2:sip invite, 3: MSNP2P SLP data, 4:logout, 10: unknown] [PayloadLegth]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUBNReceived(NSMessage message)
        {
            NetworkMessage networkMessage = message as NetworkMessage;
            if (message.InnerBody != null)
            {
                switch (message.CommandValues[1].ToString())
                {
                    case "3":
                        {
                            SLPMessage slpMessage = SLPMessage.Parse(message.InnerBody);
                            if (slpMessage.ContentType == "application/x-msnmsgr-transreqbody")
                            {
                                SLPStatusMessage slpResponseMessage = new SLPStatusMessage(slpMessage.FromMail, 200, "OK");
                                slpResponseMessage.FromMail = slpMessage.ToMail;
                                slpResponseMessage.Via = slpMessage.Via;
                                slpResponseMessage.CSeq = slpMessage.CSeq;
                                slpResponseMessage.CallId = slpMessage.CallId;
                                slpResponseMessage.MaxForwards = slpMessage.MaxForwards;
                                slpResponseMessage.ContentType = @"application/x-msnmsgr-transrespbody";

                                slpResponseMessage.BodyValues["Listening"] = "false";
                                slpResponseMessage.BodyValues["Conn-Type"] = "Firewall";
                                slpResponseMessage.BodyValues["TCP-Conn-Type"] = "Firewall";
                                slpResponseMessage.BodyValues["IPv6-global"] = string.Empty;
                                slpResponseMessage.BodyValues["UPnPNat"] = "false";
                                slpResponseMessage.BodyValues["Capabilities-Flags"] = "1";
                                slpResponseMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Resp";
                                slpResponseMessage.BodyValues["Bridge"] = "TCPv1";

                                if (slpMessage.BodyValues.ContainsKey("Hashed-Nonce"))
                                {
                                    slpResponseMessage.BodyValues["Hashed-Nonce"] = slpMessage.BodyValues["Hashed-Nonce"].Value;
                                }
                                else
                                {
                                    slpResponseMessage.BodyValues["Hashed-Nonce"] = Guid.Empty.ToString("B");
                                }

                                byte[] slpBytes = slpResponseMessage.GetBytes();
                                byte[] slpTextBytes = new byte[slpBytes.Length - 1];
                                Array.Copy(slpBytes, slpTextBytes, slpTextBytes.Length);

                                NSPayLoadMessage uunResponse = new NSPayLoadMessage("UUN", new string[] { message.CommandValues[0].ToString(), "3" },
                                    System.Text.Encoding.UTF8.GetString(slpTextBytes));

                                MessageProcessor.SendMessage(uunResponse);
                            }
                            else if (slpMessage.ContentType == "application/x-msnmsgr-transrespbody")
                            {
                                SLPRequestMessage slpResponseMessage = new SLPRequestMessage(slpMessage.FromMail, "ACK");
                                slpResponseMessage.FromMail = slpMessage.ToMail;
                                slpResponseMessage.Via = slpMessage.Via;
                                slpResponseMessage.CSeq = 0;
                                slpResponseMessage.CallId = Guid.Empty;
                                slpResponseMessage.MaxForwards = 0;
                                slpResponseMessage.ContentType = @"application/x-msnmsgr-transdestaddrupdate";

                                slpResponseMessage.BodyValues["stroPdnAsrddAlanretnI4vPI"] = "0435:001.2.861.291";
                                slpResponseMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Updated-Connecting-Port";

                                byte[] slpBytes = slpResponseMessage.GetBytes();
                                byte[] slpTextBytes = new byte[slpBytes.Length - 1];
                                Array.Copy(slpBytes, slpTextBytes, slpTextBytes.Length);

                                NSPayLoadMessage uunResponse = new NSPayLoadMessage("UUN", new string[] { message.CommandValues[0].ToString(), "3" },
                                    System.Text.Encoding.UTF8.GetString(slpTextBytes));

                                MessageProcessor.SendMessage(uunResponse);
                            }


                            break;
                        }
                    case "4":
                    case "8":
                        {
                            string logoutMsg = Encoding.UTF8.GetString(message.InnerBody);
                            if (logoutMsg.StartsWith("goawyplzthxbye") || logoutMsg == "gtfo")
                            {
                                if (messageProcessor != null)
                                    messageProcessor.Disconnect();
                            }
                            return;
                        }
                }
            }
        }

        /// <summary>
        /// Called when a UUN command has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnUUNReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a UUX command has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnUUXReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a CHG command has been received.
        /// </summary>
        /// <remarks>
        /// The notification server has confirmed our request for a status change. 
        /// This function sets the status of the contactlist owner.
        /// <code>CHG [Transaction] [Status]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnCHGReceived(NSMessage message)
        {
            ContactList.Owner.SetStatus(ParseStatus((string)message.CommandValues[1]));
        }

        #endregion

        #region Switchboard

        /// <summary>
        /// Class used for items stored in the switchboard queue.
        /// </summary>
        protected class SwitchboardQueueItem
        {
            /// <summary>
            /// The object that initiated the request.
            /// </summary>
            public object Initiator;
            /// <summary>
            /// The switchboard handler that will be handling the new switchboard session.
            /// </summary>
            public SBMessageHandler SwitchboardHandler;

            /// <summary>
            /// Constructs a queue item.
            /// </summary>
            /// <param name="switchboardHandler"></param>
            /// <param name="initiator"></param>
            public SwitchboardQueueItem(SBMessageHandler switchboardHandler, object initiator)
            {
                Initiator = initiator;
                SwitchboardHandler = switchboardHandler;
            }
        }

        /// <summary>
        /// Gets a new switchboard handler object. Called when a remote client initiated the switchboard.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="sessionHash"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        protected virtual IMessageHandler CreateSBHandler(IMessageProcessor processor, string sessionHash, int sessionId)
        {
            SBMessageHandler handler = new SBMessageHandler();
            handler.MessageProcessor = processor;
            handler.NSMessageHandler = this;
            handler.SetInvitation(sessionHash, sessionId);

            return handler;
        }

        /// <summary>
        /// Fires the <see cref="SBCreated"/> event.
        /// </summary>
        /// <param name="switchboard">The switchboard created</param>
        /// <param name="initiator">The object that initiated the switchboard request.</param>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <param name="anonymous">Indecates that whether it is an anonymous request.</param>
        protected virtual void OnSBCreated(SBMessageHandler switchboard, object initiator, string account, string name, bool anonymous)
        {
            if (SBCreated != null)
                SBCreated(this, new SBCreatedEventArgs(switchboard, initiator, account, name, anonymous));
        }

        /// <summary>
        /// Called when a RNG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the user receives a switchboard session (chatsession) request. A connection to the switchboard will be established
        /// and the corresponding events and objects are created.
        /// <code>RNG [Session] [IP:Port] 'CKI' [Hash] [Account] [Name] U messenger.msn.com 1</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnRNGReceived(NSMessage message)
        {
            SBMessageProcessor processor = new SBMessageProcessor();

            // set new connectivity settings
            ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
            string[] values = ((string)message.CommandValues[1]).Split(new char[] { ':' });

            newSettings.Host = values[0];
            newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
            processor.ConnectivitySettings = newSettings;

            // create a switchboard object
            SBMessageHandler handler = (SBMessageHandler)CreateSBHandler(processor, message.CommandValues[3].ToString(), int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture));

            processor.RegisterHandler(handler);

            // start connecting
            processor.Connect();

            // notify the client
            string account = string.Empty;
            string name = string.Empty;
            bool anonymous = false;

            if (message.CommandValues.Count >= 5)
            {
                account = message.CommandValues[4].ToString();
            }

            if (message.CommandValues.Count >= 6)
            {
                name = message.CommandValues[5].ToString();
            }

            Contact callingContact = ContactList.GetContact(account, ClientType.PassportMember);
            if (callingContact.Lists == MSNLists.None)
            {
                anonymous = true;
            }

            OnSBCreated(handler, null, account, name, anonymous);
        }

        /// <summary>
        /// Called when a XFR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the notification server has send us the location of a switch-board server in order to
        /// make contact with a client, or that we must switch to a new notification server.
        /// <code>XFR [Transaction] [SB|NS] [IP:Port] ['0'|'CKI'] [Hash|CurrentIP:CurrentPort]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnXFRReceived(NSMessage message)
        {
            if ((string)message.CommandValues[1] == "NS")
            {
                // switch to a new notification server. That means reconnecting our current message processor.
                SocketMessageProcessor processor = (SocketMessageProcessor)MessageProcessor;

                // disconnect first
                processor.Disconnect();

                // set new connectivity settings
                ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
                string[] values = ((string)message.CommandValues[2]).Split(new char[] { ':' });

                newSettings.Host = values[0];
                newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);

                processor.ConnectivitySettings = newSettings;

                // and reconnect. The login procedure will now start over again
                processor.Connect();
            }
            if ((string)message.CommandValues[1] == "SB")
            {
                if (pendingSwitchboards.Count > 0)
                {
                    if (ContactList.Owner.Status == PresenceStatus.Offline)
                        System.Diagnostics.Trace.WriteLine("Owner not yet online!", "NS15MessageHandler");

                    SBMessageProcessor processor = new SBMessageProcessor();
                    SwitchboardQueueItem queueItem = (SwitchboardQueueItem)pendingSwitchboards.Dequeue();

                    // set new connectivity settings
                    ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
                    string[] values = ((string)message.CommandValues[2]).Split(new char[] { ':' });

                    newSettings.Host = values[0];
                    newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Switchboard connectivity settings: " + newSettings.ToString(), GetType().Name);

                    processor.ConnectivitySettings = newSettings;

                    // set the switchboard objects with the processor values
                    // SBMessageHandler handler = (SBMessageHandler)CreateSBHandler(processor, message.CommandValues[4].ToString());
                    string sessionHash = message.CommandValues[4].ToString();
                    queueItem.SwitchboardHandler.SetInvitation(sessionHash);
                    queueItem.SwitchboardHandler.MessageProcessor = processor;
                    queueItem.SwitchboardHandler.NSMessageHandler = this;

                    // register this handler so the switchboard can respond
                    processor.RegisterHandler(queueItem.SwitchboardHandler);

                    // notify the client
                    OnSBCreated(queueItem.SwitchboardHandler, queueItem.Initiator, string.Empty, string.Empty, false);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "SB created event handler called", GetType().Name);

                    // start connecting
                    processor.Connect();

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Opening switchboard connection...", GetType().Name);
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Switchboard request received, but no pending switchboards available.", GetType().Name);
                }
            }
        }

        protected virtual void OnSBSReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a UBM command has been received, this message was sent by a Yahoo Messenger client.
        /// </summary>
        /// <remarks>
        /// Indicates that the notification server has send us a UBM. This is usually a message from Yahoo Messenger.
        /// <code>UBM [Remote user account] 32 [Destination user account] [3(nudge) or 2(typing) or 1(text message)] [Length]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUBMReceived(NSMessage message)
        {
            string sender = message.CommandValues[0].ToString();
            NSMessage messageClone = ((ICloneable)message).Clone() as NSMessage;  //Always pass the clone ones into messagehandler.

            if (sender == null)
            {
                sender = message.CommandValues[0].ToString();
            }

            YIMMessage msg = new YIMMessage(message, Credentials.MsnProtocol);

            if (msg.CommandValues.Count > 2)
            {
                if (msg.CommandValues[1].ToString() == ((int)ClientType.EmailMember).ToString())  //Verify sender.
                {
                    if (msg.CommandValues.Count > 3)
                    {
                        //Verify receiver.
                        if (msg.CommandValues[2].ToString() != ContactList.Owner.Mail ||
                            msg.CommandValues[3].ToString() != ((int)ContactList.Owner.ClientType).ToString())
                        {
                            return;
                        }
                    }

                    if ((!msg.InnerMessage.MimeHeader.ContainsKey(MimeHeaderStrings.TypingUser))   //filter the typing message
                        && ContactList.HasContact(sender, ClientType.EmailMember))
                    {
                        lock (P2PHandler.SwitchboardSessions)
                        {
                            foreach (SBMessageHandler sbMessageHandler in P2PHandler.SwitchboardSessions)
                            {
                                if (sbMessageHandler is YIMMessageHandler)
                                {
                                    YIMMessageHandler YimHandler = sbMessageHandler as YIMMessageHandler;
                                    if (YimHandler.Contacts.ContainsKey(ContactList.GetContact(sender, ClientType.EmailMember)))
                                    {
                                        return;  //The handler have been registered, return.
                                    }
                                }
                            }
                        }

                        //YIMMessageHandler not found, we create a new one and register it.
                        YIMMessageHandler switchboard = new YIMMessageHandler();
                        switchboard.NSMessageHandler = this;

                        switchboard.MessageProcessor = MessageProcessor;
                        lock (P2PHandler.SwitchboardSessions)
                        {
                            P2PHandler.SwitchboardSessions.Add(switchboard);
                        }

                        messageProcessor.RegisterHandler(switchboard);
                        OnSBCreated(switchboard, null, sender, sender, false);

                        switchboard.HandleMessage(MessageProcessor, messageClone);
                    }
                }

                if (msg.CommandValues[1].ToString() == ((int)ClientType.PhoneMember).ToString())
                {
                    if (msg.CommandValues.Count > 3)
                    {
                        //Verify receiver.
                        if (msg.CommandValues[2].ToString() != ContactList.Owner.Mail ||
                            msg.CommandValues[3].ToString() != ((int)ContactList.Owner.ClientType).ToString())
                        {
                            return;
                        }
                    }

                    if (ContactList.HasContact(msg.CommandValues[0].ToString(),
                        ((ClientType)(int.Parse(msg.CommandValues[1].ToString())))))
                    {
                        Contact msgSender = ContactList.GetContact(msg.CommandValues[0].ToString(),
                        ((ClientType)(int.Parse(msg.CommandValues[1].ToString()))));

                        TextMessage txtmsg = new TextMessage();
                        txtmsg.CreateFromMessage(msg.InnerMessage);

                        TextMessageEventArgs e = new TextMessageEventArgs(txtmsg, msgSender);

                        OnMobileMessageReceived(e);

                    }
                }
            }
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
                notification.ReceiverAccount.ToLowerInvariant() == ContactList.Owner.Mail.ToLowerInvariant())
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
                    if (node.InnerText != "ABCHInternal") return;

                    node = xmlDoc.SelectSingleNode(@"//NotificationData/CID");
                    if (node == null) return;
                    if (node.InnerText != ContactList.Owner.CID.ToString()) return;

                    ContactService.ServerNotificationRequest(PartnerScenario.ABChangeNotifyAlert, null, null);
                }
                else
                {
                    node = xmlDoc.SelectSingleNode(@"//NotificationData/CircleId");
                    if (node == null) return;

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
        protected virtual void OnMSGReceived(MSNMessage message)
        {
            MSGMessage msgMessage = new MSGMessage(message);
            string mime = msgMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToString();

            if (mime.IndexOf("text/x-msmsgsprofile") >= 0)
            {
                StrDictionary hdr = msgMessage.MimeHeader;

                int clientPort = 0;

                if (hdr.ContainsKey("ClientPort"))
                {
                    clientPort = int.Parse(msgMessage.MimeHeader["ClientPort"].Replace('.', ' '), System.Globalization.CultureInfo.InvariantCulture);
                    clientPort = ((clientPort & 255) * 256) + ((clientPort & 65280) / 256);
                }

                IPAddress ip = hdr.ContainsKey("ClientIP") ? IPAddress.Parse(hdr["ClientIP"]) : IPAddress.None;
                ContactList.Owner.UpdateProfile(
                    hdr["LoginTime"],
                    (hdr.ContainsKey("EmailEnabled")) ? (Convert.ToInt32(hdr["EmailEnabled"]) == 1) : false,
                    hdr["MemberIdHigh"],
                    hdr["MemberIdLow"],
                    hdr["lang_preference"],
                    hdr["preferredEmail"],
                    hdr["country"],
                    hdr["PostalCode"],
                    hdr["Gender"],
                    hdr["Kid"],
                    hdr["Age"],
                    hdr["Birthday"],
                    hdr["Wallet"],
                    hdr["sid"],
                    hdr["kv"],
                    hdr["MSPAuth"],
                    ip,
                    clientPort,
                    hdr.ContainsKey("Nickname") ? hdr["Nickname"] : String.Empty,
                    hdr.ContainsKey("MPOPEnabled") && hdr["MPOPEnabled"] != "0",
                    hdr.ContainsKey("RouteInfo") ? hdr["RouteInfo"] : String.Empty
                );

                if (IPAddress.None != ip)
                {
                    // set the external end point. This can be used in file transfer connectivity determing
                    externalEndPoint = new IPEndPoint(ip, clientPort);
                }

                ContactService.SynchronizeContactList();
            }
            else if (mime.IndexOf("x-msmsgsemailnotification") >= 0)
            {
                message.InnerBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.InnerBody).Replace("\r\n\r\n", "\r\n"));
                msgMessage = new MSGMessage(message);

                OnMailNotificationReceived(new NewMailEventArgs(
                    (string)msgMessage.MimeHeader[MimeHeaderStrings.From],
                    (string)msgMessage.MimeHeader["Message-URL"],
                    (string)msgMessage.MimeHeader["Post-URL"],
                    (string)msgMessage.MimeHeader["Subject"],
                    (string)msgMessage.MimeHeader["Dest-Folder"],
                    (string)msgMessage.MimeHeader["From-Addr"],
                    msgMessage.MimeHeader.ContainsKey("id") ? int.Parse((string)msgMessage.MimeHeader["id"], System.Globalization.CultureInfo.InvariantCulture) : 0
                ));
            }
            else if (mime.IndexOf("x-msmsgsactivemailnotification") >= 0)
            {
                //Now this is the unread OIM info, not the new mail.
                message.InnerBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.InnerBody).Replace("\r\n\r\n", "\r\n"));
                msgMessage = new MSGMessage(message);

                OnMailChanged(new MailChangedEventArgs(
                    (string)msgMessage.MimeHeader["Src-Folder"],
                    (string)msgMessage.MimeHeader["Dest-Folder"],
                    msgMessage.MimeHeader.ContainsKey("Message-Delta") ? int.Parse((string)msgMessage.MimeHeader["Message-Delta"], System.Globalization.CultureInfo.InvariantCulture) : 0
                ));
            }
            else if (mime.IndexOf("x-msmsgsinitialemailnotification") >= 0)
            {
                OnMailboxStatusReceived(new MailboxStatusEventArgs(
                    msgMessage.MimeHeader.ContainsKey("Inbox-Unread") ? int.Parse((string)msgMessage.MimeHeader["Inbox-Unread"], System.Globalization.CultureInfo.InvariantCulture) : 0,
                    msgMessage.MimeHeader.ContainsKey("Folders-Unread") ? int.Parse((string)msgMessage.MimeHeader["Folders-Unread"], System.Globalization.CultureInfo.InvariantCulture) : 0,
                    (string)msgMessage.MimeHeader["Inbox-URL"],
                    (string)msgMessage.MimeHeader["Folders-URL"],
                    (string)msgMessage.MimeHeader["Post-URL"]
                ));
            }
            else if (mime.IndexOf("x-msmsgsinitialmdatanotification") >= 0 || mime.IndexOf("x-msmsgsoimnotification") >= 0)
            {
                message.InnerBody = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.InnerBody).Replace("\r\n\r\n", "\r\n"));
                msgMessage = new MSGMessage(message);


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

                string xmlstr = msgMessage.MimeHeader["Mail-Data"];
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
                                        (string)msgMessage.MimeHeader["Inbox-URL"],      //Invalid, I think it should be obsolated.
                                        (string)msgMessage.MimeHeader["Folders-URL"],    //Invalid, I think it should be obsolated.
                                        (string)msgMessage.MimeHeader["Post-URL"]
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

                OIMService.ProcessOIM(msgMessage, mime.IndexOf("x-msmsgsinitialmdatanotification") >= 0);
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

        /// <summary>
        /// Fires the <see cref="MobileMessageReceived"/> event.
        /// </summary>
        /// <remarks>Called when the owner has received a mobile message.</remarks>
        /// <param name="e"></param>
        protected virtual void OnMobileMessageReceived(TextMessageEventArgs e)
        {
            if (MobileMessageReceived != null)
            {
                MobileMessageReceived(this, e);
            }
        }

        #endregion

        #region Contact, Circle and Group

        /// <summary>
        /// Translates the codes used by the MSN server to a MSNList object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected virtual MSNLists GetMSNList(string name)
        {
            switch (name)
            {
                case "AL":
                    return MSNLists.AllowedList;
                case "FL":
                    return MSNLists.ForwardList;
                case "BL":
                    return MSNLists.BlockedList;
                case "RL":
                    return MSNLists.ReverseList;
                case "PL":
                    return MSNLists.PendingList;
            }
            throw new MSNPSharpException("Unknown MSNList type");
        }

        /// <summary>
        /// Called when a ADL command has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnADLReceived(NSMessage message)
        {
            if (message.TransactionID != 0 &&
                message.CommandValues[1].ToString() == "OK" &&
                ContactService.ProcessADL(message.TransactionID))
            {
            }
            else
            {
                NetworkMessage networkMessage = message as NetworkMessage;
                if (networkMessage.InnerBody != null) //Payload ADL command
                {
                    if (AutoSynchronize)
                    {
                        ContactService.msRequest(
                            PartnerScenario.MessengerPendingList,
                            delegate
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
                                        ClientType type = (ClientType)int.Parse(contactNode.Attributes["t"].Value);
                                        MSNLists list = (MSNLists)int.Parse(contactNode.Attributes["l"].Value);
                                        string displayName = account;
                                        try
                                        {
                                            displayName = contactNode.Attributes["f"].Value;
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        if (ContactList.HasContact(account, type))
                                        {
                                            Contact contact = ContactList.GetContact(account, type);

                                            // Fire ReverseAdded. If this contact on Pending list other person added us, otherwise we added and other person accepted.
                                            if (contact.OnPendingList || contact.OnReverseList)
                                            {
                                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "ADL received, reverse added fired. Contact is in list: " + contact.Lists.ToString());
                                                ContactService.OnReverseAdded(new ContactEventArgs(contact));
                                            }
                                        }

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + ":" + type + " was added to your " + list.ToString(), GetType().Name);

                                    } while (contactNode.NextSibling != null);
                                }

                            });
                    }
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
                                ClientType type = (ClientType)int.Parse(contactNode.Attributes["t"].Value);
                                MSNLists list = (MSNLists)int.Parse(contactNode.Attributes["l"].Value);
                                string displayName = account;
                                try
                                {
                                    displayName = contactNode.Attributes["f"].Value;
                                }
                                catch (Exception)
                                {
                                }

                                if (list == MSNLists.ReverseList)
                                {
                                    Contact contact = ContactList.GetContact(account, displayName, type);
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "ADL received, reverse added fired. Contact is in list: " + contact.Lists.ToString());
                                    contact.Lists |= MSNLists.ReverseList;
                                    ContactService.OnReverseAdded(new ContactEventArgs(contact));
                                }

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + ":" + type + " was added to your " + list.ToString(), GetType().Name);

                            } while (contactNode.NextSibling != null);
                        }
                    }
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
                            ClientType type = (ClientType)int.Parse(contactNode.Attributes["t"].Value);
                            MSNLists list = (MSNLists)int.Parse(contactNode.Attributes["l"].Value);
                            account = account.ToLower(CultureInfo.InvariantCulture);

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, account + " has removed you from his/her " + list.ToString(), GetType().Name);

                        } while (contactNode.NextSibling != null);
                    }
                }
            }
        }

        /// <summary>
        /// Called when an ADG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact group has been added to the contact group list.
        /// Raises the ContactService.ContactGroupAdded event.
        /// <code>ADG [Transaction] [ListVersion] [Name] [GroupID] </code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnADGReceived(NSMessage message)
        {
            if (AutoSynchronize)
            {
                ContactService.abRequest(PartnerScenario.ContactSave, null);
            }
        }

        /// <summary>
        /// Called when a RMG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact group has been removed.
        /// Raises the ContactService.ContactGroupRemoved event.
        /// <code>RMG [Transaction] [ListVersion] [GroupID]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnRMGReceived(NSMessage message)
        {
            if (AutoSynchronize)
            {
                ContactService.abRequest(PartnerScenario.ContactSave, null);
            }
        }

        /// <summary>Called when a FQY (Federated Query) command has been received.
        /// <remarks>Indicates a client has different network types except PassportMember.</remarks>
        /// <code>FQY [TransactionID] [PayloadLength]
        /// <ml><d n="domain"><c n="username" t="clienttype" actual="emailaddresswithoutcountrysuffix" /></d></ml>
        /// </code>
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnFQYReceived(NSMessage message)
        {
            int pendingTransactionId = int.Parse(message.CommandValues[0].ToString());
            if (ContactService.pendingFQYs.Contains(pendingTransactionId))
            {
                lock (ContactService.pendingFQYs)
                    ContactService.pendingFQYs.Remove(pendingTransactionId);

                NetworkMessage networkMessage = message as NetworkMessage;
                if (networkMessage.InnerBody != null)
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
                            if (contactNode.Attributes["t"] != null)
                            {
                                ClientType type = (ClientType)Enum.Parse(typeof(ClientType), contactNode.Attributes["t"].Value);
                                string account = (contactNode.Attributes["n"].Value + "@" + domain).ToLowerInvariant();
                                String otherEmail = String.Empty;
                                if (contactNode.Attributes["actual"] != null)
                                {
                                    // Save original (ex: yahoo.com.tr) as other email
                                    otherEmail = String.Copy(account);
                                    // Then use actual address, this is stript address (ex: yahoo.com)
                                    account = contactNode.Attributes["actual"].Value.ToLowerInvariant();
                                }
                                ContactService.AddNewContact(account, type, String.Empty, otherEmail);
                            }
                            else
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, String.Format("FQY: {0} has passport network only.", contactNode.Attributes["n"].Value + "@" + domain), GetType().Name);
                            }

                        } while (contactNode.NextSibling != null);
                    }
                }
            }
        }


        /// <summary>
        /// Called when a NFY command has been received.
        /// <remarks>Indicates that a circle operation occured.</remarks>
        /// <code>
        /// NFY [TransactionID] [Operation: PUT|DEL] [Payload Length]\r\n[Payload Data]
        /// </code>
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnNFYReceived(NSMessage message)
        {
            #region NFY PUT

            if (message.CommandValues[0].ToString() == "PUT")
            {
                NetworkMessage networkMessage = message as NetworkMessage;
                if (networkMessage.InnerBody == null)
                    return;

                string nfyTextString = Encoding.UTF8.GetString(networkMessage.InnerBody);
                int lastLineBreak = nfyTextString.LastIndexOf("\r\n");
                if (lastLineBreak == -1)
                    return;

                string xmlString = nfyTextString.Substring(lastLineBreak + 2);
                string mimeString = nfyTextString.Substring(0, lastLineBreak).Replace("\r\n\r\n", "\r\n");
                byte[] mimeBytes = Encoding.UTF8.GetBytes(mimeString);
                MimeDictionary mimeDic = new MimeDictionary(mimeBytes);

                if (!(mimeDic.ContainsKey(MimeHeaderStrings.Content_Type) && mimeDic.ContainsKey(MimeHeaderStrings.NotifType)))
                    return;

                if (mimeDic[MimeHeaderStrings.Content_Type].Value != "application/circles+xml")
                    return;

                string[] typeMail = mimeDic[MimeHeaderStrings.From].ToString().Split(':');

                if (typeMail.Length == 0)
                    return;

                string[] guidDomain = typeMail[1].Split('@');
                if (guidDomain.Length == 0)
                    return;

                Circle circle = CircleList[typeMail[1]];

                if (circle == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                "[OnNFYReceived] Cannot complete the operation since circle not found: " + mimeDic[MimeHeaderStrings.From].ToString());
                    return;
                }

                if (xmlString == string.Empty)
                    return;  //No xml content.

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);

                if (mimeDic[MimeHeaderStrings.NotifType].Value == "Full")
                {
                    //This is an initial NFY
                }

                XmlNodeList ids = xmlDoc.SelectNodes("//circle/roster/user/id");
                if (ids.Count == 0)
                {
                    return;  //I hate indent.
                }

                foreach (XmlNode node in ids)
                {
                    if (node.InnerText.Split(':').Length == 0)
                        return;

                    string memberAccount = node.InnerText.Split(':')[1];
                    if (memberAccount == ContactList.Owner.Mail.ToLowerInvariant())
                        continue;

                    ClientType memberType = (ClientType)int.Parse(node.InnerText.Split(':')[0]);
                    string id = node.InnerText + ";via=" + mimeDic[MimeHeaderStrings.From].ToString();

                    if (circle.HasMember(id, AccountParseOption.ParseAsFullCircleAccount))
                    {
                        Contact contact = circle.ContactList.GetContact(memberAccount, memberType);
                        OnJoinedCircleConversation(new CircleMemberEventArgs(circle, contact));
                    }
                }

            }

            #endregion

            #region NFY DEL
            if (message.CommandValues[0].ToString() == "DEL")
            {
                NetworkMessage networkMessage = message as NetworkMessage;
                if (networkMessage.InnerBody == null)
                {
                    return;
                }

                string mimeString = Encoding.UTF8.GetString(networkMessage.InnerBody);
                mimeString = mimeString.Replace("\r\n\r\n", "\r\n");
                byte[] mimeBytes = Encoding.UTF8.GetBytes(mimeString);
                MimeDictionary mimeDic = new MimeDictionary(mimeBytes);

                if (!mimeDic.ContainsKey(MimeHeaderStrings.Uri))
                    return;
                string xpathUri = mimeDic[MimeHeaderStrings.Uri].ToString();
                if (xpathUri.IndexOf("/circle/roster(IM)/user") == -1)
                    return;

                string typeAccount = xpathUri.Substring("/circle/roster(IM)/user".Length);
                typeAccount = typeAccount.Substring(typeAccount.IndexOf("(") + 1);
                typeAccount = typeAccount.Substring(0, typeAccount.IndexOf(")"));

                if (!mimeDic.ContainsKey(MimeHeaderStrings.From))
                    return;

                string circleID = mimeDic[MimeHeaderStrings.From].ToString();
                string[] typeCircleID = circleID.Split(':');
                if (typeCircleID.Length < 2)
                    return;

                if (CircleList[typeCircleID[1]] == null)
                    return;

                Circle circle = CircleList[typeCircleID[1]];

                string fullAccount = typeAccount + ";via=" + circleID;
                if (!circle.HasMember(fullAccount, AccountParseOption.ParseAsFullCircleAccount))
                    return;

                Contact member = circle.GetMember(fullAccount, AccountParseOption.ParseAsFullCircleAccount);
                OnLeftCircleConversation(new CircleMemberEventArgs(circle, member));



            }

            #endregion
        }

        /// <summary>
        /// Called when a SDG command has been received.
        /// <remarks>Indicates that someone send us a message from a circle group.</remarks>
        /// <code>
        /// SDG 0 [Payload Length]\r\n[Payload Data]
        /// </code>
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnSDGReceived(NSMessage message)
        {
            /*** typing message ***
            SDG 0 421
            Routing: 1.0
            To: 9:00000000-0000-0000-0009-cdc0351b0c6d@live.com;path=IM
            From: 1:updatedynamicitem@hotmail.com;epid={7a29dadd-503d-4c5c-8e6b-a599c15de981}

            Reliability: 1.0
            Stream: 0
            Segment: 1

            Messaging: 1.0
            Content-Length: 2
            Content-Type: text/x-msmsgscontrol
            Content-Transfer-Encoding: 7bit
            Message-Type: Control
            Message-Subtype: Typing
            MIME-Version: 1.0
            TypingUser: updatedynamicitem@hotmail.com
            ***/

            /*** group text messaging ***
            SDG 0 410
            Routing: 1.0
            To: 9:00000000-0000-0000-0009-cdc0351b0c6d@live.com;path=IM
            From: 1:updatedynamicitem@hotmail.com;epid={7a29dadd-503d-4c5c-8e6b-a599c15de981}

            Reliability: 1.0
            Stream: 0
            Segment: 2

            Messaging: 1.0
            Content-Length: 2
            Content-Type: Text/plain; charset=UTF-8
            Content-Transfer-Encoding: 7bit
            Message-Type: Text
            MIME-Version: 1.0
            X-MMS-IM-Format: FN=Segoe%20UI; EF=; CO=0; CS=1; PF=0

            hi
            ***/

            NetworkMessage networkMessage = message as NetworkMessage;
            if (networkMessage.InnerBody != null)
            {
                string payloadString = Encoding.UTF8.GetString(networkMessage.InnerBody);
                int lastLineBreak = payloadString.LastIndexOf("\r\n");
                if (lastLineBreak != -1)
                {
                    string contentString = payloadString.Substring(lastLineBreak + 2);
                    string mimeString = payloadString.Substring(0, lastLineBreak).Replace("\r\n\r\n", "\r\n");
                    byte[] mimeBytes = Encoding.UTF8.GetBytes(mimeString);
                    MimeDictionary mimeDic = new MimeDictionary(mimeBytes);

                    string[] typeMail = mimeDic[MimeHeaderStrings.To].Value.Split(':');
                    if (typeMail.Length == 0)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error: Cannot find circle type in id: " + mimeDic[MimeHeaderStrings.To].Value);
                        return;
                    }

                    string[] guidDomain = typeMail[1].Split('@');
                    if (guidDomain.Length == 0)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error: Cannot find circle guid and host domain in id: " + typeMail[1]);
                        return;
                    }

                    Circle circle = CircleList[typeMail[1]];
                    if (circle == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error: Cannot find circle " + typeMail[1] + " in your circle list.");
                        return;
                    }

                    // 1:username@hotmail.com;via=9:guid@live.com
                    string fullAccount = mimeDic[MimeHeaderStrings.From].Value + ";via=" + mimeDic[MimeHeaderStrings.To].Value;
                    fullAccount = fullAccount.ToLowerInvariant();

                    Contact member = circle.GetMember(fullAccount, AccountParseOption.ParseAsFullCircleAccount);

                    if (member == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error: Cannot find circle type in id: " + mimeDic[MimeHeaderStrings.To].Value);
                        return;
                    }

                    CircleMemberEventArgs arg = new CircleMemberEventArgs(circle, member);

                    if (mimeDic.ContainsKey(MimeHeaderStrings.Content_Type) && mimeDic.ContainsKey(MimeHeaderStrings.Message_Subtype))
                    {
                        if (mimeDic[MimeHeaderStrings.Content_Type].ToString() == "text/x-msmsgscontrol" &&
                            mimeDic[MimeHeaderStrings.Message_Subtype].ToString() == "Typing")
                        {
                            //Typing
                            OnCircleTypingMessageReceived(arg);
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "Circle: " + circle.ToString() + "\r\nMember: " + member.ToString() + "\r\nIs typing....");
                        }
                    }

                    if (mimeDic.ContainsKey(MimeHeaderStrings.Content_Type) && mimeDic.ContainsKey(MimeHeaderStrings.Message_Type))
                    {
                        if (mimeDic[MimeHeaderStrings.Content_Type].ToString().IndexOf("Text/plain;") > -1 &&
                            mimeDic[MimeHeaderStrings.Message_Type].ToString() == "Text")
                        {
                            //Text message.
                            TextMessage txtMessage = new TextMessage(contentString);
                            StrDictionary strDic = new StrDictionary();
                            foreach (string key in mimeDic.Keys)
                            {
                                strDic.Add(key, mimeDic[key].ToString());
                            }

                            txtMessage.ParseHeader(strDic);
                            CircleTextMessageEventArgs textMessageArg = new CircleTextMessageEventArgs(txtMessage, circle, member);
                            OnCircleTextMessageReceived(textMessageArg);
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "Circle: " + circle.ToString() + "\r\nMember: " + member.ToString() + "\r\nSend you a text message:\r\n" + txtMessage.ToDebugString());

                        }

                        if (mimeDic[MimeHeaderStrings.Content_Type].ToString().IndexOf("Text/plain;") > -1 &&
                            mimeDic[MimeHeaderStrings.Message_Type].ToString() == "Nudge")
                        {
                            //Nudge
                            OnCircleNudgeReceived(arg);
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "Circle: " + circle.ToString() + "\r\nMember: " + member.ToString() + "\r\nSend you a nudge.");
                        }
                    }
                }
            }
        }

        protected virtual void OnCircleTypingMessageReceived(CircleMemberEventArgs e)
        {
            if (CircleTypingMessageReceived != null)
                CircleTypingMessageReceived(this, e);
        }

        protected virtual void OnCircleNudgeReceived(CircleMemberEventArgs e)
        {
            if (CircleNudgeReceived != null)
                CircleNudgeReceived(this, e);
        }

        protected virtual void OnCircleTextMessageReceived(CircleTextMessageEventArgs e)
        {
            if (CircleTextMessageReceived != null)
                CircleTextMessageReceived(this, e);
        }

        protected virtual void OnLeftCircleConversation(CircleMemberEventArgs e)
        {
            if (LeftCircleConversation != null)
                LeftCircleConversation(this, e);
        }

        protected virtual void OnJoinedCircleConversation(CircleMemberEventArgs e)
        {
            if (JoinedCircleConversation != null)
                JoinedCircleConversation(this, e);
        }

        protected virtual void OnCircleStatusChanged(CircleStatusChangedEventArgs e)
        {
            if (CircleStatusChanged != null)
                CircleStatusChanged(this, e);
        }

        protected virtual void OnCircleMemberOnline(CircleMemberEventArgs e)
        {
            if (CircleMemberOnline != null)
                CircleMemberOnline(this, e);
        }

        protected virtual void OnCircleMemberOffline(CircleMemberEventArgs e)
        {
            if (CircleMemberOffline != null)
                CircleMemberOffline(this, e);
        }

        protected virtual void OnCircleMemberStatusChanged(CircleMemberStatusChanged e)
        {
            if (CircleMemberStatusChanged != null)
                CircleMemberStatusChanged(this, e);
        }

        protected virtual void OnCircleOnline(CircleEventArgs e)
        {
            if (CircleOnline != null)
                CircleOnline(this, e);
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

            string payload = QRYFactory.CreateQRY(Credentials.ClientID, Credentials.ClientCode, message.CommandValues[1].ToString());
            MSNTicket.OIMLockKey = payload;
            MessageProcessor.SendMessage(new NSPayLoadMessage("QRY", new string[] { Credentials.ClientID }, payload));
        }

        /// <summary>
        /// Called when an PRP command has been received.
        /// </summary>
        /// <remarks>
        /// Informs about the phone numbers of the contact list owner.
        /// <code>PRP [TransactionID] [ListVersion] PhoneType Number</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnPRPReceived(NSMessage message)
        {
            string number = String.Empty;
            string type = String.Empty;
            if (message.CommandValues.Count >= 4)
            {
                if (message.CommandValues.Count >= 4)
                    number = HttpUtility.UrlDecode((string)message.CommandValues[3]);
                else
                    number = String.Empty;
                type = message.CommandValues[1].ToString();
            }
            else
            {
                number = HttpUtility.UrlDecode((string)message.CommandValues[2]);
                type = message.CommandValues[1].ToString();
            }

            switch (type)
            {
                case "PHH":
                    ContactList.Owner.SetHomePhone(number);
                    break;
                case "PHW":
                    ContactList.Owner.SetWorkPhone(number);
                    break;
                case "PHM":
                    ContactList.Owner.SetMobilePhone(number);
                    break;
                case "MBE":
                    ContactList.Owner.SetMobileDevice((number == "Y") ? true : false);
                    break;
                case "MOB":
                    ContactList.Owner.SetMobileAccess((number == "Y") ? true : false);
                    break;
                case "MFN":
                    ContactList.Owner.SetName(HttpUtility.UrlDecode((string)message.CommandValues[2]));
                    break;
            }
        }

        /// <summary>
        /// Called when a PUT command message has been received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnPUTReceived(NSMessage message)
        {
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
            }
        }



        #endregion

        #region Privacy

        /// <summary>
        /// Called when a BLP command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send the privacy mode for the contact list owner.
        /// <code>BLP [Transaction] [SynchronizationID] [PrivacyMode]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnBLPReceived(NSMessage message)
        {
            if (ContactList.Owner == null)
                return;

            string type = String.Empty;

            if (message.CommandValues.Count == 1)
                type = message.CommandValues[0].ToString();
            else
                type = message.CommandValues[1].ToString();

            switch (type)
            {
                case "AL":
                    ContactList.Owner.SetPrivacy(PrivacyMode.AllExceptBlocked);
                    if (AutoSynchronize)
                    {
                        ContactService.UpdateMe();
                    }
                    break;
                case "BL":
                    ContactList.Owner.SetPrivacy(PrivacyMode.NoneButAllowed);
                    if (AutoSynchronize)
                    {
                        ContactService.UpdateMe();
                    }
                    break;
            }
        }

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
        protected virtual void Clear()
        {
            // 1. Cancel transfers
            p2pHandler.Clear();

            // 2. Cancel web services. MSNTicket must be here.
            msnticket = MSNTicket.Empty;
            ContactService.Clear();
            StorageService.Clear();
            OIMService.Clear();
            WhatsUpService.Clear();

            // 3. isSignedIn must be here... 
            // a) ContactService.Clear() merges and saves addressbook if isSignedIn=true.
            // b) Owner.ClientCapacities = ClientCapacities.None doesn't send CHG command if isSignedIn=false.
            isSignedIn = false;
            externalEndPoint = null;

            // 4. Clear contact lists and circle list.
            ContactList.Reset();
            CircleList.Clear();
            ContactGroups.Clear();

            //5. Reset contact manager.
            Manager.Reset();

            //6. Reset censor words
            CensorWords.Clear();
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
                switch (nsMessage.Command)
                {
                    // Most used CMDs
                    case "MSG":
                        OnMSGReceived(nsMessage);
                        return;
                    case "FLN":
                        OnFLNReceived(nsMessage);
                        return;
                    case "NLN":
                        OnNLNReceived(nsMessage);
                        return;
                    case "QNG":
                        OnQNGReceived(nsMessage);
                        return;
                    case "UBX":
                        OnUBXReceived(nsMessage);
                        return;

                    // Other CMDs
                    case "ADG":
                        OnADGReceived(nsMessage);
                        return;
                    case "ADL":
                        OnADLReceived(nsMessage);
                        return;
                    case "BLP":
                        OnBLPReceived(nsMessage);
                        return;
                    case "CHG":
                        OnCHGReceived(nsMessage);
                        return;
                    case "CHL":
                        OnCHLReceived(nsMessage);
                        return;
                    case "CVR":
                        OnCVRReceived(nsMessage);
                        return;
                    case "FQY":
                        OnFQYReceived(nsMessage);
                        return;
                    case "GCF":
                        OnGCFReceived(nsMessage);
                        return;
                    case "NOT":
                        OnNOTReceived(nsMessage);
                        return;
                    case "OUT":
                        OnOUTReceived(nsMessage);
                        return;
                    case "PRP":
                        OnPRPReceived(nsMessage);
                        return;
                    case "QRY":
                        OnQRYReceived(nsMessage);
                        return;
                    case "RMG":
                        OnRMGReceived(nsMessage);
                        return;
                    case "RML":
                        OnRMLReceived(nsMessage);
                        return;
                    case "RNG":
                        OnRNGReceived(nsMessage);
                        return;
                    case "UBM":
                        OnUBMReceived(nsMessage);
                        return;
                    case "UBN":
                        OnUBNReceived(nsMessage);
                        return;
                    case "USR":
                        OnUSRReceived(nsMessage);
                        return;
                    case "UUN":
                        OnUUNReceived(nsMessage);
                        return;
                    case "UUX":
                        OnUUXReceived(nsMessage);
                        return;
                    case "VER":
                        OnVERReceived(nsMessage);
                        return;
                    case "XFR":
                        OnXFRReceived(nsMessage);
                        return;
                    case "SBS":
                        OnSBSReceived(nsMessage);
                        return;
                    case "NFY":
                        OnNFYReceived(nsMessage);
                        return;
                    case "PUT":
                        OnPUTReceived(nsMessage);
                        return;
                    case "SDG":
                        OnSDGReceived(nsMessage);
                        return;
                }

                // Check whether it is a numeric error command
                if (nsMessage.Command[0] >= '0' && nsMessage.Command[0] <= '9')
                {
                    MSNError msnError = 0;
                    try
                    {
                        int errorCode = int.Parse(nsMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
                        msnError = (MSNError)errorCode;
                    }
                    catch (Exception fe)
                    {
                        throw new MSNPSharpException("Exception Occurred when parsing an error code received from the server", fe);
                    }

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "A server error occurred\r\nError Code: " + nsMessage.Command
                        + "\r\nError Description: " + msnError.ToString());

                    OnServerErrorReceived(new MSNErrorEventArgs(msnError));
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

        #endregion
    }
};
