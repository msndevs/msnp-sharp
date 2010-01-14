#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.IO;
using System.Web;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;     

    /// <summary>
    /// Defines the type of MSNObject.
    /// <para>Thanks for ZoroNaX : http://zoronax.spaces.live.com/blog/cns!4A0B813054895814!180.entry </para>
    /// </summary>
    public enum MSNObjectType
    {
        /// <summary>
        /// Unknown msnobject type.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Avatar, Unknown
        /// </summary>
        Avatar = 1,
        /// <summary>
        /// Emotion icon.
        /// </summary>
        Emoticon = 2,
        /// <summary>
        /// User display image.
        /// </summary>
        UserDisplay = 3,
        /// <summary>
        /// ShareFile, Unknown
        /// </summary>
        ShareFile = 4,
        /// <summary>
        /// Background image.
        /// </summary>
        Background = 5,
        /// <summary>
        /// History
        /// </summary>
        History = 6,
        /// <summary>
        /// Deluxe Display Pictures
        /// </summary>
        DynamicPicture = 7,
        /// <summary>
        /// flash emoticon
        /// </summary>
        Wink = 8,
        /// <summary>
        /// Map File  A map file contains a list of items in the store.
        /// </summary>
        MapFile = 9,
        /// <summary>
        /// Dynamic Backgrounds
        /// </summary>
        DynamicBackground = 10,
        /// <summary>
        /// Voice Clip
        /// </summary>
        VoiceClip = 11,
        /// <summary>
        /// Plug-In State. Saved state of Add-ins.
        /// </summary>
        SavedState = 12,
        /// <summary>
        /// Roaming Objects. For example, your roaming display picture.
        /// </summary>
        RoamingObject = 13,
        /// <summary>
        /// Signature sound
        /// </summary>
        SignatureSound = 14
    }

    /// <summary>
    /// The MSNObject can hold an image, display, emoticon, etc.
    /// </summary>
    [Serializable()]
    public class MSNObject
    {
        private string originalContext;
        private string oldHash = String.Empty;

        [NonSerialized]
        private PersistentStream dataStream;


        private string fileLocation;
        private string creator;
        private int size;
        private MSNObjectType type;
        private string location;
        private string sha;

        /// <summary>
        /// The datastream to write to, or to read from
        /// </summary>
        protected PersistentStream DataStream
        {
            get
            {
                return dataStream;
            }
            set
            {
                dataStream = value;
            }
        }

        /// <summary>
        /// The local contact list owner
        /// </summary>
        public string Creator
        {
            get
            {
                return creator;
            }
            set
            {
                creator = value;
                UpdateInCollection();
            }
        }

        /// <summary>
        /// The original context string that was send by the remote contact
        /// </summary>
        public string OriginalContext
        {
            get
            {
                return originalContext;
            }
        }

        /// <summary>
        /// The total data size
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                UpdateInCollection();
            }
        }

        /// <summary>
        /// The type of MSN Object
        /// </summary>
        public MSNObjectType ObjectType
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
                UpdateInCollection();
            }
        }

        /// <summary>
        /// The location of the object. This is a location on the hard-drive. Use relative paths. This is only a text string; na data is read in after setting this field. Use FileLocation for that purpose.
        /// </summary>
        public string Location
        {
            get
            {
                return location;
            }
            set
            {
                location = value;
                UpdateInCollection();
            }
        }

        /// <summary>
        /// [Deprecated, use LoadFile()] Gets or sets the file location. When a file is set the file data is immediately read in memory to extract the filehash. It will retain in memory afterwards.
        /// </summary>        
        public string FileLocation
        {
            get
            {
                return fileLocation;
            }
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
            FileInfo finfo = new FileInfo(fileName);
            if (finfo.Exists)
            {
                if (this.fileLocation == fileName)
                    return;
                this.fileLocation = fileName;
                this.location = Path.GetRandomFileName();

                // close any current datastreams. One is always created in the constructor
                if (dataStream != null)
                    dataStream.Close();

                // and open a new stream
                dataStream = new PersistentStream(new MemoryStream());

                // copy the file
                byte[] buffer = new byte[512];
                Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                int cnt = 0;
                while ((cnt = fileStream.Read(buffer, 0, 512)) > 0)
                {
                    dataStream.Write(buffer, 0, cnt);
                }

                this.size = (int)dataStream.Length;
                this.sha = GetStreamHash(dataStream);

                UpdateInCollection();
            }
        }

        /// <summary>
        /// The SHA1 encrypted hash of the datastream.
        /// </summary>
        /// <remarks>
        /// Usually the application programmer don't need to set this itself.
        /// </remarks>
        public string Sha
        {
            get
            {
                return sha;
            }
            set
            {
                sha = value;
                UpdateInCollection();
            }
        }

        /// <summary>
        /// Updates the msn object in the global MSNObjectCatalog.
        /// </summary>
        public void UpdateInCollection()
        {
            if (oldHash.Length != 0)
                MSNObjectCatalog.GetInstance().Remove(oldHash);

            oldHash = CalculateChecksum();

            MSNObjectCatalog.GetInstance().Add(oldHash, this);
        }

        /// <summary>
        /// Calculates the hash of datastream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected static string GetStreamHash(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // fet file hash
            byte[] bytes = new byte[(int)stream.Length];

            //put bytes into byte array
            stream.Read(bytes, 0, (int)stream.Length);

            //create SHA1 object 
            HashAlgorithm hash = new SHA1Managed();
            byte[] hashBytes = hash.ComputeHash(bytes);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Creates a MSNObject.
        /// </summary>		
        public MSNObject()
        {
            DataStream = new PersistentStream(new MemoryStream());
        }

        /// <summary>
        /// 
        /// </summary>
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

            if (base64Encoded)
                context = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(context));

            string xmlString = context;
            if (context.IndexOf(" ") == -1)
                xmlString = GetDecodeString(context);
            MatchCollection matches = contextRe.Matches(xmlString);

            foreach (Match match in matches)
            {
                string name = match.Groups["Name"].Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                string val = match.Groups["Value"].Value;

                switch (name)
                {
                    case "creator":
                        this.creator = val;
                        break;
                    case "size":
                        this.size = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "type":
                        {
                            switch (val)
                            {
                                case "2":
                                    type = MSNObjectType.Emoticon;
                                    break;
                                case "3":
                                    type = MSNObjectType.UserDisplay;
                                    break;
                                case "5":
                                    type = MSNObjectType.Background;
                                    break;
                                case "8":
                                    type = MSNObjectType.Wink;
                                    break;
                            }
                            break;
                        }
                    case "location":
                        this.location = val;
                        break;
                    case "sha1d":
                        this.sha = val;
                        break;
                }
            }
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
            this.size = (int)inputStream.Length;
            this.type = type;
            this.location = location;// + new Random().Next().ToString();

            this.sha = GetStreamHash(inputStream);

            this.DataStream = new PersistentStream(inputStream);
        }

        /// <summary>
        /// Constructs a MSN object based on a physical file. The client programmer is responsible for inserting this object in the global msn object collection.
        /// </summary>
        /// <param name="creator"></param>
        /// <param name="type"></param>
        /// <param name="fileLocation"></param>
        public MSNObject(string creator, string fileLocation, MSNObjectType type)
        {
            this.location = Path.GetFullPath(fileLocation).Replace(Path.GetPathRoot(fileLocation), "");
            this.location += new Random().Next().ToString(CultureInfo.InvariantCulture);

            this.fileLocation = fileLocation;

            Stream stream = OpenStream();

            this.creator = creator;
            this.size = (int)stream.Length;
            this.type = type;

            this.sha = GetStreamHash(stream);
            stream.Close();
        }

        /// <summary>
        /// Returns the stream to read from. In case of an in-memory stream that stream is returned. In case of a filelocation
        /// a stream to the file will be opened and returned. The stream is not guaranteed to positioned at the beginning of the stream.
        /// </summary>
        /// <returns></returns>
        public virtual Stream OpenStream()
        {
            /*if(dataStream == null)
            {
                if(fileLocation != null && this.fileLocation.Length > 0)
                    dataStream = new PersistentStream(new FileStream(fileLocation, FileMode.Open, FileAccess.Read));
                else
                    throw new MSNPSharpException("No memorystream or filestream available to open in a MSNObject context.");
            }
            else*/
            dataStream.Open();

            // otherwise it's a memorystream
            return dataStream;
        }

        /// <summary>
        /// Returns the "url-encoded xml" string for MSNObjects.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual string GetEncodeString(string context)
        {
            return MSNHttpUtility.MSNObjectUrlEncode(context);
        }

        public static string GetDecodeString(string context)
        {
            if (context.IndexOf(" ") == -1)
            {
                return MSNHttpUtility.MSNObjectUrlDecode(context);
            }
            return context;
        }


        /// <summary>
        /// Calculates the checksum for the entire MSN Object.
        /// </summary>
        /// <remarks>This value is used to uniquely identify a MSNObject.</remarks>
        /// <returns></returns>
        public string CalculateChecksum()
        {
            string checksum = "Creator" + Creator + "Size" + Size + "Type" + (int)this.ObjectType + "Location" + Location + "FriendlyAAA=SHA1D" + Sha;

            HashAlgorithm shaAlg = new SHA1Managed();
            string baseEncChecksum = Convert.ToBase64String(shaAlg.ComputeHash(Encoding.UTF8.GetBytes(checksum)));
            return baseEncChecksum;
        }

        /// <summary>
        /// The context as an url-encoded xml string.
        /// </summary>
        public string Context
        {
            get
            {
                return GetEncodedString();
            }
            set
            {
                ParseContext(value);
            }
        }

        /// <summary>
        /// The context as an xml string, not url-encoded.
        /// </summary>
        public string ContextPlain
        {
            get
            {
                return GetXmlString();
            }
        }

        /// <summary>
        /// Returns the xml string.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetXmlString()
        {
            return "<msnobj Creator=\"" + Creator + "\" Size=\"" + Size + "\" Type=\"" + (int)this.ObjectType + "\" Location=\"" + Location + "\" Friendly=\"AAA=\" SHA1D=\"" + Sha + "\" SHA1C=\"" + CalculateChecksum() + "\"/>";
        }

        /// <summary>
        /// Returns the url-encoded xml string.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetEncodedString()
        {
            return MSNHttpUtility.MSNObjectUrlEncode(GetXmlString());
        }

        public static bool operator == (MSNObject obj1, MSNObject obj2)
        {
            if (((object)obj1) == null && ((object)obj2) == null)
                return true;
            if (((object)obj1) == null || ((object)obj2) == null)
                return false;
            return obj1.GetHashCode() == obj2.GetHashCode();
        }

        public static bool operator != (MSNObject obj1, MSNObject obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return CalculateChecksum().GetHashCode();
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
        private Hashtable objectCollection = new Hashtable();

        /// <summary>
        /// Returns the msn object with the supplied hash as checksum.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public MSNObject Get(string hash)
        {
            return (objectCollection.ContainsKey(hash)) ? (MSNObject)objectCollection[hash] : null;
        }

        /// <summary>
        /// Removes the msn object with the specified checksum from the collection.
        /// </summary>
        /// <param name="checksum"></param>
        public void Remove(string checksum)
        {
            objectCollection.Remove(checksum);
        }

        /// <summary>
        /// Removes the specified msn object from the collection.
        /// </summary>
        /// <param name="msnObject"></param>
        public void Remove(MSNObject msnObject)
        {
            objectCollection.Remove(msnObject.CalculateChecksum());
        }

        /// <summary>
        /// Adds the MSNObject (a user display, emoticon, etc) in the global collection.		
        /// </summary>
        /// <param name="msnObject"></param>
        public void Add(MSNObject msnObject)
        {
            string hash = msnObject.CalculateChecksum();
            Add(hash, msnObject);
        }

        /// <summary>
        /// Adds the MSNObject (a user display, emoticon, etc) in the global collection, with the specified checksum as index.
        /// </summary>
        /// <param name="checksum"></param>
        /// <param name="msnObject"></param>
        public void Add(string checksum, MSNObject msnObject)
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
};
