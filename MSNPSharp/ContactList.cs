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

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    [Serializable()]
    public class ContactIdentifier
    {
        private string mail;
        private ClientType clientType;

        public string Mail
        {
            get
            {
                return mail;
            }
            set
            {
                mail = value;
            }
        }

        public ClientType ClientType
        {
            get
            {
                return clientType;
            }
            set
            {
                clientType = value;
            }
        }

        public ContactIdentifier()
        {
            mail = string.Empty;
        }

        public ContactIdentifier(string account, ClientType type)
        {
            mail = account.ToLowerInvariant();
            clientType = type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            ContactIdentifier ci = obj as ContactIdentifier;
            if ((object)ci == null)
                return false;
            return (ci.Mail == Mail && ci.ClientType == ClientType);
        }

        public override int GetHashCode()
        {
            return Mail.GetHashCode() - ClientType.GetHashCode(); //This is the key line.
        }
    }

    [Serializable()]
    public class ContactList
    {
        Dictionary<ContactIdentifier, Contact> contacts = new Dictionary<ContactIdentifier, Contact>(10);

        [NonSerialized]
        private object syncRoot;
        public object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);

                return syncRoot;
            }
        }

        public class ListEnumerator : IEnumerator
        {
            private IDictionaryEnumerator baseEnum;
            protected IDictionaryEnumerator BaseEnum
            {
                get
                {
                    return baseEnum;
                }
                set
                {
                    baseEnum = value;
                }
            }

            public ListEnumerator(IDictionaryEnumerator listEnum)
            {
                baseEnum = listEnum;
            }

            public virtual bool MoveNext()
            {
                return baseEnum.MoveNext();
            }

            Object IEnumerator.Current
            {
                get
                {
                    return baseEnum.Value;
                }
            }

            public Contact Current
            {
                get
                {
                    return (Contact)baseEnum.Value;
                }
            }

            public void Reset()
            {
                baseEnum.Reset();
            }

            public IEnumerator GetEnumerator()
            {
                return this;
            }
        }

        /// <summary>
        /// Filters the forward list contacts
        /// </summary>
        public class ForwardListEnumerator : ContactList.ListEnumerator
        {
            public ForwardListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnForwardList)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Filters the pending list contacts
        /// </summary>
        public class PendingListEnumerator : ContactList.ListEnumerator
        {
            public PendingListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnPendingList)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Filter the reverse list contacts
        /// </summary>
        public class ReverseListEnumerator : ContactList.ListEnumerator
        {
            public ReverseListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnReverseList)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Filters the blocked list contacts
        /// </summary>
        public class BlockedListEnumerator : ContactList.ListEnumerator
        {
            public BlockedListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    if (((Contact)BaseEnum.Value).Blocked)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Filters the allowed list contacts
        /// </summary>
        public class AllowedListEnumerator : ContactList.ListEnumerator
        {
            public AllowedListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the allowed list
                    if (((Contact)BaseEnum.Value).OnAllowedList)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Filters the email (not IM) contacts
        /// </summary>
        public class EmailListEnumerator : ContactList.ListEnumerator
        {
            public EmailListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the email list
                    Contact c = (Contact)BaseEnum.Value;
                    if (c.Guid != Guid.Empty && c.IsMessengerUser == false)
                        return true;
                }
                return false;
            }
        }

        public ContactList.ForwardListEnumerator Forward
        {
            get
            {
                return new ContactList.ForwardListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.PendingListEnumerator Pending
        {
            get
            {
                return new ContactList.PendingListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.ReverseListEnumerator Reverse
        {
            get
            {
                return new ContactList.ReverseListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.BlockedListEnumerator BlockedList
        {
            get
            {
                return new ContactList.BlockedListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.AllowedListEnumerator Allowed
        {
            get
            {
                return new ContactList.AllowedListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.EmailListEnumerator Email
        {
            get
            {
                return new ContactList.EmailListEnumerator(contacts.GetEnumerator());
            }
        }

        public ContactList.ListEnumerator All
        {
            get
            {
                return new ContactList.ListEnumerator(contacts.GetEnumerator());
            }
        }

        /// <summary>
        /// Get the specified contact.
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <param name="account"></param>
        /// <returns>
        /// If the contact does not exist, return null.
        /// If the specified account has multi-clienttype, the contact with type
        /// <see cref="ClientType.PassportMember"/> will be returned first.
        /// If there's no PassportMember with the specified account, the contact with type 
        /// <see cref="ClientType.EmailMember"/> will be returned.Then the next is <see cref="ClientType.PhoneMember"/>
        /// ,<see cref="ClientType.LCS"/> and so on...
        /// </returns>
        internal Contact GetContact(string account)
        {
            if (!HasContact(account))
                return null;
            if (HasContact(account, ClientType.PassportMember))
                return GetContact(account, ClientType.PassportMember);
            if (HasContact(account, ClientType.EmailMember))
                return GetContact(account, ClientType.EmailMember);
            if (HasContact(account, ClientType.PhoneMember))
                return GetContact(account, ClientType.PhoneMember);
            if (HasContact(account, ClientType.LCS))
                return GetContact(account, ClientType.LCS);
            return null;

        }

        /// <summary>
        /// Get the specified contact.
        /// <para>This overload will set the contact name to a specified value (if the contact exists.).</para>
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <returns>
        /// If the contact does not exist, return null.
        /// If the specified account has multi-clienttype, the contact with type
        /// <see cref="ClientType.PassportMember"/> will be returned first.
        /// If there's no PassportMember with the specified account, the contact with type 
        /// <see cref="ClientType.EmailMember"/> will be returned.Then the next is <see cref="ClientType.PhoneMember"/>
        /// ,<see cref="ClientType.LCS"/> and so on...
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
        /// If the contact does not exist, return null.
        /// </returns>
        internal Contact GetContact(string account, string name, ClientType type)
        {
            Contact contact = GetContact(account, type);

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
        /// If the contact does not exist, return null.
        /// </returns>
        internal Contact GetContact(string account, ClientType type)
        {
            ContactIdentifier cid = new ContactIdentifier(account, type);
            if (HasContact(account, type))
            {
                return contacts[cid];
            }

            Contact tmpContact = Factory.CreateContact();

            tmpContact.SetMail(account);
            tmpContact.SetName(account);
            tmpContact.SetClientType(type);

            lock (SyncRoot)
                contacts.Add(cid, tmpContact);

            return GetContact(account, type);
        }

        public Contact GetContactByGuid(Guid guid)
        {
            foreach (Contact c in contacts.Values)
            {
                if (c.Guid == guid)
                    return c;
            }

            return null;
        }

        public Contact this[string account]
        {
            get
            {
                return GetContact(account);
            }

            set
            {
                this[account, value.ClientType] = value;
            }
        }

        public Contact this[string account, ClientType type]
        {
            get
            {
                return GetContact(account, type);
            }
            set
            {
                this[account, type] = value;
            }
        }

        /// <summary>
        /// Check whether the specified account is in the contact list.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool HasContact(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            lock (SyncRoot)
            {
                foreach (ContactIdentifier cid in contacts.Keys)
                    if (cid.Mail == account)
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
        public bool HasContact(string account, ClientType type)
        {
            ContactIdentifier cid = new ContactIdentifier(account, type);
            return contacts.ContainsKey(cid);
        }

        public void CopyTo(Contact[] array, int index)
        {
            lock (contacts)
                contacts.Values.CopyTo(array, index);
        }

        internal void Clear()
        {
            contacts.Clear();
        }

        /// <summary>
        /// Remove all the contacts with the specified account.
        /// </summary>
        /// <param name="account"></param>
        internal void Remove(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            List<ContactIdentifier> removecid = new List<ContactIdentifier>(0);
            if (HasContact(account))
            {
                lock (SyncRoot)
                {
                    foreach (ContactIdentifier cid in contacts.Keys)
                    {
                        if (cid.Mail == account)
                            removecid.Add(cid);
                    }
                    foreach (ContactIdentifier rcid in removecid)
                        contacts.Remove(rcid);
                }
            }
        }

        /// <summary>
        /// Remove a contact with specified account and client type.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="type"></param>
        internal void Remove(string account, ClientType type)
        {
            lock (SyncRoot)
            {
                contacts.Remove(new ContactIdentifier(account, type));
            }
        }

        private bool HasMultiType(string account)
        {
            int typecount = 0;

            lock (SyncRoot)
                foreach (ContactIdentifier cid in contacts.Keys)
                    if (cid.Mail == account.ToLowerInvariant())
                    {
                        if (++typecount == 2)
                            return true;
                    }

            return false;
        }
    }
};