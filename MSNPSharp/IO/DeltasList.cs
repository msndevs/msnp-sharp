#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.Core;

    /// <summary>
    /// Storage class for deltas request
    /// </summary>
    [Serializable]
    public class DeltasList : MCLSerializer
    {

        private List<FindMembershipResultType> membershipDeltas = new List<FindMembershipResultType>(0);
        private SerializableDictionary<CacheKeyType, string> cacheKeys = new SerializableDictionary<CacheKeyType, string>(0);
        private SerializableDictionary<string, string> preferredHosts = new SerializableDictionary<string, string>(0);
        private List<ABFindContactsPagedResultType> addressBookDeltas = new List<ABFindContactsPagedResultType>(0);
        
        public List<ABFindContactsPagedResultType> AddressBookDeltas
        {
            get
            {
                addressBookDeltas.Sort(CompareAddressBookDeltas);
                return addressBookDeltas;
            }
            set
            {
                addressBookDeltas = value;
            }
        }

        public List<FindMembershipResultType> MembershipDeltas
        {
            get
            {
                membershipDeltas.Sort(CompareMembershipDeltas);
                return membershipDeltas;
            }
            set
            {
                membershipDeltas = value;
            }
        }

        /// <summary>
        /// CacheKeys for webservices.
        /// </summary>
        public SerializableDictionary<CacheKeyType, string> CacheKeys
        {
            get
            {
                return cacheKeys;
            }
            set
            {
                cacheKeys = value;
            }
        }

        /// <summary>
        /// Preferred hosts for different methods.
        /// </summary>
        public SerializableDictionary<string, string> PreferredHosts
        {
            get
            {
                return preferredHosts;
            }
            set
            {
                preferredHosts = value;
            }
        }

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

        /// <summary>
        /// Empty all of the lists
        /// </summary>
        public void Empty()
        {
            membershipDeltas.Clear();
            addressBookDeltas.Clear();
        }

        /// <summary>
        /// Truncate file. This is useful after calling of Addressbook.Save
        /// </summary>
        public void Truncate()
        {
            Empty();
            Save(true);
        }

        public static DeltasList LoadFromFile(string filename, MclSerialization st, NSMessageHandler handler, bool useCache)
        {
            return (DeltasList)LoadFromFile(filename, st, typeof(DeltasList), handler, useCache);
        }

        public static int CompareAddressBookDeltas(ABFindContactsPagedResultType x, ABFindContactsPagedResultType y)
        {
            return x.Ab.lastChange.CompareTo(y.Ab.lastChange);
        }

        public static int CompareMembershipDeltas(FindMembershipResultType x, FindMembershipResultType y)
        {
            DateTime serviceTypeXMinLastChange = DateTime.MaxValue;
            DateTime serviceTypeYMinLastChange = DateTime.MaxValue;

            foreach (ServiceType serviceTypeX in x.Services)
            {
                if (WebServiceDateTimeConverter.ConvertToDateTime(serviceTypeX.LastChange) < serviceTypeXMinLastChange)
                    serviceTypeXMinLastChange = WebServiceDateTimeConverter.ConvertToDateTime(serviceTypeX.LastChange);
            }

            foreach (ServiceType serviceTypeY in y.Services)
            {
                if (WebServiceDateTimeConverter.ConvertToDateTime(serviceTypeY.LastChange) < serviceTypeYMinLastChange)
                    serviceTypeYMinLastChange = WebServiceDateTimeConverter.ConvertToDateTime(serviceTypeY.LastChange);
            }

            return serviceTypeXMinLastChange.CompareTo(serviceTypeYMinLastChange);
        }

        #region Overrides

        /// <summary>
        /// Save the <see cref="DeltasList"/> into a specified file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            FileName = filename;
            Save(false);
        }

        public override void Save()
        {
            Save(false);
        }

        public void Save(bool saveImmediately)
        {
            Version = Properties.Resources.DeltasListVersion;

            if (saveImmediately == false &&
                File.Exists(FileName) &&
                File.GetLastWriteTime(FileName) > DateTime.Now.AddSeconds(-5))
            {
                return;
            }

            base.Save(FileName);
        }

        #endregion
    }
};
