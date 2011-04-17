#region
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
using System.Net;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.P2P;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using System.Globalization;

    /// <summary>
    /// User in roster list.
    /// </summary>
    [Serializable]
    public partial class Contact
    {
        /// <summary>
        /// live.com
        /// </summary>
        public const string DefaultHostDomain = "live.com";

        #region Serializable Fields

        protected Guid guid = Guid.Empty;
        protected Guid addressBookId = Guid.Empty;
        private long cId = 0;
        private string account = string.Empty;
        private string name = string.Empty;
        private string nickName = string.Empty;
        private string contactType = string.Empty;

        private bool hasSpace = false;
        private bool mobileDevice = false;
        private bool mobileAccess = false;
        private bool isMessengerUser = false;
        private bool isHiddenContact = false;

        private PresenceStatus status = PresenceStatus.Offline;
        private IMAddressInfoType clientType = IMAddressInfoType.WindowsLive;
        private CirclePersonalMembershipRole circleRole = CirclePersonalMembershipRole.None;
        private RoleLists lists = RoleLists.None;

        #endregion

        #region NonSerialized fields

        [NonSerialized]
        private Dictionary<string, string> phoneNumbers = new Dictionary<string, string>();

        [NonSerialized]
        private Dictionary<string, object> coreProfile = new Dictionary<string, object>();

        [NonSerialized]
        private List<ContactGroup> contactGroups = new List<ContactGroup>(0);

        [NonSerialized]
        private Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>(0);

        [NonSerialized]
        private Dictionary<string, Contact> siblings = new Dictionary<string, Contact>(0);

        [NonSerialized]
        protected Dictionary<Guid, EndPointData> endPointData = new Dictionary<Guid, EndPointData>(0);

        [NonSerialized]
        private List<ActivityDetailsType> activities = new List<ActivityDetailsType>(0);

        [NonSerialized]
        private object syncObject = new object();

        [NonSerialized]
        private NSMessageHandler nsMessageHandler = null;

        [NonSerialized]
        private string siblingString = string.Empty;

        [NonSerialized]
        private string hash = string.Empty;

        [NonSerialized]
        private string comment = string.Empty;

        [NonSerialized]
        private ulong oimCount = 1;

        [NonSerialized]
        private int adlCount = 1;

        [NonSerialized]
        private object clientData = null;

        [NonSerialized]
        private DisplayImage displayImage = null;

        [NonSerialized]
        private SceneImage sceneImage = null;

        [NonSerialized]
        private PersonalMessage personalMessage = null;

        [NonSerialized]
        private Color colorScheme = Color.Empty;

        [NonSerialized]
        private Uri userTile = null;

        [NonSerialized]
        private string userTileLocation = string.Empty;

        [NonSerialized]
        private string sceneContext = string.Empty;

        [NonSerialized]
        private P2PBridge directBridge = null;

        [NonSerialized]
        internal DCNonceType dcType = DCNonceType.None;

        [NonSerialized]
        internal Guid dcPlainKey = Guid.Empty;

        [NonSerialized]
        internal Guid dcLocalHashedNonce = Guid.Empty;

        [NonSerialized]
        internal Guid dcRemoteHashedNonce = Guid.Empty;

        [NonSerialized]
        internal string HostDomain = DefaultHostDomain;

        [NonSerialized]
        private ContactList contactList = null;

        public ContactList ContactList
        {
            get
            {
                return contactList;
            }
            internal set
            {
                contactList = value;
            }
        }

        [NonSerialized]
        internal ContactType MeContact = null;

        [NonSerialized]
        internal CircleInverseInfoType circleInfo = null;

        [NonSerialized]
        private Contact gatewayContact = null;

        public Contact Via
        {
            get
            {
                return gatewayContact;
            }
            internal set
            {
                gatewayContact = value;
            }
        }

        internal void GenerateNewDCKeys()
        {
            dcType = DCNonceType.Sha1;
            dcPlainKey = Guid.NewGuid();
            dcLocalHashedNonce = HashedNonceGenerator.HashNonce(dcPlainKey);
            dcRemoteHashedNonce = Guid.Empty;
        }

        #endregion

        private Contact()
        {
        }

        protected internal Contact(string account, IMAddressInfoType cliType, NSMessageHandler handler)
            : this(Guid.Empty, account, cliType, 0, handler)
        {
        }

        protected internal Contact(string abId, string account, IMAddressInfoType cliType, long cid, NSMessageHandler handler)
            : this(new Guid(abId), account, cliType, cid, handler)
        {
        }

        protected internal Contact(Guid abId, string acc, IMAddressInfoType cliType, long cid, NSMessageHandler handler)
        {
            GenerateNewDCKeys();

            NSMessageHandler = handler;
            addressBookId = abId;
            account = acc.ToLowerInvariant();
            clientType = cliType;
            cId = cid;

            SetName(account);
            siblingString = ClientType.ToString(CultureInfo.InvariantCulture) + ":" + account;
            hash = MakeHash(Account, ClientType);

            if (NSMessageHandler != null)
            {
                NSMessageHandler.ContactManager.Add(this);
            }

            displayImage = DisplayImage.CreateDefaultImage(Account);
            sceneImage = SceneImage.CreateDefaultImage(Account);
            personalMessage = new PersonalMessage();

            if (Account == RemoteNetworkGateways.FaceBookGatewayAccount ||
                Account == RemoteNetworkGateways.LinkedInGateway)
            {
                IsHiddenContact = true;
            }
        }

        protected internal void SetCircleInfo(CircleInverseInfoType circleInfo, ContactType me)
        {
            MeContact = me;
            CID = me.contactInfo.CID;

            this.circleInfo = circleInfo;

            HostDomain = circleInfo.Content.Info.HostedDomain.ToLowerInvariant();
            CircleRole = (CirclePersonalMembershipRole)Enum.Parse(typeof(CirclePersonalMembershipRole), circleInfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role);

            SetName(circleInfo.Content.Info.DisplayName);
            SetNickName(Name);

            contactList = new ContactList(AddressBookId, new Owner(AddressBookId, me.contactInfo.passportName, me.contactInfo.CID, NSMessageHandler), this, NSMessageHandler);
            Lists = RoleLists.Allow | RoleLists.Forward;
        }

        #region Events

        /// <summary>
        /// Fired when contact places changed.
        /// </summary>
        public event EventHandler<PlaceChangedEventArgs> PlacesChanged;

        /// <summary>
        /// Fired when core profile updated via Directory Service.
        /// </summary>
        public event EventHandler<EventArgs> CoreProfileUpdated;

        /// <summary>
        /// Fired when contact's display name changed.
        /// </summary>
        public event EventHandler<EventArgs> ScreenNameChanged;

        public event EventHandler<EventArgs> PersonalMessageChanged;

        /// <summary>
        /// Fired after contact's display image has been changed.
        /// </summary>
        public event EventHandler<DisplayImageChangedEventArgs> DisplayImageChanged;

        /// <summary>
        /// Fired after contact's scene has been changed.
        /// </summary>
        public event EventHandler<SceneImageChangedEventArgs> SceneImageChanged;

        public event EventHandler<EventArgs> DirectBridgeEstablished;

        /// <summary>
        /// Fired after received contact's display image changed notification.
        /// </summary>
        public event EventHandler<DisplayImageChangedEventArgs> DisplayImageContextChanged;

        /// <summary>
        /// Fired after receiving notification that the contact's scene image has changed.
        /// </summary>
        public event EventHandler<SceneImageChangedEventArgs> SceneImageContextChanged;

        /// <summary>
        /// Fired after receiving notification that the contact's color scheme has changed.
        /// </summary>
        public event EventHandler<EventArgs> ColorSchemeChanged;

        public event EventHandler<ContactGroupEventArgs> ContactGroupAdded;
        public event EventHandler<ContactGroupEventArgs> ContactGroupRemoved;
        public event EventHandler<EventArgs> ContactBlocked;
        public event EventHandler<EventArgs> ContactUnBlocked;
        public event EventHandler<StatusChangedEventArgs> ContactOnline;
        public event EventHandler<StatusChangedEventArgs> ContactOffline;
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        #endregion

        #region Contact Properties

        internal object SyncObject
        {
            get
            {
                return syncObject;
            }
        }

        internal string SiblingString
        {
            get
            {
                return siblingString;
            }
        }

        internal NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }

            set
            {
                nsMessageHandler = value;
            }
        }

        /// <summary>
        /// The display image url from the webside.
        /// </summary>
        public Uri UserTileURL
        {
            get
            {
                return userTile;
            }

            internal set
            {
                userTile = value;
            }
        }

        /// <summary>
        /// The displayimage context.
        /// </summary>
        public string UserTileLocation
        {
            //I create this property because I don't want to play tricks with display image's OriginalContext and Context any more.

            get
            {
                return userTileLocation;
            }

            internal set
            {
                userTileLocation = MSNObject.GetDecodeString(value);
            }
        }

        /// <summary>
        /// The scenes context.
        /// </summary>
        public string SceneContext
        {
            get
            {
                return sceneContext;
            }

            internal set
            {
                sceneContext = MSNObject.GetDecodeString(value);
            }
        }

        /// <summary>
        /// Get the Guid of contact, NOT CID.
        /// </summary>
        public Guid Guid
        {
            get
            {
                return guid;
            }

            internal set
            {
                guid = value;
            }
        }

        /// <summary>
        /// The identifier of addressbook this contact belongs to.
        /// </summary>
        public Guid AddressBookId
        {
            get
            {
                return addressBookId;
            }
        }

        /// <summary>
        /// The contact id of contact, only PassportMembers have CID.
        /// </summary>
        public long CID
        {
            get
            {
                return cId;
            }
            internal set
            {
                cId = value;
            }
        }

        /// <summary>
        /// The account of contact (E-mail address, phone number, Circle guid, FacebookID etc.)
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
        }

        /// <summary>
        /// The display name of contact.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return name;
            }

            set
            {
                throw new NotImplementedException("Must be override in subclass.");
            }
        }

        public string HomePhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhonePersonal) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhonePersonal] : string.Empty;
            }
        }

        public string WorkPhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhoneBusiness) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhoneBusiness] : string.Empty;
            }
        }

        public string MobilePhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhoneMobile) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhoneMobile] : string.Empty;
            }
        }

        public Dictionary<string, string> PhoneNumbers
        {
            get
            {
                return phoneNumbers;
            }
        }

        public Dictionary<string, object> CoreProfile
        {
            get
            {
                return coreProfile;
            }
        }

        public bool MobileDevice
        {
            get
            {
                return mobileDevice;
            }
        }

        public bool MobileAccess
        {
            get
            {
                return mobileAccess;
            }
        }

        /// <summary>
        /// Indicates whether this contact has MSN Space.
        /// </summary>
        public bool HasSpace
        {
            get
            {
                return hasSpace;
            }

            internal set
            {
                hasSpace = value;
                NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
            }
        }

        public Dictionary<Guid, EndPointData> EndPointData
        {
            get
            {
                return endPointData;
            }
        }

        public bool HasSignedInWithMultipleEndPoints
        {
            get
            {
                return EndPointData.Count > 1;
            }
        }

        public int PlaceCount
        {
            get
            {
                return EndPointData.Count;
            }
        }

        public P2PVersion P2PVersionSupported
        {
            get
            {
                if (EndPointData.Count > 0)
                {
                    Guid ep = SelectBestEndPointId();

                    if (EndPointData.ContainsKey(ep))
                    {
                        return EndPointData[ep].P2PVersionSupported;
                    }
                }

                return P2PVersion.None;
            }
        }

        public P2PBridge DirectBridge
        {
            get
            {
                return directBridge;
            }
            internal set
            {
                if (directBridge != null)
                {
                    directBridge.BridgeOpened -= HandleDirectBridgeOpened;
                    directBridge.BridgeClosed -= HandleDirectBridgeClosed;
                }

                directBridge = value;

                if (directBridge != null)
                {
                    directBridge.BridgeOpened += HandleDirectBridgeOpened;
                    directBridge.BridgeClosed += HandleDirectBridgeClosed;
                }
            }
        }

        /// <summary>
        /// The online status of contact.
        /// </summary>
        public virtual PresenceStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                throw new NotImplementedException("This property is real-only for base class. Must be override in subclass.");
            }
        }

        /// <summary>
        /// Indicates whether the contact is online.
        /// </summary>
        public bool Online
        {
            get
            {
                return status != PresenceStatus.Offline;
            }
        }

        /// <summary>
        /// The type of contact's email account.
        /// </summary>
        public IMAddressInfoType ClientType
        {
            get
            {
                return clientType;
            }
        }

        /// <summary>
        /// The role of contact in the addressbook.
        /// </summary>
        public string ContactType
        {
            get
            {
                return contactType;
            }
            internal set
            {
                contactType = value;
            }
        }

        public List<ContactGroup> ContactGroups
        {
            get
            {
                return contactGroups;
            }
        }

        public Dictionary<string, Contact> Siblings
        {
            get
            {
                return siblings;
            }
        }

        public virtual DisplayImage DisplayImage
        {
            get
            {
                LoadImageFromDeltas(displayImage);
                return displayImage;
            }

            //Calling this will not fire DisplayImageChanged event.
            internal set
            {
                if (displayImage != value)
                {
                    displayImage = value;
                    SaveImage(displayImage);
                }
            }
        }

        public virtual SceneImage SceneImage
        {
            get
            {
                LoadImageFromDeltas(sceneImage);
                return sceneImage;
            }
            internal set
            {
                if (sceneImage != value)
                {
                    sceneImage = value;
                    SaveImage(sceneImage);
                }
            }
        }

        public virtual Color ColorScheme
        {
            get
            {
                return colorScheme;
            }
            internal set
            {
                if (colorScheme != value)
                {
                    colorScheme = value;
                }
            }
        }

        public PersonalMessage PersonalMessage
        {
            get
            {
                return personalMessage;
            }
            protected internal set
            {
                if (null != value /*don't eval this: && value != personalMessage*/)
                {
                    personalMessage = value;
                    OnPersonalMessageChanged(value);
                }
            }
        }

        /// <summary>
        /// Emoticons[sha]
        /// </summary>
        public Dictionary<string, Emoticon> Emoticons
        {
            get
            {
                return emoticons;
            }
        }

        public List<ActivityDetailsType> Activities
        {
            get
            {
                return activities;
            }
        }

        /// <summary>
        /// The string representation info of contact.
        /// </summary>
        public virtual string Hash
        {
            get
            {
                return hash;
            }
        }

        public object ClientData
        {
            get
            {
                return clientData;
            }
            set
            {
                clientData = value;
            }
        }

        /// <summary>
        /// Receive updated contact information automatically.
        /// <remarks>Contact details like address and phone numbers are automatically downloaded to your Address Book.</remarks>
        /// </summary>
        public bool AutoSubscribeToUpdates
        {
            get
            {
                return (contactType == MessengerContactType.Live || contactType == MessengerContactType.LivePending);
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && ClientType == IMAddressInfoType.WindowsLive)
                {
                    if (value)
                    {
                        if (!AutoSubscribeToUpdates)
                        {
                            contactType = MessengerContactType.LivePending;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                        }
                    }
                    else
                    {
                        if (contactType != MessengerContactType.Regular)
                        {
                            contactType = MessengerContactType.Regular;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                        }
                    }
                }
            }
        }

        public bool IsHiddenContact
        {
            get
            {
                return isHiddenContact;
            }

            private set
            {
                isHiddenContact = value;
            }
        }

        /// <summary>
        /// Indicates whether the contact can receive MSN message.
        /// </summary>
        public bool IsMessengerUser
        {
            get
            {
                return isMessengerUser;
            }


            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && IsMessengerUser != value)
                {
                    isMessengerUser = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId,
                        delegate  //If you don't add this, you can't see the contact online until your next login
                        {
                            Dictionary<string, RoleLists> hashlist = new Dictionary<string, RoleLists>(2);
                            hashlist.Add(Hash, Lists ^ RoleLists.Reverse);
                            string payload = ContactService.ConstructLists(hashlist, false)[0];
                            NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                        });
                }

                NotifyManager();
            }
        }


        public string Comment
        {
            get
            {
                return comment;
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && Comment != value)
                {
                    comment = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                }
            }
        }

        /// <summary>
        /// The name provide by the owner.
        /// </summary>
        public string NickName
        {
            get
            {
                return nickName;
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && NickName != value)
                {
                    nickName = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                }
            }
        }


        /// <summary>
        /// The amount of OIMs sent in a session.
        /// </summary>
        internal ulong OIMCount
        {
            get
            {
                return oimCount;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                oimCount = value;
            }
        }

        /// <summary>
        /// The amount of ADL commands send for this contact.
        /// </summary>
        internal int ADLCount
        {
            get
            {
                return adlCount;
            }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                adlCount = value;
            }
        }

        /// <summary>
        /// The role of a contact in the addressbook.
        /// </summary>
        public CirclePersonalMembershipRole CircleRole
        {
            get
            {
                return circleRole;
            }

            internal set
            {
                circleRole = value;
            }
        }

        #endregion

        #region List Properties

        public bool AppearOnline
        {
            get
            {
                return ((lists & RoleLists.Hide) == RoleLists.None);
            }
            set
            {
                if (value != AppearOnline)
                {
                    AppearOffline = !value;
                }
            }
        }

        public bool AppearOffline
        {
            get
            {
                return !AppearOnline;
            }
            set
            {
                if (value != AppearOffline)
                {
                    if (value)
                        NSMessageHandler.ContactService.AppearOffline(this, null);
                    else
                        NSMessageHandler.ContactService.AppearOnline(this, null);
                }
            }
        }

        public bool OnForwardList
        {
            get
            {
                return ((lists & RoleLists.Forward) == RoleLists.Forward);
            }
            set
            {
                if (value != OnForwardList)
                {
                    if (value)
                    {
                        NSMessageHandler.ContactService.AddContactToList(this, RoleLists.Forward, null);
                    }
                    else
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, RoleLists.Forward, null);
                    }
                }
            }
        }

        /// <summary>
        /// Adds or removes this contact into/from your AL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set this property to false.
        /// </summary>
        public bool OnAllowedList
        {
            get
            {
                return ((lists & RoleLists.Allow) == RoleLists.Allow);
            }
            set
            {
                if (value != OnAllowedList)
                {
                    if (value)
                    {
                        NSMessageHandler.ContactService.AddContactToList(this, RoleLists.Allow, null);
                    }
                    else
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, RoleLists.Allow, null);
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the contact have you on their contact list and pending your approval. 
        /// </summary>
        public bool OnPendingList
        {
            get
            {
                return ((lists & RoleLists.Pending) == RoleLists.Pending);
            }
            set
            {
                if (value != OnPendingList && value == false)
                {
                    NSMessageHandler.ContactService.RemoveContactFromList(this, RoleLists.Pending, null);
                }
            }
        }

        /// <summary>
        /// The msn lists this contact has.
        /// </summary>
        public RoleLists Lists
        {
            get
            {
                return lists;
            }

            protected internal set
            {
                lists = value;
                NotifyManager();
            }
        }

        #endregion

        #region Internal setters

        internal void SetComment(string note)
        {
            comment = note;
        }

        internal void SetIsMessengerUser(bool isMessengerEnabled)
        {
            isMessengerUser = isMessengerEnabled;
            NotifyManager();
        }

        internal void SetList(RoleLists msnLists)
        {
            lists = msnLists;
            NotifyManager();
        }

        internal void SetMobileAccess(bool enabled)
        {
            mobileAccess = enabled;
        }

        internal void SetMobileDevice(bool enabled)
        {
            mobileDevice = enabled;
        }

        internal void SetName(string newName)
        {
            if (name != newName)
            {
                string oldName = name;
                name = newName;

                // notify all of our buddies we changed our name
                OnScreenNameChanged(oldName);
            }
        }

        internal void SetColorScheme(Color color)
        {
            colorScheme = color;
        }

        internal void SetSceneImage(SceneImage scene)
        {
            sceneImage = scene;
        }

        internal void SetHasSpace(bool hasSpaceValue)
        {
            hasSpace = hasSpaceValue;
        }

        internal void SetNickName(string newNick)
        {
            nickName = newNick;
        }

        internal void SetStatus(PresenceStatus newStatus)
        {
            //Becareful deadlock!

            PresenceStatus currentStatus = PresenceStatus.Unknown;

            lock (syncObject)
            {
                currentStatus = status;
            }

            if (currentStatus != newStatus)
            {
                PresenceStatus oldStatus = currentStatus;

                lock (syncObject)
                {

                    status = newStatus;
                }

                // raise an event
                OnStatusChanged(new StatusChangedEventArgs(oldStatus, newStatus));

                // raise the online/offline events
                if (oldStatus == PresenceStatus.Offline)
                    OnContactOnline(new StatusChangedEventArgs(oldStatus, newStatus));

                if (newStatus == PresenceStatus.Offline)
                    OnContactOffline(new StatusChangedEventArgs(oldStatus, newStatus));
            }
        }

        /// <summary>
        /// This method will lead to fire <see cref="Contact.DisplayImageContextChanged"/> event if the DisplayImage.Sha has been changed.
        /// </summary>
        /// <param name="updatedImageContext"></param>
        /// <returns>
        /// false: No event was fired.<br/>
        /// true: The <see cref="Contact.DisplayImageContextChanged"/> was fired.
        /// </returns>
        internal bool FireDisplayImageContextChangedEvent(string updatedImageContext)
        {
            if (DisplayImage == updatedImageContext)
                return false;

            // If Delta already has image, just call DisplayImageChanged instead
            if (NSMessageHandler.ContactService.Deltas.HasImage(SiblingString, GetSHA(updatedImageContext), true))
            {
                string Sha = string.Empty;
                byte[] rawImageData = NSMessageHandler.ContactService.Deltas.GetRawImageDataBySiblingString(SiblingString, out Sha, true);

                if (rawImageData != null)
                    displayImage = new DisplayImage(Account, new MemoryStream(rawImageData));

                NSMessageHandler.ContactService.Deltas.Save(true);
                OnDisplayImageChanged(new DisplayImageChangedEventArgs(displayImage, DisplayImageChangedType.TransmissionCompleted, false));

                return true;
            }

            OnDisplayImageContextChanged(new DisplayImageChangedEventArgs(null, DisplayImageChangedType.UpdateTransmissionRequired));
            return true;
        }

        /// <summary>
        /// This method will lead to fire <see cref="Contact.SceneImageContextChanged"/> event if the SceneImage.Sha has been changed.
        /// </summary>
        /// <param name="updatedImageContext"></param>
        /// <returns>
        /// false: No event was fired.<br/>
        /// true: The <see cref="Contact.SceneImageContextChanged"/> was fired.
        /// </returns>
        internal bool FireSceneImageContextChangedEvent(string updatedImageContext)
        {
            if (SceneImage == updatedImageContext)
                return false;

            // If Delta already has image, just call SceneImageChanged instead
            if (NSMessageHandler.ContactService.Deltas.HasImage(SiblingString, GetSHA(updatedImageContext), false))
            {
                string Sha = string.Empty;
                byte[] rawImageData = NSMessageHandler.ContactService.Deltas.GetRawImageDataBySiblingString(SiblingString, out Sha, false);

                if (rawImageData != null)
                    sceneImage = new SceneImage(Account, new MemoryStream(rawImageData));

                NSMessageHandler.ContactService.Deltas.Save(true);
                OnSceneImageChanged(new SceneImageChangedEventArgs(sceneImage, DisplayImageChangedType.TransmissionCompleted, false));

                return true;
            }

            OnSceneImageContextChanged(new SceneImageChangedEventArgs(DisplayImageChangedType.UpdateTransmissionRequired, updatedImageContext == string.Empty));
            return true;
        }

        /// <summary>
        /// This method will lead to fire <see cref="Contact.DisplayImageChanged"/> event if the DisplayImage.Image has been changed.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>
        /// false: No event was fired.<br/>
        /// true: The <see cref="Contact.DisplayImageChanged"/> event was fired.
        /// </returns>
        internal bool SetDisplayImageAndFireDisplayImageChangedEvent(DisplayImage image)
        {
            if (image == null)
                return false;


            DisplayImageChangedEventArgs displayImageChangedArg = null;
            //if ((displayImage != null && displayImage.Sha != image.Sha && displayImage.IsDefaultImage && image.Image != null) ||     //Transmission completed. default Image -> new Image
            //    (displayImage != null && displayImage.Sha != image.Sha && !displayImage.IsDefaultImage && image.Image != null) ||     //Transmission completed. old Image -> new Image.
            //    (displayImage != null && object.ReferenceEquals(displayImage, image) && displayImage.Image != null) ||              //Transmission completed. old Image -> updated old Image.
            //    (displayImage == null))
            {

                displayImageChangedArg = new DisplayImageChangedEventArgs(image, DisplayImageChangedType.TransmissionCompleted, false);
            }

            if (!object.ReferenceEquals(displayImage, image))
            {
                displayImage = image;
            }

            SaveOriginalDisplayImageAndFireDisplayImageChangedEvent(displayImageChangedArg);

            return true;
        }

        internal bool SetSceneImageAndFireSceneImageChangedEvent(SceneImage image)
        {
            if (image == null)
                return false;

            SceneImageChangedEventArgs sceneImageChangedArg = null;
            {
                sceneImageChangedArg = new SceneImageChangedEventArgs(image, DisplayImageChangedType.TransmissionCompleted, false);
            }

            if (!object.ReferenceEquals(sceneImage, image))
            {
                sceneImage = image;
            }

            SaveOriginalSceneImageAndFireSceneImageChangedEvent(sceneImageChangedArg);

            return true;
        }

        internal void SaveOriginalDisplayImageAndFireDisplayImageChangedEvent(DisplayImageChangedEventArgs arg)
        {
            SaveImage(displayImage);
            OnDisplayImageChanged(arg);
        }

        internal void SaveOriginalSceneImageAndFireSceneImageChangedEvent(SceneImageChangedEventArgs arg)
        {
            SaveImage(sceneImage);
            OnSceneImageChanged(arg);
        }

        internal string GetSHA(string imageContext)
        {
            string decodeContext = MSNObject.GetDecodeString(imageContext);
            int indexSHA = decodeContext.IndexOf("SHA1D=\"", 0) + 7;

            return decodeContext.Substring(indexSHA, decodeContext.IndexOf("\"", indexSHA) - indexSHA);
        }

        internal void NotifyManager()
        {
            if (AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
                return;

            if (NSMessageHandler == null)
                return;

            NSMessageHandler.ContactManager.SyncProperties(this);
        }

        #region Protected

        protected virtual void OnScreenNameChanged(string oldName)
        {
            if (ScreenNameChanged != null)
            {
                ScreenNameChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPersonalMessageChanged(PersonalMessage newmessage)
        {
            if (PersonalMessageChanged != null)
            {
                PersonalMessageChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the End Points changed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPlacesChanged(PlaceChangedEventArgs e)
        {
            if (PlacesChanged != null)
                PlacesChanged(this, e);
        }

        /// <summary>
        /// Called when the core profile updated.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnCoreProfileUpdated(EventArgs e)
        {
            if (CoreProfileUpdated != null)
                CoreProfileUpdated(this, e);
        }


        internal void SetChangedPlace(PlaceChangedEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "The account " + e.EndPointData.Account +
                " was " + e.Reason +
                " at another place: " + e.PlaceName + " " + e.EndPointData.Id, GetType().Name);

            bool triggerEvent = false;

            lock (SyncObject)
            {
                switch (e.Reason)
                {
                    case PlaceChangedReason.SignedIn:

                        lock(SyncObject)
                            EndPointData[e.EndPointData.Id] = e.EndPointData;

                        triggerEvent = true;
                        break;

                    case PlaceChangedReason.SignedOut:
                        lock (SyncObject)
                        {
                            if (EndPointData.ContainsKey(e.EndPointData.Id))
                            {
                                EndPointData.Remove(e.EndPointData.Id);
                                triggerEvent = true;
                            }
                        }
                        break;
                }
            }

            if (triggerEvent)
            {
                OnPlacesChanged(e);
            }
        }


        protected virtual void OnStatusChanged(StatusChangedEventArgs e)
        {
            if (StatusChanged != null)
                StatusChanged(this, e);
        }

        protected virtual void OnContactOnline(StatusChangedEventArgs e)
        {
            if (ContactOnline != null)
                ContactOnline(this, e);
        }

        protected virtual void OnContactOffline(StatusChangedEventArgs e)
        {
            if (ContactOffline != null)
            {
                ContactOffline(this, e);
            }
        }

        protected virtual void OnDisplayImageChanged(DisplayImageChangedEventArgs arg)
        {
            if (DisplayImageChanged != null)
            {
                DisplayImageChanged(this, arg);
            }
        }

        protected virtual void OnSceneImageChanged(SceneImageChangedEventArgs arg)
        {
            if (SceneImageChanged != null)
            {
                SceneImageChanged(this, arg);
            }
        }

        protected virtual void OnDisplayImageContextChanged(DisplayImageChangedEventArgs arg)
        {
            if (DisplayImageContextChanged != null)
            {
                DisplayImageContextChanged(this, arg);
            }
        }

        protected virtual void OnSceneImageContextChanged(SceneImageChangedEventArgs arg)
        {
            if (SceneImageContextChanged != null)
            {
                SceneImageContextChanged(this, arg);
            }
        }

        internal virtual void OnColorSchemeChanged()
        {
            if (ColorSchemeChanged != null)
            {
                ColorSchemeChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void LoadImageFromDeltas(DisplayImage image)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
                return;

            if (displayImage != null && !displayImage.IsDefaultImage) //Not default, no need to restore.
                return;

            string Sha = string.Empty;
            byte[] rawImageData = NSMessageHandler.ContactService.Deltas.GetRawImageDataBySiblingString(SiblingString, out Sha, true);
            if (rawImageData != null)
            {
                displayImage = new DisplayImage(Account, new MemoryStream(rawImageData));

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "User " + ToString() + "'s displayimage" + "restored.\r\n " +
                    "Old SHA:     " + Sha + "\r\n " +
                    "Current SHA: " + displayImage.Sha + "\r\n");
            }
        }

        protected virtual void LoadImageFromDeltas(SceneImage image)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
                return;

            if (sceneImage != null && !sceneImage.IsDefaultImage) //Not default, no need to restore.
                return;

            string Sha = string.Empty;
            byte[] rawImageData = NSMessageHandler.ContactService.Deltas.GetRawImageDataBySiblingString(SiblingString, out Sha, false);
            if (rawImageData != null)
            {
                sceneImage = new SceneImage(Account, new MemoryStream(rawImageData));

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "User " + ToString() + "'s scene image" + "restored.\r\n " +
                    "Old SHA:     " + Sha + "\r\n " +
                    "Current SHA: " + displayImage.Sha + "\r\n");
            }
        }

        protected virtual void SaveImage(DisplayImage dispImage)
        {
            if (NSMessageHandler.ContactService.Deltas == null || dispImage == null)
                return;

            if (dispImage.Image == null || string.IsNullOrEmpty(dispImage.Sha))
                return;

            if (NSMessageHandler.ContactService.Deltas.SaveImageAndRelationship(SiblingString, dispImage.Sha, dispImage.GetRawData(), true))
            {
                NSMessageHandler.ContactService.Deltas.Save(true);
            }
        }

        protected virtual void SaveImage(SceneImage sceneImage)
        {
            if (NSMessageHandler.ContactService.Deltas == null || sceneImage == null)
                return;

            if (sceneImage.Image == null || string.IsNullOrEmpty(sceneImage.Sha))
                return;

            if (NSMessageHandler.ContactService.Deltas.SaveImageAndRelationship(SiblingString, sceneImage.Sha, sceneImage.GetRawData(), false))
            {
                NSMessageHandler.ContactService.Deltas.Save(true);
            }
        }

        #endregion

        #endregion

        #region Internal contact operations

        protected virtual void OnContactGroupAdded(ContactGroup group)
        {
            if (ContactGroupAdded != null)
                ContactGroupAdded(this, new ContactGroupEventArgs(group));
        }

        protected virtual void OnContactGroupRemoved(ContactGroup group)
        {
            if (ContactGroupRemoved != null)
                ContactGroupRemoved(this, new ContactGroupEventArgs(group));
        }

        protected virtual void OnContactBlocked()
        {
            if (ContactBlocked != null)
                ContactBlocked(this, new EventArgs());
        }

        protected virtual void OnContactUnBlocked()
        {
            if (ContactUnBlocked != null)
                ContactUnBlocked(this, new EventArgs());
        }

        protected void OnDirectBridgeEstablished(EventArgs e)
        {
            if (DirectBridgeEstablished != null)
                DirectBridgeEstablished(this, e);
        }

        private void HandleDirectBridgeOpened(object sender, EventArgs e)
        {
            OnDirectBridgeEstablished(e);
        }

        private void HandleDirectBridgeClosed(object sender, EventArgs e)
        {
            DirectBridge = null;
        }


        internal void AddContactToGroup(ContactGroup group)
        {
            if (!contactGroups.Contains(group))
            {
                contactGroups.Add(group);

                OnContactGroupAdded(group);
            }
        }

        internal void RemoveContactFromGroup(ContactGroup group)
        {
            if (contactGroups.Contains(group))
            {
                contactGroups.Remove(group);

                OnContactGroupRemoved(group);
            }
        }

        /// <summary>
        /// Add a membership list for this contact.
        /// </summary>
        /// <param name="list"></param>
        /// <remarks>Since AllowList and BlockList are mutally exclusive, adding a member to AllowList will lead to the remove of BlockList, revese is as the same.</remarks>
        internal void AddToList(RoleLists list)
        {
            if ((lists & list) == RoleLists.None)
            {
                lists |= list;

                if ((list & RoleLists.Block) == RoleLists.Block)
                {
                    OnContactBlocked();
                }

                NotifyManager();
            }

        }

        internal void AddSibling(Contact contact)
        {
            lock (syncObject)
                Siblings[contact.Hash] = contact;
        }

        internal void AddSibling(Contact[] contacts)
        {
            if (contacts == null)
                return;

            lock (syncObject)
            {
                foreach (Contact sibling in contacts)
                {
                    Siblings[sibling.Hash] = sibling;
                }
            }
        }

        internal void RemoveFromList(RoleLists list)
        {
            if ((lists & list) != RoleLists.None)
            {
                lists ^= list;

                // set this contact to offline when it is neither on the allow list or on the forward list
                if (!(OnForwardList || OnAllowedList))
                {
                    status = PresenceStatus.Offline;
                    //also clear the groups, becase msn loose them when removed from the two lists
                    contactGroups.Clear();
                }

                if ((list & RoleLists.Block) == RoleLists.Block)
                {
                    OnContactUnBlocked();
                }

                NotifyManager();
            }


        }

        internal void RemoveFromList()
        {
            if (NSMessageHandler != null)
            {
                OnAllowedList = false;
                OnForwardList = false;

                NotifyManager();
            }
        }

        internal static RoleLists GetConflictLists(RoleLists currentLists, RoleLists newLists)
        {
            RoleLists conflictLists = RoleLists.None;

            if ((currentLists & RoleLists.Allow) != RoleLists.None && (newLists & RoleLists.Block) != RoleLists.None)
            {
                conflictLists |= RoleLists.Allow;
            }

            if ((currentLists & RoleLists.Block) != RoleLists.None && (newLists & RoleLists.Allow) != RoleLists.None)
            {
                conflictLists |= RoleLists.Block;
            }

            return conflictLists;
        }

        internal Guid SelectBestEndPointId()
        {
            Guid ret = Guid.Empty;

            foreach (EndPointData ep in EndPointData.Values)
            {
                ret = ep.Id;

                if (ep.Id != Guid.Empty && ep.P2PVersionSupported != P2PVersion.None)
                    return ep.Id;
            }

            return ret;
        }

        internal static string MakeHash(string account, IMAddressInfoType type)
        {
            return type.ToString(CultureInfo.InvariantCulture) + ":" + account.ToLowerInvariant();
        }
        
        internal static bool IsSpecialGatewayType(IMAddressInfoType type)
        {
            if (type == IMAddressInfoType.Circle || type == IMAddressInfoType.TemporaryGroup)
                return true;

            if (type == IMAddressInfoType.RemoteNetwork)
                return true;

            return false;
        }
        
        internal static bool ParseFullAccount(
            string fullAccount,
            out IMAddressInfoType accountAddressType, 
            out string account)
        {
            IMAddressInfoType viaAccountAddressType = IMAddressInfoType.None;
            string viaAccount = string.Empty;
            return ParseFullAccount(fullAccount, out accountAddressType, out account, out viaAccountAddressType, out viaAccount);
        }

        internal static bool ParseFullAccount(
            string fullAccount,
            out IMAddressInfoType accountAddressType, out string account,
            out IMAddressInfoType viaAccountAddressType, out string viaAccount)
        {
            accountAddressType = viaAccountAddressType = IMAddressInfoType.None;
            account = viaAccount = String.Empty;

            string[] memberAndNetwork = fullAccount.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (memberAndNetwork.Length > 0)
            {
                if (memberAndNetwork.Length > 1)
                {
                    // via=;
                    if (memberAndNetwork[1].Contains("via="))
                    {
                        string via = memberAndNetwork[1].Replace("via=", String.Empty);

                        string[] viaNetwork = via.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (viaNetwork.Length > 1)
                        {
                            viaAccountAddressType = (IMAddressInfoType)Enum.Parse(typeof(IMAddressInfoType), viaNetwork[0].ToString());
                            viaAccount = viaNetwork[1].ToLowerInvariant();

                            if (viaAccountAddressType == IMAddressInfoType.Telephone)
                                viaAccount = viaAccount.Replace("tel:", String.Empty);
                        }
                        else
                        {
                            // Assume windows live account
                            viaAccountAddressType = IMAddressInfoType.WindowsLive;
                            viaAccount = viaNetwork[0].ToLowerInvariant();
                        }
                    }
                }

                string[] member = memberAndNetwork[0].Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (member.Length > 1)
                {
                    accountAddressType = (IMAddressInfoType)Enum.Parse(typeof(IMAddressInfoType), member[0].ToString());
                    account = member[1].ToLowerInvariant();

                    if (accountAddressType == IMAddressInfoType.Telephone)
                        account = viaAccount.Replace("tel:", String.Empty);
                }
                else
                {
                    // Assume windows live account
                    accountAddressType = IMAddressInfoType.WindowsLive;
                    account = member[0].ToString().ToLowerInvariant();
                }

                return true;
            }

            return false;
        }


        internal bool HasLists(RoleLists msnlists)
        {
            return ((lists & msnlists) == msnlists);
        }

        internal static RoleLists GetListForADL(
            RoleLists currentContactList,
            IMAddressInfoType addressType, 
            ServiceShortNames service)
        {
            // Delete Reverse and Block roles, no more supported.
            currentContactList &= ~RoleLists.Reverse;
            currentContactList &= ~RoleLists.Block;

            // Don't send ADL if circle has only Hide role.
            if (addressType == IMAddressInfoType.Circle && currentContactList == RoleLists.Hide)
            {
                currentContactList &= ~RoleLists.Hide;
            }

            if (service == ServiceShortNames.PE)
            {
                currentContactList &= ~RoleLists.Hide;
            }

            return currentContactList;
        }


        #endregion

        /// <summary>
        /// Gets core profile from directory service and fires <see cref="CoreProfileUpdated"/> event
        /// after async request completed.
        /// </summary>
        public void GetCoreProfile()
        {
            if (CID != 0 && NSMessageHandler != null && NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                NSMessageHandler.DirectoryService.Get(CID);
            }
        }

        public bool HasGroup(ContactGroup group)
        {
            return contactGroups.Contains(group);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public static bool operator ==(Contact contact1, Contact contact2)
        {
            if (((object)contact1) == null && ((object)contact2) == null)
                return true;
            if (((object)contact1) == null || ((object)contact2) == null)
                return false;
            return contact1.GetHashCode() == contact2.GetHashCode();
        }

        public static bool operator !=(Contact contact1, Contact contact2)
        {
            return !(contact1 == contact2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return Hash;
        }

        /// <summary>
        /// Check whether two contacts represent the same user (Have the same passport account).
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public virtual bool IsSibling(Contact contact)
        {
            if (contact == null)
                return false;

            if (ClientType == contact.ClientType && Account.ToLowerInvariant() == contact.Account.ToLowerInvariant())
                return true;

            return false;
        }

        protected virtual bool CanReceiveMessage
        {
            get
            {
                if (NSMessageHandler == null || !NSMessageHandler.IsSignedIn)
                    throw new InvalidOperationException("Cannot send a message without signning in to the server. Please sign in first.");

                if (ClientType == IMAddressInfoType.Circle && NSMessageHandler.Owner.Status == PresenceStatus.Hidden)
                    throw new InvalidOperationException("Cannot send a message to the group when you are in 'Hidden' status.");

                return true;
            }
        }

        public virtual bool SupportsMultiparty
        {
            get
            {
                lock (SyncObject)
                {
                    foreach (EndPointData ep in EndPointData.Values)
                    {
                        if (ClientCapabilitiesEx.SupportsMultipartyConversations == (ep.IMCapabilitiesEx & ClientCapabilitiesEx.SupportsMultipartyConversations))
                            return true;
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// Send a typing message indicates that you are typing.
        /// </summary>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or you send a message to a circle in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendTypingMessage()
        {
            if (CanReceiveMessage)
                NSMessageHandler.SendTypingMessage(this);
        }

        /// <summary>
        /// Send nudge.
        /// </summary>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or you send a message to a circle in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendNudge()
        {
            if (CanReceiveMessage)
                NSMessageHandler.SendNudge(this);
        }

        /// <summary>
        /// Send a text message to the contact.
        /// </summary>
        /// <param name="textMessage"></param>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or you send a message to a circle in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendMessage(TextMessage textMessage)
        {
            if (CanReceiveMessage)
                NSMessageHandler.SendTextMessage(this, textMessage);
        }

        public void SendMobileMessage(string text)
        {
            if (MobileAccess || ClientType == IMAddressInfoType.Telephone)
            {
                NSMessageHandler.SendMobileMessage(this, text);
            }
            else
            {
                SendMessage(new TextMessage(text));
            }
        }

        public void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            if (CanReceiveMessage)
                NSMessageHandler.SendEmoticonDefinitions(this, emoticons, icontype);

            if (emoticons == null)
                throw new ArgumentNullException("emoticons");

            foreach (Emoticon emoticon in emoticons)
            {
                if (!NSMessageHandler.Owner.Emoticons.ContainsKey(emoticon.Sha))
                {
                    // Add the emotions to owner's emoticon collection.
                    NSMessageHandler.Owner.Emoticons.Add(emoticon.Sha, emoticon);
                }
            }

            NSMessageHandler.SendEmoticonDefinitions(this, emoticons, icontype);
        }
    }
};
