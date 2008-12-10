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
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// Storage class for deltas request
    /// </summary>
    [Serializable]
    public class DeltasList : MCLSerializer
    {
        private List<ABFindAllResultType> addressBookDeltas = new List<ABFindAllResultType>(0);
        private List<FindMembershipResultType> membershipDeltas = new List<FindMembershipResultType>(0);
        private SerializableDictionary<string, BaseDynamicItemType> dynamicItems = new SerializableDictionary<string, BaseDynamicItemType>(0);

        /// <summary>
        /// The users that have changed their spaces or profiles.
        /// </summary>
        public SerializableDictionary<string, BaseDynamicItemType> DynamicItems
        {
            get
            {
                return dynamicItems;
            }
            set
            {
                dynamicItems = value;
            }
        }


        public List<ABFindAllResultType> AddressBookDeltas
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
            Save();
        }

        /// <summary>
        /// Load the serialized object from a file.
        /// </summary>
        /// <param name="filename">Path of file where the serialized object was saved.</param>
        /// <param name="nocompress">If true, use gzip to decompress the file(The file must be compressed).</param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static DeltasList LoadFromFile(string filename, bool nocompress, NSMessageHandler handler)
        {
            return LoadFromFile(filename, nocompress, typeof(DeltasList), handler) as DeltasList;
        }

        public static int CompareAddressBookDeltas(ABFindAllResultType x, ABFindAllResultType y)
        {
            return x.ab.lastChange.CompareTo(y.ab.lastChange);
        }

        public static int CompareMembershipDeltas(FindMembershipResultType x, FindMembershipResultType y)
        {
            foreach (ServiceType serviceTypeX in x.Services)
            {
                if (serviceTypeX.Info.Handle.Type == ServiceFilterType.Messenger)
                {
                    foreach (ServiceType serviceTypeY in y.Services)
                    {
                        if (serviceTypeY.Info.Handle.Type == ServiceFilterType.Messenger)
                        {
                            return serviceTypeX.LastChange.CompareTo(serviceTypeY.LastChange);
                        }
                    }
                }
            }

            return 0;
        }

        #region Overrides

        /// <summary>
        /// Save the <see cref="DeltasList"/> into a specified file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            Version = Properties.Resources.DeltasListVersion;
            base.Save(filename);
        }
        #endregion
    }
};
