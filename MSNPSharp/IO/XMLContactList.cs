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
        /// Merge the deltas list into current AddressBook and MemberShipList.
        /// </summary>
        /// <param name="xmlcl"></param>
        /// <param name="deltas"></param>
        /// <returns></returns>
        public static XMLContactList operator +(XMLContactList xmlcl, DeltasList deltas)
        {
            if (deltas.MembershipDeltas.Count > 0)
            {
                foreach (FindMembershipResultType membershipResult in deltas.MembershipDeltas)
                {
                    xmlcl += membershipResult;
                }
            }
            else
            {
                xmlcl += new FindMembershipResultType();
            }

            if (deltas.AddressBookDeltas.Count > 0)
            {
                foreach (ABFindAllResultType abfindallResult in deltas.AddressBookDeltas)
                {
                    xmlcl += abfindallResult;
                }
            }
            else
            {
                xmlcl += new ABFindAllResultType();
            }

            return xmlcl;
        }

        #region Membership

        DateTime msLastChange;

        SerializableDictionary<Service,
                               SerializableDictionary<MemberRole,
                               SerializableDictionary<string, BaseMember>>> mslist = new
                               SerializableDictionary<Service, SerializableDictionary<MemberRole,
                               SerializableDictionary<string, BaseMember>>>(0);

        [XmlElement("MembershipLastChange")]
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



        #region New MemberShipList
        public SerializableDictionary<Service,
                    SerializableDictionary<MemberRole,
                    SerializableDictionary<string, BaseMember>>> MembershipList
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

        private Service GetTargetService(ServiceFilterType type)
        {
            foreach (Service svc in MembershipList.Keys)
            {
                if (svc.ServiceType == type)
                {
                    return svc;
                }
            }
            return null;
        }



        public MSNLists GetMSNLists(ServiceFilterType servicetype, string account, ClientType type)
        {
            MSNLists contactlists = MSNLists.None;
            Service targetservice = GetTargetService(servicetype);

            if (targetservice != null)
            {
                string hash = Contact.MakeHash(account, type);

                if (MembershipList[targetservice].ContainsKey(MemberRole.Allow)
                    && MembershipList[targetservice][MemberRole.Allow].ContainsKey(hash))
                    contactlists |= MSNLists.AllowedList;

                if (MembershipList[targetservice].ContainsKey(MemberRole.Pending)
                    && MembershipList[targetservice][MemberRole.Pending].ContainsKey(hash))
                    contactlists |= MSNLists.PendingList;

                if (MembershipList[targetservice].ContainsKey(MemberRole.Block)
                    && MembershipList[targetservice][MemberRole.Block].ContainsKey(hash))
                {
                    contactlists |= MSNLists.BlockedList;

                    if ((contactlists & MSNLists.AllowedList) == MSNLists.AllowedList)
                    {
                        contactlists ^= MSNLists.AllowedList;
                        RemoveMemberhip(targetservice.ServiceType, account, type, MemberRole.Allow);
                    }

                }

                if (MembershipList[targetservice].ContainsKey(MemberRole.Reverse)
                    && MembershipList[targetservice][MemberRole.Reverse].ContainsKey(hash))
                    contactlists |= MSNLists.ReverseList;
            }

            return contactlists;
        }

        public void AddMemberhip(ServiceFilterType servicetype, string account, ClientType type, MemberRole memberrole, BaseMember member)
        {
            Service svc = GetTargetService(servicetype);
            if (svc != null)
            {
                string hash = Contact.MakeHash(account, type);

                if (!MembershipList[svc].ContainsKey(memberrole))
                    MembershipList[svc].Add(memberrole, new SerializableDictionary<string, BaseMember>(0));

                MembershipList[svc][memberrole][hash] = member;
            }
        }

        public void RemoveMemberhip(ServiceFilterType servicetype, string account, ClientType type, MemberRole memberrole)
        {
            Service svc = GetTargetService(servicetype);
            if (svc != null)
            {
                string hash = Contact.MakeHash(account, type);
                if (MembershipList[svc].ContainsKey(memberrole) && MembershipList[svc][memberrole].ContainsKey(hash))
                {
                    MembershipList[svc][memberrole].Remove(hash);
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
                        if (!mslist.ContainsKey(svc))
                            mslist.Add(svc, new SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>>(0));

                        if (!mslist[svc].ContainsKey(role))
                            mslist[svc].Add(role, new SerializableDictionary<string, BaseMember>(0));

                        if (mslist[svc][role].ContainsKey(hash))
                        {
                            if (mslist[svc][role][hash].LastChangedSpecified
                                && mslist[svc][role][hash].LastChanged.CompareTo(
                                range[svc][role][hash].LastChanged) <= 0)
                            {
                                mslist[svc][role][hash] = range[svc][role][hash];
                            }
                        }
                        else
                        {
                            mslist[svc][role].Add(hash, range[svc][role][hash]);
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
            // Process new SOAP deltas
            if (null != findMembership && null != findMembership.Services)
            {
                foreach (ServiceType serviceType in findMembership.Services)
                {
                    Service currentService = new Service();
                    currentService.Id = int.Parse(serviceType.Info.Handle.Id);
                    currentService.ServiceType = serviceType.Info.Handle.Type;
                    currentService.LastChange = serviceType.LastChange;
                    currentService.ForeignId = serviceType.Info.Handle.ForeignId;

                    if (serviceType.Deleted)
                    {
                        if (xmlcl.MembershipList.ContainsKey(currentService))
                        {
                            xmlcl.MembershipList.Remove(currentService);
                        }
                    }
                    else
                    {
                        if (ServiceFilterType.Messenger == currentService.ServiceType)
                        {                            
                            if (!xmlcl.MembershipList.ContainsKey(currentService))
                            {
                                xmlcl.MembershipList.Add(currentService, new SerializableDictionary<MemberRole, SerializableDictionary<string, BaseMember>>(0));
                            }

                            if (xmlcl.MembershipLastChange < currentService.LastChange && null != serviceType.Memberships)
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
                                                PassportMember pm = bm as PassportMember;
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
                                                    xmlcl.RemoveMemberhip(currentService.ServiceType, account, type, memberrole);
                                                }
                                                else
                                                {
                                                    xmlcl.AddMemberhip(currentService.ServiceType, account, type, memberrole, bm);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            xmlcl.MembershipLastChange = currentService.LastChange;
                        }
                    }
                }
            }

            // Create/Update/Delete Memberships
            Service MsngrService = xmlcl.GetTargetService(ServiceFilterType.Messenger);
            if (MsngrService != null)
            {
                foreach (MemberRole role in xmlcl.MembershipList[MsngrService].Keys)
                {
                    foreach (BaseMember bm in xmlcl.MembershipList[MsngrService][role].Values)
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

                        string displayname = bm.DisplayName == null ? account : bm.DisplayName;
                        Contact contact = xmlcl.NSMessageHandler.ContactList.GetContact(account, displayname, type);
                        contact.NSMessageHandler = xmlcl.NSMessageHandler;
                        MSNLists newlists = xmlcl.GetMSNLists(ServiceFilterType.Messenger, account, type);



                        // Set new lists.
                        contact.SetLists(newlists);
                    }
                }

                if (MsngrService != null && xmlcl.MembershipList[MsngrService].ContainsKey(MemberRole.Reverse))
                {
                    foreach (Contact contact in xmlcl.NSMessageHandler.ContactList.All)
                    {
                        // Fire ReverseRemoved, Don't trust NSMessageHandler.OnRMLReceived, not fired everytime.
                        if (xmlcl.MembershipList[MsngrService][MemberRole.Reverse].ContainsKey(Contact.MakeHash(contact.Mail, contact.ClientType))
                            && (!contact.OnReverseList))
                        {
                            xmlcl.NSMessageHandler.ContactService.OnReverseRemoved(new ContactEventArgs(contact));
                        }
                    }
                }
            }

            return xmlcl;
        }


        #endregion


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
                if (null != forwardList.contacts)
                {
                    foreach (ContactType contactType in forwardList.contacts)
                    {
                        if (null != contactType.contactInfo)
                        {
                            if (contactType.fDeleted)
                            {
                                xmlcl.AddressbookContacts.Remove(new Guid(contactType.contactId));
                            }
                            else
                            {
                                xmlcl.AddressbookContacts[new Guid(contactType.contactId)] = contactType;
                            }
                        }
                    }
                }

                if (null != forwardList.groups)
                {
                    foreach (GroupType groupType in forwardList.groups)
                    {
                        Guid key = new Guid(groupType.groupId);
                        if (groupType.fDeleted)
                        {
                            xmlcl.Groups.Remove(key);
                        }
                        else
                        {
                            xmlcl.Groups[key] = groupType;
                        }
                    }
                }

                // Update lastchange
                xmlcl.AddressbookLastChange = forwardList.ab.lastChange;
                xmlcl.DynamicItemLastChange = forwardList.ab.DynamicItemLastChanged;
            }

            // Create Groups
            foreach (GroupType group in xmlcl.Groups.Values)
            {
                xmlcl.NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.groupInfo.name, group.groupId, xmlcl.NSMessageHandler));
            }

            // Create the Forward List and Email Contacts
            foreach (ContactType ct in xmlcl.AddressbookContacts.Values)
            {
                contactInfoType cit = ct.contactInfo;
                ClientType type = ClientType.PassportMember;
                string account = cit.passportName;
                string displayname = cit.displayName;
                bool ismessengeruser = cit.isMessengerUser;

                if (cit.emails != null && account == null)
                {
                    type = ClientType.EmailMember;
                    account = cit.emails[0].email;
                    ismessengeruser |= cit.emails[0].isMessengerEnabled;
                    displayname = String.IsNullOrEmpty(cit.quickName) ? account : cit.quickName;
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
                        Contact contact = xmlcl.NSMessageHandler.ContactList.GetContact(account, type);
                        contact.NSMessageHandler = xmlcl.NSMessageHandler;
                        contact.SetGuid(new Guid(ct.contactId));
                        contact.SetHasBlog(cit.hasSpace);
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
                                if (anno.Name == "AB.NickName" && anno.Value != null)
                                {
                                    displayname = anno.Value;
                                    break;
                                }
                            }
                        }

                        if (cit.groupIds != null)
                        {
                            foreach (string groupId in cit.groupIds)
                            {
                                contact.ContactGroups.Add(xmlcl.NSMessageHandler.ContactGroups[groupId]);
                            }
                        }

                        if (cit.groupIdsDeleted != null)
                        {
                            foreach (string groupId in cit.groupIdsDeleted)
                            {
                                contact.ContactGroups.Remove(xmlcl.NSMessageHandler.ContactGroups[groupId]);
                            }
                        }
                    }
                    else
                    {
                        if (displayname == xmlcl.NSMessageHandler.Owner.Mail && !String.IsNullOrEmpty(xmlcl.NSMessageHandler.Owner.Name))
                        {
                            displayname = xmlcl.NSMessageHandler.Owner.Name;
                        }

                        xmlcl.Profile.DisplayName = displayname;
                        xmlcl.Profile.CID = cit.CID;

                        if (null != cit.annotations)
                        {
                            foreach (Annotation anno in cit.annotations)
                            {
                                string name = anno.Name;
                                string value = anno.Value;
                                name = name.Substring(name.LastIndexOf(".") + 1).ToLower(CultureInfo.InvariantCulture);
                                xmlcl.MyProperties[name] = value;
                            }
                        }

                        if (!xmlcl.MyProperties.ContainsKey("mbea"))
                            xmlcl.MyProperties["mbea"] = "0";

                        if (!xmlcl.MyProperties.ContainsKey("gtc"))
                            xmlcl.MyProperties["gtc"] = "1";

                        if (!xmlcl.MyProperties.ContainsKey("blp"))
                            xmlcl.MyProperties["blp"] = "0";

                        if (!xmlcl.MyProperties.ContainsKey("roamliveproperties"))
                            xmlcl.MyProperties["roamliveproperties"] = "1";
                    }
                }
            }

            //Update dynamic items
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
                                    if (xmlcl.Profile.DateModified < notifydata.LastChanged)
                                    {
                                        xmlcl.Profile.GetFromStorageService = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (PassportDynamicItem dyItem in xmlcl.NSMessageHandler.ContactService.Deltas.DynamicItems.Values)
            {
                if (xmlcl.NSMessageHandler.ContactList.HasContact(dyItem.PassportName, ClientType.PassportMember))
                {
                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam))
                    {
                        xmlcl.NSMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.HasNew); //TODO: Type
                    }

                    if ((dyItem.ProfileStatus == "Exist Access" && dyItem.ProfileGleam == false) ||
                        (dyItem.SpaceStatus == "Exist Access" && dyItem.SpaceGleam == false))
                    {
                        xmlcl.NSMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.Viewed); //TODO: Type
                    }

                    if (dyItem.ProfileStatus == null && dyItem.SpaceStatus == null)  //"Exist Access" means the contact has space or profile
                    {
                        xmlcl.NSMessageHandler.ContactList[dyItem.PassportName].SetdynamicItemChanged(DynamicItemState.None); //TODO: Type
                    }
                }
            }

            return xmlcl;
        }

        #endregion

        #region Profile
        private OwnerProfile profile = new OwnerProfile();

        /// <summary>
        /// Profile of current user.
        /// </summary>
        [XmlElement("Profile")]
        public OwnerProfile Profile
        {
            get
            {
                return profile;
            }
            set
            {
                profile = value;
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
