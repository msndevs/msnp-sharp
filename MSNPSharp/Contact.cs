#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
    using MSNPSharp.DataTransfer;
    using MSNPSharp.MSNWS.MSNABSharingService;

    [Serializable()]
    public class ContactStatusChangeEventArgs : EventArgs
    {
        Contact contact;
        PresenceStatus oldStatus;

        public Contact Contact
        {
            get
            {
                return contact;
            }
            set
            {
                contact = value;
            }
        }

        public PresenceStatus OldStatus
        {
            get
            {
                return oldStatus;
            }
            set
            {
                oldStatus = value;
            }
        }

        public ContactStatusChangeEventArgs(Contact contact,
                                            PresenceStatus oldStatus)
        {
            Contact = contact;
            OldStatus = oldStatus;
        }
    }


    [Serializable()]
    public class ContactEventArgs : System.EventArgs
    {
        Contact contact;

        public Contact Contact
        {
            get
            {
                return contact;
            }
            set
            {
                contact = value;
            }
        }

        public ContactEventArgs(Contact contact)
        {
            Contact = contact;
        }
    }

    [Serializable()]
    public class StatusChangeEventArgs : EventArgs
    {
        private PresenceStatus oldStatus;

        public PresenceStatus OldStatus
        {
            get
            {
                return oldStatus;
            }
            set
            {
                oldStatus = value;
            }
        }

        public StatusChangeEventArgs(PresenceStatus oldStatus)
        {
            OldStatus = oldStatus;
        }
    }

    [Serializable()]
    public class Contact
    {
        #region Fields

        private Guid guid;
        private long? cid;
        private string mail;
        private string name;
        private string nickName;

        private string homePhone;
        private string workPhone;
        private string mobilePhone;

        private bool hasBlog;
        private bool mobileDevice;
        private bool mobileAccess;
        private bool isMessengerUser;

        private ClientCapacities clientCapacities = ClientCapacities.None;
        private ClientCapacitiesEx clientCapacitiesEx = ClientCapacitiesEx.None;
        private PresenceStatus status = PresenceStatus.Offline;
        private ClientType clientType = ClientType.PassportMember;
        private string contactType;

        private List<ContactGroup> contactGroups = new List<ContactGroup>(0);
        private MSNLists lists = MSNLists.None;

        private DisplayImage displayImage;
        private PersonalMessage personalMessage;
        private string comment = string.Empty;

        private Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>(0);
        private ulong oimCount = 1;
        private object clientData;

        [NonSerialized]
        private NSMessageHandler nsMessageHandler;

        #endregion

        protected Contact()
        {
        }

        #region Events

        public event EventHandler<EventArgs> ScreenNameChanged;
        public event EventHandler<EventArgs> PersonalMessageChanged;
        public event EventHandler<EventArgs> DisplayImageChanged;
        public event EventHandler<ContactGroupEventArgs> ContactGroupAdded;
        public event EventHandler<ContactGroupEventArgs> ContactGroupRemoved;
        public event EventHandler<EventArgs> ContactBlocked;
        public event EventHandler<EventArgs> ContactUnBlocked;
        public event EventHandler<EventArgs> ContactOnline;
        public event EventHandler<StatusChangeEventArgs> ContactOffline;
        public event EventHandler<StatusChangeEventArgs> StatusChanged;

        #endregion

        #region Contact Properties

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
        /// The Guid of contact, NOT CID.
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
        /// MachineGuid in PersonalMessage.
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
        /// The contact id of contact, PassportMembers have CID only.
        /// </summary>
        public long? CID
        {
            get
            {
                return cid;
            }
            set
            {
                cid = value;
            }
        }

        public string Mail
        {
            get
            {
                return mail;
            }
            internal set
            {
                mail = value;
            }
        }

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

        public bool HasBlog
        {
            get
            {
                return hasBlog;
            }
            internal set
            {
                hasBlog = value;
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
                    HasBlog = true;
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

        public PresenceStatus Status
        {
            get
            {
                return status;
            }
        }

        public bool Online
        {
            get
            {
                return status != PresenceStatus.Offline;
            }
        }

        public ClientType ClientType
        {
            get
            {
                return clientType;
            }
            internal set
            {
                clientType = value;
            }
        }

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

                    if (DisplayImageChanged != null)
                    {
                        DisplayImageChanged(this, EventArgs.Empty);
                    }
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

        public virtual string Hash
        {
            get
            {
                return MakeHash(Mail, ClientType);
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
                            NSMessageHandler.ContactService.UpdateContact(this);
                        }
                    }
                    else
                    {
                        if (contactType != MessengerContactType.Regular)
                        {
                            contactType = MessengerContactType.Regular;
                            NSMessageHandler.ContactService.UpdateContact(this);
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
                    NSMessageHandler.ContactService.UpdateContact(this);
                }
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
                    NSMessageHandler.ContactService.UpdateContact(this);
                }
            }
        }

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
                    NSMessageHandler.ContactService.UpdateContact(this);
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
                name = System.Web.HttpUtility.UrlDecode(newName, System.Text.Encoding.UTF8);

                if (ScreenNameChanged != null)
                {
                    // notify the user we changed our name
                    ScreenNameChanged(this, EventArgs.Empty);
                }
            }
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

                if (PersonalMessageChanged != null)
                {
                    // notify the user we changed our display message
                    PersonalMessageChanged(this, EventArgs.Empty);
                }
            }
        }

        internal void SetStatus(PresenceStatus newStatus)
        {
            if (status != newStatus)
            {
                PresenceStatus oldStatus = status;
                status = newStatus;

                // raise an event									
                if (StatusChanged != null)
                    StatusChanged(this, new StatusChangeEventArgs(oldStatus));

                // raise the online/offline events
                if (oldStatus == PresenceStatus.Offline && ContactOnline != null)
                    ContactOnline(this, EventArgs.Empty);

                if (newStatus == PresenceStatus.Offline && ContactOffline != null)
                    ContactOffline(this, new StatusChangeEventArgs(oldStatus));
            }
        }

        #endregion

        #region Internal contact operations

        internal void AddContactToGroup(ContactGroup group)
        {
            if (!contactGroups.Contains(group))
            {
                contactGroups.Add(group);

                if (ContactGroupAdded != null)
                    ContactGroupAdded(this, new ContactGroupEventArgs(group));
            }
        }

        internal void RemoveContactFromGroup(ContactGroup group)
        {
            if (contactGroups.Contains(group))
            {
                contactGroups.Remove(group);

                if (ContactGroupRemoved != null)
                    ContactGroupRemoved(this, new ContactGroupEventArgs(group));
            }
        }

        internal void AddToList(MSNLists list)
        {
            if (list == MSNLists.BlockedList && !OnBlockedList)
            {
                lists |= MSNLists.BlockedList;

                if (ContactBlocked != null)
                    ContactBlocked(this, new EventArgs());
            }
            else
            {
                lists |= list;
            }
        }

        internal void RemoveFromList(MSNLists list)
        {
            if (list == MSNLists.BlockedList && OnBlockedList)
            {
                lists ^= MSNLists.BlockedList;

                if (ContactUnBlocked != null)
                    ContactUnBlocked(this, EventArgs.Empty);
            }
            else
            {
                lists ^= list;

                // set this contact to offline when it is neither on the allow list or on the forward list
                if (!(OnForwardList || OnAllowedList))
                {
                    status = PresenceStatus.Offline;
                    //also clear the groups, becase msn loose them when removed from the two lists
                    contactGroups.Clear();
                }
            }
        }

        internal void RemoveFromList()
        {
            if (NSMessageHandler != null)
            {
                OnAllowedList = false;
                OnForwardList = false;
            }
        }

        internal static string MakeHash(string account, ClientType type)
        {
            return account.ToLowerInvariant() + ":" + type.ToString();
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
    }
};
