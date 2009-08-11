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
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.MSNWS.MSNSpaceService;
    using MSNPSharp.MSNWS.MSNABSharingService;

    [Obsolete("Function no more supported by Microsoft", false)]
    public class ContactCardCompletedEventArgs : EventArgs
    {
        private Exception error;
        private bool changed;
        private ContactCard contactCard;

        /// <summary>
        /// InnerException
        /// </summary>
        public Exception Error
        {
            get
            {
                return error;
            }
        }

        /// <summary>
        /// Indicates whether the specified contact has gleams.
        /// </summary>
        public bool Changed
        {
            get
            {
                return changed;
            }
        }

        /// <summary>
        /// The contact card return.
        /// </summary>
        public ContactCard ContactCard
        {
            get
            {
                return contactCard;
            }
        }

        protected ContactCardCompletedEventArgs()
        {
        }

        public ContactCardCompletedEventArgs(bool chg, Exception err, ContactCard cc)
        {
            error = err;
            changed = chg;
            contactCard = cc;
        }
    }

    /// <summary>
    /// Provides services that related to MSN Space.
    /// </summary>
    [Obsolete("Function no more supported by Microsoft", false)]
    public class ContactSpaceService : MSNService
    {
        /// <summary>
        /// Fired after GetContactCard completed its async request.
        /// </summary>
        public event EventHandler<ContactCardCompletedEventArgs> ContactCardCompleted;

        public ContactSpaceService(NSMessageHandler nshandler)
            : base(nshandler)
        {
        }

        [Obsolete("Function no more supported by Microsoft", false)]
        public void GetContactCard(string account)
        {
            GetContactCard(account, 6, 200);
        }

        /// <summary>
        /// Get the specified contact card.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="maximagecount">Number of thumbnail image allowed</param>
        /// <param name="maxcharcount">Number of character in the blog post content which is shown as description.</param>
        [Obsolete("Function no more supported by Microsoft", false)]
        public void GetContactCard(string account, int maximagecount, int maxcharcount)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty &&
                NSMessageHandler.ContactList.HasContact(account, ClientType.PassportMember))
            {
                MsnServiceObject getXmlFeedObject = new MsnServiceObject(PartnerScenario.None, "GetXmlFeed");
                SpaceService service = (SpaceService)CreateService(MsnServiceType.Space, getXmlFeedObject);
                service.GetXmlFeedCompleted += delegate(object sender, GetXmlFeedCompletedEventArgs e)
                {
                    DeleteCompletedObject(service);

                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnContactCardCompleted(new ContactCardCompletedEventArgs(true, e.Error, null));
                        OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetContactCard", e.Error));

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.Message, GetType().Name);
                        return;
                    }

                    if (e.Result.GetXmlFeedResult != null)
                    {
                        ContactCard cc = null;
                        Album album = null;
                        ContactCardItem blogpost = null;
                        ContactCardItem spacetitle = null;
                        Dictionary<ProfileType, ProfileItem> profiles = null;

                        if (null != e.Result.GetXmlFeedResult.contactCard.elements.element)
                        {
                            foreach (elementType element in e.Result.GetXmlFeedResult.contactCard.elements.element)
                            {
                                if (element.type == ContactCardElementType.SpaceTitle.ToString())
                                {
                                    // Get space title.
                                    spacetitle = new ContactCardItem(element.url, null, element.title, null);
                                }
                                else if (element.type == ContactCardElementType.Blog.ToString())
                                {
                                    // Get the latest blog post.
                                    if (element.subElement != null && element.subElement.Length > 0)
                                    {
                                        blogpost = new ContactCardItem(element.subElement[0].url,
                                            element.subElement[0].description,
                                            element.subElement[0].title,
                                            element.subElement[0].tooltip);
                                    }
                                }
                                else if (element.type == ContactCardElementType.Album.ToString())
                                {
                                    // Get updated album photos
                                    album = new Album(element.title, element.url);
                                    foreach (subelementBaseType subelemen in element.subElement)
                                    {
                                        spaceContactCardElementsElementPhotoSubElement spacePhotoElement = subelemen as spaceContactCardElementsElementPhotoSubElement;
                                        album.Photos.Add(new ThumbnailImage(
                                            spacePhotoElement.webReadyUrl,
                                            spacePhotoElement.thumbnailUrl,
                                            spacePhotoElement.albumName,
                                            spacePhotoElement.title,
                                            spacePhotoElement.description,
                                            spacePhotoElement.tooltip));
                                    }
                                }
                                else if (element.type == ContactCardElementType.Profile.ToString())
                                {
                                    // Get updated profiles
                                    profiles = new Dictionary<ProfileType, ProfileItem>();
                                    foreach (subelementBaseType subelemen in element.subElement)
                                    {

                                        if (subelemen.type == ContactCardSubElementType.GeneralProfile.ToString())
                                        {
                                            profiles[ProfileType.GeneralProfile] = new ProfileItem(
                                                true, subelemen.url, subelemen.title, subelemen.tooltip);
                                        }

                                        if (subelemen.type == ContactCardSubElementType.PublicProfile.ToString())
                                        {
                                            profiles[ProfileType.PublicProfile] = new ProfileItem(
                                                true, subelemen.url, subelemen.title, subelemen.tooltip);
                                        }

                                        if (subelemen.type == ContactCardSubElementType.SocialProfile.ToString())
                                        {
                                            profiles[ProfileType.SocialProfile] = new ProfileItem(
                                                true, subelemen.url, subelemen.title, subelemen.tooltip);
                                        }
                                    }
                                }
                            }

                            if (profiles != null || spacetitle != null)
                            {
                                cc = new ContactCard(
                                    e.Result.GetXmlFeedResult.contactCard.elements.displayName,
                                    e.Result.GetXmlFeedResult.contactCard.elements.displayPictureUrl,
                                    account,
                                    spacetitle
                                );

                                if (profiles != null)
                                    cc.SetProfiles(profiles);

                                if (blogpost != null)
                                    cc.SetBlogPost(blogpost);

                                if (album != null)
                                    cc.SetAlbum(album);

                                OnContactCardCompleted(new ContactCardCompletedEventArgs(true, null, cc));
                            }

                            if (NSMessageHandler.ContactService.Deltas != null)
                            {
                                if (NSMessageHandler.ContactService.Deltas.DynamicItems.ContainsKey(account))
                                {
                                    BaseDynamicItemType basedyItem = NSMessageHandler.ContactService.Deltas.DynamicItems[account];
                                    if (basedyItem is PassportDynamicItem)
                                    {
                                        PassportDynamicItem psDyItem = basedyItem as PassportDynamicItem;

                                        psDyItem.ProfileGleamSpecified = true;
                                        psDyItem.ProfileGleam = false;

                                        psDyItem.SpaceGleamSpecified = true;
                                        psDyItem.SpaceGleam = false;

                                        if (psDyItem.SpaceStatus == "Exist Access" || psDyItem.ProfileStatus == "Exist Access")
                                        {
                                            NSMessageHandler.ContactList[account, ClientType.PassportMember].DynamicChanged = DynamicItemState.None;
                                            NSMessageHandler.ContactService.Deltas.DynamicItems.Remove(account);
                                        }

                                        NSMessageHandler.ContactService.Deltas.Save();
                                    }
                                }
                            }
                        }
                    }
                };

                //"1753-01-01T00:00:00.0000000-00:00"
                DateTime defaultTime = XmlConvert.ToDateTime("1753-01-01T00:00:00.0000000-00:00", XmlDateTimeSerializationMode.Utc);
                Contact contact = NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

                GetXmlFeedRequestType request = new GetXmlFeedRequestType();
                request.refreshInformation = new refreshInformationType();
                request.refreshInformation.cid = Convert.ToString(contact.CID);
                request.refreshInformation.applicationId = Properties.Resources.ApplicationStrId;


                request.refreshInformation.market = CultureInfo.CurrentCulture.Name;
                request.refreshInformation.updateAccessedTime = true;
                request.refreshInformation.brand = String.Empty;
                request.refreshInformation.storageAuthCache = String.Empty;
                request.refreshInformation.maxCharacterCount = maxcharcount.ToString();
                request.refreshInformation.maxImageCount = maximagecount.ToString();

                if (NSMessageHandler.ContactService.Deltas == null)
                {
                    return;
                }

                if (NSMessageHandler.ContactService.Deltas.DynamicItems.ContainsKey(account))
                {
                    BaseDynamicItemType dyItem = NSMessageHandler.ContactService.Deltas.DynamicItems[account];

                    if (dyItem is PassportDynamicItem)
                    {
                        PassportDynamicItem psDyItem = dyItem as PassportDynamicItem;

                        // Active contact
                        request.refreshInformation.isActiveContact = true;
                        request.refreshInformation.activeContactLastChanged = psDyItem.LiveContactLastChanged;
                        request.refreshInformation.activeContactLastChangedSpecified = true;

                        // Profile
                        if (psDyItem.ProfileGleam && psDyItem.ProfileStatus == "Exist Access")
                        {
                            if (psDyItem.ProfileLastViewSpecified)
                            {
                                request.refreshInformation.profileLastViewed = psDyItem.ProfileLastView;
                            }
                            else
                            {
                                request.refreshInformation.profileLastViewed = defaultTime;
                            }
                            request.refreshInformation.profileLastViewedSpecified = true;
                        }

                        // Space
                        if (psDyItem.SpaceGleam && psDyItem.SpaceStatus == "Exist Access")
                        {
                            if (psDyItem.SpaceLastViewedSpecified)
                            {
                                request.refreshInformation.spaceLastViewed = psDyItem.SpaceLastViewed;
                            }
                            else
                            {
                                request.refreshInformation.spaceLastViewed = defaultTime;
                            }
                            request.refreshInformation.spaceLastViewedSpecified = true;
                        }

                        // ContactProfile
                        if (psDyItem.ContactProfileStatus == "Exist Access")
                        {
                            if (psDyItem.ContactProfileLastViewedSpecified)
                            {
                                request.refreshInformation.contactProfileLastViewed = psDyItem.ContactProfileLastViewed;
                            }
                            else
                            {
                                request.refreshInformation.contactProfileLastViewed = defaultTime;
                            }

                            request.refreshInformation.contactProfileLastViewedSpecified = true;
                        }
                    }
                }
                else
                {
                    request.refreshInformation.isActiveContact = false;
                    request.refreshInformation.activeContactLastChangedSpecified = false;

                    request.refreshInformation.profileLastViewed = defaultTime;
                    request.refreshInformation.profileLastViewedSpecified = true;

                    request.refreshInformation.spaceLastViewed = defaultTime;
                    request.refreshInformation.spaceLastViewedSpecified = true;

                    request.refreshInformation.contactProfileLastViewed = defaultTime;
                    request.refreshInformation.contactProfileLastViewedSpecified = true;
                }

                service.GetXmlFeedAsync(request, getXmlFeedObject);
            }
            else
            {
                OnContactCardCompleted(new ContactCardCompletedEventArgs(false, null, null));
            }
        }

        /// <summary>
        /// Override to fire the ContactCardCompleted event.
        /// </summary>
        /// <param name="arg">Result arg.</param>
        protected virtual void OnContactCardCompleted(ContactCardCompletedEventArgs arg)
        {
            if (ContactCardCompleted != null)
                ContactCardCompleted(this, arg);
        }
    }
};
