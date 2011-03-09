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
using System.Xml;
using System.Web;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {

#pragma warning disable 67 // disable "The event XXX is never used" warning

        [Obsolete(@"Obsoleted in 4.0. Please use TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> MobileMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TypingMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CircleTypingMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use NudgeReceived instead.", true)]
        public event EventHandler<EventArgs> CircleNudgeReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CircleTextMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TypingMessageReceived, NudgeReceived or TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CrossNetworkMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. There is no more support for SB.", true)]
        public event EventHandler<EventArgs> SBCreated;

        [Obsolete(@"Obsoleted in 4.0. There is no more support for SB.", true)]
        public event EventHandler<EventArgs> ConversationCreated;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactStatusChanged event with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberStatusChanged;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOnline instead.", true)]
        public event EventHandler<EventArgs> CircleOnline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOffline instead.", true)]
        public event EventHandler<EventArgs> CircleOffline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactStatusChanged instead.", true)]
        public event EventHandler<EventArgs> CircleStatusChanged;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOnline instead with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberOnline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOffline instead with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberOffline;

        [Obsolete(@"Obsoleted in 4.0. Please use JoinedGroupChat instead. Multiparty chat is for valid for circles or temporary groups. To chat 1 on 1, just call contact.SendMessage(), contact.SendNudge()...", true)]
        public event EventHandler<EventArgs> JoinedCircleConversation;

        [Obsolete(@"Obsoleted in 4.0. Please use LeftGroupChat instead. Multiparty chat is for valid for circles or temporary groups. To chat 1 on 1, just call contact.SendMessage(), contact.SendNudge()...", true)]
        public event EventHandler<EventArgs> LeftCircleConversation;

#pragma warning restore 67 // restore "The event XXX is never used" warning

        /// <summary>
        /// The owner of the contactlist. This is the identity that logged into the messenger network.
        /// </summary>
        [Obsolete(@"Obsoleted in 3.1, please use Messenger.Owner instead.
        The Owner property's behavior changed a little.
        It will remain null until user successfully login.
        You may need to change your code if you see this notice.
        For more information and example, please refer to the example client.", true)]
        public Owner Owner
        {
            get
            {
                return ContactList.Owner;
            }
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by ADL.", true)]
        protected virtual void OnADGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by RML.", true)]
        protected virtual void OnRMGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by SDG", true)]
        protected virtual void OnUBMReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnUUXReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by multiparty chat.", true)]
        protected virtual void OnRNGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by CreateContact soap call which adds all contacts in all networks.", true)]
        protected virtual void OnFQYReceived(NSMessage message)
        {
        }

        [Obsolete("MSNP18 no more supported", true)]
        protected virtual void OnILNReceived(NSMessage message)
        {
        }

        [Obsolete("MSNP18 no more supported", true)]
        protected virtual void OnBPRReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by PUT", true)]
        protected virtual void OnCHGReceived(NSMessage message)
        {
            ContactList.Owner.SetStatus(ParseStatus((string)message.CommandValues[0]));
        }

        /// <summary>
        /// Sets whether the contact list owner has a mobile device enabled.
        /// </summary>
        [Obsolete(@"Obsoleted in 4.0", true)]
        internal void SetMobileDevice(bool enabled)
        {
            if (ContactList.Owner == null)
                throw new MSNPSharpException("Not a valid owner");

            MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MBE", enabled ? "Y" : "N" }));
        }

        #region OnNLNReceived / OnNLNReceived, MSNP21TODO

        /// <summary>
        /// Called when a NLN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact on the forward list went online.
        /// <code>NLN [status] [clienttype:account] [name] [ClientCapabilities:48] [displayimage] (MSNP18)</code>
        /// <code>NLN [status] [account] [clienttype] [name] [ClientCapabilities:0] [displayimage] (MSNP16)</code>
        /// <code>NLN [status] [account] [clienttype] [name] [ClientCapabilities] [displayimage] (MSNP15)</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        protected virtual void OnNLNReceived(NSMessage message)
        {
            PresenceStatus newstatus = ParseStatus(message.CommandValues[0].ToString());

            IMAddressInfoType accountAddressType;
            string account;
            IMAddressInfoType viaAccountAddressType;
            string viaAccount;
            string fullAccount = message.CommandValues[1].ToString();

            if (false == Contact.ParseFullAccount(fullAccount,
                out accountAddressType, out account,
                out viaAccountAddressType, out viaAccount))
            {
                return;
            }

            ClientCapabilities newcaps = ClientCapabilities.None;
            ClientCapabilitiesEx newcapsex = ClientCapabilitiesEx.None;

            string newName = (message.CommandValues.Count >= 3) ? message.CommandValues[2].ToString() : String.Empty;
            string newDisplayImageContext = message.CommandValues.Count >= 5 ? message.CommandValues[4].ToString() : String.Empty;

            if (message.CommandValues.Count >= 4)
            {
                if (message.CommandValues[3].ToString().Contains(":"))
                {
                    newcaps = (ClientCapabilities)Convert.ToInt64(message.CommandValues[3].ToString().Split(':')[0]);
                    newcapsex = (ClientCapabilitiesEx)Convert.ToInt64(message.CommandValues[3].ToString().Split(':')[1]);
                }
                else
                {
                    newcaps = (ClientCapabilities)Convert.ToInt64(message.CommandValues[3].ToString());
                }
            }

            if (viaAccountAddressType == IMAddressInfoType.Circle)
            {
                #region Circle Status, or Circle Member status

                Contact circle = ContactList.GetCircle(viaAccount);

                if (newcaps != ClientCapabilities.None &&
                    newcapsex != ClientCapabilitiesEx.None)  //This is NOT a circle's presence status.
                {
                    if (circle == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnNLNReceived] Cannot update status for user, circle not found: " + fullAccount);

                        return;
                    }

                    if (!circle.ContactList.HasContact(account, accountAddressType))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnNLNReceived] User not found in the specific circle: " + fullAccount + ", a contact was created by NSCommand.");
                    }

                    Contact contact = circle.ContactList.GetContact(account, accountAddressType);

                    contact.SetName(MSNHttpUtility.NSDecode(message.CommandValues[2].ToString()));
                    contact.EndPointData[Guid.Empty].ClientCapabilities = newcaps;
                    contact.EndPointData[Guid.Empty].ClientCapabilitiesEx = newcapsex;

                    if (contact != ContactList.Owner && newDisplayImageContext.Length > 10)
                    {
                        if (contact.DisplayImage != newDisplayImageContext)
                        {
                            contact.UserTileLocation = newDisplayImageContext;
                            contact.FireDisplayImageContextChangedEvent(newDisplayImageContext);
                        }
                    }

                    PresenceStatus oldStatus = contact.Status;

                    if (oldStatus != newstatus)
                    {
                        contact.SetStatus(newstatus);

                        // The contact changed status
                        OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, circle, oldStatus, newstatus));

                        // The contact goes online
                        OnContactOnline(new ContactStatusChangedEventArgs(contact, circle, oldStatus, newstatus));
                    }
                }
                else
                {
                    if (account == ContactList.Owner.Account.ToLowerInvariant())
                    {
                        if (circle == null)
                            return;

                        PresenceStatus oldCircleStatus = circle.Status;
                        string[] guidDomain = viaAccount.Split('@');

                        if (guidDomain.Length != 0 &&
                            (oldCircleStatus == PresenceStatus.Offline || oldCircleStatus == PresenceStatus.Hidden))
                        {
                            //This is a PUT command send from server when we login.
                            JoinMultiparty(circle);
                        }

                        circle.SetStatus(newstatus);

                        OnContactStatusChanged(new ContactStatusChangedEventArgs(circle, oldCircleStatus, newstatus));
                        OnContactOnline(new ContactStatusChangedEventArgs(circle, oldCircleStatus, newstatus));
                        return;
                    }
                }

                #endregion
            }
            else
            {
                Contact contact = (account == ContactList.Owner.Account.ToLowerInvariant() && accountAddressType == IMAddressInfoType.WindowsLive)
                    ? ContactList.Owner : ContactList.GetContact(account, accountAddressType);

                #region Contact Status

                if (contact != null)
                {
                    if (IsSignedIn && account == ContactList.Owner.Account.ToLowerInvariant() &&
                        accountAddressType == IMAddressInfoType.WindowsLive)
                    {
                        SetPresenceStatus(newstatus);
                        return;
                    }

                    contact.SetName(MSNHttpUtility.NSDecode(newName));
                    contact.EndPointData[Guid.Empty].ClientCapabilities = newcaps;
                    contact.EndPointData[Guid.Empty].ClientCapabilitiesEx = newcapsex;

                    if (contact != ContactList.Owner)
                    {
                        if (newDisplayImageContext.Length > 10 && contact.DisplayImage != newDisplayImageContext)
                        {
                            contact.UserTileLocation = newDisplayImageContext;
                            contact.FireDisplayImageContextChangedEvent(newDisplayImageContext);
                        }

                        if (message.CommandValues.Count >= 6)
                        {
                            newDisplayImageContext = message.CommandValues[5].ToString();
                            contact.UserTileURL = new Uri(HttpUtility.UrlDecode(newDisplayImageContext));
                        }

                        PresenceStatus oldStatus = contact.Status;
                        contact.SetStatus(newstatus);

                        // The contact changed status
                        OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, oldStatus, newstatus));

                        // The contact goes online
                        OnContactOnline(new ContactStatusChangedEventArgs(contact, oldStatus, newstatus));
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// Called when a FLN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a user went offline.
        /// <code>FLN [clienttype:account] [caps:0] [networkpng] (MSNP18)</code>
        /// <code>FLN [account] [clienttype] [caps:0] [networkpng] (MSNP16)</code>
        /// <code>FLN [account] [clienttype] [caps] [networkpng] (MSNP15)</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        protected virtual void OnFLNReceived(NSMessage message)
        {
            IMAddressInfoType accountAddressType;
            string account;
            IMAddressInfoType viaAccountAddressType;
            string viaAccount;
            string fullAccount = message.CommandValues[0].ToString();

            if (false == Contact.ParseFullAccount(fullAccount,
                out accountAddressType, out account,
                out viaAccountAddressType, out viaAccount))
            {
                return;
            }

            ClientCapabilities newCaps = ClientCapabilities.None;
            ClientCapabilitiesEx newCapsEx = ClientCapabilitiesEx.None;

            if (message.CommandValues.Count >= 2)
            {
                if (message.CommandValues[1].ToString().Contains(":"))
                {
                    newCaps = (ClientCapabilities)Convert.ToInt64(message.CommandValues[1].ToString().Split(':')[0]);
                    newCapsEx = (ClientCapabilitiesEx)Convert.ToInt64(message.CommandValues[1].ToString().Split(':')[1]);
                }
                else
                {
                    newCaps = (ClientCapabilities)Convert.ToInt64(message.CommandValues[1].ToString());
                }
            }

            if (viaAccountAddressType == IMAddressInfoType.Circle)
            {
                #region Circle and CircleMemberStatus

                Contact circle = ContactList.GetCircle(viaAccount);

                if (circle == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnFLNReceived] Cannot update status for user since circle not found: " + fullAccount);
                    return;
                }

                if (account == ContactList.Owner.Account.ToLowerInvariant())  //Circle status
                {
                    string capabilityString = message.CommandValues[1].ToString();
                    if (capabilityString == "0:0")  //This is a circle's presence status.
                    {
                        PresenceStatus oldCircleStatus = circle.Status;
                        PresenceStatus newCircleStatus = PresenceStatus.Offline;
                        circle.SetStatus(newCircleStatus);

                        OnContactStatusChanged(new ContactStatusChangedEventArgs(circle, oldCircleStatus, newCircleStatus));
                        OnContactOffline(new ContactStatusChangedEventArgs(circle, oldCircleStatus, newCircleStatus));
                    }

                    return;
                }
                else
                {
                    if (!circle.ContactList.HasContact(account, accountAddressType))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnFLNReceived] Cannot update status for user since user not found in the specific circle: " + fullAccount);
                        return;
                    }

                    Contact contact = circle.ContactList.GetContact(account, accountAddressType);
                    PresenceStatus oldStatus = contact.Status;

                    if (oldStatus != PresenceStatus.Offline)
                    {
                        PresenceStatus newStatus = PresenceStatus.Offline;
                        contact.SetStatus(newStatus);

                        // The contact changed status
                        OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, circle, oldStatus, newStatus));

                        // The contact goes online
                        OnContactOffline(new ContactStatusChangedEventArgs(contact, circle, oldStatus, newStatus));
                    }

                    return;
                }

                #endregion
            }
            else
            {
                Contact contact = (account == ContactList.Owner.Account.ToLowerInvariant() && accountAddressType == IMAddressInfoType.WindowsLive)
                    ? ContactList.Owner : ContactList.GetContactWithCreate(account, accountAddressType);

                #region Contact Staus

                if (contact != null)
                {
                    lock (contact.EndPointData)
                    {
                        contact.EndPointData.Clear();
                        contact.EndPointData[Guid.Empty] = new EndPointData(contact.Account.ToLowerInvariant(), Guid.Empty);
                        contact.EndPointData[Guid.Empty].ClientCapabilities = newCaps;
                        contact.EndPointData[Guid.Empty].ClientCapabilitiesEx = newCapsEx;
                    }

                    if (contact != ContactList.Owner && message.CommandValues.Count >= 3 &&
                        accountAddressType == IMAddressInfoType.Yahoo)
                    {
                        string newdp = message.CommandValues[2].ToString();
                        contact.UserTileURL = new Uri(HttpUtility.UrlDecode(newdp));
                    }

                    PresenceStatus oldStatus = contact.Status;
                    PresenceStatus newStatus = PresenceStatus.Offline;
                    contact.SetStatus(newStatus);

                    // the contact changed status
                    OnContactStatusChanged(new ContactStatusChangedEventArgs(contact, oldStatus, newStatus));

                    // the contact goes offline
                    OnContactOffline(new ContactStatusChangedEventArgs(contact, oldStatus, newStatus));
                }

                #endregion
            }
        }

        #endregion

        /// <summary>
        /// Called when a UBX command has been received.
        /// UBX [type:account;via] [payload length]
        /// </summary>
        /// <param name="message"></param>
        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnUBXReceived(NSMessage message)
        {
            //check the payload length
            if (message.InnerMessage == null || message.InnerBody == null || message.InnerBody.Length == 0)
                return;

            string fullAccount = message.CommandValues[0].ToString(); // 1:username@hotmail.com;via=9:guid@live.com

            IMAddressInfoType accountAddressType;
            string account;
            IMAddressInfoType viaAccountAddressType;
            string viaAccount;

            if (false == Contact.ParseFullAccount(fullAccount,
                out accountAddressType, out account,
                out viaAccountAddressType, out viaAccount))
            {
                return;
            }

            Contact contact = null;
            if (viaAccountAddressType == IMAddressInfoType.Circle)
            {
                Contact circle = ContactList.GetCircle(viaAccount);

                if (circle == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnUBXReceived] Cannot retrieve circle for user: " + fullAccount);
                    return;
                }
                else
                {
                    if (!circle.ContactList.HasContact(account, accountAddressType))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "[OnUBXReceived] Cannot retrieve user for from circle: " + fullAccount);
                        return;
                    }

                    contact = circle.ContactList.GetContact(account, accountAddressType);
                }
            }
            else
            {
                if (account != ContactList.Owner.Account)
                {
                    if (ContactList.HasContact(account, accountAddressType))
                    {
                        contact = ContactList.GetContact(account, accountAddressType);
                    }
                }
                else
                {
                    contact = ContactList.Owner;
                }
            }

            if (contact != null)
            {
                contact.SetPersonalMessage(new PersonalMessage(message));
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(new MemoryStream(message.InnerBody));

                    // Get Fridenly Name
                    string friendlyName = GetFriendlyNameFromUBXXmlData(xmlDoc);
                    contact.SetName(string.IsNullOrEmpty(friendlyName) ? contact.Account : friendlyName);

                    //Get UserTileLocation
                    contact.UserTileLocation = GetUserTileLocationFromUBXXmlData(xmlDoc);

                    // Get the scenes context
                    string newSceneContext = GetSceneContextFromUBXXmlData(xmlDoc);

                    if (contact.SceneContext != newSceneContext)
                    {
                        contact.SceneContext = newSceneContext;
                        contact.FireSceneImageContextChangedEvent(newSceneContext);
                    }

                    // Get the color scheme
                    Color color = GetColorSchemeFromUBXXmlData(xmlDoc);
                    if (contact.ColorScheme != color)
                    {
                        contact.ColorScheme = color;
                        contact.OnColorSchemeChanged();
                    }

                    //Get endpoint data.
                    bool isPrivateEndPoint = (contact is Owner);

                    #region Regular contacts

                    if (!isPrivateEndPoint)
                    {
                        List<EndPointData> endPoints = GetEndPointDataFromUBXXmlData(contact.Account, xmlDoc, isPrivateEndPoint);
                        if (endPoints.Count > 0)
                        {
                            foreach (EndPointData epData in endPoints)
                            {
                                contact.EndPointData[epData.Id] = epData;
                            }
                        }
                    }

                    #endregion

                    #region Only for Owner, set private endpoint info.

                    XmlNodeList privateEndPoints = xmlDoc.GetElementsByTagName("PrivateEndpointData"); //Only the owner will have this field.

                    if (privateEndPoints.Count > 0)
                    {
                        PlaceChangedReason placechangeReason = (privateEndPoints.Count >= contact.EndPointData.Count ? PlaceChangedReason.SignedIn : PlaceChangedReason.SignedOut);
                        Dictionary<Guid, PrivateEndPointData> epList = new Dictionary<Guid, PrivateEndPointData>(0);
                        foreach (XmlNode pepdNode in privateEndPoints)
                        {
                            Guid id = new Guid(pepdNode.Attributes["id"].Value);
                            PrivateEndPointData privateEndPoint = (contact.EndPointData.ContainsKey(id) ? (contact.EndPointData[id] as PrivateEndPointData) : new PrivateEndPointData(contact.Account, id));
                            privateEndPoint.Name = (pepdNode["EpName"] == null) ? String.Empty : pepdNode["EpName"].InnerText;
                            privateEndPoint.Idle = (pepdNode["Idle"] == null) ? false : bool.Parse(pepdNode["Idle"].InnerText);
                            privateEndPoint.ClientType = (pepdNode["ClientType"] == null) ? "1" : pepdNode["ClientType"].InnerText;
                            privateEndPoint.State = (pepdNode["State"] == null) ? PresenceStatus.Unknown : ParseStatus(pepdNode["State"].InnerText);
                            epList[id] = privateEndPoint;
                        }


                        if (contact is Owner)
                        {
                            TriggerPlaceChangeEvent(epList);
                        }
                    }

                    #endregion
                }
                catch (Exception xmlex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "[OnUBXReceived] Xml parse error: " + xmlex.Message);
                }
            }
        }

        /// <summary>
        /// Called when an PRP command has been received.
        /// </summary>
        /// <remarks>
        /// Informs about the phone numbers of the contact list owner.
        /// <code>PRP [TransactionID] [ListVersion] PhoneType Number</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnPRPReceived(NSMessage message)
        {
            string number = String.Empty;
            string type = String.Empty;
            if (message.CommandValues.Count >= 3)
            {
                number = MSNHttpUtility.NSDecode((string)message.CommandValues[2]);
                type = message.CommandValues[1].ToString();
            }
            else
            {
                number = MSNHttpUtility.NSDecode((string)message.CommandValues[1]);
                type = message.CommandValues[0].ToString();
            }

            switch (type)
            {
                case "PHH":
                    ContactList.Owner.PhoneNumbers[ContactPhoneTypes.ContactPhonePersonal] = number;
                    break;
                case "PHW":
                    ContactList.Owner.PhoneNumbers[ContactPhoneTypes.ContactPhoneBusiness] = number;
                    break;
                case "PHM":
                    ContactList.Owner.PhoneNumbers[ContactPhoneTypes.ContactPhoneMobile] = number;
                    break;
                case "MBE":
                    ContactList.Owner.SetMobileDevice((number == "Y") ? true : false);
                    break;
                case "MOB":
                    ContactList.Owner.SetMobileAccess((number == "Y") ? true : false);
                    break;
                case "MFN":
                    ContactList.Owner.SetName(MSNHttpUtility.NSDecode((string)message.CommandValues[1]));
                    break;
            }
        }

    }
};
