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

namespace MSNPSharp
{
    using MSNPSharp.MSNWS.MSNABSharingService;

    public class ShellContact : Contact
    {
        private Contact coreContact = null;

        internal Contact CoreContact
        {
            get { return coreContact; }
            private set { coreContact = value; }
        }

        public override RoleLists Lists
        {
            get
            {
                return base.Lists;
            }

            protected internal set
            {
                if (hasBlockList(value))
                {
                    throw new MSNPSharpException("Cannot add a ShellContact to Hide/Block list.");
                }

                base.Lists = value;
            }
        }

        public ShellContact(Contact coreContact, IMAddressInfoType type, string objectID, NSMessageHandler handler)
            : base(coreContact.AddressBookId, objectID, type, 0, handler)
        {
            CoreContact = coreContact;
            SetName(CoreContact.Name);
            SetNickName(CoreContact.NickName);
            SetList(RoleLists.Forward);
            PersonalMessage = CoreContact.PersonalMessage;

            CoreContact.ScreenNameChanged += new EventHandler<EventArgs>(CoreContact_ScreenNameChanged);
            CoreContact.PersonalMessageChanged += new EventHandler<EventArgs>(CoreContact_PersonalMessageChanged);
        }

        public ShellContact(Guid addressBookID, string objectID, IMAddressInfoType type, NSMessageHandler handler)
            : base(addressBookID, objectID, type, 0, handler)
        {
            SetList(RoleLists.Forward);
        }

        void CoreContact_PersonalMessageChanged(object sender, EventArgs e)
        {
            PersonalMessage = CoreContact.PersonalMessage;
            OnPersonalMessageChanged(PersonalMessage);
        }

        void CoreContact_ScreenNameChanged(object sender, EventArgs e)
        {
            string oldName = Name;
            SetName(CoreContact.Name);
            OnScreenNameChanged(oldName);
        }

        internal override void AddToList(RoleLists list)
        {
            if (hasBlockList(list))
            {
                throw new MSNPSharpException("Cannot add a ShellContact to Hide/Block list.");
            }

            base.AddToList(list);
        }

        internal override void SetList(RoleLists msnLists)
        {
            if (hasBlockList(msnLists))
            {
                throw new MSNPSharpException("Cannot add a ShellContact to Hide/Block list.");
            }

            base.SetList(msnLists);
        }

        private bool hasBlockList(RoleLists list)
        {
            if ((list & RoleLists.Hide) != RoleLists.None ||
                (list & RoleLists.Block) != RoleLists.None)
            {
                return true;
            }

            return false;
        }

    

    }
}
