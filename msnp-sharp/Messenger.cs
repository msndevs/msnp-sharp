#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

using System;
using System.Collections;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// Used when a new conversation is created. 
	/// </summary>
	public delegate void ConversationCreatedEventHandler(object sender, ConversationCreatedEventArgs e);

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
			get { return _conversation; }
			set { _conversation = value;}
		}

		/// <summary>
		/// </summary>
		private object	_initiator;

		/// <summary>
		/// The object that requested the switchboard. Null if the conversation was initiated by a remote client.
		/// </summary>
		public object Initiator
		{
			get { return _initiator; }
			set { _initiator = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConversationCreatedEventArgs(Conversation conversation, object initiator)
		{
			_conversation = conversation;
			_initiator    = initiator;
		}
	}

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
		#region Private

		/// <summary>
		/// </summary>
		private P2PHandler				p2pHandler;

		/// <summary>
		/// </summary>
		private NSMessageProcessor		nsMessageProcessor;
	
		/// <summary>
		/// </summary>
		private NSMessageHandler		nsMessageHandler;

		/// <summary>
		/// </summary>
		private ConnectivitySettings	connectivitySettings = new ConnectivitySettings();

		/// <summary>
		/// </summary>
		private Credentials				credentials = new Credentials();		

		/// <summary>
		/// </summary>
		private ArrayList				conversations = new ArrayList();
		

		/// <summary>
		/// </summary>
		private ArrayList				msnslpHandlers = new ArrayList();

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
		public event ConversationCreatedEventHandler ConversationCreated;

		/// <summary>
		/// Occurs when a remote client has send an invitation for a filetransfer session.
		/// </summary>
		public event MSNSLPInvitationReceivedEventHandler  TransferInvitationReceived;

		#endregion

		#region Properties

		/// <summary>
		/// The handler that handles all incoming P2P framework messages.
		/// </summary>
		/// <remarks>
		/// The handler is defined at the messenger niveau which implies there is a single
		/// p2p handler instance for every logged in account. All switchboard sessions route their messages
		/// to this p2p handler. This enables the feature to start a p2p session in one switchboard session,
		/// and continue, or close it, in another switchboard session.
		/// </remarks>
		public P2PHandler P2PHandler
		{
			get { return p2pHandler; }
			set { p2pHandler = value;}
		}

		/// <summary>
		/// The message processor that is used to send and receive nameserver messages.
		/// </summary>
		/// <remarks>
		/// This processor is mainly used by the nameserver handler.
		/// </remarks>
		public NSMessageProcessor	NameserverProcessor
		{
			get { return nsMessageProcessor; }
			set { nsMessageProcessor = value;}
		}

		/// <summary>
		/// Specifies the connection capabilities of the local machine.
		/// </summary>
		/// <remarks>
		/// Use this property to set specific connectivity settings like proxy servers and custom messenger servers.
		/// </remarks>
		public ConnectivitySettings	ConnectivitySettings
		{
			get { return connectivitySettings; }
			set { connectivitySettings = value;}
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
		public Credentials			Credentials
		{
			get { return credentials; }
			set { credentials = value;}
		}


		/// <summary>
		/// The message handler that is used to handle incoming nameserver messages.
		/// </summary>
		public NSMessageHandler	Nameserver
		{
			get { return nsMessageHandler; }
			set { nsMessageHandler = value;}
		}		

		/// <summary>
		/// Returns whether there is a connection with the messenger server.
		/// </summary>
		public bool Connected
		{
			get { return nsMessageProcessor.Connected; }
		}

		/// <summary>
		/// A list of all contacts.
		/// </summary>
		/// <remarks>
		///	This property is a reference to the ContactList object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
		/// </remarks>
		public ContactList	ContactList
		{
			get { return nsMessageHandler.ContactList; }
		}

		/// <summary>
		/// A list of all contactgroups.
		/// </summary>
		/// <remarks>
		///	This property is a reference to the ContactGroups object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
		/// </remarks>
		public ContactGroupList ContactGroups
		{
			get { return nsMessageHandler.ContactGroups; }
		}

		/// <summary>
		/// The local user logged into the network.
		/// </summary>
		/// <remarks>
		/// This property is a reference to the Owner object in the <see cref="Nameserver"/> property. This property is added here for convenient access.
		/// </remarks>
		public Owner		Owner
		{
			get { return nsMessageHandler.Owner; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connect to the messenger network.
		/// </summary>
		public virtual void Connect()
		{
			if(nsMessageProcessor == null)
				throw new MSNPSharpException("No message processor defined");
			if(nsMessageHandler   == null)
				throw new MSNPSharpException("No message handler defined");
			if(credentials		  == null)
				throw new MSNPSharpException("No credentials defined");
			if(credentials.Account.Length == 0)
				throw new MSNPSharpException("The specified account is empty");
			if(credentials.Password.Length == 0)
				throw new MSNPSharpException("The specified password is empty");
			if(credentials.ClientCode.Length == 0 || credentials.ClientID.Length == 0)
				throw new MSNPSharpException("The local messengerclient credentials (client-id and client code) are not specified. This is necessary in order to authenticate the local client with the messenger server. See for more info about the values to use the documentation of the Credentials class.");
			
			// everything is okay, resume
			nsMessageProcessor.ConnectivitySettings = connectivitySettings;
			nsMessageProcessor.RegisterHandler(nsMessageHandler);
			nsMessageHandler.MessageProcessor = nsMessageProcessor;
			nsMessageHandler.Credentials = credentials;
			nsMessageHandler.ConnectivitySettings = connectivitySettings;
			nsMessageProcessor.Connect();
		}

		/// <summary>
		/// Disconnect from the messenger network.
		/// </summary>
		public virtual void Disconnect()
		{
			if(nsMessageProcessor.Connected)
				nsMessageProcessor.Disconnect();		
		}

		#endregion

		/// <summary>
		/// Basic constructor to instantiate a Messenger object.
		/// </summary>
		public Messenger()
		{
			nsMessageProcessor = Factory.CreateNameserverProcessor();
			nsMessageHandler   = Factory.CreateNameserverHandler();
			
			p2pHandler		   = Factory.CreateP2PHandler();
			p2pHandler.NSMessageHandler = nsMessageHandler;			

			// set the events
			nsMessageHandler.SBCreated += new SBCreatedEventHandler(nsMessageHandler_SBCreated);
			nsMessageProcessor.ConnectionClosed += new EventHandler(nsMessageProcessor_ConnectionClosed);
			p2pHandler.SessionCreated += new SessionChangedEventHandler(p2pHandler_SessionCreated);
			p2pHandler.SessionClosed += new SessionChangedEventHandler(p2pHandler_SessionClosed);
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
			SBMessageHandler sbHandler = nsMessageHandler.RequestSwitchboard(this);
			
			Conversation conversation = new Conversation(this, sbHandler);			
			conversation.Switchboard.SessionClosed += new SBChangedEventHandler(Switchboard_SessionClosed);
			
			lock(conversations)
			{
				conversations.Add(conversation);
			}
			
			OnConversationCreated(conversation, this);

			return conversation;
		}

		/// <summary>
		/// Returns a MSNSLPHandler, associated with a P2P session. The returned object can be used to send or receive invitations from the remote contact.
		/// </summary>
		/// <param name="remoteAccount"></param>
		/// <returns></returns>
		public MSNSLPHandler GetMSNSLPHandler(string remoteAccount)
		{					
			P2PMessageSession p2pSession = p2pHandler.GetSession(Owner.Mail, remoteAccount);
			MSNSLPHandler msnslpHandler = (MSNSLPHandler)p2pSession.GetHandler(typeof(MSNSLPHandler));
			if(msnslpHandler == null)
			{
				// create a msn slp handler
				msnslpHandler = CreateMSNSLPHandler();
				p2pSession.RegisterHandler(msnslpHandler);
				msnslpHandler.MessageProcessor = p2pSession;
			}
			return msnslpHandler;
		}

		#endregion

		#region Protected

		/// <summary>
		/// Fires the ConversationCreated event.
		/// </summary>
		/// <param name="conversation"></param>
		/// <param name="initiator"></param>
		protected virtual void OnConversationCreated(Conversation conversation, object initiator)
		{
			if(ConversationCreated != null)
				ConversationCreated(this, new ConversationCreatedEventArgs(conversation, initiator));
		}

		#endregion 

		#region Private

		/// <summary>
		/// Creates the object and sets the external end point.
		/// </summary>
		/// <returns></returns>
		private MSNSLPHandler CreateMSNSLPHandler()
		{
			MSNSLPHandler msnslpHandler =  Factory.CreateMSNSLPHandler();
			msnslpHandler.ExternalEndPoint = Nameserver.ExternalEndPoint;
			return msnslpHandler;
		}

		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void nsMessageHandler_SBCreated(object sender, SBCreatedEventArgs e)
		{						
			e.Switchboard.MessageProcessor.RegisterHandler(P2PHandler);

			// check if the request is remote or on our initiative
			if(e.Initiator != null && (e.Initiator == this || e.Initiator == p2pHandler))
			{
				return;
			}

			// create a conversation object to handle with the switchboard
			Conversation conversation = new Conversation(this, e.Switchboard);	

			// remove the conversation from the list when the conversation has ended.
			conversation.Switchboard.SessionClosed += new SBChangedEventHandler(Switchboard_SessionClosed);

			lock(conversations)
			{
				conversations.Add(conversation);
			}

			// fire event to notify client programmer
			OnConversationCreated(conversation, e.Initiator);
		}

		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void p2pHandler_SessionCreated(object sender, P2PSessionAffectedEventArgs e)
		{			
			MSNSLPHandler msnslpHandler = CreateMSNSLPHandler();
			msnslpHandler.MessageProcessor = e.Session;
			e.Session.RegisterHandler(msnslpHandler);

			// set the correct switchboard to send messages to
			lock(conversations)
			{
				foreach(Conversation conversation in conversations)
				{
					if(conversation.Switchboard.Contacts.ContainsKey(e.Session.RemoteContact))
					{
						e.Session.MessageProcessor = conversation.SwitchboardProcessor;
					}
				}
			}
	
			msnslpHandler.TransferInvitationReceived += new MSNSLPInvitationReceivedEventHandler(msnslpHandler_TransferInvitationReceived);
			msnslpHandlers.Add(msnslpHandler);
		}

		/// <summary>
		/// Accepts by default owner display images and contact emoticons.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void msnslpHandler_TransferInvitationReceived(object sender, MSNSLPInvitationEventArgs e)
		{			
			if(e.TransferProperties.DataType == DataTransferType.DisplayImage)
			{
				e.TransferSession.DataStream = nsMessageHandler.Owner.DisplayImage.OpenStream();				
				e.TransferSession.AutoCloseStream = false;
				
				e.TransferSession.ClientData = nsMessageHandler.Owner.DisplayImage;
				
				e.Handler.AcceptTransfer (e);
			}
			else if(e.TransferProperties.DataType == DataTransferType.Emoticon)
			{								
				MSNObject msnObject = new MSNObject();
				msnObject.ParseContext(e.TransferProperties.Context);

				// send an emoticon
				foreach(Emoticon emoticon in nsMessageHandler.Owner.Emoticons.Values)
				{
					if(emoticon.Sha == msnObject.Sha)
					{
						e.TransferSession.AutoCloseStream = true;
						e.TransferSession.DataStream = emoticon.OpenStream();
						e.TransferSession.ClientData = emoticon;
						e.Handler.AcceptTransfer (e);
					}
				}				
			}
			else
			{
				// forward the invitation to the client programmer
				if(TransferInvitationReceived != null)
					TransferInvitationReceived(sender, e);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		/// <returns></returns>
		private MSNSLPHandler GetMSNSLPHandler(P2PMessageSession session)
		{
			lock(this.msnslpHandlers)
			{
				foreach(MSNSLPHandler handler in msnslpHandlers)
				{
					if(handler.MessageSession == session)
						return handler;
				}
			}
			return null;
		}


		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void p2pHandler_SessionClosed(object sender, P2PSessionAffectedEventArgs e)
		{			
			MSNSLPHandler handler = GetMSNSLPHandler(e.Session);
			if(handler != null)
			{
				lock(this.msnslpHandlers)
				{
					msnslpHandlers.Remove(handler);
				}
			}
		}

		#endregion

		/// <summary>
		/// Cleans up resources.
		/// </summary>
		protected virtual void CleanUp()
		{
			lock(conversations)
			{
				conversations.Clear();				
			}
			lock(msnslpHandlers)
			{
				msnslpHandlers.Clear();
			}
			if(p2pHandler != null)
			{
				p2pHandler.ClearMessageSessions();				
			}
		}

		/// <summary>
		/// Cleans up resources.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void nsMessageProcessor_ConnectionClosed(object sender, EventArgs e)
		{
			CleanUp();			
		}

		/// <summary>
		/// Removes a conversation object from the list when the session is closed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Switchboard_SessionClosed(object sender, EventArgs e)
		{
			lock(conversations)
			{
				foreach(Conversation conversation in conversations)
				{
					if(conversation.Switchboard == sender)
					{
						conversations.Remove(conversation);
						return;
					}
				}
			}
		}

		
	}
}
