#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

namespace MSNPSharp
{
    using System;
    using System.Net;
    using System.Reflection;
    using System.Collections;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

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

        public ContactStatusChangeEventArgs(Contact contact, PresenceStatus oldStatus)
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
        public delegate void ContactChangedEventHandler(object sender, EventArgs e);
        public delegate void StatusChangedEventHandler(object sender, StatusChangeEventArgs e);

        public event ContactChangedEventHandler ScreenNameChanged;
        public event ContactChangedEventHandler PersonalMessageChanged;
        public event ContactGroupChangedEventHandler ContactGroupAdded;
        public event ContactGroupChangedEventHandler ContactGroupRemoved;
        public event ContactChangedEventHandler ContactBlocked;
        public event ContactChangedEventHandler ContactUnBlocked;
        public event ContactChangedEventHandler ContactOnline;
        public event StatusChangedEventHandler ContactOffline;
        public event StatusChangedEventHandler StatusChanged;

        protected Contact()
        {
        }

        #region Properties
        ClientCapacities clientCapacities = ClientCapacities.None;
        public ClientCapacities ClientCapacities
        {
            get
            {
                return clientCapacities;
            }
            set
            {
                clientCapacities = value;
            }
        }

        string mail;
        public string Mail
        {
            get
            {
                return mail;
            }
        }

        string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        ClientType clienttype = ClientType.PassportMember;
        public ClientType ClientType
        {
            get
            {
                return clienttype;
            }
        }

        PersonalMessage personalMessage;
        public PersonalMessage PersonalMessage
        {
            get
            {
                return personalMessage;
            }
        }

        string guid;
        public string Guid
        {
            get
            {
                return guid;
            }
        }

        PresenceStatus status = PresenceStatus.Offline;
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

        bool hasBlog = false;
        public bool HasBlog
        {
            get
            {
                return hasBlog;
            }
        }

        DisplayImage displayImage;
        public DisplayImage DisplayImage
        {
            get
            {
                return displayImage;
            }
        }

        Hashtable emoticons;
        public Hashtable Emoticons
        {
            get
            {
                if (emoticons == null)
                    emoticons = new Hashtable();

                return emoticons;
            }
        }

        object clientData;
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

        [NonSerialized]
        IMessageHandler nsMessageHandler;
        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return (NSMessageHandler)nsMessageHandler;
            }
            set
            {
                nsMessageHandler = value;
            }
        }

        int oimcount = 1;
        internal int OIMCount
        {
            get
            {
                return oimcount;
            }

            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                oimcount = value;
            }
        }



        #endregion

        #region Phones
        bool mobileDevice = false;
        public bool MobileDevice
        {
            get
            {
                return mobileDevice;
            }
        }

        bool mobileAccess = false;
        public bool MobileAccess
        {
            get
            {
                return mobileAccess;
            }
        }

        string mobilePhone;
        public string MobilePhone
        {
            get
            {
                return mobilePhone;
            }
        }

        string homePhone;
        public string HomePhone
        {
            get
            {
                return homePhone;
            }
        }

        string workPhone;
        public string WorkPhone
        {
            get
            {
                return workPhone;
            }
        }
        #endregion

        #region Internal setters
        internal void SetName(string newName)
        {
            if (name != newName)
            {
                name = System.Web.HttpUtility.UrlDecode(newName, System.Text.Encoding.UTF8);
                if (ScreenNameChanged != null)
                {
                    // notify the user we changed our name
                    ScreenNameChanged(this, new EventArgs());
                }
            }
        }

        internal void SetPersonalMessage(PersonalMessage newpmessage)
        {
            if (personalMessage != newpmessage)
            {
                personalMessage = newpmessage;
                if (PersonalMessageChanged != null)
                {
                    // notify the user we changed our display message
                    PersonalMessageChanged(this, new EventArgs());
                }
            }
        }

        internal void SetHasBlog(bool hasblog)
        {
            this.hasBlog = hasblog;
        }

        internal void SetGuid(string guid)
        {
            this.guid = guid;
        }

        private MSNLists lists = new MSNLists();
        internal void SetLists(MSNLists lists)
        {
            this.lists = lists;
        }

        internal void AddToList(MSNLists list)
        {
            if (list == MSNLists.BlockedList && !Blocked)
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
            if (list == MSNLists.BlockedList && Blocked)
            {
                lists ^= MSNLists.BlockedList;
                if (ContactUnBlocked != null)
                    ContactUnBlocked(this, new EventArgs());
            }
            else
            {
                lists ^= list;

                // set this contact to offline when it is neither on the allow list or on the forward list
                if (OnForwardList == false && OnAllowedList == false)
                {
                    status = PresenceStatus.Offline;
                    //also clear the groups, becase msn loose them when removed from the two lists
                    contactGroups.Clear();
                }
            }
        }

        internal void SetMobileDevice(bool enabled)
        {
            mobileDevice = enabled;
        }

        internal void SetMobileAccess(bool enabled)
        {
            mobileAccess = enabled;
        }

        internal void SetHomePhone(string number)
        {
            homePhone = number;
        }

        internal void SetMobilePhone(string number)
        {
            mobilePhone = number;
        }

        internal void SetWorkPhone(string number)
        {
            workPhone = number;
        }

        internal void SetStatus(PresenceStatus newStatus)
        {
            if (status != newStatus)
            {
                PresenceStatus oldStatus = this.status;
                status = newStatus;

                // raise an event									
                if (StatusChanged != null)
                    StatusChanged(this, new StatusChangeEventArgs(oldStatus));

                // raise the online/offline events
                if (oldStatus == PresenceStatus.Offline && ContactOnline != null)
                    ContactOnline(this, new EventArgs());

                if (newStatus == PresenceStatus.Offline && ContactOffline != null)
                    ContactOffline(this, new StatusChangeEventArgs(oldStatus));
            }
        }

        internal void AddContactToGroup(ContactGroup group)
        {
            if (contactGroups.Contains(group))
                return;

            contactGroups.Add(group);

            if (ContactGroupAdded != null)
                ContactGroupAdded(this, new ContactGroupEventArgs(group));
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



        internal void SetMail(string account)
        {
            mail = account;
        }

        internal void SetUserDisplay(DisplayImage userDisplay)
        {
            displayImage = userDisplay;
        }

        internal void RemoveFromList()
        {
            if (NSMessageHandler != null)
            {
                this.OnAllowedList = false;
                this.OnForwardList = false;
            }
        }

        internal void SetClientType(ClientType type)
        {
            clienttype = type;
        }

        #endregion

        #region Public setters

        private ArrayList contactGroups = new ArrayList();
        public ArrayList ContactGroups
        {
            get
            {
                return contactGroups;
            }
        }

        public bool HasGroup(ContactGroup group)
        {
            return contactGroups.Contains(group);
        }

        internal bool HasLists(MSNLists msnlists)
        {
            return ((lists & msnlists) == msnlists);
        }

        public void UpdateScreenName()
        {
            if (NSMessageHandler != null)
            {
                NSMessageHandler.RequestScreenName(this);
            }
            else
                throw new MSNPSharpException("No valid message handler object");
        }

        public bool Blocked
        {
            get
            {
                return (lists & MSNLists.BlockedList) > 0;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    if (value == true)
                        NSMessageHandler.ContactList.BlockContact(this);
                    else
                        NSMessageHandler.ContactList.UnBlockContact(this);
                }
            }
        }

        public bool OnBlockedList
        {
            get
            {
                return ((lists & MSNLists.BlockedList) == MSNLists.BlockedList);
            }
        }

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
                        NSMessageHandler.ContactList.AddContactToList(this, MSNLists.ForwardList);
                        AddToList(MSNLists.ForwardList);
                    }
                    else
                    {
                        NSMessageHandler.ContactList.RemoveContactFromList(this, MSNLists.ForwardList);
                        RemoveFromList(MSNLists.ForwardList);
                    }
                }
            }
        }

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
                        NSMessageHandler.ContactList.AddContactToList(this, MSNLists.AllowedList);
                        AddToList(MSNLists.AllowedList);
                    }
                    else
                    {
                        NSMessageHandler.ContactList.RemoveContactFromList(this, MSNLists.AllowedList);
                        RemoveFromList(MSNLists.AllowedList);
                    }
                }
            }
        }

        public bool OnReverseList
        {
            get
            {
                return ((lists & MSNLists.ReverseList) == MSNLists.ReverseList);
            }
        }


        public bool OnPendingList
        {
            get
            {
                return ((lists & MSNLists.PendingList) == MSNLists.PendingList);
            }
        }

        #endregion

        public override int GetHashCode()
        {
            return mail.GetHashCode();
        }
    }
};