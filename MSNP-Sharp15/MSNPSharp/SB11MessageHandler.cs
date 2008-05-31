#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the C
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUSDING, BUT NOT LIMITED TO, THE 
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
using System.Collections.Generic;
using System.Text;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	#region Event argument classes
	/// <summary>
	/// Used when a new switchboard session is affected.
	/// </summary>
	public class SBEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private SBMessageHandler switchboard;

		/// <summary>
		/// The affected switchboard
		/// </summary>
		public SBMessageHandler Switchboard
		{
			get { return switchboard; }
			set { switchboard = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SBEventArgs(SBMessageHandler switchboard)
		{
			this.switchboard = switchboard;
		}
	}

	/// <summary>
	/// Used when a new switchboard session is created.
	/// </summary>
	public class SBCreatedEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private SBMessageHandler switchboard;

		/// <summary>
		/// The affected switchboard
		/// </summary>
		public SBMessageHandler Switchboard
		{
			get { return switchboard; }
			set { switchboard = value;}
		}

		/// <summary>
		/// </summary>
		private object	initiator;

		/// <summary>
		/// The object that requested the switchboard. Null if the switchboard session was initiated by a remote client.
		/// </summary>
		public object Initiator
		{
			get { return initiator; }
			set { initiator = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SBCreatedEventArgs(SBMessageHandler switchboard, object initiator)
		{
			this.switchboard = switchboard;
			this.initiator   = initiator;
		}
	}
	#endregion
	
	#region Delegates
	
	/// <summary>		
	/// Used when a switchboard is affected. 
	/// </summary>
	public delegate void SBChangedEventHandler(object sender, EventArgs e);			
			
	/// <summary>		
	/// Used when a text message is received from a remote contact. 
	/// </summary>
	public delegate void TextMessageReceivedEventHandler(object sender, TextMessageEventArgs e);

	/// <summary>		
	/// Used when a emoticon definition is received from a remote contact. 
	/// </summary>
	public delegate void EmoticonDefinitionReceivedEventHandler(object sender, EmoticonDefinitionEventArgs e);
	
	public delegate void WinkReceivedEventHandler (object sender, WinkEventArgs args);

	/// <summary>
	/// Used when a remote contact begins typing. 		
	/// </summary>
	public delegate void UserTypingEventHandler(object sender, ContactEventArgs e);


	#endregion

	/// <summary>
	/// Handles the messages from the switchboard server.
	/// </summary>
	public class SBMessageHandler : IMessageHandler
	{
		#region Private
		/// <summary>
		/// </summary>
		private	SocketMessageProcessor	messageProcessor = null;

		/// <summary>
		/// </summary>
		private NSMessageHandler		nsMessageHandler = null;

		/// <summary>
		/// </summary>
		protected bool					invited			 = false;

		/// <summary>
		/// </summary>
		private	EventHandler			processorConnectedHandler = null;

		/// <summary>
		/// </summary>
		private	EventHandler			processorDisconnectedHandler = null;

		/// <summary>
		/// </summary>
		private int						sessionId;

		/// <summary>
		/// </summary>
		protected int					SessionId
		{
			get { return sessionId; }
			set { sessionId = value;}
		}

		/// <summary>
		/// </summary>
		private string				sessionHash	    = "";
		/// <summary>
		/// The hash identifier used to define this switchboard session.
		/// </summary>
		internal string			SessionHash
		{
			get { return sessionHash; }
			set { sessionHash = value;}
		}

		/// <summary>
		/// </summary>
		protected bool				sessionEstablished = false;

		/// <summary>
		/// </summary>
		private Hashtable			contacts		= new Hashtable();

		/// <summary>
		/// Holds track of the invitations we have yet to issue
		/// </summary>
		private Queue					invitationQueue = new Queue();

		/// <summary>
		/// Supports the p2p framework
		/// </summary>
		private P2PHandler				p2pHandler = null;

		private Hashtable multiPacketMessages = new Hashtable();
		#endregion
		
		#region Public Events

		/// <summary>
		/// Fired when the owner is the only contact left. If the owner leaves too the connection is automatically closed by the server.
		/// </summary>
		public event SBChangedEventHandler			AllContactsLeft;

		/// <summary>
		/// Fired when the session is closed, either by the server or by the local client.
		/// </summary>
		public event SBChangedEventHandler			SessionClosed;

		/// <summary>
		/// Occurs when a switchboard connection has been made and the initial handshaking commands are send. This indicates that the session is ready to invite or accept other contacts.
		/// </summary>
		public event SBChangedEventHandler			SessionEstablished;

		/// <summary>
		/// Fired when a contact joins. In case of a conversation with two people in it this event is called with the remote contact specified in the event argument.
		/// </summary>
		public event ContactChangedEventHandler		ContactJoined;
		/// <summary>
		/// Fired when a contact leaves the conversation.
		/// </summary>
		public event ContactChangedEventHandler		ContactLeft;

		/// <summary>
		/// Fired when a message is received from any of the other contacts in the conversation.
		/// </summary>
		public event TextMessageReceivedEventHandler			TextMessageReceived;

		/// <summary>
		/// Fired when a contact sends a emoticon definition.
		/// </summary>
		public event EmoticonDefinitionReceivedEventHandler		EmoticonDefinitionReceived;
		
		public event WinkReceivedEventHandler WinkReceived;

		/// <summary>
		/// Fired when a contact sends a nudge
		/// </summary>
		public event ContactChangedEventHandler	NudgeReceived;

		/// <summary>
		/// Fired when any of the other contacts is typing a message.
		/// </summary>
		public event UserTypingEventHandler			UserTyping;

		/// <summary>
		/// Occurs when an exception is thrown while handling the incoming or outgoing messages.
		/// </summary>
		public event HandlerExceptionEventHandler	ExceptionOccurred;
		
		/// <summary>
		/// Occurs when the MSN Switchboard Server sends us an error.
		/// </summary>
		public event ErrorReceivedEventHandler		ServerErrorReceived;


		/// <summary>
		/// Fires the SessionClosed event.
		/// </summary>
		protected virtual void OnSessionClosed()
		{
			if(SessionClosed != null)
				SessionClosed(this, new EventArgs());
		}

		/// <summary>
		/// Fires the ServerErrorReceived event.
		/// </summary>
		protected virtual void OnServerErrorReceived(MSNError serverError)
		{
			if(ServerErrorReceived != null)
				ServerErrorReceived(this, new MSNErrorEventArgs(serverError));
		}

		/// <summary>
		/// Fires the SessionEstablished event and processes invitations in the queue.
		/// </summary>
		protected virtual void OnSessionEstablished()
		{
			sessionEstablished = true;
			if(SessionEstablished != null)
				SessionEstablished(this, new EventArgs());
			
			// process ant remaining invitations
			ProcessInvitations();			
		}

		/// <summary>
		/// Fires the <see cref="ExceptionOccurred"/> event.
		/// </summary>
		/// <param name="e">The exception which was thrown</param>
		protected virtual void OnExceptionOccurred(Exception e)
		{
			if(ExceptionOccurred != null)
				ExceptionOccurred(this, new ExceptionEventArgs(e));			
		}

		/// <summary>
		/// Fires the <see cref="ContactJoined"/> event.
		/// </summary>
		/// <param name="contact">The contact who joined the session.</param>
		protected virtual void OnContactJoined(Contact contact)
		{
			if(ContactJoined != null)
			{	
				ContactJoined(this, new ContactEventArgs(contact));
			}
		}

		/// <summary>
		/// Fires the <see cref="ContactLeft"/> event.
		/// </summary>
		/// <param name="contact">The contact who left the session.</param>
		protected virtual void OnContactLeft(Contact contact)
		{
			if(ContactLeft != null)
			{	
				ContactLeft(this, new ContactEventArgs(contact));
			}
		}

		
		/// <summary>
		/// Fires the <see cref="AllContactsLeft"/> event.
		/// </summary>		
		protected virtual void OnAllContactsLeft()
		{
			if(AllContactsLeft != null)
			{	
				AllContactsLeft(this, new EventArgs());
			}
		}

		/// <summary>
		/// Fires the <see cref="UserTyping"/> event.
		/// </summary>
		/// <param name="contact">The contact who is typing.</param>
		protected virtual void OnUserTyping(Contact contact)
		{
			// make sure we don't parse the rest of the message in the next loop											
			if(UserTyping != null)
			{
				// raise the event
				UserTyping(this, new ContactEventArgs(contact));
			}
		}

		/// <summary>
		/// Fires the <see cref="UserTyping"/> event.
		/// </summary>
		/// <param name="message">The emoticon message.</param>
		/// <param name="contact">The contact who is sending the definition.</param>
		protected virtual void OnEmoticonDefinition(MSGMessage message, Contact contact)
		{		
			EmoticonMessage emoticonMessage = new EmoticonMessage();
			emoticonMessage.CreateFromMessage(message);
			
			if(EmoticonDefinitionReceived != null)
			{
				foreach(Emoticon emoticon in emoticonMessage.Emoticons)
				{
					EmoticonDefinitionReceived(this, new EmoticonDefinitionEventArgs(contact, emoticon));
				}
			}
		}
		
		protected virtual void OnWinkReceived (MSGMessage message, Contact contact)
		{
			string body = System.Text.Encoding.UTF8.GetString(message.InnerBody);
			
			Wink obj = new Wink ();
			obj.ParseContext (body, false);
			
			if (WinkReceived != null)
				WinkReceived (this, new WinkEventArgs (contact, obj));
		}
		
		/// <summary>
		/// Fires the <see cref="NudgeReceived"/> event.
		/// </summary>
		/// <param name="contact">The contact who is sending the nudge.</param>
		protected virtual void OnNudgeReceived (Contact contact)
		{
			if (NudgeReceived != null)
			{
				NudgeReceived (this, new ContactEventArgs (contact));
			}
		}

		/// <summary>
		/// Fires the <see cref="TextMessageReceived"/> event.
		/// </summary>
		/// <param name="message">The message send.</param>
		/// <param name="contact">The contact who sended the message.</param>
		protected virtual void OnTextMessageReceived(TextMessage message, Contact contact)
		{			
			if(TextMessageReceived != null)
				TextMessageReceived(this, new TextMessageEventArgs(message, contact));
		}							

		#endregion

		#region Public

		/// <summary>
		/// </summary>
		protected SBMessageHandler()
		{
		}

		/// <summary>
		/// Called when a switchboard session is created on request of a remote client.
		/// </summary>
		/// <param name="sessionHash"></param>
		/// <param name="sessionId"></param>
		public void SetInvitation(string sessionHash, int sessionId)
		{
			this.sessionId		  = sessionId;
			this.sessionHash      = sessionHash;
			this.invited		  = true;
		}

		/// <summary>
		/// Called when a switchboard session is created on request of a local client.
		/// </summary>
		/// <param name="sessionHash"></param>
		public void SetInvitation(string sessionHash)
		{			
			this.sessionHash      = sessionHash;
			this.invited		  = false;
		}

		/// <summary>
		/// The nameserver that received the request for the switchboard session
		/// </summary>
		public NSMessageHandler NSMessageHandler
		{
			get { return nsMessageHandler; }
			set { nsMessageHandler = value;}
		}

		/// <summary>
		/// Implements the P2P framework. This object is automatically created when a succesfull connection was made to the switchboard.
		/// </summary>
		public P2PHandler P2PHandler
		{
			get { return p2pHandler; }
			set { p2pHandler = value;}
		}

		/// <summary>
		/// Indicates if the local client was invited to the session
		/// </summary>
		public	bool Invited
		{
			get { return invited; }			
		}				

		/// <summary>
		/// A collection of all <i>remote</i> contacts present in this session
		/// </summary>
		public Hashtable Contacts
		{
			get { return contacts; }
		}

		/// <summary>
		/// Indicates if the session is ready to send/accept commands. E.g. the initial handshaking and identification has been completed.
		/// </summary>
		public bool IsSessionEstablished
		{
			get { return sessionEstablished; }
		}

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
				if(processorConnectedHandler == null)
				{ 					
                    // store the new eventhandlers in order to remove them in the future
					processorConnectedHandler	= new EventHandler(SB11MessageHandler_ProcessorConnectCallback);
					processorDisconnectedHandler= new EventHandler(SB11MessageHandler_ProcessorDisconnectCallback);
				}

                // catch the connect event so we can start sending the USR command upon initiating
                ((SocketMessageProcessor)value).ConnectionEstablished += processorConnectedHandler;
                ((SocketMessageProcessor)value).ConnectionClosed += processorDisconnectedHandler;

                if (messageProcessor != null)
                {
                    // de-register from the previous message processor					
                    ((SocketMessageProcessor)messageProcessor).ConnectionEstablished -= processorConnectedHandler;
                    ((SocketMessageProcessor)messageProcessor).ConnectionClosed -= processorDisconnectedHandler;
                }
				
				messageProcessor = (SocketMessageProcessor)value;

			}
		}

		/// <summary>
		/// Closes the switchboard session by disconnecting from the server. 
		/// </summary>
		public virtual void Close()
		{
			if(MessageProcessor != null)
				((SocketMessageProcessor)MessageProcessor).Disconnect();
		}

        /// <summary>
        /// Debug string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SessionHash;
        }

		#endregion

		/// <summary>
		/// Called when the message processor has established a connection. This function will 
		/// begin the login procedure by sending the VER command.
		/// </summary>
		protected virtual void OnProcessorConnectCallback(IMessageProcessor processor)
		{			
			SendInitialMessage();			
		}

		/// <summary>
		/// Called when the message processor has disconnected. This function will 
		/// set the IsSessionEstablished to false.
		/// </summary>
		protected virtual void OnProcessorDisconnectCallback(IMessageProcessor processor)
		{
			sessionEstablished = false;
			OnSessionClosed();
		}

		/// <summary>
		/// Calls OnProcessorConnectCallback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SB11MessageHandler_ProcessorConnectCallback(object sender, EventArgs e)
		{			
            if (Settings.TraceSwitch.TraceVerbose)
                System.Diagnostics.Trace.WriteLine("Switchboard processor connected.", "SBMessageHandler");

			OnProcessorConnectCallback((IMessageProcessor)sender);
		}

		/// <summary>
		/// Calls OnProcessorDisconnectCallback.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SB11MessageHandler_ProcessorDisconnectCallback(object sender, EventArgs e)
		{
			OnProcessorDisconnectCallback((IMessageProcessor)sender);
		}
		
		#region Protected helper methods
			
		/// <summary>
		/// Handles all remaining invitations. If no connection is yet established it will do nothing.
		/// </summary>
		protected virtual void ProcessInvitations()
		{
            if (Settings.TraceSwitch.TraceVerbose)
                System.Diagnostics.Trace.WriteLine("Processing invitations for switchboard ..", "SBMessageHandler");

			if(IsSessionEstablished)
			{
				while(invitationQueue.Count > 0)
					SendInvitationCommand((string)invitationQueue.Dequeue());
                if (Settings.TraceSwitch.TraceVerbose)
                    System.Diagnostics.Trace.WriteLine("Invitation send", "SBMessageHandler");
			}
		}

		/// <summary>
		/// Sends the invitation command to the switchboard server.
		/// </summary>
		/// <param name="contact"></param>
		protected virtual void SendInvitationCommand(string contact)
		{
			MessageProcessor.SendMessage(new SBMessage("CAL", new string[] { contact } ));
		}
		
		#endregion



		#region Message sending methods

		/// <summary>
		/// Invites the specified contact to the switchboard.
		/// </summary>
		/// <remarks>
		/// If there is not yet a connection established the invitation will be stored in a queue and processed as soon as a connection is established.
		/// </remarks>
		/// <param name="contact">The contact's account to invite.</param>
		public virtual void	Invite(string contact)
		{
			if (Contacts.Contains (contact))
				return;
			
			invitationQueue.Enqueue(contact);
			ProcessInvitations();
		}

		/// <summary>
		/// Sends a plain text message to all other contacts in the conversation.
		/// </summary>
		/// <remarks>
		/// This method wraps the TextMessage object in a SBMessage object and sends it over the network.
		/// </remarks>
		/// <param name="message">The message to send.</param>
		public virtual void SendTextMessage(TextMessage message)
		{						
			//First, we have to check whether the content of the message is not to big for one message
			//There's a maximum of 1600 bytes per message, let's play safe and take 1400 bytes
			UTF8Encoding encoding = new UTF8Encoding();
			
			if (encoding.GetByteCount(message.Text) > 1400)
			{
				//we'll have to use multi-packets messages
				Guid guid = Guid.NewGuid();
				byte[] text = encoding.GetBytes(message.Text);
				int chunks = Convert.ToInt32(Math.Ceiling((double)text.GetUpperBound(0) / (double)1400));
				for (int i = 0; i < chunks; i++)
				{
					SBMessage sbMessage = new SBMessage();
					MSGMessage msgMessage = new MSGMessage();
					
					//Clone the message
					TextMessage chunkMessage = (TextMessage)message.Clone();
 
					//Get the part of the message that we are going to send
					if (text.GetUpperBound(0) - (i * 1400) > 1400)
						chunkMessage.Text = encoding.GetString(text, i * 1400, 1400);
					else
						chunkMessage.Text = encoding.GetString(text, i * 1400, text.GetUpperBound(0) - (i * 1400));

					//Add the correct headers
					msgMessage.MimeHeader.Add("Message-ID", "{" + guid.ToString() + "}");
					
					if (i == 0)
						msgMessage.MimeHeader.Add("Chunks", Convert.ToString (chunks));
					else
						msgMessage.MimeHeader.Add("Chunk", Convert.ToString (i));

					sbMessage.InnerMessage = msgMessage;

					msgMessage.InnerMessage = chunkMessage;

					//send it over the network
					MessageProcessor.SendMessage(sbMessage);
				}
			}
			else
			{
				SBMessage sbMessage = new SBMessage();
				MSGMessage msgMessage = new MSGMessage();

				sbMessage.InnerMessage = msgMessage;

				msgMessage.InnerMessage = message;

				// send it over the network
				MessageProcessor.SendMessage(sbMessage);
			}
		}

		/// <summary>
		/// Sends the definition for a list of emoticons to all other contacts in the conversation. The client-programmer must use this function if a text messages uses multiple emoticons in a single message.
		/// </summary>
		/// <remarks>Use this function before sending text messages which include the emoticon text. You can only send one emoticon message before the textmessage. So make sure that all emoticons used in the textmessage are included.</remarks>
		/// <param name="emoticons">A list of emoticon objects.</param>
		public virtual void SendEmoticonDefinitions(ArrayList emoticons)
		{
			SBMessage sbMessage = new SBMessage();
			MSGMessage msgMessage = new MSGMessage();
			EmoticonMessage emoticonMessage = new EmoticonMessage(emoticons);
			
			msgMessage.InnerMessage = emoticonMessage;
			sbMessage.InnerMessage = msgMessage;			
			
			// send it over the network
			MessageProcessor.SendMessage(sbMessage);
		}

		/// <summary>
		/// Sends a 'user is typing..' message to the switchboard, and is received by all participants.
		/// </summary>
		public virtual void SendTypingMessage()
		{
			SBMessage sbMessage = new SBMessage();
			sbMessage.Acknowledgement = "U";
			
			MSGMessage msgMessage = new MSGMessage();
			msgMessage.MimeHeader["Content-Type"] = "text/x-msmsgscontrol";
			msgMessage.MimeHeader["TypingUser"] = NSMessageHandler.Owner.Mail;
			
			sbMessage.InnerMessage = msgMessage;			
			
			// send it over the network
			MessageProcessor.SendMessage(sbMessage);
		}

		/// <summary>
		/// Sends a 'nudge' message to the switchboard, and is received by all participants.
		/// </summary>
		public virtual void SendNudge()
		{
			SBMessage sbMessage = new SBMessage();
			
			MSGMessage msgMessage = new MSGMessage();
			msgMessage.MimeHeader["Content-Type"] = "text/x-msnmsgr-datacast\r\n\r\nID: 1\r\n\r\n\r\n";
			sbMessage.InnerMessage = msgMessage;			
			
			// send it over the network
			MessageProcessor.SendMessage(sbMessage);
		}

		/// <summary>
		/// Send the first message to the server.
		/// </summary>
		/// <remarks>
		/// Depending on the <see cref="Invited"/> field a ANS command (if true), or a USR command (if false) is send.
		/// </remarks>
		protected virtual void SendInitialMessage()
		{
			if(Invited)			
				MessageProcessor.SendMessage(new SBMessage("ANS", new string[] { NSMessageHandler.Owner.Mail, SessionHash, SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture) } ));
			else
				MessageProcessor.SendMessage(new SBMessage("USR", new string[] { NSMessageHandler.Owner.Mail, SessionHash } ));
		}

		#endregion

		

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
		/// <param name="sender"></param>
		/// <param name="message">The network message received from the notification server</param>
		public virtual void HandleMessage(IMessageProcessor sender, NetworkMessage message)
		{
			try
			{
				// we expect at least a SBMessage object
				SBMessage sbMessage = (SBMessage)message;
				
				switch(sbMessage.Command)
				{														
					case "ANS": { OnANSReceived(sbMessage); break; }
					case "BYE": { OnBYEReceived(sbMessage); break; }
					case "CAL": { OnCALReceived(sbMessage); break; }
					case "IRO": { OnIROReceived(sbMessage); break; }
					case "JOI": { OnJOIReceived(sbMessage); break; }
					case "MSG": { OnMSGReceived(sbMessage); break; }
					case "USR": { OnUSRReceived(sbMessage); break; }					
					
					default:
					{
						// first check whether it is a numeric error command
						if(sbMessage.Command[0] >= '0' && sbMessage.Command[0] <= '9')
						{
							try
							{
								int errorCode = int.Parse(sbMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
								OnServerErrorReceived((MSNError)errorCode);														
							}
							catch(FormatException e)
							{
								throw new MSNPSharpException("Exception occurred while parsing an error code received from the switchboard server", e);
							}
						}
						
						// if not then it is a unknown command:
						// do nothing.
						break;
					}			
				}
			}
			catch(Exception e)
			{				
				// notify the client of this exception
				OnExceptionOccurred(e);				
				throw e;
			}
		}

        internal virtual void CopyAndClearEventHandler(SBMessageHandler HandlerTo)
        {
            HandlerTo.AllContactsLeft               += AllContactsLeft;
            HandlerTo.ContactLeft                   += ContactLeft;
            HandlerTo.ContactJoined                 += ContactJoined;
            HandlerTo.EmoticonDefinitionReceived    += EmoticonDefinitionReceived;
            HandlerTo.ExceptionOccurred             += ExceptionOccurred;
            HandlerTo.NudgeReceived                 += NudgeReceived;
            HandlerTo.ServerErrorReceived           += ServerErrorReceived;
            HandlerTo.SessionClosed                 += SessionClosed;
            HandlerTo.SessionEstablished            += SessionEstablished;
            HandlerTo.TextMessageReceived           += TextMessageReceived;
            HandlerTo.UserTyping                    += UserTyping;
            HandlerTo.WinkReceived                  += WinkReceived;

            AllContactsLeft                         = null;
            ContactLeft                             = null;
            ContactJoined                           = null;
            EmoticonDefinitionReceived              = null;
            ExceptionOccurred                       = null;
            NudgeReceived                           = null;
            ServerErrorReceived                     = null;
            SessionClosed                           = null;
            SessionEstablished                      = null;
            TextMessageReceived                     = null;
            UserTyping                              = null;
            WinkReceived                            = null;
        }

		#region Message handlers

		/// <summary>
		/// Called when a ANS command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has replied to our identification ANS command.
		/// <code>ANS [Transaction] ['OK']</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnANSReceived(SBMessage message)
		{
			if(message.CommandValues[1].ToString() == "OK")				
			{												
				// we are now ready to invite other contacts. Notify the client of this.
				OnSessionEstablished();				
			}
		}

		/// <summary>
		/// Called when a USR command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has replied to our identification USR command.
		/// <code>USR [Transaction] ['OK'] [account] [name]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnUSRReceived(SBMessage message)
		{
			if(message.CommandValues[1].ToString() == "OK" 
			&& NSMessageHandler.Owner.Mail.ToLower(System.Globalization.CultureInfo.InvariantCulture) == message.CommandValues[2].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture))
			{				
				// update the owner's name. Just to be sure.
				//NSMessageHandler.Owner.SetName(message.CommandValues[3].ToString());

				// we are now ready to invite other contacts. Notify the client of this.
				OnSessionEstablished();
			}
		}
		

		/// <summary>
		/// Called when a CAL command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that the server has replied to our request to invite a contact.
		/// <code>CAL [Transaction] ['RINGING'] [sessionId]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnCALReceived(SBMessage message)
		{
			// this is not very interesting so do nothing at the moment
		}

		/// <summary>
		/// Called when a JOI command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a remote contact has joined the session.
		/// This will fire the <see cref="ContactJoined"/> event.
		/// <code>JOI [account] [name]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnJOIReceived(SBMessage message)
		{
			// get the contact and update it's name
			Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString());
			//contact.SetName(message.CommandValues[1].ToString());

			
			if(Contacts.Contains(contact.Mail) == false)
				Contacts.Add(contact.Mail, contact);

			// notify the client programmer
			OnContactJoined(contact);						

		}

		/// <summary>
		/// Called when a BYE command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a remote contact has leaved the session.
		/// This will fire the <see cref="ContactLeft"/> event. Or, if all contacts have left, the <see cref="AllContactsLeft"/> event.
		/// <code>BYE [account]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnBYEReceived(SBMessage message)
		{
			if(Contacts.ContainsKey(message.CommandValues[0].ToString()))
			{
				// get the contact and update it's name
				Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString());
				Contacts.Remove(contact.Mail);

				// notify the client programmer
				OnContactLeft(contact);

				if(Contacts.Count == 0)
				{
					OnAllContactsLeft();
				}
			}						
		}

		/// <summary>
		/// Called when a IRO command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a remote contact was already present in the session that was joined.
		/// <code>IRO [Transaction] [Current] [Total] [Account] [Name]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnIROReceived(SBMessage message)
		{
			Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[3].ToString());
			// update the name to make sure we have it up-to-date
			//contact.SetName(message.CommandValues[4].ToString());

			if(Contacts.Contains(contact.Mail) == false)
				Contacts.Add(contact.Mail, contact);

			// notify the client programmer
			OnContactJoined(contact);
		}
					
		/// <summary>
		/// Called when a MSG command has been received.
		/// </summary>
		/// <remarks>
		/// Indicates that a remote contact has send us a message. This can be a plain text message, an invitation, or an application specific message.
		/// <code>MSG [Account] [Name] [Bodysize]</code>
		/// </remarks>
		/// <param name="message"></param>
		protected virtual void OnMSGReceived(MSNMessage message)
		{												
			// the MSG command is the most versatile one. These are all the messages
			// between clients. Like normal messages, file transfer invitations, P2P messages, etc.

			Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString());
			// update the name to make sure we have it up-to-date
			//contact.SetName(message.CommandValues[1].ToString());
			
			// get the corresponding SBMSGMessage object
			MSGMessage sbMSGMessage = new MSGMessage(message);
			
			//first check if we are dealing with multi-packet-messages
			if (sbMSGMessage.MimeHeader.ContainsKey("Message-ID"))
			{
				//is this the first message?
				if (sbMSGMessage.MimeHeader.ContainsKey("Chunks"))
				{
					multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/0", sbMSGMessage);
					return;
				}
				
				else if (sbMSGMessage.MimeHeader.ContainsKey("Chunk"))
				{
					//Is this the last message?
					if (Convert.ToInt32(sbMSGMessage.MimeHeader["Chunk"]) + 1 == Convert.ToInt32(((MSGMessage)multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"]).MimeHeader["Chunks"]))
					{
						//Paste all the pieces together
						MSGMessage completeMessage = (MSGMessage)multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"];
						multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/0");

						int chunksToProcess = Convert.ToInt32(completeMessage.MimeHeader["Chunks"]) - 2;
						List<byte> completeText = new List<byte>(completeMessage.InnerBody);
						for (int i = 0; i < chunksToProcess; i++)
						{
							MSGMessage part = (MSGMessage)multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1)];
							completeText.AddRange(part.InnerBody);

							//Remove the part from the buffer
							multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1));
						}
						
						completeText.AddRange(sbMSGMessage.InnerBody);
						completeMessage.InnerBody = completeText.ToArray();

						//process the message
						sbMSGMessage = completeMessage;
					}
					else
					{
						multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/" + sbMSGMessage.MimeHeader["Chunk"], sbMSGMessage);
						return;
					}
				}
				else
					throw new Exception("Multi-packetmessage with damaged headers received");
			}
			
			if (sbMSGMessage.MimeHeader.ContainsKey ("Content-Type"))
				switch(sbMSGMessage.MimeHeader["Content-Type"].ToLower(System.Globalization.CultureInfo.InvariantCulture))
				{
					case "text/x-msmsgscontrol":
						// make sure we don't parse the rest of the message in the next loop											
						OnUserTyping(NSMessageHandler.ContactList.GetContact(sbMSGMessage.MimeHeader["TypingUser"]));					
						break;
					
					/*case "text/x-msmsgsinvite":
						break;
					
					case "application/x-msnmsgrp2p":
						break;*/
						
					case "text/x-mms-emoticon":
					case "text/x-mms-animemoticon":
						OnEmoticonDefinition(sbMSGMessage, contact);
						break;
					
					case "text/x-msnmsgr-datacast":
						if (message.CommandValues[2].Equals ("69"))
							OnNudgeReceived (contact);
						else if (message.CommandValues[2].Equals ("1325"))
							OnWinkReceived (sbMSGMessage, contact);
						break;
							
					default:
						if(sbMSGMessage.MimeHeader["Content-Type"].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
						{
							// a normal message has been sent, notify the client programmer
							TextMessage msg = new TextMessage();
							msg.CreateFromMessage(sbMSGMessage);
							OnTextMessageReceived(msg, contact);
						}
						break;
				}
		}			
		#endregion
	}
}