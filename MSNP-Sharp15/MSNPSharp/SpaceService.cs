using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.IO;
using System.Globalization;
using MSNPSharp.MSNSPACEWS.MSNSpaceService;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Drawing;
using System.Xml;
using System.Threading;
using System.Drawing.Imaging;

namespace MSNPSharp
{
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
            get { return error; }
        }

        /// <summary>
        /// Indicates whether the specified contact has gleams.
        /// </summary>
        public bool Changed
        {
            get { return changed; }
        }

        /// <summary>
        /// The contact card return.
        /// </summary>
        public ContactCard ContactCard
        {
            get { return contactCard; }
        }

        protected ContactCardCompletedEventArg(){}
        public ContactCardCompletedEventArg (bool chg,Exception err,ContactCard cc)
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
    public delegate void ContactCardCompletedEventHandler(object sender,ContactCardCompletedEventArg arg);

    /// <summary>
    /// Provides services that related to MSN Space.
    /// </summary>
    public class ContactSpaceService : MSNService
    {
        private NSMessageHandler nsMessageHandler = null;
        WebProxy webProxy = null;
        string account = null;
        /// <summary>
        /// Fired after GetContactCard completed its async request.
        /// </summary>
        public event ContactCardCompletedEventHandler ContactCardCompleted;
 
        protected ContactSpaceService()
        {
        }

        public ContactSpaceService(NSMessageHandler nshandler)
        {
            nsMessageHandler = nshandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get { return nsMessageHandler; }
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
            this.account = account;
            if (nsMessageHandler.ContactService.Deltas.DynamicItems.ContainsKey(account))
            {
                DynamicItem dyItem = nsMessageHandler.ContactService.Deltas.DynamicItems[account];
                SpaceService service = new SpaceService();
                service.Proxy = webProxy;
                service.AuthTokenHeaderValue = new AuthTokenHeader();
                service.AuthTokenHeaderValue.Token = nsMessageHandler.Tickets[Iniproperties.SpacesTicket];
                GetXmlFeedRequestType request = new GetXmlFeedRequestType();
                request.refreshInformation = new refreshInformationType();
                request.refreshInformation.applicationId = Properties.Resources.ApplicationStrId; ;
                request.refreshInformation.cid = dyItem.CID;
                request.refreshInformation.market = CultureInfo.CurrentCulture.Name;
                request.refreshInformation.updateAccessedTime = true;
                request.refreshInformation.isActiveContact = true;
                request.refreshInformation.brand = "";
                request.refreshInformation.storageAuthCache = "";
                request.refreshInformation.maxCharacterCount = maxcharcount.ToString();
                request.refreshInformation.maxImageCount = maximagecount.ToString();

                if (dyItem.ProfileGleam)
                {
                    request.refreshInformation.profileLastViewed = dyItem.ProfileLastChanged;
                    request.refreshInformation.profileLastViewedSpecified = true;
                }

                if (dyItem.SpaceGleam)
                {
                    request.refreshInformation.spaceLastViewed = dyItem.SpaceLastViewed;
                    request.refreshInformation.spaceLastViewedSpecified = true;
                }

                service.GetXmlFeedCompleted += delegate(object sender, GetXmlFeedCompletedEventArgs e)
                {
                    if (e.Cancelled)
                        return;
                    if (e.Error != null)
                    {
                        OnContactCardCompleted(new ContactCardCompletedEventArg(true, e.Error, null));
                        OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetContactCard", e.Error));
                        if (Settings.TraceSwitch.TraceError)
                            Trace.WriteLine(e.Error.Message);
                        return;
                    }

                    if (e.Result.GetXmlFeedResult == null)
                        return;

                    Thread GetContactCardThread = new Thread(new ParameterizedThreadStart(GetCardSub));
                    GetContactCardThread.Start(e);
                };

                service.GetXmlFeedAsync(request, new object());

            }
            else
            {
                OnContactCardCompleted(new ContactCardCompletedEventArg(false, null, null));
            }
        }

        private void GetCardSub(object param)
        {
            GetXmlFeedCompletedEventArgs e = param as GetXmlFeedCompletedEventArgs;
            MemoryStream ms = new MemoryStream();
            try
            {
                HttpWebRequest httprequest = HttpWebRequest.Create(e.Result.GetXmlFeedResult.contactCard.elements.displayPictureUrl) as HttpWebRequest;
                httprequest.Timeout = 3000;  //You know, I am a fast man, I don't like things that make me waiting.
                httprequest.Proxy = webProxy;

                Stream response = httprequest.GetResponse().GetResponseStream();
                byte[] buffer = new byte[8192];
                int read = 0;

                while ((read = response.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                response.Close();
            }
            catch (Exception ex)
            {
                if (Settings.TraceSwitch.TraceError)
                    Trace.WriteLine(ex.Message);
                Properties.Resources.WLXLarge_default.Save(ms, ImageFormat.Bmp);
            }

            ContactCard cc = null;                      //ContactCard.
            Image displayImage = Image.FromStream(ms);  //Contact display image.
            ContactCardItem spacetitle = null;          //Contact space title.
            ContactCardItem blogpost = null;            //Latest blog post.
            Album album = null;                         //Album
            foreach (elementType element in e.Result.GetXmlFeedResult.contactCard.elements.element)
            {
                //Get space title.
                if (element.type == ContactCardElementType.SpaceTitle.ToString())
                {
                    spacetitle = new ContactCardItem(element.url, null, element.title, null);
                }

                //Get the latest blog post.
                if (element.type == ContactCardElementType.Blog.ToString())
                {
                    if (element.subElement != null && element.subElement.Length > 0)
                    {
                        blogpost = new ContactCardItem(GetProperty(element.subElement[0] as XmlNode[], "url"),
                            GetProperty(element.subElement[0] as XmlNode[], "description"),
                            GetProperty(element.subElement[0] as XmlNode[], "title"),
                            GetProperty(element.subElement[0] as XmlNode[], "tooltip"));
                    }
                }

                //Get updated album photos

                if (element.type == ContactCardElementType.Album.ToString())
                {
                    album = new Album(element.title, element.url);
                    foreach (XmlNode[] subelemen in element.subElement)
                    {
                        if (GetProperty(subelemen, "type") == ContactCardSubElementType.Photo.ToString())
                        {
                            try
                            {
                                HttpWebRequest httpreq = HttpWebRequest.Create(GetProperty(subelemen, "thumbnailUrl")) as HttpWebRequest;
                                httpreq.Timeout = 3000; //Fast man again.
                                httpreq.Proxy = webProxy;

                                Stream stream = httpreq.GetResponse().GetResponseStream();
                                MemoryStream mstream = new MemoryStream();
                                byte[] thumbBuffer = new byte[8192];
                                int readbyts = 0;
                                while ((readbyts = stream.Read(thumbBuffer, 0, thumbBuffer.Length)) > 0)
                                {
                                    mstream.Write(thumbBuffer, 0, readbyts);
                                }
                                stream.Close();
                                album.Photos.Add(new ThumbnailImage(GetProperty(subelemen, "webReadyUrl"),
                                    Image.FromStream(mstream), GetProperty(subelemen, "albumName"), GetProperty(subelemen, "title"), GetProperty(subelemen, "description"),
                                    GetProperty(subelemen, "tooltip")));
                            }
                            catch (Exception ex)
                            {
                                if (Settings.TraceSwitch.TraceError)
                                    Trace.WriteLine(ex.Message);
                            }
                        }
                    }
                }

                //Get updated profiles
                if (element.type == ContactCardElementType.Profile.ToString())
                {
                    foreach (XmlNode[] subelemen in element.subElement)
                    {
                        if (GetProperty(subelemen, "type") == ContactCardSubElementType.GeneralProfile.ToString())
                        {

                            cc.Profiles[ProfileType.GeneralProfile] = new ProfileItem(
                                true, GetProperty(subelemen, "url"), GetProperty(subelemen, "title"), GetProperty(subelemen, "tooltip"));
                        }

                        if (GetProperty(subelemen, "type") == ContactCardSubElementType.PublicProfile.ToString())
                        {
                            cc.Profiles[ProfileType.PublicProfile] = new ProfileItem(
                                true, GetProperty(subelemen, "url"), GetProperty(subelemen, "title"), GetProperty(subelemen, "tooltip"));
                        }

                        if (GetProperty(subelemen, "type") == ContactCardSubElementType.SocialProfile.ToString())
                        {
                            cc.Profiles[ProfileType.SocialProfile] = new ProfileItem(
                                true, GetProperty(subelemen, "url"), GetProperty(subelemen, "title"), GetProperty(subelemen, "tooltip"));
                        }
                    }
                }

            }

            if (spacetitle != null)
            {
                cc = new ContactCard(e.Result.GetXmlFeedResult.contactCard.elements.displayName, account, spacetitle);
                if (blogpost != null)
                    cc.SetBlogPost(blogpost);
                if (album != null)
                    cc.SetAlbum(album);
                OnContactCardCompleted(new ContactCardCompletedEventArg(true, null, cc));
            }


            nsMessageHandler.ContactService.Deltas.DynamicItems[account].State = DynamicItemState.Viewed;
            if (nsMessageHandler.ContactList.HasContact(account, ClientType.PassportMember))
            {
                nsMessageHandler.ContactList[account,ClientType.PassportMember].SetdynamicItemChanged(DynamicItemState.Viewed);
                nsMessageHandler.ContactService.Deltas.Save();
            }
        }

        /// <summary>
        /// Get subelement properties.
        /// </summary>
        /// <param name="nodes">SubElement object</param>
        /// <param name="name">Property name</param>
        /// <returns></returns>
        private string GetProperty(XmlNode[] nodes, string name)
        {
            foreach (XmlNode node in nodes)
            {
                if (node.Name == name)
                    return node.InnerText;
            }

            return null;
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
    }
}
