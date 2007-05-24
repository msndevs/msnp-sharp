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
	/// Handles the I/O of the Notification Server. 
	/// </summary>
	/// <remarks>
	/// NSMessageProcessor uses <see cref="NSMessagePool"/> as it's MessagePool.
	/// </remarks>
	public class NSMessageProcessor : SocketMessageProcessor
	{
		/// <summary>
		/// </summary>
		private int transactionID = 1;

		/// <summary>
		/// The transaction identifier used by the last message send
		/// </summary>
		public int TransactionID
		{
			get { return transactionID; }
			set { transactionID = value;}
		}


		/// <summary>
		/// Increases the transaction identifier by one and returns that value.
		/// </summary>
		protected int IncreaseTransactionID()
		{
			transactionID++;
			return transactionID;
		}


		/// <summary>
		/// Constructs a NSMessageProcessor object.
		/// </summary>
		private NSMessageProcessor()
		{
			MessagePool = new NSMessagePool();	
		}

		/// <summary>
		/// Creates a NSMessage object. After that the message is send to the handler.
		/// </summary>
		/// <param name="data"></param>
		protected override void OnMessageReceived(byte[] data)
		{
			base.OnMessageReceived (data);
			
			NSMessage message = new NSMessage();									

			// trace that we will be parsing
			System.Diagnostics.Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming NS command..", "NSMessageProcessor");

			// parse the incoming data
			message.ParseBytes(data);			

			// trace and dispatch the message
			if(Settings.TraceSwitch.TraceInfo)
			{
				System.Diagnostics.Trace.WriteLine("Dispatching incoming NS command : " + message.ToDebugString(), "NSMessageProcessor");				
				DispatchMessage(message);				
			}
			else
				DispatchMessage(message);

		}
		
		/// <summary>
		/// Sends the specified NSMessage over the connection.
		/// </summary>
		/// <param name="message">A NSMessage object</param>
		public override void SendMessage(NetworkMessage message)
		{
			MSNMessage NSMessage = (MSNMessage)message;
			NSMessage.TransactionID = IncreaseTransactionID();

			// prepare the message
			message.PrepareMessage();
			
			if(Settings.TraceSwitch.TraceVerbose)
				System.Diagnostics.Trace.WriteLine("Outgoing message:\r\n" + message.ToDebugString(), "NSMessageProcessor");

			// convert to bytes and send it over the socket
			SendSocketData(NSMessage.GetBytes());
		}

		/// <summary>
		/// Disconnect from the nameserver.
		/// </summary>
		/// <remarks>Sends the OUT command before disconnecting.</remarks>
		public override void Disconnect()
		{
			SendMessage(new NSMessage("OUT", new string[]{}));
			base.Disconnect ();
		}


		/// <summary>
		/// Sends the received network message, from the server, to all registered handlers.
		/// </summary>
		/// <param name="message">The message to send</param>
		protected virtual void DispatchMessage(NetworkMessage message)
		{
			// copy the messageHandlers array because the collection can be 
			// modified during the message handling. (Handlers are registered/unregistered)
			ArrayList messageHandlersCopy = (ArrayList)MessageHandlers.Clone();

			// now give the handlers the opportunity to handle the message
			foreach(IMessageHandler handler in messageHandlersCopy)
			{
				try
				{
					handler.HandleMessage(this, message);
				}
				catch(Exception e)
				{
					if(HandlerException != null)
						HandlerException(this, new ExceptionEventArgs(new MSNPSharpException("An exception occured while handling a nameserver message. See inner exception for more details.", e)));
				}
			}
		}

		/// <summary>
		/// Occurs when an exception was raised by one of the registered handlers while processing an incoming message.
		/// </summary>
		public event ProcessorExceptionEventHandler	HandlerException;
		
	}
}