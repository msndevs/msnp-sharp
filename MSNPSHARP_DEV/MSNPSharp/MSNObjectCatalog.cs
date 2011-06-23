#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    /// <summary>
    /// A collection of all available MSN objects. This class is implemented following the singleton pattern.
    /// </summary>
    /// <remarks>
    /// In this collection all user display's, emoticons, etc for the entire application are stored.
    /// This allows for easy retrieval of the corresponding msn object by passing in the encrypted hash.
    /// Note: Use <see cref="GetInstance"/> to get a reference to the global MSNObjectCatalog object on which you can call methods.
    /// </remarks>
    [Serializable]
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
        private Dictionary<string, MSNObject> objectCollection = new Dictionary<string, MSNObject>();

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
            MSNObject[] msnObjectArray = array as MSNObject[];
            if (msnObjectArray != null)
            {
                objectCollection.Values.CopyTo(msnObjectArray, index);
            }
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
