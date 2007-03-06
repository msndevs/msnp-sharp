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
using System.Text;
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{
	/// <summary>
	/// Represents a single MSG command sended over a switchboard or nameserver session.
	/// </summary>
	[Serializable()]
	public class MSGMessage : NetworkMessage
	{
		/// <summary>
		/// </summary>
		private Hashtable mimeHeader;

		/// <summary>
		/// A collection of all name/value pairs in the mime header
		/// </summary>
		public  Hashtable	MimeHeader
		{
			get { return mimeHeader; }
		}

		/// <summary>
		/// Gets the header with the inner message appended as a byte array.
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{
			StringBuilder builder = new StringBuilder();
			foreach(string key in MimeHeader.Keys)
			{
				builder.Append(key.ToString()).Append(": ").Append(MimeHeader[key].ToString()).Append("\r\n");
			}
			builder.Append("\r\n");

			if(InnerMessage != null)
				return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), InnerMessage.GetBytes());    
			return
				System.Text.Encoding.UTF8.GetBytes(builder.ToString());    			
		}


		/// <summary>
		/// Parse the MIME-header. The enumerator points after the \r\n\r\n after finishing.
		/// </summary>
		/// <remarks>
		/// The enumerator will be pointer directly before the first character of the message body when this method is finished.
		/// </remarks>
		/// <param name="enumerator"></param>
		/// <returns></returns>
		protected Hashtable ParseMime(IEnumerator enumerator)
		{
			Hashtable table = new Hashtable();
			StringBuilder name = new StringBuilder();
			StringBuilder val  = new StringBuilder();
			StringBuilder active = name;

			while(enumerator.MoveNext())
			{
				if((byte)enumerator.Current == 13)
				{					
					// no name specified -> end of header (presumably \r\n\r\n)
					if(name.Length == 0)
					{
						enumerator.MoveNext();
						return table;		
					}

					table.Add(name.ToString(), val.ToString());
					name.Length = 0;
					val.Length  = 0;
					active = name;
				}
				else if((byte)enumerator.Current == 58) //:
				{
					if(active == name)
					{
						active = val;
						enumerator.MoveNext();
					}
				}
				else if((byte)enumerator.Current != 10)
				{
					active.Append(Convert.ToChar(enumerator.Current, System.Globalization.CultureInfo.InvariantCulture));
				}
			}

			return table;
		}

		/// <summary>
		/// Parses incoming byte data from the server. This will set the properties and message body with the values specified in the data array.
		/// </summary>
		/// <exception cref="MSNPSharpException">Thrown when parsing fails.</exception>		
		/// <param name="data">The message as received from the server.</param>		
		public override void ParseBytes(byte[] data)
		{
			// parse the header
			IEnumerator enumerator = data.GetEnumerator();
			mimeHeader = ParseMime(enumerator);
			
			// get the rest of the message
			MemoryStream memStream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(memStream);
			while(enumerator.MoveNext())
				writer.Write((byte)enumerator.Current);
			InnerBody = memStream.ToArray();
			memStream.Close();			
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		public MSGMessage()
		{
			mimeHeader = new Hashtable();
			mimeHeader.Add("MIME-Version", "1.0");
		}

		/// <summary>
		/// Constructs a MSGMessage from the inner body contents of the specified SBMessage object.
		/// This will also set the InnerMessage property of the SBMessage object to the newly created MSGMessage.
		/// </summary>
		public MSGMessage(NetworkMessage message)
		{
			ParseBytes(message.InnerBody);
			message.InnerMessage = this;
		}

		/// <summary>
		/// Textual presentation.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach(string key in MimeHeader.Keys)
			{
				builder.Append(key.ToString()).Append(": ").Append(MimeHeader[key].ToString()).Append("\r\n");
			}
			builder.Append("\r\n");
			return "[MSGMessage] " + builder.ToString();
		}

	}
}
