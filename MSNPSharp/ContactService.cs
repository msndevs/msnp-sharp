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
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Services.Protocols;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// Provide webservice operations for contacts. This class cannot be inherited.
    /// </summary>
    public sealed class ContactService : MSNService
    {
        #region Fields

        private int recursiveCall;
        private string applicationId = String.Empty;
        private List<int> initialADLs = new List<int>();
        private bool abSynchronized;

        internal int initialADLcount;
        internal XMLContactList AddressBook;
        internal DeltasList Deltas;

        #endregion

        public ContactService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
#if MSNP18
            applicationId = nsHandler.Credentials.ClientInfo.ApplicationId;
#else
            applicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";  //This must be strictly matched now.
#endif
        }

        #region Events
        /// <summary>
        /// Occurs when a contact is added to any list (including reverse list)
        /// </summary>
        public event EventHandler<ListMutateEventArgs> ContactAdded;

        /// <summary>
        /// Occurs when a contact is removed from any list (including reverse list)
        /// </summary>
        public event EventHandler<ListMutateEventArgs> ContactRemoved;

        /// <summary>
        /// Occurs when another user adds us to their contactlist. A ContactAdded event with the reverse list as parameter will also be raised.
        /// </summary>
        public event EventHandler<ContactEventArgs> ReverseAdded;

        /// <summary>
        /// Occurs when another user removes us from their contactlist. A ContactRemoved event with the reverse list as parameter will also be raised.
        /// </summary>
        public event EventHandler<ContactEventArgs> ReverseRemoved;

        /// <summary>
        /// Occurs when a new contactgroup is created
        /// </summary>
        public event EventHandler<ContactGroupEventArgs> ContactGroupAdded;

        /// <summary>
        /// Occurs when a contactgroup is removed
        /// </summary>
        public event EventHandler<ContactGroupEventArgs> ContactGroupRemoved;

        /// <summary>
        /// Occurs when a call to SynchronizeList() has been made and the synchronization process is completed.
        /// This means all contact-updates are received from the server and processed.
        /// </summary>
        public event EventHandler<EventArgs> SynchronizationCompleted;
        #endregion

        #region Public members

        /// <summary>
        /// Fires the <see cref="ReverseRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnReverseRemoved(ContactEventArgs e)
        {
            if (ReverseRemoved != null)
                ReverseRemoved(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ReverseAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnReverseAdded(ContactEventArgs e)
        {
            if (ReverseAdded != null)
                ReverseAdded(this, e);
        }

        /// <summary>
        /// Fires the <see cref="ContactAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactAdded(ListMutateEventArgs e)
        {
            if (ContactAdded != null)
            {
                ContactAdded(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactRemoved(ListMutateEventArgs e)
        {
            if (ContactRemoved != null)
            {
                ContactRemoved(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactGroupAdded"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactGroupAdded(ContactGroupEventArgs e)
        {
            if (ContactGroupAdded != null)
            {
                ContactGroupAdded(this, e);
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactGroupRemoved"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnContactGroupRemoved(ContactGroupEventArgs e)
        {
            if (ContactGroupRemoved != null)
            {
                ContactGroupRemoved(this, e);
            }
        }


        /// <summary>
        /// Fires the <see cref="SynchronizationCompleted"/> event.
        /// </summary>
        /// <param name="e"></param>
        internal void OnSynchronizationCompleted(EventArgs e)
        {
            if (SynchronizationCompleted != null)
                SynchronizationCompleted(this, e);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Preferred host of the contact service. The default is "contacts.msn.com".
        /// </summary>
        internal SerializableDictionary<string, string> PreferredHosts
        {
            get
            {
                if (Deltas == null)
                    return null;

                return Deltas.PreferredHosts;
            }
            set
            {
                if (Deltas != null)
                {
                    Deltas.PreferredHosts = value;
                }
            }
        }

        /// <summary>
        /// Keep track whether a address book synchronization has been completed.
        /// </summary>
        public bool AddressBookSynchronized
        {
            get
            {
                return abSynchronized;
            }
        }

        #endregion

        #region Synchronize

        /// <summary>
        /// Rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// This method is called automatically after owner profile received and then the addressbook is merged with deltas file.
        /// After that, SignedIn event occurs and the client programmer must set it's initial status by SetPresenceStatus(). 
        /// Otherwise you won't receive online notifications from other clients or the connection is closed by the server.
        /// If you have an external contact list, you must track ProfileReceived, SignedIn and SynchronizationCompleted events.
        /// Between ProfileReceived and SignedIn: the internal addressbook is merged with deltas file.
        /// Between SignedIn and SynchronizationCompleted: the internal addressbook is merged with most recent data by soap request.
        /// All contact changes will be fired between ProfileReceived, SignedIn and SynchronizationCompleted events. 
        /// e.g: ContactAdded, ContactRemoved, ReverseAdded, ReverseRemoved.
        /// </remarks>
        internal void SynchronizeContactList()
        {
            if (AddressBookSynchronized)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "SynchronizeContactList() was called, but the list has already been synchronized.", GetType().Name);
                return;
            }

            if (recursiveCall != 0 || NSMessageHandler.AutoSynchronize == false)
            {
                DeleteRecordFile();
            }

            bool nocompress = Settings.NoCompress;
            string addressbookFile = Path.Combine(Settings.SavePath, NSMessageHandler.Owner.Mail.GetHashCode() + ".mcl");
            string deltasResultsFile = Path.Combine(Settings.SavePath, NSMessageHandler.Owner.Mail.GetHashCode() + "d" + ".mcl");

            try
            {
                AddressBook = XMLContactList.LoadFromFile(addressbookFile, nocompress, NSMessageHandler);
                Deltas = DeltasList.LoadFromFile(deltasResultsFile, nocompress, NSMessageHandler);

                NSMessageHandler.MSNTicket.CacheKeys = Deltas.CacheKeys;

                if (NSMessageHandler.AutoSynchronize &&
                    recursiveCall == 0 &&
                    (AddressBook.Version != Properties.Resources.XMLContactListVersion || Deltas.Version != Properties.Resources.DeltasListVersion))
                {
                    recursiveCall++;
                    SynchronizeContactList();
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);

                recursiveCall++;
                SynchronizeContactList();
                return;
            }

            if (NSMessageHandler.AutoSynchronize)
            {
                AddressBook.Synchronize(Deltas);
                if (AddressBook.AddressbookLastChange == DateTime.MinValue)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Getting your membership list for the first time. If you have a lot of contacts, please be patient!", GetType().Name);
                }
                msRequest(
                    "Initial",
                    delegate
                    {
                        if (AddressBook.AddressbookLastChange == DateTime.MinValue)
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Getting your address book for the first time. If you have a lot of contacts, please be patient!", GetType().Name);
                        }
                        abRequest("Initial",
                            delegate
                            {
                                SetDefaults();
                            }
                        );
                    }
                );
            }
            else
            {
                // Set lastchanged and roaming profile last change to get display picture and personal message
                NSMessageHandler.ContactService.AddressBook.MyProperties["lastchanged"] = XmlConvert.ToString(DateTime.MaxValue, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");
                NSMessageHandler.Owner.SetRoamLiveProperty(RoamLiveProperty.Enabled);
                SetDefaults();
                NSMessageHandler.OnSignedIn(EventArgs.Empty);
            }
        }

        private void SetDefaults()
        {
            // Reset
            recursiveCall = 0;

            if (NSMessageHandler.AutoSynchronize)
            {
                // Set privacy settings and roam property
                NSMessageHandler.Owner.SetPrivacy((AddressBook.MyProperties["blp"] == "1") ? PrivacyMode.AllExceptBlocked : PrivacyMode.NoneButAllowed);
                NSMessageHandler.Owner.SetNotifyPrivacy((AddressBook.MyProperties["gtc"] == "1") ? NotifyPrivacy.PromptOnAdd : NotifyPrivacy.AutomaticAdd);
                NSMessageHandler.Owner.SetRoamLiveProperty((AddressBook.MyProperties["roamliveproperties"] == "1") ? RoamLiveProperty.Enabled : RoamLiveProperty.Disabled);
                NSMessageHandler.Owner.SetMPOP((AddressBook.MyProperties["mpop"] == "1") ? MPOP.KeepOnline : MPOP.AutoLogoff);
            }

            Deltas.Profile = NSMessageHandler.StorageService.GetProfile();

            // Set display name, personal status and photo
            string mydispName = String.IsNullOrEmpty(Deltas.Profile.DisplayName) ? NSMessageHandler.Owner.NickName : Deltas.Profile.DisplayName;
            PersonalMessage pm = new PersonalMessage(Deltas.Profile.PersonalMessage, MediaType.None, null, NSMessageHandler.MachineGuid);

            NSMessageHandler.Owner.SetName(mydispName);
            NSMessageHandler.Owner.SetPersonalMessage(pm);
            NSMessageHandler.Owner.CreateDefaultDisplayImage(Deltas.Profile.Photo.DisplayImage);

            if (NSMessageHandler.AutoSynchronize)
            {
                // Send BLP
                NSMessageHandler.SetPrivacyMode(NSMessageHandler.Owner.Privacy);

                // Send Initial ADL
                Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(NSMessageHandler.ContactList.Count);
                lock (NSMessageHandler.ContactList.SyncRoot)
                {
                    foreach (Contact contact in NSMessageHandler.ContactList.All)
                    {
                        string ch = contact.Hash;
                        MSNLists l = MSNLists.None;
                        if (contact.IsMessengerUser)
                            l |= MSNLists.ForwardList;
                        if (contact.OnAllowedList)
                            l |= MSNLists.AllowedList;
                        else if (contact.OnBlockedList)
                            l |= MSNLists.BlockedList;

                        if (l != MSNLists.None && !hashlist.ContainsKey(ch))
                            hashlist.Add(ch, l);
                    }
                }
                string[] adls = ConstructLists(hashlist, true);
                initialADLcount = adls.Length;
                foreach (string payload in adls)
                {
                    NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                    NSMessageHandler.MessageProcessor.SendMessage(message);
                    initialADLs.Add(message.TransactionID);
                }
            }

            // Set screen name
            NSMessageHandler.SetScreenName(NSMessageHandler.Owner.Name);

            // Save addressbook and then truncate deltas file.
            AddressBook.Save();
            Deltas.Truncate();
        }

        internal bool ProcessADL(int transid)
        {
            if (initialADLs.Contains(transid))
            {
                initialADLs.Remove(transid);
                if (--initialADLcount <= 0)
                {
                    initialADLcount = 0;

                    if (NSMessageHandler.AutoSynchronize)
                    {
                        NSMessageHandler.OnSignedIn(EventArgs.Empty);
                        NSMessageHandler.SetPersonalMessage(NSMessageHandler.Owner.PersonalMessage);
                    }

                    if (!AddressBookSynchronized)
                    {
                        abSynchronized = true;
                        OnSynchronizationCompleted(EventArgs.Empty);

                        if (NSMessageHandler.AutoSynchronize)
                        {
                            lock (NSMessageHandler.ContactList.SyncRoot)
                            {
                                foreach (Contact contact in NSMessageHandler.ContactList.All)
                                {
                                    if (contact.OnPendingList || (contact.OnReverseList && !contact.OnAllowedList && !contact.OnBlockedList))
                                    {
                                        NSMessageHandler.ContactService.OnReverseAdded(new ContactEventArgs(contact));
                                    }
                                }

                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Async membership request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async membership request completed successfuly</param>
        internal void msRequest(string partnerScenario, FindMembershipCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                bool msdeltasOnly = false;
                DateTime serviceLastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);
                DateTime msLastChange = AddressBook.MembershipLastChange;
                if (msLastChange != serviceLastChange)
                {
                    msdeltasOnly = true;
                    serviceLastChange = msLastChange;
                }

                SharingServiceBinding sharingService = CreateSharingService(partnerScenario);
                FindMembershipRequestType request = new FindMembershipRequestType();
                request.View = "Full";  // NO default!
                request.deltasOnly = msdeltasOnly;
                request.lastChange = serviceLastChange;
                request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
                request.serviceFilter.Types = new string[]
                {
                    ServiceFilterType.Messenger,
                    ServiceFilterType.Invitation,
                    ServiceFilterType.SocialNetwork,
                    ServiceFilterType.Space,
                    ServiceFilterType.Profile,
                    ServiceFilterType.Folder,
                    ServiceFilterType.OfficeLiveWebNotification
                };

                sharingService.FindMembershipCompleted += delegate(object sender, FindMembershipCompletedEventArgs e)
                {
                    sharingService = sender as SharingServiceBinding;
                    handleServiceHeader(sharingService.ServiceHeaderValue, typeof(FindMembershipRequestType));

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if (e.Error.Message.Contains("Address Book Does Not Exist"))
                            {
                                if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
                                {
                                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("You don't have access right on this action anymore.")));
                                }
                                else
                                {
                                    ABServiceBinding abservice = CreateABService(partnerScenario);
                                    abservice.ABAddCompleted += delegate(object srv, ABAddCompletedEventArgs abadd_e)
                                    {
                                        if (abadd_e.Error == null)
                                        {
                                            recursiveCall++;
                                            SynchronizeContactList();
                                        }
                                        else
                                        {
                                            OnServiceOperationFailed(sender, new ServiceOperationFailedEventArgs("SynchronizeContactList", abadd_e.Error));
                                        }
                                    };
                                    ABAddRequestType abAddRequest = new ABAddRequestType();
                                    abAddRequest.abInfo = new abInfoType();
                                    abAddRequest.abInfo.ownerEmail = NSMessageHandler.Owner.Mail;
                                    abAddRequest.abInfo.ownerPuid = "0";
                                    abAddRequest.abInfo.fDefault = true;

                                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abservice, "ABAdd", abAddRequest);
                                    abservice.ABAddAsync(abAddRequest, new object());
                                }
                            }
                            else if ((recursiveCall == 0 && partnerScenario == "Initial")
                                || (e.Error.Message.Contains("Need to do full sync")))
                            {
                                recursiveCall++;
                                SynchronizeContactList();
                            }
                            else
                            {
                                OnServiceOperationFailed(sender, new ServiceOperationFailedEventArgs("SynchronizeContactList", e.Error));
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.ToString(), GetType().Name);
                            }
                        }
                        else
                        {
                            if (null != e.Result.FindMembershipResult)
                            {
                                AddressBook += e.Result.FindMembershipResult;
                                Deltas.MembershipDeltas.Add(e.Result.FindMembershipResult);
                                Deltas.Save();
                            }
                            if (onSuccess != null)
                            {
                                onSuccess(sharingService, e);
                            }
                        }
                    }
                };

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, "FindMembership", request);
                sharingService.FindMembershipAsync(request, partnerScenario);
            }
        }

        /// <summary>
        /// Async Address book request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async ab request completed successfuly</param>
#if MSNP18
        internal void abRequest(string partnerScenario, ABFindContactsPagedCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABFindContactsPaged", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                bool deltasOnly = false;

                ABServiceBinding abService = CreateABService(partnerScenario);
                ABFindContactsPagedRequestType request = new ABFindContactsPagedRequestType();
                request.abView = "MessengerClient8";  //NO default!
                request.extendedContent = "AB AllGroups CircleResult";

                request.filterOptions = new filterOptionsType();
                request.filterOptions.ContactFilter = new ContactFilterType();

                if (AddressBook.AddressbookLastChange != DateTime.MinValue)
                {
                    deltasOnly = true;
                    request.filterOptions.LastChanged = AddressBook.AddressbookLastChange;
                    request.filterOptions.LastChangedSpecified = true;
                }

                request.filterOptions.DeltasOnly = deltasOnly;
                request.filterOptions.ContactFilter.IncludeHiddenContacts = true;

                abService.ABFindContactsPagedCompleted += delegate(object sender, ABFindContactsPagedCompletedEventArgs e)
                {
                    abService = sender as ABServiceBinding;
                    handleServiceHeader(abService.ServiceHeaderValue, typeof(ABFindContactsPagedRequestType));
                    string abpartnerScenario = e.UserState.ToString();

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if ((recursiveCall == 0 && abpartnerScenario == "Initial")
                                || (e.Error.Message.Contains("Need to do full sync")))
                            {
                                recursiveCall++;
                                SynchronizeContactList();
                            }
                            else
                            {
                                OnServiceOperationFailed(sender, new ServiceOperationFailedEventArgs("ABFindContactsPaged", e.Error));
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.ToString(), GetType().Name);
                            }
                        }
                        else
                        {
                            if (null != e.Result.ABFindContactsPagedResult)
                            {
                                AddressBook += e.Result.ABFindContactsPagedResult;
                                Deltas.AddressBookDeltas.Add(e.Result.ABFindContactsPagedResult);
                                Deltas.Save();
                            }
                            if (onSuccess != null)
                            {
                                onSuccess(abService, e);
                            }
                        }
                    }
                };

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABFindContactsPaged", request);
                abService.ABFindContactsPagedAsync(request, partnerScenario);
            }
        }
#else
        internal void abRequest(string partnerScenario, ABFindAllCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABFindAll", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                bool deltasOnly = false;
                DateTime lastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);
                DateTime dynamicItemLastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);
                if (AddressBook.AddressbookLastChange != DateTime.MinValue)
                {
                    lastChange = AddressBook.AddressbookLastChange;
                    dynamicItemLastChange = AddressBook.DynamicItemLastChange;
                    deltasOnly = true;
                }

                ABServiceBinding abService = CreateABService(partnerScenario);
                ABFindAllRequestType request = new ABFindAllRequestType();
                request.abView = "Full";  //NO default!
                request.deltasOnly = deltasOnly;
                request.lastChange = lastChange;
                request.dynamicItemLastChange = dynamicItemLastChange;
                request.dynamicItemView = "Gleam";

                abService.ABFindAllCompleted += delegate(object sender, ABFindAllCompletedEventArgs e)
                {
                    abService = sender as ABServiceBinding;
                    handleServiceHeader(abService.ServiceHeaderValue, request.GetType());
                    string abpartnerScenario = e.UserState.ToString();

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if ((recursiveCall == 0 && abpartnerScenario == "Initial")
                                || (e.Error.Message.Contains("Need to do full sync")))
                            {
                                recursiveCall++;
                                SynchronizeContactList();
                            }
                            else
                            {
                                OnServiceOperationFailed(sender, new ServiceOperationFailedEventArgs("ABFindAll", e.Error));
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.ToString(), GetType().Name);
                            }
                        }
                        else
                        {
                            if (null != e.Result.ABFindAllResult)
                            {
                                AddressBook += e.Result.ABFindAllResult;
                                Deltas.AddressBookDeltas.Add(e.Result.ABFindAllResult);
                                Deltas.Save();
                            }
                            if (onSuccess != null)
                            {
                                onSuccess(abService, e);
                            }
                        }
                    }
                };

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABFindAll", request);
                abService.ABFindAllAsync(request, partnerScenario);
            }
        }
#endif
        internal SharingServiceBinding CreateSharingService(string partnerScenario)
        {
            SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Proxy = WebProxy;
            sharingService.Timeout = Int32.MaxValue;
            sharingService.UserAgent = Properties.Resources.WebServiceUserAgent;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = applicationId;
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            sharingService.ABApplicationHeaderValue.BrandId = NSMessageHandler.MSNTicket.MainBrandID;
            sharingService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;


            return sharingService;
        }

        internal ABServiceBinding CreateABService(string partnerScenario)
        {
            SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = WebProxy;
            abService.Timeout = Int32.MaxValue;
            abService.UserAgent = Properties.Resources.WebServiceUserAgent;
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            abService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;

            return abService;
        }

        public static string[] ConstructLists(Dictionary<string, MSNLists> contacts, bool initial)
        {
            List<string> mls = new List<string>();
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement mlElement = xmlDoc.CreateElement("ml");
            if (initial)
                mlElement.SetAttribute("l", "1");

            if (contacts == null || contacts.Count == 0)
            {
                mls.Add(mlElement.OuterXml);
                return mls.ToArray();
            }

            List<string> sortedContacts = new List<string>(contacts.Keys);
            sortedContacts.Sort(CompareContactsHash);

            int domaincontactcount = 0;
            string currentDomain = null;
            XmlElement domtelElement = null;

            foreach (string contact_hash in sortedContacts)
            {
                String name;
                String domain;
                string[] arr = contact_hash.Split(':');
                MSNLists sendlist = contacts[contact_hash];
                String type = ClientType.EmailMember.ToString();

                if (arr.Length > 0)
                    type = arr[1];

                ClientType clitype = (ClientType)Enum.Parse(typeof(ClientType), type);
                type = ((int)clitype).ToString();

                if (clitype == ClientType.PhoneMember)
                {
                    domain = String.Empty;
                    name = "tel:" + arr[0];
                }
                else
                {
                    String[] usernameanddomain = arr[0].Split('@');
                    domain = usernameanddomain[1];
                    name = usernameanddomain[0];
                }

                if (sendlist != MSNLists.None)
                {
                    if (currentDomain != domain)
                    {
                        currentDomain = domain;
                        domaincontactcount = 0;

                        if (clitype == ClientType.PhoneMember)
                        {
                            domtelElement = xmlDoc.CreateElement("t");
                        }
                        else
                        {
                            domtelElement = xmlDoc.CreateElement("d");
                            domtelElement.SetAttribute("n", currentDomain);
                        }
                        mlElement.AppendChild(domtelElement);
                    }

                    XmlElement contactElement = xmlDoc.CreateElement("c");
                    contactElement.SetAttribute("n", name);
                    contactElement.SetAttribute("l", ((int)sendlist).ToString());
                    if (clitype != ClientType.PhoneMember)
                    {
                        contactElement.SetAttribute("t", type);
                    }
                    domtelElement.AppendChild(contactElement);
                    domaincontactcount++;
                }

                if (mlElement.OuterXml.Length > 7300)
                {
                    mlElement.AppendChild(domtelElement);
                    mls.Add(mlElement.OuterXml);

                    mlElement = xmlDoc.CreateElement("ml");
                    if (initial)
                        mlElement.SetAttribute("l", "1");

                    currentDomain = null;
                    domaincontactcount = 0;
                }
            }

            if (domaincontactcount > 0 && domtelElement != null)
                mlElement.AppendChild(domtelElement);

            mls.Add(mlElement.OuterXml);
            return mls.ToArray();
        }

        private static int CompareContactsHash(string hash1, string hash2)
        {
            string[] str_arr1 = hash1.Split(':');
            string[] str_arr2 = hash2.Split(':');

            if (str_arr1[0] == null)
                return 1;

            else if (str_arr2[0] == null)
                return -1;

            string xContact, yContact;

            if (str_arr1[0].IndexOf("@") == -1)
                xContact = str_arr1[0];
            else
                xContact = str_arr1[0].Substring(str_arr1[0].IndexOf("@") + 1);

            if (str_arr2[0].IndexOf("@") == -1)
                yContact = str_arr2[0];
            else
                yContact = str_arr2[0].Substring(str_arr2[0].IndexOf("@") + 1);

            return String.Compare(xContact, yContact, true, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Contact & Group Operations

        #region Add Contact

        private void AddNonPendingContact(string account, ClientType ct, string invitation)
        {
            // Query other networks and add as new contact if available
            if (account.Contains("@") && ct == ClientType.PassportMember)
            {
                string fqypayload = "<ml l=\"2\"><d n=\"{d}\"><c n=\"{n}\" /></d></ml>";
                fqypayload = fqypayload.Replace("{d}", account.Split('@')[1]);
                fqypayload = fqypayload.Replace("{n}", account.Split('@')[0]);
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("FQY", fqypayload));
            }

            // Add contact to address book with "ContactSave"
            AddNewOrPendingContact(
                account,
                false,
                invitation,
                ct,
                delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    Contact contact = NSMessageHandler.ContactList.GetContact(account, ct);
                    contact.Guid = new Guid(e.Result.ABContactAddResult.guid);

                    if (!contact.OnBlockedList)
                    {
                        // Add to AL
                        if (ct == ClientType.PassportMember)
                        {
                            // without membership, contact service adds this contact to AL automatically.
                            Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(2);
                            hashlist.Add(contact.Hash, MSNLists.AllowedList);
                            string payload = ConstructLists(hashlist, false)[0];
                            NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                            contact.AddToList(MSNLists.AllowedList);
                        }
                        else
                        {
                            // with membership
                            contact.OnAllowedList = true;
                        }
                    }

                    // Add to Forward List
                    contact.OnForwardList = true;
                    System.Threading.Thread.CurrentThread.Join(100);

                    // Get all information. LivePending will be Live :)
                    abRequest("ContactSave", null);
                }
            );
        }

        private void AddPendingContact(Contact contact)
        {
            // Delete PL with "ContactMsgrAPI"
            RemoveContactFromList(contact, MSNLists.PendingList, null);
            System.Threading.Thread.CurrentThread.Join(200);

            // ADD contact to AB with "ContactMsgrAPI"
            AddNewOrPendingContact(
                contact.Mail,
                true,
                String.Empty,
                contact.ClientType,
                delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    contact.Guid = new Guid(e.Result.ABContactAddResult.guid);

                    // FL
                    contact.OnForwardList = true;
                    System.Threading.Thread.CurrentThread.Join(100);

                    // Add RL membership with "ContactMsgrAPI"
                    AddContactToList(contact,
                        MSNLists.ReverseList,
                        delegate
                        {
                            if (!contact.OnBlockedList)
                            {
                                // AL: Extra work for EmailMember: Add allow membership
                                if (ClientType.EmailMember == contact.ClientType)
                                {
                                    contact.OnAllowedList = true;
                                    System.Threading.Thread.CurrentThread.Join(100);
                                }
                                else
                                {
                                    // without membership, contact service adds this contact to AL automatically.
                                    Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(2);
                                    hashlist.Add(contact.Hash, MSNLists.AllowedList);
                                    string payload = ConstructLists(hashlist, false)[0];
                                    NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                                    contact.AddToList(MSNLists.AllowedList);

                                    System.Threading.Thread.CurrentThread.Join(100);
                                }
                            }

                            abRequest("ContactMsgrAPI", null);
                        }
                    );
                }
           );
        }

        private void AddNewOrPendingContact(string account, bool pending, string invitation, ClientType network, ABContactAddCompletedEventHandler onSuccess)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddContact", new MSNPSharpException("You don't have access right on this action anymore.")));
            }
            else
            {
                ABServiceBinding abService = CreateABService(pending ? "ContactMsgrAPI" : "ContactSave");
                abService.ABContactAddCompleted += delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABContactAddRequestType));
                    if (!e.Cancelled && e.Error == null)
                    {
                        if (onSuccess != null)
                        {
                            onSuccess(service, e);
                        }
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("AddContact", e.Error));
                    }
                    ((IDisposable)service).Dispose();
                };

                ABContactAddRequestType request = new ABContactAddRequestType();
                request.abId = "00000000-0000-0000-0000-000000000000";
                request.contacts = new ContactType[] { new ContactType() };
                request.contacts[0].contactInfo = new contactInfoType();

                switch (network)
                {
                    case ClientType.PassportMember:
                        request.contacts[0].contactInfo.contactType = MessengerContactType.Regular; // MUST BE "Regular". See r746
                        request.contacts[0].contactInfo.passportName = account;
                        request.contacts[0].contactInfo.isMessengerUser = request.contacts[0].contactInfo.isMessengerUserSpecified = true;
                        request.contacts[0].contactInfo.MessengerMemberInfo = new MessengerMemberInfo();
                        if (pending == false && !String.IsNullOrEmpty(invitation))
                        {
                            request.contacts[0].contactInfo.MessengerMemberInfo.PendingAnnotations = new Annotation[] { new Annotation() };
                            request.contacts[0].contactInfo.MessengerMemberInfo.PendingAnnotations[0].Name = "MSN.IM.InviteMessage";
                            request.contacts[0].contactInfo.MessengerMemberInfo.PendingAnnotations[0].Value = invitation;
                        }
                        request.contacts[0].contactInfo.MessengerMemberInfo.DisplayName = NSMessageHandler.Owner.Name;
                        request.options = new ABContactAddRequestTypeOptions();
                        request.options.EnableAllowListManagement = true; //contact service adds this contact to AL automatically if not blocked. 
                        break;

                    case ClientType.EmailMember:
                        request.contacts[0].contactInfo.emails = new contactEmailType[] { new contactEmailType() };
                        request.contacts[0].contactInfo.emails[0].contactEmailType1 = ContactEmailTypeType.Messenger2;
                        request.contacts[0].contactInfo.emails[0].email = account;
                        request.contacts[0].contactInfo.emails[0].isMessengerEnabled = true;
                        request.contacts[0].contactInfo.emails[0].Capability = "32";
                        request.contacts[0].contactInfo.emails[0].propertiesChanged =
                            String.Join(PropertyString.propertySeparator,
                            new string[] { PropertyString.Email, PropertyString.IsMessengerEnabled, PropertyString.Capability });

                        break;

                    case ClientType.PhoneMember:
                        request.contacts[0].contactInfo.phones = new contactPhoneType[] { new contactPhoneType() };
                        request.contacts[0].contactInfo.phones[0].contactPhoneType1 = ContactPhoneTypeType.ContactPhoneMobile;
                        request.contacts[0].contactInfo.phones[0].number = account;
                        request.contacts[0].contactInfo.phones[0].isMessengerEnabled = true;
                        request.contacts[0].contactInfo.phones[0].propertiesChanged =
                            String.Join(PropertyString.propertySeparator,
                            new string[] { PropertyString.Number, PropertyString.IsMessengerEnabled });
                        break;
                }

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABContactAdd", request);
                abService.ABContactAddAsync(request, new object());
            }
        }

        /// <summary>
        /// Creates a new contact on your address book and adds to allowed list if not blocked before.
        /// </summary>
        /// <param name="account">An email address or phone number to add. The email address can be yahoo account.</param>
        /// <remarks>The phone format is +CC1234567890 for phone contact, CC is Country Code</remarks>
        public void AddNewContact(string account)
        {
            AddNewContact(account, String.Empty);
        }

        /// <summary>
        /// Creates a new contact on your address book and adds to allowed list if not blocked before.
        /// </summary>
        /// <param name="account">An email address or phone number to add. The email address can be yahoo account.</param>
        /// <param name="invitation">The reason of the adding contact</param>
        /// <remarks>The phone format is +CC1234567890, CC is Country Code</remarks>
        public void AddNewContact(string account, string invitation)
        {
            long test;
            if (long.TryParse(account, out test) ||
                (account.StartsWith("+") && long.TryParse(account.Substring(1), out test)))
            {
                if (account.StartsWith("00"))
                {
                    account = "+" + account.Substring(2);
                }
                AddNewContact(account, ClientType.PhoneMember, invitation);
            }
            else
            {
                AddNewContact(account, ClientType.PassportMember, invitation);
            }
        }

        internal void AddNewContact(string account, ClientType network, string invitation)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddContact", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (NSMessageHandler.ContactList.HasContact(account, network))
            {
                Contact contact = NSMessageHandler.ContactList.GetContact(account, network);

                if (contact.OnPendingList)
                {
                    AddPendingContact(contact);
                }
                else if (contact.Guid == Guid.Empty) // This user is on AL or BL or RL.
                {
                    AddNonPendingContact(account, network, invitation);
                }
                else if (contact.Guid != Guid.Empty) // Email or Messenger buddy.
                {
                    if (!contact.IsMessengerUser) // Email buddy, make Messenger :)
                    {
                        contact.IsMessengerUser = true;
                    }
                    if (!contact.OnBlockedList) // Messenger buddy. Not in AL.
                    {
                        contact.OnAllowedList = true;
                    }
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Cannot add contact: " + contact.Hash, GetType().Name);
                }
            }
            else
            {
                AddNonPendingContact(account, network, invitation);
            }
        }

        #endregion

        #region RemoveContact

        /// <summary>
        /// Remove the specified contact from your forward list.
        /// Note that remote contacts that are allowed/blocked remain allowed/blocked.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        public void RemoveContact(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("Timer");
            abService.ABContactDeleteCompleted += delegate(object service, ABContactDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABContactDeleteRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    abRequest("ContactSave", null);
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("RemoveContact", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABContactDeleteRequestType request = new ABContactDeleteRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.contacts = new ContactIdType[] { new ContactIdType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABContactDelete", request);
            abService.ABContactDeleteAsync(request, new object());
        }

        #endregion

        #region UpdateContact

        internal void UpdateContact(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactUpdate", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (!AddressBook.AddressbookContacts.ContainsKey(contact.Guid))
                return;

            ContactType abContactType = AddressBook.AddressbookContacts[contact.Guid];

            List<string> propertiesChanged = new List<string>();
            ABContactUpdateRequestType request = new ABContactUpdateRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();
            request.contacts[0].contactInfo = new contactInfoType();

            // Comment
            if (abContactType.contactInfo.comment != contact.Comment)
            {
                propertiesChanged.Add(PropertyString.Comment);
                request.contacts[0].contactInfo.comment = contact.Comment;
            }

            // DisplayName
            if (abContactType.contactInfo.displayName != contact.Name)
            {
                propertiesChanged.Add(PropertyString.DisplayName);
                request.contacts[0].contactInfo.displayName = contact.Name;
            }

            // Annotations
            List<Annotation> annotationsChanged = new List<Annotation>();
            Dictionary<string, string> oldAnnotations = new Dictionary<string, string>();
            if (abContactType.contactInfo.annotations != null)
            {
                foreach (Annotation anno in abContactType.contactInfo.annotations)
                {
                    oldAnnotations[anno.Name] = anno.Value;
                }
            }

            // Annotations: AB.NickName
            string oldNickName = oldAnnotations.ContainsKey("AB.NickName") ? oldAnnotations["AB.NickName"] : String.Empty;
            if (oldNickName != contact.NickName)
            {
                Annotation anno = new Annotation();
                anno.Name = "AB.NickName";
                anno.Value = contact.NickName;
                annotationsChanged.Add(anno);
            }

            if (annotationsChanged.Count > 0)
            {
                propertiesChanged.Add(PropertyString.Annotation);
                request.contacts[0].contactInfo.annotations = annotationsChanged.ToArray();
            }


            // ClientType changes
            switch (contact.ClientType)
            {
                case ClientType.PassportMember:
                    {
                        // IsMessengerUser
                        if (abContactType.contactInfo.isMessengerUser != contact.IsMessengerUser)
                        {
                            propertiesChanged.Add(PropertyString.IsMessengerUser);
                            request.contacts[0].contactInfo.isMessengerUser = contact.IsMessengerUser;
                            request.contacts[0].contactInfo.isMessengerUserSpecified = true;
                            propertiesChanged.Add(PropertyString.MessengerMemberInfo); // Pang found WLM2009 add this.
                            request.contacts[0].contactInfo.MessengerMemberInfo = new MessengerMemberInfo(); // But forgot to add this...
                            request.contacts[0].contactInfo.MessengerMemberInfo.DisplayName = NSMessageHandler.Owner.Name; // and also this :)
                        }

                        // ContactType
                        if (abContactType.contactInfo.contactType != contact.ContactType)
                        {
                            propertiesChanged.Add(PropertyString.ContactType);
                            request.contacts[0].contactInfo.contactType = contact.ContactType;
                        }
                    }
                    break;

                case ClientType.EmailMember:
                    {
                        if (abContactType.contactInfo.emails != null)
                        {
                            foreach (contactEmailType em in abContactType.contactInfo.emails)
                            {
                                if (em.email.ToLowerInvariant() == contact.Mail.ToLowerInvariant() && em.isMessengerEnabled != contact.IsMessengerUser)
                                {
                                    propertiesChanged.Add(PropertyString.ContactEmail);
                                    request.contacts[0].contactInfo.emails = new contactEmailType[] { new contactEmailType() };
                                    request.contacts[0].contactInfo.emails[0].contactEmailType1 = ContactEmailTypeType.Messenger2;
                                    request.contacts[0].contactInfo.emails[0].isMessengerEnabled = contact.IsMessengerUser;
                                    request.contacts[0].contactInfo.emails[0].propertiesChanged = PropertyString.IsMessengerEnabled; //"IsMessengerEnabled";
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case ClientType.PhoneMember:
                    {
                        if (abContactType.contactInfo.phones != null)
                        {
                            foreach (contactPhoneType ph in abContactType.contactInfo.phones)
                            {
                                if (ph.number == contact.Mail && ph.isMessengerEnabled != contact.IsMessengerUser)
                                {
                                    propertiesChanged.Add(PropertyString.ContactPhone);
                                    request.contacts[0].contactInfo.phones = new contactPhoneType[] { new contactPhoneType() };
                                    request.contacts[0].contactInfo.phones[0].contactPhoneType1 = ContactPhoneTypeType.ContactPhoneMobile;
                                    request.contacts[0].contactInfo.phones[0].isMessengerEnabled = contact.IsMessengerUser;
                                    request.contacts[0].contactInfo.phones[0].propertiesChanged = PropertyString.IsMessengerEnabled; //"IsMessengerEnabled";
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }

            if (propertiesChanged.Count > 0)
            {
                ABServiceBinding abService = CreateABService(contact.IsMessengerUser ? "ContactSave" : "Timer");
                abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
                {
                    handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABContactUpdateRequestType));
                    if (!e.Cancelled && e.Error == null)
                    {
                        abRequest("ContactSave", null);
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateContact", e.Error));
                    }
                };
                request.contacts[0].propertiesChanged = String.Join(PropertyString.propertySeparator, propertiesChanged.ToArray());

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABContactUpdate", request);
                abService.ABContactUpdateAsync(request, new object());
            }
        }

        internal void UpdateMe()
        {
            if (NSMessageHandler.AutoSynchronize)
            {
                UpdatePrivacySettings();
                if (NSMessageHandler.Credentials.MsnProtocol >= MsnProtocol.MSNP16)
                {
                    UpdateGeneralDialogSettings();
                }
            }
        }

        private void UpdateGeneralDialogSettings()
        {
            Owner owner = NSMessageHandler.Owner;

            if (owner == null)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            MPOP oldMPOP = AddressBook.MyProperties["mpop"] == "1" ? MPOP.KeepOnline : MPOP.AutoLogoff;

            List<Annotation> annos = new List<Annotation>();


            if (oldMPOP != owner.MPOPMode)
            {
                Annotation anno = new Annotation();
                anno.Name = "MSN.IM.MPOP";
                anno.Value = owner.MPOPMode == MPOP.KeepOnline ? "1" : "0";
                annos.Add(anno);
            }


            if (annos.Count > 0 && NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                ABServiceBinding abService = CreateABService("PrivacyApply");  //In msnp17 this is "GeneralDialogApply"
                abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
                {
                    handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABContactUpdateRequestType));
                    if (!e.Cancelled && e.Error == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "UpdateGeneralDialogSetting completed.", GetType().Name);
                        AddressBook.MyProperties["mpop"] = owner.MPOPMode == MPOP.KeepOnline ? "1" : "0";
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateGeneralDialogSetting", e.Error));
                    }
                    ((IDisposable)service).Dispose();
                    return;
                };

                ABContactUpdateRequestType request = new ABContactUpdateRequestType();
                request.abId = "00000000-0000-0000-0000-000000000000";
                request.contacts = new ContactType[] { new ContactType() };
                request.contacts[0].contactInfo = new contactInfoType();
                request.contacts[0].contactInfo.contactType = MessengerContactType.Me;
                request.contacts[0].contactInfo.annotations = annos.ToArray();
                request.contacts[0].propertiesChanged = PropertyString.Annotation; //"Annotation";

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABContactUpdate", request);
                abService.ABContactUpdateAsync(request, new object());
            }
        }

        private void UpdatePrivacySettings()
        {
            Owner owner = NSMessageHandler.Owner;

            if (owner == null)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            PrivacyMode oldPrivacy = AddressBook.MyProperties["blp"] == "1" ? PrivacyMode.AllExceptBlocked : PrivacyMode.NoneButAllowed;
            NotifyPrivacy oldNotify = AddressBook.MyProperties["gtc"] == "1" ? NotifyPrivacy.PromptOnAdd : NotifyPrivacy.AutomaticAdd;
            RoamLiveProperty oldRoaming = AddressBook.MyProperties["roamliveproperties"] == "1" ? RoamLiveProperty.Enabled : RoamLiveProperty.Disabled;
            List<Annotation> annos = new List<Annotation>();
            List<string> propertiesChanged = new List<string>();

            if (oldPrivacy != owner.Privacy)
            {
                Annotation anno = new Annotation();
                anno.Name = "MSN.IM.BLP";
                anno.Value = owner.Privacy == PrivacyMode.AllExceptBlocked ? "1" : "0";
                annos.Add(anno);

                if (owner.Privacy == PrivacyMode.NoneButAllowed)
                    owner.SetNotifyPrivacy(NotifyPrivacy.PromptOnAdd);
            }

            if (oldNotify != owner.NotifyPrivacy)
            {
                Annotation anno = new Annotation();
                anno.Name = "MSN.IM.GTC";
                anno.Value = owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd ? "1" : "0";
                annos.Add(anno);
            }

            if (oldRoaming != owner.RoamLiveProperty)
            {
                Annotation anno = new Annotation();
                anno.Name = "MSN.IM.RoamLiveProperties";
                anno.Value = owner.RoamLiveProperty == RoamLiveProperty.Enabled ? "1" : "2";
                annos.Add(anno);
            }

            if (annos.Count > 0)
            {
                propertiesChanged.Add(PropertyString.Annotation);
            }

            // DisplayName
            //if (owner.Name != NSMessageHandler.ContactService.Deltas.Profile.DisplayName)
            //    propertiesChanged.Add("DisplayName");

            if (propertiesChanged.Count > 0 && NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                ABServiceBinding abService = CreateABService("PrivacyApply");
                abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
                {
                    handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABContactUpdateRequestType));
                    if (!e.Cancelled && e.Error == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "UpdateMe completed.", GetType().Name);

                        AddressBook.MyProperties["blp"] = owner.Privacy == PrivacyMode.AllExceptBlocked ? "1" : "0";
                        AddressBook.MyProperties["gtc"] = owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd ? "1" : "0";
                        AddressBook.MyProperties["roamliveproperties"] = owner.RoamLiveProperty == RoamLiveProperty.Enabled ? "1" : "2";
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateMe", e.Error));
                    }
                    ((IDisposable)service).Dispose();
                    return;
                };

                ABContactUpdateRequestType request = new ABContactUpdateRequestType();
                request.abId = "00000000-0000-0000-0000-000000000000";
                request.contacts = new ContactType[] { new ContactType() };
                request.contacts[0].contactInfo = new contactInfoType();
                request.contacts[0].contactInfo.contactType = MessengerContactType.Me;
                //request.contacts[0].contactInfo.displayName = owner.Name;
                request.contacts[0].contactInfo.annotations = annos.ToArray();
                request.contacts[0].propertiesChanged = String.Join(PropertyString.propertySeparator, propertiesChanged.ToArray());

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABContactUpdate", request);
                abService.ABContactUpdateAsync(request, new object());
            }
        }

        #endregion

        #region AddContactGroup & RemoveContactGroup & RenameGroup

        /// <summary>
        /// Send a request to the server to add a new contactgroup.
        /// </summary>
        /// <param name="groupName">The name of the group to add</param>
        public void AddContactGroup(string groupName)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABGroupAddRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(groupName, e.Result.ABGroupAddResult.guid, NSMessageHandler));
                    NSMessageHandler.ContactService.OnContactGroupAdded(new ContactGroupEventArgs((ContactGroup)NSMessageHandler.ContactGroups[e.Result.ABGroupAddResult.guid]));
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("AddContactGroup", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupAddRequestType request = new ABGroupAddRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupAddOptions = new ABGroupAddRequestTypeGroupAddOptions();
            request.groupAddOptions.fRenameOnMsgrConflict = false;
            request.groupAddOptions.fRenameOnMsgrConflictSpecified = true;
            request.groupInfo = new ABGroupAddRequestTypeGroupInfo();
            request.groupInfo.GroupInfo = new groupInfoType();
            request.groupInfo.GroupInfo.name = groupName;
            request.groupInfo.GroupInfo.fMessenger = false;
            request.groupInfo.GroupInfo.fMessengerSpecified = true;
            request.groupInfo.GroupInfo.groupType = "C8529CE2-6EAD-434d-881F-341E17DB3FF8";
            request.groupInfo.GroupInfo.annotations = new Annotation[] { new Annotation() };
            request.groupInfo.GroupInfo.annotations[0].Name = "MSN.IM.Display";
            request.groupInfo.GroupInfo.annotations[0].Value = "1";

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABGroupAdd", request);
            abService.ABGroupAddAsync(request, new object());
        }

        /// <summary>
        /// Send a request to the server to remove a contactgroup. Any contacts in the group will also be removed from the forward list.
        /// </summary>
        /// <param name="contactGroup">The group to remove</param>
        public void RemoveContactGroup(ContactGroup contactGroup)
        {
            foreach (Contact cnt in NSMessageHandler.ContactList.All)
            {
                if (cnt.ContactGroups.Contains(contactGroup))
                {
                    throw new InvalidOperationException("Target group not empty, please remove all contacts form the group first.");
                }
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("Timer");
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABGroupDeleteRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                    AddressBook.Groups.Remove(new Guid(contactGroup.Guid));
                    NSMessageHandler.ContactService.OnContactGroupRemoved(new ContactGroupEventArgs(contactGroup));
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("ContactGroupList.Remove", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupDeleteRequestType request = new ABGroupDeleteRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { contactGroup.Guid };

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABGroupDelete", request);
            abService.ABGroupDeleteAsync(request, new object());
        }


        /// <summary>
        /// Set the name of a contact group
        /// </summary>
        /// <param name="group">The contactgroup which name will be set</param>
        /// <param name="newGroupName">The new name</param>
        public void RenameGroup(ContactGroup group, string newGroupName)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupUpdate", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupUpdateCompleted += delegate(object service, ABGroupUpdateCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABGroupUpdateRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    group.SetName(newGroupName);
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("RenameGroup", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupUpdateRequestType request = new ABGroupUpdateRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groups = new GroupType[1] { new GroupType() };
            request.groups[0].groupId = group.Guid;
            request.groups[0].propertiesChanged = PropertyString.GroupName; //"GroupName";
            request.groups[0].groupInfo = new groupInfoType();
            request.groups[0].groupInfo.name = newGroupName;

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABGroupUpdate", request);
            abService.ABGroupUpdateAsync(request, new object());
        }

        #endregion

        #region AddContactToGroup & RemoveContactFromGroup

        public void AddContactToGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactAdd", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupContactAddCompleted += delegate(object service, ABGroupContactAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABGroupContactAddRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    contact.AddContactToGroup(group);
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("AddContactToGroup", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupContactAddRequestType request = new ABGroupContactAddRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABGroupContactAdd", request);
            abService.ABGroupContactAddAsync(request, new object());
        }

        public void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactDelete", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupContactDeleteCompleted += delegate(object service, ABGroupContactDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, typeof(ABGroupContactDeleteRequestType));
                if (!e.Cancelled && e.Error == null)
                {
                    contact.RemoveContactFromGroup(group);
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("RemoveContactFromGroup", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupContactDeleteRequestType request = new ABGroupContactDeleteRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "ABGroupContactDelete", request);
            abService.ABGroupContactDeleteAsync(request, new object());
        }
        #endregion

        #region AddContactToList

        /// <summary>
        /// Send a request to the server to add this contact to a specific list.
        /// </summary>
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to place the contact in</param>
        /// <param name="onSuccess"></param>
        internal void AddContactToList(Contact contact, MSNLists list, EventHandler onSuccess)
        {
            if (list == MSNLists.PendingList) //this causes disconnect 
                return;

            // check whether the update is necessary
            if (contact.HasLists(list))
                return;

            Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(2);
            hashlist.Add(contact.Hash, list);
            string payload = ConstructLists(hashlist, false)[0];

            if (list == MSNLists.ForwardList)
            {
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                contact.AddToList(list);

                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }

                return;
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            SharingServiceBinding sharingService = CreateSharingService((list == MSNLists.ReverseList) ? "ContactMsgrAPI" : "BlockUnblock");

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.GetTargetService(ServiceFilterType.Messenger);
            addMemberRequest.serviceHandle.Id = messengerService.Id.ToString();
            addMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);
            BaseMember member = new BaseMember();

            if (contact.ClientType == ClientType.PassportMember)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Mail;
            }
            else if (contact.ClientType == ClientType.EmailMember)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.State = MemberState.Accepted;
                emailMember.Email = contact.Mail;
                emailMember.Annotations = new Annotation[] { new Annotation() };
                emailMember.Annotations[0].Name = "MSN.IM.BuddyType";
                emailMember.Annotations[0].Value = "32:";
            }
            else if (contact.ClientType == ClientType.PhoneMember)
            {
                member = new PhoneMember();
                PhoneMember phoneMember = member as PhoneMember;
                phoneMember.State = MemberState.Accepted;
                phoneMember.PhoneNumber = contact.Mail;
            }

            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, typeof(AddMemberRequestType));
                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member already exists"))
                    {
                        // XXX But the response is OK
                        OnServiceOperationFailed(sharingService, new ServiceOperationFailedEventArgs("AddContactToList", e.Error));
                        return;
                    }

                    contact.AddToList(list);
                    AddressBook.AddMemberhip(ServiceFilterType.Messenger, contact.Mail, contact.ClientType, GetMemberRole(list), member);
                    NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, list));

                    if ((list & MSNLists.AllowedList) == MSNLists.AllowedList || (list & MSNLists.BlockedList) == MSNLists.BlockedList)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "AddMember completed: " + list, GetType().Name);
                }
            };

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, "AddMember", addMemberRequest);
            sharingService.AddMemberAsync(addMemberRequest, new object());

        }

        #endregion

        #region RemoveContactFromList

        /// <summary>
        /// Send a request to the server to remove a contact from a specific list.
        /// </summary> 
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to remove the contact from</param>
        /// <param name="onSuccess"></param>
        internal void RemoveContactFromList(Contact contact, MSNLists list, EventHandler onSuccess)
        {
            if (list == MSNLists.ReverseList) //this causes disconnect
                return;

            // check whether the update is necessary
            if (!contact.HasLists(list))
                return;

            Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(2);
            hashlist.Add(contact.Hash, list);
            string payload = ConstructLists(hashlist, false)[0];

            if (list == MSNLists.ForwardList)
            {
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));
                contact.RemoveFromList(list);

                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }
                return;
            }

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || AddressBook == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            SharingServiceBinding sharingService = CreateSharingService((list == MSNLists.PendingList) ? "ContactMsgrAPI" : "BlockUnblock");
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, typeof(DeleteMemberRequestType));
                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member does not exist"))
                    {
                        // XXX But the response is OK
                        OnServiceOperationFailed(sharingService, new ServiceOperationFailedEventArgs("RemoveContactFromList", e.Error));
                        return;
                    }

                    contact.RemoveFromList(list);
                    AddressBook.RemoveMemberhip(ServiceFilterType.Messenger, contact.Mail, contact.ClientType, GetMemberRole(list));
                    NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, list));

                    if ((list & MSNLists.AllowedList) == MSNLists.AllowedList || (list & MSNLists.BlockedList) == MSNLists.BlockedList)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DeleteMember completed: " + list, GetType().Name);
                }
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = AddressBook.GetTargetService(ServiceFilterType.Messenger);
            deleteMemberRequest.serviceHandle.Id = messengerService.Id.ToString();   //Always set to 0 ??
            deleteMemberRequest.serviceHandle.Type = messengerService.ServiceType;

            Membership memberShip = new Membership();
            memberShip.MemberRole = GetMemberRole(list);

            BaseMember member = new BaseMember();

            /* If you cannot determind the client type, just use a BaseMember and specify the membershipId.
             * The offical client just do so. But once the contact is removed and added to another rolelist,its membershipId also changed.
             * Unless you get your contactlist again, you have to use the account if you wanted to delete that contact once more.
             * To avoid this,we have to ensure the client type we've got is correct at the very beginning.
             * */

            if (contact.ClientType == ClientType.PassportMember)
            {
                member = new PassportMember();
                PassportMember passportMember = member as PassportMember;
                passportMember.PassportName = contact.Mail;
            }
            else if (contact.ClientType == ClientType.EmailMember)
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.Email = contact.Mail;
            }
            else if (contact.ClientType == ClientType.PhoneMember)
            {
                member = new PhoneMember();
                PhoneMember phoneMember = member as PhoneMember;
                phoneMember.State = MemberState.Accepted;
                phoneMember.PhoneNumber = contact.Mail;
            }

            memberShip.Members = new BaseMember[] { member };
            deleteMemberRequest.memberships = new Membership[] { memberShip };

            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, "DeleteMember", deleteMemberRequest);

            sharingService.DeleteMemberAsync(deleteMemberRequest, new object());
        }

        #endregion

        #region BlockContact & UnBlockContact

        /// <summary>
        /// Block this contact. After this you aren't able to receive messages from this contact. This contact
        /// will be placed in your block list and removed from your allowed list.
        /// </summary>
        /// <param name="contact">Contact to block</param>
        public void BlockContact(Contact contact)
        {
            if (contact.OnAllowedList)
            {
                RemoveContactFromList(
                    contact,
                    MSNLists.AllowedList,
                    delegate
                    {
                        System.Threading.Thread.CurrentThread.Join(100);

                        if (!contact.OnBlockedList)
                            AddContactToList(contact, MSNLists.BlockedList, null);
                    }
                );

                // wait some time before sending the other request. If not we may block before we removed
                // from the allow list which will give an error
                System.Threading.Thread.CurrentThread.Join(100);
            }
            else if (!contact.OnBlockedList)
            {
                AddContactToList(contact, MSNLists.BlockedList, null);
            }
        }

        /// <summary>
        /// Unblock this contact. After this you are able to receive messages from this contact. This contact
        /// will be removed from your blocked list and placed in your allowed list.
        /// </summary>
        /// <param name="contact">Contact to unblock</param>
        public void UnBlockContact(Contact contact)
        {
            if (contact.OnBlockedList)
            {
                RemoveContactFromList(
                    contact,
                    MSNLists.BlockedList,
                    delegate
                    {
                        System.Threading.Thread.CurrentThread.Join(100);

                        if (!contact.OnAllowedList)
                            AddContactToList(contact, MSNLists.AllowedList, null);
                    }
                );

                System.Threading.Thread.CurrentThread.Join(100);
            }
            else if (!contact.OnAllowedList)
            {
                AddContactToList(contact, MSNLists.AllowedList, null);
            }
        }


        #endregion

        #endregion

        #region DeleteRecordFile & handleServiceHeader

        /// <summary>
        /// Delete the record file that contains the contactlist of owner.
        /// </summary>
        public void DeleteRecordFile()
        {
            if (NSMessageHandler.Owner != null && NSMessageHandler.Owner.Mail != null)
            {
                string addressbookFile = Path.Combine(Settings.SavePath, NSMessageHandler.Owner.Mail.GetHashCode() + ".mcl");
                if (File.Exists(addressbookFile))
                {
                    File.SetAttributes(addressbookFile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(addressbookFile);
                }

                string deltasResultFile = Path.Combine(Settings.SavePath, NSMessageHandler.Owner.Mail.GetHashCode() + "d" + ".mcl");
                if (File.Exists(deltasResultFile))
                {
                    if (Deltas != null)
                    {
                        Deltas.Truncate();  //If we saved cachekey and preferred host in it, deltas can't be deleted.
                    }
                    else
                    {
                        File.SetAttributes(deltasResultFile, FileAttributes.Normal);  //By default, the file is hidden.
                        File.Delete(deltasResultFile);
                    }
                }
                abSynchronized = false;
            }
        }

        internal void ChangeCacheKeyAndPreferredHostForSpecifiedMethod(SharingServiceBinding sharingService, string methodName, object param)
        {
            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(CacheKeyType.OmegaContactServiceCacheKey, sharingService, methodName, param);
            sharingService.ABApplicationHeaderValue.CacheKey = Deltas.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
        }


        internal void ChangeCacheKeyAndPreferredHostForSpecifiedMethod(ABServiceBinding abService, string methodName, object param)
        {
            ChangeCacheKeyAndPreferredHostForSpecifiedMethod(CacheKeyType.OmegaContactServiceCacheKey, abService, methodName, param);
            abService.ABApplicationHeaderValue.CacheKey = Deltas.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
        }

        internal void ChangeCacheKeyAndPreferredHostForSpecifiedMethod(CacheKeyType keyType, SoapHttpClientProtocol webservice, string methodName, object param)
        {
            if (Deltas == null)
            {
                throw new MSNPSharpException("Deltas is null.");
            }

            string originalUrl = webservice.Url;
            string originalHost = webservice.Url.Substring(webservice.Url.IndexOf(@"://") + 3 /* @"://".Length */);
            originalHost = originalHost.Substring(0, originalHost.IndexOf(@"/"));

            if (Deltas.CacheKeys.ContainsKey(keyType) == false ||
                Deltas.CacheKeys[keyType] == string.Empty ||
                (Deltas.CacheKeys[keyType] != string.Empty &&
                (Deltas.PreferredHosts.ContainsKey(param.GetType().ToString()) == false ||
                Deltas.PreferredHosts[param.GetType().ToString()] == String.Empty)))
            {

                try
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, webservice.GetType().ToString() + " is requesting a cachekey and preferred host for calling " + methodName);

                    switch (keyType)
                    {
                        case CacheKeyType.OmegaContactServiceCacheKey:
                            webservice.Url = webservice.Url.Replace(originalHost, RDRServiceHost.OmegaContactRDRServiceHost);
                            break;
                        case CacheKeyType.StorageServiceCacheKey:
                            webservice.Url = webservice.Url.Replace(originalHost, RDRServiceHost.StorageRDRServiceHost);
                            break;
                    }

                    webservice.GetType().InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod,
                        null, webservice,
                        new object[] { param });
                }
                catch (Exception ex)
                {
                    try
                    {
                        XmlDocument errdoc = new XmlDocument();
                        string errorMessage = ex.InnerException.Message;
                        string xmlstr = errorMessage.Substring(errorMessage.IndexOf("<?xml"));
                        xmlstr = xmlstr.Substring(0, xmlstr.IndexOf("</soap:envelope>", StringComparison.InvariantCultureIgnoreCase) + "</soap:envelope>".Length);

                        //I think the xml parser microsoft used internally is just a super parser, it can ignore everything.
                        xmlstr = xmlstr.Replace("&amp;", "&");
                        xmlstr = xmlstr.Replace("&", "&amp;");


                        errdoc.LoadXml(xmlstr);
                        XmlNodeList findnodelist = errdoc.GetElementsByTagName("PreferredHostName");
                        if (findnodelist.Count > 0)
                        {
                            PreferredHosts[param.GetType().ToString()] = findnodelist[0].InnerText;
                        }
                        else
                        {
                            PreferredHosts[param.GetType().ToString()] = originalHost;  //If nothing (Storage), just use the old host.
                        }

                        findnodelist = errdoc.GetElementsByTagName("CacheKey");
                        if (findnodelist.Count > 0)
                        {
                            Deltas.CacheKeys[keyType] = findnodelist[0].InnerText;
                        }
                    }
                    catch (Exception exc)
                    {
                        Trace.WriteLineIf(
                            Settings.TraceSwitch.TraceError,
                            "An error occured while getting CacheKey and Preferred host:\r\n" +
                            "Service:    " + webservice.GetType().ToString() + "\r\n" +
                            "MethodName: " + methodName + "\r\n" +
                            "Message:    " + exc.Message);

                        PreferredHosts[param.GetType().ToString()] = originalHost; //If there's an error, we must set the host back to its original value.
                    }
                }
                Deltas.Save();
            }

            webservice.Url = originalUrl.Replace(originalHost, PreferredHosts[param.GetType().ToString()]);

        }

        internal void handleServiceHeader(ServiceHeader sh, Type requestType)
        {
            if (null != sh && NSMessageHandler.IsSignedIn)
            {
                if (sh.CacheKeyChanged)
                {
                    NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey] = sh.CacheKey;

                }

                if (!String.IsNullOrEmpty(sh.PreferredHostName))
                {
                    PreferredHosts[requestType.ToString()] = sh.PreferredHostName;
                }

                Deltas.Save();
            }
        }

        #endregion

        public string GetMemberRole(MSNLists list)
        {
            switch (list)
            {
                case MSNLists.AllowedList:
                    return MemberRole.Allow;

                case MSNLists.BlockedList:
                    return MemberRole.Block;

                case MSNLists.PendingList:
                    return MemberRole.Pending;

                case MSNLists.ReverseList:
                    return MemberRole.Reverse;
            }
            return MemberRole.ProfilePersonalContact;
        }

        public MSNLists GetMSNList(string memberRole)
        {
            switch (memberRole)
            {
                case MemberRole.Allow:
                    return MSNLists.AllowedList;
                case MemberRole.Block:
                    return MSNLists.BlockedList;
                case MemberRole.Reverse:
                    return MSNLists.ReverseList;
                case MemberRole.Pending:
                    return MSNLists.PendingList;
            }
            throw new MSNPSharpException("Unknown MemberRole type");
        }

        internal void Clear()
        {
            recursiveCall = 0;
            initialADLs.Clear();
            initialADLcount = 0;

            abSynchronized = false;
            AddressBook = null;
            Deltas = null;
        }
    }
};
