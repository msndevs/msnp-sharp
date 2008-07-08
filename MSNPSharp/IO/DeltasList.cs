using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;

    internal class AddressBookDeltasComparer : IComparer<ABFindAllResultType>
    {
        private AddressBookDeltasComparer()
        {
        }

        public static IComparer<ABFindAllResultType> Default = new AddressBookDeltasComparer();
        public int Compare(ABFindAllResultType x, ABFindAllResultType y)
        {
            return x.ab.lastChange.CompareTo(y.ab.lastChange);
        }
    }

    internal class MembershipDeltasComparer : IComparer<FindMembershipResultType>
    {
        private MembershipDeltasComparer()
        {
        }

        public static IComparer<FindMembershipResultType> Default = new MembershipDeltasComparer();
        public int Compare(FindMembershipResultType x, FindMembershipResultType y)
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
    }

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
            get { return dynamicItems; }
            set { dynamicItems = value; }
        }


        public List<ABFindAllResultType> AddressBookDeltas
        {
            get
            {
                addressBookDeltas.Sort(AddressBookDeltasComparer.Default);
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
                membershipDeltas.Sort(MembershipDeltasComparer.Default);
                return membershipDeltas;
            }
            set
            {
                membershipDeltas = value;
            }
        }

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
        /// <returns></returns>
        public static DeltasList LoadFromFile(string filename, bool nocompress)
        {
            return LoadFromFile(filename, nocompress, typeof(DeltasList)) as DeltasList;
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
}
