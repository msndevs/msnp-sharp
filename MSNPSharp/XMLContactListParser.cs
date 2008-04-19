using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MSNPSharp
{
    internal enum MembershipListChildNodes
    {
        LastChanged = 0,
        Services = 1
    }

    internal enum ServiceChildNodes
    {
        Id = 0,
        Type = 1,
        LastChange = 2,
        Memberships = 3
    }

    internal enum MembershipsChildNodes
    {
        MemberRole = 0,
        Members = 1
    }

    internal enum MemberChildNodes
    {
        MembershipId = 0,
        Account = 1,
        DisplayName = 2,
        LastChanged = 3,
        Type = 4
    }

    internal enum AddressBookChildNodes
    {
        LastChanged = 0,
        DynamicItemLastChange = 1,
        Groups = 2,
        Contacts = 3,
        Settings = 4
    }


    internal enum GroupChildNodes
    {
        Guid = 0,
        Name = 1
    }

    internal enum ContactChildNodes
    {
        ContactId = 0,
        Account = 1,
        DisplayName = 2,
        IsMessengerUser = 3,
        Groups = 4,
        LastChanged = 5
    }

    /// <summary>
    /// Parser used to parse the contact list saved in the mcl file.
    /// </summary>
    internal class XMLContactListParser
    {
        XmlDocument parserRoot = null;
        private Dictionary<MemberRole, Dictionary<string, ContactInfo>> memberShips =
            new Dictionary<MemberRole, Dictionary<string, ContactInfo>>(0);

        public Dictionary<MemberRole, Dictionary<string, ContactInfo>> MemberShips
        {
            get { return memberShips; }
            //set { memberShips = value; }
        }

        private Dictionary<string, ContactInfo> contactList = new Dictionary<string, ContactInfo>(0);

        public Dictionary<string, ContactInfo> ContactList
        {
            get { return contactList; }
            //set { contactList = value; }
        }

        private Dictionary<string, GroupInfo> groupList = new Dictionary<string, GroupInfo>(0);

        public Dictionary<string, GroupInfo> GroupList
        {
            get { return groupList; }
            //set { groupList = value; }
        }

        private Dictionary<int, Service> serviceList = new Dictionary<int, Service>(0);

        public Dictionary<int, Service> ServiceList
        {
            get { return serviceList; }
            //set { serviceList = value; }
        }

        private DateTime membershipLastChange;

        public DateTime MembershipLastChange
        {
            get { return membershipLastChange; }
            //set { membershipLastChange = value; }
        }

        private DateTime addressBookLastChange;

        public DateTime AddressBookLastChange
        {
            get { return addressBookLastChange; }
            //set { addressBookLastChange = value; }
        }

        private DateTime dynamicItemLastChange;

        public DateTime DynamicItemLastChange
        {
            get { return dynamicItemLastChange; }
        }

        protected XMLContactListParser()
        {
        }

        public XMLContactListParser(XmlDocument doc)
        {
            parserRoot = doc;

        }

        public void Parse()
        {
            //Parse the membership list
            XmlNode membershiplist = parserRoot.GetElementsByTagName(XMLContactListTags.MembershipList.ToString())[0];
            if (membershiplist.HasChildNodes && membershiplist.InnerXml != "")
            {
                membershipLastChange = XmlConvert.ToDateTime(
                    membershiplist.ChildNodes[(int)MembershipListChildNodes.LastChanged].InnerText,
                    XmlDateTimeSerializationMode.RoundtripKind);
                XmlNode serviceRoot = membershiplist.ChildNodes[(int)MembershipListChildNodes.Services];
                if (serviceRoot.HasChildNodes)
                    GetServices(serviceRoot.FirstChild);
            }

            //Parse the addressbook(forward list)
            XmlNode addressbook = parserRoot.GetElementsByTagName(XMLContactListTags.AddressBook.ToString())[0];
            if (addressbook.HasChildNodes && addressbook.InnerXml != "")
            {
                addressBookLastChange = XmlConvert.ToDateTime(
                    addressbook.ChildNodes[(int)AddressBookChildNodes.LastChanged].InnerText,
                    XmlDateTimeSerializationMode.RoundtripKind);
                dynamicItemLastChange = XmlConvert.ToDateTime(
                    addressbook.ChildNodes[(int)AddressBookChildNodes.DynamicItemLastChange].InnerText,
                    XmlDateTimeSerializationMode.RoundtripKind);
                GetGroups(addressbook.ChildNodes[(int)AddressBookChildNodes.Groups].FirstChild);
                GetContacts(addressbook.ChildNodes[(int)AddressBookChildNodes.Contacts].FirstChild);
            }
        }

        #region Get Membership list

        private void GetServices(XmlNode currentServiceNode)
        {
            do
            {
                if (currentServiceNode == null || currentServiceNode.InnerXml == "")
                    return;
                Service currentService = new Service();
                currentService.Id = int.Parse(currentServiceNode.ChildNodes[(int)ServiceChildNodes.Id].InnerText);
                currentService.LastChange = XmlConvert.ToDateTime(
                    currentServiceNode.ChildNodes[(int)ServiceChildNodes.LastChange].InnerText, XmlDateTimeSerializationMode.RoundtripKind);
                currentService.Type = currentServiceNode.ChildNodes[(int)ServiceChildNodes.Type].InnerText;
                serviceList.Add(currentService.Id, currentService);
                GetMemberRoles(currentServiceNode.ChildNodes[(int)ServiceChildNodes.Memberships].FirstChild);
                currentServiceNode = currentServiceNode.NextSibling;
            }
            while (true);
        }

        private void GetMemberRoles(XmlNode currentMembershipNode)
        {
            do
            {
                if (currentMembershipNode == null || currentMembershipNode.InnerXml == "")
                    return;
                string strrole = currentMembershipNode.ChildNodes[(int)MembershipsChildNodes.MemberRole].InnerText;
                if (Enum.IsDefined(typeof(MemberRole), strrole))
                {
                    MemberRole role = (MemberRole)Enum.Parse(typeof(MemberRole), strrole);
                    if (!MemberShips.ContainsKey(role))
                    {
                        MemberShips.Add(role, new Dictionary<string, ContactInfo>(0));
                    }
                    GetMembers(role, currentMembershipNode.ChildNodes[(int)MembershipsChildNodes.Members].FirstChild);
                }
                currentMembershipNode = currentMembershipNode.NextSibling;
            }
            while (true);
        }

        private void GetMembers(MemberRole role, XmlNode currentMemberNode)
        {
            do
            {
                if (currentMemberNode == null || currentMemberNode.InnerXml == "")
                    return;
                ContactInfo contact = new ContactInfo();
                contact.Account = currentMemberNode.ChildNodes[(int)MemberChildNodes.Account].InnerText;
                contact.DisplayName = currentMemberNode.ChildNodes[(int)MemberChildNodes.DisplayName].InnerText;
                contact.MembershipId = int.Parse(currentMemberNode.ChildNodes[(int)MemberChildNodes.MembershipId].InnerText);
                contact.Type = (ClientType)int.Parse(currentMemberNode.ChildNodes[(int)MemberChildNodes.Type].InnerText);
                contact.LastChanged = XmlConvert.ToDateTime(
                    currentMemberNode.ChildNodes[(int)MemberChildNodes.LastChanged].InnerText,
                    XmlDateTimeSerializationMode.RoundtripKind);
                MemberShips[role].Add(contact.Account, contact);
                currentMemberNode = currentMemberNode.NextSibling;
            }
            while (true);
        }
        #endregion

        #region Get AB contact list

        private void GetGroups(XmlNode currentGroupNode)
        {
            do
            {
                if (currentGroupNode == null || currentGroupNode.InnerXml == "")
                    return;
                GroupInfo group = new GroupInfo();
                group.Guid = currentGroupNode.ChildNodes[(int)GroupChildNodes.Guid].InnerText;
                group.Name = currentGroupNode.ChildNodes[(int)GroupChildNodes.Name].InnerText;
                GroupList.Add(group.Guid, group);
                currentGroupNode = currentGroupNode.NextSibling;
            }
            while (true);
        }

        private void GetContacts(XmlNode currentContactNode)
        {
            do
            {
                if (currentContactNode == null || currentContactNode.InnerXml == "")
                    return;
                ContactInfo contact = new ContactInfo();
                contact.Account = currentContactNode.ChildNodes[(int)ContactChildNodes.Account].InnerText;
                contact.DisplayName = currentContactNode.ChildNodes[(int)ContactChildNodes.DisplayName].InnerText;
                contact.Guid = currentContactNode.ChildNodes[(int)ContactChildNodes.ContactId].InnerText;
                contact.IsMessengerUser = bool.Parse(currentContactNode.ChildNodes[(int)ContactChildNodes.IsMessengerUser].InnerText);
                XmlNode groupsRoot = currentContactNode.ChildNodes[(int)ContactChildNodes.Groups];
                if (groupsRoot.HasChildNodes)
                    foreach (XmlNode group in groupsRoot.ChildNodes)
                    {
                        contact.Groups.Add(group.InnerText);
                    }
                contact.LastChanged = XmlConvert.ToDateTime(
                    currentContactNode.ChildNodes[(int)ContactChildNodes.LastChanged].InnerText,
                    XmlDateTimeSerializationMode.RoundtripKind);
                contactList.Add(contact.Account, contact);
                currentContactNode = currentContactNode.NextSibling;
            }
            while (true);
        }
        #endregion
    }
}
