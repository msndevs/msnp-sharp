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
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// Defines a single emoticon.
	/// </summary>
	[Serializable()]
	public class Emoticon : DisplayImage
	{
		/// <summary>
		/// </summary>
		public Emoticon()
		{
			Type = MSNObjectType.Emoticon;		
			Location = Guid.NewGuid().ToString();
		}

		/// <summary>
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="shortcut">The textual shortcut used in messages for this emoticon.</param>
		public Emoticon(string creator, string shortcut)
		{
			Type = MSNObjectType.Emoticon;		
			Location = Guid.NewGuid().ToString();
			Creator = creator;
			Shortcut = shortcut;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="input"></param>
		/// <param name="location"></param>
		/// <param name="shortcut">The textual shortcut used in messages for this emoticon.</param>
		public Emoticon(string creator, Stream input, string location, string shortcut)
			: base(creator, input, location)
		{
			Type = MSNObjectType.Emoticon;	
			Shortcut = shortcut;
		}		

		/// <summary>
		/// Loads the image in the specified file.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="file"></param>
		/// <param name="shortcut">The textual shortcut used in messages for this emoticon.</param>
		public Emoticon(string creator, string file, string shortcut)
		: base(creator, file)
		{
			Type = MSNObjectType.Emoticon;		
			Shortcut = shortcut;
		}

		/// <summary>
		/// </summary>
		private	string shortcut;

		/// <summary>
		/// The shortcut used for this emoticon.
		/// </summary>
		public string Shortcut
		{
			get { return shortcut; }
			set { shortcut = value;}
		}

	}
}
