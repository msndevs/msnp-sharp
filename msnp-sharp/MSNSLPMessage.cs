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
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MSNPSharp.Core;
using MSNPSharp;

namespace MSNPSharp.DataTransfer
{
	[Serializable()]
	public class MSNSLPMessage : NetworkMessage
	{
		Encoding encoding = Encoding.UTF8;
		string version = "MSNSLP/1.0";
		int maxForwards = 0;
		string to;
		string from;
		string via;
		int cSeq;
		string callId;
		string contentType;
		int contentLength;
		string body;
		string startLine;
		StrDictionary messageValues = new StrDictionary ();

		public Encoding Encoding
		{
			get { 
				return encoding; 
			}
			set { 
				encoding = value;
			}
		}

		public string Version
		{
			get { 
				return version; 
			}
			set { 
				version = value;
			}
		}

		public int MaxForwards
		{
			get { 
				return maxForwards; 
			}
			set { 
				maxForwards = value;
			}
		}

		public string To
		{
			get { 
				return to; 
			}
			set { 
				to = value;
			}
		}

		public string From
		{
			get { 
				return from; 
			}
			set { 
				from = value;
			}
		}

		public string FromMail
		{
			get { 
				return From.Replace("<msnmsgr:", "").Replace(">", ""); 
			}
		}
		
		public string ToMail
		{
			get { 
				return To.Replace("<msnmsgr:", "").Replace(">", ""); 
			}
		}
	
		public string Via
		{
			get { 
				return via; 
			}
			set { 
				via = value;
			}
		}

		public string Branch
		{
			get { 
				return Via.Substring(Via.IndexOf("{")); 
			}
		}

		public int CSeq
		{
			get { 
				return cSeq; 
			}
			set { 
				cSeq = value;
			}
		}

		public string CallId
		{
			get { 
				return callId; 
			}
			set { 
				callId = value;
			}
		}

		public string ContentType
		{
			get { 
				return contentType; 
			}
			set { 
				contentType = value;
			}
		}

		public int ContentLength
		{
			get { 
				return contentLength; 
			}
			set { 
				contentLength = value;
			}
		}

		public string Body
		{
			get { 
				return body; 
			}
			set { 
				body = value;
			}
		}

		public string StartLine
		{
			get { 
				return startLine; 
			}
			set { 
				startLine = value;
			}
		}

		public override byte[] GetBytes()
		{			
			StringBuilder builder = new StringBuilder(512);
			
			builder.Append(StartLine);
			builder.Append("\r\n");
			builder.Append("To: ");
			builder.Append(To);
			builder.Append("\r\n");
			builder.Append("From: ");
			builder.Append(From);
			builder.Append("\r\n");
			builder.Append("Via: ");
			builder.Append(Via);
			builder.Append("\r\n");
			builder.Append("CSeq: ");
			builder.Append(CSeq.ToString(CultureInfo.InvariantCulture));
			if (ContentType != null && ContentType == "application/x-msnmsgr-transrespbody")
				builder.Append("\r\n"); 
			else
				builder.Append(" \r\n"); //the space is required
			builder.Append("Call-ID: ");
			builder.Append(CallId);
			builder.Append("\r\n");
			builder.Append("Max-Forwards: ");
			builder.Append(MaxForwards.ToString(CultureInfo.InvariantCulture));
			builder.Append("\r\n");
			builder.Append("Content-Type: ");
			builder.Append(ContentType);
			builder.Append("\r\n");
			builder.Append("Content-Length: ");
			builder.Append((Encoding.GetByteCount(Body)+1).ToString(CultureInfo.InvariantCulture));
			builder.Append("\r\n");
			builder.Append("\r\n");	
			builder.Append(Body);
			
			foreach(StrKeyValuePair entry in MessageValues)
			{
				builder.Append(entry.Key).Append(": ").Append(entry.Value);
			}
		
			// get the bytes
			byte[] message = Encoding.GetBytes(builder.ToString());

			// add the additional 0x00
			byte[] totalMessage = new byte[message.Length + 1];	
			message.CopyTo(totalMessage, 0);
			totalMessage[message.Length] = 0x00;
			
			message = null;
			
			return totalMessage;
		}		

		public StrDictionary MessageValues
		{
			get { 
				return messageValues; 
			}
		}

		public override void ParseBytes(byte[] data)
		{
			// get the lines
			MessageValues.Clear();
			string[] lines = Encoding.GetString(data).Split('\n');
			StartLine = lines[0];
			
			foreach(string line in lines)
			{
				int indexOfColon = line.IndexOf(':', 0);
				if(indexOfColon > 0)
				{
					// extract the key and value from the string
					string name = line.Substring(0, indexOfColon);
					string val  = line.Substring(indexOfColon+1, line.Length - indexOfColon - 2).TrimStart (new char[] {' '});
					
					switch(name)
					{
						case "To":
							To = val; 
							break;
						case "From":
							From = val; 
							break;
						case "Via":
							Via = val; 
							break; 
						case "CSeq":
							CSeq = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture); 
							break;
						case "Call-ID":
							CallId = val; 
							break;
						case "Max-Forwards":
							MaxForwards = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture); 
							break; 
						case "Content-Type":
							ContentType = val; 
							break;
						case "Content-Length":
							ContentLength = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture); 
							break; 
						default:
							MessageValues.Add(name, val);
							break;
					}
				}
			}
		}

		public override string ToString()
		{
			if(Body == null) Body = "";
			return "[MSNSLPMessage] " + System.Text.Encoding.UTF8.GetString(this.GetBytes()) + "\r\n";
		}
	}
}
