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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Net;
using MSNPSharp.Core;
using MSNPSharp;

namespace MSNPSharp.DataTransfer
{
	// P2PMessageSession routes all messages in the p2p framework between the local client and a single remote client.
	// A single message session can hold multiple p2p transfer sessions. This for example occurs when a contact sends
	// two files directly after each other in the same switchboard session.
	// This class keeps track of the message identifiers, dispatches messages to registered message handlers and routes
	// data messages to the correct P2PTransferSession objects. Usually this class is a handler of a switchboard processor.
	// A common handler for this class is MSNSLPHandler
	public class P2PMessageSession : IMessageHandler, IMessageProcessor
	{
		uint remoteBaseIdentifier;
		uint remoteIdentifier;
		
		int sendseqno = 0;
		bool sendseqinitialized = false;
		uint srtt; //session round trip time

		P2PDCHandshakeMessage handshakeMessage;
		P2PMessagePool p2pMessagePool = new P2PMessagePool();
		bool autoAcknowledgeHandshake = true;
		bool directConnected = false;
		bool directConnectionAttempt = false;

		string remoteContact;
		string localContact;
		
        // A list of all direct processors trying to establish a connection.
        ArrayList pendingProcessors = new ArrayList();
		List<IMessageHandler> handlers = new List<IMessageHandler> ();
		// A collection of all transfersessions
		Dictionary<uint, P2PTransferSession> transferSessions = new Dictionary<uint, P2PTransferSession> ();
        
		/// Occurs when a direct connection is succesfully established.
		public event EventHandler DirectConnectionEstablished;
		/// Occurs when a direct connection attempt has failed.
		public event EventHandler DirectConnectionFailed;
        
		public P2PMessageSession()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Constructing object", "P2PMessageSession");
		}

		// The handshake message to send to the receiving client when a direct connection has been established
		// If this property is set to null no handshake message is sent.
		public P2PDCHandshakeMessage HandshakeMessage
		{
			get { 
				return handshakeMessage; 
			}
			set { 
				handshakeMessage = value;
			}
		}

		// Defines whether a direct connection handshake is automatically send to the remote client, or replied with an acknowledgement.
		// Setting this to true means the remote client will start the transfer immediately.
		// Setting this to false means the client programmer must send a handhsake message and an acknowledgement message after which the transfer will begin.
		public bool AutoHandshake
		{
			get { 
				return autoAcknowledgeHandshake; 
			}
			set { 
				autoAcknowledgeHandshake = value;
			}
		}

		// Defines whether the message session runs over a direct session or is routed via the messaging server
		public bool DirectConnected
		{
			get { 
				return directConnected;
			}
		}

		// Defines whether an attempt has been made to create a direct connection
		public bool DirectConnectionAttempt
		{
			get { 
				return directConnectionAttempt; 
			}
		}

		// The base identifier of the remote client
		public uint RemoteBaseIdentifier
		{
			get {
				return remoteBaseIdentifier;
			}
			set {
				remoteBaseIdentifier = value;
			}
		}
		
		// The expected identifier of the remote client for the next message.
		public uint RemoteIdentifier
		{
			get {
				return remoteIdentifier;
			}
			set {
				remoteIdentifier = value;
			}
		}
		
		public uint SendSeq 
		{
			get {
				return (uint) sendseqno;
			}
			set {
				sendseqno = (int) value;
			}
		}
		
		public uint GetNextSendSeq ()
		{
			int seqno;		
		
			if (!sendseqinitialized)
			{
				sendseqinitialized = true;
				
				//make it always positive
				seqno = System.Environment.TickCount & Int32.MaxValue;
			}
			else
				seqno = sendseqno;
			
			if (seqno + 1 == 0)
				seqno = 0;
				
			sendseqno = ++seqno;

			return (uint) seqno;
		}
		
		public uint RoundTripTime 
		{
			get {
				return srtt;
			}
			set {
				srtt = value;
			}
		}

		public string LocalContact
		{
			get { 
				return localContact; 
			}
			set { 
				localContact = value;
			}
		}

		public string RemoteContact
		{
			get { 
				return remoteContact;
			}
			set { 
				remoteContact = value;
			}
		}

		// Removes references to handlers and the messageprocessor. Also closes running transfer sessions and pending processors establishing connections.
		public virtual void CleanUp()
		{						
            StopAllPendingProcessors();
			AbortAllTransfers();		

			handlers.Clear();
			
			MessageProcessor = null;

			transferSessions.Clear();
		}

        // Disconnect all processors that are trying to establish a connection.
        protected void StopAllPendingProcessors()
        {
            lock (pendingProcessors)
            {
                foreach (P2PDirectProcessor processor in pendingProcessors)
                {
                    processor.Disconnect();
                    processor.UnregisterHandler(this);                    
                }
                
                pendingProcessors.Clear();
            }
        }

        // Add the processor to the pending list.
        protected void AddPendingProcessor(P2PDirectProcessor processor)
        {
            // we want to handle message from this processor			
            processor.RegisterHandler(this);

            // inform the session of connected/disconnected events
            processor.ConnectionEstablished += new EventHandler(OnDirectProcessorConnected);
            processor.ConnectionClosed += new EventHandler(OnDirectProcessorDisconnected);
            processor.ConnectingException += new ProcessorExceptionEventHandler(OnDirectProcessorException);

            lock (pendingProcessors)
            {
                pendingProcessors.Add(processor);
            }
        }

        // Use the given processor as the direct connection processor. And disconnect all other pending processors.
        protected void UsePendingProcessor(P2PDirectProcessor processor)
        {
            lock (pendingProcessors)
            {
                if (pendingProcessors.Contains(processor))
                {
                    pendingProcessors.Remove(processor);
                }
            }

            // stop all remaining attempts
            StopAllPendingProcessors();
            
            // set the direct processor as the main processor
            lock (this)
            {
                directConnected = true;
                directConnectionAttempt = true;
                preDCProcessor = MessageProcessor;
                MessageProcessor = processor;
            }

            if (DirectConnectionEstablished != null)
                DirectConnectionEstablished(this, new EventArgs());	
        }

		// Creates a direct connection with the remote client.
		public IMessageProcessor CreateDirectConnection(string host, int port)
		{			
			// create the P2P Direct processor to handle the file data
			P2PDirectProcessor processor = new P2PDirectProcessor();
			processor.ConnectivitySettings = new ConnectivitySettings();
			processor.ConnectivitySettings.Host = host;
			processor.ConnectivitySettings.Port = port;			

			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Trying to setup direct connection with remote host " + host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture), "P2PTransferSession");

            AddPendingProcessor(processor);

			// try to connect
			processor.Connect();

			return processor;
		}

		
		// Setups a P2PDirectProcessor to listen for incoming connections.
		// After a connection has been established the P2PDirectProcessor will become the main MessageProcessor to send messages.
		public IMessageProcessor ListenForDirectConnection(IPAddress host, int port)
		{
			P2PDirectProcessor processor = new P2PDirectProcessor();

			// we want this session to listen to incoming messages
			processor.RegisterHandler(this);						

			// add to the list of processors trying to establish a connection
			AddPendingProcessor(processor); 

			// start to listen
			processor.Listen(host, port);

			return processor;
		}

		// Closes the direct connection with the remote client, if available. A closing p2p message will be send first.
		// The session will fallback to the previous (SB) message processor.
		public void CloseDirectConnection()
		{
			if(DirectConnected == false)
				return;
			else
				CleanUpDirectConnection();
		}

		/// <summary>
		/// Aborts all running transfer sessions.
		/// </summary>
		public virtual void AbortAllTransfers()
		{
			foreach(P2PTransferSession session in transferSessions.Values)
				session.AbortTransfer();
		}

		// The identifier of the remote client, increases with each message received
		public void IncreaseRemoteIdentifier()
		{
			remoteIdentifier++;
		}

		// Adds the specified transfer session to the collection and sets the transfer session's message processor to be the
		// message processor of the p2p message session. This is usally a SB message processor. 
		public void AddTransferSession(P2PTransferSession session)
		{
			session.MessageProcessor = this;

			lock(handlers)
			{
				foreach(IMessageHandler handler in handlers)
				{
					session.RegisterHandler(handler);
				}
			}

			transferSessions.Add(session.SessionId, session);
		}

		// Removes the specified transfer session from the collection.
		public void RemoveTransferSession(P2PTransferSession session)
		{			
			transferSessions.Remove(session.SessionId);			
		}

		// Returns the transfer session associated with the specified session identifier.
		public P2PTransferSession GetTransferSession(uint sessionId)
		{
			return transferSessions[sessionId];			
		}

		// Searches through all handlers and returns the first object with the specified type, or null if not found.
		public object GetHandler(Type handlerType)
		{
			foreach(IMessageHandler handler in handlers)
			{
				if(handler.GetType() == handlerType)
					return handler;
			}
			
			return null;
		}


		#region Protected

		// Keeps track of clustered p2p messages
		protected P2PMessagePool P2PMessagePool
		{
			get { 
				return p2pMessagePool;
			}
			set { 
				p2pMessagePool = value;
			}
		}


		// Wraps a P2PMessage in a MSGMessage and SBMessage.
		protected SBMessage WrapMessage(NetworkMessage networkMessage)
		{
			MSGMessage msgWrapper = new MSGMessage();
			msgWrapper.MimeHeader.Add ("Content-Type", "application/x-msnmsgrp2p");
			msgWrapper.MimeHeader.Add ("P2P-Dest", RemoteContact);
			msgWrapper.InnerMessage = networkMessage;

			SBMessage sbMessageWrapper = new SBMessage();
			sbMessageWrapper.InnerMessage = msgWrapper;

			return sbMessageWrapper;
		}

		// Sends the handshake message in a direct connection.
		protected virtual void SendHandshakeMessage(IMessageProcessor processor)
		{
			if (Settings.TraceSwitch.TraceError)
				System.Diagnostics.Trace.WriteLine("Preparing to send handshake message", "P2PMessageSession");

			if(HandshakeMessage == null)
			{
				// don't throw an exception because the file transfer can continue over the switchboard
				if(Settings.TraceSwitch.TraceError)
					System.Diagnostics.Trace.WriteLine("Handshake could not be send because none is specified.");

				// but close the direct connection
				((SocketMessageProcessor)processor).Disconnect();
				return;	
			}

			uint seq = GetNextSendSeq ();
			
			HandshakeMessage.Identifier = seq;

			HandshakeMessage.DW1 = seq;
			DCHandshakeAck = HandshakeMessage.DW1;			
			
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Sending handshake message:\r\n " + HandshakeMessage.ToDebugString(), "P2PMessageSession");

            processor.SendMessage(HandshakeMessage);
		}
		
		#endregion		

		#region Private

		/// This is the processor used before a direct connection. Usually a SB processor.
		/// It is a fallback variables in case a direct connection fails.
		private IMessageProcessor	preDCProcessor = null;		

		/// <summary>
		/// Tracked to know when an acknowledgement for the handshake is received.
		/// </summary>
		private uint	DCHandshakeAck = 0;

		/// <summary>
		/// Sets the message processor back to the switchboard message processor.
		/// </summary>
		private void CleanUpDirectConnection()
		{
			lock(this)
			{								
				if(DirectConnected == false)
					return;
								
				SocketMessageProcessor directProcessor = (SocketMessageProcessor)MessageProcessor;

				directConnected = false;
				directConnectionAttempt = false;
				MessageProcessor = preDCProcessor;

				directProcessor.Disconnect();						
			}
		}

		

		/// <summary>
		/// Sets the current message processor to the processor which has just connected succesfully.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDirectProcessorConnected(object sender, EventArgs e)
		{			
			//DCHandshakeProcessor = (IMessageProcessor)sender;
			
			if (((P2PDirectProcessor)sender).IsListener == false)
			{
				if(AutoHandshake == true && HandshakeMessage != null)
				{
					SendHandshakeMessage((P2PDirectProcessor)sender);
				}
			}			
		}

		/// <summary>
		/// Cleans up the direct connection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDirectProcessorDisconnected(object sender, EventArgs e)
		{	
			CleanUpDirectConnection();	
		}

		/// <summary>
		/// Called when the direct processor could not connect. It will start the data transfer over the switchboard session.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDirectProcessorException(object sender, ExceptionEventArgs e)
		{
			lock(this)
			{
				CleanUpDirectConnection();
				directConnectionAttempt = true;
			}

			if(DirectConnectionFailed != null)
				DirectConnectionFailed(this, new EventArgs());
		}

		/// <summary>
		/// Occurs when an acknowledgement to a send handshake has been received, or a handshake is received.
		/// This will start the data transfer, provided the local client is the sender.
		/// </summary>
		protected virtual void OnHandshakeCompleted(P2PDirectProcessor processor)
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Handshake accepted", "P2PTransferSession");

            UsePendingProcessor(processor);			
		}
		#endregion
						
		#region IMessageHandler Members
		private IMessageProcessor messageProcessor = null;
		/// <summary>
		/// The message processor that sends the P2P messages to the remote contact.
		/// </summary>
		public IMessageProcessor MessageProcessor
		{
			get
			{
				return messageProcessor;
			}
			set
			{
				messageProcessor = value;
				if(messageProcessor != null)
				{
					ValidateProcessor();
					SendBuffer();
				}
			}
		}
		

		/// <summary>
		/// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
		/// </summary>
		public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
		{
			System.Diagnostics.Debug.Assert(message is P2PMessage, "Incoming message is not a P2PMessage", "");			

			P2PMessage p2pMessage = (P2PMessage)message;			

			// check whether it is an acknowledgement to data preparation message
			if(p2pMessage.Flags == 0x100 && DCHandshakeAck != 0)
			{
				//Console.WriteLine ("HANDSHAKE!");
				OnHandshakeCompleted((P2PDirectProcessor)sender);		
				return;
			}

			// check if it's a direct connection handshake
			if(p2pMessage.Flags == 0x100 && AutoHandshake == true)
			{
				//Console.WriteLine ("HANDSHAKE!");
				// create a handshake message based on the incoming p2p message and send it				
				P2PDCHandshakeMessage dcHsMessage = new P2PDCHandshakeMessage(p2pMessage);				
				sender.SendMessage(dcHsMessage.CreateAcknowledgement());				
				OnHandshakeCompleted((P2PDirectProcessor)sender);
				return;
			}

			// check if it is a content message
			if(p2pMessage.SessionId > 0)
			{				
				// get the session to handle this message				
				P2PTransferSession session = transferSessions[p2pMessage.SessionId];
				if(session != null)
					session.HandleMessage(this, p2pMessage);
				return;
			}					

			// it is not a datamessage.
			// fill up the buffer with this message and extract the messages one-by-one and dispatch
			// it to all handlers. Usually the MSNSLP handler.
			p2pMessagePool.BufferMessage(p2pMessage);

			while(p2pMessagePool.MessageAvailable)
			{
				// keep track of the remote identifier			
				IncreaseRemoteIdentifier();

				p2pMessage = p2pMessagePool.GetNextMessage();				

				lock(handlers)
				{
					// the message is not a datamessage, send it to the handlers
					foreach(IMessageHandler handler in handlers)
						handler.HandleMessage(this, message);
				}
			}
		}

		#endregion

		#region IMessageProcessor Members		
		/// <summary>
		/// Sends incoming p2p messages to the remote contact.		
		/// </summary>
		/// <remarks>
		/// Before the message is send a couple of things are checked. If there is no identifier available, the local identifier will be increased by one and set as the message identifier.
		/// Second, if the acknowledgement identifier is not set it will be set to a random value. After this the method will check for the total length of the message. If the total length
		/// is too large, the message will be splitted into multiple messages. The maximum size for p2p messages over a switchboard is 1202 bytes. The maximum size for p2p messages over a
		/// direct connection is 1352 bytes. As a result the length of the splitted messages will be 1202 or 1352 bytes or smaller, depending on the availability of a direct connection.
		/// 
		/// If a direct connection is available the message is wrapped in a <see cref="P2PDCMessage"/> object and send over the direct connection. Otherwise it will be send over a switchboard session.
		/// If there is no switchboard session available, or it has become invalid, a new switchboard session will be requested by asking this to the nameserver handler.
		/// Messages will be buffered until a switchboard session, or a direct connection, becomes available. Upon a new connection the buffered messages are directly send to the remote contact
		/// over the new connection.
		/// </remarks>
		/// <param name="message">The P2PMessage to send to the remote contact.</param>
		
		//TODO: Convert param to Generics
		public void SendMessage(NetworkMessage message)
		{	
			P2PMessage p2pMessage = (P2PMessage)message;

			// maximum length by default is 1202 bytes.
			int maxSize = 1202;

			// check whether we have a direct connection (send p2pdc messages) or not (send sb messages)
			if(DirectConnected == true)
			{
				maxSize = 1352;
			}
			
			//this is the most common identifier, so just set if not set
			if(p2pMessage.Identifier == 0)
			{
				p2pMessage.Identifier = GetNextSendSeq ();
			}
			
			// split up large messages which go to the SB
			if(p2pMessage.MessageSize > maxSize)
			{				
				byte[] totalMessage = null;
				if(p2pMessage.InnerBody != null)
					totalMessage = p2pMessage.InnerBody;
				else if(p2pMessage.InnerMessage != null)
					totalMessage = p2pMessage.InnerMessage.GetBytes();				
				else
					throw new MSNPSharpException("An error occured while splitting a large p2p message into smaller messages: Both the InnerBody and InnerMessage are null.");

				uint bytesSend = 0;
				int cnt = ((int)(p2pMessage.MessageSize / maxSize)) + 1;
				
				for(int i = 0; i < cnt; i++)
				{
					P2PMessage chunkMessage = new P2PMessage();

					// copy the values from the original message
					chunkMessage.DW2 = p2pMessage.DW2;
					chunkMessage.QW1 = p2pMessage.QW1;
					chunkMessage.Flags = p2pMessage.Flags;
					chunkMessage.Footer = p2pMessage.Footer;
					chunkMessage.Identifier = p2pMessage.Identifier;
					chunkMessage.MessageSize = (uint)Math.Min((uint)maxSize, (uint)(p2pMessage.MessageSize - bytesSend));
					chunkMessage.Offset = bytesSend;
					chunkMessage.SessionId = p2pMessage.SessionId;
					chunkMessage.TotalSize = p2pMessage.MessageSize;

					chunkMessage.InnerBody = new byte[chunkMessage.MessageSize];
					Array.Copy(totalMessage, (int)chunkMessage.Offset, chunkMessage.InnerBody, 0, (int)chunkMessage.MessageSize);

					chunkMessage.PrepareMessage();

					// now send it to propbably a SB processor
					try
					{
						if(MessageProcessor != null && ((SocketMessageProcessor)MessageProcessor).Connected == true)
						{
							if(DirectConnected == true)
								MessageProcessor.SendMessage(new P2PDCMessage(chunkMessage));
							else
							{
								// wrap the message before sending it to the (probably) SB processor
								MessageProcessor.SendMessage(WrapMessage(chunkMessage));
							}
						}
						
						else
						{
							InvalidateProcessor();
							BufferMessage(chunkMessage);
						}
					}
					catch(System.Net.Sockets.SocketException)
					{
						InvalidateProcessor();
						BufferMessage(chunkMessage);
					}
					
					bytesSend += chunkMessage.MessageSize;
				}
			}
			else
			{
				try
				{
					if(MessageProcessor != null)
					{
						if(DirectConnected == true)
							MessageProcessor.SendMessage(new P2PDCMessage(p2pMessage));
						else
							MessageProcessor.SendMessage(WrapMessage (p2pMessage));
					}
					else
					{
						InvalidateProcessor();
						BufferMessage(p2pMessage);
					}
				}
				catch(System.Net.Sockets.SocketException)
				{
					InvalidateProcessor();
					BufferMessage(p2pMessage);
				}
			}
		}	

		/// <summary>
		/// Occurs when the processor has been marked as invalid. Due to connection error, or message processor being null.
		/// </summary>
		public event EventHandler	ProcessorInvalid;

		/// <summary>
		/// Keeps track of unsend messages
		/// </summary>
		private Queue sendMessages = new Queue();

		/// <summary>
		/// 
		/// </summary>
		private	bool  processorValid = true;

		/// <summary>
		/// Indicates whether the processor is invalid
		/// </summary>
		public	bool ProcessorValid
		{
			get { return processorValid; }
		}

		/// <summary>
		/// Sets the processor as invalid, and requests the p2phandler for a new request.
		/// </summary>
		protected virtual void InvalidateProcessor()
		{
			if(processorValid == false)
				return;

			processorValid = false;
			OnProcessorInvalid();
						
		}

		/// <summary>
		/// Sets the processor as valid.
		/// </summary>
		protected virtual void ValidateProcessor()
		{
			processorValid = true;
		}

		/// <summary>
		/// Fires the ProcessorInvalid event.
		/// </summary>
		protected virtual void OnProcessorInvalid()
		{
			if(ProcessorInvalid != null)
				ProcessorInvalid(this, new EventArgs());
		}

		/// <summary>
		/// Buffer messages that can not be send because of an invalid message processor.
		/// </summary>
		/// <param name="message"></param>
		protected virtual void BufferMessage(NetworkMessage message)
		{
			sendMessages.Enqueue(message);
		}

		/// <summary>
		/// Try to resend any messages that were stored in the buffer.
		/// </summary>
		protected virtual void SendBuffer()
		{			
			if(MessageProcessor == null)
				return;

			try
			{
				while(sendMessages.Count > 0)
				{				
					P2PMessage p2pmsg = (P2PMessage) sendMessages.Dequeue ();
					
					//I guess it should be set here, when it's sent
					if ((p2pmsg.Flags & 0x1CB) == 0)
						p2pmsg.DW1 = (uint) System.Environment.TickCount & Int32.MaxValue;
					
					if(DirectConnected)
						MessageProcessor.SendMessage(new P2PDCMessage(p2pmsg));
					else
						MessageProcessor.SendMessage(WrapMessage(p2pmsg));
						
					Console.WriteLine ("------------------------ SENT --------------------- ");
					Console.WriteLine (p2pmsg.ToString ());
				}
			}
			catch(System.Net.Sockets.SocketException)
			{
				Console.WriteLine ("ERROR SENDING P2P MESSAGE");
				InvalidateProcessor();
			}
		}
		
		/// <summary>
		/// Registers a message handler. After registering the handler will receive incoming messages.
		/// </summary>
		/// <param name="handler"></param>
		public void RegisterHandler(IMessageHandler handler)
		{			
			lock(handlers)
			{
				if(handlers.Contains(handler) == true)
					return;
					
				handlers.Add(handler);
			}
		}

		/// <summary>
		/// Unregisters a message handler. After registering the handler will no longer receive incoming messages.
		/// </summary>
		/// <param name="handler"></param>
		public void UnregisterHandler(IMessageHandler handler)
		{
			lock(handlers)
			{
				handlers.Remove(handler);
			}
		}
		
		#endregion
	}
}
