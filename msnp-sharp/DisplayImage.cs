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
using System.Drawing;
using System.Drawing.Imaging;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	/// <summary>
	/// A User display image.
	/// </summary>
	/// <remarks>
	/// This class can be used for convenient access to contact's and owner's user displays. It enables you to directly retrieve a .Net image object to
	/// view and manipulate.
	/// </remarks>
	[Serializable()]
	public class DisplayImage : MSNObject
	{
		/// <summary>
		/// Constructs a null image.
		/// </summary>
		public DisplayImage()
		{
			Type = MSNObjectType.UserDisplay;		
			Location = "dotmsn.png";
		}

		/// <summary>
		/// Constructs a null image.
		/// </summary>
		/// <param name="creator">Creator of the image</param>
		public DisplayImage(string creator)
		{
			Type = MSNObjectType.UserDisplay;		
			Location = "dotmsn.png";
			Creator = creator;
		}

		/// <summary>
		/// </summary>
		private	Image	image = null;

		/// <summary>
		/// The user display image. Null if not set
		/// </summary>
		public Image	Image
		{
			get { return image; }
			set 
			{				
				image = value;
				UpdateStream();
			}
		}

		/// <summary>
		/// Loads an image based on a stream.
		/// </summary>
		/// <param name="creator">Creator of the image</param>
		/// <param name="input">Stream representing the image</param>
		/// <param name="location">A location name given to the image</param>
		public DisplayImage(string creator, Stream input, string location)
			: base(creator, input, MSNObjectType.UserDisplay, location)
		{
			RetrieveImage();
		}		

		/// <summary>
		/// Loads the image in the specified file.
		/// </summary>
		/// <param name="creator">Creator of the image</param>
		/// <param name="file">Filename of the imagefile</param>
		public DisplayImage(string creator, string file)
			: base(creator, file, MSNObjectType.UserDisplay)
		{
			RetrieveImage();
		}

		/// <summary>
		/// Saves the image to the inner stream which is send to remote contacts. Is also automatically called when setting the Image property.
		/// </summary>
		public void UpdateStream()
		{
			Stream output = new MemoryStream();			
			image.Save(output, ImageFormat.Png);
			DataStream = new PersistentStream(output);
			Size = (int)DataStream.Length;							
			Sha = GetStreamHash(DataStream);
		}
		
		/// <summary>
		/// Sets the Image object based on the MSNObject's datastream.
		/// </summary>
		public void RetrieveImage()
		{
			Stream input = OpenStream();
			if(input != null)
			{
				lock(input)
				{					
					input.Position = 0;
					if(input.Length > 0)
					{						
						image = System.Drawing.Image.FromStream(input);
					}
				}
			}
			input.Close();
		}
	}
}
