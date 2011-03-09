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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MSNPSharp.Core;
using MSNPSharp.MSNWS.MSNABSharingService;

namespace MSNPSharp
{
    /// <summary>
    /// A new type of group introduces with WLM2009.
    /// </summary>
    [Serializable()]
    public partial class Circle : Contact
    {
        /// <summary>
        /// live.com
        /// </summary>
        public const string DefaultHostDomain = "live.com";

        private ContactList contactList = null;
        private string hostDomain = DefaultHostDomain;

        private ContactType meContact = null;

        public string HostDomain
        {
            get
            {
                return hostDomain;
            }
        }

        public ContactList ContactList
        {
            get
            {
                return contactList;
            }
        }

        /// <summary>
        /// Circle constructor
        /// </summary>
        /// <param name="me">The "Me Contact" in the addressbook.</param>
        /// <param name="circleInfo"></param>
        /// <param name="handler"></param>
        public Circle(ContactType me, CircleInverseInfoType circleInfo, NSMessageHandler handler)
            : base(circleInfo.Content.Handle.Id.ToLowerInvariant(), circleInfo.Content.Handle.Id.ToLowerInvariant() + "@" + circleInfo.Content.Info.HostedDomain.ToLowerInvariant(), IMAddressInfoType.Circle, me.contactInfo.CID, handler)
        {
            hostDomain = circleInfo.Content.Info.HostedDomain.ToLowerInvariant();

            CircleRole = (CirclePersonalMembershipRole)Enum.Parse(typeof(CirclePersonalMembershipRole), circleInfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role);

            SetName(circleInfo.Content.Info.DisplayName);
            SetNickName(Name);

            meContact = me;

            CID = 0;

            contactList = new ContactList(AddressBookId, new Owner(AddressBookId, me.contactInfo.passportName, me.contactInfo.CID, NSMessageHandler), NSMessageHandler);
            Initialize();
        }

       

        protected virtual void Initialize()
        {
            ContactType = MessengerContactType.Circle;
            Lists = RoleLists.Allow | RoleLists.Forward;
        }

        protected override bool CanReceiveMessage
        {
            get
            {
                bool ok = base.CanReceiveMessage;

                if (!ok)
                    throw new InvalidOperationException("Cannot send a message without signning in to the server. Please sign in first.");

                if (NSMessageHandler.ContactList.Owner.Status == PresenceStatus.Hidden)
                    throw new InvalidOperationException("Cannot send a message to the group when you are in 'Hidden' status.");

                return true;
            }
        }
    }
};
