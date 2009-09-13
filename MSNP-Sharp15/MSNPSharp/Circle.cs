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
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    /// <summary>
    /// Used as event argument when a <see cref="Circle"/> is affected.
    /// </summary>
    [Serializable()]
    public class CircleEventArgs : EventArgs
    {
        private Circle circle;

        /// <summary>
        /// The affected contact group
        /// </summary>
        public Circle Circle
        {
            get
            {
                return circle;
            }
            set
            {
                circle = value;
            }
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        public CircleEventArgs(Circle circle)
        {
            Circle = circle;
        }
    }

    [Serializable()]
    public class Circle : Contact
    {
        private Guid addressBookId = Guid.Empty;
        private string creatorEmail = string.Empty;
        private List<Contact> members = new List<Contact>(0);
        private string hostDomain = CircleString.DefaultHostDomain;
        private string displayName = string.Empty;

        public string HostDomain
        {
            get { return hostDomain; }
        }

        public List<Contact> Members
        {
            get 
            {
                lock (members)
                    return members;
            }
        }

        public Guid AddressBookId
        {
            get
            {
                return addressBookId;
            }

            internal set
            {
                addressBookId = value;
            }
        }


        public string CreatorEmail
        {
            get
            {
                return creatorEmail;
            }

            internal set
            {
                creatorEmail = value;
            }
        }

        /// <summary>
        /// Circle account, in abId@HostDomain format.
        /// </summary>
        public new string Mail
        {
            get { return AddressBookId.ToString().ToLowerInvariant() + "@" + HostDomain.ToLowerInvariant(); }
        }

        /// <summary>
        /// The display name of circle
        /// </summary>
        public new string Name
        {
            get { return displayName; }
        }

        public override string Hash
        {
            get
            {
                return Mail.ToLowerInvariant() + ":" + ClientType.ToString();
            }
        }

        protected Circle()
            : base()
        {
            Initialize();
        }

        public Circle(Guid abId, string hostDomain, string displayName, NSMessageHandler handler)
            : base()
        {
            AddressBookId = abId;
            NSMessageHandler = handler;
            this.displayName = displayName;
            this.hostDomain = hostDomain;
            SetNickName(displayName);
            Initialize();
        }

        public override int GetHashCode()
        {
            return Mail.GetHashCode();
        }

        public override string ToString()
        {
            return Hash + "Name: " + Name;
        }

        internal new void SetName(string newName)
        {
            displayName = newName;
        }

        internal void AddMember(CircleContactMember member)
        {
            lock (members)
            {
                if (members.Contains(member))
                {
                    members[members.IndexOf(member)] = member;
                }
                else
                {
                    members.Add(member);
                }
            }
        }

        internal void RemoveMember(CircleContactMember member)
        {
            lock (members)
            {
                members.Remove(member);
            }
        }

        internal bool HasMember(CircleContactMember member)
        {
            lock (members)
                return members.Contains(member);
        }

        #region Protected
        protected virtual void Initialize()
        {
            ContactType = MessengerContactType.Circle;
            ClientType = ClientType.CircleMember;
            Lists = MSNLists.AllowedList | MSNLists.ForwardList;
        }

        #endregion
    }


    [Serializable()]
    public class CircleContactMember : Contact
    {
        private string via = string.Empty;
        private string circleMail = string.Empty;
        private ClientType memberType = ClientType.PassportMember;
        private Guid addressBookId = Guid.Empty;

        public Guid AddressBookId
        {
            get 
            {
                if (addressBookId == Guid.Empty)
                {
                    string[] viaMail = Via.Split(':');
                    if (viaMail.Length > 1)
                    {
                        string guid = viaMail[1].Split('@')[0];
                        addressBookId = new Guid(guid);
                    }
                }

                return addressBookId; 
            }
        }

        public ClientType MemberType
        {
            get { return memberType; }
        }

        public string Via
        {
            get { return via; }
        }

        /// <summary>
        /// The identifier of circle.
        /// </summary>
        public string CircleMail
        {
            get { return circleMail; }
        }

        protected CircleContactMember()
            :base()
        {
            Initialize();
        }

        public CircleContactMember(string via, string mail, ClientType type)
            :base()
        {
            this.via = via;
            this.Mail = mail;
            memberType = type;

            string[] viaMail = Via.Split(':');
            if (viaMail.Length > 0)
            {
                circleMail = viaMail[1].ToLowerInvariant();
            }

            Initialize();
        }

        public override int GetHashCode()
        {
            return (Mail + Via).GetHashCode();
        }

        public override string ToString()
        {
            return ((int)MemberType).ToString() + ":" + Mail + ";" + Via;
        }

        #region Protected
        protected virtual void Initialize()
        {
            ContactType = MessengerContactType.Circle;
            ClientType = ClientType.CircleMember;
        }

        #endregion
    }
}
