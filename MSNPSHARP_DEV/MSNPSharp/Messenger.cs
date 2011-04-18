#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.P2P;
    using MSNPSharp.Apps;
    using MSNPSharp.Core;

    /// <summary>
    /// Provides an easy interface for the client programmer.
    /// </summary>
    /// <remarks>
    /// Messenger is an important class for the client programmer. It provides an easy interface to
    /// communicate with the network. Messenger is a facade which hides all lower abstractions like message
    /// processors, protocol handlers, etc. Messenger passes through events from underlying objects. This way
    /// the client programmer can connect eventhandlers just once.
    /// </remarks>
    public class Messenger
    {
        #region Members

        private NSMessageProcessor nsMessageProcessor = null;
        private NSMessageHandler nsMessageHandler = null;
        private ConnectivitySettings connectivitySettings = null;
        private Credentials credentials = new Credentials();
        

        #endregion

        #region .ctor

        /// <summary>
        /// Basic constructor to instantiate a Messenger object.
        /// </summary>
        public Messenger()
        {
            connectivitySettings = new ConnectivitySettings();
            nsMessageProcessor = new NSMessageProcessor(connectivitySettings);
            nsMessageHandler = new NSMessageHandler();
            
        }

        #endregion

        #region Properties

        /// <summary>
        /// The message manager providing a simple way to send and receive messages.
        /// </summary>
        public MessageManager MessageManager
        {
            get
            {
                return nsMessageHandler.MessageManager;
            }
        }

        /// <summary>
        /// The message processor that is used to send and receive nameserver messages.
        /// </summary>
        /// <remarks>
        /// This processor is mainly used by the nameserver handler.
        /// </remarks>
        public NSMessageProcessor NameserverProcessor
        {
            get
            {
                return nsMessageProcessor;
            }
        }

        /// <summary>
        /// Specifies the connection capabilities of the local machine.
        /// </summary>
        /// <remarks>
        /// Use this property to set specific connectivity settings like proxy servers and custom messenger servers.
        /// </remarks>
        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return connectivitySettings;
            }
            set
            {
                connectivitySettings = value;
                NameserverProcessor.ConnectivitySettings = ConnectivitySettings;
            }
        }

        /// <summary>
        /// The credentials which identify the messenger account and the client authentication.
        /// </summary>
        /// <remarks>
        /// This property must be set before logging in the messenger service. <b>Both</b> the account 
        /// properties and the client identifier codes must be set. The first, the account, specifies the
        /// account which represents the local user, for example 'account@hotmail.com'. The second, the
        /// client codes, specifies how this client will authenticate itself against the messenger server.
        /// See <see cref="Credentials"/> for more information about this.
        /// </remarks>
        public Credentials Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                credentials = value;
            }
        }

        /// <summary>
        /// The message handler that is used to handle incoming nameserver messages.
        /// </summary>
        public NSMessageHandler Nameserver
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// Returns whether there is a connection with the messenger server.
        /// </summary>
        public bool Connected
        {
            get
            {
                return nsMessageProcessor.Connected;
            }
        }

        /// <summary>
        /// A list of all contacts.
        /// </summary>
        public ContactList ContactList
        {
            get
            {
                return Nameserver.ContactList;
            }
        }

        /// <summary>
        /// A list of all contactgroups.
        /// </summary>
        public ContactGroupList ContactGroups
        {
            get
            {
                return Nameserver.ContactGroups;
            }
        }

        /// <summary>
        /// A collection of all circles which are defined by the user who logged into the messenger network.
        /// </summary>
        public Dictionary<string, Contact> CircleList
        {
            get
            {
                return Nameserver.CircleList;
            }
        }

        /// <summary>
        /// Offline message service.
        /// </summary>
        public OIMService OIMService
        {
            get
            {
                return Nameserver.OIMService;
            }
        }

        /// <summary>
        /// Storage service to get/update display name, personal status, display picture etc.
        /// </summary>
        public MSNStorageService StorageService
        {
            get
            {
                return Nameserver.StorageService;
            }
        }

        /// <summary>
        /// The ticket string to fetch msn objects from storage service.
        /// </summary>
        public string StorageTicket
        {
            get
            {
                string ticket = string.Empty;

                if (Nameserver.MSNTicket != MSNTicket.Empty && Nameserver.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Storage))
                {
                    ticket = Nameserver.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket.Substring(2);
                }

                return ticket;
            }
        }

        /// <summary>
        /// What's Up service
        /// </summary>
        public WhatsUpService WhatsUpService
        {
            get
            {
                return Nameserver.WhatsUpService;
            }
        }

        /// <summary>
        /// Directory Service
        /// </summary>
        public MSNDirectoryService DirectoryService
        {
            get
            {
                return Nameserver.DirectoryService;
            }
        }

        /// <summary>
        /// Contact service.
        /// </summary>
        public ContactService ContactService
        {
            get
            {
                return Nameserver.ContactService;
            }
        }

        /// <summary>
        /// The local user logged into the network. It will remain null until user successfully login.
        /// You can register owner events after <see cref="NSMessageHandler.OwnerVerified"/> event.
        /// </summary>
        public Owner Owner
        {
            get
            {
                return Nameserver.Owner;
            }
        }

        /// <summary>
        /// The handler that manages all incoming/outgoing P2P framework messages.
        /// </summary>
        public P2PHandler P2PHandler
        {
            get
            {
                return Nameserver.P2PHandler;
            }
        }


        #endregion

        #region Methods

        /// <summary>
        /// Connects to the messenger network.
        /// </summary>
        public virtual void Connect()
        {
            if (NameserverProcessor == null)
                throw new MSNPSharpException("No message processor defined");

            if (Nameserver == null)
                throw new MSNPSharpException("No message handler defined");

            if (Credentials == null)
                throw new MSNPSharpException("No credentials defined");

            if (Credentials.Account.Length == 0)
                throw new MSNPSharpException("The specified account is empty");

            if (Credentials.Password.Length == 0)
                throw new MSNPSharpException("The specified password is empty");

            if (Credentials.ClientCode.Length == 0 || credentials.ClientID.Length == 0)
                throw new MSNPSharpException("The local messengerclient credentials (client-id and client code) are not specified. This is necessary in order to authenticate the local client with the messenger server. See for more info about the values to use the documentation of the Credentials class.");

            // everything is okay, resume
            NameserverProcessor.ConnectivitySettings = connectivitySettings;
            NameserverProcessor.RegisterHandler(nsMessageHandler);
            Nameserver.MessageProcessor = nsMessageProcessor;
            Nameserver.Credentials = credentials;

            NameserverProcessor.Connect();
        }

        /// <summary>
        /// Disconnects from the messenger network.
        /// </summary>
        public virtual void Disconnect()
        {
            if (nsMessageProcessor.Connected)
            {
                if (nsMessageHandler != null && Nameserver.Owner != null)
                {
                    Nameserver.Owner.SetStatus(PresenceStatus.Offline);
                }

                nsMessageProcessor.Disconnect();
            }
        }

        /// <summary>
        /// Requests <paramref name="msnObject"/> from <paramref name="remoteContact"/>.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <param name="msnObject"></param>
        /// <returns></returns>
        public ObjectTransfer RequestMsnObject(Contact remoteContact, MSNObject msnObject)
        {
            return P2PHandler.RequestMsnObject(remoteContact, msnObject);
        }

        /// <summary>
        /// Sends a file to the <paramref name="remoteContact"/>.
        /// </summary>
        /// <param name="remoteContact">Remote contact to be sent a file</param>
        /// <param name="filename">File name</param>
        /// <param name="fileStream">File stream</param>
        /// <returns></returns>
        public FileTransfer SendFile(Contact remoteContact, string filename, FileStream fileStream)
        {
            return P2PHandler.SendFile(remoteContact, filename, fileStream);
        }

        /// <summary>
        /// Starts a new activity with <paramref name="remoteContact"/>.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <param name="activityID">Activity ID</param>
        /// <param name="activityName">Activity name</param>
        /// <param name="activityData">Activity data</param>
        /// <returns></returns>
        public P2PActivity StartActivity(Contact remoteContact, uint activityID, string activityName, string activityData)
        {
            P2PActivity p2pActivity = new P2PActivity(remoteContact, activityID, activityName, activityData);

            P2PHandler.AddTransfer(p2pActivity);

            return p2pActivity;
        }

        public void SendTextMessage(Contact contact, string msg)
        {
            contact.SendMessage(new TextMessage(msg));
        }

        public void SendMobileMessage(Contact contact, string msg)
        {
            contact.SendMobileMessage(msg);
        }

        public void SendNudge(Contact contact)
        {
            contact.SendNudge();
        }

        public void SendTypingMessage(Contact contact)
        {
            contact.SendTypingMessage();
        }

        public void SendEmoticonDefinitions(Contact contact, List<Emoticon> emoticons, EmoticonType icontype)
        {
            contact.SendEmoticonDefinitions(emoticons, icontype);
        }

        #endregion
    }
};
