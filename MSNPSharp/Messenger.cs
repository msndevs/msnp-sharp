#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;    

    #region ConversationCreatedEvent

    /// <summary>
    /// Used when a new switchboard session is created.
    /// </summary>
    public class ConversationCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private Conversation _conversation;

        /// <summary>
        /// The affected conversation
        /// </summary>
        public Conversation Conversation
        {
            get
            {
                return _conversation;
            }
            set
            {
                _conversation = value;
            }
        }

        /// <summary>
        /// </summary>
        private object _initiator;

        /// <summary>
        /// The object that requested the switchboard. Null if the conversation was initiated by a remote client.
        /// </summary>
        public object Initiator
        {
            get
            {
                return _initiator;
            }
            set
            {
                _initiator = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConversationCreatedEventArgs(Conversation conversation, object initiator)
        {
            _conversation = conversation;
            _initiator = initiator;
        }
    }

    #endregion

    /// <summary>
    /// Provides an easy interface for the client programmer.
    /// </summary>
    /// <remarks>
    /// Messenger is an important class for the client programmer. It provides an
    /// easy interface to communicate with the network. Messenger is a facade which hides all
    /// lower abstractions like message processors, protocol handlers, etc.
    /// Messenger passes through events from underlying objects. This way the client programmer
    /// can connect eventhandlers just once.
    /// </remarks>
    public class Messenger
    {
        #region Members

        private NSMessageProcessor nsMessageProcessor = new NSMessageProcessor();
        private NSMessageHandler nsMessageHandler = new NSMessageHandler();
        private ConnectivitySettings connectivitySettings = new ConnectivitySettings();
        private Credentials credentials = new Credentials(MsnProtocol.MSNP18);
        private ArrayList tsMsnslpHandlers = ArrayList.Synchronized(new ArrayList());

        #endregion

        #region .ctor
        /// <summary>
        /// Basic constructor to instantiate a Messenger object.
        /// </summary>
        public Messenger()
        {
            #region private events
            nsMessageProcessor.ConnectionClosed += delegate
            {
                tsMsnslpHandlers.Clear();
            };

            nsMessageHandler.SBCreated += delegate(object sender, SBCreatedEventArgs ce)
            {
                //Register the p2phandler to handle all incoming p2p message through this switchboard.
                ce.Switchboard.MessageProcessor.RegisterHandler(nsMessageHandler.P2PHandler);

                // check if the request is remote or on our initiative
                if (ce.Initiator != null && (ce.Initiator == this || ce.Initiator == nsMessageHandler.P2PHandler))
                {
                    return;
                }

                // create a conversation object to handle with the switchboard
                Conversation c = new Conversation(this, ce.Switchboard);

                // fire event to notify client programmer
                OnConversationCreated(c, ce.Initiator);

                if (ce.Switchboard is YIMMessageHandler)
                {
                    //We must ensure that ContactJoined event fired after ConversationCreated.
                    (ce.Switchboard as YIMMessageHandler).ForceJoin(ContactList.GetContact(ce.Account, ClientType.EmailMember));
                }

                return;
            };

            nsMessageHandler.P2PHandler.SessionCreated += delegate(object sender, P2PSessionAffectedEventArgs see)
            {
                MSNSLPHandler msnslpHandler = CreateMSNSLPHandler(see.Session.Version);
                msnslpHandler.MessageProcessor = see.Session;
                see.Session.RegisterHandler(msnslpHandler);

                tsMsnslpHandlers.Add(msnslpHandler);

                // set the correct switchboard to send messages to
                lock (nsMessageHandler.P2PHandler.SwitchboardSessions)
                {
                    foreach (SBMessageHandler sb in nsMessageHandler.P2PHandler.SwitchboardSessions)
                    {
                        if (sb.GetType() == typeof(SBMessageHandler))
                        {
                            if (sb.Contacts.ContainsKey(see.Session.RemoteUser)) //if (sb.Contacts.ContainsKey(see.Session.RemoteContact))
                            {
                                see.Session.MessageProcessor = sb.MessageProcessor;
                                break;
                            }
                        }
                    }
                }

                // Accepts by default owner display images and contact emoticons.
                msnslpHandler.TransferInvitationReceived += delegate(object sndr, MSNSLPInvitationEventArgs ie)
                {
                    

                    if (ie.TransferProperties.DataType == DataTransferType.DisplayImage)
                    {
                        ie.Accept = true;

                        ie.TransferSession.DataStream = Nameserver.ContactList.Owner.DisplayImage.OpenStream();
                        ie.TransferSession.AutoCloseStream = false;
                        ie.TransferSession.ClientData = Nameserver.ContactList.Owner.DisplayImage;
                    }
                    else if (ie.TransferProperties.DataType == DataTransferType.Emoticon)
                    {
                        MSNObject msnObject = new MSNObject();
                        msnObject.ParseContext(ie.TransferProperties.Context);

                        // send an emoticon
                        foreach (Emoticon emoticon in Nameserver.ContactList.Owner.Emoticons.Values)
                        {
                            if (emoticon.Sha == msnObject.Sha)
                            {
                                ie.Accept = true;
                                ie.TransferSession.AutoCloseStream = true;
                                ie.TransferSession.DataStream = emoticon.OpenStream();
                                ie.TransferSession.ClientData = emoticon;
                            }
                        }
                    }
                    else
                    {
                        // forward the invitation to the client programmer
                        if (TransferInvitationReceived != null)
                            TransferInvitationReceived(sndr, ie);
                    }
                    return;
                };
                return;
            };

            nsMessageHandler.P2PHandler.SessionClosed += delegate(object sender, P2PSessionAffectedEventArgs e)
            {
                MSNSLPHandler handler = GetMSNSLPHandler(e.Session);
                if (handler != null)
                {
                    tsMsnslpHandlers.Remove(handler);
                }
                return;
            };

            #endregion
        }

        #endregion

        #region Public

        #region Events
        /// <summary>
        /// Occurs when a new conversation is created. Either by a local or remote invitation.
        /// </summary>
        /// <remarks>
        /// You can check the initiator object in the event arguments to see which party initiated the conversation.
        /// This event is called after the messenger server has created a switchboard handler, so there is
        /// always a valid messageprocessor.
        /// </remarks>
        public event EventHandler<ConversationCreatedEventArgs> ConversationCreated;

        /// <summary>
        /// Occurs when a remote client has send an invitation for a filetransfer session.
        /// </summary>
        public event EventHandler<MSNSLPInvitationEventArgs> TransferInvitationReceived;

        #endregion

        #region Properties

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
            }
        }

        /// <summary>
        /// The credentials which identify the messenger account and the client authentication.
        /// </summary>
        /// <remarks>
        /// This property must be set before logging in the messenger service. <b>Both</b> the account properties and
        /// the client identifier codes must be set. The first, the account, specifies the account which represents the local user,
        /// for example 'account@hotmail.com'. The second, the client codes, specifies how this client will authenticate itself
        /// against the messenger server. See <see cref="Credentials"/> for more information about this.
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
        /// <remarks>
        ///	This property is a reference to the ContactList object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public ContactList ContactList
        {
            get
            {
                return nsMessageHandler.ContactList;
            }
        }

        /// <summary>
        /// A list of all contactgroups.
        /// </summary>
        /// <remarks>
        ///	This property is a reference to the ContactGroups object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public ContactGroupList ContactGroups
        {
            get
            {
                return nsMessageHandler.ContactGroups;
            }
        }

        /// <summary>
        /// Offline message service.
        /// </summary>
        /// <remarks>
        /// This property is a reference to the OIMService object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public OIMService OIMService
        {
            get
            {
                return nsMessageHandler.OIMService;
            }
        }

        /// <summary>
        /// Storage service to get/update display name, personal status, display picture etc.
        /// </summary>
        /// <remarks>
        /// This property is a reference to the StorageService object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public MSNStorageService StorageService
        {
            get
            {
                return nsMessageHandler.StorageService;
            }
        }

        public WhatsUpService WhatsUpService
        {
            get
            {
                return nsMessageHandler.WhatsUpService;
            }
        }

        /// <summary>
        /// Contact service.
        /// </summary>
        /// <remarks>
        /// This property is a reference to the ContactService object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public ContactService ContactService
        {
            get
            {
                return nsMessageHandler.ContactService;
            }
        }

        /// <summary>
        /// The local user logged into the network.
        /// </summary>
        /// <remarks>
        /// This property is a reference to the Owner object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        [Obsolete("To avoid confuse, this property was obsoleted in 3.1, please use NSMessageHandler.ContactList.Owner instead.")]
        public Owner Owner
        {
            get
            {
                return Nameserver.ContactList.Owner;
            }
        }

        /// <summary>
        /// The handler that handles all incoming P2P framework messages.
        /// </summary>
        /// <remarks>
        /// This property is a reference to the P2PHandler object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
        /// </remarks>
        public P2PHandler P2PHandler
        {
            get
            {
                return nsMessageHandler.P2PHandler;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connect to the messenger network.
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
            Nameserver.ConnectivitySettings = connectivitySettings;

            NameserverProcessor.Connect();
        }

        /// <summary>
        /// Disconnect from the messenger network.
        /// </summary>
        public virtual void Disconnect()
        {
            if (nsMessageProcessor.Connected)
            {
                if (nsMessageHandler != null)
                    Nameserver.ContactList.Owner.SetStatus(PresenceStatus.Offline);

                nsMessageProcessor.Disconnect();
            }
        }

        /// <summary>
        /// Creates a conversation.
        /// </summary>
        /// <remarks>
        /// This method will fire the <see cref="ConversationCreated"/> event. The initiator object of the created switchboard will be <b>this</b> messenger object.
        /// </remarks>
        /// <returns></returns>
        public Conversation CreateConversation()
        {

            Conversation conversation = new Conversation(this);
            
            OnConversationCreated(conversation, this);

            return conversation;
        }


        /// <summary>
        /// Returns a MSNSLPHandler, associated with a P2P session. The returned object can be used to send or receive invitations from the remote contact.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        public MSNSLPHandler GetMSNSLPHandler(Contact remoteContact)
        {
            if (!Nameserver.ContactList.HasContact(remoteContact.Mail, remoteContact.ClientType))
                throw new MSNPSharpException("Function not supported. Only MSN user can create a P2P session.");

            P2PMessageSession p2pSession = nsMessageHandler.P2PHandler.GetSession(Nameserver.ContactList.Owner, remoteContact);
            MSNSLPHandler msnslpHandler = (MSNSLPHandler)p2pSession.GetHandler(typeof(MSNSLPHandler));
            if (msnslpHandler == null)
            {
                // create a msn slp handler
                msnslpHandler = CreateMSNSLPHandler(p2pSession.Version);
                p2pSession.RegisterHandler(msnslpHandler);
                msnslpHandler.MessageProcessor = p2pSession;
            }
            return msnslpHandler;
        }

        #endregion

        #endregion

        #region Protected

        /// <summary>
        /// Fires the ConversationCreated event.
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="initiator"></param>
        protected virtual void OnConversationCreated(Conversation conversation, object initiator)
        {
            if (ConversationCreated != null)
                ConversationCreated(this, new ConversationCreatedEventArgs(conversation, initiator));
        }

        #endregion

        #region Private

        /// <summary>
        /// Creates the object and sets the external end point.
        /// </summary>
        /// <returns></returns>
        private MSNSLPHandler CreateMSNSLPHandler(P2PVersion ver)
        {
            MSNSLPHandler msnslpHandler = new MSNSLPHandler(ver);
            msnslpHandler.ExternalEndPoint = Nameserver.ExternalEndPoint;
            return msnslpHandler;
        }

        /// <summary>
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private MSNSLPHandler GetMSNSLPHandler(P2PMessageSession session)
        {
            lock (tsMsnslpHandlers.SyncRoot)
            {
                foreach (MSNSLPHandler handler in tsMsnslpHandlers)
                {
                    if (handler.MessageSession == session)
                        return handler;
                }
            }
            return null;
        }

        #endregion

    }
};
