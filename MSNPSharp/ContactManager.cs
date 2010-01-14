using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    public class ContactManager
    {
        #region Fields
        private NSMessageHandler nsMessageHandler = null;

        private Dictionary<string, Contact> defaultContactPage = new Dictionary<string, Contact>();
        private Dictionary<string, Contact> otherContactPage = new Dictionary<string, Contact>();

        #endregion

        #region Properties

        public NSMessageHandler NSMessageHandler
        {
            get { return nsMessageHandler; }
        }

        #endregion

        #region Constructors

        public ContactManager(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
        }

        #endregion

        #region Private functions

        private string AddToDefaultContactPage(Contact contact)
        {
            string key = GetContactKey(contact);
            if (contact.AddressBookId == new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                
                lock (defaultContactPage)
                    defaultContactPage[key] = contact;
            }

            return key;
        }

        private void DisplayImageChanged(object sender, EventArgs e)
        {
            Contact contact = sender as Contact;
            if (contact == null)
                return;

            SyncDisplayImage(contact);
        }

        private string AddToOtherContactPage(Contact contact)
        {
            string key = GetContactKey(contact);
            if (contact.AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                
                lock (otherContactPage)
                {
                    if (!otherContactPage.ContainsKey(key))
                    {
                        otherContactPage[key] = contact;
                    }
                    else
                    {
                        otherContactPage[key].AddSibling(contact);
                    }
                }

                if (NeedSync(key))
                {
                    lock (defaultContactPage)
                    {
                        if (contact != defaultContactPage[key])
                            defaultContactPage[key].AddSibling(contact);
                    }
                }
            }

            return key;
        }

        private bool NeedSync(Contact contact)
        {
            return NeedSync(GetContactKey(contact));
        }

        private bool NeedSync(string key)
        {
            lock (defaultContactPage)
                return defaultContactPage.ContainsKey(key);
        }

        private bool Sync(string key)
        {
            lock (defaultContactPage)
            {
                if (!NeedSync(key))
                    return false;

                Contact root = defaultContactPage[key];

                if (root.Siblings.Count > 0)
                {
                    SyncProperties(root);
                }

            }

            return true;
        }

        #endregion

        #region Public functions

        public void SyncProperties(Contact root)
        {
            lock (root.SyncObject)
            {
                foreach (Contact sibling in root.Siblings.Values)
                {
                    if (sibling == root)
                        continue;

                    sibling.SetList(root.Lists);
                    sibling.SetIsMessengerUser(root.IsMessengerUser);

                }
            }
        }

        public void SyncDisplayImage(Contact initator)
        {
            if (initator.AddressBookId == new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {

                if (initator.Siblings.Count > 0)
                {
                    lock (initator.SyncObject)
                    {
                        foreach (Contact sibling in initator.Siblings.Values)
                        {
                            if (sibling != initator)
                                sibling.SetDisplayImage(initator.DisplayImage);
                        }
                    }
                }
            }
            else
            {
                string key = GetContactKey(initator);

                Contact root = null;
                lock (otherContactPage)
                {
                    if (!otherContactPage.ContainsKey(key))
                        return;

                    root = otherContactPage[key];
                }

                if (root.Siblings.Count > 0)
                {
                    lock (root.SyncObject)
                    {
                        foreach (Contact sibling in root.Siblings.Values)
                        {
                            if (sibling != root)
                                sibling.SetDisplayImage(root.DisplayImage);
                        }
                    }
                }
            }
        }

        public void Add(Contact contact)
        {
            string key = AddToDefaultContactPage(contact);
            AddToOtherContactPage(contact);
            contact.DisplayImageChanged += new EventHandler<EventArgs>(DisplayImageChanged);
            if (!NeedSync(key))
            {
                return;
            }

            Sync(key);

        }

        public string GetContactKey(Contact contact)
        {
            return contact.ClientType.ToString().ToLowerInvariant() + ":" + contact.Mail.ToLowerInvariant();
        }

        public void Reset()
        {
            lock (defaultContactPage)
                defaultContactPage.Clear();
            lock (otherContactPage)
                otherContactPage.Clear();
        }
        #endregion
    }
}
