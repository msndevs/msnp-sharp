namespace MSNPSharp.IO
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using System.Xml.Serialization;
    using System.Collections.Generic;

    using MemberRole = MSNPSharp.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNABSharingService.ServiceFilterType;


    #region XMLMembershipList

    /// <summary>
    /// XML Membership List file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("MembershipList")]
    public class XMLMembershipList : XMLContactList
    {
        private SerializableDictionary<int, Service> services = new SerializableDictionary<int, Service>(0);

        public XMLMembershipList()
            : base()
        {
        }

        public SerializableDictionary<int, Service> Services
        {
            get
            {
                return services;
            }

            set
            {
                services = value;
            }
        }

        public MSNLists GetMSNLists(string account)
        {
            MSNLists contactlists = MSNLists.None;
            if (Contacts.ContainsKey(account))
            {
                ContactInfo ci = Contacts[account];
                if (ci.Memberships.ContainsKey(MemberRole.Allow))
                    contactlists |= MSNLists.AllowedList;

                if (ci.Memberships.ContainsKey(MemberRole.Pending))
                    contactlists |= MSNLists.PendingList;

                if (ci.Memberships.ContainsKey(MemberRole.Reverse))
                    contactlists |= MSNLists.ReverseList;

                if (ci.Memberships.ContainsKey(MemberRole.Block))
                {
                    contactlists |= MSNLists.BlockedList;

                    if ((contactlists & MSNLists.AllowedList) == MSNLists.AllowedList)
                    {
                        contactlists ^= MSNLists.AllowedList;
                        RemoveMemberhip(account, MemberRole.Allow);
                    }
                }
            }
            return contactlists;
        }

        public void AddMemberhip(string account, ClientType type, MemberRole memberrole, int membershipid)
        {
            if (!Contacts.ContainsKey(account))
                Contacts.Add(account, new ContactInfo(account, type));

            Contacts[account].Type = type;
            Contacts[account].Memberships[memberrole] = membershipid;
        }

        public void RemoveMemberhip(string account, MemberRole memberrole)
        {
            if (Contacts.ContainsKey(account))
            {
                Contacts[account].Memberships.Remove(memberrole);

                if (0 == Contacts[account].Memberships.Count)
                    Contacts.Remove(account);
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
        /// Save the contact list into a file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            SaveToHiddenMCL(filename);
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        public override void Save()
        {
            Save(FileName);
        }
    }

    #endregion

    #region XMLAddressBook

    /// <summary>
    /// XML ForwardList file maintainer
    /// </summary>
    [XmlRoot("AddressBook"), Serializable]
    public class XMLAddressBook : XMLContactList
    {
        DateTime dynamicItemLastChange;
        SerializableDictionary<string, GroupInfo> groups = new SerializableDictionary<string, GroupInfo>(0);
        SerializableDictionary<string, string> myproperties = new SerializableDictionary<string, string>(0);

        public XMLAddressBook()
            : base()
        {
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

        public SerializableDictionary<string, GroupInfo> Groups
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

        public SerializableDictionary<string, string> MyProperties
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

        public override void Save(string filename)
        {
            SaveToHiddenMCL(filename);
        }

        public override void Save()
        {
            Save(FileName);
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
    }
    #endregion
};
