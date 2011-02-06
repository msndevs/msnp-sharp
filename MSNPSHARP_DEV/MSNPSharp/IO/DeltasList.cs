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
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.Core;
    using System.Drawing;

    /// <summary>
    /// Storage class for deltas request
    /// </summary>
    [Serializable]
    public class DeltasList : MCLSerializer
    {
        private const int MaxSlot = 1000;

        private SerializableDictionary<CacheKeyType, string> cacheKeys = new SerializableDictionary<CacheKeyType, string>(0);
        private SerializableDictionary<string, string> preferredHosts = new SerializableDictionary<string, string>(0);
        private SerializableDictionary<string, byte[]> userTileSlots = new SerializableDictionary<string, byte[]>(MaxSlot);
        private SerializableDictionary<string, uint> visitCount = new SerializableDictionary<string, uint>(MaxSlot);
        private SerializableDictionary<string, string> userImageRelationships = new SerializableDictionary<string, string>();

        private SerializableDictionary<string, string> userSceneRelationships = new SerializableDictionary<string, string>();
        private SerializableDictionary<string, byte[]> userSceneSlots = new SerializableDictionary<string, byte[]>(MaxSlot);
        private SerializableDictionary<string, uint> visitCountScene = new SerializableDictionary<string, uint>(MaxSlot);

        public SerializableDictionary<string, string> UserImageRelationships
        {
            get { return userImageRelationships; }
            set { userImageRelationships = value; }
        }

        public SerializableDictionary<string, string> UserSceneRelationships
        {
            get { return userSceneRelationships; }
            set { userSceneRelationships = value; }
        }


        public SerializableDictionary<string, uint> VisitCount
        {
            get { return visitCount; }
            set { visitCount = value; }
        }

        public SerializableDictionary<string, uint> VisitCountScene
        {
            get { return visitCountScene; }
            set { visitCountScene = value; }
        }

        /// <summary>
        /// Data structure that stores the user's display images.
        /// </summary>
        public SerializableDictionary<string, byte[]> UserTileSlots
        {
            get { return userTileSlots; }
            set { userTileSlots = value; }
        }

        /// <summary>
        /// Data structure that stores the user's scene images.
        /// </summary>
        public SerializableDictionary<string, byte[]> UserSceneSlots
        {
            get { return userSceneSlots; }
            set { userSceneSlots = value; }
        }

        /// <summary>
        /// CacheKeys for webservices.
        /// </summary>
        public SerializableDictionary<CacheKeyType, string> CacheKeys
        {
            get
            {
                return cacheKeys;
            }
            set
            {
                cacheKeys = value;
            }
        }

        /// <summary>
        /// Preferred hosts for different methods.
        /// </summary>
        public SerializableDictionary<string, string> PreferredHosts
        {
            get
            {
                return preferredHosts;
            }
            set
            {
                preferredHosts = value;
            }
        }

        #region Profile
        private OwnerProfile profile = new OwnerProfile();

        /// <summary>
        /// Profile of current user.
        /// </summary>
        [XmlElement("Profile")]
        public OwnerProfile Profile
        {
            get
            {
                return profile;
            }
            set
            {
                profile = value;
            }
        }

        #endregion

        #region Private methods

        private SerializableDictionary<string, uint> GetVisit(bool isDisplayImage)
        {
            return isDisplayImage ? VisitCount : VisitCountScene;
        }

        private SerializableDictionary<string, string> GetRelationShip(bool isDisplayImage)
        {
            return isDisplayImage ? UserImageRelationships : UserSceneRelationships;
        }

        private SerializableDictionary<string, byte[]> GetTile(bool isDisplayImage)
        {
            return isDisplayImage ? UserTileSlots : UserSceneSlots;
        }

        private bool HasRelationship(string siblingAccount, SerializableDictionary<string, string> dictRelation)
        {
            lock (dictRelation)
                return dictRelation.ContainsKey(siblingAccount.ToLowerInvariant());
        }

        private bool HasImage(string imageKey, SerializableDictionary<string, byte[]> dictImage)
        {
            lock (dictImage)
                return dictImage.ContainsKey(imageKey);
        }

        private bool HasRelationshipAndImage(string siblingAccount, out string imageKey, bool isDisplayImage)
        {
            imageKey = string.Empty;
            if (!HasRelationship(siblingAccount, GetRelationShip(isDisplayImage)))
            {
                return false;
            }

            string imgKey = string.Empty;
            lock (GetRelationShip(isDisplayImage))
                imgKey = GetRelationShip(isDisplayImage)[siblingAccount.ToLowerInvariant()];

            if (!HasImage(imgKey, GetTile(isDisplayImage)))
                return false;

            imageKey = imgKey;
            return true;
        }

        private void AddImage(string imageKey, byte[] data, SerializableDictionary<string, byte[]> dictImage)
        {
            if (HasImage(imageKey, dictImage))
                return;

            lock (dictImage)
                dictImage[imageKey] = data;
        }

        private void AddRelationship(string siblingAccount, string imageKey, SerializableDictionary<string, string> dictRelation)
        {
            lock (dictRelation)
                dictRelation[siblingAccount.ToLowerInvariant()] = imageKey;
        }

        private void AddImageAndRelationship(string siblingAccount, string imageKey, byte[] data, bool isDisplayImage)
        {
            AddImage(imageKey, data, GetTile(isDisplayImage));
            AddRelationship(siblingAccount, imageKey, GetRelationShip(isDisplayImage));
        }

        private bool RemoveImage(string imageKey, bool isDisplayImage)
        {
            bool noerror = true;

            noerror |= GetTile(isDisplayImage).Remove(imageKey);

            lock (GetVisit(isDisplayImage))
                noerror |= GetVisit(isDisplayImage).Remove(imageKey);

            lock (GetRelationShip(isDisplayImage))
            {
                Dictionary<string, string> cp = new Dictionary<string, string>(GetRelationShip(isDisplayImage));
                foreach (string account in cp.Keys)
                {
                    if (cp[account] == imageKey)
                        GetRelationShip(isDisplayImage).Remove(account);
                }
            }

            return noerror;
        }

        private bool RemoveRelationship(string siblingAccount)
        {
            if (!HasRelationship(siblingAccount, UserImageRelationships))
                return false;

            lock (UserImageRelationships)
            {
                string key = UserImageRelationships[siblingAccount];
                UserImageRelationships.Remove(siblingAccount);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageKey"></param>
        /// <param name="isDisplayImage"></param>
        /// <remarks>This function does NOT examine whether the correspondent slot does exist.</remarks>
        private uint IncreaseVisitCount(string imageKey, bool isDisplayImage)
        {
            lock (GetVisit(isDisplayImage))
            {
                if (!GetVisit(isDisplayImage).ContainsKey(imageKey))
                    GetVisit(isDisplayImage)[imageKey] = 0;

                return ++GetVisit(isDisplayImage)[imageKey];
            }
        }

        private uint DecreaseVisitCount(string imageKey)
        {
            lock (VisitCount)
            {
                if (!VisitCount.ContainsKey(imageKey))
                    return uint.MinValue;

                if (VisitCount[imageKey] > 0)
                {
                    return --VisitCount[imageKey];
                }
            }

            //error
            return 0;
        }

        private bool GetLeastVisitImage(out string imageKey, bool isDisplayImage)
        {
            imageKey = string.Empty;
            lock (GetVisit(isDisplayImage))
            {
                if (GetVisit(isDisplayImage).Count == 0)
                    return false;
                uint minValue = uint.MaxValue;
                uint maxValue = 0;
                ulong sum = 0;

                string lastKey = string.Empty;

                foreach (string key in GetVisit(isDisplayImage).Keys)
                {
                    if (GetVisit(isDisplayImage)[key] <= minValue)
                    {
                        minValue = GetVisit(isDisplayImage)[key];
                        lastKey = key;
                    }

                    if (GetVisit(isDisplayImage)[key] >= maxValue)
                        maxValue = GetVisit(isDisplayImage)[key];

                    sum += GetVisit(isDisplayImage)[key];
                }

                if (string.IsNullOrEmpty(lastKey))
                    return false;

                imageKey = lastKey;
                if (maxValue == uint.MaxValue)  //Prevent overflow.
                {
                    uint avg = (uint)(sum / (ulong)GetVisit(isDisplayImage).Count);
                    if (avg == uint.MaxValue)
                        avg = 0;

                    Dictionary<string, uint> cp = new Dictionary<string, uint>(GetVisit(isDisplayImage));
                    foreach (string imgKey in cp.Keys)
                    {
                        if (cp[imgKey] == uint.MaxValue)
                            GetVisit(isDisplayImage)[imgKey] = avg;
                    }
                }

                return true;
            }
        }

        #endregion

        #region Internal Methods

        internal bool HasImage(string siblingAccount, string imageKey, bool isDisplayImage)
        {
            if (imageKey != null)
            {
                if (HasImage(imageKey, GetTile(isDisplayImage)))
                {
                    AddRelationship(siblingAccount, imageKey, GetRelationShip(isDisplayImage));
                    return true;
                }
            }

            return false;
        }

        internal byte[] GetRawImageDataBySiblingString(string siblingAccount, out string imageKey, bool isDisplayImage)
        {
            imageKey = string.Empty;
            if (HasRelationshipAndImage(siblingAccount, out imageKey, isDisplayImage))
            {
                lock (GetTile(isDisplayImage))
                {
                    IncreaseVisitCount(imageKey, isDisplayImage);
                    return GetTile(isDisplayImage)[imageKey];
                }
            }

            return null;
        }

        internal bool SaveImageAndRelationship(string siblingAccount, string imageKey, byte[] userTile, bool isDisplayImage)
        {
            lock (GetTile(isDisplayImage))
            {
                if (GetTile(isDisplayImage).Count == MaxSlot)
                {
                    //The heaven is full.
                    string deleteKey = string.Empty;
                    if (GetLeastVisitImage(out deleteKey, isDisplayImage))
                    {
                        RemoveImage(deleteKey, isDisplayImage);
                    }
                    else
                    {
                        //OMG no one want to give a place?
                        return false;
                    }
                }

                AddImageAndRelationship(siblingAccount, imageKey, userTile, isDisplayImage);
            }

            return true;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Empty all of the lists
        /// </summary>
        public void Empty()
        {

        }

        /// <summary>
        /// Truncate file. This is useful after calling of Addressbook.Save
        /// </summary>
        public void Truncate()
        {
            Empty();
            Save(true);
        }

        public static DeltasList LoadFromFile(string filename, MclSerialization st, NSMessageHandler handler, bool useCache)
        {
            return (DeltasList)LoadFromFile(filename, st, typeof(DeltasList), handler, useCache);
        } 

        #endregion

        #region Overrides

        /// <summary>
        /// Save the <see cref="DeltasList"/> into a specified file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            FileName = filename;
            Save(false);
        }

        public override void Save()
        {
            Save(false);
        }

        public void Save(bool saveImmediately)
        {
            Version = Properties.Resources.DeltasListVersion;

            if (saveImmediately == false &&
                File.Exists(FileName) &&
                File.GetLastWriteTime(FileName) > DateTime.Now.AddSeconds(-5))
            {
                return;
            }

            base.Save(FileName);
        }

        #endregion
    }
};
