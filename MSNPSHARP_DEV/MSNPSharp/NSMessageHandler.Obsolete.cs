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
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.P2P;

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
            Owner.SetStatus(ParseStatus((string)message.CommandValues[0]));
        }

        /// <summary>
        /// Sets whether the contact list owner has a mobile device enabled.
        /// </summary>
        [Obsolete(@"Obsoleted in 4.0", true)]
        internal void SetMobileDevice(bool enabled)
        {
            if (Owner == null)
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
                    if (contact != Owner && newDisplayImageContext.Length > 10)
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
                    if (account == Owner.Account.ToLowerInvariant())
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
                Contact contact = (account == Owner.Account.ToLowerInvariant() && accountAddressType == IMAddressInfoType.WindowsLive)
                    ? Owner : ContactList.GetContact(account, accountAddressType);

                #region Contact Status

                if (contact != null)
                {
                    if (IsSignedIn && account == Owner.Account.ToLowerInvariant() &&
                        accountAddressType == IMAddressInfoType.WindowsLive)
                    {
                        //SetPresenceStatus(newstatus);
                        return;
                    }

                    contact.SetName(MSNHttpUtility.NSDecode(newName));

                    if (contact != Owner)
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

                if (account == Owner.Account.ToLowerInvariant())  //Circle status
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
                Contact contact = (account == Owner.Account.ToLowerInvariant() && accountAddressType == IMAddressInfoType.WindowsLive)
                    ? Owner : ContactList.GetContactWithCreate(account, accountAddressType);

                #region Contact Staus

                if (contact != null)
                {

                    if (contact != Owner && message.CommandValues.Count >= 3 &&
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

        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnUBXReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnPRPReceived(NSMessage message)
        {
        }


        /// <summary>
        /// Called when a UBN command has been received.
        /// </summary>
        /// <remarks>
        /// <code>UBN [account;{GUID}] [1:xml data,2:sip invite, 3: MSNP2P SLP data, 4:logout, 10: TURN] [PayloadLegth]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete(@"Obsoleted in MSNP21. Replaced by SIPv2.", true)]
        protected virtual void OnUBNReceived(NSMessage message)
        {
            NetworkMessage networkMessage = message as NetworkMessage;
            if (message.InnerBody != null)
            {
                switch (message.CommandValues[1].ToString())
                {
                    case "3":
                        {
                            SLPMessage slpMessage = SLPMessage.Parse(message.InnerBody);

                            if (slpMessage == null)
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                    "Received P2P UBN message with unknown payload:\r\n" + Encoding.UTF8.GetString(message.InnerBody), GetType().Name);
                            }
                            else
                            {
                                string source = message.CommandValues[0].ToString();
                                if (String.IsNullOrEmpty(source))
                                    source = slpMessage.Source;

                                string sourceEmail = source;
                                if (sourceEmail.Contains(";"))
                                    sourceEmail = sourceEmail.Split(';')[0].ToLowerInvariant();

                                Guid sourceGuid = slpMessage.FromEndPoint;
                                Contact sourceContact;

                                P2PVersion ver = (sourceGuid != Guid.Empty) ? P2PVersion.P2PV2 : P2PVersion.P2PV1;

                                if (ContactList.HasContact(sourceEmail, IMAddressInfoType.WindowsLive))
                                {
                                    sourceContact = ContactList.GetContact(sourceEmail, IMAddressInfoType.WindowsLive);
                                    if (sourceContact.Status == PresenceStatus.Hidden || sourceContact.Status == PresenceStatus.Offline)
                                    {
                                        // If not return, we will get a 217 error (User not online).
                                        return;
                                    }
                                }

                                sourceContact = ContactList.GetContactWithCreate(sourceEmail, IMAddressInfoType.WindowsLive);

                                if (slpMessage.ContentType == "application/x-msnmsgr-transreqbody" ||
                                    slpMessage.ContentType == "application/x-msnmsgr-transrespbody" ||
                                    slpMessage.ContentType == "application/x-msnmsgr-transdestaddrupdate")
                                {
                                    P2PSession.ProcessDirectInvite(slpMessage, this, null);
                                }
                            }
                        }
                        break;

                    case "4":
                    case "8":
                        {
                            string logoutMsg = Encoding.UTF8.GetString(message.InnerBody);
                            if (logoutMsg.StartsWith("goawyplzthxbye") || logoutMsg == "gtfo")
                            {
                                if (messageProcessor != null)
                                    messageProcessor.Disconnect();
                            }
                            return;
                        }

                    case "10":
                        {
                            SLPMessage slpMessage = SLPMessage.Parse(message.InnerBody);
                            if (slpMessage != null &&
                                slpMessage.ContentType == "application/x-msnmsgr-turnsetup")
                            {
                                SLPRequestMessage request = slpMessage as SLPRequestMessage;
                                if (request != null && request.Method == "ACK")
                                {
                                    HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + request.BodyValues["ServerAddress"].Value);
                                    wr.Proxy = ConnectivitySettings.WebProxy;
                                    //wr.Credentials = new NetworkCredential(request.BodyValues["SessionUsername"].Value, request.BodyValues["SessionPassword"].Value);

                                    wr.Credentials = new NetworkCredential(
                                        request.BodyValues["SessionUsername"].Value,
                                        request.BodyValues["SessionPassword"].Value
                                    );

                                    wr.BeginGetResponse(delegate(IAsyncResult result)
                                    {
                                        try
                                        {
                                            using (Stream stream = ((WebRequest)result.AsyncState).EndGetResponse(result).GetResponseStream())
                                            {
                                                using (StreamReader r = new StreamReader(stream, Encoding.UTF8))
                                                {
                                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                                    "TURN response: " + r.ReadToEnd(), GetType().Name);
                                                }
                                            }
                                            wr.Abort();
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                                "TURN error: " + ex.ToString(), GetType().Name);
                                        }
                                    }, wr);
                                }
                            }
                        }
                        break;
                }
            }
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by SIPv2.", true)]
        protected virtual void OnUUNReceived(NSMessage message)
        {
        }

    }
};
