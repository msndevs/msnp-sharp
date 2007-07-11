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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Web;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharp
{
	
	public enum MSNObjectType : uint
	{
		Unknown = 0,
		Emoticon = 2,
		UserDisplay = 3,
		Background = 5,
		DinamicDisplayPic = 7,
		Wink = 8,
		VoiceClip = 11,
		SavedState = 12,
		Location = 14
	}	 

	// The MSNObject can hold an image, display, emoticon, etc.
	[Serializable()]
	public class MSNObject
	{
		string originalContext = null;
		string oldHash = "";
		[NonSerialized]
		Stream dataStream = null;		

		string fileLocation = null;
		string creator;
		long size;
		MSNObjectType type;
		string location;		 
		string friendly = "\0";		 
		string sha = String.Empty;
		
		static Regex contextRe = new Regex("(?<Name>[^= ]+)=\"(?<Value>[^\"]+)\"");

		public MSNObject()
		{			
			
		}
		
		public MSNObject(string creator, Stream inputStream, MSNObjectType type, string location)
		{						
			this.creator = creator;
			this.size = inputStream.Length;
			this.type = type;
			this.location = location;
			
			this.sha = GetStreamHash(inputStream);

			this.DataStream = inputStream;
		}
		
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

		public string Creator
		{
			get {
				return creator;
			}
			set {
				creator = value;
				UpdateInCollection();
			}
		}

		public string Friendly
		{
			get {
				return friendly;
			}
			set {
				friendly = value;
				UpdateInCollection();
			}
		}
		
		public string OriginalContext
		{
			get {
				return originalContext;
			}
		}

		public long Size
		{
			get {
				return size;
			}
			set {
				size = value;
				UpdateInCollection();
			}
		}

		public MSNObjectType Type
		{
			get {
				return type;
			}
			set {
				type = value; 
				UpdateInCollection();
			}
		}

		public string Location
		{
			get {
				return location;
			}
			set {
				location = value; 
				UpdateInCollection();
			}
		}

		public string FileLocation
		{
			get {
				return fileLocation;
			}
			set 
			{ 
				this.LoadFile(value);
				fileLocation = value;
			}
		}

		public void LoadFile(string fileName)
		{
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

			this.size = DataStream.Length;
			this.sha = GetStreamHash(DataStream);

			UpdateInCollection();
		}

		public string Sha
		{
			get {
				return sha;
			}
			set {
				sha = value; 
				UpdateInCollection();
			}
		}

		public void UpdateInCollection()
		{
			if(oldHash.Length != 0)
				MSNObjectCatalog.GetInstance().Remove(oldHash);

			oldHash = Context;

			MSNObjectCatalog.GetInstance().Add(oldHash, this);
		}

		protected string GetStreamHash(Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);

			// fet file hash
			byte[] bytes = new byte[(int)stream.Length];

			//put bytes into byte array
			stream.Read(bytes, 0, (int)stream.Length); 

			//create SHA1 object 
			HashAlgorithm hash = new SHA1Managed();			
			byte[] hashBytes = hash.ComputeHash(bytes);

			return UTF8Encoding.UTF8.GetString (hashBytes);
		}
				
		public virtual void ParseContext(string context)
		{
			ParseContext(context, false);
		}

		public virtual void ParseContext(string context, bool base64Encoded)
		{
			originalContext = context;

			if(base64Encoded)
				context = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(context));
				
			string xmlString = HttpUtility.UrlDecode(context, Encoding.UTF8);
			
			MatchCollection matches  = contextRe.Matches(xmlString);
			
			foreach(Match match in matches)
			{
				string name = match.Groups["Name"].Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				string val  = match.Groups["Value"].Value;

				switch(name)
				{
					case "creator": this.creator = val; break;
					case "size"   : this.size = long.Parse(val, System.Globalization.CultureInfo.InvariantCulture); break;
					case "type"   : 
						try
						{
							this.type = (MSNObjectType) uint.Parse (val);
						} 
						catch (Exception) {}
						break;
					case "location":this.location = val; break;
					case "sha1d": this.sha = UTF8Encoding.UTF8.GetString (Convert.FromBase64String (val));; break;
					case "friendly": this.friendly = Encoding.Unicode.GetString (Convert.FromBase64String (val)); break;
				}
			}
		}
		
		public string CalculateChecksum()
		{
			string checksum = "Creator"+Creator+"Size"+Size+"Type"+(int)Type+"Location"+Location;
			checksum += "Friendly"+Convert.ToBase64String (Encoding.Unicode.GetBytes (Friendly));
			checksum += "SHA1D"+Convert.ToBase64String (UTF8Encoding.UTF8.GetBytes (Sha));

			HashAlgorithm shaAlg = new SHA1Managed();		
			
			byte[] hash = shaAlg.ComputeHash(Encoding.UTF8.GetBytes(checksum));
			
			return Convert.ToBase64String(hash);
		}

		public string Context
		{
			get {
				return GetEncodedString();
			}
			set {
				ParseContext(value);
			}
		}

		public string ContextPlain
		{
			get {
				return GetXmlString();
			}
		}
		
		protected virtual string GetXmlString()
		{
			return String.Format ("<msnobj Creator=\"{0}\" Size=\"{1}\" Type=\"{2}\" Location=\"{3}\" Friendly=\"{4}\" SHA1D=\"{5}\" SHA1C=\"{6}\"/>",
			                         Creator,
			                         Size,
			                         (int)Type,
			                         Location,
			                         Convert.ToBase64String (Encoding.UTF8.GetBytes (Friendly)),
			                         Convert.ToBase64String (UTF8Encoding.UTF8.GetBytes (Sha)), 
			                         CalculateChecksum ());
		}

		protected virtual string GetEncodedString()
		{			
			return HttpUtility.UrlEncode(GetXmlString(), Encoding.UTF8).Replace ("+", "%20");
		}
	}
	
	
	[Serializable()]
	public class MSNObjectCatalog : ICollection<MSNObject>
	{
		[NonSerialized]
		static MSNObjectCatalog instance = new MSNObjectCatalog();

		Dictionary<string, MSNObject> objectCollection;

		private MSNObjectCatalog()
		{
			objectCollection = new Dictionary<string, MSNObject> ();
		}
		
		public static MSNObjectCatalog GetInstance()
		{
			return instance;
		}
		
		public MSNObject Get(string hash)
		{
			return objectCollection[hash];
		}

		public bool Remove(string checksum)
		{
			return objectCollection.Remove(checksum);
		}

		public bool Remove(MSNObject msnObject)
		{
			return objectCollection.Remove(msnObject.CalculateChecksum());
		}

		public void	Add(MSNObject msnObject)
		{
			string hash = msnObject.CalculateChecksum();
			Add(hash, msnObject);
		}

		public void	Add(string checksum, MSNObject msnObject)
		{
			objectCollection[checksum] = msnObject;
		}

		#region ICollection Members

		public bool IsSynchronized
		{
			get {
				return false;
			}
		}
		
		public bool IsReadOnly
		{
			get {
				return false;
			}
		}

		public int Count
		{
			get
			{				
				return objectCollection.Count;
			}
		}

		public void CopyTo(MSNObject[] array, int index)
		{
			objectCollection.Values.CopyTo (array, index);
		}
		
		public void Clear ()
		{
			objectCollection.Clear ();
		}
		
		public bool Contains (MSNObject obj)
		{
			return objectCollection.ContainsValue (obj);
		}

		public object SyncRoot
		{
			get
			{				
				return null;
			}
		}

		#endregion

		#region IEnumerable Members
		
		public IEnumerator<MSNObject> GetEnumerator()
		{
			return objectCollection.Values.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
