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

    /// <summary>
    /// ContactList file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("ContactList")]
    public class XMLContactList
    {
        #region Common

        [NonSerialized]
        protected bool noCompress;

        [NonSerialized]
        private string fileName;

        public XMLContactList()
        {
        }

        protected string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        protected bool NoCompress
        {
            get
            {
                return noCompress;
            }
            set
            {
                noCompress = value;
            }
        }

        public static XMLContactList LoadFromFile(string filename, bool noCompress)
        {
            XMLContactList rtnobj = (XMLContactList)Activator.CreateInstance(typeof(XMLContactList));
            if (File.Exists(filename))
            {
                MCLFile file = MCLFileManager.GetFile(filename, noCompress);
                if (file.Content != null)
                {
                    MemoryStream mem = new MemoryStream(file.Content);
                    rtnobj = (XMLContactList)new XmlSerializer(typeof(XMLContactList)).Deserialize(mem);
                    mem.Close();
                }
            }
            rtnobj.NoCompress = noCompress;
            rtnobj.FileName = filename;
            return rtnobj;
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        public void Save()
        {
            Save(FileName);
        }

        /// <summary>
        /// Save the contact list into a file.
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            SaveToHiddenMCL(filename);
        }

        private void SaveToHiddenMCL(string filename)
        {
            XmlSerializer ser = new XmlSerializer(this.GetType());
            MemoryStream ms = new MemoryStream();
            ser.Serialize(ms, this);
            MCLFile file = MCLFileManager.GetFile(filename, noCompress);
            file.Content = ms.ToArray();
            MCLFileManager.Save(file, true);
            ms.Close();
        }

        #endregion

        #region Membership

        DateTime msLastChange;
        SerializableDictionary<int, Service> services = new SerializableDictionary<int, Service>(0);
        SerializableDictionary<string, MembershipContactInfo> mscontacts = new SerializableDictionary<string, MembershipContactInfo>(0);

        public DateTime MembershipLastChange
        {
            get
            {
                return msLastChange;
            }
            set
            {
                msLastChange = value;
            }
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

        public SerializableDictionary<string, MembershipContactInfo> MembershipContacts
        {
            get
            {
                return mscontacts;
            }
            set
            {
                mscontacts = value;
            }
        }

        public MSNLists GetMSNLists(string account)
        {
            MSNLists contactlists = MSNLists.None;
            if (MembershipContacts.ContainsKey(account))
            {
                MembershipContactInfo ci = MembershipContacts[account];
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
            if (!MembershipContacts.ContainsKey(account))
                MembershipContacts.Add(account, new MembershipContactInfo(account, type));

            MembershipContacts[account].Type = type;
            MembershipContacts[account].Memberships[memberrole] = membershipid;
        }

        public void RemoveMemberhip(string account, MemberRole memberrole)
        {
            if (MembershipContacts.ContainsKey(account))
            {
                MembershipContacts[account].Memberships.Remove(memberrole);

                if (0 == MembershipContacts[account].Memberships.Count)
                    MembershipContacts.Remove(account);
            }
        }

        public virtual void Add(Dictionary<string, MembershipContactInfo> range)
        {
            foreach (string account in range.Keys)
            {
                if (mscontacts.ContainsKey(account))
                {
                    if (mscontacts[account].LastChanged.CompareTo(range[account].LastChanged) <= 0)
                    {
                        mscontacts[account] = range[account];
                    }
                }
                else
                {
                    mscontacts.Add(account, range[account]);
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
                    if (msLastChange.CompareTo(innerService.LastChange) < 0)
                        msLastChange = innerService.LastChange;

        }

        #endregion

        #region Addressbook

        DateTime abLastChange;
        DateTime dynamicItemLastChange;
        SerializableDictionary<string, string> myproperties = new SerializableDictionary<string, string>(0);
        SerializableDictionary<string, GroupInfo> groups = new SerializableDictionary<string, GroupInfo>(0);
        SerializableDictionary<Guid, AddressbookContactInfo> abcontacts = new SerializableDictionary<Guid, AddressbookContactInfo>(0);

        public DateTime AddressbookLastChange
        {
            get
            {
                return abLastChange;
            }
            set
            {
                abLastChange = value;
            }
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

        public SerializableDictionary<Guid, AddressbookContactInfo> AddressbookContacts
        {
            get
            {
                return abcontacts;
            }
            set
            {
                abcontacts = value;
            }
        }

        public void AddGroup(Dictionary<string, GroupInfo> range)
        {
            foreach (GroupInfo group in range.Values)
            {
                AddGroup(group);
            }
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

        public virtual void Add(Dictionary<Guid, AddressbookContactInfo> range)
        {
            foreach (Guid guid in range.Keys)
            {
                if (abcontacts.ContainsKey(guid))
                {
                    if (abcontacts[guid].LastChanged.CompareTo(range[guid].LastChanged) <= 0)
                    {
                        abcontacts[guid] = range[guid];
                    }
                }
                else
                {
                    abcontacts.Add(guid, range[guid]);
                }
            }
        }

        public virtual AddressbookContactInfo Find(string email, ClientType type)
        {
            foreach (AddressbookContactInfo ci in AddressbookContacts.Values)
            {
                if (ci.Account == email && ci.Type == type)
                    return ci;
            }
            return null;
        }

        #endregion
    }
};
