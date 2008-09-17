#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
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

    public class ContactCardCompletedEventArg
    {
        private Exception error = null;
        private bool changed = false;
        private ContactCard contactCard = null;

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

        protected ContactCardCompletedEventArg()
        {
        }

        public ContactCardCompletedEventArg(bool chg, Exception err, ContactCard cc)
        {
            error = err;
            changed = chg;
            contactCard = cc;
        }
    }

    /// <summary>
    /// The delegate is used when the request to a contact card returns.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="arg"></param>
    public delegate void ContactCardCompletedEventHandler(object sender, ContactCardCompletedEventArg arg);

    /// <summary>
    /// Provides services that related to MSN Space.
    /// </summary>
    public class ContactSpaceService : MSNService
    {
        /// <summary>
        /// Fired after GetContactCard completed its async request.
        /// </summary>
        public event ContactCardCompletedEventHandler ContactCardCompleted;

        public ContactSpaceService(NSMessageHandler nshandler)
            : base(nshandler)
        {
        }

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
        public void GetContactCard(string account, int maximagecount, int maxcharcount)
        {
            if (NSMessageHandler.ContactService.Deltas.DynamicItems.ContainsKey(account) &&
                NSMessageHandler.MSNTicket.SSOTickets.ContainsKey(SSOTicketType.Spaces))
            {
                SpaceService service = CreateSpaceService();
                service.GetXmlFeedCompleted += delegate(object sender, GetXmlFeedCompletedEventArgs e)
                {
                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnContactCardCompleted(new ContactCardCompletedEventArg(true, e.Error, null));
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

                            OnContactCardCompleted(new ContactCardCompletedEventArg(true, null, cc));
                        }

                        BaseDynamicItemType basedyItem = NSMessageHandler.ContactService.Deltas.DynamicItems[account];
                        basedyItem.ProfileGleamSpecified = true;
                        basedyItem.ProfileGleam = false;

                        basedyItem.SpaceGleamSpecified = true;
                        basedyItem.SpaceGleam = false;

                        if (NSMessageHandler.ContactList.HasContact(account, ClientType.PassportMember))
                        {
                            if (basedyItem.SpaceStatus == "Exist Access" || basedyItem.ProfileStatus == "Exist Access")
                            {
                                NSMessageHandler.ContactList[account, ClientType.PassportMember].SetdynamicItemChanged(DynamicItemState.Viewed);
                            }
                            NSMessageHandler.ContactService.Deltas.Save();
                        }
                    }
                };

                BaseDynamicItemType dyItem = NSMessageHandler.ContactService.Deltas.DynamicItems[account];
                GetXmlFeedRequestType request = new GetXmlFeedRequestType();
                request.refreshInformation = new refreshInformationType();
                request.refreshInformation.applicationId = Properties.Resources.ApplicationStrId;
                request.refreshInformation.cid = dyItem.CID;
                request.refreshInformation.market = CultureInfo.CurrentCulture.Name;
                request.refreshInformation.updateAccessedTime = true;
                request.refreshInformation.brand = String.Empty;
                request.refreshInformation.storageAuthCache = String.Empty;
                request.refreshInformation.maxCharacterCount = maxcharcount.ToString();
                request.refreshInformation.maxImageCount = maximagecount.ToString();
                //"1753-01-01T00:00:00.0000000-00:00"
                DateTime defaultTime = XmlConvert.ToDateTime("1753-01-01T00:00:00.0000000-00:00", XmlDateTimeSerializationMode.Utc);

                //Active contact
                if (dyItem.LiveContactLastChangedSpecified)
                {
                    request.refreshInformation.isActiveContact = true;
                    request.refreshInformation.activeContactLastChanged = dyItem.LiveContactLastChanged;
                    request.refreshInformation.activeContactLastChangedSpecified = true;
                }
                else
                {
                    request.refreshInformation.isActiveContact = false;
                    request.refreshInformation.activeContactLastChangedSpecified = false;
                }

                //Profile
                if (dyItem.ProfileGleam && dyItem.ProfileStatus == "Exist Access")
                {
                    if (dyItem.ProfileLastViewSpecified)
                    {
                        request.refreshInformation.profileLastViewed = dyItem.ProfileLastView;
                    }
                    else
                    {
                        request.refreshInformation.profileLastViewed = defaultTime;
                    }
                    request.refreshInformation.profileLastViewedSpecified = true;
                }

                //Space
                if (dyItem.SpaceGleam && dyItem.SpaceStatus == "Exist Access")
                {
                    if (dyItem.SpaceLastViewedSpecified)
                    {
                        request.refreshInformation.spaceLastViewed = dyItem.SpaceLastViewed;
                    }
                    else
                    {
                        request.refreshInformation.spaceLastViewed = defaultTime;
                    }
                    request.refreshInformation.spaceLastViewedSpecified = true;
                }


                //ContactProfile
                if (dyItem.ContactProfileStatus == "Exist Access")
                {
                    if (dyItem.ContactProfileLastViewedSpecified)
                    {
                        request.refreshInformation.contactProfileLastViewed = dyItem.ContactProfileLastViewed;
                    }
                    else
                    {
                        request.refreshInformation.contactProfileLastViewed = defaultTime;
                    }

                    request.refreshInformation.contactProfileLastViewedSpecified = true;
                }

                service.GetXmlFeedAsync(request, new object());
            }
            else
            {
                OnContactCardCompleted(new ContactCardCompletedEventArg(false, null, null));
            }
        }

        /// <summary>
        /// Override to fire the ContactCardCompleted event.
        /// </summary>
        /// <param name="arg">Result arg.</param>
        protected virtual void OnContactCardCompleted(ContactCardCompletedEventArg arg)
        {
            if (ContactCardCompleted != null)
                ContactCardCompleted(this, arg);
        }

        private SpaceService CreateSpaceService()
        {
            SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Spaces);

            SpaceService service = new SpaceService();
            service.Proxy = WebProxy;
            service.AuthTokenHeaderValue = new AuthTokenHeader();
            service.AuthTokenHeaderValue.Token = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Spaces].Ticket;
            return service;
        }
    }
};
