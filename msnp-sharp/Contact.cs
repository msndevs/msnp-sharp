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
	[Serializable()]
	public class ContactStatusChangeEventArgs : EventArgs
	{
		Contact contact;
		PresenceStatus oldStatus;

		public Contact Contact
		{
			get { 
				return contact; 
			}
			set { 
				contact = value;
			}
		}

		public PresenceStatus OldStatus
		{
			get { 
				return oldStatus; 
			}
			set { 
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
			get { 
				return contact ; 
			}
			set { 
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
			get { 
				return oldStatus; 
			}
			set { 
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
		string guid;
		string mail;
		string name;
		string homePhone;
		string workPhone;
		string mobilePhone;
		bool hasBlog;
		
		[NonSerialized]
		IMessageHandler nsMessageHandler;

		ArrayList contactGroups = new ArrayList ();
		MSNLists lists;	
		PresenceStatus status = PresenceStatus.Offline;	

		DisplayImage displayImage = null;
		
		PersonalMessage personalMessage;

		Hashtable emoticons = null;

		ClientCapacities clientCapacities = 0;

		bool mobileDevice = false;
		bool mobileAccess = false;

		object clientData;

		protected Contact()
		{
		}
		
		public bool	MobileDevice
		{
			get { 
				return mobileDevice; 
			}
		}

		public bool	MobileAccess
		{
			get { 
				return mobileAccess; 
			}
		}

		public string HomePhone 
		{ 
			get { 
				return homePhone; 
			} 
		}
		
		public string WorkPhone 
		{ 
			get { 
				return workPhone; 
			} 
		}
		
		public string MobilePhone 
		{ 
			get { 
				return mobilePhone; 
			} 
		}

		public ClientCapacities ClientCapacities
		{
			get { 
				return clientCapacities; 
			}
			set { 
				clientCapacities = value;
			}
		}

		public string Mail
		{
			get { 
				return mail;  
			}
		}

		public string Name
		{
			get { 
				return name;  
			}
		}
		
		public PersonalMessage PersonalMessage
		{
			get { 
				return personalMessage; 
			}
		}
		
		public string Guid
		{
			get { 
				return guid; 
			}
		}
		
		public PresenceStatus Status
		{
			get { 
				return status; 
			}
		}

		public bool Online
		{
			get { 
				return status != PresenceStatus.Offline; 
			}
		}

		public bool HasBlog
		{
			get { 
				return hasBlog; 
			}
		}

		public NSMessageHandler NSMessageHandler
		{
			get { 
				return (NSMessageHandler)nsMessageHandler; 
			}
			set { 
				nsMessageHandler = value;
			}
		}

		public DisplayImage DisplayImage
		{
			get { 
				return displayImage; 
			}
		}

		public Hashtable Emoticons
		{
			get 
			{ 
				if(emoticons == null)
					emoticons = new Hashtable();

				return emoticons; 
			}
		}

		public object ClientData
		{
			get { 
				return clientData; 
			}
			set { 
				clientData = value;
			}
		}


		#region Public events

		public delegate void ContactChangedEventHandler(object sender, 
		                                                EventArgs e);

		public delegate void StatusChangedEventHandler(object sender, 
		                                               StatusChangeEventArgs e);

		public event ContactChangedEventHandler ScreenNameChanged;

		public event ContactChangedEventHandler PersonalMessageChanged;
		
		public event ContactGroupChangedEventHandler ContactGroupAdded;

		public event ContactGroupChangedEventHandler ContactGroupRemoved;

		public event ContactChangedEventHandler ContactBlocked;
		
		public event ContactChangedEventHandler ContactUnBlocked;

		public event ContactChangedEventHandler ContactOnline;

		public event StatusChangedEventHandler ContactOffline;

		public event StatusChangedEventHandler StatusChanged;

		#endregion

		#region Internal setters
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

		internal void SetLists(MSNLists lists)
		{
			this.lists = lists;
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

		internal void AddContactToGroup(ContactGroup group)
		{
			if(contactGroups.Contains(group))
		    	return;

			contactGroups.Add(group);

			if(ContactGroupAdded != null)
				ContactGroupAdded (this, new ContactGroupEventArgs (group));
		}

		internal void RemoveContactFromGroup(ContactGroup group)
		{
			if(contactGroups.Contains(group))
			{
				contactGroups.Remove(group);

				if(ContactGroupRemoved != null)
					ContactGroupRemoved (this, new ContactGroupEventArgs (group));
			}
		}
		
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

		internal void SetMail(string account)
		{
			mail = account;
		}

		internal void SetUserDisplay(DisplayImage userDisplay)
		{
			displayImage = userDisplay;
		}

		#endregion

		#region Public setters

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

		public void UpdateScreenName()
		{
			if(NSMessageHandler != null)
			{
				NSMessageHandler.RequestScreenName(this);
			}
			else
				throw new MSNPSharpException("No valid message handler object");
		}

		public bool Blocked
		{
			get { 
				return (lists & MSNLists.BlockedList) > 0; 
			}
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

		public bool OnBlockedList
		{
			get { 
				return ((lists & MSNLists.BlockedList) == MSNLists.BlockedList); 
			}			
		}

		public bool OnForwardList
		{
			get { 
				return ((lists & MSNLists.ForwardList) == MSNLists.ForwardList); 
			}
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

		public bool OnAllowedList
		{
			get { 
				return ((lists & MSNLists.AllowedList) == MSNLists.AllowedList); 
			}
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

		public bool OnReverseList
		{
			get { 
				return ((lists & MSNLists.ReverseList) == MSNLists.ReverseList); 
			}
		}
		
		
		public bool OnPendingList
		{
			get { 
				return ((lists & MSNLists.PendingList) == MSNLists.PendingList); 
			}
		}


		public void RemoveFromList()
		{
			if(NSMessageHandler != null)
				NSMessageHandler.RemoveContact(this);
		}		

		#endregion

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
