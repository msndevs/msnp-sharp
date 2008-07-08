#region Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com>
/*
Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com> All rights reserved.

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

using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;
    using System.Diagnostics;

    /// <summary>
    /// ContactList file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("ContactList")]
    public class XMLContactList : MCLSerializer
    {

        public static XMLContactList LoadFromFile(string filename, bool nocompress)
        {
            return LoadFromFile(filename, nocompress, typeof(XMLContactList)) as XMLContactList;
        }

        /// <summary>
        /// Merge the deltas list into current AddressBook and MemberShipList.
        /// </summary>
        /// <param name="deltas">DeltasList that loads from a mcl file.</param>
        /// <param name="nsMessageHandler"></param>
        public void Merge(DeltasList deltas, NSMessageHandler nsMessageHandler)
        {
            if (deltas.MembershipDeltas.Count > 0)
            {
                foreach (FindMembershipResultType membershipResult in deltas.MembershipDeltas)
                {
                    Merge(membershipResult, nsMessageHandler);
                }
            }
            else
            {
                Merge(new FindMembershipResultType(), nsMessageHandler);
            }

            foreach (ABFindAllResultType abfindallResult in deltas.AddressBookDeltas)
            {
                Merge(abfindallResult, nsMessageHandler);
            }
        }

        #region Membership

        DateTime msLastChange;
        SerializableDictionary<int, Service> services = new SerializableDictionary<int, Service>(0);
        SerializableDictionary<ContactIdentifier, MembershipContactInfo> mscontacts = new SerializableDictionary<ContactIdentifier, MembershipContactInfo>(0);

        /// <summary>
        /// 
        /// </summary>
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

        public SerializableDictionary<ContactIdentifier, MembershipContactInfo> MembershipContacts
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

        public MSNLists GetMSNLists(string account, ClientType type)
        {
            MSNLists contactlists = MSNLists.None;
            ContactIdentifier cid = new ContactIdentifier(account, type);

            if (MembershipContacts.ContainsKey(cid))
            {
                MembershipContactInfo ci = MembershipContacts[cid];
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
                        RemoveMemberhip(account, type, MemberRole.Allow);
                    }
                }
            }
            return contactlists;
        }

        public void AddMemberhip(string account, ClientType type, MemberRole memberrole, int membershipid)
        {
            ContactIdentifier cid = new ContactIdentifier(account, type);

            if (!MembershipContacts.ContainsKey(cid))
                MembershipContacts.Add(cid, new MembershipContactInfo(account, type));

            MembershipContacts[cid].Type = type;
            MembershipContacts[cid].Memberships[memberrole] = membershipid;
        }

        public void RemoveMemberhip(string account, ClientType type, MemberRole memberrole)
        {
            ContactIdentifier cid = new ContactIdentifier(account, type);

            if (MembershipContacts.ContainsKey(cid))
            {
                MembershipContacts[cid].Memberships.Remove(memberrole);

                if (0 == MembershipContacts[cid].Memberships.Count)
                    MembershipContacts.Remove(cid);
            }
        }

        public virtual void Add(Dictionary<ContactIdentifier, MembershipContactInfo> range)
        {
            foreach (ContactIdentifier account in range.Keys)
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
        /// Merge changes into membership list and add membership contacts
        /// </summary>
        /// <param name="findMembership"></param>
        /// <param name="nsMessageHandler"></param>
        public void Merge(FindMembershipResultType findMembership, NSMessageHandler nsMessageHandler)
        {
            if (null != findMembership && null != findMembership.Services)
            {
                foreach (ServiceType serviceType in findMembership.Services)
                {
                    if (serviceType.Deleted)
                    {
                        Services.Remove(int.Parse(serviceType.Info.Handle.Id));
                    }
                    else
                    {
                        Service currentService = new Service();
                        currentService.LastChange = serviceType.LastChange;
                        currentService.Id = int.Parse(serviceType.Info.Handle.Id);
                        currentService.Type = serviceType.Info.Handle.Type;
                        currentService.ForeignId = serviceType.Info.Handle.ForeignId;
                        Services[currentService.Id] = currentService;

                        if (ServiceFilterType.Messenger == currentService.Type)
                        {
                            if (MembershipLastChange < currentService.LastChange && null != serviceType.Memberships)
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
                                                account = account.ToLower(CultureInfo.InvariantCulture);

                                                if (bm.Deleted)
                                                {
                                                    RemoveMemberhip(account, type, memberrole);
                                                }
                                                else
                                                {
                                                    AddMemberhip(account, type, memberrole, Convert.ToInt32(bm.MembershipId));
                                                    ContactIdentifier cid = new ContactIdentifier(account, type);
                                                    MembershipContacts[cid].LastChanged = bm.LastChanged;
                                                    MembershipContacts[cid].DisplayName = String.IsNullOrEmpty(bm.DisplayName) ? account : bm.DisplayName;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            MembershipLastChange = currentService.LastChange;
                        }
                    }
                }
            }

            // Create Messenger Contacts
            if (MembershipContacts.Count > 0)
            {
                foreach (MembershipContactInfo msci in MembershipContacts.Values)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(msci.Account, msci.DisplayName, msci.Type);
                    contact.SetLists(GetMSNLists(msci.Account, msci.Type));
                    contact.NSMessageHandler = nsMessageHandler;
                }
            }
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

        /// <summary>
        /// Merge changes to addressbook and add address book contacts
        /// </summary>
        /// <param name="forwardList"></param>
        /// <param name="nsMessageHandler"></param>
        public void Merge(ABFindAllResultType forwardList, NSMessageHandler nsMessageHandler)
        {
            if (AddressbookLastChange < forwardList.ab.lastChange)
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

                            if (contactType.contactInfo.phones != null && account == null)
                            {
                                type = ClientType.PhoneMember;
                                account = contactType.contactInfo.phones[0].number;
                                ismessengeruser |= contactType.contactInfo.phones[0].isMessengerEnabled;
                                displayname = String.IsNullOrEmpty(contactType.contactInfo.quickName) ? account : contactType.contactInfo.quickName;
                            }

                            if (account == null)
                                continue; // PassportnameHidden... Nothing to do...

                            if (contactType.fDeleted)
                            {
                                AddressbookContacts.Remove(new Guid(contactType.contactId));
                            }
                            else
                            {
                                AddressbookContactInfo ci = new AddressbookContactInfo(account, type, new Guid(contactType.contactId));
                                ci.DisplayName = displayname;
                                ci.IsMessengerUser = ismessengeruser;
                                ci.LastChanged = contactType.lastChange;
                                ci.Comment = contactType.contactInfo.comment;

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

                                    Profile.DisplayName = ci.DisplayName;
                                    Profile.CID = contactType.contactInfo.CID;

                                    if (null != contactType.contactInfo.annotations)
                                    {
                                        foreach (Annotation anno in contactType.contactInfo.annotations)
                                        {
                                            string name = anno.Name;
                                            string value = anno.Value;
                                            name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                                            MyProperties[name] = value;
                                        }
                                    }

                                    if (!MyProperties.ContainsKey("mbea"))
                                        MyProperties["mbea"] = "0";

                                    if (!MyProperties.ContainsKey("gtc"))
                                        MyProperties["gtc"] = "1";

                                    if (!MyProperties.ContainsKey("blp"))
                                        MyProperties["blp"] = "0";

                                    if (!MyProperties.ContainsKey("roamliveproperties"))
                                        MyProperties["roamliveproperties"] = "1";
                                }

                                if (AddressbookContacts.ContainsKey(ci.Guid))
                                {
                                    if (AddressbookContacts[ci.Guid].LastChanged.CompareTo(ci.LastChanged) < 0)
                                    {
                                        AddressbookContacts[ci.Guid] = ci;
                                    }
                                }
                                else
                                {
                                    AddressbookContacts.Add(ci.Guid, ci);
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
                            Groups.Remove(groupType.groupId);
                        }
                        else
                        {
                            GroupInfo group = new GroupInfo();
                            group.Name = groupType.groupInfo.name;
                            group.Guid = groupType.groupId;
                            Groups[group.Guid] = group;
                        }
                    }
                }

                // Update lastchange
                AddressbookLastChange = forwardList.ab.lastChange;
                DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;
            }

            // Create Groups
            foreach (GroupInfo group in Groups.Values)
            {
                nsMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.Name, group.Guid, nsMessageHandler));
            }

            // Create the Forward List and Email Contacts
            foreach (AddressbookContactInfo abci in AddressbookContacts.Values)
            {
                if (abci.Account != nsMessageHandler.Owner.Mail)
                {
                    Contact contact = nsMessageHandler.ContactList.GetContact(abci.Account,abci.Type);
                    contact.SetGuid(abci.Guid);
                    contact.SetClientType(abci.Type);
                    contact.SetComment(abci.Comment);
                    contact.SetIsMessengerUser(abci.IsMessengerUser);
                    if (abci.IsMessengerUser)
                        contact.AddToList(MSNLists.ForwardList); //IsMessengerUser is only valid in AddressBook member

                    if (!String.IsNullOrEmpty(abci.DisplayName))
                        contact.SetName(abci.DisplayName);

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

            //Update dynamic items
            if (forwardList.DynamicItems != null)
            {
                foreach (BaseDynamicItemType dyItem in forwardList.DynamicItems)
                {
                    //XmlNode[] nodes = obj as XmlNode[];
                    //DynamicItem dyItem = new DynamicItem(nodes);
                    if (dyItem is PassportDynamicItem)
                    {
                        if (dyItem.SpaceGleam || dyItem.ProfileGleam)
                        {
                            nsMessageHandler.ContactService.Deltas.DynamicItems[(dyItem as PassportDynamicItem).PassportName] = dyItem;
                        }
                    }
                }
            }

            foreach (PassportDynamicItem dyItem in nsMessageHandler.ContactService.Deltas.DynamicItems.Values)
            {
                if (nsMessageHandler.ContactList.HasContact(dyItem.PassportName, ClientType.PassportMember))
                {
                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam))
                    {
                        nsMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.HasNew); //TODO: Type
                    }

                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam == false) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam == false))
                    {
                        nsMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.Viewed); //TODO: Type
                    }

                    if (dyItem.ProfileStatus == null && dyItem.SpaceStatus == null)  //"Exist Access" means the contact has space or profile
                    {
                        nsMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.None); //TODO: Type
                    }
                }
            }

        }

        #endregion

        #region Profile
        private OwnerProfile profile = new OwnerProfile();

        /// <summary>
        /// Profile of current user.
        /// </summary>
        public OwnerProfile Profile
        {
            get { return profile; }
            set { profile = value; }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Save the <see cref="XMLContactList"/> into a specified file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            Version = Properties.Resources.XMLContactListVersion;
            base.Save(filename);
        }
        #endregion
    
    }
};
