#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

#define TRACE

namespace MSNPSharp
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Collections;
    using System.Diagnostics;
    using System.Collections.Generic;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    using System.Globalization;

    using MSNPSharp.MSNABSharingService;

    [Serializable()]
    public class ContactList
    {
        private NSMessageHandler nsHandler = null;
        internal XMLAddressBook AddressBook = null;
        private XMLMembershipList MemberShipList = null;
        private Owner owner = new Owner();
        private Dictionary<string, Contact> contacts = new Dictionary<string, Contact>(10);

        public Owner Owner
        {
            get
            {
                return owner;
            }
        }

        public ContactList(NSMessageHandler ns)
        {
            nsHandler = ns;
            Owner.NSMessageHandler = ns;
            Owner.ClientCapacities = ClientCapacities.CanHandleMSNC7 | ClientCapacities.CanMultiPacketMSG | ClientCapacities.CanReceiveWinks;
        }

        public bool HasContact(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            return contacts.ContainsKey(account);
        }

        public void CopyTo(Contact[] array, int index)
        {
            contacts.Values.CopyTo(array, index);
        }

        public void Clear()
        {
            contacts.Clear();
        }

        public void RemoveContact(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (HasContact(account))
            {
                contacts.Remove(account);
                AddressBook.Remove(account);
                AddressBook.Save();
                if (MemberShipList.ContainsKey(account))
                {
                    MSNLists list = MemberShipList.GetMSNLists(account);
                    if ((list & MSNLists.AllowedList) == MSNLists.AllowedList)
                    {
                        MemberShipList.MemberRoles[MemberRole.Allow].Remove(account);
                        MemberShipList.Save();
                    }
                }
            }
        }


        #region ListEnumerators

        public class ListEnumerator : IEnumerator
        {
            private IDictionaryEnumerator baseEnum;

            protected IDictionaryEnumerator BaseEnum
            {
                get
                {
                    return baseEnum;
                }
                set
                {
                    baseEnum = value;
                }
            }

            public ListEnumerator(IDictionaryEnumerator listEnum)
            {
                baseEnum = listEnum;
            }

            public virtual bool MoveNext()
            {
                return baseEnum.MoveNext();
            }

            Object IEnumerator.Current
            {
                get
                {
                    return baseEnum.Value;
                }
            }

            public Contact Current
            {
                get
                {
                    return (Contact)baseEnum.Value;
                }
            }

            public void Reset()
            {
                baseEnum.Reset();
            }

            public IEnumerator GetEnumerator()
            {
                return this;
            }
        }

        //filters the forward list contacts
        public class ForwardListEnumerator : ListEnumerator
        {
            public ForwardListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnForwardList)
                        return true;
                }

                return false;
            }
        }

        //filters the pending list contacts
        public class PendingListEnumerator : ListEnumerator
        {
            public PendingListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnPendingList)
                        return true;
                }

                return false;
            }
        }

        //filter the reverse list contacts
        public class ReverseListEnumerator : ListEnumerator
        {
            public ReverseListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the forward boolean
                    if (((Contact)BaseEnum.Value).OnReverseList)
                        return true;
                }
                return false;
            }
        }

        //filters the blocked list contacts
        public class BlockedListEnumerator : ListEnumerator
        {
            public BlockedListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    if (((Contact)BaseEnum.Value).Blocked)
                        return true;
                }
                return false;
            }
        }

        //filters the allowed list contacts
        public class AllowedListEnumerator : ListEnumerator
        {
            public AllowedListEnumerator(IDictionaryEnumerator listEnum)
                : base(listEnum)
            {
            }

            public override bool MoveNext()
            {
                while (BaseEnum.MoveNext())
                {
                    // filter on the allowed list
                    if (((Contact)BaseEnum.Value).OnAllowedList)
                        return true;
                }
                return false;
            }
        }

        #endregion

        #region Lists

        public ForwardListEnumerator Forward
        {
            get
            {
                return new ForwardListEnumerator(contacts.GetEnumerator());
            }
        }

        public PendingListEnumerator Pending
        {
            get
            {
                return new PendingListEnumerator(contacts.GetEnumerator());
            }
        }

        public ReverseListEnumerator Reverse
        {
            get
            {
                return new ReverseListEnumerator(contacts.GetEnumerator());
            }
        }

        public BlockedListEnumerator BlockedList
        {
            get
            {
                return new BlockedListEnumerator(contacts.GetEnumerator());
            }
        }

        public AllowedListEnumerator Allowed
        {
            get
            {
                return new AllowedListEnumerator(contacts.GetEnumerator());
            }
        }

        public ListEnumerator All
        {
            get
            {
                return new ListEnumerator(contacts.GetEnumerator());
            }
        }

        #endregion

        #region GetContact, GetContactByGuid, this[]

        public Contact GetContact(string account)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (contacts.ContainsKey(account))
            {
                return contacts[account];
            }
            else
            {
                Contact tmpContact = Factory.CreateContact();

                tmpContact.SetMail(account);
                tmpContact.SetName(account);

                contacts.Add(account, tmpContact);

                return contacts[account];
            }
        }

        public Contact GetContact(string account, string name)
        {
            account = account.ToLower(CultureInfo.InvariantCulture);
            if (contacts.ContainsKey(account))
                return contacts[account];
            else
            {
                Contact tmpContact = Factory.CreateContact();

                tmpContact.SetMail(account);
                tmpContact.SetName(name);

                contacts.Add(account, tmpContact);

                return contacts[account];
            }
        }

        public Contact GetContactByGuid(string guid)
        {
            foreach (Contact c in contacts.Values)
            {
                if (c.Guid == guid)
                    return c;
            }

            return null;
        }

        public Contact this[string account]
        {
            get
            {
                account = account.ToLower(CultureInfo.InvariantCulture);
                if (contacts.ContainsKey(account))
                    return contacts[account];

                return null;
            }
            set
            {
                account = account.ToLower(CultureInfo.InvariantCulture);
                contacts[account] = value;
            }
        }

        #endregion

        #region Contact & Group Operations

        /// <summary>
        /// Send the synchronize command to the server. This will rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// You <b>must</b> call this function before setting your initial status, otherwise you won't received online notifications of other clients.
        /// Please note that you can only synchronize a single time per session! (this is limited by the the msn-servers)
        /// </remarks>
        internal virtual void Synchronize()
        {
            string contactfile = Path.GetFullPath(@".\") + Convert.ToBase64String(Encoding.Unicode.GetBytes(Owner.Mail)).Replace("\\", "-") + ".mcl";
            MemberShipList = new XMLMembershipList(contactfile, true);
            AddressBook = new XMLAddressBook(contactfile, true);

            bool msdeltasOnly = false;
            DateTime serviceLastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            // sharingService.ABApplicationHeaderValue.CacheKey = ""; // XXX TODO GET Saved Sharing Service Iniproperties.SharingServiceCacheKey from Addressbok
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];

            FindMembershipRequestType request = new FindMembershipRequestType();
            request.serviceFilter = new FindMembershipRequestTypeServiceFilter();
            request.serviceFilter.Types = new ServiceFilterType[]
            { 
                ServiceFilterType.Messenger,
                ServiceFilterType.Invitation,
                ServiceFilterType.SocialNetwork,
                ServiceFilterType.Space,
                ServiceFilterType.Profile
            };
            request.deltasOnly = msdeltasOnly;
            request.lastChange = serviceLastChange;

            int findMembershipErrorCount = 0;
            sharingService.FindMembershipCompleted += new FindMembershipCompletedEventHandler(sharingService_FindMembershipCompleted);
            sharingService.FindMembershipAsync(request, findMembershipErrorCount);
        }

        private void sharingService_FindMembershipCompleted(object sender, FindMembershipCompletedEventArgs e)
        {
            // Cache key for Sharing service...
            handleCachekeyChange(((SharingServiceBinding)sender).ServiceHeaderValue, false);

            if (e.Cancelled)
                return;

            if (e.Error != null)
            {
                int findMembershipErrorCount = 1 + Convert.ToInt32(e.UserState);
                throw new MSNPSharpException(e.Error.Message, e.Error);
            }

            bool deltasOnly = false;
            DateTime lastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);

            ABServiceBinding abService = new ABServiceBinding();
            abService.Timeout = Int32.MaxValue;
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];

            ABFindAllRequestType request = new ABFindAllRequestType();
            request.deltasOnly = deltasOnly;
            request.lastChange = lastChange;

            abService.ABFindAllCompleted += new ABFindAllCompletedEventHandler(abService_ABFindAllCompleted);
            abService.ABFindAllAsync(request, e.Result.FindMembershipResult);
        }

        private void abService_ABFindAllCompleted(object sender, ABFindAllCompletedEventArgs e)
        {
            Trace.WriteLine("abService_ABFindAllCompleted");

            if (e.Cancelled || e.Error != null)
            {
                if (e.Error != null)
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                return;
            }

            ABServiceBinding abService = (ABServiceBinding)sender;
            FindMembershipResultType addressBook = (FindMembershipResultType)e.UserState;
            ABFindAllResultType forwardList = e.Result.ABFindAllResult;

            // 1: Cache key for AdressBook service...
            handleCachekeyChange(abService.ServiceHeaderValue, true);

            // 2: Groups
            Dictionary<string, GroupInfo> groups = new Dictionary<string, GroupInfo>(0); //For saving.
            if (null != forwardList.groups)
            {
                foreach (GroupType groupType in forwardList.groups)
                {
                    GroupInfo group = new GroupInfo();
                    group.Name = groupType.groupInfo.name;
                    group.Guid = groupType.groupId;
                    groups[group.Guid] = group;
                }
            }

            // 3: Memberships
            Dictionary<MemberRole, Dictionary<string, ContactInfo>> mShips = new Dictionary<MemberRole, Dictionary<string, ContactInfo>>();
            Dictionary<int, Service> services = new Dictionary<int, Service>(0);
            Service currentService = new Service();
            int messengerServiceId = 0;

            if (null != addressBook.Services)
            {
                foreach (ServiceType serviceType in addressBook.Services)
                {
                    currentService.LastChange = serviceType.LastChange;
                    currentService.Id = int.Parse(serviceType.Info.Handle.Id);
                    currentService.Type = serviceType.Info.Handle.Type;
                    currentService.ForeignId = serviceType.Info.Handle.ForeignId;
                    services[currentService.Id] = currentService;
                    if (serviceType.Info.Handle.Type == ServiceFilterType.Messenger)
                        messengerServiceId = int.Parse(serviceType.Info.Handle.Id);

                    DateTime serviceLastChange = serviceType.LastChange;

                    if (null != serviceType.Memberships)
                    {
                        foreach (Membership membership in serviceType.Memberships)
                        {
                            if (null != membership.Members)
                            {
                                MemberRole lst = membership.MemberRole;
                                if (!mShips.ContainsKey(lst))
                                {
                                    mShips[lst] = new Dictionary<string, ContactInfo>();
                                }

                                foreach (BaseMember bm in membership.Members)
                                {
                                    ContactInfo ci = new ContactInfo();
                                    ci.Type = ClientType.PassportMember;
                                    ci.LastChanged = bm.LastChanged;
                                    ci.MembershipId = Convert.ToInt32(bm.MembershipId);

                                    if (bm is PassportMember)
                                    {
                                        PassportMember pm = (PassportMember)bm;
                                        if (pm.IsPassportNameHidden)
                                        {
                                            // Occurs ArgumentNullException, PassportName = null
                                            continue;
                                        }
                                        ci.Account = pm.PassportName;
                                    }
                                    else if (bm is EmailMember)
                                    {
                                        ci.Account = ((EmailMember)bm).Email;
                                        ci.Type = ClientType.EmailMember;

                                    }

                                    if (!String.IsNullOrEmpty(bm.DisplayName))
                                        ci.DisplayName = bm.DisplayName;
                                    else
                                        ci.DisplayName = ci.Account;

                                    if (ci.Account != null && !mShips[lst].ContainsKey(ci.Account))
                                    {
                                        mShips[lst][ci.Account] = ci;

                                        if (!MemberShipList.ContainsKey(ci.Account) || MemberShipList[ci.Account].LastChanged.CompareTo(ci.LastChanged) < 0)
                                        {
                                            MemberShipList[ci.Account] = ci;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 4: Contact List
            Dictionary<string, ContactInfo> diccontactList = new Dictionary<string, ContactInfo>(0);
            Dictionary<string, string> props = AddressBook.MyProperties;

            if (null != forwardList.contacts)
            {
                foreach (ContactType contactType in forwardList.contacts)
                {
                    if (null != contactType.contactInfo)
                    {
                        ContactInfo ci = new ContactInfo();
                        ci.Guid = contactType.contactId;
                        ci.Account = contactType.contactInfo.passportName;
                        ci.DisplayName = contactType.contactInfo.displayName;
                        ci.LastChanged = contactType.lastChange;
                        ci.IsMessengerUser = contactType.contactInfo.isMessengerUser;

                        if (contactType.contactInfo.emails != null && ci.Account == null)
                        {
                            if (Enum.IsDefined(typeof(ClientCapacities), long.Parse(contactType.contactInfo.emails[0].Capability)))
                            {
                                ci.Capability = (ClientCapacities)long.Parse(contactType.contactInfo.emails[0].Capability);
                            }
                            ci.Type = ClientType.EmailMember;

                            ci.Account = contactType.contactInfo.emails[0].email;
                            ci.IsMessengerUser |= contactType.contactInfo.emails[0].isMessengerEnabled;
                            ci.DisplayName = String.IsNullOrEmpty(contactType.contactInfo.quickName) ? ci.Account : contactType.contactInfo.quickName;
                        }

                        if (ci.Account == null)
                            continue; // PassportnameHidden... Nothing to do...

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

                        if (contactType.contactInfo.contactType == contactInfoTypeContactType.Me)
                        {
                            if (props.Count == 0)   //The first time login, no settings saved
                            {
                                props["mbea"] = "0";
                                props["gtc"] = "1";
                                props["blp"] = "0";
                                props["roamliveproperties"] = "1";
                            }
                            props["displayname"] = ci.DisplayName;
                            if (null != contactType.contactInfo.annotations)
                            {
                                foreach (Annotation anno in contactType.contactInfo.annotations)
                                {
                                    string name = anno.Name;
                                    string value = anno.Value;
                                    name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                                    props[name] = value;
                                }
                            }
                        }

                        string[] groupids = new string[0];
                        if (contactType.contactInfo.groupIds != null)
                        {
                            groupids = contactType.contactInfo.groupIds;
                        }
                        ci.Groups = new List<string>(groupids);
                        if (diccontactList.ContainsKey(ci.Account))
                        {
                            if (diccontactList[ci.Account].LastChanged.CompareTo(ci.LastChanged) < 0)
                            {
                                diccontactList[ci.Account] = ci;
                            }
                        }
                        else
                        {
                            diccontactList.Add(ci.Account, ci);
                        }
                    }
                }
            }

            // 5: Combine all information and save them into a file.
            MemberShipList.CombineMemberRoles(mShips);
            MemberShipList.CombineService(services);

            AddressBook.LastChange = forwardList.ab.lastChange;
            AddressBook.DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;
            AddressBook.AddGroupRange(groups);
            AddressBook.AddRange(diccontactList);

            MemberShipList.Save();
            AddressBook.Save();

            // 6: Create Groups
            foreach (GroupInfo group in AddressBook.Groups.Values)
            {
                nsHandler.contactGroups.AddGroup(new ContactGroup(group.Name, group.Guid, nsHandler));
            }

            // 7: Add Contacts
            foreach (ContactInfo ci in MemberShipList.Values)
            {
                Contact contact = GetContact(ci.Account);
                contact.SetLists(MemberShipList.GetMSNLists(ci.Account));
                contact.NSMessageHandler = nsHandler;
                contact.SetClientType(MemberShipList[ci.Account].Type);

                if (AddressBook.ContainsKey(ci.Account))
                {
                    ContactInfo abci = AddressBook[ci.Account];
                    contact.SetGuid(abci.Guid);

                    foreach (string groupId in abci.Groups)
                    {
                        contact.ContactGroups.Add(nsHandler.contactGroups[groupId]);
                    }

                    if (abci.Type == ClientType.EmailMember)
                    {
                        contact.ClientCapacities = abci.Capability;
                    }

                    contact.SetName((abci.LastChanged.CompareTo(ci.LastChanged) < 0) ? ci.DisplayName : abci.DisplayName);

                    if (abci.IsMessengerUser)
                    {
                        contact.AddToList(MSNLists.ForwardList); //IsMessengerUser is only valid in AddressBook member
                    }
                }
            }

            foreach (ContactInfo ci in AddressBook.Values)  //The mail contact on your forward list
            {
                if (!MemberShipList.ContainsKey(ci.Account) && ci.Account != Owner.Mail)
                {
                    Contact contact = GetContact(ci.Account);
                    contact.SetLists(MemberShipList.GetMSNLists(ci.Account));
                    if (ci.Type == ClientType.EmailMember)
                        contact.ClientCapacities = ci.Capability;
                    contact.NSMessageHandler = nsHandler;
                    contact.SetClientType(ci.Type);
                    if (ci.IsMessengerUser)
                        contact.SetLists(MSNLists.ForwardList);
                }
            }

            // 8: Fire the ReverseAdd event
            if (MemberShipList.MemberRoles.ContainsKey(MemberRole.Pending))
            {
                foreach (ContactInfo ci in MemberShipList.MemberRoles[MemberRole.Pending].Values)
                {
                    //You just can't add these contacts into a contact group
                    Contact contact = GetContact(ci.Account);
                    contact.SetName(ci.DisplayName);
                    contact.AddToList(MSNLists.PendingList);
                    contact.NSMessageHandler = nsHandler;
                    nsHandler.OnReverseAdded(contact);
                }
            }

            nsHandler.SetPrivacyMode((AddressBook.MyProperties["blp"] == "1") ? PrivacyMode.AllExceptBlocked : PrivacyMode.NoneButAllowed);

            string[] adls = ConstructADLString(MemberShipList.Values, true, new MSNLists());
            nsHandler.initialadlcount = adls.Length;
            foreach (string str in adls)
            {
                NSMessage message = new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(str).ToString() });
                nsHandler.MessageProcessor.SendMessage(message);
                nsHandler.initialadls.Add(message.TransactionID);
                nsHandler.MessageProcessor.SendMessage(new NSMessage(str));
            }

            
        }

        internal string[] ConstructADLString(Dictionary<string, ContactInfo>.ValueCollection contacts, bool initial, MSNLists lists)
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

            List<ContactInfo> sortedContacts = new List<ContactInfo>(contacts);
            sortedContacts.Sort(DomainNameComparer.Default);
            string currentDomain = null;
            int domaincontactcount = 0;

            foreach (ContactInfo contact in sortedContacts)
            {
                MSNLists sendlist = lists;
                String[] usernameanddomain = contact.Account.Split('@');
                String type = ((int)contact.Type).ToString();
                String domain = usernameanddomain[1];
                String name = usernameanddomain[0];

                if (initial)
                {
                    sendlist = 0;
                    lists = MemberShipList.GetMSNLists(contact.Account);
                    if (AddressBook.ContainsKey(contact.Account))
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

        private class DomainNameComparer : IComparer<ContactInfo>
        {
            private DomainNameComparer()
            {
            }

            public static IComparer<ContactInfo> Default = new DomainNameComparer();
            public int Compare(ContactInfo x, ContactInfo y)
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


        /// <summary>
        /// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
        /// </summary>
        /// <param name="account">An e-mail adress to add</param>
        public virtual void AddNewContact(string account)
        {
            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "ContactSave";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABContactAddCompleted += delegate(object service, ABContactAddCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    Contact newcontact = GetContact(account);
                    newcontact.SetGuid(e.Result.ABContactAddResult.guid);
                    newcontact.NSMessageHandler = nsHandler;
                    nsHandler.OnContactAdded(this, new ListMutateEventArgs(newcontact, MSNLists.AllowedList | MSNLists.ForwardList));
                    //Add the new contact to our allowed and forward list,or we can't see its state
                    newcontact.OnAllowedList = true;
                    newcontact.OnForwardList = true;
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABContactAddRequestType request = new ABContactAddRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactInfo = new contactInfoType();
            request.contacts[0].contactInfo.contactType = contactInfoTypeContactType.LivePending;
            request.contacts[0].contactInfo.passportName = account;
            request.contacts[0].contactInfo.isMessengerUser = request.contacts[0].contactInfo.isMessengerUserSpecified = true;
            request.contacts[0].contactInfo.MessengerMemberInfo = new MessengerMemberInfo();
            request.contacts[0].contactInfo.MessengerMemberInfo.DisplayName = account;
            request.options = new ABContactAddRequestTypeOptions();
            request.options.EnableAllowListManagement = true;

            abService.ABContactAddAsync(request, new object());
        }

        /// <summary>
        /// Remove the specified contact from your forward and allow list. Note that remote contacts that are blocked remain blocked.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        public virtual void RemoveContact(Contact contact)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty.ToString())
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "Timer";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABContactDeleteCompleted += delegate(object service, ABContactDeleteCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.NSMessageHandler = null;
                    RemoveContact(contact.Mail);
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABContactDeleteRequestType request = new ABContactDeleteRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.contacts = new ContactIdType[] { new ContactIdType() };
            request.contacts[0].contactId = contact.Guid;

            abService.ABContactDeleteAsync(request, new object());
        }

        /// <summary>
        /// Send a request to the server to add a new contactgroup.
        /// </summary>
        /// <param name="groupName">The name of the group to add</param>
        internal virtual void AddContactGroup(string groupName)
        {
            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABGroupAddCompleted += delegate(object service, ABGroupAddCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    nsHandler.contactGroups.AddGroup(new ContactGroup(groupName, e.Result.ABGroupAddResult.guid, nsHandler));
                    nsHandler.OnContactGroupAdded(this, new ContactGroupEventArgs((ContactGroup)nsHandler.contactGroups[e.Result.ABGroupAddResult.guid]));
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
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
            foreach (Contact cnt in All)
            {
                if (cnt.ContactGroups.Contains(contactGroup))
                {
                    throw new InvalidOperationException("Target group not empty, please remove all contacts form the group first.");
                }
            }

            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "Timer";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABGroupDeleteCompleted += delegate(object service, ABGroupDeleteCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    nsHandler.ContactGroups.RemoveGroup(contactGroup);
                    AddressBook.Groups.Remove(contactGroup.Guid);
                    AddressBook.Save();
                    nsHandler.OnContactGroupRemoved(this, new ContactGroupEventArgs(contactGroup));
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
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

        public virtual void AddContactToGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty.ToString())
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABGroupContactAddCompleted += delegate(object service, ABGroupContactAddCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.AddContactToGroup(group);
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupContactAddRequestType request = new ABGroupContactAddRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid;

            abService.ABGroupContactAddAsync(request, new object());
        }

        public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            if (contact.Guid == null || contact.Guid == Guid.Empty.ToString())
                throw new InvalidOperationException("This is not a valid Messenger contact.");

            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABGroupContactDeleteCompleted += delegate(object service, ABGroupContactDeleteCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    contact.RemoveContactFromGroup(group);
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            ABGroupContactDeleteRequestType request = new ABGroupContactDeleteRequestType();
            request.abId = "00000000-0000-0000-0000-000000000000";
            request.groupFilter = new groupFilterType();
            request.groupFilter.groupIds = new string[] { group.Guid };
            request.contacts = new ContactType[] { new ContactType() };
            request.contacts[0].contactId = contact.Guid;

            abService.ABGroupContactDeleteAsync(request, new object());
        }

        /// <summary>
        /// Set the name of a contact group
        /// </summary>
        /// <param name="group">The contactgroup which name will be set</param>
        /// <param name="newGroupName">The new name</param>
        internal virtual void RenameGroup(ContactGroup group, string newGroupName)
        {
            ABServiceBinding abService = new ABServiceBinding();
            abService.ABApplicationHeaderValue = new ABApplicationHeader();
            abService.ABApplicationHeaderValue.IsMigration = false;
            abService.ABApplicationHeaderValue.PartnerScenario = "GroupSave";
            abService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.AddressBookCacheKey];
            abService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";

            abService.ABAuthHeaderValue = new ABAuthHeader();
            abService.ABAuthHeaderValue.ManagedGroupRequest = false;
            abService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            abService.ABGroupUpdateCompleted += delegate(object service, ABGroupUpdateCompletedEventArgs e)
            {
                handleCachekeyChange(((ABServiceBinding)service).ServiceHeaderValue, true);
                if (!e.Cancelled && e.Error == null)
                {
                    group.SetName(newGroupName);
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
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

        /// <summary>
        /// Send a request to the server to add this contact to a specific list.
        /// </summary>
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to place the contact in</param>
        internal virtual void AddContactToList(Contact contact, MSNLists list)
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
                nsHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                nsHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                contact.AddToList(list);
                return;
            }

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = "BlockUnblock";
            sharingService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.SharingServiceCacheKey];
            sharingService.ABApplicationHeaderValue.BrandId = nsHandler._Tickets[Iniproperties.BrandID];
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;
            sharingService.AddMemberCompleted += delegate(object service, AddMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleCachekeyChange(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled && e.Error == null)
                {
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("AddMember completed.");

                    contact.AddToList(list);
                    ContactInfo ci = MemberShipList[contact.Mail];
                    MemberShipList.MemberRoles[GetMemberRole(list)][ci.Account] = ci;
                    MemberShipList.Save();

                    nsHandler.MessageProcessor.SendMessage(new NSMessage("ADL", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            AddMemberRequestType addMemberRequest = new AddMemberRequestType();
            addMemberRequest.serviceHandle = new HandleType();

            Service messengerService = new Service();
            foreach (Service srv in MemberShipList.Services.Values)
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
            else
            {
                member = new EmailMember();
                EmailMember emailMember = member as EmailMember;
                emailMember.Email = contact.Mail;
            }
            memberShip.Members = new BaseMember[] { member };
            addMemberRequest.memberships = new Membership[] { memberShip };

            sharingService.AddMemberAsync(addMemberRequest, new object());
        }

        /// <summary>
        /// Send a request to the server to remove a contact from a specific list.
        /// </summary> 
        /// <param name="contact">The affected contact</param>
        /// <param name="list">The list to remove the contact from</param>
        internal virtual void RemoveContactFromList(Contact contact, MSNLists list)
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
                nsHandler.MessageProcessor.SendMessage(new NSMessage("RML", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                nsHandler.MessageProcessor.SendMessage(new NSMessage(payload));
                contact.RemoveFromList(list);
                return;
            }

            SharingServiceBinding sharingService = new SharingServiceBinding();
            sharingService.Timeout = Int32.MaxValue;
            sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
            sharingService.ABApplicationHeaderValue.ApplicationId = "996CDE1E-AA53-4477-B943-2BE802EA6166";
            sharingService.ABApplicationHeaderValue.IsMigration = false;
            sharingService.ABApplicationHeaderValue.PartnerScenario = "BlockUnblock";
            sharingService.ABApplicationHeaderValue.CacheKey = nsHandler._Tickets[Iniproperties.SharingServiceCacheKey];
            sharingService.ABApplicationHeaderValue.BrandId = nsHandler._Tickets[Iniproperties.BrandID];
            sharingService.ABAuthHeaderValue = new ABAuthHeader();
            sharingService.ABAuthHeaderValue.TicketToken = nsHandler._Tickets[Iniproperties.ContactTicket];
            sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;
            sharingService.DeleteMemberCompleted += delegate(object service, DeleteMemberCompletedEventArgs e)
            {
                // Cache key for Sharing service...
                handleCachekeyChange(((SharingServiceBinding)service).ServiceHeaderValue, false);
                if (!e.Cancelled && e.Error == null)
                {
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("DeleteMember completed.");

                    contact.RemoveFromList(list);
                    MemberShipList.MemberRoles[GetMemberRole(list)].Remove(contact.Mail);
                    MemberShipList.Save();

                    nsHandler.MessageProcessor.SendMessage(new NSMessage("RML", new string[] { Encoding.UTF8.GetByteCount(payload).ToString() }));
                    nsHandler.MessageProcessor.SendMessage(new NSMessage(payload));

                }
                else if (e.Error != null)
                {
                    throw new MSNPSharpException(e.Error.Message, e.Error);
                }
                ((IDisposable)service).Dispose();
                return;
            };

            DeleteMemberRequestType deleteMemberRequest = new DeleteMemberRequestType();
            deleteMemberRequest.serviceHandle = new HandleType();

            Service messengerService = new Service();
            foreach (Service srv in MemberShipList.Services.Values)
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
            memberShip.Members = new BaseMember[] { member };
            deleteMemberRequest.memberships = new Membership[] { memberShip };
            sharingService.DeleteMemberAsync(deleteMemberRequest, new object());
        }


        private void handleCachekeyChange(ServiceHeader sh, bool isABServiceBinding)
        {
            if (null != sh && sh.CacheKeyChanged)
            {
                if (isABServiceBinding)
                {
                    nsHandler._Tickets[Iniproperties.AddressBookCacheKey] = sh.CacheKey; // SAVE TO ADDRESS BOOK AS ab_cache_key
                }
                else
                {
                    nsHandler._Tickets[Iniproperties.SharingServiceCacheKey] = sh.CacheKey; // SAVE TO ADDRESS BOOK AS sh_cache_key
                }
            }
        }

        public virtual void BlockContact(Contact contact)
        {
            if (contact.OnAllowedList)
            {
                RemoveContactFromList(contact, MSNLists.AllowedList);

                // wait some time before sending the other request. If not we may block before we removed
                // from the allow list which will give an error
                System.Threading.Thread.Sleep(50);
            }

            if (!contact.OnBlockedList)
                AddContactToList(contact, MSNLists.BlockedList);
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
                AddContactToList(contact, MSNLists.AllowedList);
                RemoveContactFromList(contact, MSNLists.BlockedList);
            }
        }

        public virtual void DeleteRecordFile()
        {
            if (Owner != null && Owner.Mail != null)
            {
                string contactfile = Path.GetFullPath(@".\") + Convert.ToBase64String(Encoding.Unicode.GetBytes(Owner.Mail)).Replace("\\", "-") + ".mcl";
                if (File.Exists(contactfile))
                {
                    File.SetAttributes(contactfile, FileAttributes.Normal);  //By default, the file is hidden.
                    File.Delete(contactfile);
                }
            }
        }

        /// <summary>
        /// Translates the codes used by the MSN server to a MSNList object.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected internal MSNLists GetMSNList(string name)
        {
            switch (name)
            {
                case "AL":
                    return MSNLists.AllowedList;
                case "FL":
                    return MSNLists.ForwardList;
                case "BL":
                    return MSNLists.BlockedList;
                case "RL":
                    return MSNLists.ReverseList;
                case "PL":
                    return MSNLists.PendingList;
            }
            throw new MSNPSharpException("Unknown MSNList type");
        }

        protected internal MemberRole GetMemberRole(MSNLists list)
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


        #endregion


    }
};