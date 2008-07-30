using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MemberRole = MSNPSharp.MSNWS.MSNABSharingService.MemberRole;
    using ServiceFilterType = MSNPSharp.MSNWS.MSNABSharingService.ServiceFilterType;

    #region Contact Types

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

        /// <summary>
        /// Overrided. Treat contacts with same account but different clienttype as different contacts.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            ContactInfo cinfo = obj as ContactInfo;
            return ((Account.ToLowerInvariant() == cinfo.Account.ToLowerInvariant()) && (Type == cinfo.Type));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion

    /// <summary>
    /// Contact type for membership list
    /// </summary>
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

    #endregion

    #region Service
    /// <summary>
    /// Membership service
    /// </summary>
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

    #region Owner properties

    /// <summary>
    /// Base class for profile resource
    /// </summary>
    [Serializable]
    public class ProfileResource
    {
        private DateTime dateModified;
        private string resourceID = null;

        /// <summary>
        /// Last modify time of the resource
        /// </summary>
        public DateTime DateModified
        {
            get
            {
                return dateModified;
            }
            set
            {
                dateModified = value;
            }
        }

        /// <summary>
        /// Identifier of the resource
        /// </summary>
        public string ResourceID
        {
            get
            {
                return resourceID;
            }
            set
            {
                resourceID = value;
            }
        }
    }

    /// <summary>
    /// Owner's photo resource in profile
    /// </summary>
    [Serializable]
    public class ProfilePhoto : ProfileResource
    {
        private string preAthURL = null;
        private SerializableMemoryStream displayImage = null;

        public string PreAthURL
        {
            get
            {
                return preAthURL;
            }
            set
            {
                preAthURL = value;
            }
        }

        public SerializableMemoryStream DisplayImage
        {
            get
            {
                return displayImage;
            }
            set
            {
                displayImage = value;
            }
        }
    }

    /// <summary>
    /// Owner profile
    /// </summary>
    [Serializable]
    public class OwnerProfile : ProfileResource
    {
        private string cID = string.Empty;
        private string displayName = string.Empty;
        private string personalMessage = string.Empty;
        private ProfilePhoto photo = new ProfilePhoto();
        
        public bool GetFromStorageService = true;

        /// <summary>
        /// DisplayImage of owner.
        /// </summary>
        public ProfilePhoto Photo
        {
            get
            {
                return photo;
            }
            set
            {
                photo = value;
            }
        }

        /// <summary>
        /// CID of owner.
        /// </summary>
        public string CID
        {
            get
            {
                return cID;
            }
            set
            {
                cID = value;
            }
        }

        /// <summary>
        /// DisplayName of owner
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        /// <summary>
        /// Personal description of owner.
        /// </summary>
        public string PersonalMessage
        {
            get
            {
                return personalMessage;
            }
            set
            {
                personalMessage = value;
            }
        }


    }
    #endregion
}
