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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// User in roster list.
    /// </summary>
    [Serializable()]
    public class Contact
    {
        #region Fields

        protected Guid guid;
        protected Guid addressBookId;
        private long cid;
        private string mail;
        private string name;
        private string nickName;

        private string homePhone;
        private string workPhone;
        private string mobilePhone;
        private string contactType;
        private string comment = string.Empty;

        private bool hasSpace;
        private bool mobileDevice;
        private bool mobileAccess;
        private bool isMessengerUser;

        private ClientCapacities clientCapacities = ClientCapacities.None;
        private ClientCapacitiesEx clientCapacitiesEx = ClientCapacitiesEx.None;
        private PresenceStatus status = PresenceStatus.Offline;
        private ClientType clientType = ClientType.PassportMember;
        private CirclePersonalMembershipRole circleRole = CirclePersonalMembershipRole.None;

        private List<ContactGroup> contactGroups = new List<ContactGroup>(0);
        private MSNLists lists = MSNLists.None;

        private DisplayImage displayImage;
        private PersonalMessage personalMessage;


        private Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>(0);
        private Dictionary<string, Contact> siblings = new Dictionary<string, Contact>(0);

        private ulong oimCount = 1;
        private int adlCount = 1;
        private object clientData;

        private List<ActivityDetailsType> activities = new List<ActivityDetailsType>(0);
        private Uri userTile;

        private object syncObject = new object();

        [NonSerialized]
        private NSMessageHandler nsMessageHandler;

        #endregion

        private Contact()
        {
        }

        protected internal Contact(string abId, string account, ClientType cliType, NSMessageHandler handler)
        {
            NSMessageHandler = handler;
            addressBookId = new Guid(abId);
            mail = account.ToLowerInvariant();
            clientType = cliType;
            SetName(account);

            if (NSMessageHandler != null)
            {
                NSMessageHandler.Manager.Add(this);
            }
        }

        protected internal Contact(Guid abId, string account, ClientType cliType, NSMessageHandler handler)
        {
            NSMessageHandler = handler;
            addressBookId = abId;
            mail = account.ToLowerInvariant();
            clientType = cliType;
            SetName(account);

            if (NSMessageHandler != null)
            {
                NSMessageHandler.Manager.Add(this);
            }
        }

        #region Events

        public event EventHandler<EventArgs> ScreenNameChanged;
        public event EventHandler<EventArgs> PersonalMessageChanged;
        public event EventHandler<EventArgs> DisplayImageChanged;
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


        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }

            internal protected set
            {
                nsMessageHandler = value;
            }
        }

        /// <summary>
        /// The display image url from the webside.
        /// </summary>
        public Uri UserTile
        {
            get
            {
                return userTile;
            }
            set
            {
                userTile = value;
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
        /// EndPoint ID
        /// </summary>
        public Guid MachineGuid
        {
            get
            {
                if (PersonalMessage != null)
                {
                    if (PersonalMessage.MachineGuid != null)
                    {
                        if (PersonalMessage.MachineGuid != Guid.Empty)
                            return PersonalMessage.MachineGuid;
                    }
                }
                return Guid.Empty;
            }
        }

        /// <summary>
        /// The contact id of contact, only PassportMembers have CID.
        /// </summary>
        public long CID
        {
            get
            {
                return cid;
            }

            internal set
            {
                cid = value;
            }
        }

        /// <summary>
        /// The email account of contact.
        /// </summary>
        public string Mail
        {
            get
            {
                return mail;
            }
        }

        /// <summary>
        /// The display name of contact.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        public string HomePhone
        {
            get
            {
                return homePhone;
            }
        }

        public string WorkPhone
        {
            get
            {
                return workPhone;
            }
        }

        public string MobilePhone
        {
            get
            {
                return mobilePhone;
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
                NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
            }
        }

        public ClientCapacities ClientCapacities
        {
            get
            {
                return clientCapacities;
            }
            set
            {
                clientCapacities = value;

                if ((clientCapacities & ClientCapacities.HasMSNSpaces) == ClientCapacities.HasMSNSpaces)
                {
                    SetHasSpace(true);
                }
            }
        }

        public ClientCapacitiesEx ClientCapacitiesEx
        {
            get
            {
                return clientCapacitiesEx;
            }
            set
            {
                clientCapacitiesEx = value;
            }
        }

        /// <summary>
        /// The online status of contact.
        /// </summary>
        public PresenceStatus Status
        {
            get
            {
                return status;
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
        public ClientType ClientType
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

        public DisplayImage DisplayImage
        {
            get
            {
                return displayImage;
            }

            set
            {
                if (displayImage != value)
                {
                    displayImage = value;

                    OnDisplayImageChanged();
                }
            }
        }

        public PersonalMessage PersonalMessage
        {
            get
            {
                return personalMessage;
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
                return MakeHash(Mail, ClientType, AddressBookId);
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
                if (NSMessageHandler != null && Guid != Guid.Empty && ClientType == ClientType.PassportMember)
                {
                    if (value)
                    {
                        if (!AutoSubscribeToUpdates)
                        {
                            contactType = MessengerContactType.LivePending;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
                        }
                    }
                    else
                    {
                        if (contactType != MessengerContactType.Regular)
                        {
                            contactType = MessengerContactType.Regular;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the contact is a mail contact or a messenger buddy.
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
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
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
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
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
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId);
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
            get { return adlCount; }
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

        public bool OnForwardList
        {
            get
            {
                return ((lists & MSNLists.ForwardList) == MSNLists.ForwardList);
            }
            set
            {
                if (value != OnForwardList)
                {
                    if (value)
                    {
                        NSMessageHandler.ContactService.AddContactToList(this, MSNLists.ForwardList, null);
                    }
                    else
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.ForwardList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Blocks/unblocks this contact. If blocked, will be placed in your BL and removed
        /// from your AL; otherwise, will be removed from your BL and placed in your AL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set the <see cref="OnAllowedList"/> or <see cref="OnBlockedList"/> to false.
        /// </summary>
        public bool Blocked
        {
            get
            {
                return OnBlockedList;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    if (value)
                        NSMessageHandler.ContactService.BlockContact(this);
                    else
                        NSMessageHandler.ContactService.UnBlockContact(this);
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
                return ((lists & MSNLists.AllowedList) == MSNLists.AllowedList);
            }
            set
            {
                if (value != OnAllowedList)
                {
                    if (value)
                    {
                        Blocked = false;
                    }
                    else if (!OnReverseList)
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.AllowedList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Adds or removes this contact into/from your BL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set this property to false.
        /// </summary>
        public bool OnBlockedList
        {
            get
            {
                return ((lists & MSNLists.BlockedList) == MSNLists.BlockedList);
            }
            set
            {
                if (value != OnBlockedList)
                {
                    if (value)
                    {
                        Blocked = true;
                    }
                    else if (!OnReverseList)
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.BlockedList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the contact have you on their contact list. 
        /// </summary>
        public bool OnReverseList
        {
            get
            {
                return ((lists & MSNLists.ReverseList) == MSNLists.ReverseList);
            }
        }

        /// <summary>
        /// Indicates whether the contact have you on their contact list and pending your approval. 
        /// </summary>
        public bool OnPendingList
        {
            get
            {
                return ((lists & MSNLists.PendingList) == MSNLists.PendingList);
            }
            set
            {
                if (value != OnPendingList && value == false)
                {
                    NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.PendingList, null);
                }
            }
        }

        /// <summary>
        /// The msn lists this contact has.
        /// </summary>
        public MSNLists Lists
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

        internal void SetHomePhone(string number)
        {
            homePhone = number;
        }

        internal void SetIsMessengerUser(bool isMessengerEnabled)
        {
            isMessengerUser = isMessengerEnabled;
            NotifyManager();
        }

        internal void SetList(MSNLists msnLists)
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

        internal void SetMobilePhone(string number)
        {
            mobilePhone = number;
        }

        internal void SetWorkPhone(string number)
        {
            workPhone = number;
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

        internal void SetHasSpace(bool hasSpaceValue)
        {
            hasSpace = hasSpaceValue;
        }

        internal void SetNickName(string newNick)
        {
            nickName = newNick;
        }

        internal void SetPersonalMessage(PersonalMessage newpmessage)
        {
            if (personalMessage != newpmessage)
            {
                personalMessage = newpmessage;
                // notify the user we changed our display message
                OnPersonalMessageChanged(newpmessage);
            }
        }

        internal void SetStatus(PresenceStatus newStatus)
        {
            lock (syncObject)
            {
                if (status != newStatus)
                {
                    PresenceStatus oldStatus = status;
                    status = newStatus;

                    // raise an event									
                    OnStatusChanged(oldStatus);

                    // raise the online/offline events
                    if (oldStatus == PresenceStatus.Offline)
                        OnContactOnline(oldStatus);

                    if (newStatus == PresenceStatus.Offline)
                        OnContactOffline(oldStatus);
                }
            }
        }

        internal void SetDisplayImage(DisplayImage image)
        {
            displayImage = image;
        }

        internal void NotifyManager()
        {
            if (AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
                return;

            if (NSMessageHandler == null)
                return;

            NSMessageHandler.Manager.SyncProperties(this);
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

        protected virtual void OnStatusChanged(PresenceStatus oldStatus)
        {
            if (StatusChanged != null)
                StatusChanged(this, new StatusChangedEventArgs(oldStatus));
        }

        protected virtual void OnContactOnline(PresenceStatus oldStatus)
        {
            if (ContactOnline != null)
                ContactOnline(this, new StatusChangedEventArgs(oldStatus));
        }

        protected virtual void OnContactOffline(PresenceStatus oldStatus)
        {
            if (ContactOffline != null)
            {
                ContactOffline(this, new StatusChangedEventArgs(oldStatus));
            }
        }

        protected virtual void OnDisplayImageChanged()
        {
            if (DisplayImageChanged != null)
            {
                DisplayImageChanged(this, EventArgs.Empty);
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

        internal void AddToList(MSNLists list)
        {
            if ((lists & list) == MSNLists.None)
            { 
                lists |= list;

                if ((list & MSNLists.BlockedList) == MSNLists.BlockedList)
                {
                    OnContactBlocked();
                }
            }

            NotifyManager();
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

        internal void RemoveFromList(MSNLists list)
        {
            if ((lists & list) != MSNLists.None)
            {
                lists ^= list;

                // set this contact to offline when it is neither on the allow list or on the forward list
                if (!(OnForwardList || OnAllowedList))
                {
                    status = PresenceStatus.Offline;
                    //also clear the groups, becase msn loose them when removed from the two lists
                    contactGroups.Clear();
                }

                if ((list & MSNLists.BlockedList) == MSNLists.BlockedList)
                {
                    OnContactUnBlocked();
                }
            }

            NotifyManager();
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

        internal static string MakeHash(string account, ClientType type, Guid abId)
        {
            return type.ToString() + ":" + account.ToLowerInvariant() + ";via=" + abId.ToString("D").ToLowerInvariant();
        }

        internal static string MakeHash(string account, ClientType type, string abId)
        {
            return type.ToString() + ":" + account.ToLowerInvariant() + ";via=" + abId.ToLowerInvariant();
        }

        internal bool HasLists(MSNLists msnlists)
        {
            return ((lists & msnlists) == msnlists);
        }


        #endregion

        public bool HasGroup(ContactGroup group)
        {
            return contactGroups.Contains(group);
        }

        public void UpdateScreenName()
        {
            if (NSMessageHandler == null)
                throw new MSNPSharpException("No valid message handler object");

            NSMessageHandler.RequestScreenName(this);
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

    }
};
