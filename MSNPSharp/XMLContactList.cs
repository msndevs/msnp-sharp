namespace MSNPSharp
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.IO;
    using MSNPSharp.IO;
    using System.Globalization;
    using MemberRole = MSNPSharp.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNABSharingService.ServiceFilterType;

    #region ContactInfo
    internal class ContactInfo
    {
        private string account;
        public string Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        private string guid;
        public string Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        private int membershipId;
        public int MembershipId
        {
            get
            {
                return membershipId;
            }
            set
            {
                membershipId = value;
            }
        }

        private bool isMessengerUser;
        public bool IsMessengerUser
        {
            get
            {
                return isMessengerUser;
            }
            set
            {
                isMessengerUser = value;
            }
        }

        private ClientType type = ClientType.PassportMember;
        public ClientType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }


        private string displayname;
        public string DisplayName
        {
            get
            {
                return displayname;
            }
            set
            {
                displayname = value;
            }
        }

        private DateTime lastchanged;
        public DateTime LastChanged
        {
            get
            {
                return lastchanged;
            }
            set
            {
                lastchanged = value;
            }
        }

        private ClientCapacities capability = 0;
        public ClientCapacities Capability
        {
            get
            {
                return capability;
            }
            set
            {
                capability = value;
            }
        }

        private List<string> groups = new List<string>(0);
        public List<string> Groups
        {
            get
            {
                return groups;
            }
            set
            {
                groups = value;
            }
        }

        public override string ToString()
        {
            string debugstr = String.Empty;
            if (account != null)
                debugstr += account + "  |  isMessengerUser:  " + IsMessengerUser.ToString();
            if (displayname != null)
                debugstr += "  |  diaplayName:  " + displayname;
            return debugstr;
        }
    }

    #endregion

    #region GroupInfo
    internal struct GroupInfo
    {
        private string guid;
        public string Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    #endregion

    #region Service
    internal struct Service
    {
        private int id;
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        private ServiceFilterType type;
        public ServiceFilterType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        private DateTime lastChange;
        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
            set
            {
                lastChange = value;
            }
        }

        private string foreignId;
        public string ForeignId
        {
            get
            {
                return foreignId;
            }
            set
            {
                foreignId = value;
            }
        }

        public override string ToString()
        {
            return Convert.ToString(Type);
        }
    }

    #endregion

    #region XMLMembershipList

    /// <summary>
    /// XML Membership List file maintainer
    /// </summary>
    internal class XMLMembershipList : XMLContactList
    {
        private string fileName = String.Empty;
        private Dictionary<int, Service> services = new Dictionary<int, Service>(0);
        private Dictionary<MemberRole, Dictionary<string, ContactInfo>> rolelists = new Dictionary<MemberRole, Dictionary<string, ContactInfo>>(0);

        public XMLMembershipList(string filename)
            : this(filename, false)
        {
        }

        public XMLMembershipList(string filename, bool nocompress)
            : base(nocompress)
        {
            fileName = filename;
            LoadFromFile(filename);
        }

        public Dictionary<int, Service> Services
        {
            get
            {
                return services;
            }
        }

        public Dictionary<MemberRole, Dictionary<string, ContactInfo>> MemberRoles
        {
            get
            {
                return rolelists;
            }
        }

        public MSNLists GetMSNLists(string account)
        {
            MSNLists contactlists = MSNLists.None;

            if (rolelists.ContainsKey(MemberRole.Allow) && rolelists[MemberRole.Allow].ContainsKey(account))
                contactlists |= MSNLists.AllowedList;

            if (rolelists.ContainsKey(MemberRole.Pending) && rolelists[MemberRole.Pending].ContainsKey(account))
                contactlists |= MSNLists.PendingList;

            if (rolelists.ContainsKey(MemberRole.Reverse) && rolelists[MemberRole.Reverse].ContainsKey(account))
                contactlists |= MSNLists.ReverseList;

            if (rolelists.ContainsKey(MemberRole.Block) && rolelists[MemberRole.Block].ContainsKey(account))
            {
                contactlists |= MSNLists.BlockedList;
                if ((contactlists & MSNLists.AllowedList) == MSNLists.AllowedList)
                    contactlists ^= MSNLists.AllowedList;
            }

            return contactlists;
        }

        public void CombineMemberRoles(Dictionary<MemberRole, Dictionary<string, ContactInfo>> memberroles)
        {
            foreach (MemberRole role in memberroles.Keys)
            {
                if (rolelists.ContainsKey(role))
                {
                    foreach (string account in memberroles[role].Keys)
                    {
                        if (rolelists[role].ContainsKey(account))
                        {
                            rolelists[role][account] = memberroles[role][account];
                        }
                        else
                        {
                            rolelists[role].Add(account, memberroles[role][account]);
                        }
                    }
                }
                else
                {
                    rolelists.Add(role, memberroles[role]);
                }
            }
        }

        /// <summary>
        /// Combine the new services with old ones and get Messenger service's lastchange property.
        /// </summary>
        /// <param name="serviceRange"></param>
        public void CombineService(Dictionary<int, Service> serviceRange)
        {
            foreach (Service service in serviceRange.Values)
                services[service.Id] = service;

            foreach (Service innerService in Services.Values)
                if (innerService.Type == ServiceFilterType.Messenger)
                    if (lastChange.CompareTo(innerService.LastChange) < 0)
                        lastChange = innerService.LastChange;

        }

        /// <summary>
        /// Load the contact list from a file.
        /// </summary>
        /// <param name="filename"></param>
        public override void LoadFromFile(string filename)
        {
            base.LoadFromFile(filename);
            this.Clear();
            XMLContactListParser parser = new XMLContactListParser(doc);
            parser.Parse();
            Add(parser.MembershipContactList);
            lastChange = parser.MembershipLastChange;
            services = parser.ServiceList;
            rolelists = parser.MemberShips;
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            base.LoadFromFile(filename);
            XmlNode membershipRoot = doc.GetElementsByTagName(XMLContactListTags.MembershipList.ToString())[0];
            if (membershipRoot == null)
                membershipRoot = CreateNode(XMLContactListTags.MembershipList.ToString(), null);

            membershipRoot.RemoveAll();
            membershipRoot.AppendChild(CreateNode(MembershipListChildNodes.LastChanged.ToString(), XmlConvert.ToString(
                lastChange, XmlDateTimeSerializationMode.RoundtripKind)));

            XmlNode serviceRoot = CreateNode(MembershipListChildNodes.Services.ToString(), null);
            XmlNode membersRoot = null;
            foreach (int serviceId in services.Keys)
            {
                XmlNode serviceNode = CreateNode(XMLContactListTags.Service.ToString(), null);
                serviceNode.AppendChild(CreateNode(ServiceChildNodes.Id.ToString(),
                    XmlConvert.ToString(serviceId)));
                serviceNode.AppendChild(CreateNode(ServiceChildNodes.Type.ToString(), Convert.ToString(services[serviceId].Type)));
                serviceNode.AppendChild(CreateNode(ServiceChildNodes.LastChange.ToString(),
                    XmlConvert.ToString(services[serviceId].LastChange,
                    XmlDateTimeSerializationMode.RoundtripKind)));
                XmlNode membersRootTmp = CreateNode(ServiceChildNodes.Memberships.ToString(), null);
                serviceNode.AppendChild(membersRootTmp);
                serviceRoot.AppendChild(serviceNode);
                if (services[serviceId].Type == ServiceFilterType.Messenger)
                    membersRoot = membersRootTmp;
            }
            membershipRoot.AppendChild(serviceRoot);

            if (membersRoot != null)
            {
                int[] roleArray = (int[])Enum.GetValues(typeof(MemberRole));
                foreach (int role in roleArray)
                {
                    if (rolelists.ContainsKey((MemberRole)role))
                    {
                        membersRoot.AppendChild(GetList((MemberRole)role));
                    }
                }
            }

            SaveToHiddenMCL(filename);
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        public override void Save()
        {
            Save(fileName);
        }

        private XmlNode GetList(MemberRole memberrole)
        {
            XmlNode listroot = CreateNode(XMLContactListTags.Membership.ToString(), null);
            listroot.AppendChild(CreateNode(MembershipsChildNodes.MemberRole.ToString(), memberrole.ToString()));
            XmlNode membersRoot = CreateNode(MembershipsChildNodes.Members.ToString(), null);
            foreach (ContactInfo cinfo in rolelists[memberrole].Values)
            {
                XmlNode memberNode = CreateNode(XMLContactListTags.Member.ToString(), null);
                if (cinfo != null/* && (cinfo.msnlist & list) == list*/)
                {
                    memberNode.AppendChild(CreateNode(MemberChildNodes.MembershipId.ToString(), cinfo.MembershipId.ToString()));
                    memberNode.AppendChild(CreateNode(MemberChildNodes.Account.ToString(), cinfo.Account));
                    memberNode.AppendChild(CreateNode(MemberChildNodes.DisplayName.ToString(), cinfo.DisplayName));
                    memberNode.AppendChild(CreateNode(MemberChildNodes.LastChanged.ToString(),
                        XmlConvert.ToString(cinfo.LastChanged, XmlDateTimeSerializationMode.RoundtripKind)));
                    memberNode.AppendChild(CreateNode(MemberChildNodes.Type.ToString(),
                        XmlConvert.ToString((int)cinfo.Type)));

                    membersRoot.AppendChild(memberNode);
                }
            }
            listroot.AppendChild(membersRoot);

            return listroot;
        }
    }

    #endregion

    #region XMLAddressBook

    /// <summary>
    /// XML ForwardList file maintainer
    /// </summary>
    internal class XMLAddressBook : XMLContactList
    {
        string fileName = String.Empty;
        DateTime dynamicItemLastChange;
        Dictionary<string, GroupInfo> groups = new Dictionary<string, GroupInfo>(0);
        Dictionary<string, string> myproperties = new Dictionary<string, string>(0);

        public XMLAddressBook(string filename)
            : this(filename, false)
        {
        }

        public XMLAddressBook(string filename, bool nocompress)
            : base(nocompress)
        {
            fileName = filename;
            LoadFromFile(filename);
        }

        public DateTime DynamicItemLastChange
        {
            get
            {
                return dynamicItemLastChange;
            }
            set
            {
                dynamicItemLastChange = value;
            }
        }

        public Dictionary<string, GroupInfo> Groups
        {
            get
            {
                return groups;
            }
        }

        public Dictionary<string, string> MyProperties
        {
            get
            {
                return myproperties;
            }
            set
            {
                myproperties = value;
            }
        }

        public override void LoadFromFile(string filename)
        {
            base.LoadFromFile(filename);
            this.Clear();
            XmlNode addressbookRoot = doc.GetElementsByTagName(XMLContactListTags.AddressBook.ToString())[0];
            if (addressbookRoot == null)
                addressbookRoot = CreateNode(XMLContactListTags.AddressBook.ToString(), null);

            XMLContactListParser parser = new XMLContactListParser(doc);
            parser.Parse();
            Add(parser.AddressBookContactList);
            LastChange = parser.AddressBookLastChange;
            dynamicItemLastChange = parser.DynamicItemLastChange;
            groups = parser.GroupList;

            XmlNodeList annos = doc.GetElementsByTagName(XMLContactListTags.Annotation.ToString());
            foreach (XmlNode node in annos)
            {
                string name = node.ChildNodes[0].InnerText;
                string value = node.ChildNodes[1].InnerText;
                name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                myproperties[name] = value;
            }
        }

        public override void Save(string filename)
        {
            base.LoadFromFile(filename);
            XmlNode addressbookRoot = doc.GetElementsByTagName(XMLContactListTags.AddressBook.ToString())[0];
            if (addressbookRoot == null)
                addressbookRoot = CreateNode(XMLContactListTags.AddressBook.ToString(), null);
            addressbookRoot.RemoveAll();

            addressbookRoot.AppendChild(CreateNode(AddressBookChildNodes.LastChanged.ToString(), XmlConvert.ToString
                (lastChange, XmlDateTimeSerializationMode.RoundtripKind)));
            addressbookRoot.AppendChild(CreateNode(AddressBookChildNodes.DynamicItemLastChange.ToString(), XmlConvert.ToString
                (dynamicItemLastChange, XmlDateTimeSerializationMode.RoundtripKind)));

            addressbookRoot.AppendChild(GetGroups());
            addressbookRoot.AppendChild(GetContacts());

            XmlNode settingsroot = CreateNode(XMLContactListTags.Settings.ToString(), null);
            settingsroot.AppendChild(CreateNode(XMLContactListTags.contactType.ToString(), XMLContactListTags.Me.ToString()));
            XmlNode AnnotationsRoot = CreateNode(XMLContactListTags.Annotations.ToString(), null);

            foreach (string name in myproperties.Keys)
            {
                XmlNode annotation = CreateNode(XMLContactListTags.Annotation.ToString(), null);
                annotation.AppendChild(CreateNode(XMLContactListTags.Name.ToString(), name));
                annotation.AppendChild(CreateNode(XMLContactListTags.Value.ToString(), myproperties[name]));
                AnnotationsRoot.AppendChild(annotation);
            }
            settingsroot.AppendChild(AnnotationsRoot);
            addressbookRoot.AppendChild(settingsroot);

            SaveToHiddenMCL(filename);
        }

        public override void Save()
        {
            Save(fileName);
        }

        public void AddGroup(GroupInfo group)
        {
            if (groups.ContainsKey(group.Guid))
            {
                groups[group.Guid] = group;
            }
            else
            {
                groups.Add(group.Guid, group);
            }
        }

        public void AddGroup(Dictionary<string, GroupInfo> range)
        {
            foreach (GroupInfo group in range.Values)
            {
                AddGroup(group);
            }
        }

        private XmlNode GetGroups()
        {
            XmlNode grouproot = CreateNode(XMLContactListTags.groups.ToString(), null);
            foreach (string groupId in groups.Keys)
            {
                XmlNode groupNode = CreateNode(XMLContactListTags.Group.ToString(), null);
                groupNode.AppendChild(CreateNode(GroupChildNodes.Guid.ToString(), groupId));
                groupNode.AppendChild(CreateNode(GroupChildNodes.Name.ToString(), groups[groupId].Name));
                grouproot.AppendChild(groupNode);
            }
            return grouproot;
        }

        private XmlNode GetContacts()
        {
            XmlNode contactRoot = CreateNode(XMLContactListTags.contacts.ToString(), null);
            foreach (ContactInfo cinfo in this.Values)
            {
                XmlNode contactNode = CreateNode(XMLContactListTags.Contact.ToString(), null); //This node MUST comes first.
                if (cinfo != null)
                {
                    contactNode.AppendChild(CreateNode(ContactChildNodes.ContactId.ToString(), cinfo.Guid));
                    contactNode.AppendChild(CreateNode(ContactChildNodes.Account.ToString(), cinfo.Account));
                    contactNode.AppendChild(CreateNode(ContactChildNodes.DisplayName.ToString(), cinfo.DisplayName));
                    contactNode.AppendChild(CreateNode(ContactChildNodes.IsMessengerUser.ToString(),
                        XmlConvert.ToString(cinfo.IsMessengerUser)));

                    XmlNode groupIdRoot = CreateNode(ContactChildNodes.Groups.ToString(), null);
                    foreach (string guid in cinfo.Groups)
                    {
                        groupIdRoot.AppendChild(CreateNode(XMLContactListTags.Group.ToString(), guid));
                    }
                    contactNode.AppendChild(groupIdRoot);
                    contactNode.AppendChild(CreateNode(ContactChildNodes.LastChanged.ToString(),
                        XmlConvert.ToString(cinfo.LastChanged, XmlDateTimeSerializationMode.RoundtripKind)));
                    contactNode.AppendChild(CreateNode(ContactChildNodes.Type.ToString(), XmlConvert.ToString((int)cinfo.Type)));
                    contactNode.AppendChild(CreateNode(ContactChildNodes.Capability.ToString(),
                        XmlConvert.ToString((long)cinfo.Capability)));
                    contactRoot.AppendChild(contactNode);
                }
            }
            return contactRoot;
        }
    }
    #endregion
};
