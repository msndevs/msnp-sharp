using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using MSNPSharp.IO;
using System.Globalization;

namespace MSNPSharp
{
    internal class ContactInfo
    {
        public string account;
        public string guid;
        public int membershipId;
        public bool isMessengerUser;
        public ClientType type;
        //public MSNLists msnlist;
        public string displayname;
        public DateTime lastchanged;
        public List<string> groups = new List<string>(0);
    }

    internal struct GroupInfo
    {
        public string guid;
        public string name;
    }

    internal struct Service
    {
        public int Id;
        public string Type;
        public DateTime LastChange;
    }

    internal enum MemberRole
    {
        Allow = 2,
        Block = 4,
        Reverse = 8,
        Pending = 16,
        Contributor,
        ProfileGeneral,
        ProfilePersonalContact,
        ProfileSocial,
        ProfileExpression,
        TwoWayRelationship,
        OneWayRelationship

    }

    /// <summary>
    /// XML Membership List file maintainer
    /// </summary>
    internal class XMLMembershipList : XMLContactList
    {
        private string fileName = "";
        private int serviceId = 0;
        private Dictionary<MemberRole, Dictionary<string, ContactInfo>> rolelists
            = new Dictionary<MemberRole, Dictionary<string, ContactInfo>>(0);

        public XMLMembershipList(string filename)
        {
            fileName = filename;
            LoadFromFile(filename);
        }

        public MSNLists GetMSNLists(string account)
        {
            MSNLists contactlists = new MSNLists();
            if (rolelists.ContainsKey(MemberRole.Allow))
                if (rolelists[MemberRole.Allow].ContainsKey(account))
                    contactlists |= MSNLists.AllowedList;
            if (rolelists.ContainsKey(MemberRole.Pending))
                if (rolelists[MemberRole.Pending].ContainsKey(account))
                    contactlists |= MSNLists.PendingList;
            if (rolelists.ContainsKey(MemberRole.Reverse))
                if (rolelists[MemberRole.Reverse].ContainsKey(account))
                    contactlists |= MSNLists.ReverseList;
            if (rolelists.ContainsKey(MemberRole.Block))
                if (rolelists[MemberRole.Block].ContainsKey(account))
                {
                    contactlists |= MSNLists.BlockedList;
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
        /// Load the contact list from a file.
        /// </summary>
        /// <param name="filename"></param>
        public override void LoadFromFile(string filename)
        {
            base.LoadFromFile(filename);
            this.Clear();
            MembershipParser msrp = new MembershipParser(doc.GetElementsByTagName(XMLContactListTags.MembershipList.ToString())[0]);
            msrp.Parse();
            AddRange(msrp.ContactList);
            lastChange = msrp.LastChange;
            serviceId = msrp.MessengerServiceId;
            rolelists = msrp.MemberRoles;
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
            int[] roleArray = (int[])Enum.GetValues(typeof(MemberRole));
            foreach (int role in roleArray)
            {
                if (rolelists.ContainsKey((MemberRole)role))
                    membershipRoot.AppendChild(GetList((MemberRole)role));
            }
            XmlNode owner = CreateNode(msFSMInput.Info.ToString(), null);
            owner.AppendChild(CreateNode(msFSMInput.Id.ToString(), serviceId.ToString()));
            owner.AppendChild(CreateNode(msFSMInput.Type.ToString(), XMLContactListTags.Messenger.ToString()));
            owner.AppendChild(CreateNode(msFSMInput.LastChange.ToString(), XmlConvert.ToString(
                lastChange, XmlDateTimeSerializationMode.RoundtripKind)));
            membershipRoot.AppendChild(owner);

            SaveToHiddenMCL(filename);
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        public override void Save()
        {
            Save(fileName);
        }

        public int MessengerServiceId
        {
            get { return serviceId; }
            set { serviceId = value; }
        }

        public Dictionary<MemberRole, Dictionary<string, ContactInfo>> MemberRoles
        {
            get { return rolelists; }
            set { rolelists = value; }
        }

        private XmlNode GetList(MemberRole memberrole)
        {
            XmlNode listroot = CreateNode(XMLContactListTags.Members.ToString(), null);

            listroot.AppendChild(CreateNode(msFSMInput.MemberRole.ToString(), memberrole.ToString()));
            foreach (ContactInfo cinfo in rolelists[memberrole].Values)
            {
                XmlNode contactNode = doc.CreateElement(msFSMInput.Member.ToString());
                if (cinfo != null/* && (cinfo.msnlist & list) == list*/)
                {
                    contactNode.AppendChild(CreateNode(msFSMInput.MembershipId.ToString(), cinfo.membershipId.ToString()));
                    if (cinfo.type == ClientType.MessengerUser)
                        contactNode.AppendChild(CreateNode(msFSMInput.PassportName.ToString(), cinfo.account));
                    else
                        contactNode.AppendChild(CreateNode(msFSMInput.Email.ToString(), cinfo.account));

                    contactNode.AppendChild(CreateNode(msFSMInput.DisplayName.ToString(), cinfo.displayname));
                    contactNode.AppendChild(CreateNode(msFSMInput.LastChanged.ToString(),
                        XmlConvert.ToString(cinfo.lastchanged, XmlDateTimeSerializationMode.RoundtripKind)));
                    listroot.AppendChild(contactNode);
                }
            }

            return listroot;
        }
    }


    /// <summary>
    /// XML ForwardList file maintainer
    /// </summary>
    internal class XMLAddressBook : XMLContactList
    {
        string fileName = "";
        DateTime dynamicItemLastChange;
        Dictionary<string, GroupInfo> groups = new Dictionary<string, GroupInfo>(0);
        Dictionary<string, string> myproperties = new Dictionary<string, string>(0);

        public XMLAddressBook(string filename)
        {
            fileName = filename;
            LoadFromFile(filename);
        }

        public override void LoadFromFile(string filename)
        {
            base.LoadFromFile(filename);
            this.Clear();
            XmlNode addressbookRoot = doc.GetElementsByTagName(XMLContactListTags.AddressBook.ToString())[0];
            if (addressbookRoot == null)
                addressbookRoot = CreateNode(XMLContactListTags.AddressBook.ToString(), null);

            AddressBookParser abparser = new AddressBookParser(addressbookRoot);
            abparser.Parse();
            AddRange(abparser.ContactList);
            LastChange = abparser.LastChange;
            dynamicItemLastChange = abparser.DynamicItemLastChange;
            groups = abparser.GroupList;

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
            //Add a CacheKey node here so our parser will have the same behavior when parsing the addressbook
            addressbookRoot.AppendChild(CreateNode(abFSMInput.CacheKey.ToString(), abFSMInput.CacheKey.ToString()));
            addressbookRoot.AppendChild(GetGroups());
            addressbookRoot.AppendChild(GetContacts());
            XmlNode abInfo = CreateNode(abFSMInput.ab.ToString(), null);
            abInfo.AppendChild(CreateNode(abFSMInput.lastChange.ToString(), XmlConvert.ToString
                (lastChange, XmlDateTimeSerializationMode.RoundtripKind)));
            abInfo.AppendChild(CreateNode(abFSMInput.DynamicItemLastChanged.ToString(), XmlConvert.ToString
                (dynamicItemLastChange, XmlDateTimeSerializationMode.RoundtripKind)));
            addressbookRoot.AppendChild(abInfo);

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
            if (groups.ContainsKey(group.guid))
            {
                groups[group.guid] = group;
            }
            else
            {
                groups.Add(group.guid, group);
            }
        }

        public void AddGroupRange(Dictionary<string, GroupInfo> range)
        {
            foreach (GroupInfo group in range.Values)
            {
                AddGroup(group);
            }
        }

        public Dictionary<string, GroupInfo> Groups
        {
            get { return groups; }
        }

        public DateTime DynamicItemLastChange
        {
            get { return dynamicItemLastChange; }
            set { dynamicItemLastChange = value; }
        }

        public Dictionary<string, string> MyProperties
        {
            get { return myproperties; }
            set { myproperties = value; }
        }

        private XmlNode GetGroups()
        {
            XmlNode grouproot = CreateNode(XMLContactListTags.groups.ToString(), null);
            foreach (string groupId in groups.Keys)
            {
                XmlNode groupNode = CreateNode(XMLContactListTags.Group.ToString(), null);
                groupNode.AppendChild(CreateNode(abFSMInput.groupId.ToString(), groupId));
                groupNode.AppendChild(CreateNode(abFSMInput.name.ToString(), groups[groupId].name));
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
                    contactNode.AppendChild(CreateNode(abFSMInput.contactId.ToString(), cinfo.guid));
                    if (cinfo.type == ClientType.MessengerUser)
                    {
                        contactNode.AppendChild(CreateNode(abFSMInput.email.ToString(), cinfo.account));
                        contactNode.AppendChild(CreateNode(abFSMInput.isMessengerEnabled.ToString(),
                            XmlConvert.ToString(cinfo.isMessengerUser)));
                    }
                    else
                    {
                        contactNode.AppendChild(CreateNode(abFSMInput.passportName.ToString(), cinfo.account));
                        contactNode.AppendChild(CreateNode(abFSMInput.isMessengerUser.ToString(),
                            XmlConvert.ToString(cinfo.isMessengerUser)));
                    }
                    contactNode.AppendChild(CreateNode(abFSMInput.displayName.ToString(), cinfo.displayname));
                    contactNode.AppendChild(CreateNode(abFSMInput.lastChange.ToString(),
                        XmlConvert.ToString(cinfo.lastchanged, XmlDateTimeSerializationMode.RoundtripKind)));
                    XmlNode groupIdRoot = CreateNode(XMLContactListTags.groupIds.ToString(), null);
                    foreach (string guid in cinfo.groups)
                    {
                        groupIdRoot.AppendChild(CreateNode(abFSMInput.guid.ToString(), guid));
                    }
                    contactNode.AppendChild(groupIdRoot);
                    contactRoot.AppendChild(contactNode);
                }
            }
            return contactRoot;
        }
    }
}
