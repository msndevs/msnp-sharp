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

namespace MSNPSharp
{
    using MSNPSharp.Core;

    [Obsolete(@"Obsoleted in 4.0, please use RoleLists instead.", true)]
    public enum MSNLists
    {
    }

    [Obsolete(@"Obsoleted in 4.0, please use ClientCapabilities instead.", true)]
    public enum ClientCapacities
    {
    }

    [Obsolete(@"Obsoleted in 4.0, please use ClientCapabilitiesEx instead.", true)]
    public enum ClientCapacitiesEx
    {
    }

    [Obsolete(@"Obsoleted in 4.0, please use IMAddressInfoType instead.", true)]
    public enum ClientType
    {
    }

    /// <summary>
    /// Defines the privacy mode of the owner of the contactlist
    /// <list type="bullet">
    /// <item>AllExceptBlocked - Allow all contacts to send you messages except those on your blocked list</item>
    /// <item>NoneButAllowed - Reject all messages except those from people on your allow list</item></list>
    /// </summary>
    [Obsolete(@"Obsoleted in MSNP21, please set it up from profile.", true)]
    public enum PrivacyMode
    {
        /// <summary>
        /// Unknown privacy mode.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Allow all contacts to send you messages except those on your blocked list.
        /// </summary>
        AllExceptBlocked = 1,
        /// <summary>
        /// Reject all messages except those from people on your allow list.
        /// </summary>
        NoneButAllowed = 2
    }

    /// <summary>
    /// Defines the way MSN handles with new contacts
    /// <list type="bullet">
    /// <item>PromptOnAdd - Notify the clientprogram when a contact adds you and let the program handle the response itself</item>
    /// <item>AutomaticAdd - When someone adds you MSN will automatically add them on your list</item>
    /// </list>
    /// </summary>
    [Obsolete(@"Obsoleted in MSNP21, please set it up from profile.", true)]
    public enum NotifyPrivacy
    {
        /// <summary>
        /// Unknown notify privacy.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Notify the clientprogram when a contact adds you and let the program handle the response itself.
        /// </summary>
        PromptOnAdd = 1,
        /// <summary>
        /// When someone adds you MSN will automatically add them on your list.
        /// </summary>
        AutomaticAdd = 2
    }

    /// <summary>
    /// Use the same display picture and personal message wherever I sign in.
    /// </summary>
    [Obsolete(@"Obsoleted in MSNP21, please set it up from profile.", true)]
    public enum RoamLiveProperty
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Enabled
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Disabled
        /// </summary>
        Disabled = 2
    }
};
