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
using MSNPSharp;
using MSNPSharp.DataTransfer;

namespace MSNPSharp.Core
{
	/// <summary>
	/// NSMessage represents a single message, or command, send from and to the Notification Server.
	/// </summary>
	[Serializable()]
	public class NSMessage : MSNMessage
	{
		#region Private
		#endregion

		#region Public
		/// <summary>
		/// Returns the NS message as a byte array. This can be directly send over a networkconnection.
		/// Remember to set the transaction ID before calling this method.
		/// Uses UTF8 Encoding.
		/// No MSG commands can be send to the NS, and is therefore not supported by this function.
		/// This function will add a \r\n at the end of the string, with the exception of a QRY command. Which does not work with a trailing \r\n.
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{
			if(Command != "QRY")
				return base.GetBytes();

			// for the exception do it ourselves
			StringBuilder builder = new StringBuilder(64);
			builder.Append(Command);
			builder.Append(' ');
			builder.Append(TransactionID.ToString(System.Globalization.CultureInfo.InvariantCulture));
			foreach(string val in CommandValues)
			{				
				builder.Append(val);
			}
			
			if(Command != "QRY") builder.Append("\r\n");
			
			return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
		}

	

		/// <summary>
		/// Constructor.
		/// </summary>
		public NSMessage()
		{
			
		}

		/// <summary>
		/// Constructor providing initial values.
		/// </summary>
		public NSMessage(string command, ArrayList commandValues)
			: base(command, commandValues)
		{			
		}

		/// <summary>
		/// Constructor providing initial values.
		/// </summary>
		public NSMessage(string command, string[] commandValues)
			: base(command, new ArrayList(commandValues))
		{			
		}

		#endregion
	}
}
