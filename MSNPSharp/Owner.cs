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
	public class Owner : Contact
	{				
		PrivacyMode	privacy = 0;
		NotifyPrivacy notifyPrivacy = 0;
		bool passportVerified = false;

		public delegate void ProfileReceivedEventHandler(object sender, EventArgs e);
		public event ProfileReceivedEventHandler ProfileReceived;
		
		private void CreateDefaultDisplayImage()
		{			
			if(DisplayImage != null)
				return;

			System.Drawing.Image pngImage = Properties.Resources.owner;
			DisplayImage image = new DisplayImage(Mail);
			image.Image = pngImage;
			DisplayImage = image;
		}

		internal void SetPassportVerified(bool verified)
		{
			passportVerified = verified;
			CreateDefaultDisplayImage();
		}

		internal void SetPrivacy(PrivacyMode mode)
		{
			privacy = mode;
		}

		internal void SetNotifyPrivacy(NotifyPrivacy mode)
		{
			notifyPrivacy = mode;
		}

		public new DisplayImage DisplayImage
		{
			get { 
				return base.DisplayImage; 
			}
			set 
			{				
				if(base.DisplayImage != null)
					MSNObjectCatalog.GetInstance().Remove(base.DisplayImage);
                
				SetUserDisplay(value);
				
				if(value == null) 
					return;

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
                if (value != null)
                    base.SetPersonalMessage(value);
			}
		}

        /*public void BroadcastDisplayImage()
        {
			if(NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline && Status != PresenceStatus.Unknown)
			{
				// resend the user status so other client can see the new msn object
				NSMessageHandler.SetPresenceStatus(Status);
			}
		}*/	 

		public new string MobilePhone
		{
			get { 
				return base.MobilePhone; 
			}
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberMobile(value);
				}
			}
		}

		public new string WorkPhone
		{
			get { 
				return base.WorkPhone; 
			}
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberWork(value);
				}
			}
		}

		public new string HomePhone
		{
			get { 
				return base.HomePhone; 
			}
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPhoneNumberHome(value);
				}
			}
		}

		public new bool MobileDevice
		{
			get { 
				return base.MobileDevice; 
			}
			// it seems the server does not like it when we want to set mobile device ourselves!
			/*set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetMobileDevice(value);
				}
			}*/
		}

		public new bool MobileAccess
		{
			get { 
				return base.MobileAccess; 
			}
			set 
			{
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetMobileAccess(value);
				}
			}
		}

		public PrivacyMode Privacy
		{
			get { 
				return privacy; 
			}
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPrivacyMode(value);
				}
			}
		}

		public bool PassportVerified
		{
			get 
			{
				return passportVerified; 
			}
		}

		public NotifyPrivacy NotifyPrivacy
		{
			get { 
				return notifyPrivacy; 
			}
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetNotifyPrivacyMode(value);
				}
			}
		}

		new public PresenceStatus Status
		{
			get { 
				return base.Status; 
			}
			set 
			{ 
				if(NSMessageHandler != null)
				{
					NSMessageHandler.SetPresenceStatus(value);
				}			
			}
		}

		new public string Name
		{
			get { 
				return base.Name; 
			}
			set 
			{
				if(NSMessageHandler != null)
				{					
					NSMessageHandler.SetScreenName(value);
				}
			}
		}

		#region Profile datafields
		bool validProfile = false;
		string loginTime;
		bool emailEnabled = false;
		string memberIdHigh;
		string memberIdLowd;
		string preferredLanguage;
		string preferredMail;
		string country;
		string postalCode;
		string gender;
		string kid;
		string age;
		string birthday;
		string wallet;
		string sid;
		string kV;
		string mSPAuth;
		IPAddress clientIP;
		int	clientPort;
		
		public bool ValidProfile
		{
			get { 
				return validProfile; 
			}
		}
		
		public string LoginTime
		{
			get { 
				return loginTime; 
			}
			set { 
				loginTime = value;
			}
		}

		public bool	EmailEnabled
		{
			get { 
				return emailEnabled; 
			}
			set { 
				emailEnabled  = value;
			}
		}

		public string MemberIdHigh
		{
			get { 
				return memberIdHigh; 
			}
			set { 
				memberIdHigh = value;
			}
		}

		public string MemberIdLowd
		{
			get { 
				return memberIdLowd; 
			}
			set { 
				memberIdLowd = value;
			}
		}

		public string PreferredLanguage
		{
			get { 
				return preferredLanguage; 
			}
			set { 
				preferredLanguage = value;
			}
		}

		public string PreferredMail
		{
			get { 
				return preferredMail; 
			}
			set { 
				preferredMail = value;
			}
		}

		public string Country
		{
			get { 
				return country; 
			}
			set { 
				country = value;
			}
		}
				
		public string PostalCode
		{
			get { 
				return postalCode; 
			}
			set { 
				postalCode = value;
			}
		}

		public string Gender
		{
			get { 
				return gender; 
			}
			set { 
				gender = value;
			}
		}
		
		public string Kid
		{
			get { 
				return kid; 
			}
			set { 
				kid = value;
			}
		}

		public string Age
		{
			get { 
				return age; 
			}
			set { 
				age = value;
			}
		}

		public string Birthday
		{
			get { 
				return birthday; 
			}
			set { 
				birthday = value;
			}
		}

				
		public string Wallet
		{
			get { 
				return wallet; 
			}
			set { 
				wallet = value;
			}
		}

		public string Sid
		{
			get { 
				return sid; 
			}
			set { 
				sid = value;
			}
		}

		public string KV
		{
			get { 
				return kV; 
			}
			set { 
				kV = value;
			}
		}

		public string MSPAuth
		{
			get { 
				return mSPAuth;
			}
			set { 
				mSPAuth = value;
			}
		}
																								
		public IPAddress ClientIP
		{
			get { 
				return clientIP; 
			}
			set { 
				clientIP = value;
			}
		}

		public int ClientPort
		{
			get { 
				return clientPort; 
			}
			set { 
				clientPort = value;
			}
		}		

		#endregion

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