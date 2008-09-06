using System;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

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

        [Obsolete("Use NSMessageHandler.ContactService.AddNewContact")]
        public virtual void AddNewContact(string account)
        {
            ContactService.AddNewContact(account);
        }

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

        [Obsolete("Use NSMessageHandler.ContactService.BlockContact")]
        public virtual void BlockContact(Contact contact)
        {
            ContactService.BlockContact(contact);
        }

        [Obsolete("Use NSMessageHandler.ContactService.UnBlockContact")]
        public virtual void UnBlockContact(Contact contact)
        {
            ContactService.UnBlockContact(contact);
        }

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
        [Obsolete]
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
        [Obsolete]
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
                         ContactService.OnReverseAdded(new ContactEventArgs(contact));
                     }
                );
            }

            // send a list mutation event
            ContactService.OnContactAdded(new ListMutateEventArgs(contact, Type));
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
        [Obsolete]
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
            if (list == MSNLists.ReverseList)
                ContactService.OnReverseRemoved(new ContactEventArgs(contact));

            if (list == MSNLists.ForwardList)
                ContactService.OnContactRemoved(new ListMutateEventArgs(contact, list));
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

        [Obsolete("Use Owner.OnProfileReceived")]
        protected virtual void OnProfileReceived(EventArgs e)
        {
            Owner.OnProfileReceived(e);
        }

        #endregion

        #region Obsolate Events
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
