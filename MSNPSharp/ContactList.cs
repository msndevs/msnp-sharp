#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    [Serializable]
    public class ContactList : Dictionary<IMAddressInfoType, Dictionary<string, Contact>>
    {
        private static IMAddressInfoType[] addressTypes = (IMAddressInfoType[])Enum.GetValues(typeof(IMAddressInfoType));

        [NonSerialized]
        private NSMessageHandler nsMessageHandler;
        [NonSerialized]
        private object syncRoot;

        private Guid addressBookId = Guid.Empty;
        private Owner owner = null;

        public ContactList(NSMessageHandler handler)
            : this(WebServiceConstants.MessengerIndividualAddressBookId, null, handler)
        {
        }

        public ContactList(string abId, Owner owner, NSMessageHandler handler)
            : this(new Guid(abId), owner, handler)
        {
        }

        public ContactList(Guid abId, Owner owner, NSMessageHandler handler)
        {
            Reset();

            this.addressBookId = abId;
            this.nsMessageHandler = handler;
            this.owner = owner;
        }

        #region ListEnumerators

        public class ListEnumerator : IEnumerator<Contact>
        {
            private Dictionary<string, Contact>.Enumerator baseEnum;
            private RoleLists listFilter;

            public ListEnumerator(Dictionary<string, Contact>.Enumerator listEnum, RoleLists filter)
            {
                baseEnum = listEnum;
                listFilter = filter;
            }

            public virtual bool MoveNext()
            {
                if (listFilter == RoleLists.None)
                {
                    return baseEnum.MoveNext();
                }

                while (baseEnum.MoveNext())
                {
                    if (Current.HasLists(listFilter))
                        return true;
                }

                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    return baseEnum.Current;
                }
            }

            public Contact Current
            {
                get
                {
                    return baseEnum.Current.Value;
                }
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                baseEnum.Dispose();
            }

            public IEnumerator<Contact> GetEnumerator()
            {
                return this;
            }
        }

        public class EmailListEnumerator : ContactList.ListEnumerator
        {
            public EmailListEnumerator(Dictionary<string, Contact>.Enumerator listEnum)
                : base(listEnum, RoleLists.None)
            {
            }

            public override bool MoveNext()
            {
                while (base.MoveNext())
                {
                    if (Current.Guid != Guid.Empty && Current.IsMessengerUser == false)
                        return true;
                }
                return false;
            }
        }

        #endregion

        #region Lists

        #region windows live

        /// <summary>
        /// All Windows Live contacts including Forward, Allow, Block, Reverse, Pending, Email, Hide.
        /// </summary>
        public ContactList.ListEnumerator All
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.None);
            }
        }

        /// <summary>
        /// All Windows Live contacts on your address book.
        /// </summary>
        public ContactList.ListEnumerator Forward
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Forward);
            }
        }

        /// <summary>
        /// All Windows Live contacts on your allowed list who can send instant messages.
        /// </summary>
        public ContactList.ListEnumerator Allowed
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Allow);
            }
        }

        /// <summary>
        /// All Windows Live contacts on your allowed list who CANNOT send instant messages.
        /// </summary>
        public ContactList.ListEnumerator BlockedList
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Block);
            }
        }

        /// <summary>
        /// All Windows Live contacts on your hidden list who CANNOT see your status but CAN send offline messages.
        /// </summary>
        public ContactList.ListEnumerator HiddenList
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Hide);
            }
        }

        /// <summary>
        /// All Windows Live contacts who have you on their contactlist.
        /// </summary>
        public ContactList.ListEnumerator Reverse
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Reverse);
            }
        }

        /// <summary>
        ///  All pending Windows Live contacts.
        /// </summary>
        public ContactList.ListEnumerator Pending
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator(), RoleLists.Pending);
            }
        }

        /// <summary>
        /// All Windows Live contacts on your email list. IsMessengerUser property is false.
        /// </summary>
        public ContactList.ListEnumerator Email
        {
            get
            {
                return new ContactList.EmailListEnumerator(base[IMAddressInfoType.WindowsLive].GetEnumerator());
            }
        }

        #endregion

        #region circle

        public ContactList.ListEnumerator Circles
        {
            get
            {
                return new ContactList.ListEnumerator(base[IMAddressInfoType.Circle].GetEnumerator(), RoleLists.None);
            }
        }

        #endregion

        #endregion

        #region Properties

        public object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                }
                return syncRoot;
            }
        }

        /// <summary>
        /// The addressbook identifier of this addressbook.
        /// </summary>
        public Guid AddressBookId
        {
            get
            {
                return addressBookId;
            }
        }

        /// <summary>
        /// The owner of the contactlist. This is the identity that logged into the messenger network.
        /// </summary>
        public Owner Owner
        {
            get
            {
                return owner;
            }
        }

        public Contact this[string account]
        {
            get
            {
                return GetContact(account);
            }
            set
            {
                IMAddressInfoType key = value.ClientType;
                string hash = Contact.MakeHash(account, value.ClientType, AddressBookId.ToString("D"));
                base[key][hash] = value;
            }
        }

        public Contact this[string account, IMAddressInfoType type]
        {
            get
            {
                return GetContact(account, type);
            }
            set
            {
                IMAddressInfoType key = value.ClientType;
                string hash = Contact.MakeHash(account, value.ClientType, AddressBookId.ToString("D"));
                base[key][hash] = value;
            }
        }

        #endregion

        /// <summary>
        /// Get the specified contact.
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <returns>
        /// If the contact does not exist, returns null.
        /// </returns>
        public Contact GetContact(string account)
        {
            foreach (IMAddressInfoType addressType in addressTypes)
            {
                if (HasContact(account, addressType))
                    return GetContact(account, addressType);
            }

            return null;
        }

        public Contact GetContact(string account, IMAddressInfoType type)
        {
            string hash = Contact.MakeHash(account, type, AddressBookId.ToString("D"));
            if (base[type].ContainsKey(hash))
            {
                return base[type][hash];
            }
            return null;
        }

        public Contact GetContactByGuid(Guid guid)
        {
            if (guid != Guid.Empty)
            {
                lock (SyncRoot)
                {
                    foreach (IMAddressInfoType addressType in addressTypes)
                    {
                        foreach (Contact contact in base[addressType].Values)
                        {
                            if (contact.Guid == guid)
                                return contact;
                        }
                    }
                }
            }
            return null;
        }

        public Contact GetContactByCID(long cid)
        {
            if (cid != 0)
            {
                lock (SyncRoot)
                {
                    foreach (IMAddressInfoType addressType in addressTypes)
                    {
                        foreach (Contact contact in base[addressType].Values)
                        {
                            if (contact.CID == cid)
                                return contact;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check whether the specified account is in the contact list.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool HasContact(string account)
        {
            foreach (IMAddressInfoType ct in addressTypes)
            {
                if (HasContact(account, ct))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether the account with specified client type is in the contact list.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasContact(string account, IMAddressInfoType type)
        {
            string hash = Contact.MakeHash(account, type, AddressBookId.ToString("D"));
            return base[type].ContainsKey(hash);
        }

        public bool HasMultiType(string account)
        {
            int typecount = 0;
            foreach (IMAddressInfoType ct in addressTypes)
            {
                if (HasContact(account, ct) && ++typecount > 1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a contact with specified account and client type.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="type"></param>
        public bool Remove(string account, IMAddressInfoType type)
        {
            string hash = Contact.MakeHash(account, type, AddressBookId.ToString("D"));
            lock (SyncRoot)
            {
                return base[type].Remove(hash);
            }
        }

        /// <summary>
        /// Reset the contact list and clear the owner.
        /// </summary>
        public void Reset()
        {
            if (Owner != null)
            {
                Owner.Emoticons.Clear();
                Owner.EndPointData.Clear();
                Owner.LocalEndPointClientCapabilities = ClientCapabilities.None;
                Owner.LocalEndPointClientCapabilitiesEx = ClientCapabilitiesEx.None;
            }

            owner = null;

            lock (SyncRoot)
            {
                foreach (IMAddressInfoType addressType in addressTypes)
                {
                    base[addressType] = new Dictionary<string, Contact>();
                }
            }
        }

        /// <summary>
        /// Copy the whole contact list out.
        /// </summary>
        /// <returns></returns>
        public Contact[] ToArray(IMAddressInfoType type)
        {
            lock (SyncRoot)
            {
                Contact[] array = new Contact[base[type].Values.Count];
                base[type].Values.CopyTo(array, 0);
                return array;
            }
        }

        #region Internal

        /// <summary>
        /// Get the specified contact.
        /// <para>This overload will set the contact name to a specified value (if the contact exists.).</para>
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <returns>
        /// If the contact does not exist, returns null.
        /// </returns>
        internal Contact GetContact(string account, string name)
        {
            Contact contact = GetContact(account);

            if (contact != null)
                lock (SyncRoot)
                    contact.SetName(name);

            return contact;
        }

        /// <summary>
        /// Get a contact with specified account and client type, if the contact does not exist, create it.
        /// <para>This overload will set the contact name to a specified value.</para>
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name">The new name you want to set.</param>
        /// <param name="type"></param>
        /// <returns>
        /// A <see cref="Contact"/> object.
        /// If the contact does not exist, create it.
        /// </returns>
        internal Contact GetContact(string account, string name, IMAddressInfoType type)
        {
            Contact contact = GetContactWithCreate(account, type);

            lock (SyncRoot)
                contact.SetName(name);

            return contact;
        }

        /// <summary>
        /// Get a contact with specified account and client type, if the contact does not exist, create it.
        /// </summary>
        /// <param name="account">Account (Mail) of a contact</param>
        /// <param name="type">Contact type.</param>
        /// <returns>
        /// A <see cref="Contact"/> object.
        /// If the contact does not exist, create it.
        /// </returns>
        internal Contact GetContactWithCreate(string account, IMAddressInfoType type)
        {
            string hash = Contact.MakeHash(account, type, AddressBookId.ToString("D"));

            if (base[type].ContainsKey(hash))
            {
                return base[type][hash];
            }

            Contact tmpContact = new Contact(AddressBookId, account, type, 0, nsMessageHandler);

            lock (SyncRoot)
                base[type][hash] = tmpContact;

            return GetContact(account, type);
        }

        /// <summary>
        /// Remove all the contacts with the specified account.
        /// </summary>
        /// <param name="account"></param>
        internal void Remove(string account)
        {
            foreach (IMAddressInfoType ct in addressTypes)
            {
                if (HasContact(account, ct))
                    Remove(account, ct);
            }
        }

        /// <summary>
        /// Set the owner for default addressbook. This funcation can be only called once.
        /// </summary>
        /// <param name="owner"></param>
        internal void SetOwner(Owner owner)
        {
            if (AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                throw new InvalidOperationException("Only default addressbook can call this function.");
            }

            if (Owner != null)
            {
                throw new InvalidOperationException("Owner already set.");
            }

            if (owner.AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                throw new InvalidOperationException("Invalid owner: This is not the owner for default addressbook.");
            }

            this.owner = owner;
        }

        #endregion
    }
};