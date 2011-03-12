#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.IO;
using System.Net;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;

    [Serializable]
    public class Owner : Contact
    {
        /// <summary>
        /// Fired when owner places changed.
        /// </summary>
        public event EventHandler<PlaceChangedEventArgs> PlacesChanged;

        private string epName = Environment.MachineName;
        private bool passportVerified;

        public Owner(string abId, string account, long cid, NSMessageHandler handler)
            : this(new Guid(abId), account, cid, handler)
        {
        }

        public Owner(Guid abId, string account, long cid, NSMessageHandler handler)
            : base(abId, account, IMAddressInfoType.WindowsLive, cid, handler)
        {
        }

        /// <summary>
        /// Called when the End Points changed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPlacesChanged(PlaceChangedEventArgs e)
        {
            if (PlacesChanged != null)
                PlacesChanged(this, e);
        }


        /// <summary>
        /// Fired when owner profile received.
        /// </summary>
        public event EventHandler<EventArgs> ProfileReceived;

        internal void CreateDefaultDisplayImage(SerializableMemoryStream sms)
        {
            if (sms == null)
            {
                sms = new SerializableMemoryStream();
                Image msnpsharpDefaultImage = Properties.Resources.MSNPSharp_logo.Clone() as Image;
                msnpsharpDefaultImage.Save(sms, msnpsharpDefaultImage.RawFormat);
            }

            DisplayImage displayImage = new DisplayImage(Account.ToLowerInvariant(), sms);

            this.DisplayImage = displayImage;
        }

        internal void SetChangedPlace(PrivateEndPointData signedOnOffPlace, PlaceChangedReason action)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "The account was " + action + " at another place: " + signedOnOffPlace.Name + " " + signedOnOffPlace.Id, GetType().Name);

            bool triggerEvent = false;

            lock (SyncObject)
            {
                switch (action)
                {
                    case PlaceChangedReason.SignedIn:
                        EndPointData[signedOnOffPlace.Id] = signedOnOffPlace;
                        triggerEvent = true;
                        break;

                    case PlaceChangedReason.SignedOut:
                        if (EndPointData.ContainsKey(signedOnOffPlace.Id))
                        {
                            EndPointData.Remove(signedOnOffPlace.Id);
                            triggerEvent = true;
                        }
                        break;
                }
            }

            if (triggerEvent)
            {
                OnPlacesChanged(new PlaceChangedEventArgs(signedOnOffPlace, action));
            }
        }

        /// <summary>
        /// This place's name
        /// </summary>
        public string EpName
        {
            get
            {
                return epName;
            }
            set
            {
                if (NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline)
                {
                    NSMessageHandler.SetPresenceStatus(
                        Status,
                        LocalEndPointIMCapabilities, LocalEndPointIMCapabilitiesEx,
                        LocalEndPointPECapabilities, LocalEndPointPECapabilitiesEx,
                        value, PersonalMessage, false);
                }

                epName = value;
            }
        }

        /// <summary>
        /// Get or set the IM <see cref="ClientCapabilities"/> of local end point.
        /// </summary>
        public ClientCapabilities LocalEndPointIMCapabilities
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].IMCapabilities;

                return ClientCapabilities.None;
            }
            set
            {
                if (value != LocalEndPointIMCapabilities)
                {
                    NSMessageHandler.SetPresenceStatus(
                        Status,
                        value, LocalEndPointIMCapabilitiesEx,
                        LocalEndPointPECapabilities, LocalEndPointPECapabilitiesEx,
                        EpName, PersonalMessage, false);

                    EndPointData[NSMessageHandler.MachineGuid].IMCapabilities = value;
                }
            }
        }

        /// <summary>
        /// Get or set the IM <see cref="ClientCapabilitiesEx"/> of local end point.
        /// </summary>
        public ClientCapabilitiesEx LocalEndPointIMCapabilitiesEx
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].IMCapabilitiesEx;

                return ClientCapabilitiesEx.None;
            }

            set
            {
                if (value != LocalEndPointIMCapabilitiesEx)
                {
                    NSMessageHandler.SetPresenceStatus(
                        Status,
                        LocalEndPointIMCapabilities, value,
                        LocalEndPointPECapabilities, LocalEndPointPECapabilitiesEx,
                        EpName, PersonalMessage, false);

                    EndPointData[NSMessageHandler.MachineGuid].IMCapabilitiesEx = value;
                }
            }
        }

        /// <summary>
        /// Get or set the PE (P2P) <see cref="ClientCapabilities"/> of local end point.
        /// </summary>
        public ClientCapabilities LocalEndPointPECapabilities
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].PECapabilities;

                return ClientCapabilities.None;
            }
            set
            {
                if (value != LocalEndPointPECapabilities)
                {
                    NSMessageHandler.SetPresenceStatus(
                        Status,
                        LocalEndPointIMCapabilities, LocalEndPointIMCapabilitiesEx,
                        value, LocalEndPointPECapabilitiesEx,
                        EpName, PersonalMessage, false);

                    EndPointData[NSMessageHandler.MachineGuid].PECapabilities = value;
                }
            }
        }

        /// <summary>
        /// Get or set the PE (P2P) <see cref="ClientCapabilitiesEx"/> of local end point.
        /// </summary>
        public ClientCapabilitiesEx LocalEndPointPECapabilitiesEx
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].PECapabilitiesEx;

                return ClientCapabilitiesEx.None;
            }

            set
            {
                if (value != LocalEndPointPECapabilitiesEx)
                {
                    NSMessageHandler.SetPresenceStatus(
                        Status,
                        LocalEndPointIMCapabilities, LocalEndPointIMCapabilitiesEx,
                        LocalEndPointPECapabilities, value,
                        EpName, PersonalMessage, false);

                    EndPointData[NSMessageHandler.MachineGuid].PECapabilitiesEx = value;
                }
            }
        }

        /// <summary>
        /// Sign the owner out from every place.
        /// </summary>
        public void SignoutFromEverywhere()
        {
            foreach (Guid place in EndPointData.Keys)
            {
                if (place != NSMessageHandler.MachineGuid)
                {
                    SignoutFrom(place);
                }
            }

            SignoutFrom(NSMessageHandler.MachineGuid);
            Status = PresenceStatus.Offline;
        }

        /// <summary>
        /// Sign the owner out from the specificed place.
        /// </summary>
        /// <param name="endPointID">The EndPoint guid to be signed out</param>
        public void SignoutFrom(Guid endPointID)
        {
            if (endPointID == Guid.Empty)
            {
                SignoutFromEverywhere();
                return;
            }

            if (EndPointData.ContainsKey(endPointID))
            {
                NSMessageHandler.SignoutFrom(endPointID);

                if (endPointID == NSMessageHandler.MachineGuid)
                {
                    Status = PresenceStatus.Offline;
                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Invalid place (signed out already): " + endPointID.ToString("B"), GetType().Name);
            }
        }

        /// <summary>
        /// Owner display image. The image is broadcasted automatically.
        /// </summary>
        public override DisplayImage DisplayImage
        {
            get
            {
                return base.DisplayImage;
            }

            internal set
            {
                if (value != null)
                {
                    if (base.DisplayImage != null)
                    {
                        if (value.Sha == base.DisplayImage.Sha)
                        {
                            return;
                        }

                        MSNObjectCatalog.GetInstance().Remove(base.DisplayImage);
                    }

                    SetDisplayImageAndFireDisplayImageChangedEvent(value);
                    value.Creator = Account;

                    MSNObjectCatalog.GetInstance().Add(base.DisplayImage);

                    PersonalMessage pm = (PersonalMessage == null)
                        ? new PersonalMessage(String.IsNullOrEmpty(NSMessageHandler.ContactService.Deltas.Profile.DisplayName) ? NickName : NSMessageHandler.ContactService.Deltas.Profile.DisplayName)
                        : PersonalMessage;

                    pm.UserTileLocation = value.IsDefaultImage ? string.Empty : value.ContextPlain;
                    
                    if (NSMessageHandler != null && NSMessageHandler.IsSignedIn &&
                        Status != PresenceStatus.Offline && Status != PresenceStatus.Unknown)
                    {
                        NSMessageHandler.SetPresenceStatus(
                            Status,
                            LocalEndPointIMCapabilities, LocalEndPointIMCapabilitiesEx,
                            LocalEndPointPECapabilities, LocalEndPointPECapabilitiesEx,
                            EpName, PersonalMessage, true);
                    }
                }
            }
        }

        public override SceneImage SceneImage
        {
            get
            {
                return base.SceneImage;
            }
            internal set
            {
                if (value != null)
                {
                    if (base.SceneImage != null)
                    {
                        if (value.Sha == base.SceneImage.Sha)
                        {
                            return;
                        }
                    }

                    value.Creator = Account;
                    base.SetSceneImage(value);
                }
            }
        }

        public override Color ColorScheme
        {
            get
            {
                return base.ColorScheme;
            }
            internal set
            {
                if (ColorScheme != value)
                {
                    base.ColorScheme = value;
                    NSMessageHandler.ContactService.Deltas.Profile.ColorScheme = ColorTranslator.ToOle(value);

                    base.OnColorSchemeChanged();
                }
            }
        }

        /// <summary>
        /// Set the scene image and the scheme color for the owner.
        /// </summary>
        /// <param name="imageScene">Set this to null or the default display image if you want the default MSN scene.</param>
        /// <param name="schemeColor"></param>
        /// <returns>
        /// The result will return false if the image scene and color are the same, compared to the current one.
        /// </returns>
        public bool SetScene(Image imageScene, Color schemeColor)
        {
            if (imageScene == SceneImage.Image && schemeColor == ColorScheme)
                return false;

            ColorScheme = schemeColor;
            if (imageScene != SceneImage.Image)
            {
                if (imageScene != null)
                {
                    MemoryStream sms = new MemoryStream();
                    imageScene.Save(sms, imageScene.RawFormat);

                    SceneImage = new SceneImage(NSMessageHandler.ContactList.Owner.Account.ToLowerInvariant(), sms);
                }
                else
                    SceneImage = new SceneImage(NSMessageHandler.ContactList.Owner.Account.ToLowerInvariant(), true);

                SaveOriginalSceneImageAndFireSceneImageChangedEvent(
                    new SceneImageChangedEventArgs(SceneImage, DisplayImageChangedType.TransmissionCompleted, false));
            }
            else
                NSMessageHandler.ContactService.Deltas.Save(true);

            if (NSMessageHandler != null)
                NSMessageHandler.SetSceneData(SceneImage, ColorScheme);

            return true;
        }

        /// <summary>
        /// Personel message
        /// </summary>
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

        public new string MobilePhone
        {
            get
            {
                return base.MobilePhone;
            }
            set
            {
                PhoneNumbers[ContactPhoneTypes.ContactPhoneMobile] = value;
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
                    ContactList.Owner.PhoneNumbers[ContactPhoneTypes.ContactPhoneBusiness] = value;
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
                    ContactList.Owner.PhoneNumbers[ContactPhoneTypes.ContactPhonePersonal] = value;
                }
            }
        }

        public new bool MobileDevice
        {
            get
            {
                return base.MobileDevice;
            }
        }

        public new bool MobileAccess
        {
            get
            {
                return base.MobileAccess;
            }
        }

        /// <summary>
        /// Whether this account is verified by email. If an account is not verified, "(email not verified)" will be displayed after a contact's displayname.
        /// </summary>
        public bool PassportVerified
        {
            get
            {
                return passportVerified;
            }
            internal set
            {
                passportVerified = value;
            }
        }

        /// <summary>
        /// Reaction when sign in at another place.
        /// </summary>
        [Obsolete(@"Obsoleted in MSNP21, default is enabled and cannot be disabled.", true)]
        public object MPOPMode
        {
            get
            {
                return MPOP.KeepOnline;
            }
        }

        public override PresenceStatus Status
        {
            get
            {
                return base.Status;
            }

            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPresenceStatus(
                        value,
                        LocalEndPointIMCapabilities, LocalEndPointIMCapabilitiesEx,
                        LocalEndPointPECapabilities, LocalEndPointPECapabilitiesEx,
                        EpName, PersonalMessage, false);
                }

                (EndPointData[NSMessageHandler.MachineGuid] as PrivateEndPointData).State = base.Status;
            }
        }

        public override string Name
        {
            get
            {
                return string.IsNullOrEmpty(base.Name) ? NickName : base.Name;
            }

            set
            {
                if (Name == value)
                    return;

                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetScreenName(value);
                }
            }
        }


        #region Profile datafields

        private Dictionary<string, string> msgProfile = new Dictionary<string, string>();
        bool validProfile;

        public bool ValidProfile
        {
            get
            {
                return validProfile;
            }
            internal set
            {
                validProfile = value;
            }
        }

        public bool EmailEnabled
        {
            get
            {
                return msgProfile.ContainsKey("EmailEnabled") && msgProfile["EmailEnabled"] == "1";
            }
            set
            {
                msgProfile["EmailEnabled"] = value ? "1" : "0";
            }
        }

        public long MemberIdHigh
        {
            get
            {
                return msgProfile.ContainsKey("MemberIdHigh") ? long.Parse(msgProfile["MemberIdHigh"]) : 0;
            }
            set
            {
                msgProfile["MemberIdHigh"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public long MemberIdLowd
        {
            get
            {
                return msgProfile.ContainsKey("MemberIdLow") ? long.Parse(msgProfile["MemberIdLow"]) : 0;
            }
            set
            {
                msgProfile["MemberIdLow"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public string PreferredLanguage
        {
            get
            {
                return msgProfile.ContainsKey("lang_preference") ? msgProfile["lang_preference"] : String.Empty;
            }
            set
            {
                msgProfile["lang_preference"] = value;
            }
        }

        public string Country
        {
            get
            {
                return msgProfile.ContainsKey("country") ? msgProfile["country"] : String.Empty;
            }
            set
            {
                msgProfile["country"] = value;
            }
        }

        public string Kid
        {
            get
            {
                return msgProfile.ContainsKey("Kid") ? msgProfile["Kid"] : String.Empty;
            }
            set
            {
                msgProfile["Kid"] = value;
            }
        }

        public long Flags
        {
            get
            {
                return msgProfile.ContainsKey("Flags") ? long.Parse(msgProfile["Flags"]) : 0;
            }
            set
            {
                msgProfile["Flags"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public string Sid
        {
            get
            {
                return msgProfile.ContainsKey("Sid") ? msgProfile["Sid"] : String.Empty;
            }
            set
            {
                msgProfile["Sid"] = value;
            }
        }

        public IPAddress ClientIP
        {
            get
            {
                return msgProfile.ContainsKey("ClientIP") ? IPAddress.Parse(msgProfile["ClientIP"]) : IPAddress.None;
            }
            set
            {
                msgProfile["ClientIP"] = value.ToString();
            }
        }

        /// <summary>
        /// Route address, used for PNRP??
        /// </summary>
        public string RouteInfo
        {
            get
            {
                return msgProfile.ContainsKey("RouteInfo") ? msgProfile["RouteInfo"] : String.Empty;
            }
            internal set
            {
                msgProfile["RouteInfo"] = value;
            }
        }

        public int ClientPort
        {
            get
            {
                return msgProfile.ContainsKey("ClientPort") ? int.Parse(msgProfile["ClientPort"]) : 0;
            }
            set
            {
                msgProfile["ClientPort"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        #endregion

        /*
EmailEnabled: 1
MemberIdHigh: 123456
MemberIdLow: -1234567890
lang_preference: 2052
country: US
Kid: 0
Flags: 1073742915
sid: 72652
ClientIP: XXX.XXX.XXX.XXX
Nickname: New
RouteInfo: msnp://XXX.XXX.XXX.XXX/013557A5
*/
        internal void UpdateProfile(StrDictionary hdr)
        {
            foreach (StrKeyValuePair pair in hdr)
            {
                msgProfile[String.Copy(pair.Key)] = String.Copy(pair.Value);
            }

            ValidProfile = true;

            if (msgProfile.ContainsKey("Nickname"))
                SetNickName(msgProfile["Nickname"]);

            OnProfileReceived(EventArgs.Empty);
        }

        /// <summary>
        /// Called when the server has send a profile description.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProfileReceived(EventArgs e)
        {
            if (ProfileReceived != null)
                ProfileReceived(this, e);
        }

    }
};
