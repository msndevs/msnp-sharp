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
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{
	// Buffers the incoming data from the notification server (NS).
	// The main purpose of this class is to ensure that MSG, IPG and NOT payload commands are processed
	// only when they are complete. Payload commands can be quite large and may be larger
	// than the socket buffer. This pool
	// will buffer the data and release the messages, or commands, when they are
	// fully retrieved from the server.
	public class NSMessagePool : MessagePool
	{
		Queue<MemoryStream> messageQueue;
		MemoryStream bufferStream = null;
		BinaryWriter bufferWriter = null;
		int remainingBuffer = 0;
		
		public NSMessagePool()
		{
			messageQueue = new Queue <MemoryStream> ();
			CreateNewBuffer();
		}

		protected Queue<MemoryStream> MessageQueue
		{
			get { 
				return messageQueue; 
			}
		}

		// This points to the current message we are writing to
		protected MemoryStream BufferStream
		{
			get { 
				return bufferStream; 
			}
			set { 
				bufferStream = value;
			}
		}

		// This is the interface to the bufferStream
		protected BinaryWriter BufferWriter
		{
			get { 
				return bufferWriter; 
			}
			set { 
				bufferWriter = value;
			}
		}

		// Creates a new memorystream to server as the buffer.
		private void CreateNewBuffer()
		{
			bufferStream = new MemoryStream(64);
			bufferWriter = new BinaryWriter(bufferStream);
		}

		// Enques the current buffer memorystem when a message is completely retrieved.
		private void EnqueueCurrentBuffer()
		{
			messageQueue.Enqueue(bufferStream);
		}

		// Is true when there are message available to retrieve.
		public override bool MessageAvailable
		{
			get
			{
				return messageQueue.Count > 0;
			}
		}

		// Get the next message as a byte array.
		// The returned data includes all newlines which seperate the commands ("\r\n")
		public override byte[] GetNextMessageData()
		{
			MemoryStream memStream = (MemoryStream)messageQueue.Dequeue();
			return memStream.ToArray();
		}

		// Stores the raw data in a buffer. When a full message is detected it is inserted on the internal stack.
		// You can retrieve these messages bij calling GetNextMessageData().
		public override void BufferData(BinaryReader reader)
		{
			int length = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
					
			// there is nothing in the bufferstream so we expect a command right away
			while(length > 0)
			{
				// should we buffer the current message
				if(remainingBuffer > 0)
				{
					// read as much as possible in the current message stream
					int readLength = Math.Min(remainingBuffer, length);
					byte[] msgBuffer = reader.ReadBytes(readLength);
					bufferStream.Write(msgBuffer, 0, msgBuffer.Length);

					// subtract what we have read from the total length
					remainingBuffer -= readLength;
					length = (int)(reader.BaseStream.Length - reader.BaseStream.Position);

					// when we have read everything we can start a new message
					if(remainingBuffer == 0)
					{
						EnqueueCurrentBuffer();
						CreateNewBuffer();
					}
				}
				else
				{
					// read until we come across a newline
					byte val = reader.ReadByte();
					
					if(val != '\n')
						bufferWriter.Write(val);
					else
					{
						// write the last newline
						bufferWriter.Write(val);				
						
						// check if it's a payload command
						bufferStream.Position = 0;
						byte[] cmd = new byte[3];
						cmd[0] = (byte)bufferStream.ReadByte();
						cmd[1] = (byte)bufferStream.ReadByte();
						cmd[2] = (byte)bufferStream.ReadByte();
						
						if((cmd[0] == 'M' && cmd[1] == 'S' && cmd[2] == 'G') ||		// MSG payload command
						   (cmd[0] == 'I' && cmd[1] == 'P' && cmd[2] == 'G') ||		// IPG pager command
						   (cmd[0] == 'N' && cmd[1] == 'O' && cmd[2] == 'T') ||
						   (cmd[0] == 'U' && cmd[1] == 'B' && cmd[2] == 'X') ||
						   (cmd[0] == 'G' && cmd[1] == 'C' && cmd[2] == 'F') ||
                           (cmd[0] == 'U' && cmd[1] == 'B' && cmd[2] == 'M'))	// NOT notification command
						{
							bufferStream.Seek(-3, SeekOrigin.End);

							// calculate the length by reading backwards from the end
							remainingBuffer = 0;
							int size = 0;
							int iter = 0;

							while((size = bufferStream.ReadByte())>0 && size >= '0' && size <= '9')
							{
								remainingBuffer += (int)((size-'0') * Math.Pow(10, iter));
								bufferStream.Seek(-2, SeekOrigin.Current);
								iter++;
							}
							
							// move to the end of the stream before we are going to write
							bufferStream.Seek(0, SeekOrigin.End);
							
							if (remainingBuffer == 0)
							{
								EnqueueCurrentBuffer ();
								CreateNewBuffer ();	
							}
						}
						else
						{
							// it was just a plain command start a new message
							EnqueueCurrentBuffer();
							CreateNewBuffer();							
						}
					}
					length--;
				}
			}			
		}
	}
}
