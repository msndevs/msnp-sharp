using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using MSNPSharp.IO;

namespace MSNPSharp
{
    internal enum XMLContactListTags
    {
        ContactList,
        MembershipList,
        AddressBook,
        Members,
        groups,
        Group,
        contacts,
        Contact,
        Settings,
        contactType,
        Name,
        Value,
        Me,
        Annotations,
        Annotation,
        Service,
        Membership
    }


    internal abstract class XMLContactList:Dictionary<string,ContactInfo>
    {
        protected XmlDocument doc;
        protected DateTime lastChange;

        protected void CreateDoc()
        {
            doc = new XmlDocument();
            XmlDeclaration xmldecl;
            xmldecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(xmldecl);
            XmlNode root = CreateNode(XMLContactListTags.ContactList.ToString(), null);
            doc.AppendChild(root);
            root.AppendChild(CreateNode(XMLContactListTags.MembershipList.ToString(), null));
            root.AppendChild(CreateNode(XMLContactListTags.AddressBook.ToString(), null));
        }

        protected XMLContactList()
            : base(0)
        {
        }

        protected virtual XmlNode CreateNode(string name, string innerText)
        {
            if (doc != null)
            {
                XmlNode rtn = doc.CreateElement(name);
                rtn.InnerText = innerText;
                return rtn;
            }
            CreateDoc();
            return CreateNode(name, innerText);
        }

        protected virtual void SaveToHiddenMCL(string filename)
        {
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);
            MCLFile file = new MCLFile(filename);
            file.Content = ms.ToArray();
            file.SaveAndHide();
        }


        public virtual void LoadFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                CreateDoc();
                MCLFile file = new MCLFile(filename);
                if (file.Content != null)
                {
                    MemoryStream ms = new MemoryStream(file.Content);
                    //string str = Encoding.UTF8.GetString(ms.ToArray());  //Just for test
                    doc.Load(ms);
                }
            }
            else
            {
                CreateDoc();
                SaveToHiddenMCL(filename);
            }
        }

        public abstract void Save(string filename);
        public abstract void Save();

        public virtual void AddRange(Dictionary<string, ContactInfo> range)
        {
            foreach (string account in range.Keys)
            {
                if (this.ContainsKey(account))
                {
                    if (this[account].LastChanged.CompareTo(range[account].LastChanged) <= 0)
                    {
                        this[account] = range[account];
                    }
                }
                else
                {
                    Add(account, range[account]);
                }
            }
        }

        public DateTime LastChange
        {
            set { lastChange = value; }
            get { return lastChange; }
        }
    }
}
