namespace MSNPSharp.IO
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.IO;
    using MSNPSharp.IO;
    using System.Globalization;
    using MemberRole = MSNPSharp.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNABSharingService.ServiceFilterType;
    using System.Xml.Serialization;

    #region XMLMembershipList

    /// <summary>
    /// XML Membership List file maintainer
    /// </summary>
    [XmlRoot("MembershipList"), Serializable]
    public class XMLMembershipList : XMLContactList
    {
        private SerializableDictionary<int, Service> services = new SerializableDictionary<int, Service>(0);
        private SerializableDictionary<MemberRole, SerializableDictionary<string, ContactInfo>> rolelists = new SerializableDictionary<MemberRole, SerializableDictionary<string, ContactInfo>>(0);

        protected XMLMembershipList()
            :base()
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

        public SerializableDictionary<MemberRole, SerializableDictionary<string, ContactInfo>> MemberRoles
        {
            get
            {
                return rolelists;
            }

            set
            {
                rolelists = value;
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
                    rolelists.Add(role, new SerializableDictionary<string, ContactInfo>(memberroles[role]));
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

        protected XMLAddressBook()
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
