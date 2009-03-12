#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Xml;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

namespace MSNPSharp
{
    /// <summary>
    /// ContactCard element types
    /// </summary>
    internal enum ContactCardElementType
    {
        SpaceTitle,
        Profile,
        Blog,
        Album
    }

    /// <summary>
    /// SubElement types
    /// </summary>
    internal enum ContactCardSubElementType
    {
        Photo,
        Post,
        GeneralProfile,
        PublicProfile,
        SocialProfile
    }

    public class ContactCard
    {
        #region Fields

        private string displayName = string.Empty;
        private string mail = string.Empty;
        private string displayImageUrl = String.Empty;
        private ContactCardItem space;
        private Album album;
        private ContactCardItem newPost;
        private Dictionary<ProfileType, ProfileItem> profiles = new Dictionary<ProfileType, ProfileItem>(0);

        #endregion

        #region Properties
        /// <summary>
        /// Display name of the contact.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }


        public string Mail
        {
            get
            {
                return mail;
            }
        }

        /// <summary>
        /// The display image of contact that stored in the space.
        /// </summary>
        public string DisplayImageUrl
        {
            get
            {
                return displayImageUrl;
            }
        }


        /// <summary>
        /// The space title element.
        /// </summary>
        public ContactCardItem Space
        {
            get
            {
                return space;
            }
        }

        /// <summary>
        /// Album element of space.
        /// </summary>
        public Album Album
        {
            get
            {
                return album;
            }
        }

        /// <summary>
        /// The latest blog post in the space.
        /// </summary>
        public ContactCardItem NewPost
        {
            get
            {
                return newPost;
            }
        }

        /// <summary>
        /// The profiles of the contact.
        /// </summary>
        public Dictionary<ProfileType, ProfileItem> Profiles
        {
            get
            {
                return profiles;
            }
        }
        #endregion

        public ContactCard(string displayname, string displayimageurl, string account, ContactCardItem spacetitle)
        {
            displayName = displayname;
            displayImageUrl = displayimageurl;
            mail = account;
            space = spacetitle;
        }

        #region Internal setters
        internal void SetBlogPost(ContactCardItem value)
        {
            newPost = value;
        }

        internal void SetAlbum(Album value)
        {
            album = value;
        }

        internal void SetProfiles(Dictionary<ProfileType, ProfileItem> p)
        {
            profiles = p;
        }
        #endregion
    }

    /// <summary>
    /// Base class for contact card element.
    /// </summary>
    public class ContactCardItem
    {
        #region Fields
        private string _url = string.Empty;
        private string _description = string.Empty;
        private string _title = string.Empty;
        private string _toolTip = string.Empty;
        #endregion

        #region Properties

        /// <summary>
        /// Url of the item.
        /// </summary>
        public string Url
        {
            get
            {
                return _url;
            }
        }

        /// <summary>
        /// New item description.
        /// <list type="bullet">
        /// <item>For photo objects, this property is file description.</item>
        /// <item>For blog post objects, this property is the content of new bolg.</item>
        /// <item>For space, album and profile objects, this property is set to null.</item>
        /// </list>
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
        }

        /// <summary>
        /// <list type="bullet">
        /// <item>For photo objects, this property is filename.</item>
        /// <item>For space objects, this property is a space title.</item>
        /// <item>For blog post objects, this property is title of a new bolg.</item>
        /// <item>For album objects, this property is album title.</item>
        /// </list>
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }
        }

        /// <summary>
        /// Tooltip shown for the new object.
        /// <list type="bullet">
        /// <item>For space and album objects, this property is set to null.</item>
        /// </list>
        /// </summary>
        public string ToolTip
        {
            get
            {
                return _toolTip;
            }
        }
        #endregion

        protected ContactCardItem()
        {
        }
        public ContactCardItem(string url, string desc, string title, string tooltip)
        {
            _url = url;
            _description = desc;
            _title = title;
            _toolTip = tooltip;
        }
    }

    /// <summary>
    /// A Thumbnail of space photo.
    /// </summary>
    public class ThumbnailImage : ContactCardItem
    {
        private string albumName = string.Empty;
        private string thumbnailUrl = string.Empty;

        #region Properties

        /// <summary>
        /// Indicates which album is this image in.
        /// </summary>
        public string AlbumName
        {
            get
            {
                return albumName;
            }
        }

        /// <summary>
        /// Thumbnail URL
        /// </summary>
        public string ThumbnailUrl
        {
            get
            {
                return thumbnailUrl;
            }
        }
        #endregion

        public ThumbnailImage(string url, string thumbnailurl, string album, string title, string desc, string tooltip)
            : base(url, desc, title, tooltip)
        {
            thumbnailUrl = thumbnailurl;
            albumName = album;
        }

        #region Internal setters

        internal void SetAlbumName(string name)
        {
            albumName = name;
        }
        #endregion
    }

    /// <summary>
    /// Photo album class.
    /// </summary>
    public class Album : ContactCardItem
    {
        private List<ThumbnailImage> photos = new List<ThumbnailImage>(0);

        /// <summary>
        /// Lastest uploaded photos in the photo album.
        /// </summary>
        public List<ThumbnailImage> Photos
        {
            get
            {
                return photos;
            }
        }

        public Album(string title, string url)
            : base(url, null, title, null)
        {
        }
    }

    /// <summary>
    /// Profile element of contactcard.
    /// </summary>
    public class ProfileItem : ContactCardItem
    {
        private bool hasUpdated = false;

        public bool HasUpdated
        {
            get
            {
                return hasUpdated;
            }
        }

        public ProfileItem(bool updated, string url, string title, string tooltip)
            : base(url, null, title, tooltip)
        {
            hasUpdated = updated;
        }
    }
};
