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
using System.IO;
using System.Net;
using System.Xml;
using System.Web;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.P2P;

    partial class NSMessageHandler
    {

#pragma warning disable 67 // disable "The event XXX is never used" warning

        [Obsolete(@"Obsoleted in 4.0. Please use TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> MobileMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TypingMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CircleTypingMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use NudgeReceived instead.", true)]
        public event EventHandler<EventArgs> CircleNudgeReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CircleTextMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. Please use TypingMessageReceived, NudgeReceived or TextMessageReceived instead.", true)]
        public event EventHandler<EventArgs> CrossNetworkMessageReceived;

        [Obsolete(@"Obsoleted in 4.0. There is no more support for SB.", true)]
        public event EventHandler<EventArgs> SBCreated;

        [Obsolete(@"Obsoleted in 4.0. There is no more support for SB.", true)]
        public event EventHandler<EventArgs> ConversationCreated;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactStatusChanged event with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberStatusChanged;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOnline instead.", true)]
        public event EventHandler<EventArgs> CircleOnline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOffline instead.", true)]
        public event EventHandler<EventArgs> CircleOffline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactStatusChanged instead.", true)]
        public event EventHandler<EventArgs> CircleStatusChanged;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOnline instead with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberOnline;

        [Obsolete(@"Obsoleted in 4.0. Please use ContactOffline instead with Via property instead.", true)]
        public event EventHandler<EventArgs> CircleMemberOffline;

        [Obsolete(@"Obsoleted in 4.0. Please use JoinedGroupChat instead. Multiparty chat is for valid for circles or temporary groups. To chat 1 on 1, just call contact.SendMessage(), contact.SendNudge()...", true)]
        public event EventHandler<EventArgs> JoinedCircleConversation;

        [Obsolete(@"Obsoleted in 4.0. Please use LeftGroupChat instead. Multiparty chat is for valid for circles or temporary groups. To chat 1 on 1, just call contact.SendMessage(), contact.SendNudge()...", true)]
        public event EventHandler<EventArgs> LeftCircleConversation;

#pragma warning restore 67 // restore "The event XXX is never used" warning

        [Obsolete(@"Obsoleted in MSNP21. Replaced by ADL.", true)]
        protected virtual void OnADGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by RML.", true)]
        protected virtual void OnRMGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by SDG.", true)]
        protected virtual void OnUBMReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by NFY PUT.", true)]
        protected virtual void OnUUXReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by multiparty chat.", true)]
        protected virtual void OnRNGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by CreateContact soap call which adds all contacts in all networks.", true)]
        protected virtual void OnFQYReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by NFY PUT.", true)]
        protected virtual void OnCHGReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in 4.0. Replaced by NFY PUT/DEL.", true)]
        protected virtual void OnNLNReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in 4.0.Replaced by NFY PUT/DEL.", true)]
        protected virtual void OnFLNReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by NFY PUT.", true)]
        protected virtual void OnUBXReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by SIPv2.", true)]
        protected virtual void OnUBNReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21. Replaced by SIPv2.", true)]
        protected virtual void OnUUNReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in MSNP21", true)]
        protected virtual void OnPRPReceived(NSMessage message)
        {
        }

        [Obsolete(@"Obsoleted in 4.0", true)]
        internal void SetMobileDevice(bool enabled)
        {
        }

        [Obsolete("MSNP18 no more supported.", true)]
        protected virtual void OnILNReceived(NSMessage message)
        {
        }

        [Obsolete("MSNP18 no more supported.", true)]
        protected virtual void OnBPRReceived(NSMessage message)
        {
        }
    }
};
