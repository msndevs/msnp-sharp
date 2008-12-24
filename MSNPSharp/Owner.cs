#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    [Serializable()]
    public class Owner : Contact
    {
        bool passportVerified;
        PrivacyMode privacy = PrivacyMode.Unknown;
        NotifyPrivacy notifyPrivacy = NotifyPrivacy.Unknown;
        RoamLiveProperty roamLiveProperty = RoamLiveProperty.Unspecified;

        public event EventHandler<EventArgs> ProfileReceived;

        private void CreateDefaultDisplayImage()
        {
            if (DisplayImage != null)
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

        internal void SetRoamLiveProperty(RoamLiveProperty mode)
        {
            roamLiveProperty = mode;
        }

        public new DisplayImage DisplayImage
        {
            get
            {
                return base.DisplayImage;
            }
            set
            {
                if (value == null)
                    return;

                if (base.DisplayImage != null)
                {
                    if (value == base.DisplayImage)
                    {
                        return;
                    }

                    MSNObjectCatalog.GetInstance().Remove(base.DisplayImage);
                }
                SetUserDisplay(value);

                value.Creator = Mail;

                MSNObjectCatalog.GetInstance().Add(base.DisplayImage);

                BroadcastDisplayImage();
            }
        }

        public new PersonalMessage PersonalMessage
        {
            get
            {
                return base.PersonalMessage;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPersonalMessage(value);
                }
                if (value != null)
                    base.SetPersonalMessage(value);
            }
        }

        public void BroadcastDisplayImage()
        {
            if (NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline && Status != PresenceStatus.Unknown)
            {
                // resend the user status so other client can see the new msn object
                string capacities = ((long)ClientCapacities).ToString();
                string context = String.Empty;

                if (DisplayImage != null)
                    context = DisplayImage.Context;

                NSMessageHandler.MessageProcessor.SendMessage(new NSMessage("CHG", new string[] { NSMessageHandler.ParseStatus(Status), capacities, context }));
            }
        }

        public new string MobilePhone
        {
            get
            {
                return base.MobilePhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberMobile(value);
                }
            }
        }

        public new string WorkPhone
        {
            get
            {
                return base.WorkPhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberWork(value);
                }
            }
        }

        public new string HomePhone
        {
            get
            {
                return base.HomePhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberHome(value);
                }
            }
        }

        public new bool MobileDevice
        {
            get
            {
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
            get
            {
                return base.MobileAccess;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetMobileAccess(value);
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

        public PrivacyMode Privacy
        {
            get
            {
                return privacy;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPrivacyMode(value);
                }
            }
        }

        public NotifyPrivacy NotifyPrivacy
        {
            get
            {
                return notifyPrivacy;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetNotifyPrivacyMode(value);
                }
            }
        }

        public RoamLiveProperty RoamLiveProperty
        {
            get
            {
                return roamLiveProperty;
            }
            set
            {
                roamLiveProperty = value;
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.ContactService.UpdateMe();
                }
            }
        }

#if MSNP16

        bool mpopEnabled;
        string _routeInfo = string.Empty;

        internal void SetRouteInfo(string info)
        {
            _routeInfo = info;
        }

        /// <summary>
        /// Route address, used for PNRP??
        /// </summary>
        public string RouteInfo
        {
            get
            {
                return _routeInfo;
            }
        }

        internal void SetMPOP(bool enabled)
        {
            mpopEnabled = enabled;
        }

        /// <summary>
        /// Whether the contact list owner has Multiple Points of Presence Support (MPOP) that is owner connect from multiple places.
        /// </summary>
        public bool MPOPEnabled
        {
            get
            {
                return mpopEnabled;
            }
        }

#endif

        new public PresenceStatus Status
        {
            get
            {
                return base.Status;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPresenceStatus(value);
                }
            }
        }

        new public string Name
        {
            get
            {
                return (base.Name == null) ? NickName : base.Name;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetScreenName(value);
                }
            }
        }

        string nickName;
        public string NickName
        {
            get
            {
                return nickName;
            }
        }


        #region Profile datafields
        bool validProfile;
        string loginTime;
        bool emailEnabled;
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
        int clientPort;

        public bool ValidProfile
        {
            get
            {
                return validProfile;
            }
        }

        public string LoginTime
        {
            get
            {
                return loginTime;
            }
            set
            {
                loginTime = value;
            }
        }

        public bool EmailEnabled
        {
            get
            {
                return emailEnabled;
            }
            set
            {
                emailEnabled = value;
            }
        }

        public string MemberIdHigh
        {
            get
            {
                return memberIdHigh;
            }
            set
            {
                memberIdHigh = value;
            }
        }

        public string MemberIdLowd
        {
            get
            {
                return memberIdLowd;
            }
            set
            {
                memberIdLowd = value;
            }
        }

        public string PreferredLanguage
        {
            get
            {
                return preferredLanguage;
            }
            set
            {
                preferredLanguage = value;
            }
        }

        public string PreferredMail
        {
            get
            {
                return preferredMail;
            }
            set
            {
                preferredMail = value;
            }
        }

        public string Country
        {
            get
            {
                return country;
            }
            set
            {
                country = value;
            }
        }

        public string PostalCode
        {
            get
            {
                return postalCode;
            }
            set
            {
                postalCode = value;
            }
        }

        public string Gender
        {
            get
            {
                return gender;
            }
            set
            {
                gender = value;
            }
        }

        public string Kid
        {
            get
            {
                return kid;
            }
            set
            {
                kid = value;
            }
        }

        public string Age
        {
            get
            {
                return age;
            }
            set
            {
                age = value;
            }
        }

        public string Birthday
        {
            get
            {
                return birthday;
            }
            set
            {
                birthday = value;
            }
        }


        public string Wallet
        {
            get
            {
                return wallet;
            }
            set
            {
                wallet = value;
            }
        }

        public string Sid
        {
            get
            {
                return sid;
            }
            set
            {
                sid = value;
            }
        }

        public string KV
        {
            get
            {
                return kV;
            }
            set
            {
                kV = value;
            }
        }

        public string MSPAuth
        {
            get
            {
                return mSPAuth;
            }
            set
            {
                mSPAuth = value;
            }
        }

        public IPAddress ClientIP
        {
            get
            {
                return clientIP;
            }
            set
            {
                clientIP = value;
            }
        }

        public int ClientPort
        {
            get
            {
                return clientPort;
            }
            set
            {
                clientPort = value;
            }
        }

        #endregion

        /// <summary>
        /// This will update the profile of the Owner object. 
        /// </summary>
        /// <remarks>This method fires the <see cref="ProfileReceived"/> event.</remarks>
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
        /// <param name="mspAuth"></param>
        /// <param name="clientIP"></param>
        /// <param name="clientPort"></param>
        /// <param name="nick"></param>
        /// <param name="mpop"></param>
        /// <param name="routeInfo"></param>
        internal void UpdateProfile(
            string loginTime, bool emailEnabled, string memberIdHigh,
            string memberIdLowd, string preferredLanguage, string preferredMail,
            string country, string postalCode, string gender,
            string kid, string age, string birthday,
            string wallet, string sid, string kv,
            string mspAuth, IPAddress clientIP, int clientPort,
            string nick
#if MSNP16
            , bool mpop
            , string routeInfo
#endif
            )
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
            MSPAuth = mspAuth;
            ClientIP = clientIP;
            ClientPort = clientPort;
            nickName = nick;
#if MSNP16
            mpopEnabled = mpop;
            _routeInfo = routeInfo;
#endif

            validProfile = true;

            OnProfileReceived(EventArgs.Empty);
        }

        /// <summary>
        /// Called when the server has send a profile description.
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnProfileReceived(EventArgs e)
        {
            if (ProfileReceived != null)
                ProfileReceived(this, e);
        }
    }
};