using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.MSNABSharingService;

namespace MSNPSharp.IO
{
    /// <summary>
    /// Storage class for deltas request
    /// </summary>
    [Serializable]
    public class DeltasList:MCLSerializer
    {

        private List<ABFindAllResultType> addressBookDeltasReplies = new List<ABFindAllResultType>(0);

        public List<ABFindAllResultType> AddressBookDeltasReplies
        {
            get { return addressBookDeltasReplies; }
            set { addressBookDeltasReplies = value; }
        }

        private List<FindMembershipResultType> membershipDeltasReplies = new List<FindMembershipResultType>(0);

        public List<FindMembershipResultType> MembershipDeltasReplies
        {
            get { return membershipDeltasReplies; }
            set { membershipDeltasReplies = value; }
        }

        /// <summary>
        /// Empty all of the lists
        /// </summary>
        public void Empty()
        {
            membershipDeltasReplies.Clear();
            addressBookDeltasReplies.Clear();
        }

        public static DeltasList LoadFromFile(string filename, bool nocompress)
        {
            return LoadFromFile(filename, nocompress, typeof(DeltasList)) as DeltasList;
        }
    }
}
