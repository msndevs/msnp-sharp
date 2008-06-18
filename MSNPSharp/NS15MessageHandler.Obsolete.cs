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
    }
};
