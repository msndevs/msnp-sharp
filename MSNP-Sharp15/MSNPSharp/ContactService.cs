using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// Provide webservice operations for contacts
    /// </summary>
    public class ContactService : MSNService
    {
        #region Fields

        private int recursiveCall = 0;
        private string applicationId = String.Empty;
        private List<int> initialADLs = new List<int>();
        private int initialADLcount = 0;
        private bool abSynchronized = false;

        internal XMLContactList AddressBook = null;
        internal DeltasList Deltas = null;

        #endregion

        public ContactService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
            applicationId = Properties.Resources.ApplicationId;
        }

        #region Properties

        /// <summary>
        /// Preferred host of the contact service.
        /// </summary>
        private string PreferredHost
        {
            get
            {
                if (AddressBook != null && AddressBook.MyProperties != null && AddressBook.MyProperties.ContainsKey("preferredhost") && !String.IsNullOrEmpty(AddressBook.MyProperties["preferredhost"]))
                {
                    return AddressBook.MyProperties["preferredhost"];
                }
                return "contacts.msn.com";
            }
        }

        /// <summary>
        /// Keep track whether a address book synchronization has been completed so we can warn the client programmer
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
        /// Send the synchronize command to the server. This will rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// You <b>must</b> call this function before setting your initial status, otherwise you won't received online notifications of other clients.
        /// Please note that you can only synchronize a single time per session! (this is limited by the the msn-servers)
        /// </remarks>
        public virtual void SynchronizeContactList()
        {
            if (AddressBookSynchronized)
            {
                if (Settings.TraceSwitch.TraceWarning)
                    Trace.WriteLine("SynchronizeContactList() was called, but the list has already been synchronized. Make sure the AutoSynchronize property is set to false in order to manually synchronize the contact list.", "NSMessageHandler");

                return;
            }

            if (recursiveCall != 0)
            {
                DeleteRecordFile();
            }

            bool nocompress = true;
            string addressbookFile = Path.GetFullPath(@".\") + NSMessageHandler.Owner.Mail.GetHashCode() + ".mcl";
            string deltasResultsFile = Path.GetFullPath(@".\") + NSMessageHandler.Owner.Mail.GetHashCode() + "d" + ".mcl";
            try
            {
                AddressBook = XMLContactList.LoadFromFile(addressbookFile, nocompress, NSMessageHandler);
                Deltas = DeltasList.LoadFromFile(deltasResultsFile, nocompress, NSMessageHandler);
                if ((AddressBook.Version != Properties.Resources.XMLContactListVersion
                    || Deltas.Version != Properties.Resources.DeltasListVersion)
                    && recursiveCall == 0)
                {
                    recursiveCall++;
                    SynchronizeContactList();
                    return;
                }
            }
            catch (Exception ex)
            {
                if (Settings.TraceSwitch.TraceError)
                    Trace.WriteLine(ex.Message);

                recursiveCall++;
                SynchronizeContactList();
                return;
            }

            AddressBook += Deltas;

            if (AddressBook.AddressbookLastChange != DateTime.MinValue)
            {
                SetDefaults(true);
            }
            else
            {
                msRequest(
                    "Initial",
                    delegate
                    {
                        abRequest("Initial",
                            delegate
                            {
                                SetDefaults(true);
                            }
                        );
                    }
                );
            }
        }

        private void SetDefaults(bool sendinitialADL)
        {
            // Reset
            recursiveCall = 0;

            // Set privacy settings and roam property
            NSMessageHandler.Owner.SetPrivacy((AddressBook.MyProperties["blp"] == "1") ? PrivacyMode.AllExceptBlocked : PrivacyMode.NoneButAllowed);
            NSMessageHandler.Owner.SetNotifyPrivacy((AddressBook.MyProperties["gtc"] == "1") ? NotifyPrivacy.PromptOnAdd : NotifyPrivacy.AutomaticAdd);
            NSMessageHandler.Owner.SetRoamLiveProperty((AddressBook.MyProperties["roamliveproperties"] == "1") ? RoamLiveProperty.Enabled : RoamLiveProperty.Disabled);

            AddressBook.Profile = NSMessageHandler.StorageService.GetProfile();
            AddressBook.Save(); // The first and the last AddressBook.Save()
            Deltas.Truncate();

            // Set display name, personal status and photo
            string mydispName = String.IsNullOrEmpty(AddressBook.Profile.DisplayName) ? NSMessageHandler.Owner.NickName : AddressBook.Profile.DisplayName;
            PersonalMessage pm = new PersonalMessage(AddressBook.Profile.PersonalMessage, MediaType.None, null);
            NSMessageHandler.Owner.SetName(mydispName);
            NSMessageHandler.Owner.SetPersonalMessage(pm);

            if (AddressBook.Profile.Photo != null && AddressBook.Profile.Photo.DisplayImage != null)
            {
                System.Drawing.Image fileImage = System.Drawing.Image.FromStream(AddressBook.Profile.Photo.DisplayImage);
                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = fileImage;
                NSMessageHandler.Owner.DisplayImage = displayImage;
            }

            // Send BLP
            NSMessageHandler.SetPrivacyMode(NSMessageHandler.Owner.Privacy);

            // Send ADL
            if (sendinitialADL)
            {
                string[] adls = ConstructLists(AddressBook.MembershipContacts.Values, true, MSNLists.None);

                initialADLcount = adls.Length;
                foreach (string payload in adls)
                {
                    NSPayLoadMessage message = new NSPayLoadMessage("ADL", payload);
                    NSMessageHandler.MessageProcessor.SendMessage(message);
                    initialADLs.Add(message.TransactionID);
                }
            }
        }

        internal bool ProcessADL(int transid)
        {
            if (initialADLs.Contains(transid))
            {
                initialADLs.Remove(transid);
                if (--initialADLcount <= 0)
                {
                    initialADLcount = 0;

                    NSMessageHandler.SetScreenName(NSMessageHandler.Owner.Name);
                    NSMessageHandler.SetPersonalMessage(NSMessageHandler.Owner.PersonalMessage);

                    NSMessageHandler.OnSignedIn();

                    if (!AddressBookSynchronized)
                    {
                        msRequest(
                            "Initial",
                            delegate
                            {
                                abRequest("Initial",
                                    delegate
                                    {
                                        abSynchronized = true;
                                        SetDefaults(false);

                                        // No contacts are sent so we are done synchronizing call the callback
                                        // MSNP8: New callback defined, SynchronizationCompleted.
                                        NSMessageHandler.OnSynchronizationCompleted(this, EventArgs.Empty);

                                        // Fire the ReverseAdded event (pending)
                                        lock (NSMessageHandler.ContactList.SyncRoot)
                                        {
                                            foreach (Contact pendingContact in NSMessageHandler.ContactList.Pending)
                                            {
                                                NSMessageHandler.OnReverseAdded(pendingContact);
                                            }
                                        }
                                    }
                                );
                            }
                        );
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
            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("No Contact Ticket")));
            }
            else
            {
                bool msdeltasOnly = false;
                DateTime serviceLastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);
                if (AddressBook.MembershipLastChange != DateTime.MinValue)
                {
                    msdeltasOnly = true;
                    serviceLastChange = AddressBook.MembershipLastChange;
                }

                SharingServiceBinding sharingService = CreateSharingService(partnerScenario);
                sharingService.FindMembershipCompleted += delegate(object sender, FindMembershipCompletedEventArgs e)
                {
                    sharingService = sender as SharingServiceBinding;
                    handleServiceHeader(sharingService.ServiceHeaderValue, false);

                    if (!e.Cancelled)
                    {
                        if (e.Error != null)
                        {
                            if (e.Error.Message.Contains("Address Book Does Not Exist"))
                            {
                                if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
                                {
                                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("FindMembership", new MSNPSharpException("No Contact Ticket")));
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

                FindMembershipRequestType request = new FindMembershipRequestType();
                request.deltasOnly = msdeltasOnly;
                request.lastChange = serviceLastChange;
                request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
                request.serviceFilter.Types = new ServiceFilterType[]
                {
                    ServiceFilterType.Messenger
                    /*ServiceFilterType.Invitation,
                    ServiceFilterType.SocialNetwork,
                    ServiceFilterType.Space,
                    ServiceFilterType.Profile
                     * */
                };
                sharingService.FindMembershipAsync(request, partnerScenario);
            }
        }

        /// <summary>
        /// Async Address book request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async ab request completed successfuly</param>
        private void abRequest(string partnerScenario, ABFindAllCompletedEventHandler onSuccess)
        {
            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABFindAll", new MSNPSharpException("No Contact Ticket")));
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
                abService.ABFindAllCompleted += delegate(object sender, ABFindAllCompletedEventArgs e)
                {
                    abService = sender as ABServiceBinding;
                    handleServiceHeader(abService.ServiceHeaderValue, true);
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

                ABFindAllRequestType request = new ABFindAllRequestType();
                request.deltasOnly = deltasOnly;
                request.lastChange = lastChange;
                request.dynamicItemLastChange = dynamicItemLastChange;
                request.dynamicItemView = "Gleam";

                abService.ABFindAllAsync(request, partnerScenario);
            }
        }

        internal SharingServiceBinding CreateSharingService(string partnerScenario)
        {
            NSMessageHandler.MSNTicket.RenewIfExpired(SSOTicketType.Contact);

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Proxy = WebProxy;
            sharingService.Url = "https://" + PreferredHost + "/abservice/SharingService.asmx";
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = applicationId;
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            sharingService.ABApplicationHeaderValue.BrandId = NSMessageHandler.MSNTicket.MainBrandID;
            sharingService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.SharingServiceCacheKey;
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;

            return sharingService;
        }

        internal ABServiceBinding CreateABService(string partnerScenario)
        {
            NSMessageHandler.MSNTicket.RenewIfExpired(SSOTicketType.Contact);

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = WebProxy;
            abService.Timeout = Int32.MaxValue;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            abService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.ABServiceCacheKey;
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;

            return abService;
        }

        internal string[] ConstructLists(Dictionary<ContactIdentifier, MembershipContactInfo>.ValueCollection contacts, bool initial, MSNLists lists)
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

            List<MembershipContactInfo> sortedContacts = new List<MembershipContactInfo>(contacts);
            sortedContacts.Sort(CompareContacts);

            int domaincontactcount = 0;
            string currentDomain = null;
            XmlElement domtelElement = null;

            foreach (MembershipContactInfo contact in sortedContacts)
            {
                String name;
                String domain;
                MSNLists sendlist = lists;
                String type = ((int)contact.Type).ToString();

                if (contact.Type == ClientType.PhoneMember)
                {
                    domain = String.Empty;
                    name = "tel:" + contact.Account;
                }
                else
                {
                    String[] usernameanddomain = contact.Account.Split('@');
                    domain = usernameanddomain[1];
                    name = usernameanddomain[0];
                }

                if (initial)
                {
                    sendlist = MSNLists.None;
                    lists = AddressBook.GetMSNLists(contact.Account, contact.Type);
                    if (NSMessageHandler.ContactList.GetContact(contact.Account, contact.Type).IsMessengerUser)
                        sendlist |= MSNLists.ForwardList;
                    if ((lists & MSNLists.AllowedList) == MSNLists.AllowedList)
                        sendlist |= MSNLists.AllowedList;
                    else if ((lists & MSNLists.BlockedList) == MSNLists.BlockedList)
                        sendlist |= MSNLists.BlockedList;
                }

                if (sendlist != MSNLists.None && type != "0")
                {
                    if (currentDomain != domain)
                    {
                        currentDomain = domain;
                        domaincontactcount = 0;

                        if (contact.Type == ClientType.PhoneMember)
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
                    if (contact.Type != ClientType.PhoneMember)
                    {
                        contactElement.SetAttribute("t", type);
                    }
                    domtelElement.AppendChild(contactElement);
                    domaincontactcount++;
                }
                //domaincontactcount > 100 will leads to bug if it's less than 100 and mlElement.OuterXml.Length > 7300, it's possible...someone just report this problem..
                if (/*domaincontactcount > 100 &&*/ mlElement.OuterXml.Length > 7300)
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

        private static int CompareContacts(MembershipContactInfo x, MembershipContactInfo y)
        {
            if (x.Account == null)
                return 1;

            else if (y.Account == null)
                return -1;

            string xContact, yContact;

            if (x.Account.IndexOf("@") == -1)
                xContact = x.Account;
            else
                xContact = x.Account.Substring(x.Account.IndexOf("@") + 1);

            if (y.Account.IndexOf("@") == -1)
                yContact = y.Account;
            else
                yContact = y.Account.Substring(y.Account.IndexOf("@") + 1);

            return String.Compare(xContact, yContact, true, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Contact & Group Operations

        #region Add Contact

        private void AddNonPendingContact(string account, ClientType ct, string invitation)
        {
            // Query other networks and add as new contact if available
            if (ct == ClientType.PassportMember)
            {
                string fqypayload = "<ml l=\"2\"><d n=\"{d}\"><c n=\"{n}\" /></d></ml>";
                fqypayload = fqypayload.Replace("{d}", account.Split(("@").ToCharArray())[1]);
                fqypayload = fqypayload.Replace("{n}", account.Split(("@").ToCharArray())[0]);
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
                    contact.SetGuid(new Guid(e.Result.ABContactAddResult.guid));
                    contact.NSMessageHandler = NSMessageHandler;

                    // Add to AL
                    if (ct == ClientType.PassportMember)
                    {
                        // without membership
                        SerializableDictionary<ContactIdentifier, MembershipContactInfo> contacts = new SerializableDictionary<ContactIdentifier, MembershipContactInfo>();
                        contacts.Add(new ContactIdentifier(account, ct), new MembershipContactInfo(account, ct));
                        string payload = ConstructLists(contacts.Values, false, MSNLists.AllowedList)[0];
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                        contact.AddToList(MSNLists.AllowedList);
                    }
                    else
                    {
                        // with membership
                        contact.OnAllowedList = true;
                        contact.AddToList(MSNLists.AllowedList);
                    }

                    // Add to Forward List
                    contact.OnForwardList = true;
                    System.Threading.Thread.Sleep(100);

                    // Get all information. LivePending will be Live :)
                    abRequest(
                        "ContactSave",
                        delegate
                        {
                            contact = NSMessageHandler.ContactList.GetContact(contact.Mail, contact.ClientType);
                            NSMessageHandler.OnContactAdded(this, new ListMutateEventArgs(contact, MSNLists.AllowedList | MSNLists.ForwardList));
                        });
                }
            );
        }

        private void AddPendingContact(Contact contact)
        {
            // Delete PL with "ContactMsgrAPI"
            RemoveContactFromList(contact, MSNLists.PendingList, null);
            System.Threading.Thread.Sleep(200);

            // ADD contact to AB with "ContactMsgrAPI"
            AddNewOrPendingContact(
                contact.Mail,
                true,
                String.Empty,
                contact.ClientType,
                delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    contact.SetGuid(new Guid(e.Result.ABContactAddResult.guid));
                    contact.NSMessageHandler = NSMessageHandler;

                    // FL
                    contact.OnForwardList = true;
                    System.Threading.Thread.Sleep(100);

                    // Add RL membership with "ContactMsgrAPI"
                    AddContactToList(contact,
                        MSNLists.ReverseList,
                        delegate
                        {
                            // AL: Extra work for EmailMember: Add allow membership
                            if (ClientType.EmailMember == contact.ClientType)
                            {
                                AddContactToList(contact, MSNLists.AllowedList, null);
                                System.Threading.Thread.Sleep(100);
                            }
                            else
                            {
                                // ADL AL without membership, so the user can see our status...
                                SerializableDictionary<ContactIdentifier, MembershipContactInfo> contacts = new SerializableDictionary<ContactIdentifier, MembershipContactInfo>();
                                contacts.Add(new ContactIdentifier(contact.Mail, contact.ClientType), new MembershipContactInfo(contact.Mail, contact.ClientType));
                                string payload = ConstructLists(contacts.Values, false, MSNLists.AllowedList)[0];

                                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                                contact.AddToList(MSNLists.AllowedList);

                                System.Threading.Thread.Sleep(100);
                            }

                            // abrequest(ContactMsgrAPI)
                            abRequest(
                                "ContactMsgrAPI",
                                delegate
                                {
                                    contact = NSMessageHandler.ContactList.GetContact(contact.Mail, contact.ClientType);
                                    NSMessageHandler.OnContactAdded(this, new ListMutateEventArgs(contact, MSNLists.AllowedList | MSNLists.ForwardList));
                                });
                        }
                    );
                }
           );
        }

        private void AddNewOrPendingContact(string account, bool pending, string invitation, ClientType network, ABContactAddCompletedEventHandler onSuccess)
        {
            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactAdd", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService(pending ? "ContactMsgrAPI" : "ContactSave");
            abService.ABContactAddCompleted += delegate(object service, ABContactAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    if (onSuccess != null)
                    {
                        onSuccess(service, e);
                    }
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("AddContact", e.Error));
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
                    request.contacts[0].contactInfo.contactType = contactInfoTypeContactType.LivePending;
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
                    request.options.EnableAllowListManagement = true;
                    break;

                case ClientType.EmailMember:
                    request.contacts[0].contactInfo.emails = new contactEmailType[] { new contactEmailType() };
                    request.contacts[0].contactInfo.emails[0].contactEmailType1 = ContactEmailTypeType.Messenger2;
                    request.contacts[0].contactInfo.emails[0].email = account;
                    request.contacts[0].contactInfo.emails[0].isMessengerEnabled = true;
                    request.contacts[0].contactInfo.emails[0].Capability = "32";
                    request.contacts[0].contactInfo.emails[0].propertiesChanged = "Email IsMessengerEnabled Capability";
                    break;

                case ClientType.PhoneMember:
                    request.contacts[0].contactInfo.phones = new contactPhoneType[] { new contactPhoneType() };
                    request.contacts[0].contactInfo.phones[0].contactPhoneType1 = ContactPhoneTypeType.ContactPhoneMobile;
                    request.contacts[0].contactInfo.phones[0].number = account;
                    request.contacts[0].contactInfo.phones[0].isMessengerEnabled = true;
                    request.contacts[0].contactInfo.phones[0].propertiesChanged = "Number IsMessengerEnabled";
                    break;
            }

            abService.ABContactAddAsync(request, new object());
        }

        /// <summary>
        /// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
        /// </summary>
        /// <param name="account">An e-mail adress to add</param>
        public virtual void AddNewContact(string account)
        {
            AddNewContact(account, ClientType.PassportMember, String.Empty);
        }

        /// <summary>
        /// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
        /// </summary>
        /// <param name="account">An e-mail adress to add</param>
        /// <param name="network">The service type of target contact</param>
        /// <param name="invitation"></param>
        public void AddNewContact(string account, ClientType network, string invitation)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);

            if (NSMessageHandler.ContactList.HasContact(account, network))
            {
                Contact contact = NSMessageHandler.ContactList.GetContact(account, network);
                if (contact.Guid != Guid.Empty)
                {
                    RemoveContactFromList(contact, MSNLists.PendingList, null);
                    contact.RemoveFromList(MSNLists.PendingList);
                    return;
                }
            }

            if (MSNLists.PendingList == (AddressBook.GetMSNLists(account, network) & MSNLists.PendingList))
            {
                AddPendingContact(NSMessageHandler.ContactList.GetContact(account, network));
            }
            else
            {
                AddNonPendingContact(account, network, invitation);
            }
        }

        #endregion

        #region RemoveContact

        /// <summary>
        /// Remove the specified contact from your forward and allow list. Note that remote contacts that are blocked remain blocked.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        public virtual void RemoveContact(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactDelete", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("Timer");
            abService.ABContactDeleteCompleted += delegate(object service, ABContactDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.NSMessageHandler = null;
                    NSMessageHandler.ContactList.Remove(contact.Mail);
                    AddressBook.AddressbookContacts.Remove(contact.Guid);
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

            abService.ABContactDeleteAsync(request, new object());
        }

        #endregion

        #region UpdateContact

        internal void UpdateContact(Contact contact, string displayName, bool isMessengerUser, string comment)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABContactUpdate", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService(isMessengerUser ? "ContactSave" : "Timer");
            abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    abRequest("ContactSave", null);
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(abService,
                        new ServiceOperationFailedEventArgs("UpdateContact", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            List<string> propertiesChanged = new List<string>();
            ABContactUpdateRequestType request = new ABContactUpdateRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid.ToString();
            request.contacts[0].contactInfo = new contactInfoType();

            if (isMessengerUser != contact.IsMessengerUser)
            {
                switch (contact.ClientType)
                {
                    case ClientType.PassportMember:
                        propertiesChanged.Add("IsMessengerUser");
                        request.contacts[0].contactInfo.isMessengerUser = isMessengerUser;
                        request.contacts[0].contactInfo.isMessengerUserSpecified = true;
                        break;

                    case ClientType.EmailMember:
                        propertiesChanged.Add("ContactEmail");
                        request.contacts[0].contactInfo.emails = new contactEmailType[] { new contactEmailType() };
                        request.contacts[0].contactInfo.emails[0].contactEmailType1 = ContactEmailTypeType.Messenger2;
                        request.contacts[0].contactInfo.emails[0].isMessengerEnabled = isMessengerUser;
                        request.contacts[0].contactInfo.emails[0].propertiesChanged = "IsMessengerEnabled";
                        break;

                    case ClientType.PhoneMember:
                        propertiesChanged.Add("ContactPhone");
                        request.contacts[0].contactInfo.phones = new contactPhoneType[] { new contactPhoneType() };
                        request.contacts[0].contactInfo.phones[0].contactPhoneType1 = ContactPhoneTypeType.ContactPhoneMobile;
                        request.contacts[0].contactInfo.phones[0].isMessengerEnabled = isMessengerUser;
                        request.contacts[0].contactInfo.phones[0].propertiesChanged = "IsMessengerEnabled";
                        break;
                }
            }

            if (displayName != String.Empty && displayName != contact.Name)
            {
                propertiesChanged.Add("Annotation");
                request.contacts[0].contactInfo.annotations = new Annotation[] { new Annotation() };
                request.contacts[0].contactInfo.annotations[0].Name = "AB.NickName";
                request.contacts[0].contactInfo.annotations[0].Value = displayName;
            }

            if (comment != null && comment != contact.Comment)
            {
                propertiesChanged.Add("Comment");
                request.contacts[0].contactInfo.comment = comment;
            }

            if (propertiesChanged.Count > 0)
            {
                request.contacts[0].propertiesChanged = String.Join(" ", propertiesChanged.ToArray());
                abService.ABContactUpdateAsync(request, new object());
            }
        }

        internal void UpdateMe()
        {
            Owner owner = NSMessageHandler.Owner;

            if (owner == null)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            PrivacyMode oldPrivacy = AddressBook.MyProperties["blp"] == "1" ? PrivacyMode.AllExceptBlocked : PrivacyMode.NoneButAllowed;
            NotifyPrivacy oldNotify = AddressBook.MyProperties["gtc"] == "1" ? NotifyPrivacy.PromptOnAdd : NotifyPrivacy.AutomaticAdd;
            RoamLiveProperty oldRoaming = AddressBook.MyProperties["roamliveproperties"] == "1" ? RoamLiveProperty.Enabled : RoamLiveProperty.Disabled;
            List<Annotation> annos = new List<Annotation>();

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

            if (annos.Count > 0 && NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                ABServiceBinding abService = CreateABService("PrivacyApply");
                abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
                {
                    handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                    if (!e.Cancelled && e.Error == null)
                    {
                        if (Settings.TraceSwitch.TraceVerbose)
                            Trace.WriteLine("UpdateMe completed.");

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
                request.contacts[0].contactInfo.contactType = contactInfoTypeContactType.Me;
                request.contacts[0].contactInfo.contactTypeSpecified = true;
                request.contacts[0].contactInfo.annotations = annos.ToArray();
                request.contacts[0].propertiesChanged = "Annotation";
                abService.ABContactUpdateAsync(request, new object());
            }
        }

        #endregion

        #region AddContactGroup & RemoveContactGroup & RenameGroup

        /// <summary>
        /// Send a request to the server to add a new contactgroup.
        /// </summary>
        /// <param name="groupName">The name of the group to add</param>
        internal virtual void AddContactGroup(string groupName)
        {
            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupAdd", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(groupName, e.Result.ABGroupAddResult.guid, NSMessageHandler));
                    NSMessageHandler.OnContactGroupAdded(this, new ContactGroupEventArgs((ContactGroup)NSMessageHandler.ContactGroups[e.Result.ABGroupAddResult.guid]));
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

            abService.ABGroupAddAsync(request, new object());
        }

        /// <summary>
        /// Send a request to the server to remove a contactgroup. Any contacts in the group will also be removed from the forward list.
        /// </summary>
        /// <param name="contactGroup">The group to remove</param>
        internal virtual void RemoveContactGroup(ContactGroup contactGroup)
        {
            foreach (Contact cnt in NSMessageHandler.ContactList.All)
            {
                if (cnt.ContactGroups.Contains(contactGroup))
                {
                    throw new InvalidOperationException("Target group not empty, please remove all contacts form the group first.");
                }
            }

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupDelete", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("Timer");
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    NSMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                    AddressBook.Groups.Remove(new Guid(contactGroup.Guid));
                    NSMessageHandler.OnContactGroupRemoved(this, new ContactGroupEventArgs(contactGroup));
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

            abService.ABGroupDeleteAsync(request, new object());
        }


        /// <summary>
        /// Set the name of a contact group
        /// </summary>
        /// <param name="group">The contactgroup which name will be set</param>
        /// <param name="newGroupName">The new name</param>
        public virtual void RenameGroup(ContactGroup group, string newGroupName)
        {
            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupUpdate", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupUpdateCompleted += delegate(object service, ABGroupUpdateCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
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
            request.groups[0].propertiesChanged = "GroupName";
            request.groups[0].groupInfo = new groupInfoType();
            request.groups[0].groupInfo.name = newGroupName;

            abService.ABGroupUpdateAsync(request, new object());
        }

        #endregion

        #region AddContactToGroup & RemoveContactFromGroup

        public virtual void AddContactToGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactAdd", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupContactAddCompleted += delegate(object service, ABGroupContactAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
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

            abService.ABGroupContactAddAsync(request, new object());
        }

        public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("ABGroupContactDelete", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            ABServiceBinding abService = CreateABService("GroupSave");
            abService.ABGroupContactDeleteCompleted += delegate(object service, ABGroupContactDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
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
        internal virtual void AddContactToList(Contact contact, MSNLists list, EventHandler onSuccess)
        {
            if (list == MSNLists.PendingList) //this causes disconnect 
                return;

            // check whether the update is necessary
            if (contact.HasLists(list))
                return;

            SerializableDictionary<ContactIdentifier, MembershipContactInfo> contacts = new SerializableDictionary<ContactIdentifier, MembershipContactInfo>();
            contacts.Add(new ContactIdentifier(contact.Mail, contact.ClientType), new MembershipContactInfo(contact.Mail, contact.ClientType));
            string payload = ConstructLists(contacts.Values, false, list)[0];

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

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("AddMember", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            SharingServiceBinding sharingService = CreateSharingService((list == MSNLists.ReverseList) ? "ContactMsgrAPI" : "BlockUnblock");
            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member already exists"))
                    {
                        OnServiceOperationFailed(sharingService, new ServiceOperationFailedEventArgs("AddContactToList", e.Error));
                        return;
                    }

                    contact.AddToList(list);
                    AddressBook.AddMemberhip(contact.Mail, contact.ClientType, GetMemberRole(list), 0); // 0: XXXXXX
                    if ((list & MSNLists.AllowedList) == MSNLists.AllowedList || (list & MSNLists.BlockedList) == MSNLists.BlockedList)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }

                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("AddMember completed: " + list);
                }
            };

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = new Service();
            foreach (Service srv in AddressBook.Services.Values)
            {
                if (srv.Type == ServiceFilterType.Messenger)
                    messengerService = srv;
            }
            addMemberRequest.serviceHandle.Id = messengerService.Id.ToString();
            addMemberRequest.serviceHandle.Type = ServiceFilterType.Messenger;

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
        internal virtual void RemoveContactFromList(Contact contact, MSNLists list, EventHandler onSuccess)
        {
            if (list == MSNLists.ReverseList) //this causes disconnect
                return;

            // check whether the update is necessary
            if (!contact.HasLists(list))
                return;

            SerializableDictionary<ContactIdentifier, MembershipContactInfo> contacts = new SerializableDictionary<ContactIdentifier, MembershipContactInfo>();
            contacts.Add(new ContactIdentifier(contact.Mail, contact.ClientType), new MembershipContactInfo(contact.Mail, contact.ClientType));
            string payload = ConstructLists(contacts.Values, false, list)[0];

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

            if (!NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Contact))
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("DeleteMember", new MSNPSharpException("No Contact Ticket")));
                return;
            }

            SharingServiceBinding sharingService = CreateSharingService((list == MSNLists.PendingList) ? "ContactMsgrAPI" : "BlockUnblock");
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled)
                {
                    if (null != e.Error && false == e.Error.Message.Contains("Member does not exist"))
                    {
                        OnServiceOperationFailed(sharingService, new ServiceOperationFailedEventArgs("RemoveContactFromList", e.Error));
                        return;
                    }

                    contact.RemoveFromList(list);
                    AddressBook.RemoveMemberhip(contact.Mail, contact.ClientType, GetMemberRole(list));

                    if ((list & MSNLists.AllowedList) == MSNLists.AllowedList || (list & MSNLists.BlockedList) == MSNLists.BlockedList)
                    {
                        NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("RML", payload));
                    }

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }

                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("DeleteMember completed: " + list);
                }
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = new Service();
            foreach (Service srv in AddressBook.Services.Values)
            {
                if (srv.Type == ServiceFilterType.Messenger)
                    messengerService = srv;
            }
            deleteMemberRequest.serviceHandle.Id = messengerService.Id.ToString();   //Always set to 0 ??
            deleteMemberRequest.serviceHandle.Type = ServiceFilterType.Messenger;

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
            sharingService.DeleteMemberAsync(deleteMemberRequest, new object());
        }

        #endregion

        #region BlockContact & UnBlockContact

        public virtual void BlockContact(Contact contact)
        {
            if (contact.OnAllowedList)
            {
                RemoveContactFromList(
                    contact,
                    MSNLists.AllowedList,
                    delegate
                    {
                        if (!contact.OnBlockedList)
                            AddContactToList(contact, MSNLists.BlockedList, null);
                    }
                );

                // wait some time before sending the other request. If not we may block before we removed
                // from the allow list which will give an error
                System.Threading.Thread.Sleep(100);
            }

            if (!contact.OnBlockedList)
                AddContactToList(contact, MSNLists.BlockedList, null);
        }

        /// <summary>
        /// Unblock this contact. After this you are able to receive messages from this contact. This contact
        /// will be removed from your blocked list and placed in your allowed list.
        /// </summary>
        /// <param name="contact">Contact to unblock</param>
        public virtual void UnBlockContact(Contact contact)
        {
            if (contact.OnBlockedList)
            {
                AddContactToList(
                    contact,
                    MSNLists.AllowedList,
                    delegate
                    {
                        RemoveContactFromList(contact, MSNLists.BlockedList, null);
                    }
                );

                System.Threading.Thread.Sleep(100);
                RemoveContactFromList(contact, MSNLists.BlockedList, null);
            }
        }


        #endregion

        #endregion

        #region DeleteRecordFile & handleServiceHeader

        /// <summary>
        /// Delete the record file that contains the contactlist of owner.
        /// </summary>
        public virtual void DeleteRecordFile()
        {
            if (NSMessageHandler.Owner != null && NSMessageHandler.Owner.Mail != null)
            {
                string addressbookFile = Path.GetFullPath(@".\") + NSMessageHandler.Owner.Mail.GetHashCode() + ".mcl";
                if (File.Exists(addressbookFile))
                {
                    File.SetAttributes(addressbookFile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(addressbookFile);
                }

                string deltasResultFile = Path.GetFullPath(@".\") + NSMessageHandler.Owner.Mail.GetHashCode() + "d" + ".mcl";
                if (File.Exists(deltasResultFile))
                {
                    File.SetAttributes(deltasResultFile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(deltasResultFile);
                }
            }
        }

        internal void handleServiceHeader(ServiceHeader sh, bool isABServiceBinding)
        {
            if (null != sh)
            {
                if (sh.CacheKeyChanged)
                {
                    if (isABServiceBinding)
                    {
                        NSMessageHandler.MSNTicket.ABServiceCacheKey = sh.CacheKey;
                    }
                    else
                    {
                        NSMessageHandler.MSNTicket.SharingServiceCacheKey = sh.CacheKey;
                    }
                }

                if (!String.IsNullOrEmpty(sh.PreferredHostName))
                {
                    AddressBook.MyProperties["preferredhost"] = sh.PreferredHostName;
                }
            }
        }

        #endregion

        protected virtual MemberRole GetMemberRole(MSNLists list)
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
