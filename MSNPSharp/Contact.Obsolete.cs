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
using System.Net;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.P2P;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    partial class Contact
    {

        [Obsolete("Please use EndPointData instead.", true)]
        public Guid MachineGuid
        {
            get
            {
                return SelectRandomEPID();
            }
        }

        [Obsolete("Block role is no more supported. Please use AppearOffline instead.")]
        public bool Blocked
        {
            get
            {
                return AppearOffline;
            }
            set
            {
                AppearOffline = value;
            }
        }

        [Obsolete("Block role is no more supported. Please use AppearOffline instead.")]
        public bool OnBlockedList
        {
            get
            {
                return AppearOffline;
            }
            set
            {
                AppearOffline = value;
            }
        }

        [Obsolete("Reverse role is no more supported.")]
        public bool OnReverseList
        {
            get
            {
                return ((lists & RoleLists.Reverse) == RoleLists.Reverse);
            }
        }
    }
};
