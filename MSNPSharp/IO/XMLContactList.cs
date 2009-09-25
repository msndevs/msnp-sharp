#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
    using MSNPSharp.Core;

    /// <summary>
    /// ContactList file maintainer
    /// </summary>
    [Serializable]
    [XmlRoot("ContactList")]
    public class XMLContactList : MCLSerializer
    {
        public static XMLContactList LoadFromFile(string filename, MclSerialization st, NSMessageHandler handler, bool useCache)
        {
            return (XMLContactList)LoadFromFile(filename, st, typeof(XMLContactList), handler, useCache);
        }

        /// <summary>
        /// Loads contacts from saved mcl file, merges the deltas list saved into current AddressBook and MemberShipList.
        /// </summary>
        /// <param name="deltas"></param>
        internal void Synchronize(DeltasList deltas)
        {
            // Create Memberships
            SerializableDictionary<string, SerializableDictionary<string, BaseMember>> ms =
                GetTargetMemberships(ServiceFilterType.Messenger);

            if (ms != null)
            {
                foreach (string role in ms.Keys)
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
                            contact.CID = cid;
                            contact.Lists |= msnlist;
                        }
                    }
                }
            }

            #region Create Groups

            foreach (GroupType group in Groups.Values)
            {
                NSMessageHandler.ContactGroups.AddGroup(new ContactGroup(group.groupInfo.name, group.groupId, NSMessageHandler));
            }

            #endregion

            #region Create Circles

            foreach (CircleInfo circle in CircleResults.Values)
            {
                if (circle.CircleResultInfo.Deleted)
                {
                    NSMessageHandler.CircleList.RemoveCircle(new Guid(circle.CircleResultInfo.Content.Handle.Id), circle.CircleResultInfo.Content.Info.HostedDomain);
                }
                else
                {
                    Circle newcircle = CombineCircle(circle.CircleMember, circle.CircleResultInfo);
                    CircleInviter initator = CombineCircleInviter(circle.CircleMember);

                    if (NSMessageHandler.CircleList[newcircle.Mail] == null)
                    {
                        if (newcircle.Role == CirclePersonalMembershipRole.StatePendingOutbound)
                        {
                            NSMessageHandler.ContactService.FireJoinCircleEvent(new JoinCircleInvitationEventArgs(newcircle, initator));
                            continue;
                        }
                        else
                        {
                            NSMessageHandler.CircleList.AddCircle(newcircle);
                        }
                    }
                }

                string id = circle.CircleResultInfo.Content.Handle.Id.ToLowerInvariant() + "@" + circle.CircleResultInfo.Content.Info.HostedDomain;
                MSNLists list = NSMessageHandler.ContactService.GetMSNList(circle.MemberRole);

                NSMessageHandler.CircleList[id].AddToList(list);
                if (list == MSNLists.BlockedList)
                    NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.AllowedList);

                if (list == MSNLists.AllowedList)
                    NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.BlockedList);
            }

            #endregion

            // Create the Forward List and Email Contacts
            foreach (ContactType contactType in AddressbookContacts.Values)
            {
                UpdateContact(contactType);
            }

            Merge(deltas);
        }

        #region New MembershipList

        private SerializableDictionary<string, ServiceMembership> mslist = new SerializableDictionary<string, ServiceMembership>(0);
        public SerializableDictionary<string, ServiceMembership> MembershipList
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

        public string MembershipLastChange
        {
            get
            {
                if (MembershipList.Keys.Count == 0)
                    return WebServiceConstants.ZeroTime;

                List<Service> services = new List<Service>();
                foreach (string sft in MembershipList.Keys)
                    services.Add(new Service(MembershipList[sft].Service));

                services.Sort();
                return services[services.Count - 1].LastChange;
            }
        }

        internal Service GetTargetService(string type)
        {
            if (MembershipList.ContainsKey(type))
                return MembershipList[type].Service;

            return null;
        }

        internal SerializableDictionary<string, SerializableDictionary<string, BaseMember>> GetTargetMemberships(string serviceFilterType)
        {
            if (MembershipList.ContainsKey(serviceFilterType))
                return MembershipList[serviceFilterType].Memberships;

            return null;
        }

        public void AddMemberhip(string servicetype, string account, ClientType type, string memberrole, BaseMember member)
        {
            SerializableDictionary<string, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(servicetype);
            if (ms != null)
            {
                if (!ms.ContainsKey(memberrole))
                    ms.Add(memberrole, new SerializableDictionary<string, BaseMember>(0));

                ms[memberrole][Contact.MakeHash(account, type)] = member;
            }
        }

        public void RemoveMemberhip(string servicetype, string account, ClientType type, string memberrole)
        {
            SerializableDictionary<string, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(servicetype);
            if (ms != null)
            {
                string hash = Contact.MakeHash(account, type);
                if (ms.ContainsKey(memberrole) && ms[memberrole].ContainsKey(hash))
                {
                    ms[memberrole].Remove(hash);
                }
            }
        }

        public bool HasMemberhip(string servicetype, string account, ClientType type, string memberrole)
        {
            SerializableDictionary<string, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(servicetype);
            return (ms != null) && ms.ContainsKey(memberrole) && ms[memberrole].ContainsKey(Contact.MakeHash(account, type));
        }

        public BaseMember GetBaseMember(string servicetype, string account, ClientType type, string memberrole)
        {
            string hash = Contact.MakeHash(account, type);
            SerializableDictionary<string, SerializableDictionary<string, BaseMember>> ms = GetTargetMemberships(servicetype);
            if ((ms != null) && ms.ContainsKey(memberrole) && ms[memberrole].ContainsKey(hash))
            {
                return ms[memberrole][hash];
            }
            return null;
        }

        public virtual void Add(
            Dictionary<Service,
            Dictionary<string,
            Dictionary<string, BaseMember>>> range)
        {
            foreach (Service svc in range.Keys)
            {
                foreach (string role in range[svc].Keys)
                {
                    foreach (string hash in range[svc][role].Keys)
                    {
                        if (!mslist.ContainsKey(svc.ServiceType))
                            mslist.Add(svc.ServiceType, new ServiceMembership(svc));

                        if (!mslist[svc.ServiceType].Memberships.ContainsKey(role))
                            mslist[svc.ServiceType].Memberships.Add(role, new SerializableDictionary<string, BaseMember>(0));

                        if (mslist[svc.ServiceType].Memberships[role].ContainsKey(hash))
                        {
                            if (/* mslist[svc.ServiceType].Memberships[role][hash].LastChangedSpecified
                                && */
                                WebServiceDateTimeConverter.ConvertToDateTime(mslist[svc.ServiceType].Memberships[role][hash].LastChanged).CompareTo(
                                WebServiceDateTimeConverter.ConvertToDateTime(range[svc][role][hash].LastChanged)) <= 0)
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

        public XMLContactList Merge(DeltasList deltas)
        {
            if (deltas.MembershipDeltas.Count > 0)
            {
                foreach (FindMembershipResultType membershipResult in deltas.MembershipDeltas)
                {
                    Merge(membershipResult);
                }
            }

            if (deltas.AddressBookDeltas.Count > 0)
            {
                foreach (ABFindContactsPagedResultType findcontactspagedResult in deltas.AddressBookDeltas)
                {
                    Merge(findcontactspagedResult);
                }

            }

            return this;
        }

        public XMLContactList Merge(FindMembershipResultType findMembership)
        {
            // Process new FindMemberships (deltas)
            if (null != findMembership && null != findMembership.Services)
            {
                foreach (ServiceType serviceType in findMembership.Services)
                {
                    Service oldService = GetTargetService(serviceType.Info.Handle.Type);

                    if (oldService == null ||
                        WebServiceDateTimeConverter.ConvertToDateTime(oldService.LastChange)
                        < WebServiceDateTimeConverter.ConvertToDateTime(serviceType.LastChange))
                    {
                        if (serviceType.Deleted)
                        {
                            if (MembershipList.ContainsKey(serviceType.Info.Handle.Type))
                            {
                                MembershipList.Remove(serviceType.Info.Handle.Type);
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
                                MembershipList.Add(updatedService.ServiceType, new ServiceMembership(updatedService));
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
                                            string memberrole = membership.MemberRole;
                                            List<BaseMember> members = new List<BaseMember>(membership.Members);
                                            members.Sort(CompareBaseMembers);

                                            foreach (BaseMember bm in members)
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
                                                else if (bm is CircleMember)
                                                {
                                                    type = ClientType.CircleMember;
                                                    account = ((CircleMember)bm).CircleId;
                                                    if (!circlesMembership.ContainsKey(memberrole))
                                                    {
                                                        circlesMembership.Add(memberrole, new List<CircleMember>(0));
                                                    }
                                                    circlesMembership[memberrole].Add(bm as CircleMember);
                                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, serviceType.Info.Handle.Type + " Membership " + bm.GetType().ToString() + ": " + memberrole + ":" + account);
                                                }

                                                if (account != null && type != ClientType.None)
                                                {
                                                    account = account.ToLowerInvariant();
                                                    MSNLists msnlist = NSMessageHandler.ContactService.GetMSNList(memberrole);

                                                    if (bm.Deleted)
                                                    {
                                                        #region Members deleted in other clients.

                                                        if (type != ClientType.CircleMember)
                                                        {
                                                            if (HasMemberhip(updatedService.ServiceType, account, type, memberrole) &&
                                                                WebServiceDateTimeConverter.ConvertToDateTime(MembershipList[updatedService.ServiceType].Memberships[memberrole][Contact.MakeHash(account, type)].LastChanged)
                                                                < WebServiceDateTimeConverter.ConvertToDateTime(bm.LastChanged))
                                                            {
                                                                RemoveMemberhip(updatedService.ServiceType, account, type, memberrole);
                                                            }

                                                            if (NSMessageHandler.ContactList.HasContact(account, type))
                                                            {
                                                                Contact contact = NSMessageHandler.ContactList.GetContact(account, type);
                                                                contact.CID = cid;
                                                                if (contact.HasLists(msnlist))
                                                                {
                                                                    contact.RemoveFromList(msnlist);

                                                                    // Fire ReverseRemoved
                                                                    if (msnlist == MSNLists.ReverseList)
                                                                    {
                                                                        NSMessageHandler.ContactService.OnReverseRemoved(new ContactEventArgs(contact));
                                                                    }

                                                                    // Send a list remove event
                                                                    NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, msnlist));
                                                                }
                                                            }
                                                        }

                                                        #endregion

                                                    }
                                                    else
                                                    {
                                                        #region Members added in other clients.

                                                        if (type != ClientType.CircleMember)
                                                        {
                                                            if (false == MembershipList[updatedService.ServiceType].Memberships.ContainsKey(memberrole) ||
                                                                /*new*/ false == MembershipList[updatedService.ServiceType].Memberships[memberrole].ContainsKey(Contact.MakeHash(account, type)) ||
                                                                /*probably membershipid=0*/ WebServiceDateTimeConverter.ConvertToDateTime(bm.LastChanged)
                                                                > WebServiceDateTimeConverter.ConvertToDateTime(MembershipList[updatedService.ServiceType].Memberships[memberrole][Contact.MakeHash(account, type)].LastChanged))
                                                            {
                                                                AddMemberhip(updatedService.ServiceType, account, type, memberrole, bm);
                                                            }

                                                            string displayname = bm.DisplayName == null ? account : bm.DisplayName;
                                                            Contact contact = NSMessageHandler.ContactList.GetContact(account, displayname, type);
                                                            contact.CID = cid;

                                                            if (!contact.HasLists(msnlist))
                                                            {
                                                                contact.AddToList(msnlist);

                                                                // Don't fire ReverseAdded(contact.Pending) here... It fires 2 times:
                                                                // The first is OnConnect after abSynchronized
                                                                // The second is here, not anymore here :)
                                                                // The correct place is in OnADLReceived.

                                                                // Send a list add event
                                                                NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, msnlist));
                                                            }
                                                        }

                                                        #endregion
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
                                            string memberrole = membership.MemberRole;
                                            List<BaseMember> members = new List<BaseMember>(membership.Members);
                                            members.Sort(CompareBaseMembers);
                                            foreach (BaseMember bm in members)
                                            {
                                                string account = null;
                                                ClientType type = ClientType.None;

                                                switch (bm.Type)
                                                {
                                                    case MembershipType.Passport:
                                                        type = ClientType.PassportMember;
                                                        PassportMember pm = bm as PassportMember;
                                                        if (!pm.IsPassportNameHidden)
                                                        {
                                                            account = pm.PassportName;
                                                        }
                                                        break;

                                                    case MembershipType.Email:
                                                        type = ClientType.EmailMember;
                                                        account = ((EmailMember)bm).Email;
                                                        break;

                                                    case MembershipType.Phone:
                                                        type = ClientType.PhoneMember;
                                                        account = ((PhoneMember)bm).PhoneNumber;
                                                        break;

                                                    case MembershipType.Role:
                                                    case MembershipType.Service:
                                                    case MembershipType.Everyone:
                                                    case MembershipType.Partner:
                                                        account = bm.Type + "/" + bm.MembershipId;
                                                        break;

                                                    case MembershipType.Domain:
                                                        account = ((DomainMember)bm).DomainName;
                                                        break;

                                                    case MembershipType.Circle:
                                                        type = ClientType.CircleMember;
                                                        account = ((CircleMember)bm).CircleId;
                                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, serviceType.Info.Handle.Type + " Membership " + bm.GetType().ToString() + ": " + memberrole + ":" + account);
                                                        break;
                                                }

                                                if (account != null)
                                                {
                                                    if (bm.Deleted)
                                                    {
                                                        RemoveMemberhip(updatedService.ServiceType, account, type, memberrole);
                                                    }
                                                    else
                                                    {
                                                        AddMemberhip(updatedService.ServiceType, account, type, memberrole, bm);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }

                            // Update service.LastChange
                            MembershipList[updatedService.ServiceType].Service = updatedService;
                        }
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Merge changes into membership list and add membership contacts
        /// </summary>
        /// <param name="xmlcl"></param>
        /// <param name="findMembership"></param>
        /// <returns></returns>
        public static XMLContactList operator +(XMLContactList xmlcl, FindMembershipResultType findMembership)
        {
            return xmlcl.Merge(findMembership);
        }

        private static int CompareBaseMembers(BaseMember x, BaseMember y)
        {
            return x.LastChanged.CompareTo(y.LastChanged);
        }


        #endregion

        #region Addressbook

        string abLastChange = WebServiceConstants.ZeroTime;

        SerializableDictionary<string, string> myproperties = new SerializableDictionary<string, string>(0);
        SerializableDictionary<Guid, GroupType> groups = new SerializableDictionary<Guid, GroupType>(0);
        SerializableDictionary<Guid, ContactType> abcontacts = new SerializableDictionary<Guid, ContactType>(0);
        SerializableDictionary<string, CircleInfo> circleResults = new SerializableDictionary<string, CircleInfo>(0);

        SerializableDictionary<string, List<CircleMember>> circlesMembership = new SerializableDictionary<string, List<CircleMember>>(0);

        public SerializableDictionary<string, CircleInfo> CircleResults
        {
            get
            {
                return circleResults;
            }
            set
            {
                circleResults = value;
            }
        }

        [XmlElement("AddressbookLastChange")]
        public string AddressbookLastChange
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

        public XMLContactList Merge(ABFindContactsPagedResultType forwardList)
        {
            #region AddressBook changed

            DateTime dt1 = WebServiceDateTimeConverter.ConvertToDateTime(AddressbookLastChange);
            DateTime dt2 = WebServiceDateTimeConverter.ConvertToDateTime(forwardList.Ab.lastChange);

            if (forwardList.Ab != null && forwardList.Ab.abId != WebServiceConstants.MessengerAddressBookId)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Getting non-default addressbook: \r\nId: " +
                    forwardList.Ab.abId + "\r\nName: " + forwardList.Ab.abInfo.name +
                    "\r\nType: " + forwardList.Ab.abInfo.AddressBookType + "\r\nMembers:");

                string id = forwardList.Ab.abId + "@" + CircleString.DefaultHostDomain;
                foreach (ContactType contact in forwardList.Contacts)
                {
                    if (NSMessageHandler.Owner != null && contact.contactInfo.CID == NSMessageHandler.Owner.CID)
                    {
                        if (NSMessageHandler.CircleList[id] != null)
                        {
                            //This is the return of CircleIdAlert, correct the contactId.
                            NSMessageHandler.CircleList[id].Guid = new Guid(contact.contactId);
                            CircleResults[id].CircleMember.contactId = contact.contactId;
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Circle Id changed, new Id is: " + contact.contactId);
                        }
                    }
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, " DisplayName: " + contact.contactInfo.displayName + " Type: " + contact.contactInfo.contactType);
                }
            }

            if (forwardList.Ab != null &&
                WebServiceDateTimeConverter.ConvertToDateTime(AddressbookLastChange) <
                WebServiceDateTimeConverter.ConvertToDateTime(forwardList.Ab.lastChange)
                && forwardList.Ab.abId == WebServiceConstants.MessengerAddressBookId)
            {
                if (null != forwardList.Groups)
                {
                    foreach (GroupType groupType in forwardList.Groups)
                    {
                        Guid key = new Guid(groupType.groupId);
                        if (groupType.fDeleted)
                        {
                            Groups.Remove(key);

                            ContactGroup contactGroup = NSMessageHandler.ContactGroups[groupType.groupId];
                            if (contactGroup != null)
                            {
                                NSMessageHandler.ContactGroups.RemoveGroup(contactGroup);
                                NSMessageHandler.ContactService.OnContactGroupRemoved(new ContactGroupEventArgs(contactGroup));
                            }
                        }
                        else
                        {
                            Groups[key] = groupType;

                            // Add a new group									
                            NSMessageHandler.ContactGroups.AddGroup(
                                new ContactGroup(System.Web.HttpUtility.UrlDecode(groupType.groupInfo.name), groupType.groupId, NSMessageHandler));

                            // Fire the event
                            NSMessageHandler.ContactService.OnContactGroupAdded(
                                new ContactGroupEventArgs(NSMessageHandler.ContactGroups[groupType.groupId]));
                        }
                    }
                }



                if (null != forwardList.Contacts)
                {
                    foreach (ContactType contactType in forwardList.Contacts)
                    {
                        if (null != contactType.contactInfo)
                        {
                            if (contactType.CreatedBy == "96" &&
                                contactType.LastModifiedBy == ((int)ClientType.CircleMember).ToString() &&
                                contactType.contactInfo.contactType == MessengerContactType.Regular)
                            {
                                //A deleted or modified circle;
                                continue;
                            }

                            if (contactType.CreatedBy == "96" &&
                                contactType.contactInfo.contactType == MessengerContactType.Circle)
                            {
                                //A new circle;
                                continue;
                            }

                            Contact contact = NSMessageHandler.ContactList.GetContactByGuid(new Guid(contactType.contactId));

                            if (contactType.fDeleted)
                            {
                                AddressbookContacts.Remove(new Guid(contactType.contactId));

                                if (contact != null)
                                {
                                    contact.RemoveFromList(MSNLists.ForwardList);
                                    NSMessageHandler.ContactService.OnContactRemoved(new ListMutateEventArgs(contact, MSNLists.ForwardList));

                                    contact.Guid = Guid.Empty;
                                    contact.SetIsMessengerUser(false);

                                    if (MSNLists.None == contact.Lists)
                                    {
                                        NSMessageHandler.ContactList.Remove(contact.Mail, contact.ClientType);
                                        contact.NSMessageHandler = null;
                                    }
                                }
                            }
                            else
                            {
                                AddressbookContacts[new Guid(contactType.contactId)] = contactType;
                                UpdateContact(contactType);
                                NSMessageHandler.ContactService.OnContactAdded(new ListMutateEventArgs(contact, MSNLists.ForwardList));
                            }
                        }
                    }
                }

                if (forwardList.Ab != null)
                {
                    // Update lastchange
                    AddressbookLastChange = forwardList.Ab.lastChange;
                }
            }

            #endregion

            #region Circle changed

            if (forwardList.CircleResult != null)
            {
                if (null != forwardList.CircleResult.Circles)
                {
                    List<ContactType> circleContactsAdded = new List<ContactType>(0);
                    List<CircleInverseInfoType> circleAdded = new List<CircleInverseInfoType>(0);

                    if (forwardList.Contacts != null)
                    {
                        foreach (ContactType contactType in forwardList.Contacts)
                        {
                            if (contactType.CreatedBy == "96" &&
                                contactType.contactInfo.contactType == MessengerContactType.Circle &&
                                contactType.fDeleted == false)
                            {
                                circleContactsAdded.Add(contactType);
                            }
                        }
                    }

                    foreach (CircleInverseInfoType circle in forwardList.CircleResult.Circles)
                    {
                        if (circle.Deleted)
                        {
                            CircleResults.Remove(circle.Content.Handle.Id.ToLowerInvariant() + "@" + circle.Content.Info.HostedDomain.ToLowerInvariant());
                            NSMessageHandler.CircleList.RemoveCircle(new Guid(circle.Content.Handle.Id), circle.Content.Info.HostedDomain.ToLowerInvariant());
                        }
                        else
                        {
                            circleAdded.Add(circle);
                        }
                    }

                    if (circleContactsAdded.Count == circleAdded.Count)
                    {
                        for (int i = 0; i < circleAdded.Count; i++)
                        {
                            CircleInverseInfoType circleinfo = circleAdded[i];
                            ContactType contactType = circleContactsAdded[i];

                            string circleId = circleinfo.Content.Handle.Id.ToLowerInvariant() + "@" + circleinfo.Content.Info.HostedDomain.ToLowerInvariant();

                            bool newadded = true;
                            string memberRole = MemberRole.Allow;

                            if (CircleResults.ContainsKey(circleId))
                            {
                                if (CircleResults[circleId].CircleResultInfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role
                                    == circleinfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role)
                                {
                                    newadded = false;
                                    memberRole = CircleResults[circleId].MemberRole;
                                }
                                else
                                {
                                    //This will prevent 933 server error.
                                    //this case means the owner left a circle using another client, then the remote client reinvit the owner.
                                    CircleResults.Remove(circleId);
                                    NSMessageHandler.CircleList.RemoveCircle(new Guid(circleinfo.Content.Handle.Id), circleinfo.Content.Info.HostedDomain);
                                }
                            }

                            CircleResults[circleId] = new CircleInfo(contactType, circleinfo);  //Refresh the info.
                            CircleResults[circleId].MemberRole = memberRole;

                            if (newadded)
                            {
                                Circle newcircle = CombineCircle(contactType, circleinfo);
                                CircleInviter initator = CombineCircleInviter(contactType);

                                if (newcircle.Role == CirclePersonalMembershipRole.StatePendingOutbound)
                                {
                                    NSMessageHandler.ContactService.FireJoinCircleEvent(new JoinCircleInvitationEventArgs(newcircle, initator));
                                }
                                else
                                {
                                    NSMessageHandler.CircleList.AddCircle(newcircle);
                                }
                            }
                        }
                    }
                }
            }

            foreach (string memberRole in circlesMembership.Keys)
            {
                foreach (CircleMember member in circlesMembership[memberRole])
                {
                    string id = member.CircleId.ToLowerInvariant() + "@" + CircleString.DefaultHostDomain;

                    if (memberRole == MemberRole.Block)
                    {
                        if (member.Deleted)
                        {
                            //Deleted from block list.
                            if (CircleResults.ContainsKey(id))
                            {
                                CircleResults[id].MemberRole = MemberRole.Allow;
                                NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.BlockedList);
                                NSMessageHandler.CircleList[id].AddToList(MSNLists.AllowedList);
                            }
                        }
                        else
                        {
                            //Added to block list.
                            if (CircleResults.ContainsKey(id))
                            {
                                CircleResults[id].MemberRole = MemberRole.Block;
                                NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.AllowedList);
                                NSMessageHandler.CircleList[id].AddToList(MSNLists.BlockedList);
                            }
                        }
                    }

                    if (memberRole == MemberRole.Allow)
                    {
                        if (member.Deleted)
                        {
                            //Deleted from allow list.
                            if (CircleResults.ContainsKey(id))
                            {
                                CircleResults[id].MemberRole = MemberRole.Block;
                                NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.AllowedList);
                                NSMessageHandler.CircleList[id].AddToList(MSNLists.BlockedList);
                            }
                        }
                        else
                        {
                            //Added to allow list.
                            if (CircleResults.ContainsKey(id))
                            {
                                CircleResults[id].MemberRole = MemberRole.Allow;
                                NSMessageHandler.CircleList[id].RemoveFromList(MSNLists.BlockedList);
                                NSMessageHandler.CircleList[id].AddToList(MSNLists.AllowedList);
                            }
                        }
                    }
                }
            }

            circlesMembership.Clear();

            #endregion

            //NO DynamicItems any more

            return this;
        }

        /// <summary>
        /// Merge changes to addressbook and add address book contacts
        /// </summary>
        /// <param name="xmlcl">Addressbook</param>
        /// <param name="forwardList"></param>
        /// <returns></returns>
        public static XMLContactList operator +(XMLContactList xmlcl, ABFindContactsPagedResultType forwardList)
        {
            return xmlcl.Merge(forwardList);
        }

        private CircleInviter CombineCircleInviter(ContactType contact)
        {
            CircleInviter initator = null;

            if (contact.contactInfo.NetworkInfoList.Length > 0)
            {
                foreach (NetworkInfoType networkInfo in contact.contactInfo.NetworkInfoList)
                {
                    if (networkInfo.DomainId == 1)
                    {
                        initator = new CircleInviter(networkInfo.InviterEmail, networkInfo.InviterName, networkInfo.InviterMessage);
                    }
                }
            }

            return initator;
        }


        private Circle CombineCircle(ContactType contact, CircleInverseInfoType circleinfo)
        {
            Circle circle = new Circle(
                new Guid(circleinfo.Content.Handle.Id),
                new Guid(contact.contactId),
                circleinfo.Content.Info.HostedDomain,
                circleinfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role,
                circleinfo.Content.Info.DisplayName,
                NSMessageHandler);


            if (contact.contactInfo != null)
            {

                circle.CID = contact.contactInfo.CID;
            }

            return circle;
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
                foreach (contactEmailType cet in cit.emails)
                {
                    if (cet.isMessengerEnabled)
                    {
                        type = (ClientType)Enum.Parse(typeof(ClientType), cet.Capability);
                        account = cet.email;
                        ismessengeruser |= cet.isMessengerEnabled;
                        displayname = account;
                        break;
                    }
                }
            }

            if (cit.phones != null && account == null)
            {
                foreach (contactPhoneType cpt in cit.phones)
                {
                    if (cpt.isMessengerEnabled)
                    {
                        type = ClientType.PhoneMember;
                        account = cpt.number;
                        ismessengeruser |= cpt.isMessengerEnabled;
                        displayname = account;
                        break;
                    }
                }
            }

            if (account != null)
            {
                if (cit.contactType != MessengerContactType.Me)
                {
                    Contact contact = NSMessageHandler.ContactList.GetContact(account, type);
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

                    if (cit.contactType == MessengerContactType.Live &&
                        cit.NetworkInfoList != null &&
                        !String.IsNullOrEmpty(cit.NetworkInfoList[0].UserTileURL))
                    {
                        contact.UserTile = new Uri(cit.NetworkInfoList[0].UserTileURL);
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
                            MyProperties[anno.Name] = anno.Value;
                        }
                    }

                    InitializeMyProperties();
                }
            }
        }

        /// <summary>
        /// Set MyProperties to default value.
        /// </summary>
        public void InitializeMyProperties()
        {
            if (!MyProperties.ContainsKey(AnnotationNames.MSN_IM_MBEA))
                MyProperties[AnnotationNames.MSN_IM_MBEA] = "0";

            if (!MyProperties.ContainsKey(AnnotationNames.MSN_IM_GTC))
                MyProperties[AnnotationNames.MSN_IM_GTC] = "1";

            if (!MyProperties.ContainsKey(AnnotationNames.MSN_IM_BLP))
                MyProperties[AnnotationNames.MSN_IM_BLP] = "0";

            if (!MyProperties.ContainsKey(AnnotationNames.MSN_IM_MPOP))
                MyProperties[AnnotationNames.MSN_IM_MPOP] = "0";

            if (!MyProperties.ContainsKey(AnnotationNames.MSN_IM_RoamLiveProperties))
                MyProperties[AnnotationNames.MSN_IM_RoamLiveProperties] = "1";

            if (!MyProperties.ContainsKey(AnnotationNames.Live_Profile_Expression_LastChanged))
                MyProperties[AnnotationNames.Live_Profile_Expression_LastChanged] = XmlConvert.ToString(DateTime.MaxValue, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz");
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
