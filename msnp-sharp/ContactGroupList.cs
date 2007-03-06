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

namespace MSNPSharp
{
	/// <summary>
	/// Summary description for ContactGroupList.
	/// </summary>
	[Serializable()]
	public class ContactGroupList : IEnumerable
	{
		/// <summary>
		/// </summary>
		private ArrayList list = new ArrayList();

		internal void AddGroup(ContactGroup group)
		{
			list.Add(group);
		}
		internal void RemoveGroup(ContactGroup group)
		{
			list.Remove(group);
		}


		/// <summary>
		/// The nameserver handler that performs the adding/removing actions.
		/// </summary>
		[NonSerialized]
		protected  NSMessageHandler	nsMessageHandler = null;
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="handler"></param>
		internal ContactGroupList(NSMessageHandler handler)
		{
			nsMessageHandler = handler;
		}


		
		/// <summary>
		/// Add a new contactgroup.
		/// </summary>
		/// <remarks>
		/// This method delegates the action to the nameserver handler.
		/// </remarks>
		/// <param name="name">The name of the new contactgroup</param>
		protected virtual void Add(string name)
		{
			if(nsMessageHandler == null)
				throw new MSNPSharpException("No nameserver handler defined");
            nsMessageHandler.AddContactGroup(name);
		}

		/// <summary>
		/// Removes an existing contactgroup.
		/// </summary>
		/// <remarks>
		/// This method delegates the action to the nameserver handler.
		/// </remarks>
		protected virtual void Remove(ContactGroup group)
		{
			if(nsMessageHandler == null)
				throw new MSNPSharpException("No nameserver handler defined");
			if(this[group.Guid] != null)
				nsMessageHandler.RemoveContactGroup(group);
			else
				throw new MSNPSharpException("Contactgroup not defined in this list");
		}


		/// <summary>
		/// Returns the contactgroup specified by the Name.
		/// </summary>		
		public ContactGroup GetByName (string name)
		{
			foreach(ContactGroup group in list)
			{
				if(group.Name == name)
					return group;
			}
			return null;		}

		/// <summary>
		/// Returns the contactgroup specified by the name.
		/// </summary>
		public ContactGroup this[string guid]
		{
			get 
			{
				foreach(ContactGroup group in list)
				{
					if(group.Guid == guid)
						return group;
				}
				return null;
			}
		}
		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator to ContactGroup objects in the list.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{			
			return list.GetEnumerator();
		}

		#endregion
	}
}
