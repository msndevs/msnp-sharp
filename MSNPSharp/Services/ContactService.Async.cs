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

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    // No error, creates a new contact or returns existing.
                    if (callback != null)
                    {
                        callback(service, e);
                    }
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abService, MsnServiceType.AB, createContactObject, request));
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

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (callback != null)
                    {
                        callback(sender, e);
                    }
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

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled)
                {
                    if (callback != null)
                    {
                        callback(wlcSender, e);
                    }
                }
            };

            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(abServiceBinding, MsnServiceType.AB, manageWLConnectionObject, wlconnectionRequest));
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

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
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
