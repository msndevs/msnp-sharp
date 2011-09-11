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
using System.Drawing;
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
    using MSNPSharp.MSNWS.MSNDirectoryService;

    partial class ContactService
    {

        private void FindMembershipAsync(string partnerScenario, FindMembershipCompletedEventHandler findMembershipCallback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            bool msdeltasOnly = false;
            DateTime serviceLastChange = WebServiceDateTimeConverter.ConvertToDateTime(WebServiceConstants.ZeroTime);
            DateTime msLastChange = WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.MembershipLastChange);
            string strLastChange = WebServiceConstants.ZeroTime;

            if (msLastChange != serviceLastChange)
            {
                msdeltasOnly = true;
                strLastChange = AddressBook.MembershipLastChange;
            }

            FindMembershipRequestType request = new FindMembershipRequestType();
            request.View = "Full";  // NO default!
            request.deltasOnly = msdeltasOnly;
            request.lastChange = strLastChange;
            request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
            request.serviceFilter.Types = new string[]
            {
                ServiceFilterType.Messenger,
                ServiceFilterType.IMAvailability
                /*,ServiceFilterType.Profile,
                ServiceFilterType.SocialNetwork,
                ServiceFilterType.Invitation,
                ServiceFilterType.Folder,
                ServiceFilterType.OfficeLiveWebNotification*/
            };

            MsnServiceState FindMembershipObject = new MsnServiceState(partnerScenario, "FindMembership", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, FindMembershipObject);
            sharingService.FindMembershipCompleted += delegate(object sender, FindMembershipCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                // Cancelled or signed off
                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (e.Error != null)
                {
                    // Handle errors and recall this method if necesarry.
                    if (e.Error.Message.ToLowerInvariant().Contains("need to do full sync")
                        || e.Error.Message.ToLowerInvariant().Contains("full sync required"))
                    {
                        // recursive Call -----------------------------
                        DeleteRecordFile();
                        FindMembershipAsync(partnerScenario, findMembershipCallback);
                    }
                    else if (e.Error.Message.ToLowerInvariant().Contains("address book does not exist"))
                    {
                        ABAddRequestType abAddRequest = new ABAddRequestType();
                        abAddRequest.abInfo = new abInfoType();
                        abAddRequest.abInfo.ownerEmail = NSMessageHandler.Owner.Account;
                        abAddRequest.abInfo.ownerPuid = "0";
                        abAddRequest.abInfo.fDefault = true;

                        MsnServiceState ABAddObject = new MsnServiceState(partnerScenario, "ABAdd", true);
                        ABServiceBinding abservice = (ABServiceBinding)CreateService(MsnServiceType.AB, ABAddObject);
                        abservice.ABAddCompleted += delegate(object srv, ABAddCompletedEventArgs abadd_e)
                        {
                            OnAfterCompleted(new ServiceOperationEventArgs(abservice, MsnServiceType.AB, abadd_e));

                            if (abadd_e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                                return;

                            if (abadd_e.Error == null)
                            {
                                // recursive Call -----------------------------
                                DeleteRecordFile();
                                FindMembershipAsync(partnerScenario, findMembershipCallback);
                            }
                        };
                        RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abservice, MsnServiceType.AB, ABAddObject, abAddRequest));
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            "UNHANDLED ERROR: " + e.Error.Message.ToString(), GetType().Name);

                        // Pass to the callback
                        if (findMembershipCallback != null)
                        {
                            findMembershipCallback(sharingService, e);
                        }
                    }
                }
                else
                {
                    // No error, fire event handler.
                    if (findMembershipCallback != null)
                    {
                        findMembershipCallback(sharingService, e);
                    }
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, FindMembershipObject, request));
        }


        private void ABFindContactsPagedAsync(string partnerScenario, abHandleType abHandle, ABFindContactsPagedCompletedEventHandler abFindContactsPagedCallback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABFindContactsPaged", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            bool deltasOnly = false;
            ABFindContactsPagedRequestType request = new ABFindContactsPagedRequestType();
            request.abView = "MessengerClient8";  //NO default!

            if (abHandle == null ||
                abHandle.ABId == WebServiceConstants.MessengerIndividualAddressBookId)
            {
                request.extendedContent = "AB AllGroups CircleResult";

                request.filterOptions = new filterOptionsType();
                request.filterOptions.ContactFilter = new ContactFilterType();

                if (DateTime.MinValue != WebServiceDateTimeConverter.ConvertToDateTime(AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId)))
                {
                    deltasOnly = true;
                    request.filterOptions.LastChanged = AddressBook.GetAddressBookLastChange(WebServiceConstants.MessengerIndividualAddressBookId);
                }

                request.filterOptions.DeltasOnly = deltasOnly;
                request.filterOptions.ContactFilter.IncludeHiddenContacts = true;

                // Without these two lines we cannot get the Connect contacts correctly.
                request.filterOptions.ContactFilter.IncludeShellContactsSpecified = true;
                request.filterOptions.ContactFilter.IncludeShellContacts = true;
            }
            else
            {
                request.extendedContent = "AB";
                request.abHandle = abHandle;
            }

            MsnServiceState ABFindContactsPagedObject = new MsnServiceState(partnerScenario, "ABFindContactsPaged", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABFindContactsPagedObject);
            abService.ABFindContactsPagedCompleted += delegate(object sender, ABFindContactsPagedCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                // Cancelled or signed off
                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (e.Error != null)
                {
                    // Handle errors and recall this method if necesarry.

                    if ((e.Error.Message.ToLowerInvariant().Contains("need to do full sync")
                         || e.Error.Message.ToLowerInvariant().Contains("full sync required")))
                    {
                        if (abHandle == null ||
                            abHandle.ABId == WebServiceConstants.MessengerIndividualAddressBookId)
                        {
                            // Default addressbook
                            DeleteRecordFile();
                        }
                        else
                        {
                            // Circle addressbook
                            AddressBook.RemoveCircle(new Guid(abHandle.ABId).ToString("D").ToLowerInvariant(), false);
                        }

                        // recursive Call -----------------------------
                        ABFindContactsPagedAsync(partnerScenario, abHandle, abFindContactsPagedCallback);
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            "UNHANDLED ERROR: " + e.Error.Message.ToString(), GetType().Name);

                        // Pass to the callback
                        if (abFindContactsPagedCallback != null)
                        {
                            abFindContactsPagedCallback(sender, e);
                        }
                    }
                }
                else
                {
                    // No error, fire event handler.
                    if (abFindContactsPagedCallback != null)
                    {
                        abFindContactsPagedCallback(sender, e);
                    }
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABFindContactsPagedObject, request));
        }

        private void CreateContactAsync(string account, IMAddressInfoType network, Guid abId,
            CreateContactCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("CreateContact", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            CreateContactType request = new CreateContactType();

            if (network == IMAddressInfoType.Telephone)
            {
                contactInfoType contactInfo = new contactInfoType();

                contactPhoneType cpt = new contactPhoneType();
                cpt.contactPhoneType1 = ContactPhoneTypes.ContactPhoneMobile;
                cpt.number = account;

                contactInfo.phones = new contactPhoneType[] { cpt };
                request.contactInfo = contactInfo;
            }
            else
            {
                contactHandleType contactHandle = new contactHandleType();
                contactHandle.Email = account;
                contactHandle.Cid = 0;
                contactHandle.Puid = 0;
                contactHandle.CircleId = WebServiceConstants.MessengerIndividualAddressBookId;
                request.contactHandle = contactHandle;
            }

            if (abId != Guid.Empty)
            {
                abHandleType abHandle = new abHandleType();
                abHandle.ABId = abId.ToString("D").ToLowerInvariant();
                abHandle.Puid = 0;
                abHandle.Cid = 0;

                request.abHandle = abHandle;
            }

            MsnServiceState createContactObject = new MsnServiceState(abId == Guid.Empty ? PartnerScenario.ContactSave : PartnerScenario.CircleSave, "CreateContact", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, createContactObject);
            abService.CreateContactCompleted += delegate(object service, CreateContactCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, createContactObject, request));
        }

        private void UpdateContactAsync(ContactType contact, string abId, ABContactUpdateCompletedEventHandler callback)
        {
            ABContactUpdateRequestType request = new ABContactUpdateRequestType();
            request.abId = abId;
            request.contacts = new ContactType[] { contact };
            request.options = new ABContactUpdateRequestTypeOptions();
            request.options.EnableAllowListManagementSpecified = true;
            request.options.EnableAllowListManagement = true;

            MsnServiceState ABContactUpdateObject = new MsnServiceState(contact.contactInfo.isMessengerUser ? PartnerScenario.ContactSave : PartnerScenario.Timer, "ABContactUpdate", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABContactUpdateObject);
            abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABContactUpdateObject, request));
        }


        private void BreakConnectionAsync(Guid contactGuid, Guid abID, bool block, bool delete,
            BreakConnectionCompletedEventHandler callback)
        {
            BreakConnectionRequestType breakconnRequest = new BreakConnectionRequestType();
            breakconnRequest.contactId = contactGuid.ToString("D");
            breakconnRequest.blockContact = block;
            breakconnRequest.deleteContact = delete;

            if (abID != Guid.Empty)
            {
                abHandleType handler = new abHandleType();
                handler.ABId = abID.ToString("D");
                handler.Cid = 0;
                handler.Puid = 0;

                breakconnRequest.abHandle = handler;
            }

            MsnServiceState breakConnectionObject = new MsnServiceState(PartnerScenario.BlockUnblock, "BreakConnection", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, breakConnectionObject);
            abService.BreakConnectionCompleted += delegate(object sender, BreakConnectionCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(sender, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, breakConnectionObject, breakconnRequest));
        }

        private void ManageWLConnectionAsync(Guid contactGuid, Guid abID, string inviteMessage,
           bool connection, bool presence, int action, int relType, int relRole,
           ManageWLConnectionCompletedEventHandler callback)
        {
            ManageWLConnectionRequestType wlconnectionRequest = new ManageWLConnectionRequestType();

            wlconnectionRequest.contactId = contactGuid.ToString("D");
            wlconnectionRequest.connection = connection;
            wlconnectionRequest.presence = presence;
            wlconnectionRequest.action = action;

            wlconnectionRequest.relationshipType = relType;
            wlconnectionRequest.relationshipRole = relRole;

            if (!String.IsNullOrEmpty(inviteMessage))
            {
                Annotation anno = new Annotation();
                anno.Name = AnnotationNames.MSN_IM_InviteMessage;
                anno.Value = inviteMessage;

                wlconnectionRequest.annotations = new Annotation[] { anno };
            }

            if (abID != Guid.Empty)
            {
                abHandleType abHandle = new abHandleType();
                abHandle.ABId = abID.ToString("D").ToLowerInvariant();
                abHandle.Puid = 0;
                abHandle.Cid = 0;

                wlconnectionRequest.abHandle = abHandle;
            }

            MsnServiceState manageWLConnectionObject = new MsnServiceState(abID == Guid.Empty ? PartnerScenario.ContactSave : PartnerScenario.CircleInvite, "ManageWLConnection", true);
            ABServiceBinding abServiceBinding = (ABServiceBinding)CreateService(MsnServiceType.AB, manageWLConnectionObject);
            abServiceBinding.ManageWLConnectionCompleted += delegate(object wlcSender, ManageWLConnectionCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abServiceBinding, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(wlcSender, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abServiceBinding, MsnServiceType.AB, manageWLConnectionObject, wlconnectionRequest));
        }


        private void ABGroupAddAsync(string groupName, ABGroupAddCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABGroupAddRequestType request = new ABGroupAddRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupAddOptions = new ABGroupAddRequestTypeGroupAddOptions();
            request.groupAddOptions.fRenameOnMsgrConflict = false;
            request.groupAddOptions.fRenameOnMsgrConflictSpecified = true;
            request.groupInfo = new ABGroupAddRequestTypeGroupInfo();
            request.groupInfo.GroupInfo = new groupInfoType();
            request.groupInfo.GroupInfo.name = groupName;
            request.groupInfo.GroupInfo.fMessenger = false;
            request.groupInfo.GroupInfo.fMessengerSpecified = true;
            request.groupInfo.GroupInfo.groupType = WebServiceConstants.MessengerGroupType;
            request.groupInfo.GroupInfo.annotations = new Annotation[] { new Annotation() };
            request.groupInfo.GroupInfo.annotations[0].Name = AnnotationNames.MSN_IM_Display;
            request.groupInfo.GroupInfo.annotations[0].Value = "1";

            MsnServiceState ABGroupAddObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupAdd", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupAddObject);
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupAddObject, request));
        }


        private void ABGroupUpdateAsync(ContactGroup group, string newGroupName, ABGroupUpdateCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupUpdate", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABGroupUpdateRequestType request = new ABGroupUpdateRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groups = new GroupType[1] { new GroupType() };
            request.groups[0].groupId = group.Guid;
            request.groups[0].propertiesChanged = PropertyString.GroupName; //"GroupName";
            request.groups[0].groupInfo = new groupInfoType();
            request.groups[0].groupInfo.name = newGroupName;

            MsnServiceState ABGroupUpdateObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupUpdate", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupUpdateObject);
            abService.ABGroupUpdateCompleted += delegate(object service, ABGroupUpdateCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupUpdateObject, request));
        }


        private void ABGroupDeleteAsync(ContactGroup contactGroup, ABGroupDeleteCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABGroupDeleteRequestType request = new ABGroupDeleteRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { contactGroup.Guid };

            MsnServiceState ABGroupDeleteObject = new MsnServiceState(PartnerScenario.Timer, "ABGroupDelete", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupDeleteObject);
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupDeleteObject, request));
        }

        private void ABGroupContactAddAsync(Contact contact, ContactGroup group, ABGroupContactAddCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABGroupContactAddRequestType request = new ABGroupContactAddRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            MsnServiceState ABGroupContactAddObject = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupContactAdd", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupContactAddObject);
            abService.ABGroupContactAddCompleted += delegate(object service, ABGroupContactAddCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupContactAddObject, request));
        }

        private void ABGroupContactDeleteAsync(Contact contact, ContactGroup group, ABGroupContactDeleteCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABGroupContactDeleteRequestType request = new ABGroupContactDeleteRequestType();
            request.abId = WebServiceConstants.MessengerIndividualAddressBookId;
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            MsnServiceState ABGroupContactDelete = new MsnServiceState(PartnerScenario.GroupSave, "ABGroupContactDelete", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, ABGroupContactDelete);
            abService.ABGroupContactDeleteCompleted += delegate(object service, ABGroupContactDeleteCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, ABGroupContactDelete, request));
        }


        private void AddMemberAsync(Contact contact, RoleLists list, AddMemberCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            addMemberRequest.serviceHandle.Id = messengerService.Id.ToString();
            addMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);
            BaseMember member = new BaseMember();

            if (contact.ClientType == IMAddressInfoType.WindowsLive)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Account;
                passportMember.State = MemberState.Accepted;
                passportMember.Type = MembershipType.Passport;
            }
            else if (contact.ClientType == IMAddressInfoType.Yahoo)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.State = MemberState.Accepted;
                emailMember.Type = MembershipType.Email;
                emailMember.Email = contact.Account;
                emailMember.Annotations = new Annotation[] { new Annotation() };
                emailMember.Annotations[0].Name = AnnotationNames.MSN_IM_BuddyType;
                emailMember.Annotations[0].Value = "32:";
            }
            else if (contact.ClientType == IMAddressInfoType.Telephone)
            {
                member = new PhoneMember();
                PhoneMember phoneMember = member as PhoneMember;
                phoneMember.State = MemberState.Accepted;
                phoneMember.Type = MembershipType.Phone;
                phoneMember.PhoneNumber = contact.Account;
            }
            else if (contact.ClientType == IMAddressInfoType.Circle)
            {
                member = new CircleMember();
                CircleMember circleMember = member as CircleMember;
                circleMember.Type = MembershipType.Circle;
                circleMember.State = MemberState.Accepted;
                circleMember.CircleId = contact.AddressBookId.ToString("D").ToLowerInvariant();
            }

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            MsnServiceState AddMemberObject = new MsnServiceState((list == RoleLists.Reverse) ? PartnerScenario.ContactMsgrAPI : PartnerScenario.BlockUnblock, "AddMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, AddMemberObject);
            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                // Update AB
                AddressBook.AddMemberhip(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list), member, Scenario.ContactServeAPI);

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, AddMemberObject, addMemberRequest));
        }



        private void DeleteMemberAsync(Contact contact, RoleLists list, DeleteMemberCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.SelectTargetService(ServiceFilterType.Messenger);
            deleteMemberRequest.serviceHandle.Id = messengerService.Id.ToString();   //Always set to 0 ??
            deleteMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);

            BaseMember deleteMember = null; // BaseMember is an abstract type, so we cannot create a new instance.
            // If we have a MembershipId different from 0, just use it. Otherwise, use email or phone number. 
            BaseMember baseMember = AddressBook.SelectBaseMember(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list));
            int membershipId = (baseMember == null || String.IsNullOrEmpty(baseMember.MembershipId)) ? 0 : int.Parse(baseMember.MembershipId);

            switch (contact.ClientType)
            {
                case IMAddressInfoType.WindowsLive:

                    deleteMember = new PassportMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Passport : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PassportMember).PassportName = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Yahoo:

                    deleteMember = new EmailMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Email : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as EmailMember).Email = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Telephone:

                    deleteMember = new PhoneMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Phone : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PhoneMember).PhoneNumber = contact.Account;
                    }
                    break;

                case IMAddressInfoType.Circle:
                    deleteMember = new CircleMember();
                    deleteMember.Type = (baseMember == null) ? MembershipType.Circle : baseMember.Type;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    (deleteMember as CircleMember).CircleId = contact.AddressBookId.ToString("D").ToLowerInvariant();
                    break;
            }

            deleteMember.MembershipId = membershipId.ToString();

            memberShip.Members = new BaseMember[] { deleteMember };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            MsnServiceState DeleteMemberObject = new MsnServiceState((list == RoleLists.Pending) ? PartnerScenario.ContactMsgrAPI : PartnerScenario.BlockUnblock, "DeleteMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, DeleteMemberObject);
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                // Update AB
                AddressBook.RemoveMemberhip(ServiceFilterType.Messenger, contact.Account, contact.ClientType, GetMemberRole(list), Scenario.ContactServeAPI);

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, DeleteMemberObject, deleteMemberRequest));
        }



        private void CreateCircleAsync(string circleName, CreateCircleCompletedEventHandler callback)
        {
            string addressBookId = string.Empty;

            //This is M$ style, you will never guess out the meaning of these numbers.
            ContentInfoType properties = new ContentInfoType();
            properties.Domain = 1;
            properties.HostedDomain = Contact.DefaultHostDomain;
            properties.Type = 2;
            properties.MembershipAccess = 0;
            properties.IsPresenceEnabled = true;
            properties.RequestMembershipOption = 2;
            properties.DisplayName = circleName;

            CreateCircleRequestType request = new CreateCircleRequestType();
            request.properties = properties;
            request.callerInfo = new callerInfoType();
            request.callerInfo.PublicDisplayName = NSMessageHandler.Owner.Name == string.Empty ? NSMessageHandler.Owner.Account : NSMessageHandler.Owner.Name;

            MsnServiceState createCircleObject = new MsnServiceState(PartnerScenario.CircleSave, "CreateCircle", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, createCircleObject);
            sharingService.CreateCircleCompleted += delegate(object sender, CreateCircleCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(sender, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, createCircleObject, request));
        }

        private void AddServiceAsync(string serviceName, AddServiceCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddService", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                AddServiceRequestType r = new AddServiceRequestType();
                r.serviceInfo = new InfoType();
                r.serviceInfo.Handle = new HandleType();
                r.serviceInfo.Handle.Type = serviceName;
                r.serviceInfo.Handle.ForeignId = String.Empty;
                r.serviceInfo.InverseRequired = true;

                MsnServiceState addServiceObject = new MsnServiceState(PartnerScenario.CircleSave, "AddService", true);
                SharingServiceBinding abService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, addServiceObject);
                abService.AddServiceCompleted += delegate(object service, AddServiceCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.Sharing, e));

                    if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                        serviceName + "=" + e.Result.AddServiceResult + " created...");

                    // Update service membership...
                    msRequest(PartnerScenario.BlockUnblock,
                        delegate
                        {
                            if (callback != null)
                                callback(abService, e);
                        });
                };

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.Sharing, addServiceObject, r));
            }
        }

    }
};
