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
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;
using MSNPSharp.Core;
using MSNPSharp;

namespace MSNPSharp.DataTransfer
{
	/// <summary>
	/// A single transfer of data within a p2p session. 
	/// </summary>
	/// <remarks>
	/// P2PTransferSession handles all messages with a specified session id in the p2p header.
	/// Optional a direct connection can be created. It will try to connect to the remote client or listening for incoming connections.
	/// If that succeeds and the local client is the sender of the data a seperate thread will be started to send data messages over the direct connection.
	/// However, if the direct connection fails it will send the data messages over the switchboard session. These latter messages go via the messenger servers and is therefore quite slow compared to direct connections
	/// but it is guaranteed to work even when both machines are behind a proxy, firewall or router.
	/// </remarks>
	public class P2PTransferSession : IMessageHandler, IMessageProcessor
	{
		#region Properties

		/// <summary>
		/// </summary>
		private bool	autoCloseStream = false;

		/// <summary>
		/// Defines whether the stream is automatically closed after the transfer has finished or been aborted.
		/// </summary>
		public  bool	AutoCloseStream
		{
			get { return autoCloseStream; }
			set { autoCloseStream = value;}
		}

		/// <summary>
		/// </summary>
		private	object clientData = null;

		/// <summary>
		/// This property can be used by the client-programmer to include application specific data
		/// </summary>
		public object ClientData
		{
			get { return clientData; }
			set { clientData = value;}
		}

		/// <summary>
		/// Tracked to know when an acknowledgement for the (switchboards) data preparation message is received
		/// </summary>
		private uint DataPreparationAck = 0;

		/// <summary>
		/// Tracked to send the disconnecting message (0x40 flag) with the correct datamessage identifiers as it's acknowledge identifier. (protocol)
		/// </summary>
		private uint dataMessageIdentifier = 0;

		/// <summary>
		/// </summary>
		private uint sessionId = 0;

		/// <summary>
		/// The session id which this object handles. P2P messages will be redirected to this object based on their session id.
		/// </summary>
		public uint SessionId
		{
			get { return sessionId; }
			set { sessionId = value; }
		}

		/// <summary>
		/// </summary>
		private P2PMessageSession messageSession = null;

		/// <summary>
		/// The message session which keeps track of the local / remote message identifiers and redirects messages to this handler based on the session id
		/// </summary>
		public P2PMessageSession MessageSession
		{
			get { return messageSession; }
			set { 
				messageSession = value;
				MessageProcessor = messageSession;
				messageSession.DirectConnectionEstablished += new EventHandler(messageSession_DirectConnectionEstablished);
				messageSession.DirectConnectionFailed += new EventHandler(messageSession_DirectConnectionFailed);
			}
		}

		/// <summary>
		/// </summary>
		private Guid callId = Guid.Empty;

		/// <summary>
		/// The unique call-id used in MSNSLP messages
		/// </summary>
		public Guid	CallId
		{
			get { return callId; }
			set { callId = value;}
		}

        /// <summary>
        /// </summary>
		private uint messageFlag = 0;

		/// <summary>
		/// This value is set in the flag field in a p2p header.
		/// </summary>
		/// <remarks>
		/// For filetransfers this value is for example 0x1000030
		/// </remarks>
		public uint	MessageFlag
		{
			get { return messageFlag; }
			set { messageFlag = value;}
		}		


		/// <summary>
		/// The stream to read from when data is send, or to write to when data is received.
		/// </summary>
		private Stream dataStream;

		/// <summary>
		/// The stream to read from when data is send, or to write to when data is received. Default is a MemorySteam.
		/// </summary>
		/// <remarks>
		/// In the eventhandler, when an invitation is received, the client programmer must set this property in order to enable the transfer to succeed.
		/// In the case of the filetransfer, when the local client is the receiver, the incoming data is written to the specified datastream.
		/// In the case of the invitation for a msn object (display picture, emoticons, background), when the local client is the sender, the outgoing data is read from the specified datastream.
		/// </remarks>
		public Stream DataStream
		{
			get {
				return dataStream; 
			}
			set { dataStream = value;}
		}		


		/// <summary>
		/// </summary>
		private bool isSender = false;

		/// <summary>
		/// Defines whether the local client is sender or receiver
		/// </summary>
		public bool	IsSender
		{
			get { return isSender; }
			set { isSender = value; }
		}				
				
		#endregion
	
		#region Public

		/// <summary>
		/// Constructor.
		/// </summary>
		public P2PTransferSession()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Constructing p2p transfer session object", "P2PTransferSession");	
		}

		/// <summary>
		/// Aborts the datatransfer, if available. This will send a P2P abort message and stop the sending thread.
		/// It will not close a direct connection. If AutoCloseStream is set to true, the datastream will be closed.
		/// </summary>
		public void AbortTransfer()
		{
			if(transferThread != null && transferThread.ThreadState == ThreadState.Running)
			{
				AbortTransferThread();
				SendAbortMessage();			

				if(AutoCloseStream)
					DataStream.Close();

				OnTransferAborted();
			}
		}

		
		/// <summary>
		/// Starts a seperate thread to send the data in the stream to the remote client. It will first wait for a direct connection if tryDirectConnection is set to true.
		/// </summary>
		/// <remarks>
		/// This method will not open or close the specified datastream.
		/// </remarks>
		public void StartDataTransfer(bool tryDirectConnection)
		{
			if(transferThread != null)
			{
				throw new MSNPSharpException("Start of data transfer failed because there is already a thread sending the data.");
			}

			System.Diagnostics.Debug.Assert(sessionId != 0, "Trying to initiate p2p data transfer but no session is specified");
			System.Diagnostics.Debug.Assert(dataStream != null, "Trying to initiate p2p data transfer but no session is specified");
			
			isSender = true;

			if(	messageSession.DirectConnected == false && 
				messageSession.DirectConnectionAttempt == false && 
				tryDirectConnection == true)
			{
			
				waitingDirectConnection = true;
				return;
			}

			waitingDirectConnection = false;

			transferThreadStart = new ThreadStart(TransferDataEntry);
			transferThread = new Thread(transferThreadStart);
			transferThread.Start();
		}


		#region IMessageHandler Members
		/// <summary>
		/// </summary>
		private IMessageProcessor messageProcessor = null;
		/// <summary>
		/// The message processor to which p2p messages (this includes p2p data messages) will be send
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
			}
		}
	
		/// <summary>
		/// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
		/// </summary>
		public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
		{
			System.Diagnostics.Debug.Assert(message is P2PMessage, "Incoming message is not a P2PMessage", "");

			P2PMessage p2pMessage = (P2PMessage)message;									
			
			/*
			    0x00 - No flags
			    0x01 - Unknown
			    0x02 - Acknowledgement
			    0x04 - Waiting for a reply
			    0x08 - Error (Possibly binary level)
			    0x10 - Unknown
			    0x20 - Data for DP/CE
			    0x40 - Bye ack (He who got BYE)
			    0x80 - Bye ack (He who sent BYE)
			    0x1000030 - Data for FT
			*/
			
			if(p2pMessage.Flags == 0x80)
			{				
				AbortTransfer();
			}
			
			// check to see if our session data has been transferred correctly
			if(p2pMessage.SessionId > 0
			      && p2pMessage.IsAcknowledgement
			      && p2pMessage.DW1 == dataMessageIdentifier)
			{
				// inform the handlers
				OnTransferFinished();
				
				// notify the remote client that we are finished
				SendDisconnectMessage();

				return;
			}

			// check if it is a content message
			// if it is not a file transfer message, and the footer is not set to 1 (for emoticons/user displays), ignore it.
			if(p2pMessage.SessionId > 0 && p2pMessage.InnerBody.Length > 0
				&& (p2pMessage.Flags == (uint) P2PFlag.FileData || p2pMessage.Footer == 1))
			{
				// indicates whether we must stream this message
				bool writeToStream = true;
				
				// check if it is a data preparation message send via the SB
				if(p2pMessage.TotalSize == 4 && p2pMessage.MessageSize == 4
					&& p2pMessage.InnerBody[0] == 0 && p2pMessage.InnerBody[1] == 0)																							   
				{
					writeToStream = false;
				}
				
				if(writeToStream)
				{
					// store the data message identifier because we want to reference it if we abort the transfer
					dataMessageIdentifier = p2pMessage.Identifier;

					if(DataStream == null)  
						throw new MSNPSharpException("Data was received in a P2P session, but no datastream has been specified to write to.");

					if(DataStream.Length < (long)p2pMessage.Offset + (long)p2pMessage.InnerBody.Length)
						DataStream.SetLength((long)p2pMessage.Offset + (long)p2pMessage.InnerBody.Length);

					DataStream.Seek((long)p2pMessage.Offset, SeekOrigin.Begin);
					DataStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);
					
					OnTransferPulse ();

					// check for end of file transfer
					if(p2pMessage.Offset + p2pMessage.MessageSize == p2pMessage.TotalSize)
					{
						// keep track of the remote identifier									
						MessageSession.IncreaseRemoteIdentifier();	
						P2PMessage ack = p2pMessage.CreateAcknowledgement();
						ack.SessionId = p2pMessage.SessionId;
						ack.Identifier = 0;
						ack.TotalSize  = p2pMessage.TotalSize;

						SendMessage(ack);
						
						if(AutoCloseStream)
							DataStream.Close();
						
						OnTransferFinished();

						// notify the remote client we close the direct connection
						SendDisconnectMessage();
					} 

					// finished handling this message
					return;
				}						
			}

			if(Settings.TraceSwitch.TraceVerbose)
				System.Diagnostics.Trace.WriteLine("P2P Info message received", "P2PTransferSession");

			// it is not a datamessage.
			// fill up the buffer with this message and extract the messages one-by-one and dispatch
			// it to all handlers.
			p2pMessagePool.BufferMessage(p2pMessage);

			while(p2pMessagePool.MessageAvailable)
			{
				// keep track of the remote identifier							
				MessageSession.IncreaseRemoteIdentifier();

				p2pMessage = p2pMessagePool.GetNextMessage();

				// the message is not a datamessage, send it to the handlers
				lock(handlers)
				{
					foreach(IMessageHandler handler in handlers)
						handler.HandleMessage(this, message);				
				}
			}
		}


		#endregion
		
		#region IMessageProcessor Members
												

		/// <summary>
		/// Sends a message for this session to the message processor. If a direct connection is established the p2p message is directly send to the message processor.
		/// If there is no direct connection available, it will wrap the incoming p2p message in a MSGMessage with the correct parameters. It also sets the identifiers and acknowledge session, provided they're not already set.
		/// </summary>
		/// <param name="message"></param>
		public void SendMessage(NetworkMessage message)
		{						
			P2PMessage p2pMessage = (P2PMessage)message;

			// check whether it's already set. This is important to check for acknowledge messages.
			/*if(p2pMessage.Identifier == 0)
			{
				MessageSession.IncreaseLocalIdentifier();
				p2pMessage.Identifier = MessageSession.LocalIdentifier;
			}
			
			if(p2pMessage.DW1 == 0)
				p2pMessage.DW1 = (uint)new Random().Next(50000, int.MaxValue);*/

			// split up large messages which go to the SB
			if(MessageSession.DirectConnected == false && p2pMessage.MessageSize > 1202)
			{
				uint bytesSend = 0;
				int cnt = ((int)(p2pMessage.MessageSize / 1202)) + 1;
				for(int i = 0; i < cnt; i++)
				{
					P2PMessage chunkMessage = new P2PMessage();

					// copy the values from the original message
					chunkMessage.DW2 = p2pMessage.DW2;
					chunkMessage.QW1  = p2pMessage.QW1;
					chunkMessage.Flags         = p2pMessage.Flags;
					chunkMessage.Footer        = p2pMessage.Footer;
					if(p2pMessage.Flags == (uint) P2PFlag.FileData)
						chunkMessage.Footer        = 0x2;
					chunkMessage.Identifier    = p2pMessage.Identifier;
					chunkMessage.MessageSize   = (uint)Math.Min((uint)1202, (uint)(p2pMessage.TotalSize - bytesSend));
					chunkMessage.Offset        = bytesSend;
					chunkMessage.SessionId     = p2pMessage.SessionId;
					chunkMessage.TotalSize     = p2pMessage.TotalSize;

					chunkMessage.InnerBody = new byte[chunkMessage.MessageSize];
					Array.Copy(p2pMessage.InnerBody, (int)chunkMessage.Offset, chunkMessage.InnerBody, 0, (int)chunkMessage.MessageSize);

					chunkMessage.DW1 = (uint)System.Environment.TickCount;	

					chunkMessage.PrepareMessage();

					//SBMessage sbMessage = WrapMessage(chunkMessage);

					// now send it to propbably a SB processor
					if(MessageProcessor != null)
						MessageProcessor.SendMessage(chunkMessage);
				}
			}
			else
			{
				IMessageProcessor processor = null;
				bool	direct = false;
				lock(this)
				{
					processor = MessageProcessor;
					direct = MessageSession.DirectConnected;
				}
				// send a single message
				if(direct == true)
				{
					// now send it to probably a P2PDirectProcessor
					if(processor != null)
						processor.SendMessage(p2pMessage);
				}
				else
				{
					// wrap the message before sending it to the SB processor
					p2pMessage.PrepareMessage();
					if(processor != null)
						processor.SendMessage(p2pMessage);
				}				
			}
		}


		/// <summary>
		///  Collection of handlers
		/// </summary>
		private ArrayList handlers = new ArrayList();

		/// <summary>
		/// Registers handlers for incoming p2p messages.
		/// </summary>
		/// <param name="handler"></param>
		public void RegisterHandler(IMessageHandler handler)
		{			
			UnregisterHandler(handler);
			lock(handlers)
			{
				handlers.Add(handler);
			}
		}

		/// <summary>
		/// Unregisters handlers.
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

		#endregion		

		#region Protected
		/// <summary>
		/// Sends the remote client a p2p message with the 0x80 flag to abort.
		/// </summary>
		protected virtual void SendAbortMessage()
		{
			P2PMessage disconnectMessage = new P2PMessage();
			disconnectMessage.Flags = 0x80;
			disconnectMessage.SessionId = SessionId;
			disconnectMessage.DW1 = dataMessageIdentifier;
			MessageProcessor.SendMessage(disconnectMessage);
		}

		/// <summary>
		/// Keeps track of clustered p2p messages
		/// </summary>
		protected P2PMessagePool P2PMessagePool
		{
			get { return p2pMessagePool; }
			set { p2pMessagePool = value;}
		}

		/// <summary>
		/// The thread in which the data messages are send
		/// </summary>
		protected Thread TransferThread
		{
			get { return transferThread; }
			set { transferThread = value;}
		}

		/// <summary>
		/// Kickstart object to start the data transfer thread
		/// </summary>
		protected ThreadStart TransferThreadStart
		{
			get { return transferThreadStart; }
			set { transferThreadStart = value;}
		}		

		/// <summary>
		/// Fires the TransferStarted event.
		/// </summary>
		protected virtual void OnTransferStarted()
		{			
			if(TransferStarted != null)
				TransferStarted(this, new EventArgs());
		}

		/// <summary>
		/// Fires the TransferFinished event.
		/// </summary>
		protected virtual void OnTransferFinished()
		{
			if(TransferFinished != null)
				TransferFinished(this, new EventArgs());
		}

		/// <summary>
		/// Fires the TransferAborted event.
		/// </summary>
		protected virtual void OnTransferAborted()
		{			
			if(TransferAborted != null)
				TransferAborted(this, new EventArgs());
		}
		
		/// <summary>
		/// Fires the TransferPulse event.
		/// </summary>
		protected virtual void OnTransferPulse()
		{			
			if(TransferPulse != null)
				TransferPulse(this, new EventArgs());
		}
		

		/// <summary>
		/// Entry point for the thread. This thread will send the data messages to the message processor.
		/// In case it is a direct connection P2PDCMessages will be send. If no direct connection is established
		/// P2PMessage objects are wrapped in a SBMessage object and send to the message processor. Which is in the latter case
		/// probably a SB processor.
		/// </summary>
		protected void TransferDataEntry()
		{
			if(Settings.TraceSwitch.TraceInfo)
				System.Diagnostics.Trace.WriteLine("Starting transfer thread", "P2PTransferSession");

			OnTransferStarted();

			try
			{				
				bool direct = false;				

				// check whether we have a direct connection								
				direct = MessageSession.DirectConnected;
				
				if(direct == false)					
				{
					// send the data preparation message
					P2PDataMessage p2pDataMessage = new P2PDataMessage();	
					p2pDataMessage.WritePreparationBytes();

					p2pDataMessage.SessionId = sessionId;

					MessageSession.IncreaseLocalIdentifier();
					p2pDataMessage.Identifier = MessageSession.LocalIdentifier;
					
					p2pDataMessage.DW1 = (uint)System.Environment.TickCount;

					// store the acknowledge identifier so we can accept the acknowledge later on
					DataPreparationAck = p2pDataMessage.DW1;

					MessageProcessor.SendMessage(p2pDataMessage);					
				}
				
				MessageSession.IncreaseLocalIdentifier();
				uint messageIdentifier = MessageSession.LocalIdentifier;

				// keep track of this identifier because it will be used in the disconnecting message.
				dataMessageIdentifier = messageIdentifier;

				long currentPosition = 0;							
				long lastPosition = dataStream.Length; 

				while(currentPosition < lastPosition)
				{
					// send the data message
					P2PDataMessage p2pDataMessage = new P2PDataMessage();	

					p2pDataMessage.SessionId = sessionId;

					p2pDataMessage.Offset = (uint)currentPosition;
					p2pDataMessage.TotalSize = (uint)dataStream.Length;

					// send the rest of the data						
					lock(dataStream)
					{
						dataStream.Seek(currentPosition, SeekOrigin.Begin);
						int bytesWritten = p2pDataMessage.WriteBytes(dataStream, 1202);
						currentPosition += bytesWritten;
					}

					p2pDataMessage.Flags = messageFlag;

					// set this footer so we know 
					if(messageFlag == 0x1000030)
						p2pDataMessage.Footer = 2;

					p2pDataMessage.Identifier = messageIdentifier;

					p2pDataMessage.DW1 = (uint)System.Environment.TickCount;
					
					MessageProcessor.SendMessage(p2pDataMessage);			
				}								
			}
			catch(Exception e)
			{
				OnTransferAborted();				

				if(Settings.TraceSwitch.TraceInfo)
					System.Diagnostics.Trace.WriteLine("Exception in transfer thread: " + e.ToString(), "P2PTransferSession");

				SendAbortMessage();					

				throw new MSNPSharpException("Exception Occurred while sending p2p data. See inner exception for details.", e);
			}
			finally
			{
				if(Settings.TraceSwitch.TraceInfo)
					System.Diagnostics.Trace.WriteLine("Stopping transfer thread", "P2PTransferSession");
			}
		}


		/// <summary>
		/// Sends the remote client a p2p message with the 0x40 flag to indicate we are going to close the connection.
		/// </summary>
		protected virtual void SendDisconnectMessage()
		{
			P2PMessage disconnectMessage = new P2PMessage();				
			disconnectMessage.Flags = 0x40;
			disconnectMessage.SessionId = SessionId;		
			disconnectMessage.DW1 = dataMessageIdentifier;			// aargh it took me long to figure this one out
			MessageProcessor.SendMessage(disconnectMessage);
		}

		/// <summary>
		/// Aborts a running data transfer thread.
		/// </summary>
		protected virtual void AbortTransferThread()
		{
			if(transferThread != null && transferThread.ThreadState == ThreadState.Running)
			{
				transferThread.Abort();
			}
		}


		#endregion
		
		#region Private

		/// <summary>
		/// </summary>
		private P2PMessagePool p2pMessagePool = new P2PMessagePool();

		
		/// <summary>
		/// </summary>
		private Thread		transferThread = null;

		/// <summary>
		/// </summary>
		private ThreadStart transferThreadStart	= null;

		/// <summary>
		/// </summary>
		private bool		waitingDirectConnection = false;

		/// <summary>
		/// Indicates whether the session is waiting for the result of a direct connection attempt
		/// </summary>
		protected bool	WaitingDirectConnection
		{
			get { return waitingDirectConnection; }
			set { waitingDirectConnection = value;}
		}
		

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the sending of data messages has started.
		/// </summary>
		public event	EventHandler	TransferStarted;
		/// <summary>
		/// Occurs when the sending of data messages has finished.
		/// </summary>
		public event	EventHandler	TransferFinished;
		/// <summary>
		/// Occurs when the transfer of data messages has been aborted.
		/// </summary>
		public event	EventHandler	TransferAborted;
		/// <summary>
		/// Occurs when data is received. Can be used by the client programmer to track progress
		/// </summary>
		public event	EventHandler	TransferPulse;

		#endregion		

		/// <summary>
		/// Start the transfer session if it is waiting for a direct connection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void messageSession_DirectConnectionEstablished(object sender, EventArgs e)
		{
			if(waitingDirectConnection == true)
			{
				waitingDirectConnection = false;
				StartDataTransfer(true);
			}
		}

		/// <summary>
		/// Start the transfer session if it is waiting for a direct connection. Because the direct connection attempt failed the transfer will be over the switchboard.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void messageSession_DirectConnectionFailed(object sender, EventArgs e)
		{
			if(waitingDirectConnection == true)
			{
				waitingDirectConnection = false;
				StartDataTransfer(false);
			}
		}
	}
}
