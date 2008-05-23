namespace MSNPSharp.IO
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using MemberRole = MSNPSharp.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNABSharingService.ServiceFilterType;

    #region ContactInfo

    [Serializable]
    public class ContactInfo
    {
        protected ContactInfo()
        {
        }

        private string account;
        public string Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        private ClientType type = ClientType.PassportMember;
        public ClientType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        private string displayname;
        public string DisplayName
        {
            get
            {
                return displayname;
            }
            set
            {
                displayname = value;
            }
        }

        private DateTime lastchanged;
        public DateTime LastChanged
        {
            get
            {
                return lastchanged;
            }
            set
            {
                lastchanged = value;
            }
        }

        private ClientCapacities capability = 0;
        public ClientCapacities Capability
        {
            get
            {
                return capability;
            }
            set
            {
                capability = value;
            }
        }

        private object tags;

        /// <summary>
        /// Save whatever you want
        /// </summary>
        public object Tags
        {
            get
            {
                return tags;
            }
            set
            {
                tags = value;
            }
        }

        /// <summary>
        /// The string for this instance
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string debugstr = String.Empty;

            if (account != null)
                debugstr += account + " | " + Type.ToString();

            if (displayname != null)
                debugstr += " | " + displayname;

            return debugstr;
        }
    }

    #endregion


    [Serializable]
    public class MembershipContactInfo : ContactInfo
    {
        protected MembershipContactInfo()
        {
        }

        public MembershipContactInfo(string account, ClientType type)
        {
            Account = account;
            Type = type;
        }

        private SerializableDictionary<MemberRole, int> memberships = new SerializableDictionary<MemberRole, int>();
        public SerializableDictionary<MemberRole, int> Memberships
        {
            get
            {
                return memberships;
            }
            set
            {
                memberships = value;
            }
        }
    }

    [Serializable]
    public class AddressbookContactInfo : ContactInfo
    {
        public AddressbookContactInfo()
        {
        }

        public AddressbookContactInfo(string account, ClientType type, Guid guid)
        {
            Account = account;
            Type = type;
            Guid = guid;
        }

        private Guid guid;
        public Guid Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        private bool isMessengerUser;
        public bool IsMessengerUser
        {
            get
            {
                return isMessengerUser;
            }
            set
            {
                isMessengerUser = value;
            }
        }

        private List<string> groups = new List<string>(0);
        public List<string> Groups
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
    }


    #region GroupInfo
    [Serializable]
    public class GroupInfo
    {
        private string guid;
        public string Guid
        {
            get
            {
                return guid;
            }
            set
            {
                guid = value;
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    #endregion

    #region Service
    [Serializable]
    public class Service
    {
        private int id;
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        private ServiceFilterType type;
        public ServiceFilterType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        private DateTime lastChange;
        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
            set
            {
                lastChange = value;
            }
        }

        private string foreignId;
        public string ForeignId
        {
            get
            {
                return foreignId;
            }
            set
            {
                foreignId = value;
            }
        }

        public override string ToString()
        {
            return Convert.ToString(Type);
        }
    }

    #endregion
}
