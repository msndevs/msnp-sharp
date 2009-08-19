#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Net;
using System.Web;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {
        /// <summary>
        /// Called when a ILN command has been received.
        /// </summary>
        /// <remarks>
        /// ILN indicates the initial status of a contact. Used for MSNP15 and MSNP16, not MSNP18.
        /// It is send after initial log on or after adding/removing contact from the contactlist.
        /// Fires the <see cref="ContactOnline"/> and/or the <see cref="ContactStatusChanged"/> events.
        /// <code>ILN 0 [status] [account] [clienttype] [name] [clientcapacities:0] [displayimage] (MSNP16)</code>
        /// <code>ILN 0 [status] [account] [clienttype] [name] [clientcapacities] [displayimage] (MSNP15)</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete("MSNP18 no more supported")]
        protected virtual void OnILNReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when a BPR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send a phone number for a contact. Usually send after a synchronization command.
        /// <code>BPR [Type] [Number]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete("Echo")]
        protected virtual void OnBPRReceived(NSMessage message)
        {
            string commandone = (string)message.CommandValues[0];

            Contact contact = null;
            int index = 2;

            if (commandone.IndexOf('@') != -1)
            {
                contact = ContactList.GetContact(commandone, ClientType.PassportMember);
                index = 2;
            }

            string number = HttpUtility.UrlDecode((string)message.CommandValues[index]);

            if (contact.Lists != MSNLists.None)
            {
                switch ((string)message.CommandValues[index - 1])
                {
                    case "PHH":
                        contact.SetHomePhone(number);
                        break;
                    case "PHW":
                        contact.SetWorkPhone(number);
                        break;
                    case "PHM":
                        contact.SetMobilePhone(number);
                        break;
                    case "MOB":
                        contact.SetMobileAccess((number == "Y"));
                        break;
                    case "MBE":
                        contact.SetMobileDevice((number == "Y"));
                        break;
                    case "HSB":
                        contact.HasBlog = (number == "1");
                        break;
                }
            }
            else
                throw new MSNPSharpException("Phone numbers are sent but lastContact == null");
        }
    }
};
