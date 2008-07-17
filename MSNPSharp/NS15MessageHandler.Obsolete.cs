using System;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {
        /// <summary>
        /// Defines if the contact list is automatically synchronized upon connection.
        /// </summary>
        /// <remarks>No more used. Using this property causes error!!!</remarks>
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

        /// <summary>
        /// Send the synchronize command to the server. This will rebuild the contactlist with the most recent data.
        /// </summary>
        /// <remarks>
        /// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
        /// You <b>must</b> call this function before setting your initial status, otherwise you won't received online notifications of other clients.
        /// Please note that you can only synchronize a single time per session! (this is limited by the the msn-servers)
        /// </remarks>
        [Obsolete("Use NSMessageHandler.ContactService.SynchronizeContactList")]
        public virtual void SynchronizeContactList()
        {
            contactService.SynchronizeContactList();
        }

        /// <summary>
        /// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
        /// </summary>
        /// <param name="account">An e-mail adress to add</param>
        [Obsolete("Use NSMessageHandler.ContactService.AddNewContact")]
        public virtual void AddNewContact(string account)
        {
            ContactService.AddNewContact(account);
        }

        /// <summary>
        /// Remove the specified contact from your forward and allow list. Note that remote contacts that are blocked remain blocked.
        /// </summary>
        /// <param name="contact">Contact to remove</param>
        [Obsolete("Use NSMessageHandler.ContactService.RemoveContact")]
        public virtual void RemoveContact(Contact contact)
        {
            ContactService.RemoveContact(contact);
        }

        [Obsolete("Use NSMessageHandler.ContactService.AddContactToGroup")]
        public virtual void AddContactToGroup(Contact contact, ContactGroup group)
        {
            ContactService.AddContactToGroup(contact, group);
        }

        [Obsolete("Use NSMessageHandler.ContactService.RemoveContactFromGroup")]
        public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
        {
            ContactService.RemoveContactFromGroup(contact, group);
        }

        /// <summary>
        /// Block this contact. This way you don't receive any messages anymore. This contact
        /// will be removed from your allow list and placed in your blocked list.
        /// </summary>
        /// <param name="contact">Contact to block</param>
        [Obsolete("Use NSMessageHandler.ContactService.BlockContact")]
        public virtual void BlockContact(Contact contact)
        {
            ContactService.BlockContact(contact);
        }

        /// <summary>
        /// Unblock this contact. After this you are able to receive messages from this contact. This contact
        /// will be removed from your blocked list and placed in your allowed list.
        /// </summary>
        /// <param name="contact">Contact to unblock</param>
        [Obsolete("Use NSMessageHandler.ContactService.UnBlockContact")]
        public virtual void UnBlockContact(Contact contact)
        {
            ContactService.UnBlockContact(contact);
        }

        /// <summary>
        /// Send an offline message to a contact.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="msg"></param>
        [Obsolete("Use NSMessageHandler.OIMService.SendOIMMessage")]
        public virtual void SendOIMMessage(string account, string msg)
        {
            oimService.SendOIMMessage(account, msg);
        }

        [Obsolete("No longer used")]
        protected virtual void OnLSTReceived(NSMessage message)
        {
        }

        [Obsolete("No longer used")]
        protected virtual void OnSYNReceived(NSMessage message)
        {
        }

        [Obsolete("Sending GTC command to NS causes disconnect", true)]
        protected virtual void OnGTCReceived(NSMessage message)
        {
        }

        #region Outdated Contact and Group Handlers

        /// <summary>
        /// Called when a LSG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send us a contactgroup definition. This adds a new <see cref="ContactGroup"/> object to the ContactGroups colletion.
        /// <code>LSG [Name] [Guid]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnLSGReceived(NSMessage message)
        {
            ContactGroups.AddGroup(new ContactGroup((string)message.CommandValues[0], (string)message.CommandValues[1], this));
        }

        /// <summary>
        /// Called when a ADC command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact has been added to a list.
        /// This function raises the ReverseAdded event when a remote contact has added the contactlist owner on it's (forward) contact list.
        /// The ContactAdded event is always raised.
        /// <code>ADD [Transaction] [Type] [Listversion] [Account] [Name] ([Group])</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnADCReceived(NSMessage message)
        {
            if (message.CommandValues.Count == 4)
            {
                Contact c = ContactList.GetContactByGuid(new Guid(message.CommandValues[2].ToString().Remove(0, 2)));
                //Add contact to group
                string guid = message.CommandValues[3].ToString();

                if (contactGroups[guid] != null)
                    c.AddContactToGroup(contactGroups[guid]);

                return;
            }

            MSNLists Type = GetMSNList((string)message.CommandValues[1]);
            Contact contact = ContactList.GetContact(message.CommandValues[2].ToString().Remove(0, 2));

            contact.NSMessageHandler = this;

            if (message.CommandValues.Count >= 4)
                contact.SetName(message.CommandValues[3].ToString().Remove(0, 2));

            if (message.CommandValues.Count >= 5)
                contact.SetGuid(new Guid(message.CommandValues[4].ToString().Remove(0, 2)));

            // add the list to this contact						
            contact.AddToList(MSNLists.PendingList);

            // check whether another user added us .The client programmer can then act on it.

            // only fire the event if >=4 because name is just sent when other user add you to her/his contact list
            // msnp11 allows you to add someone to the reverse list.
            if (Type == MSNLists.ReverseList && message.CommandValues.Count >= 4)
            {
                ContactService.msRequest(
                    "MessengerPendingList",
                     delegate
                     {
                         contact = ContactList.GetContact(contact.Mail);
                         OnReverseAdded(contact);
                     }
                );
            }

            // send a list mutation event
            if (ContactAdded != null)
                ContactAdded(this, new ListMutateEventArgs(contact, Type));
        }


        /// <summary>
        /// Called when a REM command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a contact has been removed from a list.
        /// Raises the ReverseRemoved event when another contact has removed the contact list owner from it's list.
        /// Raises always the ContactRemoved event.
        /// <code>REM [Transaction] [Type] [List version] [Account] ([Group])</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnREMReceived(NSMessage message)
        {
            MSNLists list = GetMSNList((string)message.CommandValues[1]);

            Contact contact = null;
            if (list == MSNLists.ForwardList)
                contact = ContactList.GetContactByGuid(new Guid((string)message.CommandValues[2]));
            else
                contact = ContactList.GetContact((string)message.CommandValues[2]);

            if (contact == null)
                return;

            if (message.CommandValues.Count == 4)
            {
                //Remove from group
                string group = message.CommandValues[3].ToString();

                if (contactGroups[group] != null)
                    contact.RemoveContactFromGroup(contactGroups[group]);

                return;
            }

            //remove the contact from list
            contact.RemoveFromList(list);

            // check whether another user removed us
            if (list == MSNLists.ReverseList && ReverseRemoved != null)
                ReverseRemoved(this, new ContactEventArgs(contact));

            if (list == MSNLists.ForwardList && ContactRemoved != null)
                ContactRemoved(this, new ListMutateEventArgs(contact, list));
        }

        /// <summary>
        /// Called when an PRP command has been received.
        /// </summary>
        /// <remarks>
        /// Informs about the phone numbers of the contact list owner.
        /// <code>PRP [TransactionID] [ListVersion] PhoneType Number</code>
        /// </remarks>
        /// <param name="message"></param>
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

        #endregion
    }
};
