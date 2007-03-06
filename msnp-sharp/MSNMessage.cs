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
using System.Text;
using System.Globalization;
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{
	/// <summary>
	/// The base class for all messages which are directly send to, or received from the msn server.
	/// This class has the basic methods to perform bytearray to message and message to bytearray conversions.
	/// The SB and NS command messages are typical examples which use this class as their baseclass.
	/// </summary>
	[Serializable()]
	public class MSNMessage : NetworkMessage
	{
		#region Private
		/// <summary>
		/// </summary>
		private int			transactionID;
		/// <summary>
		/// </summary>
		private string		command;
		/// <summary>
		/// </summary>
		private ArrayList	commandValues = new ArrayList(4);

		/// <summary>
		/// </summary>
		private string		acknowledgement = "N";
		#endregion

		#region Public overrides
		/// <summary>
		/// Clears the Command and CommandValues when an innermessage is set. So the innermessage can set these properties.
		/// </summary>
		public override void PrepareMessage()
		{
			if(InnerMessage != null)
			{
				Command = "";
				CommandValues.Clear();
			}
			base.PrepareMessage ();
		}
		#endregion
		#region Public

		/// <summary>
		/// Acknowledgement
		/// </summary>
		public string Acknowledgement
		{
			get { return acknowledgement; }
			set { acknowledgement = value;}
		}

		/// <summary>
		/// The transaction identifier of this message. Only valid when the message is received from the notification server.
		/// </summary>
		public int		TransactionID
		{
			get { return transactionID; }
			set { transactionID = value;}
		}


		/// <summary>
		/// The command name. This is always a string composed of 3 characters.
		/// </summary>
		public string	Command
		{
			get { return command; }
			set { command = value;}
		}
		
		/// <summary>
		/// A list of all values after the initial command string. The values are all stored as strings. After construction this is an empty list.
		/// </summary>
		public ArrayList CommandValues
		{
			get { return commandValues; }
			set { commandValues = value;}
		}


		/// <summary>
		/// Returns the command message as a byte array. This can be directly send over a networkconnection.
		/// Remember to set the transaction ID before calling this method.
		/// Uses UTF8 Encoding.				
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{
			byte[] contents = null;

			if(InnerMessage != null)
			{
				contents = InnerMessage.GetBytes();							

				// prepare a default MSG message if an inner message is specified
				if(Command.Length == 0)
				{
					Command = "MSG";		
					CommandValues.Add(Acknowledgement);
					CommandValues.Add(contents.Length.ToString(CultureInfo.InvariantCulture));
				}				
			}
			
			StringBuilder builder = new StringBuilder(128);
			builder.Append(Command);

			if (CommandValues.Count > 0)
			{
				builder.Append(' ');
				builder.Append(TransactionID.ToString(CultureInfo.InvariantCulture));
				foreach(string val in CommandValues)
				{
					builder.Append(' ');
					builder.Append(val);
				}
				builder.Append("\r\n");
			}

			if(InnerMessage != null)
				return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), contents);
			else
				return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
		}

		/// <summary>
		/// Parses incoming byte data send from the network.
		/// This will set the command values and other properties of the class, depending on the message.
		/// </summary>
		/// <param name="data">The raw message as received from the server</param>
		public override void ParseBytes(byte[] data)
		{
			int cnt = 0;
			int bodyStart = 0;
			
			while(data[cnt] != '\r')
			{				
				cnt++;

				// watch out for buffer overflow
				if(cnt == data.Length)
					throw new MSNPSharpException("Parsing of incoming command message failed. No newline was detected.");
			}
			
			bodyStart = cnt + 1;
			while(bodyStart < data.Length && (data[bodyStart] == '\r' || data[bodyStart] == '\n'))
			{
				bodyStart++;
			}

			// get the command parameters
			Command = System.Text.Encoding.ASCII.GetString(data, 0, 3);
			CommandValues  = new ArrayList(System.Text.Encoding.UTF8.GetString(data, 4, cnt - 4).Split(new char[] { ' '}));
			
			// set the inner body contents, if it is available
			if(bodyStart < data.Length)
			{
				int startIndex = bodyStart;
				int newLength  = data.Length - startIndex;
				InnerBody = new byte[newLength]; 
				Array.Copy(data, startIndex, InnerBody, 0, newLength);
			}			
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		public MSNMessage()
		{
			
		}

		/// <summary>
		/// Constructor providing initial values.
		/// </summary>
		public MSNMessage(string command, ArrayList commandValues)
		{
			Command		  = command;
			CommandValues = commandValues;
		}		

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			PrepareMessage();

			return System.Text.Encoding.ASCII.GetString (GetBytes ());
		}

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override string ToDebugString()
		{
			PrepareMessage();
			return base.ToDebugString ();
		}


		#endregion
	}
}
