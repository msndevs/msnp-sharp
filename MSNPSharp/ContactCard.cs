using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;

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
        private Image displayImage = Properties.Resources.owner;
        private ContactCardItem space = null;
        private List<ThumbnailImage> photos = new List<ThumbnailImage>(0);
        private ContactCardItem newPost = null;
        private Dictionary<ProfileType, ProfileItem> profiles = new Dictionary<ProfileType, ProfileItem>(0);

        #endregion

        #region Properties
        /// <summary>
        /// Display name of the contact.
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
        }


        public string Mail
        {
            get { return mail; }
        }

        /// <summary>
        /// The display image of contact that stored in the space.
        /// </summary>
        public Image DisplayImage
        {
            get { return displayImage; }
        }


        /// <summary>
        /// The space title element.
        /// </summary>
        public ContactCardItem Space
        {
            get { return space; }
        }

        /// <summary>
        /// Latest photos uploaded to the photo album.
        /// </summary>
        public List<ThumbnailImage> Photos
        {
            get { return photos; }
        }

        /// <summary>
        /// The latest blog post in the space.
        /// </summary>
        public ContactCardItem NewPost
        {
            get { return newPost; }
        }

        /// <summary>
        /// The profiles of the contact.
        /// </summary>
        public Dictionary<ProfileType, ProfileItem> Profiles
        {
            get { return profiles; }
        }
        #endregion

        public ContactCard(string displayname, string account, ContactCardItem spacetitle)
        {
            displayName = displayname;
            mail = account;
            space = spacetitle;
        }

        internal void SetBlogPost(ContactCardItem value)
        {
            newPost = value;
        }
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
            get { return _url; }
        }

        /// <summary>
        /// New item description.
        /// <list type="bullet">
        /// <item>For photo objects, this property is file description.</item>
        /// <item>For blog post objects, this property is the content of new bolg.</item>
        /// <item>For space and profile objects, this property is set to null.</item>
        /// </list>
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// <list type="bullet">
        /// <item>For photo objects, this property is filename.</item>
        /// <item>For space objects, this property is a space title.</item>
        /// <item>For blog post objects, this property is title of a new bolg.</item>
        /// </list>
        /// </summary>
        public string Title
        {
            get { return _title; }
        }

        /// <summary>
        /// Tooltip shown for the new object.
        /// <list type="bullet">
        /// <item>For space objects, this property is set to null.</item>
        /// </list>
        /// </summary>
        public string ToolTip
        {
            get { return _toolTip; }
        } 
        #endregion

        protected ContactCardItem() { }
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
    public class ThumbnailImage:ContactCardItem
    {
        private string albumName = string.Empty;
        private Image image = null;

        #region Properties

        /// <summary>
        /// Indicates which album is this image in.
        /// </summary>
        public string AlbumName
        {
            get { return albumName; }
        }

        /// <summary>
        /// Thumbnail image
        /// </summary>
        public Image Image
        {
            get { return image; }
        } 
        #endregion

        public ThumbnailImage(string url, Image img, string album, string title, string desc, string tooltip)
            : base(url, desc, title, tooltip)
        {
            image = img;
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
    /// Profile element of contactcard.
    /// </summary>
    public class ProfileItem : ContactCardItem
    {
        private bool hasUpdated = false;

        public bool HasUpdated
        {
            get { return hasUpdated; }
        }

        public ProfileItem(bool updated, string url, string title, string tooltip)
            : base(url, null, title, tooltip)
        {
            hasUpdated = updated;
        }
    }
}
