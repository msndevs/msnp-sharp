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
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    partial class ContactService
    {

        #region BlockContact & UnBlockContact

        /// <summary>
        /// Block this contact. After this you aren't able to receive messages from this contact. This contact
        /// will be placed in your block list and removed from your allowed list.
        /// </summary>
        /// <param name="contact">Contact to block</param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        public void BlockContact(Contact contact)
        {
            if (contact.OnAllowedList)
            {
                RemoveContactFromList(
                    contact,
                    RoleLists.Allow,
                    delegate
                    {
                        if (!contact.OnBlockedList)
                            AddContactToList(contact, RoleLists.Block, null);
                    }
                );
            }
            else if (!contact.OnBlockedList)
            {
                AddContactToList(contact, RoleLists.Block, null);
            }
        }

        /// <summary>
        /// Unblock this contact. After this you are able to receive messages from this contact. This contact
        /// will be removed from your blocked list and placed in your allowed list.
        /// </summary>
        /// <param name="contact">Contact to unblock</param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        public void UnBlockContact(Contact contact)
        {
            if (contact.OnBlockedList)
            {
                RemoveContactFromList(
                    contact,
                    RoleLists.Block,
                    delegate
                    {
                        if (!contact.OnAllowedList)
                            AddContactToList(contact, RoleLists.Allow, null);
                    }
                );
            }
            else if (!contact.OnAllowedList)
            {
                AddContactToList(contact, RoleLists.Allow, null);
            }
        }

        #region Block/UnBlock Circle

        /// <summary>
        /// Block a specific <see cref="Contact"/>. The ContactBlocked event of corresponding <see cref="Contact"/> will be fired after block operation succeeded.
        /// </summary>
        /// <param name="circle">The circle to block.</param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        public void BlockCircle(Contact circle)
        {
            if (circle.OnBlockedList)
                return;


            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState AddMemberObject = new MsnServiceState(PartnerScenario.BlockUnblock, "AddMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, AddMemberObject);

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            addMemberRequest.serviceHandle.Id = messengerService.Id.ToString();
            addMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = MemberRole.Block;

            CircleMember member = new CircleMember();

            member.Type = MessengerContactType.Circle;
            member.State = MemberState.Accepted;
            member.CircleId = circle.AddressBookId.ToString();

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member already exists"))
                    {
                        return;
                    }

                    NSMessageHandler.SendBlockCircleNSCommands(circle.AddressBookId, circle.HostDomain);

                    circle.RemoveFromList(RoleLists.Allow);
                    circle.AddToList(RoleLists.Block);
                    circle.SetStatus(PresenceStatus.Offline);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "AddMember completed: " + circle.ToString(), GetType().Name);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, AddMemberObject, addMemberRequest));
        }

        /// <summary>
        /// Unblock a specific <see cref="Contact"/>. The ContactUnBlocked event of corresponding <see cref="Contact"/> will be fired after unblock operation succeeded.
        /// </summary> 
        /// <param name="circle">The affected circle</param>
        [Obsolete(@"Obsoleted in 4.0", true)]
        public void UnBlockCircle(Contact circle)
        {
            if (!circle.OnBlockedList)
                return;

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            MsnServiceState DeleteMemberObject = new MsnServiceState(PartnerScenario.BlockUnblock, "DeleteMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, DeleteMemberObject);
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member does not exist"))
                    {
                        return;
                    }

                    NSMessageHandler.SendUnBlockCircleNSCommands(circle.AddressBookId, circle.HostDomain);
                    circle.RemoveFromList(RoleLists.Block);
                    circle.AddToList(RoleLists.Allow);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DeleteMember completed: " + circle.ToString(), GetType().Name);
                }
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            deleteMemberRequest.serviceHandle.Id = messengerService.Id.ToString();   //Always set to 0 ??
            deleteMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = MemberRole.Block;

            CircleMember clMember = new CircleMember();
            clMember.State = MemberState.Accepted;
            clMember.Type = MessengerContactType.Circle;
            clMember.CircleId = circle.AddressBookId.ToString();

            memberShip.Members = new CircleMember[] { clMember };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, DeleteMemberObject, deleteMemberRequest));
        }


        #endregion


        #endregion


    }
};
