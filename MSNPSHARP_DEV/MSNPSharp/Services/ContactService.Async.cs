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
            request.view = "Full";  // NO default!
            request.deltasOnly = msdeltasOnly;
            request.lastChange = strLastChange;
            request.serviceFilter = new ServiceFilter();
            request.serviceFilter.Types = new ServiceName[]
            {
                ServiceName.Messenger,
                ServiceName.IMAvailability,
                ServiceName.SocialNetwork
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
                        DeleteRecordFile(true);
                        FindMembershipAsync(partnerScenario, findMembershipCallback);
                    }
                    else if (e.Error.Message.ToLowerInvariant().Contains("address book does not exist"))
                    {
                        ABAddRequestType abAddRequest = new ABAddRequestType();
                        abAddRequest.abInfo = new abInfoType();
                        abAddRequest.abInfo.ownerEmail = NSMessageHandler.Owner.Account;
                        abAddRequest.abInfo.ownerPuid = 0;
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
                                DeleteRecordFile(true);
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
                            DeleteRecordFile(true);
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

        private void FindFriendsInCommonAsync(Guid abId, long cid, int count,
            FindFriendsInCommonCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindFriendsInCommon", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (cid == NSMessageHandler.Owner.CID)
                return;

            abHandleType abHandle = new abHandleType();
            abHandle.Puid = 0;

            if (abId != Guid.Empty)
            {
                // Find in circle
                abHandle.ABId = abId.ToString("D").ToLowerInvariant();
            }
            else if (cid != 0)
            {
                // Find by CID
                abHandle.Cid = cid;
            }

            FindFriendsInCommonRequestType request = new FindFriendsInCommonRequestType();
            request.domainID = DomainIds.WindowsLiveDomain;
            request.maxResults = count;
            request.options = "List Count Matched Unmatched "; // IncludeInfo

            request.targetAB = abHandle;

            MsnServiceState findFriendsInCommonObject = new MsnServiceState(PartnerScenario.Timer, "FindFriendsInCommon", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, findFriendsInCommonObject);
            abService.FindFriendsInCommonCompleted += delegate(object service, FindFriendsInCommonCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, findFriendsInCommonObject, request));
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

        private void DeleteContactAsync(Contact contact,
            DeleteContactCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteContact", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            DeleteContactRequestType request = new DeleteContactRequestType();
            request.contactId = contact.Guid.ToString("D").ToLowerInvariant();

            MsnServiceState deleteContactObject = new MsnServiceState(PartnerScenario.Timer, "DeleteContact", true);
            ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, deleteContactObject);
            abService.DeleteContactCompleted += delegate(object service, DeleteContactCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(abService, MsnServiceType.AB, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (callback != null)
                {
                    callback(service, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, deleteContactObject, request));
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


        private void AddMemberAsync(Contact contact, ServiceName serviceName, RoleLists list, AddMemberCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            // check whether the update is necessary
            if (contact.HasLists(list))
                return;

            RoleId memberRole = ContactList.GetMemberRole(list);

            if (memberRole == RoleId.None)
                return;

            Service service = AddressBook.SelectTargetService(serviceName);

            if (service == null)
            {
                AddServiceAsync(serviceName,
                    delegate
                    {
                        // RESURSIVE CALL
                        AddMemberAsync(contact, serviceName, list, callback);
                    });
                return;
            }


            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();
            addMemberRequest.serviceHandle.Id = service.Id.ToString();
            addMemberRequest.serviceHandle.Type = serviceName;

            Membership memberShip = new Membership();
            memberShip.MemberRole = memberRole;
            BaseMember member = null; // Abstract

            if (contact.ClientType == IMAddressInfoType.WindowsLive)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Account;
                passportMember.State = MemberState.Accepted;
                passportMember.Type = MembershipType.Passport;
            }
            else if (contact.ClientType == IMAddressInfoType.Yahoo ||
                contact.ClientType == IMAddressInfoType.OfficeCommunicator)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.State = MemberState.Accepted;
                emailMember.Type = MembershipType.Email;
                emailMember.Email = contact.Account;
                emailMember.Annotations = new Annotation[] { new Annotation() };
                emailMember.Annotations[0].Name = AnnotationNames.MSN_IM_BuddyType;
                emailMember.Annotations[0].Value = (contact.ClientType == IMAddressInfoType.Yahoo) ?
                    "32:" : "02:";
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

            if (member == null)
                return;

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            MsnServiceState AddMemberObject = new MsnServiceState(PartnerScenario.ContactMsgrAPI, "AddMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, AddMemberObject);
            sharingService.AddMemberCompleted += delegate(object srv, AddMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                // Update AB
                AddressBook.AddMemberhip(serviceName, contact.Account, contact.ClientType, memberRole, member);

                if (callback != null)
                {
                    callback(srv, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, AddMemberObject, addMemberRequest));
        }



        private void DeleteMemberAsync(Contact contact, ServiceName serviceName, RoleLists list, DeleteMemberCompletedEventHandler callback)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            // check whether the update is necessary
            if (!contact.HasLists(list))
                return;

            RoleId memberRole = ContactList.GetMemberRole(list);
            if (memberRole == RoleId.None)
                return;

            Service service = AddressBook.SelectTargetService(serviceName);

            if (service == null)
            {
                AddServiceAsync(serviceName,
                    delegate
                    {
                        // RESURSIVE CALL
                        DeleteMemberAsync(contact, serviceName, list, callback);
                    });
                return;
            }


            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();
            deleteMemberRequest.serviceHandle.Id = service.Id.ToString();
            deleteMemberRequest.serviceHandle.Type = serviceName;

            Membership memberShip = new Membership();
            memberShip.MemberRole = memberRole;

            BaseMember deleteMember = null; // BaseMember is an abstract type, so we cannot create a new instance.
            // If we have a MembershipId different from 0, just use it. Otherwise, use email or phone number. 
            BaseMember baseMember = AddressBook.SelectBaseMember(serviceName, contact.Account, contact.ClientType, memberRole);
            int membershipId = (baseMember == null) ? 0 : baseMember.MembershipId;

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
                    else
                    {
                        deleteMember.MembershipId = membershipId;
                        deleteMember.MembershipIdSpecified = true;
                    }
                    break;

                case IMAddressInfoType.Yahoo:
                case IMAddressInfoType.OfficeCommunicator:

                    deleteMember = new EmailMember();
                    deleteMember.Type = MembershipType.Email;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as EmailMember).Email = contact.Account;
                    }
                    else
                    {
                        deleteMember.MembershipId = membershipId;
                        deleteMember.MembershipIdSpecified = true;
                    }
                    break;

                case IMAddressInfoType.Telephone:

                    deleteMember = new PhoneMember();
                    deleteMember.Type = MembershipType.Phone;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    if (membershipId == 0)
                    {
                        (deleteMember as PhoneMember).PhoneNumber = contact.Account;
                    }
                    else
                    {
                        deleteMember.MembershipId = membershipId;
                        deleteMember.MembershipIdSpecified = true;
                    }
                    break;

                case IMAddressInfoType.Circle:
                    deleteMember = new CircleMember();
                    deleteMember.Type = MembershipType.Circle;
                    deleteMember.State = (baseMember == null) ? MemberState.Accepted : baseMember.State;
                    (deleteMember as CircleMember).CircleId = contact.AddressBookId.ToString("D").ToLowerInvariant();
                    break;
            }            

            memberShip.Members = new BaseMember[] { deleteMember };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            MsnServiceState DeleteMemberObject = new MsnServiceState(PartnerScenario.ContactMsgrAPI, "DeleteMember", true);
            SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, DeleteMemberObject);
            sharingService.DeleteMemberCompleted += delegate(object srv, DeleteMemberCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(sharingService, MsnServiceType.Sharing, e));

                if (e.Cancelled || NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                // Update AB
                AddressBook.RemoveMemberhip(serviceName, contact.Account, contact.ClientType, memberRole);

                if (callback != null)
                {
                    callback(srv, e);
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(sharingService, MsnServiceType.Sharing, DeleteMemberObject, deleteMemberRequest));
        }



        private void CreateCircleAsync(string circleName, CreateCircleCompletedEventHandler callback)
        {
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

        private void AddServiceAsync(ServiceName serviceName, AddServiceCompletedEventHandler callback)
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

                    if (e.Error == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            serviceName + "=" + e.Result.AddServiceResult + " created...");

                        // Update service membership...
                        msRequest(PartnerScenario.BlockUnblock,
                            delegate
                            {
                                if (callback != null)
                                    callback(abService, e);
                            });
                    }
                };

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.Sharing, addServiceObject, r));
            }
        }

    }
};
