#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Changed in 2006 by Thiago M. Sayão <thiago.sayao@gmail.com>: Added support to MSNP11

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
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Net;
using System.Security.Cryptography;
using System.Diagnostics;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;


namespace MSNPSharp
{
	#region Event argument classes
	/// <summary>
	/// Used when a list (FL, Al, BL, RE) is received via synchronize or on request.
	/// </summary>
	[Serializable()]
	public class ListReceivedEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private MSNLists affectedList;

		/// <summary>
		/// The list which was send by the server
		/// </summary>
		public MSNLists AffectedList
		{
			get { return affectedList; }
			set { affectedList = value;}
		}

		/// <summary>
		/// Constructory.
		/// </summary>
		/// <param name="affectedList"></param>
		public ListReceivedEventArgs(MSNLists affectedList)
		{
			AffectedList = affectedList;
		}
	}

	/// <summary>
	/// Used when the local user is signed off.
	/// </summary>
	[Serializable()]
	public class SignedOffEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private SignedOffReason signedOffReason;

		/// <summary>
		/// The list which was send by the server
		/// </summary>
		public SignedOffReason SignedOffReason
		{
			get { return signedOffReason; }
			set { signedOffReason = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="signedOffReason"></param>
		public SignedOffEventArgs(SignedOffReason signedOffReason)
		{
			this.signedOffReason = signedOffReason;
		}
	}


	/// <summary>
	/// Used as event argument when an answer to a ping is received.
	/// </summary>
	[Serializable()]
	public class PingAnswerEventArgs : EventArgs
	{
		/// <summary>
		/// The number of seconds to wait before sending another PNG, 
		/// and is reset to 50 every time a command is sent to the server. 
		/// In environments where idle connections are closed after a short time, 
		/// you should send a command to the server (even if it's just a PNG) at least this often.
		/// Note: MSNPSharp does not handle this! E.g. if you experience unexpected connection dropping call the Ping() method.
		/// </summary>
		public int	SecondsToWait
		{
			get { return secondsToWait; }
			set { secondsToWait = value;}
		}

		/// <summary>
		/// </summary>
		private int	secondsToWait;
	

		/// <summary>
		/// </summary>
		/// <param name="seconds"></param>
		public PingAnswerEventArgs(int seconds)
		{
			SecondsToWait = seconds;			
		}
	}


	/// <summary>
	/// Used as event argument when any contact list mutates.
	/// </summary>
	[Serializable()]
	public class ListMutateEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private Contact contact;

		/// <summary>
		/// The affected contact.
		/// </summary>
		public Contact	Contact
		{
			get { return contact; }
			set { contact = value;}
		}

		/// <summary>
		/// </summary>
		private MSNLists	affectedList;

		/// <summary>
		/// The list which mutated.
		/// </summary>
		public MSNLists	AffectedList
		{
			get { return affectedList; }
			set { affectedList = value;}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contact"></param>
		/// <param name="affectedList"></param>
		public ListMutateEventArgs(Contact contact, MSNLists affectedList)
		{			
			Contact = contact;
			AffectedList = affectedList;
		}
	}


	/// <summary>
	/// Used as event argument when msn sends us an error.
	/// </summary>	
	[Serializable()]
	public class MSNErrorEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private MSNError msnError;
		
		/// <summary>
		/// The error that occurred
		/// </summary>
		public MSNError MSNError
		{
			get { return msnError; }
			set { msnError = value;}
		}


		/// <summary>
		/// Constructory.
		/// </summary>
		/// <param name="msnError"></param>
		public MSNErrorEventArgs(MSNError msnError)
		{
			this.msnError = msnError;
		}
	}	

	#endregion

	#region Enumeration
	/// <summary>
	/// Defines why a user has (been) signed off.
	/// </summary>
	/// <remarks>
	/// <b>OtherClient</b> is used when this account has signed in from another location. <b>ServerDown</b> is used when the msn server is going down.
	/// </remarks>
	public enum SignedOffReason
	{
		/// <summary>
		/// None.
		/// </summary>
		None, 
		/// <summary>
		/// User logged in on the other client.
		/// </summary>
		OtherClient, 
		/// <summary>
		/// Server went down.
		/// </summary>
		ServerDown
	}
	#endregion

	#region Delegates

	/// <summary>
	/// This delegate is used when events are fired and a single contact is affected. 
	/// </summary>
	public delegate void ContactChangedEventHandler(object sender, ContactEventArgs e);

	/// <summary>
	/// This delegate is used when events are fired and a single contact group is affected. 
	/// </summary>
	public delegate void ContactGroupChangedEventHandler(object sender, ContactGroupEventArgs e);

	/// <summary>
	/// This delegate is used when a single contact changed it's status. 
	/// </summary>
	public delegate void ContactStatusChangedEventHandler(object sender, ContactStatusChangeEventArgs e);

	/// <summary>
	/// This delegate is used when a complete list has been received from the server. 
	/// </summary>
	public delegate void ListReceivedEventHandler(object sender, ListReceivedEventArgs e);

	/// <summary>
	/// This delegate is used when a user signed off.
	/// </summary>
	public delegate void SignedOffEventHandler(object sender, SignedOffEventArgs e);
	
	/// <summary>
	/// This delegate is used in a ping answer event. 
	/// </summary>
	public delegate void PingAnswerEventHandler(object sender, PingAnswerEventArgs e);

	/// <summary>
	/// This delegate is used when a list is mutated: a contact is added or removed from a specific list. 
	/// </summary>
	public delegate void ListMutatedAddedEventHandler(object sender, ListMutateEventArgs e);

	/// <summary>
	/// This delegate is used when a new switchboard is created. 
	/// </summary>
	public delegate void SBCreatedEventHandler(object sender, SBCreatedEventArgs e);

	/// <summary>
	/// This delegate is used when the mailbox status of the contact list owner has changed. 
	/// </summary>
	public delegate void MailboxStatusEventHandler(object sender, MailboxStatusEventArgs e);
	/// <summary>
	/// This delegate is used when the contact list owner has received new e-mail.
	/// </summary>
	public delegate void NewMailEventHandler(object sender, NewMailEventArgs e);
	/// <summary>
	/// This delegate is used when the contact list owner has removed or moved existing e-mail.
	/// </summary>
	public delegate void MailChangedEventHandler(object sender, MailChangedEventArgs e);


	#endregion

	/// <summary>
	/// Handles the protocol messages from the notification server.
	/// NS11Handler implements protocol version MSNP9.
	/// </summary>
	public class NSMessageHandler : IMessageHandler
	{
		#region Private
		/// <summary>
		/// </summary>
		private	SocketMessageProcessor	messageProcessor = null;
		/// <summary>
		/// </summary>
		private ConnectivitySettings	connectivitySettings = null;

		/// <summary>
		/// </summary>
		private IPEndPoint	externalEndPoint = null;		

		/// <summary>
		/// </summary>
		private Credentials	credentials = null;

		/// <summary>
		/// </summary>
		private bool		autoSynchronize = true;

		/// <summary>
		/// </summary>
		private ContactList	contactList   = new ContactList();

		/// <summary>
		/// </summary>
		private ContactGroupList contactGroups = null;		

		/// <summary>
		/// </summary>
		private Owner		owner = new Owner();

		/// <summary>
		/// Tracks the last contact object which has been synchronized. Used for BPR commands.
		/// </summary>
		private Contact		lastContactSynced = null;

		/// <summary>
		/// Tracks the number of LST commands to expect
		/// </summary>
		private int syncContactsCount = 0;

		/// <summary>
		/// Keep track whether a synchronization has been completed so we can warn the client programmer
		/// </summary>
		private bool synSended = false;

		/// <summary>
		/// Keeps track of the last received synchronization identifier
		/// </summary>
		private int	lastSync = -1;

		/// <summary>
		/// Defines whether the user is signed in the messenger network
		/// </summary>
		private bool		isSignedIn = false;

		/// <summary>
		/// A collection of all switchboard handlers waiting for a connection
		/// </summary>
		private Queue		pendingSwitchboards = new Queue();

		/// <summary>
		/// Event handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NSMessageHandler_ProcessorConnectCallback(object sender, EventArgs e)
		{
			synSended = false;
			IMessageProcessor processor = (IMessageProcessor)sender;	
			OnProcessorConnectCallback(processor);
		}

		/// <summary>
		/// Event handler.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NSMessageHandler_ProcessorDisconnectCallback(object sender, EventArgs e)
		{
			IMessageProcessor processor = (IMessageProcessor)sender;	

			if(IsSignedIn == true)
				OnSignedOff(SignedOffReason.None);			

			OnProcessorDisconnectCallback(processor);

			Clear();
			
		}		

		#endregion

		#region Protected

		/// <summary>
		/// Clears all resources associated with a nameserver session.
		/// </summary>
		/// <remarks>
		/// Called after we the processor has disconnected. This will clear the contactlist and free other resources.
		/// </remarks>
		protected virtual void Clear()
		{
			contactList = new ContactList();						
			contactGroups = new ContactGroupList(this);
			owner = new Owner();
			owner.NSMessageHandler = this;			

			synSended = false;
		}

		/// <summary>
		/// Translates the codes used by the MSN server to a MSNList object.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected MSNLists GetMSNList(string name)
		{
			switch(name)
			{
				case "AL": return MSNLists.AllowedList;
				case "FL": return MSNLists.ForwardList;
				case "BL": return MSNLists.BlockedList;
				case "RL": return MSNLists.ReverseList;
				case "PL": return MSNLists.PendingList;
			}
			throw new MSNPSharpException("Unknown MSNList type");
		}

		/// <summary>
		/// Translates the MSNList object to the codes used by the MSN server.
		/// </summary>
		/// <param name="list"></param>
		/// <returns></returns>
		protected string  GetMSNList(MSNLists list)
		{
			switch(list)
			{
				case MSNLists.AllowedList: return "AL";
				case MSNLists.ForwardList: return "FL";
				case MSNLists.BlockedList: return "BL";
				case MSNLists.ReverseList: return "RL";
				case MSNLists.PendingList: return "PL";
			}
			throw new MSNPSharpException("Unknown MSNList type");
		}
		
		/// <summary>
		/// Class used for items stored in the switchboard queue.
		/// </summary>
		protected class SwitchboardQueueItem
		{
			/// <summary>
			/// The object that initiated the request.
			/// </summary>
			public object Initiator;
			/// <summary>
			/// The switchboard handler that will be handling the new switchboard session.
			/// </summary>
			public SBMessageHandler	SwitchboardHandler;

			/// <summary>
			/// Constructs a queue item.
			/// </summary>
			/// <param name="switchboardHandler"></param>
			/// <param name="initiator"></param>
			public SwitchboardQueueItem(SBMessageHandler switchboardHandler, object initiator)
			{
				Initiator = initiator;
				SwitchboardHandler = switchboardHandler;
			}
		}

		#endregion

		#region Public Events
		

		/// <summary>
		/// Occurs when a new contactgroup is created
		/// </summary>
		public event ContactGroupChangedEventHandler	ContactGroupAdded;
		/// <summary>
		/// Occurs when a contactgroup is removed
		/// </summary>
		public event ContactGroupChangedEventHandler	ContactGroupRemoved;
		/// <summary>
		/// Occurs when a contactgroup property is changed
		/// </summary>
		//public event ContactGroupChangedEventHandler		ContactGroupChanged;		

		/// <summary>
		/// Occurs when an exception is thrown while handling the incoming or outgoing messages
		/// </summary>
		public event HandlerExceptionEventHandler	ExceptionOccurred;

		/// <summary>
		/// Occurs when the user could not be signed in due to authentication errors. Most likely due to an invalid account or password. Note that this event will also raise the more general <see cref="ExceptionOccurred"/> event.
		/// </summary>
		public event HandlerExceptionEventHandler	AuthenticationError;

		/*/// <summary>
		/// Occurs when a message is received from a MSN server. This is not a message from another contact!
		/// These messages are handled internally in dotMSN, but by using this event you can peek at the incoming messages.
		/// </summary>
		public event MessageReceivedHandler		MessageReceived; */

		/// <summary>
		/// Occurs when an answer is received after sending a ping to the MSN server via the SendPing() method
		/// </summary>
		public event PingAnswerEventHandler			PingAnswer;

		/// <summary>
		/// Occurs when a contact is added to any list (including reverse list)
		/// </summary>
		public event ListMutatedAddedEventHandler		ContactAdded;
		/// <summary>
		/// Occurs when a contact is removed from any list (including reverse list)
		/// </summary>
		public event ListMutatedAddedEventHandler		ContactRemoved;

		/// <summary>
		/// Occurs when another user adds us to their contactlist. A ContactAdded event with the reverse list as parameter will also be raised.
		/// </summary>
		public event ContactChangedEventHandler		ReverseAdded;
		/// <summary>
		/// Occurs when another user removes us from their contactlist. A ContactRemoved event with the reverse list as parameter will also be raised.
		/// </summary>
		public event ContactChangedEventHandler		ReverseRemoved;
		
		/// <summary>
		/// Occurs when any contact changes status
		/// </summary>
		public event ContactStatusChangedEventHandler ContactStatusChanged;

		/// <summary>
		/// Occurs when any contact goes from offline status to another status
		/// </summary>
		public event ContactChangedEventHandler		ContactOnline;
		/// <summary>
		/// Occurs when any contact goed from any status to offline status
		/// </summary>
		public event ContactChangedEventHandler		ContactOffline;

		/*/// <summary>
		/// Occurs when any of the 4 lists is received. The requested list is given in the event arguments object.
		/// </summary>
		public event ListReceivedEventHandler		ListReceived;*/

		/// <summary>
		/// Occurs when a call to SynchronizeList() has been made and the synchronization process is completed.
		/// This means all contact-updates are received from the server and processed.
		/// </summary>
		public event EventHandler					SynchronizationCompleted;

		/// <summary>
		/// Occurs when the authentication and authorzation with the server has finished. The client is now connected to the messenger network.
		/// </summary>
		public event EventHandler					SignedIn;

		/// <summary>
		/// Occurs when the message processor has disconnected, and thus the user is no longer signed in.
		/// </summary>
		public event SignedOffEventHandler			SignedOff;

		/// <summary>
		/// Occurs when a switchboard session has been created
		/// </summary>
		public event SBCreatedEventHandler			SBCreated;		
		
		/// <summary>
		/// Occurs when the server notifies the client with the status of the owner's mailbox.
		/// </summary>
		public event MailboxStatusEventHandler		MailboxStatusReceived;

		/// <summary>
		/// Occurs when new mail is received by the Owner.
		/// </summary>
		public event NewMailEventHandler			NewMailReceived;

		/// <summary>
		/// Occurs when unread mail is read or mail is moved by the Owner.
		/// </summary>
		public event MailChangedEventHandler			MailboxChanged;

		/// <summary>
		/// Occurs when the the server send an error.
		/// </summary>
		public event ErrorReceivedEventHandler			ServerErrorReceived;

		/// <summary>
		/// Fires the ServerErrorReceived event.
		/// </summary>
		/// <param name="msnError"></param>
		protected virtual void OnServerErrorReceived(MSNError msnError)
		{
			if(ServerErrorReceived != null)
				ServerErrorReceived(this, new MSNErrorEventArgs(msnError));
		}

		/// <summary>
		/// Fires the SignedIn event.
		/// </summary>
		protected virtual void OnSignedIn()
		{		
			isSignedIn = true;
			if(SignedIn != null)
				SignedIn(this, new EventArgs());
		}

		/// <summary>
		/// Fires the SignedOff event.
		/// </summary>
		protected virtual void OnSignedOff(SignedOffReason reason)
		{		
			isSignedIn = false;
			if(SignedOff != null)
				SignedOff(this, new SignedOffEventArgs(reason));
		}

		/// <summary>
		/// Fires the <see cref="ExceptionOccurred"/> event.
		/// </summary>
		/// <param name="e">The exception which was thrown</param>
		protected virtual void OnExceptionOccurred(Exception e)
		{
			if(ExceptionOccurred != null)
				ExceptionOccurred(this, new ExceptionEventArgs(e));

			if(Settings.TraceSwitch.TraceError)
				System.Diagnostics.Trace.WriteLine(e.ToString(), "NS11MessageHandler");
		}

		/// <summary>
		/// Fires the <see cref="AuthenticationError"/> event.
		/// </summary>
		/// <param name="e">The exception which was thrown</param>
		protected virtual void OnAuthenticationErrorOccurred(UnauthorizedException e)
		{
			if(AuthenticationError != null)
				AuthenticationError(this, new ExceptionEventArgs(e));

			if(Settings.TraceSwitch.TraceError)
				System.Diagnostics.Trace.WriteLine(e.ToString(), "NS11MessageHandler");
		}


		/// <summary>
		/// Fires the <see cref="ExceptionOccurred"/> event.
		/// </summary>
		/// <param name="switchboard">The switchboard created</param>
		/// <param name="initiator">The object that initiated the switchboard request.</param>
		protected virtual void OnSBCreated(SBMessageHandler switchboard, object initiator)
		{
			if(SBCreated != null)
				SBCreated(this, new SBCreatedEventArgs(switchboard, initiator));
		}
		#endregion

		#region Public

		/// <summary>
		/// Constructor.
		/// </summary>
		private NSMessageHandler()
		{			
			owner.NSMessageHandler = this;
			contactGroups = new ContactGroupList(this);
		}

		/// <summary>
		/// Defines if the contact list is automatically synchronized upon connection.
		/// </summary>
		public bool AutoSynchronize
		{
			get { return autoSynchronize; }
			set { autoSynchronize = value;}
		}

		/// <summary>
		/// Defines whether the user is signed in the messenger network
		/// </summary>
		public bool		IsSignedIn
		{
			get { return isSignedIn; }
		}

		/// <summary>
		/// The end point as perceived by the server. This is set after the owner's profile is received.
		/// </summary>
		public IPEndPoint ExternalEndPoint
		{
			get { return externalEndPoint; }
		}
		
		/// <summary>
		/// A collection of all contacts which are on any of the lists of the person who logged into the messenger network
		/// </summary>
		public ContactList ContactList
		{
			get { return contactList; }			
		}

		/// <summary>
		/// The owner of the contactlist. This is the identity that logged into the messenger network.
		/// </summary>
		public Owner	Owner
		{
			get { return owner; }
		}
	 
		/// <summary>
		/// A collection of all contactgroups which are defined by the user who logged into the messenger network.\
		/// </summary>
		public ContactGroupList ContactGroups
		{
			get { return contactGroups; }
		}

		/// <summary>
		/// These credentials are used for user authentication and client identification
		/// </summary>
		public Credentials Credentials
		{
			get { return credentials; }
			set { credentials = value;}
		}

		/// <summary>
		/// If WebProxy is set the Webproxy is used for the
		/// authentication with Passport.com
		/// </summary>
		public ConnectivitySettings ConnectivitySettings
		{
			get { return connectivitySettings; }
			set { connectivitySettings = value;}
		}

		#region Message sending methods

		/// <summary>
		/// Send the synchronize command to the server. This will rebuild the contactlist with the most recent data.
		/// </summary>
		/// <remarks>
		/// Synchronizing is the most complete way to retrieve data about groups, contacts, privacy settings, etc.
		/// You <b>must</b> call this function before setting your initial status, otherwise you won't received online notifications of other clients.
		/// Please note that you can only synchronize a single time per session! (this is limited by the the msn-servers)
		/// </remarks>
		public virtual void SynchronizeContactList()
		{						
			if(synSended)
			{
				if(Settings.TraceSwitch.TraceWarning)
					Trace.WriteLine("SynchronizeContactList() was called, but the list has already been synchronized. Make sure the AutoSynchronize property is set to false in order to manually synchronize the contact list.", "NS11MessageHandler");
				return;
			}
			MessageProcessor.SendMessage(new NSMessage("SYN", new string[] { "0", "0" }));
		}
		
		
		/// <summary>
		/// Moves a contact from the current contact group to a new contact group.
		/// </summary>
		/// <param name="contact">Contact to be moved</param>
		/// <param name="group">The new contact group</param>
		// MSNP11 allows multiple groups for each contact
		/*public virtual void ChangeGroup(Contact contact, ContactGroup group)
		{			
			// first remove from the old contact group with the REM command, then add it to the new contact group 
			if(contact.OnForwardList)
				MessageProcessor.SendMessage(new NSMessage("REM", new string[]{GetMSNList(MSNLists.ForwardList), contact.Guid, contact.ContactGroup.Guid } ));
			
			MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(MSNLists.ForwardList), "C="+contact.Guid, group.Guid  } ));
		}*/
		
		public virtual void AddContactToGroup(Contact contact, ContactGroup group)
		{
			if (contact.OnForwardList && !contact.HasGroup (group))
				MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(MSNLists.ForwardList), "C="+contact.Guid, group.Guid  } ));
		}

		public virtual void RemoveContactFromGroup(Contact contact, ContactGroup group)
		{
			if(contact.OnForwardList && contact.HasGroup (group))
				MessageProcessor.SendMessage(new NSMessage("REM", new string[]{GetMSNList(MSNLists.ForwardList), contact.Guid, group.Guid } ));
		}

		/// <summary>
		/// Send the server a request for the contact's screen name.
		/// </summary>
		/// <remarks>
		/// When the server replies with the screen name the Name property of the <see cref="Contact"/> will
		/// be updated.
		/// </remarks>
		/// <param name="contact"></param>
		public virtual void RequestScreenName(Contact contact)
		{
			MessageProcessor.SendMessage(new NSMessage("SBP", new string[]{ contact.Guid, "MFN", System.Web.HttpUtility.UrlEncode(contact.Name, Encoding.UTF8) } ));			
		}		

		/// <summary>
		/// Creates a new contact and sends a request to the server to add this contact to the forward and allowed list.
		/// </summary>
		/// <remarks>A server error will be received when the account is for some reason invalid. See also the <see cref="ServerErrorReceived"/> event.</remarks>
		/// <param name="account">The principal's passport account. Usually an e-mail address.</param>		
		public virtual void AddNewContact(string account)
		{			
			if(!contactList.HasContact (account))
			{
				MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(MSNLists.ForwardList), "N="+account, "F="+account } ));
				MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(MSNLists.AllowedList), "N="+account } ));
			}
			else
				throw new MSNPSharpException("Account already exists in any of the four possible lists. Use AddContactToList instead.");
		}

		/// <summary>
		/// Send a request to the server to add this contact to a specific list.
		/// </summary>
		/// <param name="contact">The affected contact</param>
		/// <param name="list">The list to place the contact in</param>
		public virtual void AddContactToList(Contact contact, MSNLists list)
		{
			if(list == MSNLists.PendingList) //this causes disconnect 
				return;

			// check whether the update is necessary
			if(list == MSNLists.ForwardList && contact.OnForwardList) return;
			if(list == MSNLists.BlockedList && contact.OnBlockedList) return;
			if(list == MSNLists.AllowedList && contact.OnAllowedList) return;
			if(list == MSNLists.ReverseList && contact.OnReverseList) return;
			
			if (list == MSNLists.ForwardList)
				MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(list), "N="+contact.Mail, "F="+contact.Mail } ));
			else
				MessageProcessor.SendMessage(new NSMessage("ADC", new string[]{GetMSNList(list), "N="+contact.Mail } ));
		}


		/// <summary>
		/// Send a request to the server to add a new contactgroup.
		/// </summary>
		/// <param name="groupName">The name of the group to add</param>
		public virtual void AddContactGroup(string groupName)
		{
			// > 128 bytes will fail and disconnect immediately
			// >  61 bytes will give erro 229
			// so as a safety margin we set out for 40 characters
			string name = System.Web.HttpUtility.UrlEncode(groupName, Encoding.UTF8).Replace("+", "%20");

			if(name.Length > 60)
				throw new MSNPSharpException("Contactgroup name is too long.");
				
			MessageProcessor.SendMessage(new NSMessage("ADG", new string[]{name, "0" }));
		}

		/// <summary>
		/// Send a request to the server to remove a contactgroup. Any contacts in the group will also be removed from the forward list.
		/// </summary>
		/// <param name="contactGroup">The group to remove</param>
		public virtual void RemoveContactGroup(ContactGroup contactGroup)
		{			
			MessageProcessor.SendMessage(new NSMessage("RMG", new string[]{ contactGroup.Guid.ToString() }));
		}

		/// <summary>
		/// Send a request to the server to remove a contact from a specific list.
		/// </summary> 
		/// <param name="contact">The affected contact</param>
		/// <param name="list">The list to remove the contact from</param>
		public virtual void RemoveContactFromList(Contact contact, MSNLists list)
		{
			if (list == MSNLists.ReverseList) //this causes disconnect
				return;
		
			// check whether the update is necessary
			if(list == MSNLists.ForwardList && !contact.OnForwardList) return;
			if(list == MSNLists.BlockedList && !contact.OnBlockedList) return;
			if(list == MSNLists.AllowedList && !contact.OnAllowedList) return;
			if(list == MSNLists.PendingList && !contact.OnPendingList) return;

			if(list == MSNLists.ForwardList)
				MessageProcessor.SendMessage(new NSMessage("REM", new string[]{ GetMSNList(list), contact.Guid } ));
			else
				MessageProcessor.SendMessage(new NSMessage("REM", new string[]{ GetMSNList(list), contact.Mail } ));		
		}

		/// <summary>
		/// Block this contact. This way you don't receive any messages anymore. This contact
		/// will be removed from your allow list and placed in your blocked list.
		/// </summary>
		/// <param name="contact">Contact to block</param>
		public virtual void BlockContact(Contact contact)
		{			
			if(contact.OnAllowedList)
			{
				RemoveContactFromList(contact, MSNLists.AllowedList);

				// wait some time before sending the other request. If not we may block before we removed
				// from the allow list which will give an error
				System.Threading.Thread.Sleep(50);
			}

			if(!contact.OnBlockedList)
				AddContactToList(contact, MSNLists.BlockedList);						
		}

		/// <summary>
		/// Unblock this contact. After this you are able to receive messages from this contact. This contact
		/// will be removed from your blocked list and placed in your allowed list.
		/// </summary>
		/// <param name="contact">Contact to unblock</param>
		public virtual void UnBlockContact(Contact contact)
		{
			if(contact.OnBlockedList)
			{
				AddContactToList(contact, MSNLists.AllowedList);
				RemoveContactFromList(contact, MSNLists.BlockedList);
			}
		}

		/// <summary>
		/// Remove the specified contact from your forward and allow list. Note that remote contacts that are blocked remain blocked.
		/// </summary>
		/// <param name="contact">Contact to remove</param>
		public virtual void RemoveContact(Contact contact)
		{		
			if (contact.OnAllowedList)
				RemoveContactFromList(contact, MSNLists.AllowedList);
				
			if (contact.OnForwardList)
				RemoveContactFromList(contact, MSNLists.ForwardList);
				
			if (contact.OnPendingList)
				RemoveContactFromList(contact, MSNLists.PendingList);
		}

		/// <summary>
		/// Set the contactlist owner's privacy mode.
		/// </summary>
		/// <param name="privacy">New privacy setting</param>
		public virtual void SetPrivacyMode(PrivacyMode privacy)
		{
			if(privacy == PrivacyMode.AllExceptBlocked)			
				MessageProcessor.SendMessage(new NSMessage("BLP", new string[] { "AL" }));
				
			if(privacy == PrivacyMode.NoneButAllowed)
				MessageProcessor.SendMessage(new NSMessage("BLP", new string[] { "BL" }));
				
		}

		/// <summary>
		/// Set the contactlist owner's notification mode.
		/// </summary>
		/// <param name="privacy">New notify privacy setting</param>
		public virtual void SetNotifyPrivacyMode(NotifyPrivacy privacy)
		{
			if(privacy == NotifyPrivacy.AutomaticAdd)
				MessageProcessor.SendMessage(new NSMessage("GTC", new string[] { "N" }));				
			if(privacy == NotifyPrivacy.PromptOnAdd)
				MessageProcessor.SendMessage(new NSMessage("GTC", new string[] { "A" }));				
		}

		/// <summary>
		/// Set the status of the contactlistowner (the client).
		/// Note: you can only set the status _after_ you have synchronized the list using SynchronizeList(). Otherwise you won't receive online notifications from other clients or the connection is closed by the server.
		/// </summary>
		/// <param name="status"></param>
		public virtual void SetPresenceStatus(PresenceStatus status)
		{
			// check whether we are allowed to send a CHG command
			if(synSended == false)
				throw new MSNPSharpException("Can't set status. You must call SynchronizeList() and wait for the SynchronizationCompleted event before you can set an initial status.");

			//MSNP9/MSNC1: added the 268435456 at the end			
			string context = "";
			if(Owner.DisplayImage != null)
				context = Owner.DisplayImage.Context;
			
			if (status == PresenceStatus.Offline)
				messageProcessor.Disconnect ();
			//don't set the same status or it will result in disconnection
			else if (status != Owner.Status)
				MessageProcessor.SendMessage(new NSMessage("CHG", new string[] { ParseStatus(status), "1073791084", context }));						
		}

		/// <summary>
		/// Sets the contactlist owner's screenname. After receiving confirmation from the server
		/// this will set the Owner object's name which will in turn raise the NameChange event.
		/// </summary>
		public virtual void SetScreenName(string newName)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MFN", System.Web.HttpUtility.UrlEncode(newName, Encoding.UTF8).Replace("+", "%20") }));
		}
		
		public virtual void SetPersonalMessage (PersonalMessage pmsg)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			
			MSNMessage msg = new MSNMessage ();
			string payload = pmsg.Payload; 
			msg.Command = payload;
			
			int size = payload.Length;
			
			MessageProcessor.SendMessage(new NSMessage("UUX", new string[] { Convert.ToString (size) }));
			MessageProcessor.SendMessage(msg);
		}

		/// <summary>
		/// Sets the telephonenumber for the contact list owner's homephone.
		/// </summary>
		public virtual void SetPhoneNumberHome(string number)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			if(number.Length > 30)
				throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");
			
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHH", System.Web.HttpUtility.UrlEncode(number, Encoding.UTF8).Replace("+", "%20") }));
		}
		/// <summary>
		/// Sets the telephonenumber for the contact list owner's workphone.
		/// </summary>
		public virtual void SetPhoneNumberWork(string number)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			if(number.Length > 30)
				throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHW", System.Web.HttpUtility.UrlEncode(number, Encoding.UTF8).Replace("+", "%20") }));
		}

		/// <summary>
		/// Sets the telephonenumber for the contact list owner's mobile phone.
		/// </summary>
		public virtual void SetPhoneNumberMobile(string number)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			if(number.Length > 30)
				throw new MSNPSharpException("Telephone number too long. Maximum length for a phone number is 30 digits.");
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "PHM", System.Web.HttpUtility.UrlEncode(number, Encoding.UTF8).Replace("+", "%20") }));
		}

		/// <summary>
		/// Sets whether the contact list owner allows remote contacts to send messages to it's mobile device.
		/// </summary>
		public virtual void SetMobileAccess(bool enabled)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");						
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MOB", enabled ? "Y" : "N"  }));
		}

		/// <summary>
		/// Sets whether the contact list owner has a mobile device enabled.
		/// </summary>
		public virtual void SetMobileDevice(bool enabled)
		{
			if(owner == null) throw new MSNPSharpException("Not a valid owner");			
			MessageProcessor.SendMessage(new NSMessage("PRP", new string[] { "MBE", enabled ? "Y" : "N" }));
		}

		/// <summary>
		/// Set the name of a contact group
		/// </summary>
		/// <param name="group">The contactgroup which name will be set</param>
		/// <param name="newGroupName">The new name</param>
		public virtual void RenameGroup(ContactGroup group, string newGroupName)
		{						
			MessageProcessor.SendMessage(new NSMessage("REG", new string[] { group.Guid.ToString(System.Globalization.CultureInfo.InvariantCulture), System.Web.HttpUtility.UrlEncode(newGroupName, Encoding.UTF8).Replace("+", "%20"), "0" }));
		}

		/// <summary>
		/// Send the first message to the server. This is usually the VER command.
		/// </summary>
		protected virtual void SendInitialMessage()
		{
			MessageProcessor.SendMessage(new NSMessage("VER", new string[] { "MSNP11", "CVR0" } ));
		}


		/// <summary>
		/// Sends a request to the server to start a new switchboard session.
		/// </summary>
		public virtual SBMessageHandler RequestSwitchboard(object initiator)
		{
			// create a switchboard object
			SBMessageHandler handler = Factory.CreateSwitchboardHandler();			

			RequestSwitchboard(handler, initiator);

			return handler;
		}

		/// <summary>
		/// Sends a request to the server to start a new switchboard session. The specified switchboard handler will be associated with the new switchboard session.
		/// </summary>
		/// <param name="switchboardHandler">The switchboard handler to use. A switchboard processor will be created and connected to this handler.</param>
		/// <param name="initiator">The object that initiated the request for the switchboard.</param>
		/// <returns></returns>
		public virtual void RequestSwitchboard(SBMessageHandler switchboardHandler, object initiator)
		{
			pendingSwitchboards.Enqueue(new SwitchboardQueueItem(switchboardHandler, initiator));
			MessageProcessor.SendMessage(new NSMessage("XFR", new string[] { "SB"} ));			
		}

		/// <summary>
		/// Sends a mobile message to the specified remote contact. This only works when the remote contact has it's mobile device enabled and has MSN-direct enabled.
		/// </summary>
		/// <param name="receiver"></param>
		/// <param name="text"></param>
		public virtual void SendMobileMessage(Contact receiver, string text)
		{
			SendMobileMessage(receiver, text, "", "");
		}

		/// <summary>
		/// Sends a mobile message to the specified remote contact. This only works when the remote contact has it's mobile device enabled and has MSN-direct enabled.
		/// </summary>
		/// <param name="receiver"></param>
		/// <param name="text"></param>
		/// <param name="callbackNumber"></param>
		/// <param name="callbackDevice"></param>
		public virtual void SendMobileMessage(Contact receiver, string text, string callbackNumber, string callbackDevice)
		{
			if(receiver.MobileAccess == false)
				throw new MSNPSharpException("A direct message can not be send. The specified contact has no mobile device enabled.");
			
			// create a body message
			MobileMessage bodyMessage = new MobileMessage();
			bodyMessage.CallbackDeviceName = callbackDevice;
			bodyMessage.CallbackNumber = callbackNumber;
			bodyMessage.Receiver = receiver.Mail;
			bodyMessage.Text = text;

			// create an NS message to transport it
			NSMessage nsMessage = new NSMessage();
			nsMessage.InnerMessage = bodyMessage;

			// and send it
			MessageProcessor.SendMessage(nsMessage);            
		}

		#endregion

		#region IMessageHandler Members	

        /// <summary>
        /// </summary>
		private	EventHandler	processorConnectedHandler = null;

		/// <summary>
		/// </summary>
		private	EventHandler	processorDisconnectedHandler = null;

		/// <summary>
		/// The processor to handle the messages
		/// </summary>
		public IMessageProcessor MessageProcessor
		{
			get
			{				
				return messageProcessor;
			}
			set
			{
				if(processorConnectedHandler != null && messageProcessor != null)
				{
					// de-register from the previous message processor					
					((SocketMessageProcessor)messageProcessor).ConnectionEstablished -= processorConnectedHandler;
				}
				
				if(processorConnectedHandler == null)
				{
					processorConnectedHandler = new EventHandler(NSMessageHandler_ProcessorConnectCallback);
					processorDisconnectedHandler = new EventHandler(NSMessageHandler_ProcessorDisconnectCallback);
				}

				messageProcessor = (SocketMessageProcessor)value;

				// catch the connect event so we can start sending the USR command upon initiating
				((SocketMessageProcessor)messageProcessor).ConnectionEstablished += processorConnectedHandler;

				// and make sure we respond on closing
				((SocketMessageProcessor)messageProcessor).ConnectionClosed += processorDisconnectedHandler;
			}
		}


		/// <summary>
		/// Called when the message processor has established a connection. This function will 
		/// begin the login procedure by sending the VER command.
		/// </summary>
		/// <param name="sender"></param>
		protected virtual void OnProcessorConnectCallback(IMessageProcessor sender)
		{
			SendInitialMessage();			
		}

		/// <summary>
		/// Called when the message processor has disconnected.
		/// </summary>
		/// <param name="sender"></param>
		protected virtual void OnProcessorDisconnectCallback(IMessageProcessor sender)
		{
			// do nothing
		}


		/// <summary>
		/// Handles message from the processor.
		/// </summary>
		/// <remarks>
		/// This is one of the most important functions of the class.
		/// It handles incoming messages and performs actions based on the commands in the messages.
		/// Many commands will affect the data objects in MSNPSharp, like <see cref="Contact"/> and <see cref="ContactGroup"/>.
		/// For example contacts are renamed, contactgroups are added and status is set.
		/// Exceptions which occur in this method are redirected via the <see cref="ExceptionOccurred"/> event.
		/// </remarks>
		/// <param name="sender">The message processor that dispatched the message.</param>
		/// <param name="message">The network message received from the notification server</param>
		public virtual void HandleMessage(IMessageProcessor sender, NetworkMessage message)
		{
			try
			{
				// we expect at least a NSMessage object
				NSMessage nsMessage = (NSMessage)message;
				
				switch(nsMessage.Command)
				{														
					case "ADC":	 OnADCReceived(nsMessage); break;
					case "ADG":  OnADGReceived(nsMessage); break; 
					case "BLP":  OnBLPReceived(nsMessage); break; 
					case "BPR":  OnBPRReceived(nsMessage); break; 
					case "CHG":  OnCHGReceived(nsMessage); break; 
					case "CHL":  OnCHLReceived(nsMessage); break; 
					case "CVR":  OnCVRReceived(nsMessage); break; 
					case "FLN":  OnFLNReceived(nsMessage); break; 
					case "GTC":  OnGTCReceived(nsMessage); break; 
					case "ILN":  OnILNReceived(nsMessage); break;
					case "LSG":  OnLSGReceived(nsMessage); break; 
					case "LST":  OnLSTReceived(nsMessage); break; 
					case "MSG":  OnMSGReceived(nsMessage); break; 
					case "NLN":  OnNLNReceived(nsMessage); break; 
					case "NOT":  OnNOTReceived(nsMessage); break; 
					case "OUT":  OnOUTReceived(nsMessage); break; 
					case "PRP":  OnPRPReceived(nsMessage); break; 
					case "QNG":  OnQNGReceived(nsMessage); break; 
					case "RMG":  OnRMGReceived(nsMessage); break; 
					case "REM":  OnREMReceived(nsMessage); break; 
					case "RNG":  OnRNGReceived(nsMessage); break; 
					case "SYN":  OnSYNReceived(nsMessage); break; 
					case "USR":  OnUSRReceived(nsMessage); break; 
					case "VER":  OnVERReceived(nsMessage); break;
					case "XFR":  OnXFRReceived(nsMessage); break; 
					case "UBX":	 OnUBXReceived(nsMessage); break;
					default:
						// first check whether it is a numeric error command
						if(nsMessage.Command[0] >= '0' && nsMessage.Command[0] <= '9')
						{
							MSNError msnError = 0;
							try
							{
								int errorCode = int.Parse(nsMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
								msnError = (MSNError)errorCode;
							}
							catch(Exception e)
							{
								throw new MSNPSharpException("Exception Occurred when parsing an error code received from the server", e);
							}
							OnServerErrorReceived(msnError);
						}
						
						// if not then it is a unknown command:
						// do nothing.
						break;			
				}
			}
			catch(Exception e)
			{				
				// notify the client of this exception
				OnExceptionOccurred(e);
				throw e;
			}
		}

		#region Passport authentication
		/// <summary>
		/// Authenticates the contactlist owner with the Passport (Nexus) service.		
		/// </summary>
		/// <remarks>
		/// The passport uri in the ConnectivitySettings is used to determine the service location. Also if a WebProxy is specified the resource will be accessed
		/// through the webproxy.
		/// </remarks>
		/// <param name="twnString">The string passed as last parameter of the USR command</param>
		/// <returns>The ticket string</returns>
		private string AuthenticatePassport(string twnString)
		{			
			// check whether we have settings to follow
			if(ConnectivitySettings == null)
				throw new MSNPSharpException("No ConnectivitySettings specified in the NSMessageHandler");

			try
			{
				// first login to nexus			
				/*
				Bas Geertsema: as of 14 march 2006 this is not done anymore since it returned always the same URI, and only increased login time.
				
				WebRequest request = HttpWebRequest.Create(ConnectivitySettings.PassportUri);
				if(ConnectivitySettings.WebProxy != null)
					request.Proxy = ConnectivitySettings.WebProxy;

				Uri uri = null;
				// create the header				
				using(WebResponse response = request.GetResponse())
				{
					string urls = response.Headers.Get("PassportURLs");			

					// get everything from DALogin= till the next ,
					// example string: PassportURLs: DARealm=Passport.Net,DALogin=login.passport.com/login2.srf,DAReg=http://register.passport.net/uixpwiz.srf,Properties=https://register.passport.net/editprof.srf,Privacy=http://www.passport.com/consumer/privacypolicy.asp,GeneralRedir=http://nexusrdr.passport.com/redir.asp,Help=http://memberservices.passport.net/memberservice.srf,ConfigVersion=11
					Regex re = new Regex("DALogin=([^,]+)");
					Match m  = re.Match(urls);
					if(m.Success == false)
					{
						throw new MSNPSharpException("Regular expression failed; no DALogin (messenger login server) could be extracted");
					}											
					string loginServer = m.Groups[1].ToString();

					uri = new Uri("https://"+loginServer);
					response.Close();
				}
				*/

				string ticket = null;
				// login at the passport server
				using(WebResponse response = PassportServerLogin(ConnectivitySettings.PassportUri, twnString, 0))
				{								
					// at this point the login has succeeded, otherwise an exception is thrown
					ticket = response.Headers.Get("Authentication-Info");
					Regex re = new Regex("from-PP='([^']+)'");
					Match m  = re.Match(ticket);
					if(m.Success == false)
					{
						throw new MSNPSharpException("Regular expression failed; no ticket could be extracted");
					}
					// get the ticket (kind of challenge string
					ticket = m.Groups[1].ToString();			

					response.Close();
				}

				return ticket;
			}
			catch(UnauthorizedException e)
			{				
				// call this event
				OnAuthenticationErrorOccurred(e);

				// rethrow to client programmer
				throw e;
			}
			catch(Exception e)
			{
				// wrap in a new exception
				throw new MSNPSharpException("Authenticating with the Nexus service failed : " +e.ToString(), e);
			}
		}


		/// <summary>
		/// Login at the Passport server.		
		/// </summary>
		/// <exception cref="UnauthorizedException">Thrown when the credentials are invalid.</exception>
		/// <param name="serverUri"></param>
		/// <param name="twnString"></param>
		/// <returns></returns>
		private WebResponse PassportServerLogin(Uri serverUri, string twnString, int retries)
		{
            // create the header to login
			// example header:
			// >>> GET /login2.srf HTTP/1.1\r\n
			// >>> Authorization: Passport1.4 OrgVerb=GET,OrgURL=http%3A%2F%2Fmessenger%2Emsn%2Ecom,example%40passport.com,pwd=password,lc=1033,id=507,tw=40,fs=1,ru=http%3A%2F%2Fmessenger%2Emsn%2Ecom,ct=1062764229,kpp=1,kv=5,ver=2.1.0173.1,tpf=43f8a4c8ed940c04e3740be46c4d1619\r\n
			// >>> Host: login.passport.com\r\n
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(serverUri);			
			if(ConnectivitySettings.WebProxy != null)
				request.Proxy = ConnectivitySettings.WebProxy;
            
            request.Headers.Clear();
			string authorizationHeader = "Authorization: Passport1.4 OrgVerb=GET,OrgURL=http%3A%2F%2Fmessenger%2Emsn%2Ecom,sign-in="+HttpUtility.UrlEncode(Credentials.Account)+",pwd="+HttpUtility.UrlEncode(Credentials.Password)+"," + twnString;
			request.Headers.Add(authorizationHeader);
			//string headersstr = request.Headers.ToString();

            // auto redirect does not work correctly in this case! (we get HTML pages that way)
			request.AllowAutoRedirect = false;		
			request.PreAuthenticate = false;
			request.Timeout = 60000;

            if (Settings.TraceSwitch.TraceVerbose)
                System.Diagnostics.Trace.WriteLine("Making connection with Passport. URI=" + request.RequestUri, "NS11MessgeHandler");
				

			// now do the transaction
			try
			{
				// get response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                if (Settings.TraceSwitch.TraceVerbose)
                    System.Diagnostics.Trace.WriteLine("Response received from Passport service: "+response.StatusCode.ToString ()+".", "NS11MessgeHandler");

				// check for responses						
				if(response.StatusCode == HttpStatusCode.OK)
				{					
					// 200 OK response
					return response;
				}
				else if(response.StatusCode == HttpStatusCode.Found)
				{
					// 302 Found (this means redirection)
					string newUri = response.Headers.Get("Location");
					response.Close();					

					// call ourselfs again to try a new login					
					return PassportServerLogin(new Uri(newUri), twnString, retries);
				}
				else if(response.StatusCode == HttpStatusCode.Unauthorized)
				{
					// 401 Unauthorized 
                    throw new UnauthorizedException("Failed to login. Response of passport server: " + response.Headers.Get(0));
				}
				else
				{
					throw new MSNPSharpException("Passport server responded with an unknown header");
				}				
			}
			catch(Exception e)
			{
				if (retries < 3)
				{
					return PassportServerLogin(serverUri, twnString, retries+1);
				}
				else
					throw e;
			}
		}		
		#endregion

		#region Helper methods

		/// <summary>
		/// Translates messenger's textual status to the corresponding value of the Status enum.
		/// </summary>
		/// <param name="status">Textual MSN status received from server</param>
		/// <returns>The corresponding enum value</returns>
		protected PresenceStatus ParseStatus(string status)
		{
			switch(status)
			{											
				case "NLN":
					return PresenceStatus.Online; 
				case "BSY":
					return PresenceStatus.Busy; 
				case "IDL":
					return PresenceStatus.Idle; 
				case "BRB":
					return PresenceStatus.BRB; 
				case "AWY":
					return PresenceStatus.Away; 
				case "PHN":
					return PresenceStatus.Phone; 
				case "LUN":
					return PresenceStatus.Lunch; 
				case "FLN":
					return PresenceStatus.Offline; 
				case "HDN":
					return PresenceStatus.Hidden; 
				default: break;
			}

			// unknown status
			return PresenceStatus.Unknown;
		}


		/// <summary>
		/// Translates MSNStatus enumeration to messenger's textual status presentation.
		/// </summary>
		/// <param name="status">MSNStatus enum object representing the status to translate</param>
		/// <returns>The corresponding textual value</returns>
		protected string ParseStatus(PresenceStatus status)
		{
			switch(status)
			{											
				case PresenceStatus.Online:
					return "NLN"; 
				case PresenceStatus.Busy:
					return "BSY";
				case PresenceStatus.Idle:
					return "IDL";
				case PresenceStatus.BRB:
					return "BRB";
				case PresenceStatus.Away:
					return "AWY";
				case PresenceStatus.Phone:
					return "PHN";
				case PresenceStatus.Lunch:
					return "LUN";
				case PresenceStatus.Offline:
					return "FLN";
				case PresenceStatus.Hidden:
					return "HDN";
				default: break;
			}

			// unknown status
			return "Unknown status";
		}

		#endregion

		#region Command handler methods

		#region Login procedure


		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Indicates that the server has approved our version of the protocol. This function will send the CVR command.
		/// <code>VER [Transaction] [Protocol1] ([Protocol2]) [Clientversion]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnVERReceived(NSMessage message)
		{						
			// check for valid credentials
			if(Credentials == null)
				throw new MSNPSharpException("No Credentials passed in the NSMessageHandler");


			// send client information back
			MessageProcessor.SendMessage(new NSMessage("CVR", new string[] { "0x040c", "winnt", "5.1", "i386", "MSNMSGR", "7.0.0777", "msmsgs", Credentials.Account}));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Indicates that the server has approved our client details. This function will send the USR command. 
		/// <code>CVR [Transaction] [Recommended version] [Recommended version] [Minimum version] [Download url] [Info url]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnCVRReceived(NSMessage message)
		{
			MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "TWN", "I", Credentials.Account}));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Indicates that the server has approved our user details and we can authenticate with passport.com.
		/// <code>
		/// USR [Transaction] ['TWN'] ['S'] [Loginvalues] 
		/// or
		/// USR [Transaction] ['OK'] [Account] [Name] [VerifiedPassport] [?]
		/// </code>
		/// When the second parameter is OK this means we are succesfully connected to the messenger network.
		/// In that case, the <see cref="SignedIn"/> event is fired.
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnUSRReceived(NSMessage message)
		{
			if((string)message.CommandValues[1] == "TWN")
			{
				string ticket = AuthenticatePassport((string)message.CommandValues[3]);
			
				// send back the ticket we received
				MessageProcessor.SendMessage(new NSMessage("USR", new string[] { "TWN", "S", ticket}));						
			}
			else if((string)message.CommandValues[1] == "OK")
			{
				// we sucesfully logged in, set the owner's name
				Owner.SetMail(message.CommandValues[2].ToString());
				Owner.SetPassportVerified(message.CommandValues[3].Equals("1"));
				
				if(AutoSynchronize)
					SynchronizeContactList();	
				else
					// notify the client programmer
					OnSignedIn();
			}
		}


		#endregion

		
		/// <summary>
		/// Called when a CHL (challenge) command message has been received.
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OnCHLReceived(NSMessage message)
		{
			if(Credentials == null)
				throw new MSNPSharpException("No credentials available for the NSMSNP11 handler. No challenge answer could be send.");


			//string md = MSNP11.ChallengeBuilder.CreateChallengeResponse (Credentials.ClientCode, message.CommandValues[1].ToString ());
			//string md    = HashMD5(message.CommandValues[1] + Credentials.ClientCode);
			
			MSNP11CHL.clsQRYFactory qryfactory = new MSNP11CHL.clsQRYFactory ();
			string md =  qryfactory.CreateQRY (Credentials.ClientID, Credentials.ClientCode, message.CommandValues[1].ToString ());
			MessageProcessor.SendMessage(new NSMessage("QRY", new string[] { " " + Credentials.ClientID, " 32\r\n", md }));
		}


		/// <summary>
		/// Called when a ILN command has been received.
		/// </summary>
		/// <remarks>
		/// ILN indicates the initial status of a contact.
		/// It is send after initial log on or after adding/removing contact from the contactlist.
		/// Fires the ContactOnline and/or the ContactStatusChange events.
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnILNReceived(NSMessage message)
		{
			// allright
			Contact contact = ContactList.GetContact((string)message.CommandValues[2]);
			contact.SetName((string)message.CommandValues[3]);
			PresenceStatus oldStatus = contact.Status;
			contact.SetStatus(ParseStatus((string)message.CommandValues[1]));
			
			// set the client capabilities, if available
			if(message.CommandValues.Count >= 5)
				contact.ClientCapacities = (ClientCapacities)int.Parse(message.CommandValues[4].ToString());

			// check whether there is a msn object available
			if(message.CommandValues.Count >= 6)
			{
				DisplayImage userDisplay = new DisplayImage();
				userDisplay.Context = message.CommandValues[5].ToString();
				contact.SetUserDisplay(userDisplay);
			}

			if(oldStatus == PresenceStatus.Unknown || oldStatus == PresenceStatus.Offline)
			{
				if(ContactOnline != null)
					ContactOnline(this, new ContactEventArgs(contact));
			}
			if(ContactStatusChanged != null)
				ContactStatusChanged(this, new ContactStatusChangeEventArgs(contact, oldStatus));			
		}


		/// <summary>
		/// Called when a NLN command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a contact on the forward list went online.
		/// <code>NLN [status] [account] [name]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnNLNReceived(NSMessage message)
		{
			Contact contact = ContactList.GetContact((string)message.CommandValues[1]);						
			contact.SetName((string)message.CommandValues[2]);
			PresenceStatus oldStatus = contact.Status;
			contact.SetStatus(ParseStatus((string)message.CommandValues[0]));

			// set the client capabilities, if available
			if(message.CommandValues.Count >= 4)
				contact.ClientCapacities = (ClientCapacities)int.Parse(message.CommandValues[3].ToString());
					
			// the contact changed status, fire event
			if(ContactStatusChanged != null)
				ContactStatusChanged(this, new ContactStatusChangeEventArgs(contact, oldStatus));
			
			// the contact goes online, fire event
			if(ContactOnline != null)
				ContactOnline(this, new ContactEventArgs(contact));
		}

		/// <summary>
		/// Called when a NOT command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a notification message has been received.
		/// <code>NOT [body length]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnNOTReceived(NSMessage message)
		{						
			// build a notification message
			NotificationMessage notification = new NotificationMessage(message);

			if(Settings.TraceSwitch.TraceVerbose)
				Trace.WriteLine("Notification received : " + notification.ToDebugString());

		}

		/// <summary>
		/// Called when an OUT command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has signed off the user.
		/// <code>OUT [Reason]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnOUTReceived(NSMessage message)
		{
			if(message.CommandValues.Count == 2)
			{
				switch(message.CommandValues[1].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture))
				{
					case "OTH": OnSignedOff(SignedOffReason.OtherClient); break;
					case "SSD": OnSignedOff(SignedOffReason.ServerDown); break;
					default:
						OnSignedOff(SignedOffReason.None); break;
				}
			}
			else
				OnSignedOff(SignedOffReason.None);
		}

		/// <summary>
		/// Called when an PRP command has been received.
		/// </summary>
		/// <remarks>
		/// Informs about the phone numbers of the contact list owner.
		/// <code>PRP [TransactionID] [ListVersion] PhoneType Number</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnPRPReceived(NSMessage message)
		{
			string number = "";
			string type   = "";
			if(message.CommandValues.Count >= 4)
			{
				if(message.CommandValues.Count >= 4)
					number = HttpUtility.UrlDecode((string)message.CommandValues[3]);			
				else
					number = "";
				type   = message.CommandValues[2].ToString();
			}
			else
			{
				number = HttpUtility.UrlDecode((string)message.CommandValues[1]);			
				type   = message.CommandValues[0].ToString();
			}
			
			switch(type)
			{
				case "PHH":	Owner.SetHomePhone(number); break;
				case "PHW":	Owner.SetWorkPhone(number); break;
				case "PHM":	Owner.SetMobilePhone(number); break;
				case "MBE":	Owner.SetMobileDevice((number == "Y") ? true : false); break;
				case "MOB":	Owner.SetMobileAccess((number == "Y") ? true : false); break;
				case "MFN": Owner.SetName(HttpUtility.UrlDecode((string)message.CommandValues[1])); break;
			}
		}

		/// <summary>
		/// Called when a CHG command has been received.
		/// </summary>
		/// <remarks>
		/// The notification server has confirmed our request for a status change. 
		/// This function sets the status of the contactlist owner.
		/// <code>CHG [Transaction] [Status]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnCHGReceived(NSMessage message)
		{
			Owner.SetStatus(ParseStatus((string)message.CommandValues[1]));
		}

		
		/// <summary>
		/// Called when a FLN command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a user went offline.
		/// <code>FLN [account]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnFLNReceived(NSMessage message)
		{
			Contact			contact	  = ContactList.GetContact((string)message.CommandValues[0]);
			PresenceStatus	oldStatus = contact.Status;
			contact.SetStatus(PresenceStatus.Offline);								

			// the contact changed status, fire event
			if(ContactStatusChanged != null)
				ContactStatusChanged(this, new ContactStatusChangeEventArgs(contact, oldStatus));

			// the contact goes offline, fire event
			if(ContactOffline != null)
				ContactOffline(this, new ContactEventArgs(contact));
		}

		
		/// <summary>
		/// Called when a LST command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has send either a forward, reverse, blocked or access list.
		/// <code>LST [Contact Guid] [List Bit] [Display Name] [Group IDs]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnLSTReceived(NSMessage message)
		{
			// decrease the number of LST commands following the SYN command we can expect
			syncContactsCount--;
			
			int indexer = 0;

			//contact email					
			string _contact = message.CommandValues[indexer].ToString ();			
			Contact contact = ContactList.GetContact(_contact.Remove (0,2));
			contact.NSMessageHandler = this;
			indexer++;

			// store this contact for the upcoming BPR commands			
			lastContactSynced = contact;
			
			//contact name
			if (message.CommandValues.Count > 3)
			{
				string name = message.CommandValues[indexer].ToString ();
				contact.SetName(name.Remove (0, 2));
								
				indexer++;
			}
			
			if (message.CommandValues.Count > indexer && message.CommandValues[indexer].ToString ().Length > 2)
			{
				string guid = message.CommandValues[indexer].ToString ().Remove (0, 2);
				contact.SetGuid (guid);
				
				indexer++;
			}

			//contact list				
			if (message.CommandValues.Count > indexer)
			{	
				try
				{
					int lstnum = int.Parse(message.CommandValues[indexer].ToString ());
					
					contact.SetLists ((MSNLists) lstnum);				
					indexer++;
					
				} catch (System.FormatException) { }
			}

			if (message.CommandValues.Count > indexer)
			{
				string[] groupids = message.CommandValues[indexer].ToString ().Split(new char[]{','});
				
				foreach(string groupid in groupids)
					if(groupid.Length > 0 && contactGroups[groupid] != null)
						contact.ContactGroups.Add(contactGroups[groupid]); //we add this way so the event doesn't get fired
			}

			// if all LST commands are send in this synchronization cyclus we call the callback
			if(syncContactsCount <= 0)
			{
				// set this so the user can set initial presence
				synSended = true;

				if(AutoSynchronize == true) 
					OnSignedIn();				
				
				// call the event
				if(SynchronizationCompleted != null)
				{					
					SynchronizationCompleted(this, new EventArgs());
				}
			}
		}

		/// <summary>
		/// Gets a new switchboard handler object. Called when a remote client initiated the switchboard.
		/// </summary>
		/// <param name="processor"></param>
		/// <param name="sessionHash"></param>
		/// <param name="sessionId"></param>
		/// <returns></returns>
		protected virtual IMessageHandler CreateSBHandler(IMessageProcessor processor, string sessionHash, int sessionId)
		{
			SBMessageHandler handler = Factory.CreateSwitchboardHandler();
			handler.MessageProcessor = processor;
			handler.NSMessageHandler = this;
			handler.SetInvitation(sessionHash, sessionId);

			return handler;
		}

		
		/// <summary>
		/// Called when a XFR command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the notification server has send us the location of a switch-board server in order to
		/// make contact with a client, or that we must switch to a new notification server.
		/// <code>XFR [Transaction] [SB|NS] [IP:Port] ['0'|'CKI'] [Hash|CurrentIP:CurrentPort]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnXFRReceived(NSMessage message)
		{			
			if((string)message.CommandValues[1] == "NS")
			{
				// switch to a new notification server. That means reconnecting our current message processor.
				SocketMessageProcessor processor = (SocketMessageProcessor)MessageProcessor;
				
				// disconnect first
				processor.Disconnect();

				// set new connectivity settings
				ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
				string[] values = ((string)message.CommandValues[2]).Split(new char[] { ':' }); 
								
				newSettings.Host = values[0];
				newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);

				processor.ConnectivitySettings = newSettings;

				// and reconnect. The login procedure will now start over again
				processor.Connect();				
			}
			if((string)message.CommandValues[1] == "SB")
			{
				if(pendingSwitchboards.Count > 0)
				{
				
					if(Owner.Status == PresenceStatus.Offline)
						System.Diagnostics.Trace.WriteLine("Owner not yet online!", "NS9MessageHandler");

					SBMessageProcessor processor = Factory.CreateSwitchboardProcessor();

					SwitchboardQueueItem queueItem = (SwitchboardQueueItem)pendingSwitchboards.Dequeue();
					
					// set new connectivity settings
					ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
					string[] values = ((string)message.CommandValues[2]).Split(new char[] { ':' }); 
									
					newSettings.Host = values[0];
					newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
					
					if (Settings.TraceSwitch.TraceVerbose)
						System.Diagnostics.Trace.WriteLine("Switchboard connectivity settings: " + newSettings.ToString(), "NS9MessageHandler");
					
					processor.ConnectivitySettings = newSettings;

					// set the switchboard objects with the processor values
					//SBMessageHandler handler = (SBMessageHandler)CreateSBHandler(processor, message.CommandValues[4].ToString());
					string sessionHash = message.CommandValues[4].ToString();
					queueItem.SwitchboardHandler.SetInvitation(sessionHash);
					queueItem.SwitchboardHandler.MessageProcessor = processor;
					queueItem.SwitchboardHandler.NSMessageHandler = this;					

					// register this handler so the switchboard can respond
					processor.RegisterHandler(queueItem.SwitchboardHandler);											

					// notify the client
					OnSBCreated(queueItem.SwitchboardHandler, queueItem.Initiator);
					
					if (Settings.TraceSwitch.TraceVerbose)
						System.Diagnostics.Trace.WriteLine("SB created event handler called", "NS9MessageHandler");

					// start connecting
					processor.Connect();
					
					if (Settings.TraceSwitch.TraceVerbose)
						System.Diagnostics.Trace.WriteLine("Opening switchboard connection..", "NS9MessageHandler");

				}
				else
				{
					if (Settings.TraceSwitch.TraceWarning)
						System.Diagnostics.Trace.WriteLine("Switchboard request received, but no pending switchboards available.", "NS9MessageHandler");			
				}
			}
		}
		
		protected virtual void OnUBXReceived(NSMessage message)
		{
			//check the payload length
			if (message.CommandValues[1].ToString () == "0")
				return;
				
			Contact contact = ContactList.GetContact (message.CommandValues[0].ToString ());
			
			PersonalMessage pm = new PersonalMessage (message);
			contact.SetPersonalMessage (pm);
		}


		/// <summary>
		/// Called when a RNG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the user receives a switchboard session (chatsession) request. A connection to the switchboard will be established
		/// and the corresponding events and objects are created.
		/// <code>RNG [Session] [IP:Port] 'CKI' [Hash] [Account] [Name]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnRNGReceived(NSMessage message)
		{			
			SBMessageProcessor processor = Factory.CreateSwitchboardProcessor();
				
			// set new connectivity settings
			ConnectivitySettings newSettings = new ConnectivitySettings(processor.ConnectivitySettings);
			string[] values = ((string)message.CommandValues[1]).Split(new char[] { ':' }); 
								
			newSettings.Host = values[0];
			newSettings.Port = int.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
			processor.ConnectivitySettings = newSettings;

			// create a switchboard object
			SBMessageHandler handler = (SBMessageHandler)CreateSBHandler(processor, message.CommandValues[3].ToString(), int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture));

			processor.RegisterHandler(handler);

			// start connecting
			processor.Connect();

			// notify the client
			OnSBCreated(handler, null);
		}


		/// <summary>
		/// Called when a BPR command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has send a phone number for a contact. Usually send after a synchronization command.
		/// <code>BPR [Type] [Number]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnBPRReceived(NSMessage message)
		{
			string commandone = (string) message.CommandValues[0];
			
			Contact contact;
			int index;
			
			if (commandone.IndexOf ('@') != -1)
			{
				contact = ContactList.GetContact (commandone);
				index = 2;
			}
			else
			{
				contact = lastContactSynced;
				index = 1;
			}
				
			string number = HttpUtility.UrlDecode((string)message.CommandValues[index]);
			
			if(contact != null)
			{
				switch((string)message.CommandValues[index-1])
				{
					case "PHH": contact.SetHomePhone(number); break;
					case "PHW": contact.SetWorkPhone(number); break;
					case "PHM": contact.SetMobilePhone(number); break;
					case "MOB": contact.SetMobileAccess((number == "Y")); break;
					case "MBE": contact.SetMobileDevice((number == "Y")); break;
					case "HSB": contact.SetHasBlog((number == "1")); break;
				}
			}
			else
				throw new MSNPSharpException("Phone numbers are sent but lastContact == null");
		}


		/// <summary>
		/// Called when a LSG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has send us a contactgroup definition. This adds a new <see cref="ContactGroup"/> object to the ContactGroups colletion.
		/// <code>LSG [Name] [Guid]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnLSGReceived(NSMessage message)
		{
			ContactGroups.AddGroup(				
				new ContactGroup((string)message.CommandValues[0], (string)message.CommandValues[1], this));
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
		protected virtual void OnSYNReceived(NSMessage message)
		{
			int syncNr = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);
			
			syncContactsCount = int.Parse((string)message.CommandValues[3], System.Globalization.CultureInfo.InvariantCulture);

			// check whether there is a new list version or no contacts are on the list
			if(lastSync == syncNr || syncContactsCount == 0)
			{				
				syncContactsCount = 0;
				// set this so the user can set initial presence
				synSended = true;

				if(AutoSynchronize == true) 
					OnSignedIn();

				// no contacts are sent so we are done synchronizing
				// call the callback
				// MSNP8: New callback defined, SynchronizationCompleted.
				if(SynchronizationCompleted != null)
				{					
					SynchronizationCompleted(this, new EventArgs());
					synSended = true;
				}
			}
			else
			{
				lastSync = syncNr;
				lastContactSynced = null;																												
			}		
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
		protected virtual void OnADCReceived(NSMessage message)
		{					if(message.CommandValues.Count == 4)
			{
				Contact c = ContactList.GetContactByGuid (message.CommandValues[2].ToString ().Remove (0, 2));
				//Add contact to group
				string guid = message.CommandValues[3].ToString ();
				
				if (contactGroups [guid] != null)
					c.AddContactToGroup (contactGroups[guid]);
					
				return;
			}

			MSNLists Type = GetMSNList((string)message.CommandValues[1]);
			Contact contact = ContactList.GetContact(message.CommandValues[2].ToString ().Remove (0, 2));
			
			contact.NSMessageHandler = this;

			if(message.CommandValues.Count >= 4)
				contact.SetName (message.CommandValues[3].ToString ().Remove (0,2));

			if(message.CommandValues.Count >= 5)
				contact.SetGuid (message.CommandValues[4].ToString ().Remove (0,2));			
			// add the list to this contact						
			contact.AddToList(Type);

			// check whether another user added us .The client programmer can then act on it.
			
			// only fire the event if >=4 because name is just sent when other user add you to her/his contact list
			// msnp11 allows you to add someone to the reverse list.
			if(Type == MSNLists.ReverseList && ReverseAdded != null && message.CommandValues.Count >= 4)
			{
				ReverseAdded(this, new ContactEventArgs(contact));
			}

			// send a list mutation event
			if(ContactAdded != null)
				ContactAdded(this, new ListMutateEventArgs(contact, Type));
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
		protected virtual void OnREMReceived(NSMessage message)
		{
			MSNLists list = GetMSNList((string)message.CommandValues[1]);
			
			Contact contact = null;
			if (list == MSNLists.ForwardList)			
				contact = ContactList.GetContactByGuid((string)message.CommandValues[2]);
			else
				contact = ContactList.GetContact((string)message.CommandValues[2]);
				
			if (contact == null)
				return;
						
			if (message.CommandValues.Count == 4)
			{
				//Remove from group
				string group = message.CommandValues[3].ToString ();
				
				if(contactGroups[group] != null)
					contact.RemoveContactFromGroup (contactGroups[group]);
					
				return;
			}
			
			//remove the contact from list
			contact.RemoveFromList (list);
								
			// check whether another user removed us
			if(list == MSNLists.ReverseList && ReverseRemoved != null)
				ReverseRemoved(this, new ContactEventArgs(contact));

			if(ContactRemoved != null)
				ContactRemoved(this, new ListMutateEventArgs(contact, list));
		}


		/// <summary>
		/// Called when a BLP command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has send the privacy mode for the contact list owner.
		/// <code>BLP [Transaction] [SynchronizationID] [PrivacyMode]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnBLPReceived(NSMessage message)
		{
			if(Owner == null) return;
		
			string type = "";
			if(message.CommandValues.Count == 1)
				type = message.CommandValues[0].ToString();
			else
				type = message.CommandValues[2].ToString();
			switch(type)
			{
				case "AL":
					Owner.SetPrivacy(PrivacyMode.AllExceptBlocked);
					break;
				case "BL":
					owner.SetPrivacy(PrivacyMode.NoneButAllowed);
					break;
			}
		}
		

		/// <summary>
		/// Called when a GTC command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has send us the privacy notify setting for the contact list owner.
		/// <code>GTC [Transaction] [SynchronizationID] [NotifyPrivacy]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnGTCReceived(NSMessage message)
		{		
			if(Owner == null) return;

			switch((string)message.CommandValues[0])
			{
				case "A":
					owner.SetNotifyPrivacy(NotifyPrivacy.PromptOnAdd);
					break;
				case "N":
					owner.SetNotifyPrivacy(NotifyPrivacy.AutomaticAdd);
					break;
			}
		}
		

		/// <summary>
		/// Called when an ADG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a contact group has been added to the contact group list.
		/// Raises the ContactGroupAdded event.
		/// <code>ADG [Transaction] [ListVersion] [Name] [GroepID] </code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnADGReceived(NSMessage message)
		{
			string guid = message.CommandValues[2].ToString ();
			
			// add a new group									
			ContactGroups.AddGroup(new ContactGroup(System.Web.HttpUtility.UrlDecode((string)message.CommandValues[1]), guid, this));
			
			// fire the event
			if(ContactGroupAdded != null)
			{
				ContactGroupAdded(this, new ContactGroupEventArgs((ContactGroup)ContactGroups[guid]));
			}						
		}


		/// <summary>
		/// Called when a RMG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a contact group has been removed.
		/// Raises the ContactGroupRemoved event.
		/// <code>RMG [Transaction] [ListVersion] [GroupID]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnRMGReceived(NSMessage message)
		{
			string guid = message.CommandValues[1].ToString ();
			
			ContactGroup contactGroup = (ContactGroup)contactGroups[guid];
			ContactGroups.RemoveGroup(contactGroup);									

			if(ContactGroupRemoved != null)
			{
				ContactGroupRemoved(this, new ContactGroupEventArgs(contactGroup));
			}			
		}
		

		/// <summary>
		/// Called when a MSG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the notification server has send us a MSG. This is usually a MSG from the 'HOTMAIL' user (account is also 'HOTMAIL') which includes notifications about the contact list owner's profile, new mail, number of unread mails in the inbox, etc.
		/// <code>MSG [Account] [Name] [Length]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnMSGReceived(MSNMessage message)
		{
			MSGMessage msgMessage = new MSGMessage(message);

			Regex  ContentTypeRE = new Regex("Content-Type:\\s+(?<ContentType>[\\w/\\-0-9]+)", RegexOptions.Multiline | RegexOptions.Compiled);
			Match  match;
			if((match = ContentTypeRE.Match((string)msgMessage.MimeHeader["Content-Type"])).Success)
			{
				switch(match.Groups["ContentType"].ToString())
				{
					case "text/x-msmsgsprofile" :					OnProfileReceived(msgMessage); break;
					case "text/x-msmsgsemailnotification":
					case "application/x-msmsgsemailnotification":	OnMailNotificationReceived(msgMessage); break;
					case "text/x-msmsgsactivemailnotification":		OnMailChanged(msgMessage);		break;
					case "text/x-msmsgsinitialemailnotification":					
					case "application/x-msmsgsinitialemailnotification":
																	OnMailboxStatusReceived(msgMessage); break;
				}
			}

		}

	
		/// <summary>
		/// Called when the owner has removed or moved e-mail.
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OnMailChanged(MSGMessage message)
		{
			// dispatch the event
			if(MailboxChanged != null)
			{
				string sourceFolder = (string)message.MimeHeader["Src-Folder"];
				string destFolder	= (string)message.MimeHeader["Dest-Folder"];
				int	   count		= int.Parse((string)message.MimeHeader["Message-Delta"], System.Globalization.CultureInfo.InvariantCulture);
				MailboxChanged(this, new MailChangedEventArgs(sourceFolder, destFolder, count));
			}
		}

		/// <summary>
		/// Called when the server sends the status of the owner's mailbox.
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OnMailboxStatusReceived(MSGMessage message)
		{
			// dispatch the event
			if(MailboxStatusReceived != null)
			{
				int inboxUnread   = int.Parse((string)message.MimeHeader["Inbox-Unread"], System.Globalization.CultureInfo.InvariantCulture);
				int foldersUnread = int.Parse((string)message.MimeHeader["Folders-Unread"], System.Globalization.CultureInfo.InvariantCulture);
				string inboxURL   = (string)message.MimeHeader["Inbox-URL"];
				string folderURL  = (string)message.MimeHeader["Folders-URL"];
				string postURL    = (string)message.MimeHeader["Post-URL"];
				MailboxStatusReceived(this, new MailboxStatusEventArgs(inboxUnread, foldersUnread, new Uri(inboxURL), new Uri(folderURL), new Uri(postURL)));
			}
		}

		/// <summary>
		/// Called when the owner has received new e-mail, or e-mail has been removed / moved. Fires the <see cref="NewMailReceived"/> event.
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OnMailNotificationReceived(MSGMessage message)
		{			
			// dispatch the event
			if(NewMailReceived != null)
			{
				string from			= (string)message.MimeHeader["From"];
				string messageUrl	= (string)message.MimeHeader["Message-URL"];
				string postUrl		= (string)message.MimeHeader["Post-URL"];
				string subject		= (string)message.MimeHeader["Subject"];
				string destFolder   = (string)message.MimeHeader["Dest-Folder"];
				string fromMail     = (string)message.MimeHeader["From-Addr"];
				int    id			= int.Parse((string)message.MimeHeader["id"], System.Globalization.CultureInfo.InvariantCulture);
				NewMailReceived(this, new NewMailEventArgs(from, new Uri(postUrl), new Uri(messageUrl), subject, destFolder, fromMail, id));
			}						
		}

		/// <summary>
		/// Called when the server has send a profile description. This will update the profile of the Owner object. 
		/// </summary>
		/// <param name="message"></param>
		protected virtual void OnProfileReceived(MSGMessage message)
		{
			int clientPort = int.Parse(message.MimeHeader["ClientPort"].ToString().Replace('.', ' '), System.Globalization.CultureInfo.InvariantCulture);
			clientPort = ((clientPort & 255) * 256) + ((clientPort & 65280) / 256);			

			Owner.UpdateProfile(
				(string)message.MimeHeader["LoginTime"],
				bool.Parse((string)message.MimeHeader["EmailEnabled"]),
				(string)message.MimeHeader["MemberIdHigh"],
				(string)message.MimeHeader["MemberIdLow"],
				(string)message.MimeHeader["lang_preference"],
				(string)message.MimeHeader["preferredEmail"],
				(string)message.MimeHeader["country"],
				(string)message.MimeHeader["PostalCode"],
				(string)message.MimeHeader["Gender"],
				(string)message.MimeHeader["Kid"],
				(string)message.MimeHeader["Age"],				
				(string)message.MimeHeader["Birthday"],
				(string)message.MimeHeader["Wallet"],
				(string)message.MimeHeader["sid"],
				(string)message.MimeHeader["kv"],
				(string)message.MimeHeader["MSPAuth"],
				IPAddress.Parse((string)message.MimeHeader["ClientIP"]),
				clientPort);

			// set the external end point. This can be used in file transfer connectivity determing
			externalEndPoint = new IPEndPoint(IPAddress.Parse((string)message.MimeHeader["ClientIP"]), clientPort);
		}

		/// <summary>
		/// Called when a QNG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates a ping answer. The number of seconds indicates the timespan in which another ping must be send.
		/// <code>QNG [Seconds]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnQNGReceived(NSMessage message)
		{			
			if(PingAnswer != null)
			{
				// get the number of seconds till the next ping and fire the event
				// with the correct parameters.
				int seconds = int.Parse((string)message.CommandValues[0], System.Globalization.CultureInfo.InvariantCulture);
				PingAnswer(this, new PingAnswerEventArgs(seconds));
			}
		}
						
		#endregion

		#endregion

		#endregion
	}
}
