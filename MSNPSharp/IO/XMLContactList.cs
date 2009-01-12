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
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// ContactList file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("ContactList")]
    public class XMLContactList : MCLSerializer
    {
        public static XMLContactList LoadFromFile(string filename, bool nocompress, NSMessageHandler handler)
        {
            return LoadFromFile(filename, nocompress, typeof(XMLContactList), handler) as XMLContactList;
        }

        /// <summary>
        /// Loads contacts from saved mcl file, merges the deltas list saved into current AddressBook and MemberShipList.
        /// </summary>
        /// <param name="deltas"></param>
        internal void Synchronize(DeltasList deltas)
        {
            // Create Memberships
            SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>> ms =
                GetTargetMemberships(ServiceFilterType.Messenger);

            if (ms != null)
            {
                foreach (MemberRole role in ms.Keys)
                {
                    MSNLists msnlist = NSMessageHandler.ContactService.GetMSNList(role);
                    foreach (BaseMember bm in ms[role].Values)
                    {
                        long? cid = null;
                        string account = null;
                        ClientType type = ClientType.None;

                        if (bm is PassportMember)
                        {
                            type = ClientType.PassportMember;
                            PassportMember pm = (PassportMember)bm;
                            if (!pm.IsPassportNameHidden)
                            {
                                account = pm.PassportName;
                            }
                            cid = Convert.ToInt64(pm.CID);
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

                        if (account != null && type != ClientType.None)
                        {
                            string displayname = bm.DisplayName == null ? account : bm.DisplayName;
                            Contact contact = NSMessageHandler.ContactList.GetContact(account, displayname, type);
                            contact.NSMessageHandler = NSMessageHandler;
                            contact.CID = cid;
                            contact.Lists |= msnlist;
                        }
                    }
                }
            }

            // Create Groups
            foreach (GroupType group in Groups.Values)
            {
                NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.groupInfo.name, group.groupId, NSMessageHandler));
            }

            // Create the Forward List and Email Contacts
            foreach (ContactType contactType in AddressbookContacts.Values)
            {
                UpdateContact(contactType);
            }

            // Update dynamic items
            List<string> dyItemsToRemove = new List<string>();
            foreach (PassportDynamicItem dyItem in NSMessageHandler.ContactService.Deltas.DynamicItems.Values)
            {
                if (NSMessageHandler.ContactList.HasContact(dyItem.PassportName, ClientType.PassportMember))
                {
                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam))
                    {
                        NSMessageHandler.ContactList[dyItem.PassportName].DynamicChanged = DynamicItemState.HasNew;
                    }

                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam == false) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam == false))
                    {
                        NSMessageHandler.ContactList[dyItem.PassportName].DynamicChanged = DynamicItemState.None;
                        dyItemsToRemove.Add(dyItem.PassportName);
                    }

                    if (dyItem.ProfileStatus == null && dyItem.SpaceStatus == null)  //"Exist Access" means the contact has space or profile
                    {
                        NSMessageHandler.ContactList[dyItem.PassportName].DynamicChanged = DynamicItemState.None;
                        dyItemsToRemove.Add(dyItem.PassportName);
                    }
                }
                else if (dyItem.PassportName == NSMessageHandler.Owner.Mail && dyItem.Notifications != null)
                {
                    foreach (NotificationDataType notifydata in dyItem.Notifications)
                    {
                        if (notifydata.StoreService.Info.Handle.Type == ServiceFilterType.Profile)
                        {
                            if (NSMessageHandler.ContactService.Deltas.Profile.DateModified < notifydata.LastChanged)
                            {
                                NSMessageHandler.ContactService.AddressBook.MyProperties["lastchanged"] =
                                    XmlConvert.ToString(notifydata.LastChanged, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");
                            }
                        }
                    }
                    dyItemsToRemove.Add(dyItem.PassportName);
                }
            }
            lock (NSMessageHandler.ContactService.Deltas.DynamicItems)
            {
                foreach (string account in dyItemsToRemove)
                {
                    NSMessageHandler.ContactService.Deltas.DynamicItems.Remove(account);
                }
            }


            // Merge deltas
            XMLContactList ops = this;
            if (deltas.MembershipDeltas.Count > 0)
            {
                foreach (FindMembershipResultType membershipResult in deltas.MembershipDeltas)
                {
                    ops += membershipResult;
                }
            }

            if (deltas.AddressBookDeltas.Count > 0)
            {
                foreach (ABFindAllResultType abfindallResult in deltas.AddressBookDeltas)
                {
                    ops += abfindallResult;
                }
            }

        }

        #region New MembershipList

        private SerializableDictionary<ServiceFilterType, ServiceMembership> mslist = new SerializableDictionary<ServiceFilterType, ServiceMembership>(0);
        public SerializableDictionary<ServiceFilterType, ServiceMembership> MembershipList
        {
            get
            {
                return mslist;
            }
            set
            {
                mslist = value;
            }
        }

        public DateTime MembershipLastChange
        {
            get
            {
                if (MembershipList.Keys.Count == 0)
                    return XmlConvert.ToDateTime("0001-01-01T00:00:00.0000000-08:00", XmlDateTimeSerializationMode.RoundtripKind);

                List<Service> services = new List<Service>();
                foreach (ServiceFilterType sft in MembershipList.Keys)
                    services.Add(new Service(MembershipList[sft].Service));

                services.Sort();
                return services[services.Count - 1].LastChange;
            }
        }

        internal Service GetTargetService(ServiceFilterType type)
        {
            if (MembershipList.ContainsKey(type))
                return MembershipList[type].Service;

            return null;
        }

        internal SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>> GetTargetMemberships(ServiceFilterType type)
        {
            if (MembershipList.ContainsKey(type))
                return MembershipList[type].Memberships;

            return null;
        }

        public void AddMemberhip(ServiceFilterType servicetype, string account, ClientType type, MemberRole memberrole, BaseMember member)
        {
            Service svc = GetTargetService(servicetype);
            if (svc != null)
            {
                SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(svc.ServiceType);
                string hash = Contact.MakeHash(account, type);
                if (!ms.ContainsKey(memberrole))
                    ms.Add(memberrole, new SerializableDictionary<string, BaseMember>(0));

                ms[memberrole][hash] = member;
            }
        }

        public void RemoveMemberhip(ServiceFilterType servicetype, string account, ClientType type, MemberRole memberrole)
        {
            Service svc = GetTargetService(servicetype);
            if (svc != null)
            {
                SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(svc.ServiceType);
                string hash = Contact.MakeHash(account, type);
                if (ms.ContainsKey(memberrole) && ms[memberrole].ContainsKey(hash))
                {
                    ms[memberrole].Remove(hash);
                }
            }
        }

        public virtual void Add(
            Dictionary<Service,
            Dictionary<MemberRole,
            Dictionary<string, BaseMember>>> range)
        {
            foreach (Service svc in range.Keys)
            {
                foreach (MemberRole role in range[svc].Keys)
                {
                    foreach (string hash in range[svc][role].Keys)
                    {
                        if (!mslist.ContainsKey(svc.ServiceType))
                            mslist.Add(svc.ServiceType, new ServiceMembership(svc));

                        if (!mslist[svc.ServiceType].Memberships.ContainsKey(role))
                            mslist[svc.ServiceType].Memberships.Add(role, new SerializableDictionary<string, BaseMember>(0));

                        if (mslist[svc.ServiceType].Memberships[role].ContainsKey(hash))
                        {
                            if (mslist[svc.ServiceType].Memberships[role][hash].LastChangedSpecified
                                && mslist[svc.ServiceType].Memberships[role][hash].LastChanged.CompareTo(range[svc][role][hash].LastChanged) <= 0)
                            {
                                mslist[svc.ServiceType].Memberships[role][hash] = range[svc][role][hash];
                            }
                        }
                        else
                        {
                            mslist[svc.ServiceType].Memberships[role].Add(hash, range[svc][role][hash]);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Merge changes into membership list and add membership contacts
        /// </summary>
        /// <param name="xmlcl"></param>
        /// <param name="findMembership"></param>
        /// <returns></returns>
        public static XMLContactList operator +(XMLContactList xmlcl, FindMembershipResultType findMembership)
        {
            // Process new FindMemberships (deltas)
            if (null != findMembership && null != findMembership.Services)
            {
                foreach (ServiceType serviceType in findMembership.Services)
                {
                    Service oldService = xmlcl.GetTargetService(serviceType.Info.Handle.Type);

                    if (oldService == null || oldService.LastChange < serviceType.LastChange)
                    {
                        if (serviceType.Deleted)
                        {
                            if (xmlcl.MembershipList.ContainsKey(serviceType.Info.Handle.Type))
                            {
                                xmlcl.MembershipList.Remove(serviceType.Info.Handle.Type);
                            }
                        }
                        else
                        {
                            Service updatedService = new Service();
                            updatedService.Id = int.Parse(serviceType.Info.Handle.Id);
                            updatedService.ServiceType = serviceType.Info.Handle.Type;
                            updatedService.LastChange = serviceType.LastChange;
                            updatedService.ForeignId = serviceType.Info.Handle.ForeignId;

                            if (oldService == null)
                            {
                                xmlcl.MembershipList.Add(updatedService.ServiceType, new ServiceMembership(updatedService));
                            }

                            if (null != serviceType.Memberships)
                            {
                                #region Messenger memberhips
                                if (ServiceFilterType.Messenger == serviceType.Info.Handle.Type)
                                {
                                    foreach (Membership membership in serviceType.Memberships)
                                    {
                                        if (null != membership.Members)
                                        {
                                            MemberRole memberrole = membership.MemberRole;
                                            foreach (BaseMember bm in membership.Members)
                                            {
                                                long? cid = null;
                                                string account = null;
                                                ClientType type = ClientType.None;

                                                if (bm is PassportMember)
                                                {
                                                    type = ClientType.PassportMember;
                                                    PassportMember pm = bm as PassportMember;
                                                    if (!pm.IsPassportNameHidden)
                                                    {
                                                        account = pm.PassportName;
                                                    }
                                                    cid = Convert.ToInt64(pm.CID);
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

                                                if (account != null && type != ClientType.None)
                                                {
                                                    account = account.ToLowerInvariant();
                                                    MSNLists msnlist = xmlcl.NSMessageHandler.ContactService.GetMSNList(memberrole);

                                                    if (bm.Deleted)
                                                    {
                                                        xmlcl.RemoveMemberhip(updatedService.ServiceType, account, type, memberrole);

                                                        if (xmlcl.NSMessageHandler.ContactList.HasContact(account, type))
                                                        {
                                                            Contact contact = xmlcl.NSMessageHandler.ContactList.GetContact(account, type);
                                                            contact.NSMessageHandler = xmlcl.NSMessageHandler;
                                                            contact.CID = cid;
                                                            contact.RemoveFromList(msnlist);

                                                            // Fire ReverseRemoved
                                                            if (msnlist == MSNLists.ReverseList)
                                                            {
                                                                xmlcl.NSMessageHandler.ContactService.OnReverseRemoved(new ContactEventArgs(contact));
                                                            }

                                                            // Send a list remove event
                                                            xmlcl.NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, msnlist));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        xmlcl.AddMemberhip(updatedService.ServiceType, account, type, memberrole, bm);

                                                        string displayname = bm.DisplayName == null ? account : bm.DisplayName;
                                                        Contact contact = xmlcl.NSMessageHandler.ContactList.GetContact(account, displayname, type);
                                                        contact.NSMessageHandler = xmlcl.NSMessageHandler;
                                                        contact.CID = cid;
                                                        contact.AddToList(msnlist);

                                                        // Don't fire ReverseAdded(contact.Pending) here... It fires 2 times:
                                                        // The first is OnConnect after abSynchronized
                                                        // The second is here, not anymore here :)
                                                        // The correct place is in OnADLReceived.

                                                        // Send a list add event
                                                        xmlcl.NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, msnlist));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                #region ~Messenger
                                else
                                {
                                    foreach (Membership membership in serviceType.Memberships)
                                    {
                                        if (null != membership.Members)
                                        {
                                            MemberRole memberrole = membership.MemberRole;
                                            foreach (BaseMember bm in membership.Members)
                                            {
                                                string account = null;
                                                ClientType type = ClientType.None;

                                                switch (bm.Type)
                                                {
                                                    case "Passport":
                                                        type = ClientType.PassportMember;
                                                        PassportMember pm = bm as PassportMember;
                                                        if (!pm.IsPassportNameHidden)
                                                        {
                                                            account = pm.PassportName;
                                                        }
                                                        break;

                                                    case "Email":
                                                        type = ClientType.EmailMember;
                                                        account = ((EmailMember)bm).Email;
                                                        break;

                                                    case "Phone":
                                                        type = ClientType.PhoneMember;
                                                        account = ((PhoneMember)bm).PhoneNumber;
                                                        break;

                                                    case "Role":
                                                    case "Service":
                                                    case "Everyone":
                                                        account = bm.Type + "/" + bm.MembershipId;
                                                        break;

                                                    case "Domain":
                                                        account = ((DomainMember)bm).DomainName;
                                                        break;
                                                }

                                                if (account != null)
                                                {
                                                    if (bm.Deleted)
                                                    {
                                                        xmlcl.RemoveMemberhip(updatedService.ServiceType, account, type, memberrole);
                                                    }
                                                    else
                                                    {
                                                        xmlcl.AddMemberhip(updatedService.ServiceType, account, type, memberrole, bm);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }

                            // Update service.LastChange
                            xmlcl.MembershipList[updatedService.ServiceType].Service = updatedService;
                        }
                    }
                }
            }
            return xmlcl;
        }


        #endregion

        #region Addressbook

        DateTime abLastChange;
        DateTime dynamicItemLastChange;
        SerializableDictionary<string, string> myproperties = new SerializableDictionary<string, string>(0);
        SerializableDictionary<Guid, GroupType> groups = new SerializableDictionary<Guid, GroupType>(0);
        SerializableDictionary<Guid, ContactType> abcontacts = new SerializableDictionary<Guid, ContactType>(0);

        [XmlElement("AddressbookLastChange")]
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

        [XmlElement("DynamicItemLastChange")]
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

        public SerializableDictionary<Guid, GroupType> Groups
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

        public SerializableDictionary<Guid, ContactType> AddressbookContacts
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

        public void AddGroup(Dictionary<Guid, GroupType> range)
        {
            foreach (GroupType group in range.Values)
            {
                AddGroup(group);
            }
        }

        public void AddGroup(GroupType group)
        {
            Guid key = new Guid(group.groupId);
            if (groups.ContainsKey(key))
            {
                groups[key] = group;
            }
            else
            {
                groups.Add(key, group);
            }
        }

        public virtual void Add(Dictionary<Guid, ContactType> range)
        {
            foreach (Guid guid in range.Keys)
            {
                abcontacts[guid] = range[guid];
            }
        }

        /// <summary>
        /// Merge changes to addressbook and add address book contacts
        /// </summary>
        /// <param name="xmlcl">Addressbook</param>
        /// <param name="forwardList"></param>
        /// <returns></returns>
        public static XMLContactList operator +(XMLContactList xmlcl, ABFindAllResultType forwardList)
        {
            if (forwardList.ab != null && xmlcl.AddressbookLastChange < forwardList.ab.lastChange)
            {
                if (null != forwardList.groups)
                {
                    foreach (GroupType groupType in forwardList.groups)
                    {
                        Guid key = new Guid(groupType.groupId);
                        if (groupType.fDeleted)
                        {
                            xmlcl.Groups.Remove(key);

                            ContactGroup contactGroup = (ContactGroup)xmlcl.NSMessageHandler.ContactGroups[groupType.groupId];
                            if (contactGroup != null)
                            {
                                xmlcl.NSMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                                xmlcl.NSMessageHandler.ContactService.OnContactGroupRemoved(new ContactGroupEventArgs(contactGroup));
                            }
                        }
                        else
                        {
                            xmlcl.Groups[key] = groupType;

                            // Add a new group
                            xmlcl.NSMessageHandler.ContactGroups.AddGroup(
                                new ContactGroup(System.Web.HttpUtility.UrlDecode(groupType.groupInfo.name), groupType.groupId, xmlcl.NSMessageHandler));

                            // Fire the event
                            xmlcl.NSMessageHandler.ContactService.OnContactGroupAdded(
                                new ContactGroupEventArgs(xmlcl.NSMessageHandler.ContactGroups[groupType.groupId]));
                        }
                    }
                }

                if (null != forwardList.contacts)
                {
                    foreach (ContactType contactType in forwardList.contacts)
                    {
                        if (null != contactType.contactInfo)
                        {
                            Contact contact = xmlcl.NSMessageHandler.ContactList.GetContactByGuid(new Guid(contactType.contactId));

                            if (contactType.fDeleted)
                            {
                                xmlcl.AddressbookContacts.Remove(new Guid(contactType.contactId));

                                if (contact != null)
                                {
                                    contact.OnForwardList = false;

                                    xmlcl.NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, MSNLists.ForwardList));

                                    contact.Guid = Guid.Empty;
                                    contact.SetIsMessengerUser(false);

                                    if (MSNLists.None == contact.Lists)
                                    {
                                        xmlcl.NSMessageHandler.ContactList.Remove(contact.Mail, contact.ClientType);
                                        contact.NSMessageHandler = null;
                                    }
                                }
                            }
                            else
                            {
                                xmlcl.AddressbookContacts[new Guid(contactType.contactId)] = contactType;
                                xmlcl.UpdateContact(contactType);
                                xmlcl.NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, MSNLists.ForwardList));
                            }
                        }
                    }
                }

                // Update lastchange
                xmlcl.AddressbookLastChange = forwardList.ab.lastChange;
                xmlcl.DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;
            }

            // Update dynamic items
            if (forwardList.DynamicItems != null)
            {
                foreach (BaseDynamicItemType dyItem in forwardList.DynamicItems)
                {
                    PassportDynamicItem pdi = dyItem as PassportDynamicItem;
                    if (pdi != null)
                    {
                        if (pdi.SpaceGleam || pdi.ProfileGleam)
                        {
                            xmlcl.NSMessageHandler.ContactService.Deltas.DynamicItems[pdi.PassportName] = dyItem;
                        }
                        else if (pdi.PassportName == xmlcl.NSMessageHandler.Owner.Mail && pdi.Notifications != null)
                        {
                            foreach (NotificationDataType notifydata in dyItem.Notifications)
                            {
                                if (notifydata.StoreService.Info.Handle.Type == ServiceFilterType.Profile)
                                {
                                    if (xmlcl.NSMessageHandler.ContactService.Deltas.Profile.DateModified < notifydata.LastChanged)
                                    {
                                        xmlcl.NSMessageHandler.ContactService.AddressBook.MyProperties["lastchanged"] =
                                            XmlConvert.ToString(notifydata.LastChanged, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return xmlcl;
        }

        private void UpdateContact(ContactType contactType)
        {
            contactInfoType cit = contactType.contactInfo;
            ClientType type = ClientType.PassportMember;
            string account = cit.passportName;
            string displayname = cit.displayName;
            bool ismessengeruser = cit.isMessengerUser;

            if (cit.emails != null && account == null)
            {
                type = ClientType.EmailMember;
                account = cit.emails[0].email;
                ismessengeruser |= cit.emails[0].isMessengerEnabled;
                displayname = account;
            }

            if (cit.phones != null && account == null)
            {
                type = ClientType.PhoneMember;
                account = cit.phones[0].number;
                ismessengeruser |= cit.phones[0].isMessengerEnabled;
                displayname = String.IsNullOrEmpty(cit.quickName) ? account : cit.quickName;
            }

            if (account != null)
            {
                if (cit.contactType != contactInfoTypeContactType.Me)
                {
                    Contact contact = NSMessageHandler.ContactList.GetContact(account, type);
                    contact.NSMessageHandler = NSMessageHandler;
                    contact.Guid = new Guid(contactType.contactId);
                    contact.CID = Convert.ToInt64(cit.CID);
                    contact.ContactType = cit.contactType;
                    //contact.SetHasBlog(cit.hasSpace);   //DONOT trust this
                    contact.SetComment(cit.comment);
                    contact.SetIsMessengerUser(ismessengeruser);
                    contact.SetMobileAccess(cit.isMobileIMEnabled);
                    if (contact.IsMessengerUser)
                        contact.AddToList(MSNLists.ForwardList); //IsMessengerUser is only valid in AddressBook member

                    if (!String.IsNullOrEmpty(displayname))
                    {
                        if (contact.Name == contact.Mail && displayname != contact.Mail)
                            contact.SetName(displayname);
                    }

                    if (cit.phones != null)
                    {
                        foreach (contactPhoneType cp in cit.phones)
                        {
                            switch (cp.contactPhoneType1)
                            {
                                case ContactPhoneTypeType.ContactPhoneMobile:
                                    contact.SetMobilePhone(cp.number);
                                    break;

                                case ContactPhoneTypeType.ContactPhonePersonal:
                                    contact.SetHomePhone(cp.number);
                                    break;

                                case ContactPhoneTypeType.ContactPhoneBusiness:
                                    contact.SetWorkPhone(cp.number);
                                    break;
                            }
                        }
                    }

                    if (null != cit.annotations)
                    {
                        foreach (Annotation anno in cit.annotations)
                        {
                            switch (anno.Name)
                            {
                                case "AB.NickName":
                                    contact.SetNickName(anno.Value);
                                    break;
                            }
                        }
                    }

                    if (cit.groupIds != null)
                    {
                        foreach (string groupId in cit.groupIds)
                        {
                            contact.ContactGroups.Add(NSMessageHandler.ContactGroups[groupId]);
                        }
                    }

                    if (cit.groupIdsDeleted != null)
                    {
                        foreach (string groupId in cit.groupIdsDeleted)
                        {
                            contact.ContactGroups.Remove(NSMessageHandler.ContactGroups[groupId]);
                        }
                    }
                }
                else
                {
                    if (displayname == NSMessageHandler.Owner.Mail && !String.IsNullOrEmpty(NSMessageHandler.Owner.Name))
                    {
                        displayname = NSMessageHandler.Owner.Name;
                    }

                    NSMessageHandler.Owner.Guid = new Guid(contactType.contactId);
                    NSMessageHandler.Owner.CID = Convert.ToInt64(cit.CID);
                    NSMessageHandler.Owner.ContactType = cit.contactType;
                    NSMessageHandler.Owner.SetName(displayname);

                    //NSMessageHandler.ContactService.Deltas.Profile.DisplayName = displayname;

                    if (null != cit.annotations)
                    {
                        foreach (Annotation anno in cit.annotations)
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

                    if (!MyProperties.ContainsKey("mpop"))
                        MyProperties["mpop"] = "0";

                    if (!MyProperties.ContainsKey("roamliveproperties"))
                        MyProperties["roamliveproperties"] = "1";

                    if (!MyProperties.ContainsKey("lastchanged"))
                        MyProperties["lastchanged"] = XmlConvert.ToString(DateTime.MaxValue, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");
                }
            }
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
