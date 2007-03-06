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
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// A collection of all contacts.	
	/// </summary>
	/// <remarks>
	/// Contactlist is a strongly-typed collection with extended functionality to support easy enumerating.
	/// By providing multiple enumerators one can walk through the forward list, blocked list, etc.
	/// All contacts are still stored only once in the internal collection.
	/// 
	/// Example of how to use this class:
	/// <code>
	/// // loop through all contacts in our forward list
	/// foreach(Contact contact in Contacts.Forward)
	/// {
	///		// do something with contact
	/// }
	/// // loop through all contacts available
	/// foreach(Contact contact in Contacts.All)
	/// {
	///		// do something with contact
	/// }
	/// </code>
	/// </remarks>
	[Serializable()]
	public class ContactList
	{		
		/// <summary>
		/// The container that holds the contacts
		/// </summary>
		private Hashtable contacts = new Hashtable(8);

		/// <summary>
		/// The listenumerator is the base class for the more specific other iterators in ContactList.
		/// </summary>
		public class ListEnumerator : IEnumerator
		{
			/// <summary>
			/// </summary>
			private IDictionaryEnumerator baseEnum;

			/// <summary>
			/// The enumerator will use a standard IDictionaryEnumerator to do the work
			/// </summary>
			protected IDictionaryEnumerator BaseEnum
			{
				get { return baseEnum; }
				set { baseEnum = value;}
			}

			/// <summary>
			/// </summary>
			/// <param name="listEnum"></param>
			public ListEnumerator(IDictionaryEnumerator listEnum)
			{
				baseEnum = listEnum;
			}

			/// <summary> 
			/// </summary>
			/// <returns></returns>
			public virtual bool MoveNext()
			{					
				return baseEnum.MoveNext();
			}

			/// <summary>
			/// </summary>
			Object IEnumerator.Current 
			{
				get { return baseEnum.Value; }
			}

			/// <summary>
			/// The current contact the enumerator points to.
			/// </summary>
			public Contact Current 
			{
				get { return (Contact)baseEnum.Value; }
			}

			/// <summary>
			/// </summary>
			public void Reset()
			{
				baseEnum.Reset();
			}

			/// <summary>
			/// </summary>
			/// <returns></returns>
			public IEnumerator GetEnumerator()
			{			
				return this;
			}
		}


		/// <summary>
		/// Enumerating through this list will only process contacts which are on your forward list:
		/// these are the contacts on YOUR contactlist.
		/// </summary>
		public class ForwardListEnumerator : ContactList.ListEnumerator
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="listEnum"></param>
			public ForwardListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			/// <summary>
			/// Used by the foreach language structure
			/// </summary>
			/// <returns>True when there are still items left</returns>
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

		/// <summary>
		/// Enumerating through this list will only process contacts which are on your pending list
		/// </summary>
		public class PendingListEnumerator : ContactList.ListEnumerator
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="listEnum"></param>
			public PendingListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			/// <summary>
			/// Used by the foreach language structure
			/// </summary>
			/// <returns>True when there are still items left</returns>
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


		/// <summary>
		/// Enumerating through this list will only process contacts which are on your reverse list:
		/// these are contacts who have YOU in their contactlist.
		/// </summary>
		public class ReverseListEnumerator : ContactList.ListEnumerator
		{
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="listEnum"></param>
			public ReverseListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			/// <summary>
			/// </summary>
			/// <returns></returns>
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

		/// <summary>
		/// Enumerating through this list will only process contacts which are on your blocked list:
		/// these are contacts who YOU have blocked and are not able to see your status.
		/// </summary>
		public class BlockedListEnumerator : ContactList.ListEnumerator
		{
			/// <summary>
			/// </summary>
			/// <param name="listEnum"></param>
			public BlockedListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			/// <summary>
			/// </summary>
			/// <returns></returns>
			public override bool MoveNext()
			{					
				while(BaseEnum.MoveNext())
				{
					// filter on the forward boolean						
					if((((Contact)BaseEnum.Value).Blocked))
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Enumerating through this list will only process contacts which are on your allowed list:
		/// these are contacts who are allowed to see your status.
		/// </summary>
		public class AllowedListEnumerator : ContactList.ListEnumerator
		{
			/// <summary>
			/// </summary>
			/// <param name="listEnum"></param>
			public AllowedListEnumerator(IDictionaryEnumerator listEnum)
				: base(listEnum)
			{
			}

			/// <summary>
			/// </summary>
			/// <returns></returns>
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


		/// <summary>
		/// A collection of all contacts on the forward list
		/// </summary>
		public ContactList.ForwardListEnumerator Forward
		{
			get { return new ContactList.ForwardListEnumerator(contacts.GetEnumerator()); }
		}
		
		/// <summary>
		/// A collection of all contacts on the pending list
		/// </summary>
		public ContactList.PendingListEnumerator Pending
		{
			get { return new ContactList.PendingListEnumerator(contacts.GetEnumerator()); }
		}

		/// <summary>
		/// A collection of all contacts on the reverse list
		/// </summary>
		public ContactList.ReverseListEnumerator Reverse
		{
			get { return new ContactList.ReverseListEnumerator(contacts.GetEnumerator()); }
		}		

		/// <summary>
		/// A collection of all contacts on the blocked list
		/// </summary>
		public ContactList.BlockedListEnumerator BlockedList
		{
			get { return new ContactList.BlockedListEnumerator(contacts.GetEnumerator()); }
		}

		/// <summary>
		/// A collection of all contacts on the allowed list
		/// </summary>
		public ContactList.AllowedListEnumerator Allowed
		{
			get { return new ContactList.AllowedListEnumerator(contacts.GetEnumerator()); }
		}

		/// <summary>
		/// A collection of all contacts in your lists.
		/// </summary>
		public ContactList.ListEnumerator All
		{
			get { return new ContactList.ListEnumerator(contacts.GetEnumerator()); }
		}

		/// <summary>
		/// Retrieves the Contact object with the specified account. The account is typically an e-mail adres, eg name@hotmail.com.
		/// When no contact is found a new contact is created with the specified account.
		/// </summary>
		/// <param name="account">The account, or e-mail address, of the contact to search for</param>
		/// <returns>The associated Contact object.</returns>
		public Contact GetContact(string account)
		{			
			if(contacts.ContainsKey(account))
				return (Contact)contacts[account];
			else
			{
				Contact	tmpContact = Factory.CreateContact();
				tmpContact.SetMail(account);
				tmpContact.SetName(account);				
				//tmpContact.SetContactGroup(String.Empty);
				contacts.Add(account, tmpContact);
				return (Contact)contacts[account];
			}			
		}
		
		public Contact GetContactByGuid (string guid)
		{
			foreach (Contact c in contacts.Values)
			{
				if (c.Guid == guid)
					return c;
			}
			
			return null;
		}
		
		/// <summary>
		/// Retrieves the Contact object with the specified account.
		/// </summary>
		/// <remarks>Uses <see cref="GetContact"/>. The account is usually a principal's e-mail adress.</remarks>
		public Contact this[string key]
		{
			get
			{
				return GetContact (key);
			}
			set
			{
				contacts[key] = value;
			}
		}
		
		public bool HasContact (string account)
		{
			return (contacts[account] != null);
		}

		
		/// <summary>
		/// Copies all contacts to the specified array, starting at the index.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(Contact[] array, int index)
		{
			((Hashtable)contacts).CopyTo(array, index);
		}
	}
	
}
