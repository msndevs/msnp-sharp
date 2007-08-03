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
using System.IO;
using MSNPSharp.Core;
using MSNPSharp;

namespace MSNPSharp.DataTransfer
{
	/// <summary>
	/// Buffers incomplete P2PMessage and releases them when the message is fully received.
	/// </summary>
	public class P2PMessagePool
	{
		/// <summary>
		/// </summary>
		private Hashtable messageStreams = new Hashtable();

		/// <summary>
		/// </summary>
		private Queue availableMessages = new Queue(1);

		/// <summary>
		/// Constructor
		/// </summary>
		public P2PMessagePool()
		{
			
		}

		/// <summary>
		/// Buffers the incoming raw data internal.This method is often used after receiving incoming data from a socket or another source.
		/// </summary>		
		/// <param name="message">The message to buffer.</param>
		public void BufferMessage(P2PMessage message)
		{
			// assume the start of a p2p message		
			if(message.IsAcknowledgement == true || message.MessageSize == 0 || message.MessageSize == message.TotalSize)
			{
				availableMessages.Enqueue(message);
			}
			else
			{
				// if this message already exists in the buffer append the current p2p message to the buffer
				if(messageStreams.ContainsKey(message.Identifier))
				{
					((MemoryStream)messageStreams[message.Identifier]).Write(message.InnerBody, 0, message.InnerBody.Length);
				}
				else
				{
					MemoryStream bufferStream = new MemoryStream();
					messageStreams.Add(message.Identifier, bufferStream);
					bufferStream.Write(message.InnerBody, 0, message.InnerBody.Length);
				}

				// check whether this is the last message
				if(message.Offset + message.MessageSize == message.TotalSize)
				{
					// set the correct fields to match the whole message
					message.Offset = 0;
					message.MessageSize = (uint)message.TotalSize;
					MemoryStream bufferStream = (MemoryStream)messageStreams[message.Identifier];

					// set the inner body to the whole message
					message.InnerBody = bufferStream.ToArray();					

					// and make it available for the client
					availableMessages.Enqueue(message);

					// remove the old memorystream buffer, and clear up resources in the hashtable
					bufferStream.Close();
					messageStreams.Remove(message.Identifier);
				}
			}
		}

		/// <summary>
		/// Defines whether there is a message available to retrieve
		/// </summary>		
		public bool MessageAvailable
		{
			get { return availableMessages.Count > 0; }
		}

		/// <summary>
		/// Retrieves the next p2p message from the buffer.
		/// </summary>
		/// <returns></returns>
		public P2PMessage GetNextMessage()
		{
			System.Diagnostics.Debug.Assert(availableMessages.Count > 0, "No p2p messages available in queue");

			return (P2PMessage)availableMessages.Dequeue();
		}
	}
}
