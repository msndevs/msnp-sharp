using System;
using System.Collections.Generic;
using System.Text;
using System.Xml ;
using System.Diagnostics;

namespace MSNPSharp
{
    /// <summary>
    /// Membership list FSM state
    /// </summary>
    internal enum msFSMState
    {
        Start = 0,
        GetListType = 1,
        GetMemberInfo = 2,
        ServiceInfo = 3,
        Accept = -1
    }

    /// <summary>
    /// Membership list FSM inputs
    /// </summary>
    internal enum msFSMInput
    {
        Other = 0,
        MemberRole = 1,
        Member = 2,
        PassportName = 3,
        Email = 4,
        DisplayName = 5,
        LastChanged = 6,
        MembershipId = 7,
        LastChange = 8,
        Id = 9,
        Type = 10,
        Info = 11,
        EOF = 12
    }

    /// <summary>
    /// Address Book FSM state
    /// </summary>
    internal enum abFSMState
    {
        Star = 0,
        GetCacheKey = 1,
        GetGroupInfo = 2,
        GetContactInfo = 3,
        AddressBookInfo = 4,
        Accept = -1
    }

    /// <summary>
    /// Address Book FSM inputs
    /// </summary>
    internal enum abFSMInput
    {
        Other = 0,
        CacheKey = 1,
        groupId = 2,
        name = 3,
        contactId = 4,
        email = 5,
        passportName = 6,
        guid = 7,
        displayName = 8,
        lastChange = 9,
        isMessengerEnabled = 10,
        isMessengerUser = 11,
        ab = 12,
        DynamicItemLastChanged = 13,
        EOF = 14
    }


    /// <summary>
    /// Parse the membership document and get all contacts.
    /// </summary>
    internal class MembershipParser : XMLNodeEnumerator
    {
        /// <summary>
        /// Membership list state machine table
        /// </summary>
        private static int[,] msStateTable = new int[5, 13]
        {
            {0,1,0,0,0,0,0,0,0,0,0,3,-1},
            {0,1,2,0,0,0,0,0,0,0,0,3,-1},
            {0,1,2,2,2,2,2,2,0,0,0,3,-1},
            {0,1,0,0,0,0,0,0,3,3,3,0,-1},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };

        int serviceId = 0;
        MSNLists currentList = MSNLists.AllowedList;
        ContactInfo lastContact = new ContactInfo();
        Service lastService;
        Dictionary<string, ContactInfo> currentRole = new Dictionary<string, ContactInfo>(0);
        Dictionary<string, ContactInfo> contactList = new Dictionary<string, ContactInfo>(0);
        Dictionary<MemberRole, Dictionary<string, ContactInfo>> lists =
            new Dictionary<MemberRole, Dictionary<string, ContactInfo>>(0);

        DateTime lastChange;

        public Dictionary<string, ContactInfo> ContactList
        {
            get { return contactList; }
        }

        /// <summary>
        /// The LastChange property of Messenger service, NOT addressbook.
        /// </summary>
        public DateTime LastChange
        {
            get { return lastChange; }
        }

        public int MessengerServiceId
        {
            get { return serviceId; }
        }

        /// <summary>
        /// Just used to get the membershipId of contacts in different lists.
        /// DONOT get any other priperties except membershipId by searching this dictionary.
        /// </summary>
        public Dictionary<MemberRole, Dictionary<string, ContactInfo>> MemberRoles
        {
            get { return lists; }
            set { lists = value; }
        }

        public MembershipParser(XmlNode mdoc) :
            base(mdoc, msStateTable, typeof(msFSMInput))
        {
        }

        private bool SetServiceInfo()
        {
            if (lastService.Type == "Messenger" && lastService.LastChange != DateTime.MinValue)
            {
                lastChange = lastService.LastChange;
                serviceId = lastService.Id;
                return true;
            }
            return false;
        }

        private void AppendInfo()
        {

            if (currentList != MSNLists.ForwardList)
            {
                ContactInfo tmpinfo;
                if (lastContact.account != null)
                {
                    if (currentRole.ContainsKey(lastContact.account))
                    {
                        tmpinfo = currentRole[lastContact.account];
                        if (tmpinfo.lastchanged.CompareTo(lastContact.lastchanged) < 0)
                            tmpinfo = lastContact;
                    }
                    else
                        currentRole.Add(lastContact.account, lastContact);
                }

                if (lastContact.account != null)
                {
                    if (contactList.ContainsKey(lastContact.account))
                    {
                        tmpinfo = contactList[lastContact.account];
                        if (tmpinfo.lastchanged.CompareTo(lastContact.lastchanged) < 0)
                        {
                            if (lastContact.displayname != null)
                                tmpinfo.displayname = lastContact.displayname;

                            tmpinfo.lastchanged = lastContact.lastchanged;
                            tmpinfo = lastContact;
                        }
                    }
                    else
                    {
                        if (lastContact.displayname == null)
                            lastContact.displayname = lastContact.account;
                        contactList.Add(lastContact.account, lastContact);
                    }
                }
                lastContact = new ContactInfo();
            }
        }


        protected override void ProcessHandler(int state, string input, int inputType, int depth)
        {
            if (isleafNode)
                switch ((msFSMState)state)
                {
                    case msFSMState.GetMemberInfo:
                        switch ((msFSMInput)inputType)
                        {
                            case msFSMInput.Email:
                                lastContact.account = input;
                                lastContact.type = ClientType.YahooMessengerUser;
                                break;
                            case msFSMInput.PassportName:
                                lastContact.account = input;
                                lastContact.type = ClientType.MessengerUser;
                                break;
                            case msFSMInput.DisplayName:
                                lastContact.displayname = input;
                                break;
                            case msFSMInput.LastChanged:
                                lastContact.lastchanged = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
                                break;
                            case msFSMInput.MembershipId :
                                int.TryParse(input, out lastContact.membershipId);
                                break;
                        }
                        break;
                    case msFSMState.GetListType:
                        if (Enum.IsDefined(typeof(MemberRole), input))
                        {
                            MemberRole role = (MemberRole)Enum.Parse(typeof(MemberRole), input);
                            if (!lists.ContainsKey(role))
                            {
                                currentRole = new Dictionary<string, ContactInfo>(0);
                                lists.Add(role, currentRole);
                            }
                            else
                            {
                                currentRole = lists[role];
                            }
                        }
                        break;
                    case msFSMState.ServiceInfo:
                        //We only care for the messenger service
                        if (SetServiceInfo())
                            break;
                        switch ((msFSMInput)inputType)
                        {
                            case msFSMInput.Id:
                                int.TryParse(input, out lastService.Id);
                                break;
                            case msFSMInput.Type:
                                lastService.Type = input;
                                break;
                            case msFSMInput.LastChange:
                                lastService.LastChange = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
                                break;
                        }
                        
                        break;
                }
            else
                switch ((msFSMState)(state))
                {
                    case msFSMState.Accept:
                        SetServiceInfo();
                        goto case msFSMState.GetMemberInfo;
                    case msFSMState.GetListType:
                    case msFSMState.GetMemberInfo:
                        if ((msFSMInput)(inputType) == msFSMInput.Member ||
                            (msFSMInput)inputType == msFSMInput.MemberRole ||
                            (msFSMInput)inputType == msFSMInput.EOF)
                            AppendInfo();
                        break;

                }
        }
    }

    /// <summary>
    /// Parse the Forward List document and get all contacts on your Forward List.
    /// </summary>
    internal class AddressBookParser : XMLNodeEnumerator
    {
        /// <summary>
        /// Addressbook state machine table
        /// </summary>
        private static int[,] abStateTable = new int[6, 15]
        {
            {0,1,0,0,0,0,0,0,0,0,0,0,0,0,-1},
            {0,0,2,0,3,0,0,0,0,0,0,0,4,0,-1},
            {0,0,2,2,3,0,0,0,0,0,0,0,4,0,-1},
            {0,0,0,0,3,3,3,3,3,3,3,3,4,0,-1},
            {0,0,0,0,0,0,0,0,0,4,0,0,0,4,-1},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}
        };

        GroupInfo currentGroup;
        ContactInfo lastContact = new ContactInfo();
        Dictionary<string, ContactInfo> tmplst = new Dictionary<string, ContactInfo>(0);
        Dictionary<string, GroupInfo> tmpgrouplst = new Dictionary<string, GroupInfo>(0);
        DateTime lastChange;
        DateTime dynamicItemLastChange;

        string cachekey = "";

        public AddressBookParser(XmlNode abdoc)
            : base(abdoc ,abStateTable ,typeof(abFSMInput))
        {
        }

        public string Cachkey
        {
            get { return cachekey; }
        }

        public Dictionary<string, ContactInfo> ContactList
        {
            get { return tmplst; }
        }

        public Dictionary<string, GroupInfo> GroupList
        {
            get { return tmpgrouplst; }
        }

        public DateTime LastChange
        {
            get { return lastChange; }
        }

        public DateTime DynamicItemLastChange
        {
            get { return dynamicItemLastChange; }
        }

        protected override void ProcessHandler(int state, string input, int inputType, int depth)
        {
            if (isleafNode)
                switch ((abFSMState)(state))
                {
                    case abFSMState.GetCacheKey:

                        cachekey = input;
                        break;
                    case abFSMState.GetGroupInfo:
                        switch ((abFSMInput)inputType)
                        {
                            case abFSMInput.groupId:
                                currentGroup.guid = input;
                                break;
                            case abFSMInput.name:
                                currentGroup.name = input;
                                tmpgrouplst.Add(currentGroup.guid, currentGroup); //Assume that group id always comes first.
                                break;
                        }
                        break;
                    case abFSMState.GetContactInfo:
                        switch ((abFSMInput)inputType)
                        {
                            case abFSMInput.contactId:
                                lastContact.guid = input;
                                break;
                            case abFSMInput.displayName:
                                lastContact.displayname = input;
                                break;
                            case abFSMInput.email:
                            case abFSMInput.passportName:
                                lastContact.account = input;
                                break;
                            case abFSMInput.isMessengerEnabled:
                            case abFSMInput.isMessengerUser:
                                bool result;
                                bool.TryParse(input, out result);
                                lastContact.isMessengerUser |= result;
                                break;
                            case abFSMInput.guid:
                                lastContact.groups.Add(input);
                                break;
                            case abFSMInput.lastChange:
                                lastContact.lastchanged = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
                                break;
                        }
                        break;
                    case abFSMState.AddressBookInfo:
                        switch ((abFSMInput)inputType)
                        {
                            case abFSMInput.lastChange:
                                lastChange = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
                                break;
                            case abFSMInput.DynamicItemLastChanged:
                                dynamicItemLastChange = XmlConvert.ToDateTime(input, XmlDateTimeSerializationMode.RoundtripKind);
                                break;
                        }
                        break;
                }
            else
                switch ((abFSMState)(state))
                {
                    case abFSMState.Accept:
                    case abFSMState.GetContactInfo:
                        if (lastContact.account != null && 
                            ((abFSMInput)inputType == abFSMInput.contactId ||
                            (abFSMInput)inputType == abFSMInput.EOF))
                        {
                            if (!tmplst.ContainsKey(lastContact.account))
                            {
                                tmplst.Add(lastContact.account, lastContact);
                            }
                            lastContact = new ContactInfo();
                        }
                        break;
                }
        }
    }
}
