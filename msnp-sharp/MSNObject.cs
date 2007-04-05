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
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Web;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	
	public enum MSNObjectType
	{
		Unknown = 0,
		Emoticon = 2,
		UserDisplay = 3,
		Background = 5,
		Wink = 8
	}	 

	/// <summary>
	/// The MSNObject can hold an image, display, emoticon, etc.
	/// </summary>
	[Serializable()]
	public class MSNObject
	{		
		private string	originalContext = null;
		private string	oldHash = "";
		[NonSerialized]
		private Stream dataStream = null;		

		private string fileLocation = null;
		private string creator;
		private int	size;
		private MSNObjectType type;
		private string location;		 
		private string friendly = "\0";		 
		private string sha = String.Empty;

		
		public MSNObject()
		{			
			
		}

		/// <summary>
		/// Constructs a MSN object based on a (memory)stream. The client programmer is responsible for inserting this object in the global msn object collection.
		/// The stream must remain open during the whole life-length of the application.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="inputStream"></param>
		/// <param name="type"></param>
		/// <param name="location"></param>
		public MSNObject(string creator, Stream inputStream, MSNObjectType type, string location)
		{						
			this.creator = creator;
			this.size	= (int)inputStream.Length;
			this.type 	= type;
			this.location = location;// + new Random().Next().ToString();
			
			this.sha = GetStreamHash(inputStream);

			this.DataStream = inputStream;
		}

		/// <summary>
		/// Constructs a MSN object based on a physical file. The client programmer is responsible for inserting this object in the global msn object collection.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="type"></param>
		/// <param name="fileLocation"></param>
		public MSNObject(string creator, string fileLocation, MSNObjectType type)
		{			
			this.location  = Path.GetFullPath(fileLocation).Replace(Path.GetPathRoot(fileLocation), "");			
			this.location += new Random().Next().ToString(CultureInfo.InvariantCulture);

			this.fileLocation = fileLocation;

			//Stream stream = OpenStream();
			
			this.creator = creator;
			//this.size	= (int)stream.Length;
			this.type 	= type;
			
			//this.sha = GetStreamHash(stream);
			//stream.Close();
		}	
		
		/// <summary>
		/// The datastream to write to, or to read from
		/// </summary>
		public virtual Stream DataStream
		{
			get {
				if (dataStream == null)
					dataStream = new MemoryStream ();
				
				return dataStream;
			}
			set {
				dataStream = value;
			}
		}

		/// <summary>
		/// The local contact list owner
		/// </summary>
		public string Creator
		{
			get { return creator; }
			set { creator = value; UpdateInCollection(); }
		}

		/// <summary>
		/// The friendly name of the file
		/// </summary>
		public string Friendly
		{
			get {
				return friendly;
			}
			set { friendly = value; UpdateInCollection(); }
		}
		
		/// <summary>
		/// The original context string that was send by the remote contact
		/// </summary>
		public string OriginalContext
		{
			get { return originalContext; }
		}

		/// <summary>
		/// The total data size
		/// </summary>
		public int Size
		{
			get { return size; }
			set { size = value; UpdateInCollection(); }
		}

		/// <summary>
		/// The type of MSN Object
		/// </summary>
		public MSNObjectType Type
		{
			get { return type; }
			set { type = value; UpdateInCollection(); }
		}

		/// <summary>
		/// The location of the object. This is a location on the hard-drive. Use relative paths. This is only a text string; na data is read in after setting this field. Use FileLocation for that purpose.
		/// </summary>
		public string Location
		{
			get { return location; }
			set { location = value; UpdateInCollection(); }
		}

		/// <summary>
		/// [Deprecated, use LoadFile()] Gets or sets the file location. When a file is set the file data is immediately read in memory to extract the filehash. It will retain in memory afterwards.
		/// </summary>
		public string FileLocation
		{
			get { return fileLocation; }
			set 
			{ 
                this.LoadFile(value);
				fileLocation = value;
			}
		}

		/// <summary>
        /// Gets or sets the file location. When a file is set the file data is immediately read in memory to extract the filehash. It will retain in memory afterwards.
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadFile(string fileName)
        {
            if (this.fileLocation == fileName) return;
            this.fileLocation = fileName;
            this.location = Path.GetRandomFileName();

            // copy the file
            byte[] buffer = new byte[512];
            Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int cnt = 0;
            while ((cnt = fileStream.Read(buffer, 0, 512)) > 0)
            {
                DataStream.Write(buffer, 0, cnt);
            }

            this.size = (int)DataStream.Length;
            this.sha = GetStreamHash(DataStream);

            UpdateInCollection();
        }

		/// <summary>
		/// The SHA1 encrypted hash of the datastream.
		/// </summary>
		/// <remarks>
		/// Usually the application programmer don't need to set this itself.
		/// </remarks>
		public string Sha
		{
			get { return sha; }
			set { sha = value; UpdateInCollection(); }
		}

		/// <summary>
		/// Updates the msn object in the global MSNObjectCatalog.
		/// </summary>
		public void UpdateInCollection()
		{
			if(oldHash.Length != 0)
				MSNObjectCatalog.GetInstance().Remove(oldHash);

			oldHash = Context;

			MSNObjectCatalog.GetInstance().Add(oldHash, this);
		}

		/// <summary>
		/// Calculates the hash of datastream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		protected string GetStreamHash(Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);

			// fet file hash
			byte[] bytes=new byte[(int)stream.Length];

			//put bytes into byte array
			stream.Read(bytes,0, (int)stream.Length); 

			//create SHA1 object 
			HashAlgorithm hash = new SHA1Managed();			
			byte[] hashBytes = hash.ComputeHash(bytes);

			return Convert.ToBase64String(hashBytes);
		}
				
		private static Regex contextRe = new Regex("(?<Name>[^= ]+)=\"(?<Value>[^\"]+)\"");

		/// <summary>
		/// Parses a context send by the remote contact and set the corresponding class variables. Context input is assumed to be not base64 encoded.
		/// </summary>
		/// <param name="context"></param>
		public virtual void ParseContext(string context)
		{
			ParseContext(context, false);
		}

		/// <summary>
		/// Parses a context send by the remote contact and set the corresponding class variables.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="base64Encoded"></param>
		public virtual void ParseContext(string context, bool base64Encoded)
		{
			originalContext = context;

			if(base64Encoded)
				context = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(context));
			
			string xmlString = System.Web.HttpUtility.UrlDecode(context);
			MatchCollection matches  = contextRe.Matches(xmlString);
			
			foreach(Match match in matches)
			{
				string name = match.Groups["Name"].Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				string val  = match.Groups["Value"].Value;

				switch(name)
				{
					case "creator": this.creator = val; break;
					case "size"   : this.size = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture); break;
					case "type"   :
					{						
						switch(val)
						{
							case "2": type = MSNObjectType.Emoticon; break;
							case "3": type = MSNObjectType.UserDisplay; break;
							case "5": type = MSNObjectType.Background; break;
							case "8": type = MSNObjectType.Wink; break;
						}
						break;
					}
					case "location":this.location = val; break;
					case "sha1d": this.sha = val; break;
					case "friendly": this.friendly = UTF8Encoding.UTF8.GetString(Convert.FromBase64String (val)); break;
					//case "friendly": this.friendly =  val; break;
				}
			}
		}
		
		/// <summary>
		/// Calculates the checksum for the entire MSN Object.
		/// </summary>
		/// <remarks>This value is used to uniquely identify a MSNObject.</remarks>
		/// <returns></returns>
		public string CalculateChecksum()
		{
			string checksum = "Creator"+Creator+"Size"+Size+"Type"+(int)this.Type+"Location"+Location+"Friendly"+Friendly+"SHA1D"+Sha;

			HashAlgorithm shaAlg = new SHA1Managed();			
			string baseEncChecksum = Convert.ToBase64String(shaAlg.ComputeHash(Encoding.ASCII.GetBytes(checksum)));
			return baseEncChecksum;
		}

		/// <summary>
		/// The context as an url-encoded xml string.
		/// </summary>
		public string Context
		{
			get { return GetEncodedString(); }
			set { ParseContext(value);  }
		}

		/// <summary>
		/// The context as an xml string, not url-encoded.
		/// </summary>
		public string ContextPlain
		{
			get { return GetXmlString(); }			
		}
		
		/// <summary>
		/// Returns the xml string.
		/// </summary>
		/// <returns></returns>
		protected virtual string GetXmlString()
		{
			return String.Format ("<msnobj Creator=\"{0}\" Size=\"{1}\" Type=\"{2}\" Location=\"{3}\" Friendly=\"{4}\" SHA1D=\"{5}\" SHA1C=\"{6}\"/>",
			                         Creator,
			                         Size,
			                         (int)Type,
			                         Location,
			                         Convert.ToBase64String (Encoding.UTF8.GetBytes (Friendly)),
			                         Sha.Replace (' ', '+'), //this is needed, it's not on the docs, but it has, trust me!
			                         CalculateChecksum ());
		}

		/// <summary>
		/// Returns the url-encoded xml string.
		/// </summary>
		/// <returns></returns>
		protected virtual string GetEncodedString()
		{			
			return HttpUtility.UrlPathEncode(GetXmlString());
		}
	}


	/// <summary>
	/// A collection of all available MSN objects. This class is implemented following the singleton pattern.
	/// </summary>
	/// <remarks>
	/// In this collection all user display's, emoticons, etc for the entire application are stored.
	/// This allows for easy retrieval of the corresponding msn object by passing in the encrypted hash.
	/// Note: Use <see cref="GetInstance"/> to get a reference to the global MSNObjectCatalog object on which you can call methods.
	/// </remarks>
	[Serializable()]
	public class MSNObjectCatalog : ICollection
	{
		/// <summary>
		/// The single instance
		/// </summary>
		[NonSerialized]
		private static MSNObjectCatalog instance = new MSNObjectCatalog();

		/// <summary>
		/// Collection of all msn objects
		/// </summary>
		private Hashtable	objectCollection = new Hashtable();

		/// <summary>
		/// Returns the msn object with the supplied hash as checksum.
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		public MSNObject Get(string hash)
		{
			object msnObject = objectCollection[hash];
			if(msnObject == null)
				return null;
			else
				return (MSNObject)msnObject;
		}

		/// <summary>
		/// Removes the msn object with the specified checksum from the collection.
		/// </summary>
		/// <param name="checksum"></param>
		public void	Remove(string checksum)
		{
			objectCollection.Remove(checksum);
		}

		/// <summary>
		/// Removes the specified msn object from the collection.
		/// </summary>
		/// <param name="msnObject"></param>
		public void	Remove(MSNObject msnObject)
		{
			objectCollection.Remove(msnObject.CalculateChecksum());
		}

		/// <summary>
		/// Adds the MSNObject (a user display, emoticon, etc) in the global collection.		
		/// </summary>
		/// <param name="msnObject"></param>
		public void	Add(MSNObject msnObject)
		{
			string hash = msnObject.CalculateChecksum(); 
			Add(hash, msnObject);
		}

		/// <summary>
		/// Adds the MSNObject (a user display, emoticon, etc) in the global collection, with the specified checksum as index.
		/// </summary>
		/// <param name="checksum"></param>
		/// <param name="msnObject"></param>
		public void	Add(string checksum, MSNObject msnObject)
		{
			objectCollection[checksum] = msnObject;
		}

		/// <summary>
		/// Returns a reference to the global MSNObjectCatalog object.
		/// </summary>
		public static MSNObjectCatalog GetInstance()
		{
			return instance;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		private MSNObjectCatalog()
		{
		}

		#region ICollection Members

		/// <summary>
		/// Returns false,because ObjectCatalog is by default not synchronized.
		/// </summary>
		public bool IsSynchronized
		{
			get
			{				
				return false;
			}
		}

		/// <summary>
		/// The number of objects in the catalog.
		/// </summary>
		public int Count
		{
			get
			{				
				return objectCollection.Count;
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(Array array, int index)
		{
			objectCollection.CopyTo(array, index);
		}

		/// <summary>
		/// </summary>
		public object SyncRoot
		{
			get
			{				
				return null;
			}
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{			
			return objectCollection.GetEnumerator();
		}

		#endregion
	}
}
