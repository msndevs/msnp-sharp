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
using System.Net;
using System.Web;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {
        [Obsolete("No more used. Using this property causes error!!!", true)]
        public bool AutoSynchronize
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        [Obsolete("No more used. Using this method causes error!!!", true)]
        public virtual void SynchronizeContactList()
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.AddNewContact", true)]
        public virtual void AddNewContact(string account)
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.RemoveContact", true)]
        public virtual void RemoveContact(Contact contact)
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.AddContactToGroup", true)]
        public virtual void AddContactToGroup(Contact contact, ContactGroup group)
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.RemoveContactFromGroup", true)]
        public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.BlockContact", true)]
        public virtual void BlockContact(Contact contact)
        {
        }

        [Obsolete("Please use NSMessageHandler.ContactService.UnBlockContact", true)]
        public virtual void UnBlockContact(Contact contact)
        {
        }

        [Obsolete("Please use NSMessageHandler.OIMService.SendOIMMessage", true)]
        public virtual void SendOIMMessage(string account, string msg)
        {
        }

        [Obsolete("Deprecated as of MSNP13. Instead, the client will fetch the contact list through a SOAP request.", true)]
        protected virtual void OnLSTReceived(NSMessage message)
        {
        }

        [Obsolete("Deprecated as of MSNP13. Instead, the client will fetch the contact list through a SOAP request.", true)]
        protected virtual void OnSYNReceived(NSMessage message)
        {
        }

        [Obsolete("Deprecated as of MSNP13. The GTC command is no longer accepted by the server.", true)]
        protected virtual void OnGTCReceived(NSMessage message)
        {
        }

        #region Outdated Contact and Group Handlers

        [Obsolete("Deprecated as of MSNP13. Instead, the client will fetch the contact list through a SOAP request.", true)]
        protected virtual void OnLSGReceived(NSMessage message)
        {
        }

        [Obsolete("Deprecated as of MSNP13. ADC command has been replaced by ADL.", true)]
        protected virtual void OnADCReceived(NSMessage message)
        {
        }

        [Obsolete("Deprecated as of MSNP13. REM command has been replaced by RML.", true)]
        protected virtual void OnREMReceived(NSMessage message)
        {
        }

        /// <summary>
        /// Called when an PRP command has been received.
        /// </summary>
        /// <remarks>
        /// Informs about the phone numbers of the contact list owner.
        /// <code>PRP [TransactionID] [ListVersion] PhoneType Number</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnPRPReceived(NSMessage message)
        {
            string number = String.Empty;
            string type = String.Empty;
            if (message.CommandValues.Count >= 4)
            {
                if (message.CommandValues.Count >= 4)
                    number = HttpUtility.UrlDecode((string)message.CommandValues[3]);
                else
                    number = String.Empty;
                type = message.CommandValues[1].ToString();
            }
            else
            {
                number = HttpUtility.UrlDecode((string)message.CommandValues[2]);
                type = message.CommandValues[1].ToString();
            }

            switch (type)
            {
                case "PHH":
                    Owner.SetHomePhone(number);
                    break;
                case "PHW":
                    Owner.SetWorkPhone(number);
                    break;
                case "PHM":
                    Owner.SetMobilePhone(number);
                    break;
                case "MBE":
                    Owner.SetMobileDevice((number == "Y") ? true : false);
                    break;
                case "MOB":
                    Owner.SetMobileAccess((number == "Y") ? true : false);
                    break;
                case "MFN":
                    Owner.SetName(HttpUtility.UrlDecode((string)message.CommandValues[2]));
                    break;
            }
        }

        /// <summary>
        /// Called when a BPR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send a phone number for a contact. Usually send after a synchronization command.
        /// <code>BPR [Type] [Number]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnBPRReceived(NSMessage message)
        {
            string commandone = (string)message.CommandValues[0];

            Contact contact = null;
            int index = 2;

            if (commandone.IndexOf('@') != -1)
            {
                contact = ContactList.GetContact(commandone);
                index = 2;
            }
            //else
            //{
            //    contact = lastContactSynced;
            //    index = 1;
            //}

            string number = HttpUtility.UrlDecode((string)message.CommandValues[index]);

            if (contact != null)
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
                        contact.SetHasBlog((number == "1"));
                        break;
                }
            }
            else
                throw new MSNPSharpException("Phone numbers are sent but lastContact == null");
        }

        [Obsolete("Please use NSMessageHandler.Owner.OnProfileReceived", true)]
        protected virtual void OnProfileReceived(EventArgs e)
        {
        }

        #endregion

        #region Obsolete Events
        /// <summary>
        /// Occurs when a new contactgroup is created
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ContactGroupAdded.", true)]
        public event ContactGroupChangedEventHandler ContactGroupAdded;

        /// <summary>
        /// Occurs when a contactgroup is removed
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ContactGroupRemoved.", true)]
        public event ContactGroupChangedEventHandler ContactGroupRemoved;

        /// <summary>
        /// Occurs when a contact is added to any list (including reverse list)
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ContactAdded.", true)]
        public event ListMutatedAddedEventHandler ContactAdded;

        /// <summary>
        /// Occurs when a contact is removed from any list (including reverse list)
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ContactRemoved.", true)]
        public event ListMutatedAddedEventHandler ContactRemoved;

        /// <summary>
        /// Occurs when another user adds us to their contactlist. A ContactAdded event with the reverse list as parameter will also be raised.
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ReverseAdded.", true)]
        public event ContactChangedEventHandler ReverseAdded;

        /// <summary>
        /// Occurs when another user removes us from their contactlist. A ContactRemoved event with the reverse list as parameter will also be raised.
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.ReverseRemoved.", true)]
        public event ContactChangedEventHandler ReverseRemoved;

        /// <summary>
        /// Occurs when a call to SynchronizeList() has been made and the synchronization process is completed.
        /// This means all contact-updates are received from the server and processed.
        /// </summary>
        [Obsolete("Please use NSMessageHandler.ContactService.SynchronizationCompleted.", true)]
        public event EventHandler SynchronizationCompleted;
        #endregion
    }
};
