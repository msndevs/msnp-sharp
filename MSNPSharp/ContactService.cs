using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.MSNABSharingService;
using System.IO;
using System.Xml;
using System.Globalization;
using MSNPSharp.IO;
using MSNPSharp.Core;
using System.Diagnostics;
using System.Net;
using MSNPSharp.MSNStorageService;

namespace MSNPSharp
{
    public class ContactService : MSNService
    {
        #region Fields

        private int recursiveCall = 0;
        private WebProxy webProxy = null;
        private NSMessageHandler nsMessageHandler = null;
        private const string applicationId = "CFE80F9D-180F-4399-82AB-413F33A1FA11";
        internal XMLContactList AddressBook = null;        

        #endregion

        public ContactService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }

            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };
        }

        #region Properties

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

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
            if (nsMessageHandler.AddressBookSynchronized)
            {
                if (Settings.TraceSwitch.TraceWarning)
                    Trace.WriteLine("SynchronizeContactList() was called, but the list has already been synchronized. Make sure the AutoSynchronize property is set to false in order to manually synchronize the contact list.", "NS11MessageHandler");
                return;
            }

            /* == T O D O ==
             * MAIN.mcl is main big file...
             * 
             * 1) When connecting(in SynchronizeContactList):
             *    If DELTA.mcl is exists, merge with MAINFILE and truncate delta...
             *    If not exists create DELTA.mcl...
             *    Now, DELTA.mcl have not any contact... Request "Initial" ms & ab and write to DELTA.mcl...
             *    When finished don't truncate, don't merge...
             * 
             * 2) On connected(after SynchronizeContactList)
             *    When we create a user or group write to DELTA.mcl. Don't truncate, don't merge...
             * 
             * 3) On disconnected: do nothing, When re-connected, go to step 1.
             * 
             * NOTE: DELTA.MCL can be serialized as array of FindMembershipResult & ABFindAllResult
             *       So we can call refreshMS(FindMembershipResult) and refreshAB(ABFindAllResult)
             */


            if (recursiveCall != 0)
            {
                DeleteRecordFile();
            }

            bool nocompress = true;
            string addressbookFile = Path.GetFullPath(@".\") + Convert.ToBase64String(Encoding.Unicode.GetBytes(nsMessageHandler.Owner.Mail)).Replace("\\", "-") + ".mcl";
            AddressBook = XMLContactList.LoadFromFile(addressbookFile, nocompress);

            msRequest(
                "Initial",
                delegate
                {
                    recursiveCall = 0;  //reset
                    abRequest("Initial",
                        delegate
                        {
                            // Get my profile (display name, personal status, photos etc.
                            try
                            {
                                StorageService storageService = new StorageService();
                                storageService.StorageApplicationHeaderValue = new StorageApplicationHeader();
                                storageService.StorageApplicationHeaderValue.ApplicationID = "Messenger Client 8.5";
                                storageService.StorageApplicationHeaderValue.Scenario = "Initial";
                                storageService.StorageUserHeaderValue = new StorageUserHeader();
                                storageService.StorageUserHeaderValue.Puid = 0;
                                storageService.StorageUserHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.StorageTicket];

                                GetProfileRequestType request = new GetProfileRequestType();
                                request.profileHandle = new Handle();
                                request.profileHandle.Alias = new Alias();
                                request.profileHandle.Alias.Name = AddressBook.MyProperties["cid"];
                                request.profileHandle.Alias.NameSpace = "MyCidStuff";
                                request.profileHandle.RelationshipName = "MyProfile";
                                request.profileAttributes = new profileAttributes();
                                request.profileAttributes.ExpressionProfileAttributes = new profileAttributesExpressionProfileAttributes();

                                GetProfileResponse response = storageService.GetProfile(request);

                                // Display name
                                if (!String.IsNullOrEmpty(response.GetProfileResult.ExpressionProfile.DisplayName))
                                {
                                    AddressBook.MyProperties["displayname"] = response.GetProfileResult.ExpressionProfile.DisplayName;
                                }

                                // Personal status
                                if (!String.IsNullOrEmpty(response.GetProfileResult.ExpressionProfile.PersonalStatus))
                                {
                                    AddressBook.MyProperties["personalmessage"] = response.GetProfileResult.ExpressionProfile.PersonalStatus;
                                }

                                AddressBook.Save();

                                // Display image
                                /*
                                if (null != response.GetProfileResult.ExpressionProfile.Photo)
                                {
                                    string url = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;
                                    if (!url.StartsWith("http"))
                                    {
                                        url = "http://storage.live.com" + url;
                                    }

                                    Uri uri = new Uri(url + "?" + System.Web.HttpUtility.UrlEncode(nsMessageHandler.Tickets[Iniproperties.StorageTicket].Split('&')[0]));

                                    HttpWebRequest fwr = (HttpWebRequest)WebRequest.Create(uri);

                                    Stream stream = fwr.GetResponse().GetResponseStream();
                                    MemoryStream ms = new MemoryStream();
                                    byte[] data = new byte[8192];
                                    int read;
                                    while ((read = stream.Read(data, 0, data.Length)) > 0)
                                    {
                                        ms.Write(data, 0, read);
                                    }
                                    stream.Close();

                                    System.Drawing.Image fileImage = System.Drawing.Image.FromStream(ms);
                                    DisplayImage displayImage = new DisplayImage();
                                    displayImage.Image = fileImage;

                                    nsMessageHandler.Owner.DisplayImage = displayImage;
                                }
                                */
                            }
                            catch
                            {
                            }
                            finally
                            {
                                nsMessageHandler.OnInitialSyncDone(ConstructADLString(AddressBook.MembershipContacts.Values, true, MSNLists.None));
                            }
                        }
                    );
                }
            );
        }

        /// <summary>
        /// Async membership request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async membership request completed successfuly</param>
        internal void msRequest(string partnerScenario, FindMembershipCompletedEventHandler onSuccess)
        {
            bool msdeltasOnly = false;
            DateTime serviceLastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);

            if (AddressBook.MembershipLastChange != DateTime.MinValue)
            {
                msdeltasOnly = true;
                serviceLastChange = AddressBook.MembershipLastChange;
            }

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Proxy = webProxy;
            sharingService.Url = "https://" + PreferredHost + "/abservice/SharingService.asmx";
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = applicationId;
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.SharingServiceCacheKey))
            {
                sharingService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.SharingServiceCacheKey];
            }
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            sharingService.FindMembershipCompleted += delegate(object sender, FindMembershipCompletedEventArgs e)
            {
                sharingService = sender as SharingServiceBinding;
                handleServiceHeader(sharingService.ServiceHeaderValue, false);

                if (e.Cancelled)
                    return;

                if (e.Error != null)
                {
                    if (recursiveCall == 0 && partnerScenario == "Initial")
                    {
                        recursiveCall++;
                        SynchronizeContactList();
                        return;
                    }
                    else
                    {
                        OnServiceOperationFailed(sender,
                                new ServiceOperationFailedEventArgs("SynchronizeContactList", e.Error));
                        return;
                    }
                }

                if (null != e.Result.FindMembershipResult)
                {
                    refreshMS(e.Result.FindMembershipResult);
                }

                if (onSuccess != null)
                    onSuccess(sharingService, e);

            };

            FindMembershipRequestType request = new FindMembershipRequestType();
            request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
            request.serviceFilter.Types = new ServiceFilterType[]
            { 
                ServiceFilterType.Messenger
            /*
                ServiceFilterType.Invitation,
                ServiceFilterType.SocialNetwork,
                ServiceFilterType.Space,
                ServiceFilterType.Profile
             * */

            };

            request.deltasOnly = msdeltasOnly;
            request.lastChange = serviceLastChange;

            sharingService.FindMembershipAsync(request, partnerScenario);
        }

        /// <summary>
        /// Async Address book request
        /// </summary>
        /// <param name="partnerScenario"></param>
        /// <param name="onSuccess">The delegate to be executed after async ab request completed successfuly</param>
        internal void abRequest(string partnerScenario, ABFindAllCompletedEventHandler onSuccess)
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

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Timeout = Int32.MaxValue;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = partnerScenario;
            if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.AddressBookCacheKey))
            {
                abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            }
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            abService.ABFindAllCompleted += delegate(object sender, ABFindAllCompletedEventArgs e)
            {
                abService = sender as ABServiceBinding;
                handleServiceHeader(abService.ServiceHeaderValue, true);
                string abpartnerScenario = e.UserState.ToString();

                if (e.Cancelled || e.Error != null)
                {
                    if (e.Error != null)
                    {
                        if (recursiveCall == 0 && abpartnerScenario == "Initial")
                        {
                            recursiveCall++;
                            SynchronizeContactList();
                            return;
                        }
                        else
                        {
                            OnServiceOperationFailed(sender,
                                new ServiceOperationFailedEventArgs("SynchronizeContactList", e.Error));
                            return;
                        }
                    }
                    return;
                }

                if (null != e.Result.ABFindAllResult)
                {
                    refreshAB(e.Result.ABFindAllResult);
                }

                if (onSuccess != null)
                    onSuccess(abService, e);
            };

            ABFindAllRequestType request = new ABFindAllRequestType();
            request.deltasOnly = deltasOnly;
            request.lastChange = lastChange;
            request.dynamicItemLastChange = dynamicItemLastChange;
            request.dynamicItemView = "Gleam";

            abService.ABFindAllAsync(request, partnerScenario);
        }

        private void refreshMS(FindMembershipResultType findMembership)
        {
            if (null != findMembership && null != findMembership.Services)
            {
                Dictionary<int, Service> services = new Dictionary<int, Service>(0);

                foreach (ServiceType serviceType in findMembership.Services)
                {
                    Service currentService = new Service();
                    currentService.LastChange = serviceType.LastChange;
                    currentService.Id = int.Parse(serviceType.Info.Handle.Id);
                    currentService.Type = serviceType.Info.Handle.Type;
                    currentService.ForeignId = serviceType.Info.Handle.ForeignId;
                    services[currentService.Id] = currentService;

                    if (null != serviceType.Memberships)
                    {
                        foreach (Membership membership in serviceType.Memberships)
                        {
                            if (null != membership.Members)
                            {
                                MemberRole memberrole = membership.MemberRole;
                                foreach (BaseMember bm in membership.Members)
                                {
                                    string account = null;
                                    ClientType type = ClientType.PassportMember;

                                    if (bm is PassportMember)
                                    {
                                        type = ClientType.PassportMember;
                                        PassportMember pm = (PassportMember)bm;
                                        if (!pm.IsPassportNameHidden)
                                        {
                                            account = pm.PassportName;
                                        }
                                    }
                                    else if (bm is EmailMember)
                                    {
                                        type = ClientType.EmailMember;
                                        account = ((EmailMember)bm).Email;
                                    }
                                    else if (bm is PhoneMember)
                                    {
                                        type = ClientType.PhoneMember;
                                        account = ((PhoneMember)bm).PhoneNumber;
                                    }

                                    if (account != null)
                                    {
                                        AddressBook.AddMemberhip(account, type, memberrole, Convert.ToInt32(bm.MembershipId));
                                        AddressBook.MembershipContacts[account].LastChanged = bm.LastChanged;
                                        AddressBook.MembershipContacts[account].DisplayName = String.IsNullOrEmpty(bm.DisplayName) ? account : bm.DisplayName;
                                    }
                                }
                            }
                        }
                    }
                }
                // Combine all membership information.
                AddressBook.CombineService(services);
                AddressBook.Save();
            }
        }

        internal void refreshAB(ABFindAllResultType forwardList)
        {
            if (null != forwardList.contacts)
            {
                foreach (ContactType contactType in forwardList.contacts)
                {
                    if (null != contactType.contactInfo)
                    {
                        ClientType type = ClientType.PassportMember;
                        string account = contactType.contactInfo.passportName;
                        string displayname = contactType.contactInfo.displayName;
                        bool ismessengeruser = contactType.contactInfo.isMessengerUser;

                        if (contactType.contactInfo.emails != null && account == null)
                        {
                            /* This is not related with ClientCapacities, I think :)
                            if (Enum.IsDefined(typeof(ClientCapacities), long.Parse(contactType.contactInfo.emails[0].Capability)))
                            {
                                ci.Capability = (ClientCapacities)long.Parse(contactType.contactInfo.emails[0].Capability);
                            }
                             * */

                            type = ClientType.EmailMember;
                            account = contactType.contactInfo.emails[0].email;
                            ismessengeruser |= contactType.contactInfo.emails[0].isMessengerEnabled;
                            displayname = String.IsNullOrEmpty(contactType.contactInfo.quickName) ? account : contactType.contactInfo.quickName;
                        }

                        if (account == null)
                            continue; // PassportnameHidden... Nothing to do...

                        if (contactType.fDeleted)
                        {
                            AddressBook.AddressbookContacts.Remove(new Guid(contactType.contactId));
                        }
                        else
                        {
                            AddressbookContactInfo ci = new AddressbookContactInfo(account, type, new Guid(contactType.contactId));
                            ci.DisplayName = displayname;
                            ci.IsMessengerUser = ismessengeruser;
                            ci.LastChanged = contactType.lastChange;

                            if (null != contactType.contactInfo.annotations)
                            {
                                foreach (Annotation anno in contactType.contactInfo.annotations)
                                {
                                    if (anno.Name == "AB.NickName" && anno.Value != null)
                                    {
                                        ci.DisplayName = anno.Value;
                                        break;
                                    }
                                }
                            }

                            if (contactType.contactInfo.groupIds != null)
                            {
                                ci.Groups = new List<string>(contactType.contactInfo.groupIds);
                            }

                            if (contactType.contactInfo.contactType == contactInfoTypeContactType.Me)
                            {
                                if (ci.DisplayName == nsMessageHandler.Owner.Mail && nsMessageHandler.Owner.Name != String.Empty)
                                {
                                    ci.DisplayName = nsMessageHandler.Owner.Name;
                                }

                                AddressBook.MyProperties["displayname"] = ci.DisplayName;
                                AddressBook.MyProperties["cid"] = contactType.contactInfo.CID;

                                if (null != contactType.contactInfo.annotations)
                                {
                                    foreach (Annotation anno in contactType.contactInfo.annotations)
                                    {
                                        string name = anno.Name;
                                        string value = anno.Value;
                                        name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                                        AddressBook.MyProperties[name] = value;
                                    }
                                }

                                if (!AddressBook.MyProperties.ContainsKey("mbea"))
                                    AddressBook.MyProperties["mbea"] = "0";

                                if (!AddressBook.MyProperties.ContainsKey("gtc"))
                                    AddressBook.MyProperties["gtc"] = "1";

                                if (!AddressBook.MyProperties.ContainsKey("blp"))
                                    AddressBook.MyProperties["blp"] = "0";

                                if (!AddressBook.MyProperties.ContainsKey("roamliveproperties"))
                                    AddressBook.MyProperties["roamliveproperties"] = "1";

                                if (!AddressBook.MyProperties.ContainsKey("personalmessage"))
                                    AddressBook.MyProperties["personalmessage"] = AddressBook.MyProperties["displayname"];
                            }

                            if (AddressBook.AddressbookContacts.ContainsKey(ci.Guid))
                            {
                                if (AddressBook.AddressbookContacts[ci.Guid].LastChanged.CompareTo(ci.LastChanged) < 0)
                                {
                                    AddressBook.AddressbookContacts[ci.Guid] = ci;
                                }
                            }
                            else
                            {
                                AddressBook.AddressbookContacts.Add(ci.Guid, ci);
                            }
                        }
                    }
                }
            }

            if (null != forwardList.groups)
            {
                foreach (GroupType groupType in forwardList.groups)
                {
                    if (groupType.fDeleted)
                    {
                        AddressBook.Groups.Remove(groupType.groupId);
                    }
                    else
                    {
                        GroupInfo group = new GroupInfo();
                        group.Name = groupType.groupInfo.name;
                        group.Guid = groupType.groupId;
                        AddressBook.Groups[group.Guid] = group;
                    }
                }
            }

            // Save Address Book
            AddressBook.AddressbookLastChange = forwardList.ab.lastChange;
            AddressBook.DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;
            AddressBook.Save();

            // Create Groups
            foreach (GroupInfo group in AddressBook.Groups.Values)
            {
                nsMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.Name, group.Guid, nsMessageHandler));
            }

            // Create the messenger contacts
            if (AddressBook.MembershipContacts.Count > 0)
            {
                foreach (MembershipContactInfo ci in AddressBook.MembershipContacts.Values)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(ci.Account, ci.DisplayName);
                    contact.SetLists(AddressBook.GetMSNLists(ci.Account));
                    contact.SetClientType(AddressBook.MembershipContacts[ci.Account].Type);
                    contact.NSMessageHandler = nsMessageHandler;

                    AddressbookContactInfo abci = AddressBook.Find(ci.Account, ci.Type);
                    if (abci != null)
                    {
                        contact.SetGuid(abci.Guid);
                        contact.SetIsMessengerUser(abci.IsMessengerUser);
                        if (abci.IsMessengerUser)
                        {
                            contact.AddToList(MSNLists.ForwardList); //IsMessengerUser is only valid in AddressBook member
                        }

                        foreach (string groupId in abci.Groups)
                        {
                            contact.ContactGroups.Add(nsMessageHandler.ContactGroups[groupId]);
                        }

                        if (abci.Type == ClientType.EmailMember)
                        {
                            contact.ClientCapacities = abci.Capability;
                        }

                        contact.SetName((abci.LastChanged.CompareTo(ci.LastChanged) < 0 && String.IsNullOrEmpty(abci.DisplayName)) ? ci.DisplayName : abci.DisplayName);
                    }
                }
            }

            // Create the mail contacts on your forward list
            foreach (AddressbookContactInfo abci in AddressBook.AddressbookContacts.Values)
            {
                if (!AddressBook.MembershipContacts.ContainsKey(abci.Account) && abci.Account != nsMessageHandler.Owner.Mail)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(abci.Account, abci.DisplayName);
                    contact.SetGuid(abci.Guid);
                    contact.SetIsMessengerUser(abci.IsMessengerUser);
                    contact.SetClientType(abci.Type);
                    contact.NSMessageHandler = nsMessageHandler;

                    foreach (string groupId in abci.Groups)
                    {
                        contact.ContactGroups.Add(nsMessageHandler.ContactGroups[groupId]);
                    }

                    if (abci.Type == ClientType.EmailMember)
                    {
                        contact.ClientCapacities = abci.Capability;
                    }
                }
            }
        }

        internal string[] ConstructADLString(Dictionary<string, MembershipContactInfo>.ValueCollection contacts, bool initial, MSNLists lists)
        {
            List<string> mls = new List<string>();

            StringBuilder ml = new StringBuilder();
            ml.Append("<ml" + (initial ? " l=\"1\">" : ">"));

            if (contacts == null || contacts.Count == 0)
            {
                ml.Append("</ml>");
                mls.Add(ml.ToString());
                return mls.ToArray();
            }

            List<MembershipContactInfo> sortedContacts = new List<MembershipContactInfo>(contacts);
            sortedContacts.Sort(DomainNameComparer.Default);
            string currentDomain = null;
            int domaincontactcount = 0;

            foreach (MembershipContactInfo contact in sortedContacts)
            {
                MSNLists sendlist = lists;
                String[] usernameanddomain = contact.Account.Split('@');
                String type = ((int)contact.Type).ToString();
                String domain = usernameanddomain[1];
                String name = usernameanddomain[0];

                if (initial)
                {
                    sendlist = 0;
                    lists = AddressBook.GetMSNLists(contact.Account);
                    AddressbookContactInfo abci = AddressBook.Find(contact.Account, contact.Type);
                    if (abci != null && abci.IsMessengerUser)
                        sendlist |= MSNLists.ForwardList;
                    if ((lists & MSNLists.AllowedList) == MSNLists.AllowedList)
                        sendlist |= MSNLists.AllowedList;
                    else if ((lists & MSNLists.BlockedList) == MSNLists.BlockedList)
                        sendlist |= MSNLists.BlockedList;
                }

                String strlist = ((int)sendlist).ToString();

                if (strlist != "0" && type != "0")
                {
                    if (currentDomain != domain)
                    {
                        currentDomain = domain;

                        if (domaincontactcount > 0)
                            ml.Append("</d>");

                        ml.Append("<d n=\"" + currentDomain + "\">");
                        domaincontactcount = 0;
                    }

                    ml.Append("<c n=\"" + name + "\" l=\"" + strlist + "\" t=\"" + type + "\"/>");
                    domaincontactcount++;
                }

                if (ml.Length > 7300)
                {
                    if (domaincontactcount > 0)
                        ml.Append("</d>");

                    ml.Append("</ml>");
                    mls.Add(ml.ToString());
                    ml.Length = 0;
                    ml.Append("<ml" + (initial ? " l=\"1\">" : ">"));

                    currentDomain = null;
                    domaincontactcount = 0;
                }
            }

            if (domaincontactcount > 0)
                ml.Append("</d>");

            ml.Append("</ml>");
            mls.Add(ml.ToString());
            return mls.ToArray();
        }

        private class DomainNameComparer : IComparer<MembershipContactInfo>
        {
            private DomainNameComparer()
            {
            }

            public static IComparer<MembershipContactInfo> Default = new DomainNameComparer();
            public int Compare(MembershipContactInfo x, MembershipContactInfo y)
            {
                if (x.Account == null)
                    return 1;
                else if (y.Account == null)
                    return -1;

                string xDomainName = x.Account.Substring(x.Account.IndexOf("@") + 1);
                string yDomainName = y.Account.Substring(y.Account.IndexOf("@") + 1);
                return String.Compare(xDomainName, yDomainName, true, CultureInfo.InvariantCulture);
            }
        }

        #endregion

        #region Contact & Group Operations

        #region Add Contact

        private void AddNonPendingContact(string account, ClientType ct, string invitation)
        {
            //1: Add contact to address book with "ContactSave"
            AddNewOrPendingContact(
                account,
                false,
                invitation,
                ct,
                delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(account);
                    contact.SetGuid(new Guid(e.Result.ABContactAddResult.guid));
                    contact.NSMessageHandler = nsMessageHandler;

                    //2: ADL AL without membership, so the user can see our status...
                    string payload = "<ml><d n=\"{d}\"><c n=\"{n}\" l=\"2\" t=\"1\" /></d></ml>";
                    payload = payload.Replace("{d}", account.Split(("@").ToCharArray())[1]);
                    payload = payload.Replace("{n}", account.Split(("@").ToCharArray())[0]);
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                    contact.AddToList(MSNLists.AllowedList);

                    // 3: ADL FL
                    contact.OnForwardList = true;
                    System.Threading.Thread.Sleep(100);

                    // 4: Send FQY command, and wait OnFQYReceived 
                    // then 5: abRequest("ContactSave")
                    payload = "<ml l=\"2\"><d n=\"{d}\"><c n=\"{n}\" /></d></ml>";
                    payload = payload.Replace("{d}", account.Split(("@").ToCharArray())[1]);
                    payload = payload.Replace("{n}", account.Split(("@").ToCharArray())[0]);
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("FQY", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                }
            );
        }

        private void AddPendingContact(Contact contact)
        {
            // 1: ADL AL without membership, so the user can see our status...
            string payload = "<ml><d n=\"{d}\"><c n=\"{n}\" l=\"2\" t=\"{t}\" /></d></ml>";
            payload = payload.Replace("{d}", contact.Mail.Split(("@").ToCharArray())[1]);
            payload = payload.Replace("{n}", contact.Mail.Split(("@").ToCharArray())[0]);
            payload = payload.Replace("{t}", ((int)contact.ClientType).ToString());
            nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
            nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));
            contact.AddToList(MSNLists.AllowedList);

            System.Threading.Thread.Sleep(100);

            // 2: ADD contact to AB with "ContactMsgrAPI"
            AddNewOrPendingContact(
                contact.Mail,
                true,
                String.Empty,
                contact.ClientType,
                delegate(object service, ABContactAddCompletedEventArgs e)
                {
                    contact.SetGuid(new Guid(e.Result.ABContactAddResult.guid));
                    contact.NSMessageHandler = nsMessageHandler;

                    // 3: ADL FL
                    contact.OnForwardList = true;
                    System.Threading.Thread.Sleep(100);

                    // 4: Delete pending membership with "ContactMsgrAPI"
                    RemoveContactFromList(contact, MSNLists.PendingList, null);
                    System.Threading.Thread.Sleep(100);

                    //5: Add Reverse membership with "ContactMsgrAPI"
                    AddContactToList(contact,
                        MSNLists.ReverseList,
                        delegate
                        {
                            // 6: Extra work for EmailMember: Add allow membership
                            if (ClientType.EmailMember == contact.ClientType)
                            {
                                AddContactToList(contact, MSNLists.AllowedList, null);
                                System.Threading.Thread.Sleep(100);
                            }

                            // 7: abrequest(ContactMsgrAPI)
                            abRequest(
                                "ContactMsgrAPI",
                                delegate
                                {
                                    contact = nsMessageHandler.ContactList.GetContact(contact.Mail);
                                    nsMessageHandler.OnContactAdded(this, new ListMutateEventArgs(contact, MSNLists.AllowedList | MSNLists.ForwardList));
                                });
                        }
                    );
                }
           );
        }

        private void AddNewOrPendingContact(string account, bool pending, string invitation, ClientType network, ABContactAddCompletedEventHandler onSuccess)
        {
            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = pending ? "ContactMsgrAPI" : "ContactSave";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
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
                    request.contacts[0].contactInfo.MessengerMemberInfo.DisplayName = nsMessageHandler.Owner.Name;
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

            if (AddressBook.Find(account, network) != null)
            {
                Contact contact = nsMessageHandler.ContactList.GetContact(account);
                RemoveContactFromList(contact, MSNLists.PendingList, null);
                contact.RemoveFromList(MSNLists.PendingList);
                return;
            }

            if (MSNLists.PendingList == (AddressBook.GetMSNLists(account) & MSNLists.PendingList))
            {
                AddPendingContact(nsMessageHandler.ContactList.GetContact(account));
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

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "Timer";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            abService.ABContactDeleteCompleted += delegate(object service, ABContactDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.NSMessageHandler = null;
                    nsMessageHandler.ContactList.Remove(contact.Mail);
                    AddressBook.AddressbookContacts.Remove(contact.Guid);
                    AddressBook.Save();
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

        internal void UpdateContact(Contact contact, string displayName, bool isMessengerUser)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty)
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = isMessengerUser ? "ContactSave" : "Timer";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            abService.ABContactUpdateCompleted += delegate(object service, ABContactUpdateCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.SetIsMessengerUser(isMessengerUser);
                    AddressBook.AddressbookContacts[contact.Guid].IsMessengerUser = contact.IsMessengerUser;
                    if (!String.IsNullOrEmpty(displayName))
                    {
                        contact.SetName(displayName);
                        AddressBook.AddressbookContacts[contact.Guid].DisplayName = displayName;
                    }                    
                    AddressBook.Save();
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
                }
            }

            if (displayName != String.Empty && displayName != contact.Name)
            {
                propertiesChanged.Add("Annotation");
                request.contacts[0].contactInfo.annotations = new Annotation[] { new Annotation() };
                request.contacts[0].contactInfo.annotations[0].Name = "AB.NickName";
                request.contacts[0].contactInfo.annotations[0].Value = displayName;
            }

            if (propertiesChanged.Count > 0)
            {
                request.contacts[0].propertiesChanged = String.Join(" ", propertiesChanged.ToArray());
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
            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    nsMessageHandler.ContactGroups.AddGroup(new ContactGroup(groupName, e.Result.ABGroupAddResult.guid, nsMessageHandler));
                    nsMessageHandler.OnContactGroupAdded(this, new ContactGroupEventArgs((ContactGroup)nsMessageHandler.ContactGroups[e.Result.ABGroupAddResult.guid]));
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
            foreach (Contact cnt in nsMessageHandler.ContactList.All)
            {
                if (cnt.ContactGroups.Contains(contactGroup))
                {
                    throw new InvalidOperationException("Target group not empty, please remove all contacts form the group first.");
                }
            }

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "Timer";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    nsMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                    AddressBook.Groups.Remove(contactGroup.Guid);
                    AddressBook.Save();
                    nsMessageHandler.OnContactGroupRemoved(this, new ContactGroupEventArgs(contactGroup));
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
            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
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

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
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

            ABServiceBinding abService = new ABServiceBinding();
            abService.Proxy = webProxy;
            abService.Url = "https://" + PreferredHost + "/abservice/abservice.asmx";
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = applicationId;

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
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

            string payload = "<ml><d n=\"{d}\"><c n=\"{n}\" t=\"" + ((int)contact.ClientType).ToString() + "\" l=\"{l}\" /></d></ml>";
            payload = payload.Replace("{d}", contact.Mail.Split(("@").ToCharArray())[1]);
            payload = payload.Replace("{n}", contact.Mail.Split(("@").ToCharArray())[0]);
            payload = payload.Replace("{l}", ((int)list).ToString());

            if (list == MSNLists.ForwardList)
            {
                nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                contact.AddToList(list);

                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }

                return;
            }

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Proxy = webProxy;
            sharingService.Url = "https://" + PreferredHost + "/abservice/SharingService.asmx";
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = applicationId;
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = (list == MSNLists.ReverseList) ? "ContactMsgrAPI" : "BlockUnblock";
            sharingService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.SharingServiceCacheKey];
            sharingService.ABApplicationHeaderValue.BrandId = nsMessageHandler.Tickets[Iniproperties.BrandID];
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;
            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled && e.Error == null)
                {
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("AddMember completed.");

                    contact.AddToList(list);

                    AddressBook.AddMemberhip(contact.Mail, contact.ClientType, GetMemberRole(list), 0); // 0: XXXXXX
                    AddressBook.Save();

                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(sharingService,
                        new ServiceOperationFailedEventArgs("AddContactToList", e.Error));
                }
                return;
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

            string payload = "<ml><d n=\"{d}\"><c n=\"{n}\" t=\"" + ((int)contact.ClientType).ToString() + "\" l=\"{l}\" /></d></ml>";
            payload = payload.Replace("{d}", contact.Mail.Split(("@").ToCharArray())[1]);
            payload = payload.Replace("{n}", contact.Mail.Split(("@").ToCharArray())[0]);
            payload = payload.Replace("{l}", ((int)list).ToString());

            if (list == MSNLists.ForwardList)
            {
                nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("RML", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                contact.RemoveFromList(list);
                if (onSuccess != null)
                {
                    onSuccess(this, EventArgs.Empty);
                }
                return;
            }

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Proxy = webProxy;
            sharingService.Url = "https://" + PreferredHost + "/abservice/SharingService.asmx";
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = applicationId;
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = (list == MSNLists.PendingList) ? "ContactMsgrAPI" : "BlockUnblock";
            sharingService.ABApplicationHeaderValue.CacheKey = nsMessageHandler.Tickets[Iniproperties.SharingServiceCacheKey];
            sharingService.ABApplicationHeaderValue.BrandId = nsMessageHandler.Tickets[Iniproperties.BrandID];
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.ContactTicket];
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleServiceHeader(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled && e.Error == null)
                {
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("DeleteMember completed.");

                    contact.RemoveFromList(list);

                    if (AddressBook.MembershipContacts.ContainsKey(contact.Mail))
                    {
                        AddressBook.RemoveMemberhip(contact.Mail, GetMemberRole(list));
                        AddressBook.Save();
                    }                    

                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage("RML", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsMessageHandler.MessageProcessor.SendMessage(new NSMessage(payload));

                    if (onSuccess != null)
                    {
                        onSuccess(this, EventArgs.Empty);
                    }
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(sharingService,
                        new ServiceOperationFailedEventArgs("RemoveContactFromList", e.Error));
                }
                return;
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
            else
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.Email = contact.Mail;
            }
            //member.MembershipId = MemberShipList.MemberRoles[memberShip.MemberRole][contact.Mail].MembershipId.ToString();

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

        public virtual void DeleteRecordFile()
        {
            if (nsMessageHandler.Owner != null && nsMessageHandler.Owner.Mail != null)
            {
                string addressbookFile = Path.GetFullPath(@".\") + Convert.ToBase64String(Encoding.Unicode.GetBytes(nsMessageHandler.Owner.Mail)).Replace("\\", "-") + ".mcl";
                if (File.Exists(addressbookFile))
                {
                    File.SetAttributes(addressbookFile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(addressbookFile);
                }
            }
        }

        private void handleServiceHeader(ServiceHeader sh, bool isABServiceBinding)
        {
            if (null != sh)
            {
                if (sh.CacheKeyChanged)
                {
                    if (isABServiceBinding)
                    {
                        nsMessageHandler.Tickets[Iniproperties.AddressBookCacheKey] = sh.CacheKey; // SAVE TO ADDRESS BOOK AS ab_cache_key
                    }
                    else
                    {
                        nsMessageHandler.Tickets[Iniproperties.SharingServiceCacheKey] = sh.CacheKey; // SAVE TO ADDRESS BOOK AS sh_cache_key
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
    }
};
