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
using System.Net;
using System.Reflection;
using System.Collections;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// Used as event argument when a contact changes it's status.
	/// </summary>
	[Serializable()]
	public class ContactStatusChangeEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private Contact			contact;

		/// <summary>
		/// The affected contact.
		/// </summary>
		public Contact			Contact
		{
			get { return contact; }
			set { contact = value;}
		}

		/// <summary>
		/// </summary>
		private PresenceStatus	oldStatus;

		/// <summary>
		/// The status the contact had before the change.
		/// </summary>
		public PresenceStatus	OldStatus
		{
			get { return oldStatus; }
			set { oldStatus = value;}
		}

		
		/// <summary>
		/// Constructor, mostly used internal by the library.
		/// </summary>
		/// <param name="contact"></param>
		/// <param name="oldStatus"></param>
		public ContactStatusChangeEventArgs(Contact contact, PresenceStatus oldStatus)
		{
			Contact		= contact;
			OldStatus	= oldStatus;
		}
	}


	/// <summary>
	/// Used as event argument when a contact is affected.
	/// </summary>
	[Serializable()]
	public class ContactEventArgs : System.EventArgs
	{
		/// <summary>
		/// The affected contact
		/// </summary>
		public Contact Contact
		{
			get { return contact ; }
			set { contact = value; }
		}

		/// <summary>
		/// </summary>
		private Contact contact;
		

		/// <summary>
		/// Constructor, mostly used internal by the library.
		/// </summary>
		/// <param name="contact"></param>
		public ContactEventArgs(Contact contact)
		{
			Contact		= contact;
		}
	}


	/// <summary>
	/// Used as event argument when a contact changed status.
	/// </summary>
	/// <remarks>
	/// The difference between this class and <see cref="ContactStatusChangeEventArgs"/> is that this class is dispatched by the Contact class. And as such does not contain a Contact property, since that is the sender object.
	/// </remarks>
	[Serializable()]
	public class StatusChangeEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private PresenceStatus oldStatus;

		/// <summary>
		/// The old presence status
		/// </summary>
		public PresenceStatus OldStatus
		{
			get { return oldStatus; }
			set { oldStatus = value;}
		}

		/// <summary>
		/// Constructs a StatusChangeEventArgs object.
		/// </summary>
		/// <param name="oldStatus"></param>
		public StatusChangeEventArgs(PresenceStatus oldStatus)
		{
			OldStatus = oldStatus;
		}
	}


	/// <summary>
	/// Represents a single contact.
	/// </summary>
	/// <remarks>
	/// Of every contact in the contactlist there is only one single Contact instance in the library at any time. 
	/// By getting/setting properties you can easily block, remove or change contactgroup of a contact.
	/// It is possible that a Contact object exists, but is not available in the contact list. This is the case when
	/// a contact is in a switchboard session (invited by somebody else), who is not on the local owner's contact list.
	/// </remarks>
	[Serializable()]
	public class Contact
	{
		#region Private
		
		/// <summary>
		/// Constructor.
		/// </summary>
		protected Contact()
		{
		}

		private string guid;
		private string mail;
		private string name;
		private string homePhone;
		private string workPhone;
		private string mobilePhone;
		private bool hasBlog;

		
		[NonSerialized]
		private IMessageHandler nsMessageHandler;

		//private string			contactGroup = String.Empty;
		private ArrayList		contactGroups = new ArrayList ();
		private MSNLists		lists;	
		private PresenceStatus	status  = PresenceStatus.Offline;		

		private DisplayImage	displayImage = null;
		private PersonalMessage personalMessage;

		private	Hashtable		emoticons = null;

		private ClientCapacities	clientCapacities = 0;

		private bool			mobileDevice = false;
		private bool			mobileAccess = false;
		#endregion
		
		#region Public fields
		/// <summary>
		/// Indicates whether the contact has a mobile device enabled.
		/// </summary>
		public bool	MobileDevice
		{
			get { return mobileDevice; }
		}

		/// <summary>
		/// Indicates whether the contact allows contacts to send mobile messages.
		/// </summary>
		public bool	MobileAccess
		{
			get { return mobileAccess; }
		}


		/// <summary>
		/// Telephonenumber at home
		/// </summary>
		public string HomePhone { get { return homePhone; } }
		/// <summary>
		/// Telephonenumber at work
		/// </summary>
		public string WorkPhone { get { return workPhone; } }
		/// <summary>
		/// Mobile phonenumber
		/// </summary>
		public string MobilePhone { get { return mobilePhone; } }

		
		/// <summary>
		/// A list of all capacities of the remote client.
		/// </summary>
		/// <remarks>
		/// Use bitwise AND ( &amp; ) to extract specific capacities. For example:
		/// <code>
		/// if(contact.ClientCapacities &amp; ClientCapacities.Mobile)
		/// { 
		///		// client is a mobile device 
		/// }
		/// </code>
		/// </remarks>
		public ClientCapacities ClientCapacities
		{
			get { return clientCapacities; }
			set { clientCapacities = value;}
		}

		/// <summary>
		/// The contact's unique e-mail adress. Used to identify a Microsoft Passport account
		/// </summary>
		public string Mail
		{
			get { return mail;  }
		}

		/// <summary>
		/// The username (screenname) of this contact
		/// </summary>
		public string Name
		{
			get { return name;  }
		}
		
		public PersonalMessage PersonalMessage
		{
			get { return personalMessage; }
		}
		
		public string Guid
		{
			get { return guid; }
		}
		
		/// <summary>
		/// Retrieve the current status of this contact. It defaults to offline
		/// </summary>
		public PresenceStatus Status
		{
			get { return status; }
		}

		/// <summary>
		/// Indicates whether the specified is online. It will return true for all presence states (online, away, busy, etc) except PresenceStatus.Offline
		/// </summary>
		public bool Online
		{
			get { return status != PresenceStatus.Offline; }
		}

		/// <summary>
		/// Indicates whether the contact has an updated blog
		/// </summary>
		public bool HasBlog
		{
			get { return hasBlog; }
		}

		/// <summary>
		/// The notification message handler which controls this contact object
		/// </summary>
		public	NSMessageHandler NSMessageHandler
		{
			get { return (NSMessageHandler)nsMessageHandler; }
			set { nsMessageHandler = (NSMessageHandler)value;}
		}

		/// <summary>
		/// The user display image of the contact. Null if not present
		/// </summary>
		public DisplayImage DisplayImage
		{
			get { return displayImage; }
		}

		/// <summary>
		/// A collection of all emoticons used by this contact
		/// </summary>
		public Hashtable Emoticons
		{
			get 
			{ 
				if(emoticons == null)
					emoticons = new Hashtable();

				return emoticons; 
			}
		}

		
		#endregion					
		
		/// <summary>
		/// </summary>
		private object clientData;


		/// <summary>
		/// The client programmer can specify custom data related to this contact in this property
		/// </summary>
		public object ClientData
		{
			get { return clientData; }
			set { clientData = value;}
		}

		/// <summary>
		/// </summary>
		internal bool inList = false;

		#region Public events

		/// <summary>
		/// Used in events where a property of the contact changed
		/// </summary>
		public delegate void ContactChangedEventHandler(object sender, EventArgs e);


		/// <summary>
		/// Used in events where the presence state of a contact changed
		/// </summary>
		public delegate void StatusChangedEventHandler(object sender, StatusChangeEventArgs e);

		
		/// <summary>
		/// Called when the username of this contact has changed
		/// </summary>
		public event ContactChangedEventHandler		ScreenNameChanged;

		/// <summary>
		/// Called when the display message of this contact has changed
		/// </summary>
		public event ContactChangedEventHandler		PersonalMessageChanged;
		
		/// <summary>
		/// Called when this user has been moved to another contactgroup
		/// </summary>
		//public event ContactChangedEventHandler		ContactGroupChanged;


		public event ContactGroupChangedEventHandler ContactGroupAdded;


		public event ContactGroupChangedEventHandler ContactGroupRemoved;

		/// <summary>
		/// Called when this user has been blocked
		/// </summary>
		public event ContactChangedEventHandler		ContactBlocked;
		
		/// <summary>
		/// Called when this user has been unblocked
		/// </summary>
		public event ContactChangedEventHandler		ContactUnBlocked;		

		/// <summary>
		/// Called when this contact goes online
		/// </summary>
		public event ContactChangedEventHandler		ContactOnline;

		/// <summary>		
		/// Called when this contact goes offline
		/// </summary>
		public event StatusChangedEventHandler			ContactOffline;

		/// <summary>
		/// Called when the user changed from state
		/// </summary>
		public event StatusChangedEventHandler			StatusChanged;

		#endregion

		#region Internal setters
		/// <summary>
		/// Used internal by the NS message handler. Raises the ScreenNameChanged event
		/// </summary>		
		/// <param name="newName">URL-encoded name</param>
		internal void SetName(string newName)
		{
			if(name != newName)
			{			
				name = System.Web.HttpUtility.UrlDecode(newName, System.Text.Encoding.UTF8);				
				if(ScreenNameChanged != null)
				{
					// notify the user we changed our name
					ScreenNameChanged(this, new EventArgs());
				}
			}
		}
		
		internal void SetPersonalMessage(PersonalMessage newpmessage)
		{
			if(personalMessage != newpmessage)
			{
				personalMessage = newpmessage;
				if(PersonalMessageChanged != null)
				{
					// notify the user we changed our display message
					PersonalMessageChanged(this, new EventArgs());
				}
			}
		}
		
		internal void SetHasBlog (bool hasblog)
		{
			this.hasBlog = hasblog;
		}
		
		internal void SetGuid (string guid)
		{
			this.guid = guid;
		}

		/// <summary>
		/// Sets the list this contact is in. Received after a LST command
		/// </summary>
		/// <param name="lists"></param>
		internal void SetLists(MSNLists lists)
		{
			this.lists = lists;
		}

	
		/// <summary>
		/// Sets the contact's home phone
		/// </summary>		
		internal void SetMobileDevice(bool enabled)
		{
			mobileDevice = enabled;
		}

		/// <summary>
		/// Sets the contact's home phone
		/// </summary>
		internal void SetMobileAccess(bool enabled)
		{
			mobileAccess = enabled;
		}

		/// <summary>
		/// Sets the contact's home phone
		/// </summary>
		/// <param name="number"></param>
		internal void SetHomePhone(string number)
		{
			homePhone = number;
		}

		/// <summary>
		/// Sets the contact's home phone
		/// </summary>
		/// <param name="number"></param>
		internal void SetMobilePhone(string number)
		{
			mobilePhone = number;
		}

		/// <summary>
		/// Sets the contact's work phone
		/// </summary>
		/// <param name="number"></param>
		internal void SetWorkPhone(string number)
		{
			workPhone = number;
		}
	
		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="StatusChanged"/>, <see cref="ContactOnline"/> or <see cref="ContactOffline"/>. Depending on the (new) presence state.
		/// </summary>		
		/// <param name="newStatus"></param>
		internal void SetStatus(PresenceStatus newStatus)
		{
			if(status != newStatus)
			{
				PresenceStatus oldStatus = this.status;
				status = newStatus;

				// raise an event									
				if(StatusChanged != null)
					StatusChanged(this, new StatusChangeEventArgs(oldStatus));

				// raise the online/offline events
				if(oldStatus == PresenceStatus.Offline && ContactOnline != null)
					ContactOnline(this, new EventArgs());
				if(newStatus == PresenceStatus.Offline && ContactOffline != null)
					ContactOffline(this, new StatusChangeEventArgs(oldStatus));
			}
		}



		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="ContactGroupChanged"/> event.
		/// </summary>
		/// <param name="newGroup"></param>
		//[Deprecated]
		/*internal void SetContactGroup(string guid)
		{
			if (contactGroups.Contains (guid))
				return;
				
			if(NSMessageHandler == null)
				return;
				
			ContactGroup group;
			group = (ContactGroup)((NSMessageHandler)NSMessageHandler).ContactGroups[guid];
			
			if (group == null)
				return;
			
			contactGroups.Add(group);
			
			if(ContactGroupChanged != null)
				ContactGroupChanged(this, new EventArgs());
		}*/

		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="ContactGroupAdded"/> event.
		/// </summary>
		/// <param name="group"></param>
		internal void AddContactToGroup(ContactGroup group)
		{
			if(contactGroups.Contains(group))
		    	return;

			contactGroups.Add(group);

			if(ContactGroupAdded != null)
				ContactGroupAdded (this, new ContactGroupEventArgs (group));
		}


		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="RemoveContactFromGroup"/> event.
		/// </summary>
		/// <param name="group"></param>
		internal void RemoveContactFromGroup(ContactGroup group)
		{
			if(contactGroups.Contains(group))
			{
				contactGroups.Remove(group);

				if(ContactGroupRemoved != null)
					ContactGroupRemoved (this, new ContactGroupEventArgs (group));
			}
		}
		
		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="ContactBlocked"/> event if the list is the Blocked list and the contact wasn't on the blocked list before.
		/// </summary>
		/// <param name="list"></param>
		internal void AddToList(MSNLists list)
		{
			if(list == MSNLists.BlockedList && !Blocked)
			{
				lists |= MSNLists.BlockedList;
				if(ContactBlocked != null)
					ContactBlocked(this, new EventArgs());
				
			}
			else
			{
				lists |= list;
			}
		}

		/// <summary>
		/// Used internal by the NS message handler. It will raise the <see cref="ContactUnBlocked"/> event if the list is the Blocked list and the contact was on the blocked list before.
		/// </summary>
		/// <param name="list"></param>
		internal void RemoveFromList(MSNLists list)
		{
			if(list == MSNLists.BlockedList && Blocked)
			{
				lists ^= MSNLists.BlockedList;
				if(ContactUnBlocked != null)
					ContactUnBlocked(this, new EventArgs());
			}
			else
			{				
				lists ^= list;

				// set this contact to offline when it is neither on the allow list or on the forward list
				if(OnForwardList == false && OnAllowedList == false)
				{
					status = PresenceStatus.Offline;
					//also clear the groups, becase msn loose them when removed from the two lists
					contactGroups.Clear ();	
				}
			}
		}

		/// <summary>
		/// Used internal by the ContactList. This will set the contact's account, or mail adress, when it is created.
		/// </summary>
		internal void SetMail(string account)
		{
			mail = account;
		}

		/// <summary>
		/// Used internal. This will set the msn object context for the contact's user display.
		/// </summary>
		internal void SetUserDisplay(DisplayImage userDisplay)
		{
			displayImage = userDisplay;
		}

		#endregion

		#region Public setters

		/// <summary>
		/// The contactgroup this contact belongs to. You can change this by setting a new ContactGroup object.
		/// </summary>
		//[Deprecated]
		/*public ContactGroup ContactGroup
		{
			get 
			{ 
				return (contactGroups.Count > 0) ? (ContactGroup) contactGroups[0] : null;
			}
			set
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.ChangeGroup(this, value);
				}
			}
		}*/
		
		public ArrayList ContactGroups
		{
			get {
				return contactGroups;
			}
		}
		
		public bool HasGroup (ContactGroup group)
		{
			return contactGroups.Contains (group);
		}

		/// <summary>
		/// Request the server to send us the current screenname for this contact
		/// After receiving the screenname it will raise the ScreenNameChanged event
		/// </summary>
		public void UpdateScreenName()
		{
			if(NSMessageHandler != null)
			{
				NSMessageHandler.RequestScreenName(this);
			}
			else
				throw new MSNPSharpException("No valid message handler object");
		}

		/// <summary>
		/// Get or set whether this person is blocked. When someone is 'blocked' this means he or she is on your blocked list.
		/// </summary>
		public bool Blocked
		{
			get { return (lists & MSNLists.BlockedList) > 0; }
			set 
			{ 
				if(NSMessageHandler != null)
				{
					if(value == true)
						NSMessageHandler.BlockContact(this);
					else 
						NSMessageHandler.UnBlockContact(this);
				}
			}
		}


		/// <summary>
		/// Indicates whether this contact is on the blocked list. When a contact is on your blocked list it can't see your presence status or send messages to you.
		/// </summary>
		/// <remarks>
		/// You can use the <see cref="Blocked"/> property to block a contact.
		/// </remarks>
		public bool OnBlockedList
		{
			get { return ((lists & MSNLists.BlockedList) == MSNLists.BlockedList); }			
		}

		/// <summary>
		/// Indicates whether this contact is on the forward list
		/// </summary>
		public bool OnForwardList
		{
			get { return ((lists & MSNLists.ForwardList) == MSNLists.ForwardList); }
			set 
			{ 
				if(value != OnForwardList)
				{
					if(value)
						NSMessageHandler.AddContactToList(this, MSNLists.ForwardList);
					else
						NSMessageHandler.RemoveContactFromList(this, MSNLists.ForwardList);
				}					
			}
		}

		/// <summary>
		/// Indicates whether this contact is on the allowed list
		/// </summary>
		public bool OnAllowedList
		{
			get { return ((lists & MSNLists.AllowedList) == MSNLists.AllowedList); }
			set 
			{ 
				if(value != OnAllowedList)
				{
					if(value)
						NSMessageHandler.AddContactToList(this, MSNLists.AllowedList);
					else
						NSMessageHandler.RemoveContactFromList(this, MSNLists.AllowedList);
				}					
			}
		}

		/// <summary>
		/// Indicates whether this contact is on the reversed list. Obviously this field is read-only because the other client decides whether he accepts you or not.
		/// </summary>
		public bool OnReverseList
		{
			get { return ((lists & MSNLists.ReverseList) == MSNLists.ReverseList); }
		}
		
		
		public bool OnPendingList
		{
			get { return ((lists & MSNLists.PendingList) == MSNLists.PendingList); }
		}


		
		/// <summary>
		/// Removes the contact from both the forward and allowed list.
		/// </summary>
		public void RemoveFromList()
		{
			if(NSMessageHandler != null)
				NSMessageHandler.RemoveContact(this);
		}		

		#endregion

		/// <summary>
		/// Used to compare contacts. This returns the Mail-field hashcode.
		/// </summary>
		/// <returns></returns>
		override public int GetHashCode()
		{
			return mail.GetHashCode();
		}
	}

	

	/// <summary>
	/// Represents the owner of the contactlist.
	/// </summary>
	/// <remarks>
	/// </remarks>
	[Serializable()]
	public class Owner : Contact
	{				
		#region Private
		
		/// <summary>
		/// </summary>
		private PrivacyMode		privacy = 0;
		/// <summary>
		/// </summary>
		private NotifyPrivacy	notifyPrivacy = 0;

		/// <summary>
		/// </summary>
		private bool			passportVerified = false;

		/// <summary>
		/// Creates a default dotmsn image if there is no display image yet available.
		/// </summary>
		private void CreateDefaultDisplayImage()
		{			
			if(DisplayImage != null)
				return;

			//System.Resources.ResourceManager rm = new System.Resources.ResourceManager("ResourceClient.Images", System.Reflection.Assembly.GetExecutingAssembly());
			System.Drawing.Image pngImage = new System.Drawing.Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("owner.png"));
			DisplayImage image = new DisplayImage(Mail);
			image.Image = pngImage;
			DisplayImage = image;
		}
		#endregion

		#region Events

		/// <summary>
		/// </summary>
		public delegate void ProfileReceivedEventHandler(object sender, EventArgs e);

		/// <summary>
		/// Occurs when the MSN server has sended the profile on login
		/// </summary>
		public event ProfileReceivedEventHandler ProfileReceived;

		#endregion

		#region Private setters
		/// <summary>
		/// Sets the private passportVerified member.
		/// </summary>
		/// <param name="verified"></param>
		internal void SetPassportVerified(bool verified)
		{
			passportVerified = verified;
			CreateDefaultDisplayImage();
		}

		/// <summary>
		/// Sets the privacy mode of the contactlist owner.
		/// </summary>
		/// <param name="mode"></param>
		internal void SetPrivacy(PrivacyMode mode)
		{
			privacy = mode;
		}

		/// <summary>
		/// Sets the notification mode of the contactlist owner.
		/// </summary>
		/// <param name="mode"></param>
		internal void SetNotifyPrivacy(NotifyPrivacy mode)
		{
			notifyPrivacy = mode;
		}
		#endregion

		#region Public setters
		/// <summary>
		/// The display image of the owner visible to remote contacts.
		/// </summary>
		public new DisplayImage DisplayImage
		{
			get { return base.DisplayImage; }
			set 
			{				
				if(base.DisplayImage != null)
					MSNObjectCatalog.GetInstance().Remove(base.DisplayImage);
                
				SetUserDisplay(value);
				
				if(value == null) return;

                value.Creator = Mail;

				MSNObjectCatalog.GetInstance().Add(base.DisplayImage);

                //BroadcastDisplayImage();
			}	 
		}
		
		public new PersonalMessage PersonalMessage
		{
			get { return base.PersonalMessage; }
			set
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPersonalMessage (value);
				}
			}
		}

        /// <summary>
        /// Sends the display image details to other users, for example when the user display has changed.
        /// </summary>
        /// <remarks>This method actually resends the current presence state, and along with it the image display details.</remarks>
        // This will do nothing on MSNP11
        /*public void BroadcastDisplayImage()
        {
			if(NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline && Status != PresenceStatus.Unknown)
			{
				// resend the user status so other client can see the new msn object
				NSMessageHandler.SetPresenceStatus(Status);
			}
		}*/	 

		/// <summary>
		/// Get or set the mobile phone number.
		/// </summary>
		public new string MobilePhone
		{
			get { return base.MobilePhone; }
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberMobile(value);
				}
			}
		}

		/// <summary>
		/// Get or set the work phone number.
		/// </summary>
		public new string WorkPhone
		{
			get { return base.WorkPhone; }
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberWork(value);
				}
			}
		}

		/// <summary>
		/// Get or set the home phone number.
		/// </summary>
		public new string HomePhone
		{
			get { return base.HomePhone; }
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberHome(value);
				}
			}
		}

		/// <summary>
		/// Gets whether the owner has a mobile device.
		/// </summary>
		public new bool MobileDevice
		{
			get { return base.MobileDevice; }
			// it seems the server does not like it when we want to set mobile device ourselves!
			/*set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetMobileDevice(value);
				}
			}*/
		}

		/// <summary>
		/// Get or set whether the owner allows remote contacts to send messages to it's mobile device.
		/// </summary>
		public new bool MobileAccess
		{
			get { return base.MobileAccess; }
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetMobileAccess(value);
				}
			}
		}

		/// <summary>
		/// Get or set the privacy mode from the contactlist owner.
		/// This defines which users are allowed to send you messages
		/// </summary>
		public PrivacyMode Privacy
		{
			get { return privacy; }
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPrivacyMode(value);
				}
			}
		}

		/// <summary>
		/// Gets whether the current account is verified by the passport service
		/// </summary>
		public bool PassportVerified
		{
			get 
			{
				return passportVerified; 
			}
		}


		/// <summary>
		/// Get or set the notify privacy from the contactlist owner.
		/// This defines how MSN notifies us when someone adds us.
		/// </summary>
		public NotifyPrivacy NotifyPrivacy
		{
			get { return notifyPrivacy; }
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetNotifyPrivacyMode(value);
				}
			}
		}


		/// <summary>
		/// Retrieve the current status of this contact. It defaults to offline.
		/// When you set the status a request for status change will be send to the server.
		/// Only after confirmation from the server the status is actually changed!
		/// </summary>
		new public PresenceStatus Status
		{
			get { return base.Status; }
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPresenceStatus(value);
				}			
			}
		}


		/// <summary>
		/// Change the contact list owner's screenname
		/// </summary>
		/// <remarks>
		/// In contrast to a remote contact, in the owner object you can not only request the screenname but also set it. After
		/// assigning a value to the Name property a request for the name change will be send to the server.
		/// Only after confirmation from the server the name is actually changed.
		/// </remarks>
		new public string Name
		{
			get { return base.Name; }
			set 
			{
				if(NSMessageHandler != null)
				{					
					NSMessageHandler.SetScreenName(value);
				}
			}
		}

		#endregion		

		#region Profile datafields

		/// <summary>
		/// </summary>
		private bool validProfile = false;

		/// <summary>
		/// Indicates if the profile has been received from the messenger server. If this is false all profile fields are not valid.
		/// </summary>
		public bool ValidProfile
		{
			get { return validProfile; }
		}
		
		/// <summary>
		/// (Profile) Unix time you logged in - that is, in seconds since midnight UTC on January 1st, 1970.
		/// </summary>
		public string LoginTime
		{
			get { return loginTime; }
			set { loginTime = value;}
		}

		/// <summary>
		/// </summary>
		private string loginTime;

		/// <summary>
		/// (Profile) Whether or not the user's account has email notification (currently just activated Hotmail and MSN.com accounts) - 1 or 0 
		/// </summary>
		public bool	  EmailEnabled
		{
			get { return emailEnabled ; }
			set { emailEnabled  = value;}
		}

		private bool	  emailEnabled = false;

		/// <summary>
		/// (Profile) Unspecified
		/// </summary>
		public string MemberIdHigh
		{
			get { return memberIdHigh; }
			set { memberIdHigh = value;}
		}

		/// <summary>
		/// </summary>
		private string memberIdHigh;
				
		/// <summary>
		/// (Profile) Unspecified
		/// </summary>
		public string MemberIdLowd
		{
			get { return memberIdLowd; }
			set { memberIdLowd = value;}
		}

		/// <summary>
		/// </summary>
		private string memberIdLowd;

		/// <summary>
		/// (Profile) Preferred language number
		/// </summary>
		public string PreferredLanguage
		{
			get { return preferredLanguage; }
			set { preferredLanguage = value;}
		}

		/// <summary>
		/// </summary>
		private string preferredLanguage;
				
		/// <summary>
		/// (Profile) User's primary email address
		/// </summary>
		public string PreferredMail
		{
			get { return preferredMail; }
			set { preferredMail = value;}
		}

		/// <summary>
		/// </summary>
		private string preferredMail;
			
		/// <summary>
		/// (Profile) Two-digit country code
		/// </summary>
		public string Country
		{
			get { return country; }
			set { country = value;}
		}

		/// <summary>
		/// </summary>
		private string country;
				
		/// <summary>
		/// (Profile) User's post-code (or zip code, in the U.S.) 
		/// </summary>
		public string PostalCode
		{
			get { return postalCode; }
			set { postalCode = value;}
		}

		/// <summary>
		/// </summary>
		private string postalCode;

		/// <summary>
		///(Profile)  User's gender (m, f, or U if unspecified)
		/// </summary>
		public string Gender
		{
			get { return gender; }
			set { gender = value;}
		}

		/// <summary>
		/// </summary>
		private string gender;
				
		/// <summary>
		/// (Profile) Whether your account is a Kids Passport (0 or 1)
		/// </summary>
		public string Kid
		{
			get { return kid; }
			set { kid = value;}
		}

		/// <summary>
		/// </summary>
		private string kid;
				
		/// <summary>
		/// (Profile) Your given age in years
		/// </summary>
		public string Age
		{
			get { return age; }
			set { age = value;}
		}

		/// <summary>
		/// </summary>
		private string age;

		/// <summary>
		/// (Profile) Numerical birthday
		/// </summary>
		public string Birthday
		{
			get { return birthday; }
			set { birthday = value;}
		}

		/// <summary>
		/// </summary>
		private string birthday;
				
		/// <summary>
		/// (Profile) Uncertain: whether you have an MS Wallet (0 or 1) 
		/// </summary>
		public string Wallet
		{
			get { return wallet; }
			set { wallet = value;}
		}

		/// <summary>
		/// </summary>
		private string wallet;

		/// <summary>
		/// (Profile) A number needed for Hotmail login 
		/// </summary>
		public string Sid
		{
			get { return sid; }
			set { sid = value;}
		}

		/// <summary>
		/// </summary>
		private string sid;

		/// <summary>
		/// (Profile) Another number needed for Hotmail login
		/// </summary>
		public string KV
		{
			get { return kV; }
			set { kV = value;}
		}

		/// <summary>
		/// </summary>
		private string kV;

		/// <summary>
		/// (Profile) String used for Hotmail login
		/// </summary>
		public string MSPAuth
		{
			get { return mSPAuth; }
			set { mSPAuth = value;}
		}

		/// <summary>
		/// </summary>
		private string mSPAuth;
																								
		/// <summary>
		/// (Profile) The IP address the server thinks you're connecting from.
		/// </summary>
		public IPAddress ClientIP
		{
			get { return clientIP; }
			set { clientIP = value;}
		}

		/// <summary>
		/// </summary>
		private IPAddress clientIP;

		/// <summary>
		/// (Profile) The presumably port you're connecting from.
		/// </summary>
		public int		ClientPort
		{
			get { return clientPort; }
			set { clientPort = value;}
		}		

		/// <summary>
		/// </summary>
		private int		clientPort;
		#endregion

		/// <summary>
		/// Sets all profile values and sets ProfileValid to true and fires the ProfileReceived event.
		/// </summary>
		/// <param name="loginTime"></param>
		/// <param name="emailEnabled"></param>
		/// <param name="memberIdHigh"></param>
		/// <param name="memberIdLowd"></param>
		/// <param name="preferredLanguage"></param>
		/// <param name="preferredMail"></param>
		/// <param name="country"></param>
		/// <param name="postalCode"></param>
		/// <param name="gender"></param>
		/// <param name="kid"></param>
		/// <param name="age"></param>
		/// <param name="birthday"></param>
		/// <param name="wallet"></param>
		/// <param name="sid"></param>
		/// <param name="kv"></param>
		/// <param name="MSPAuth"></param>
		/// <param name="clientIP"></param>
		/// <param name="clientPort"></param>
		internal void UpdateProfile(
			string loginTime,		bool   emailEnabled,		string memberIdHigh,
			string memberIdLowd,	string preferredLanguage,	string preferredMail,
			string country,			string postalCode,			string gender,
			string kid,				string age,					string birthday,
			string wallet,			string sid,					string kv,	
			string MSPAuth,			IPAddress clientIP,			int	   clientPort)
		{
			LoginTime = loginTime;
			EmailEnabled = emailEnabled;		
			MemberIdHigh = memberIdHigh;
			MemberIdLowd = memberIdLowd;
			PreferredLanguage = preferredLanguage;
			PreferredMail = preferredMail;
			Country = country; 
			PostalCode = postalCode;
			Gender = gender;
			Kid = kid;
			Age = age;
			Birthday = birthday;
			Wallet = wallet;
			Sid = sid;
			KV = kv;
			this.MSPAuth = MSPAuth;
			ClientIP = clientIP;
			ClientPort = clientPort;

			this.validProfile = true;
			if(ProfileReceived != null)
				ProfileReceived(this, new EventArgs());			
		}
	}

}
