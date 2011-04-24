using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    public class ShellContact : Contact
    {
        private string sourceID = string.Empty;

        /// <summary>
        /// The gateway id of remote network this contact belongs to.
        /// </summary>
        public string SourceID
        {
            get { return sourceID; }
            private set { sourceID = value; }
        }
        private string objectID = string.Empty;

        /// <summary>
        /// The account of the shell contact.
        /// </summary>
        public string ObjectID
        {
            get { return objectID; }
            set { objectID = value; }
        }

        private Contact coreContact = null;

        internal Contact CoreContact
        {
            get { return coreContact; }
            private set { coreContact = value; }
        }



        public ShellContact(Contact coreContact, IMAddressInfoType type, string sourceID, string objectID)
            : base(coreContact.AddressBookId, objectID, type, 0, coreContact.NSMessageHandler)
        {
            SourceID = sourceID;
            ObjectID = objectID;
            CoreContact = coreContact;
            SetName(CoreContact.Name);
            SetNickName(CoreContact.NickName);
            SetList(CoreContact.Lists);
            SetStatus(CoreContact.Status);
            PersonalMessage = CoreContact.PersonalMessage;

            CoreContact.ScreenNameChanged += new EventHandler<EventArgs>(CoreContact_ScreenNameChanged);
            CoreContact.StatusChanged += new EventHandler<StatusChangedEventArgs>(CoreContact_StatusChanged);
            CoreContact.PersonalMessageChanged += new EventHandler<EventArgs>(CoreContact_PersonalMessageChanged);
            CoreContact.ContactBlocked += new EventHandler<EventArgs>(CoreContact_ContactBlocked);
            CoreContact.ContactUnBlocked += new EventHandler<EventArgs>(CoreContact_ContactUnBlocked);
        }

        void CoreContact_ContactUnBlocked(object sender, EventArgs e)
        {
            RemoveFromList(RoleLists.Hide);
        }

        void CoreContact_ContactBlocked(object sender, EventArgs e)
        {
            Lists |= RoleLists.Hide;
            OnContactBlocked();
        }

        void CoreContact_PersonalMessageChanged(object sender, EventArgs e)
        {
            PersonalMessage = CoreContact.PersonalMessage;
            OnPersonalMessageChanged(PersonalMessage);
        }

        void CoreContact_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            SetStatus(CoreContact.Status);
        }

        void CoreContact_ScreenNameChanged(object sender, EventArgs e)
        {
            string oldName = Name;
            SetName(CoreContact.Name);
            OnScreenNameChanged(oldName);
        }

        protected override void OnContactOffline(StatusChangedEventArgs e)
        {
            base.OnContactOffline(e);
            NSMessageHandler.OnContactOffline(new ContactStatusChangedEventArgs(this, e.OldStatus, e.NewStatus));
        }

        protected override void OnContactOnline(StatusChangedEventArgs e)
        {
            base.OnContactOnline(e);
            NSMessageHandler.OnContactOnline(new ContactStatusChangedEventArgs(this, e.OldStatus, e.NewStatus));
        }
        protected override void  OnStatusChanged(StatusChangedEventArgs e)
        {
            base.OnStatusChanged(e);
            NSMessageHandler.OnContactStatusChanged(new ContactStatusChangedEventArgs(this, e.OldStatus, e.NewStatus));
        }

    }
}
