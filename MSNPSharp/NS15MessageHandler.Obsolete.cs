namespace MSNPSharp
{
    using System;
    using MSNPSharp.Core;

    partial class NSMessageHandler
    {
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

        /// <summary>
        /// Tracks the last contact object which has been synchronized. Used for BPR commands.
        /// </summary>
        [Obsolete]
        private Contact lastContactSynced = null;

        /// <summary>
        /// Tracks the number of LST commands to expect
        /// </summary>
        [Obsolete]
        private int syncContactsCount = 0;

        /// <summary>
        /// Keeps track of the last received synchronization identifier
        /// </summary>
        [Obsolete]
        private int lastSync = -1;

        /// <summary>
        /// Called when a LST command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send either a forward, reverse, blocked or access list.
        /// <code>LST [Contact Guid] [List Bit] [Display Name] [Group IDs]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnLSTReceived(NSMessage message)
        {
            // decrease the number of LST commands following the SYN command we can expect
            syncContactsCount--;

            int indexer = 0;

            //contact email					
            string _contact = message.CommandValues[indexer].ToString();
            Contact contact = ContactList.GetContact(_contact.Remove(0, 2));
            contact.NSMessageHandler = this;
            indexer++;

            // store this contact for the upcoming BPR commands			
            lastContactSynced = contact;

            //contact name
            if (message.CommandValues.Count > 3)
            {
                string name = message.CommandValues[indexer].ToString();
                contact.SetName(name.Remove(0, 2));

                indexer++;
            }

            if (message.CommandValues.Count > indexer && message.CommandValues[indexer].ToString().Length > 2)
            {
                string guid = message.CommandValues[indexer].ToString().Remove(0, 2);
                contact.SetGuid(new Guid(guid));

                indexer++;
            }

            //contact list				
            if (message.CommandValues.Count > indexer)
            {
                try
                {
                    int lstnum = int.Parse(message.CommandValues[indexer].ToString());

                    contact.SetLists((MSNLists)lstnum);
                    indexer++;

                }
                catch (System.FormatException)
                {
                }
            }

            if (message.CommandValues.Count > indexer)
            {
                string[] groupids = message.CommandValues[indexer].ToString().Split(new char[] { ',' });

                foreach (string groupid in groupids)
                    if (groupid.Length > 0 && contactGroups[groupid] != null)
                        contact.ContactGroups.Add(contactGroups[groupid]); //we add this way so the event doesn't get fired
            }

            // if all LST commands are send in this synchronization cyclus we call the callback
            if (syncContactsCount <= 0)
            {
                // set this so the user can set initial presence
                abSynchronized = true;

                if (AutoSynchronize == true)
                    OnSignedIn();

                // call the event
                if (SynchronizationCompleted != null)
                {
                    SynchronizationCompleted(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Called when a SYN command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has send an answer to a request for a list synchronization from the client. It stores the 
        /// parameters of the command internally for future use with other commands.
        /// Raises the SynchronizationCompleted event when there are no contacts on any list.
        /// <code>SYN [Transaction] [Cache] [Cache] [Contact count] [Group count]</code>
        /// </remarks>
        /// <param name="message"></param>
        [Obsolete]
        protected virtual void OnSYNReceived(NSMessage message)
        {
            int syncNr = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);

            syncContactsCount = int.Parse((string)message.CommandValues[3], System.Globalization.CultureInfo.InvariantCulture);

            // check whether there is a new list version or no contacts are on the list
            if (lastSync == syncNr || syncContactsCount == 0)
            {
                syncContactsCount = 0;
                // set this so the user can set initial presence
                abSynchronized = true;

                if (AutoSynchronize == true)
                    OnSignedIn();

                // no contacts are sent so we are done synchronizing
                // call the callback
                // MSNP8: New callback defined, SynchronizationCompleted.
                if (SynchronizationCompleted != null)
                {
                    SynchronizationCompleted(this, new EventArgs());
                    abSynchronized = true;
                }
            }
            else
            {
                lastSync = syncNr;
                lastContactSynced = null;
            }
        }
    }
};
