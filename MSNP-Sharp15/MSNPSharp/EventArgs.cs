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
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// Used in events where a exception is raised. Via these events the client programmer
	/// can react on these exceptions.
	/// </summary>
	[Serializable()]
	public class ExceptionEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private Exception _exception;

		/// <summary>
		/// The exception that was raised
		/// </summary>
		public Exception Exception
		{
			get { return _exception; }
			set { _exception = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="e"></param>
		public ExceptionEventArgs(Exception e)
		{
			_exception = e;
		}
	}

	/// <summary>
	/// Used as event argument when a textual message is send.
	/// </summary>
	[Serializable()]
	public class TextMessageEventArgs : EventArgs
	{
		/// <summary>
		/// </summary>
		private TextMessage message;

		/// <summary>
		/// The message send.
		/// </summary>
		public TextMessage Message
		{
			get { return message; }
			set { message = value;}
		}

		private Contact sender;

		/// <summary>
		/// The sender of the message.
		/// </summary>
		public Contact Sender
		{
			get { return sender; }
			set { sender = value;}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="sender"></param>
		public TextMessageEventArgs(TextMessage message, Contact sender)
		{
			Message = message;
			Sender  = sender;
		}
	}
	
	[Serializable()]
	public class WinkEventArgs : EventArgs
	{
		private Contact sender;
		
		/// <summary>
		/// The sender of the message.
		/// </summary>
		public Contact Sender
		{
			get { return sender; }
			set { sender = value;}
		}

		private Wink wink;
		
		public Wink Wink
		{
			get { return wink; }
			set { wink = value; }
		}
		
		public WinkEventArgs (Contact contact, Wink wink)
		{
			this.sender = contact;
			this.wink = wink;
		}
	}

	/// <summary>
	/// Used as event argument when a emoticon definition is send.
	/// </summary>
	[Serializable()]
	public class EmoticonDefinitionEventArgs : EventArgs
	{
		private Contact sender;

		/// <summary>
		/// The sender of the message.
		/// </summary>
		public Contact Sender
		{
			get { return sender; }
			set { sender = value;}
		}

		/// <summary>
		/// </summary>
		private Emoticon emoticon;

		/// <summary>
		/// The emoticon which is defined
		/// </summary>
		public Emoticon Emoticon
		{
			get { return emoticon; }
			set { emoticon = value;}
		}



		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="emoticon"></param>
		public EmoticonDefinitionEventArgs(Contact sender, Emoticon emoticon)
		{
			this.sender = sender;
			this.emoticon = emoticon;			
		}
	}
}
