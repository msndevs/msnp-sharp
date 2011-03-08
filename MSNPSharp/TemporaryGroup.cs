#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MSNPSharp.Core;
using MSNPSharp.MSNWS.MSNABSharingService;

namespace MSNPSharp
{
    [Serializable]
    public partial class TemporaryGroup : Contact
    {
        private int transactionID = 0;
        private ContactList contactList;

        public ContactList ContactList
        {
            get
            {
                return contactList;
            }
        }

        public int TransactionID
        {
            get
            {
                return transactionID;
            }
        }

        public TemporaryGroup(string groupAddress, NSMessageHandler handler, int transId)
            : base(Guid.Empty, groupAddress, IMAddressInfoType.TemporaryGroup, 0, handler)
        {
            SetName(groupAddress);
            SetNickName(Name);

            this.transactionID = transId;
            SetStatus(PresenceStatus.Online);

            contactList = new ContactList(AddressBookId, new Owner(AddressBookId, groupAddress, 0, NSMessageHandler), NSMessageHandler);
            Lists = RoleLists.Allow;
        }
    }
};
