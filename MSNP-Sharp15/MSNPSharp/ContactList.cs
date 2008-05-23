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
using System.Collections;
using System.Collections.Generic;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;
using System.Globalization;

namespace MSNPSharp
{

	[Serializable()]
	public class ContactList
	{		
		Dictionary<string, Contact> contacts 
			= new Dictionary<string, Contact> (10);

		public class ListEnumerator : IEnumerator
		{
			private IDictionaryEnumerator baseEnum;

			protected IDictionaryEnumerator BaseEnum
			{
				get { return baseEnum; }
				set { baseEnum = value;}
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
				get { return baseEnum.Value; }
			}

			public Contact Current 
			{
				get { 
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

		//filters the forward list contacts
		public class ForwardListEnumerator : ContactList.ListEnumerator
		{
			public ForwardListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			public override bool MoveNext()
			{					
				while(BaseEnum.MoveNext())
				{
					// filter on the forward boolean
					if(((Contact)BaseEnum.Value).OnForwardList)
						return true;
				}
				
				return false;
			}
		}

		//filters the pending list contacts
		public class PendingListEnumerator : ContactList.ListEnumerator
		{
			public PendingListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			public override bool MoveNext()
			{					
				while(BaseEnum.MoveNext())
				{
					// filter on the forward boolean
					if(((Contact)BaseEnum.Value).OnPendingList)
						return true;
				}
				
				return false;
			}
		}

		//filter the reverse list contacts
		public class ReverseListEnumerator : ContactList.ListEnumerator
		{
			public ReverseListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			public override bool MoveNext()
			{					
				while(BaseEnum.MoveNext())
				{
					// filter on the forward boolean
					if(((Contact)BaseEnum.Value).OnReverseList)
						return true;
				}
				return false;
			}
		}

		//filters the blocked list contacts
		public class BlockedListEnumerator : ContactList.ListEnumerator
		{
			public BlockedListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			public override bool MoveNext()
			{					
				while(BaseEnum.MoveNext())
				{
					if(((Contact)BaseEnum.Value).Blocked)
						return true;
				}
				return false;
			}
		}

		//filters the allowed list contacts
		public class AllowedListEnumerator : ContactList.ListEnumerator
		{
			public AllowedListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			public override bool MoveNext()
			{
				while(BaseEnum.MoveNext())
				{
					// filter on the allowed list
					if(((Contact)BaseEnum.Value).OnAllowedList)
						return true;
				}
				return false;
			}
		}

		public ContactList.ForwardListEnumerator Forward
		{
			get { 
				return new ContactList.ForwardListEnumerator(contacts.GetEnumerator()); 
			}
		}
		
		public ContactList.PendingListEnumerator Pending
		{
			get { 
				return new ContactList.PendingListEnumerator(contacts.GetEnumerator()); 
			}
		}

		public ContactList.ReverseListEnumerator Reverse
		{
			get { 
				return new ContactList.ReverseListEnumerator(contacts.GetEnumerator()); 
			}
		}

		public ContactList.BlockedListEnumerator BlockedList
		{
			get { 
				return new ContactList.BlockedListEnumerator(contacts.GetEnumerator()); 
			}
		}

		public ContactList.AllowedListEnumerator Allowed
		{
			get { 
				return new ContactList.AllowedListEnumerator(contacts.GetEnumerator()); 
			}
		}

		public ContactList.ListEnumerator All
		{
			get { 
				return new ContactList.ListEnumerator(contacts.GetEnumerator()); 
			}
		}

		internal Contact GetContact(string account)
		{
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (contacts.ContainsKey(account))
            {
                return contacts[account];
            }
            else
            {
                Contact tmpContact = Factory.CreateContact();

                tmpContact.SetMail(account);
                tmpContact.SetName(account);

                contacts.Add(account, tmpContact);

                return contacts[account];
            }			
		}

        internal Contact GetContact(string account, string name)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (contacts.ContainsKey(account))
                return contacts[account];
            else
            {
                Contact tmpContact = Factory.CreateContact();

                tmpContact.SetMail(account);
                tmpContact.SetName(name);

                contacts.Add(account, tmpContact);

                return contacts[account];
            }
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
                account = account.ToLower(CultureInfo.InvariantCulture);
				if(contacts.ContainsKey(account))
					return contacts[account];
				
				return null;
			}
			set
			{
                account = account.ToLower(CultureInfo.InvariantCulture);
				contacts[account] = value;
			}
		}
		
		public bool HasContact (string account)
		{
            account = account.ToLower(CultureInfo.InvariantCulture);
			return contacts.ContainsKey (account);
		}

		public void CopyTo(Contact[] array, int index)
		{
			contacts.Values.CopyTo (array, index);
		}
		
		internal void Clear()
		{
			contacts.Clear ();
		}

        internal void Remove(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (HasContact(account))
            {
                contacts.Remove(account);
            }
        }
	}
}