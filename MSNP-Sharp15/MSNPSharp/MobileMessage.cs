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
using System.Text;
using MSNPSharp.Core;
using System.Xml;

namespace MSNPSharp
{
	/// <summary>
	/// Message that is send to a mobile device.
	/// </summary>
	public class MobileMessage : MSNMessage
	{
		#region Private
		private string callbackNumber = "";
		private string callbackDeviceName = "";

		private string text = "";

		private string receiver = "";
		#endregion

		#region Public

		/// <summary>
		/// The telephone number that the remote contact will see.
		/// </summary>
		public string CallbackNumber
		{
			get { return callbackNumber; }
			set { callbackNumber = value;}
		}

		/// <summary>
		/// The telephone device type that the remote contact will see. (e.g. "Homephone", "Work phone")
		/// </summary>
		public string CallbackDeviceName
		{
			get { return callbackDeviceName; }
			set { callbackDeviceName = value;}
		}

		/// <summary>
		/// The text that will be send to the remote contact.
		/// </summary>
		public string Text
		{
			get { return text; }
			set { text = value;}
		}

		/// <summary>
		/// The account of the remote contact.
		/// </summary>
		public string Receiver
		{
			get { return receiver;}
			set { receiver = value;}
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		public MobileMessage()
		{			
		}

		/// <summary>
		/// Throws an exception.
		/// </summary>
		/// <param name="data"></param>
		public override void ParseBytes(byte[] data)
		{
			throw new MSNPSharpException("MobileMessage can not parse data. This is done via a notification document.");
		}

		/// <summary>
		/// Returns the XML formatted message that represents the mobile message.
		/// </summary>
		/// <returns></returns>
		public override byte[] GetBytes()
		{			
			MemoryStream memStream = new MemoryStream();			
			
			XmlTextWriter writer = new XmlTextWriter(memStream, new System.Text.UTF8Encoding(false));
			
			writer.Formatting = Formatting.None;
			writer.Indentation = 0;
			writer.IndentChar = ' ';	
									
			// check whether there is a call back number set
			if(callbackNumber.Length > 0)
			{
				writer.WriteStartElement("PHONE");
				writer.WriteAttributeString("pri", "1");
				writer.WriteStartElement("LOC");
				
				writer.WriteString(callbackDeviceName);
				writer.WriteEndElement();
				writer.WriteStartElement("NUM");
				writer.WriteString(callbackNumber);
				writer.WriteEndElement();
				writer.WriteEndElement();				
			}
			
			if(Text.Length > 113)
				throw new MSNPSharpException("Mobile text message too long. A maximum of 113 characters is allowed.");
							
			writer.WriteRaw("<TEXT xml:space=\"preserve\" enc=\"utf-8\">");
			writer.WriteString(text);
			writer.WriteRaw("</TEXT>");			
			writer.Flush();					
			
			// return byte data
			// strip off first 3 bytes. These are 0xef, 0xbb and 0xbf.
			//byte[] body = memStream.ToArray();
			//byte[] msg  = new byte[body.Length - 3];
			//Array.Copy(body, 3, msg, 0, msg.Length);
			return memStream.ToArray();			
		}

		/// <summary>
		/// Sets the command and commandvalues of the parent.
		/// </summary>
		public override void PrepareMessage()
		{			
			base.PrepareMessage ();			
			if(ParentMessage != null && ParentMessage is MSNMessage)
			{
				((MSNMessage)ParentMessage).Command = "PGD";
				((MSNMessage)ParentMessage).CommandValues.Add(Receiver);
				((MSNMessage)ParentMessage).CommandValues.Add("1");
			}
		}

		/// <summary>
		/// Returns the XML formatted body.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "[MobileMessage]" + System.Text.Encoding.UTF8.GetString(this.GetBytes());	
		}
		
	}
}
