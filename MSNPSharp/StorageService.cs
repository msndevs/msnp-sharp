using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNStorageService;

    /// <summary>
    /// Provide webservice operations for Storage Service
    /// </summary>
    public class MSNStorageService : MSNService
    {
        private WebProxy webProxy = null;
        private NSMessageHandler nsMessageHandler = null;

        protected MSNStorageService()
        {
        }

        public MSNStorageService(NSMessageHandler nsHandler)
            : this()
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }


        private StorageService CreateStorageService(String Scenario)
        {
            StorageService storageService = new StorageService();
            storageService.Proxy = webProxy;
            storageService.StorageApplicationHeaderValue = new StorageApplicationHeader();
            storageService.StorageApplicationHeaderValue.ApplicationID = "Messenger Client 8.5";
            storageService.StorageApplicationHeaderValue.Scenario = Scenario;
            storageService.StorageUserHeaderValue = new StorageUserHeader();
            storageService.StorageUserHeaderValue.Puid = 0;
            storageService.StorageUserHeaderValue.TicketToken = nsMessageHandler.Tickets[Iniproperties.StorageTicket];
            return storageService;
        }

        //.... @_@
        private ExpressionProfileAttributesType CreateFullExpressionProfileAttributes()
        {
            ExpressionProfileAttributesType expAttrib = new ExpressionProfileAttributesType();
            expAttrib.DateModified = true;
            expAttrib.DateModifiedSpecified = true;
            expAttrib.DisplayName = true;
            expAttrib.DisplayNameLastModified = true;
            expAttrib.DisplayNameLastModifiedSpecified = true;
            expAttrib.DisplayNameSpecified = true;
            expAttrib.Flag = true;
            expAttrib.FlagSpecified = true;
            expAttrib.PersonalStatus = true;
            expAttrib.PersonalStatusLastModified = true;
            expAttrib.PersonalStatusLastModifiedSpecified = true;
            expAttrib.PersonalStatusSpecified = true;
            expAttrib.Photo = true;
            expAttrib.PhotoSpecified = true;
            expAttrib.ResourceID = true;
            expAttrib.ResourceIDSpecified = true;
            expAttrib.StaticUserTilePublicURL = true;
            expAttrib.StaticUserTilePublicURLSpecified = true;

            return expAttrib;
        }

        /// <summary>
        /// Initialize the user profile if the contact connect to live network the firt time.
        /// 
        /// CreateProfile
        /// ShareItem
        /// AddMember
        /// [GetProfile]
        /// CreateDocument
        /// CreateRelationships
        /// UpdateProfile
        /// FindDocuments
        /// UpdateDynamicItem - Error
        /// ABContactUpdate
        /// 
        /// 9 steps, what the hell!!
        /// </summary>
        private void CreateProfile()
        {
            if (false == nsMessageHandler.Tickets.ContainsKey(Iniproperties.StorageTicket))
                return;

            try
            {
                StorageService storageService = CreateStorageService("RoamingSeed");
                storageService.AllowAutoRedirect = true;

                //1. CreateProfile, create a new profile and return its resource id.
                CreateProfileRequestType createRequest = new CreateProfileRequestType();
                createRequest.profile = new CreateProfileRequestTypeProfile();
                createRequest.profile.ExpressionProfile = new ExpressionProfile();
                createRequest.profile.ExpressionProfile.PersonalStatus = "";
                createRequest.profile.ExpressionProfile.RoleDefinitionName = "ExpressionProfileDefault";
                string resId_Prof = "";
                try
                {
                    CreateProfileResponse createResponse = storageService.CreateProfile(createRequest);
                    resId_Prof = createResponse.CreateProfileResult;
                    NSMessageHandler.ContactService.AddressBook.Profile.ResourceID = resId_Prof;
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("CreateProfile error : " + ex.Message);
                }

                //2. ShareItem, share the profile.
                ShareItemRequestType shareItemRequest = new ShareItemRequestType();
                shareItemRequest.resourceID = resId_Prof;
                shareItemRequest.displayName = "Messenger Roaming Identity";
                string cacheKey = "";
                try
                {
                    ShareItemResponseType shareItemResponse = storageService.ShareItem(shareItemRequest);
                    cacheKey = storageService.AffinityCacheHeaderValue.CacheKey;
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("ShareItem error : " + ex.Message);  //Item already shared.
                }

                //3. AddMember, add a ProfileExpression role member into the newly created profile and define messenger service.
                HandleType srvHandle = new HandleType();
                srvHandle.ForeignId = "MyProfile";
                srvHandle.Id = "0";
                srvHandle.Type = ServiceFilterType.Profile;
                if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.ContactTicket))
                {
                    SharingServiceBinding sharingService = NSMessageHandler.ContactService.CreateSharingService("RoamingSeed");
                    sharingService.AllowAutoRedirect = true;

                    AddMemberRequestType addMemberRequest = new AddMemberRequestType();

                    addMemberRequest.serviceHandle = srvHandle;

                    Membership memberShip = new Membership();
                    memberShip.MemberRole = MemberRole.ProfileExpression;
                    RoleMember roleMember = new RoleMember();
                    roleMember.Type = "Role";
                    roleMember.Id = "Allow";
                    roleMember.State = MemberState.Accepted;
                    roleMember.MaxRoleRecursionDepth = "0";
                    roleMember.MaxDegreesSeparation = "0";

                    RoleMemberDefiningService defService = new RoleMemberDefiningService();
                    defService.ForeignId = "";
                    defService.Id = "0";
                    defService.Type = "Messenger";

                    roleMember.DefiningService = defService;
                    memberShip.Members = new RoleMember[] { roleMember };
                    addMemberRequest.memberships = new Membership[] { memberShip };
                    try
                    {
                        AddMemberResponse addMemberResponse = sharingService.AddMember(addMemberRequest);
                    }
                    catch (Exception ex)
                    {
                        if (Settings.TraceSwitch.TraceError)
                            Trace.WriteLine("AddMember error : " + ex.Message);
                    }
                }

                // [GetProfile], get the new ProfileExpression resource id.
                GetProfileRequestType getprofileRequest = new GetProfileRequestType();

                Alias alias = new Alias();
                alias.NameSpace = "MyCidStuff";
                alias.Name = NSMessageHandler.ContactService.AddressBook.Profile.CID;

                Handle pHandle = new Handle();
                pHandle.RelationshipName = "MyProfile";
                pHandle.Alias = alias;

                getprofileRequest.profileHandle = pHandle;
                getprofileRequest.profileAttributes = new profileAttributes();

                ExpressionProfileAttributesType expAttrib = CreateFullExpressionProfileAttributes();

                getprofileRequest.profileAttributes.ExpressionProfileAttributes = expAttrib;
                string resId_ExProf = "";

                try
                {
                    GetProfileResponse getprofileResponse = storageService.GetProfile(getprofileRequest);
                    resId_ExProf = getprofileResponse.GetProfileResult.ExpressionProfile.ResourceID;
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("GetProfile error : " + ex.Message);
                }

                //4. CreateDocument, create a new document for this profile and return its resource id.
                CreateDocumentRequestType createDocRequest = new CreateDocumentRequestType();
                createDocRequest.relationshipName = "Messenger User Tile";

                Handle parenthandle = new Handle();
                parenthandle.RelationshipName = "/UserTiles";

                parenthandle.Alias = alias;
                createDocRequest.parentHandle = parenthandle;
                createDocRequest.document = new Photo();
                createDocRequest.document.Name = "MSNPSharp";

                PhotoStream photoStream = new PhotoStream();
                photoStream.DataSize = 0;
                photoStream.MimeType = "png";
                photoStream.DocumentStreamType = "UserTileStatic";
                MemoryStream mem = new MemoryStream();
                Properties.Resources.WLXLarge_default.Save(mem, System.Drawing.Imaging.ImageFormat.Png);
                photoStream.Data = mem.ToArray();
                createDocRequest.document.DocumentStreams = new PhotoStream[] { photoStream };

                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = Properties.Resources.WLXLarge_default;  //Set default
                NSMessageHandler.Owner.DisplayImage = displayImage;

                string resId_Doc = "";
                try
                {
                    CreateDocumentResponseType createDocResponse = storageService.CreateDocument(createDocRequest);
                    resId_Doc = createDocResponse.CreateDocumentResult;
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("CreateDocument error : " + ex.Message);
                }

                //5. CreateRelationships, create a relationship between ProfileExpression role member and the new document.
                CreateRelationshipsRequestType createRelationshipRequest = new CreateRelationshipsRequestType();
                Relationship relationship = new Relationship();
                relationship.RelationshipName = "ProfilePhoto";
                relationship.SourceType = "SubProfile"; //From SubProfile
                relationship.TargetType = "Photo";      //To Photo
                relationship.SourceID = resId_ExProf;  //From Expression profile
                relationship.TargetID = resId_Doc;     //To Document

                createRelationshipRequest.relationships = new Relationship[] { relationship };
                try
                {
                    storageService.CreateRelationships(createRelationshipRequest);
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("CreateRelationships error : " + ex.Message);
                }

                //6. UpdateProfile
                UpdateProfileRequestType updateProfileRequest = new UpdateProfileRequestType();
                updateProfileRequest.profile = new UpdateProfileRequestTypeProfile();
                updateProfileRequest.profile.ResourceID = resId_Prof;
                ExpressionProfile expProf = new ExpressionProfile();
                expProf.FreeText = "Update";
                expProf.DisplayName = NSMessageHandler.Owner.NickName;
                updateProfileRequest.profile.ExpressionProfile = expProf;

                updateProfileRequest.profileAttributesToDelete = new UpdateProfileRequestTypeProfileAttributesToDelete();
                ExpressionProfileAttributesType exProfAttrbUpdate = new ExpressionProfileAttributesType();
                exProfAttrbUpdate.PersonalStatus = true;
                exProfAttrbUpdate.PersonalStatusSpecified = true;

                updateProfileRequest.profileAttributesToDelete.ExpressionProfileAttributes = exProfAttrbUpdate;
                try
                {
                    storageService.UpdateProfile(updateProfileRequest);
                }
                catch (Exception ex)
                {
                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine("UpdateProfile error ; " + ex.Message);
                }

                //7. FindDocuments Hmm....


                //8. UpdateDynamicItem
                if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.ContactTicket))
                {
                    ABServiceBinding abService = NSMessageHandler.ContactService.CreateABService("RoamingSeed");
                    abService.AllowAutoRedirect = true;

                    UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                    updateDyItemRequest.abId = Guid.Empty.ToString();

                    PassportDynamicItem passportDyItem = new PassportDynamicItem();
                    passportDyItem.Type = "Passport";
                    passportDyItem.PassportName = NSMessageHandler.Owner.Mail;
                    passportDyItem.Changes = "Notifications";

                    NotificationDataType notification = new NotificationDataType();
                    notification.Status = "Exist Access";
                    //notification.InstanceId = "0";
                    //notification.Gleam = false;
                    notification.LastChanged = DateTime.Now;

                    ServiceType srvInfo = new ServiceType();
                    srvInfo.Changes = "";
                    //srvInfo.LastChange = XmlConvert.ToDateTime("0001-01-01T00:00:00", XmlDateTimeSerializationMode.RoundtripKind);
                    //srvInfo.Deleted = false;

                    InfoType info = new InfoType();
                    info.Handle = srvHandle;
                    info.IsBot = false;
                    info.InverseRequired = false;

                    srvInfo.Info = info;
                    notification.StoreService = srvInfo;
                    passportDyItem.Notifications = new BaseDynamicItemTypeNotifications();
                    passportDyItem.Notifications.NotificationData = notification;
                    updateDyItemRequest.dynamicItems = new PassportDynamicItem[] { passportDyItem };
                    try
                    {
                        abService.UpdateDynamicItem(updateDyItemRequest);
                    }
                    catch (Exception ex)
                    {
                        if (Settings.TraceSwitch.TraceError)
                            Trace.WriteLine("UpdateDynamicItem error ; " + ex.Message);
                    }

                    //9. ABContactUpdate
                    ABContactUpdateRequestType abcontactUpdateRequest = new ABContactUpdateRequestType();
                    abcontactUpdateRequest.abId = Guid.Empty.ToString();

                    ContactType meContact = new ContactType();
                    meContact.propertiesChanged = "Annotation";

                    contactInfoType meinfo = new contactInfoType();
                    meinfo.contactTypeSpecified = true;
                    meinfo.contactType = contactInfoTypeContactType.Me;

                    Annotation anno = new Annotation();
                    anno.Name = "MSN.IM.RoamLiveProperties";
                    anno.Value = "1";

                    meinfo.annotations = new Annotation[] { anno };
                    meContact.contactInfo = meinfo;
                    abcontactUpdateRequest.contacts = new ContactType[] { meContact };
                    try
                    {
                        ABContactUpdateResponse abcupdateResponse = abService.ABContactUpdate(abcontactUpdateRequest);
                    }
                    catch (Exception ex)
                    {
                        if (Settings.TraceSwitch.TraceError)
                            Trace.WriteLine("ABContactUpdate error ; " + ex.Message);
                    }
                }

                //10. OK, there's no 10, that's all....
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Get my profile. Display name, personal status and display photo.
        /// </summary>
        public OwnerProfile GetProfile()
        {
            if (nsMessageHandler.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                nsMessageHandler.Tickets.ContainsKey(Iniproperties.StorageTicket))
            {
                try
                {
                    StorageService storageService = CreateStorageService("Initial");

                    GetProfileRequestType request = new GetProfileRequestType();
                    request.profileHandle = new Handle();
                    request.profileHandle.Alias = new Alias();
                    request.profileHandle.Alias.Name = NSMessageHandler.ContactService.AddressBook.Profile.CID;
                    request.profileHandle.Alias.NameSpace = "MyCidStuff";
                    request.profileHandle.RelationshipName = "MyProfile";
                    request.profileAttributes = new profileAttributes();
                    request.profileAttributes.ExpressionProfileAttributes = CreateFullExpressionProfileAttributes();

                    GetProfileResponse response = storageService.GetProfile(request);

                    NSMessageHandler.ContactService.AddressBook.Profile.DateModified = response.GetProfileResult.ExpressionProfile.DateModified;
                    NSMessageHandler.ContactService.AddressBook.Profile.ResourceID = response.GetProfileResult.ExpressionProfile.ResourceID;

                    // Display name
                    NSMessageHandler.ContactService.AddressBook.Profile.DisplayName = response.GetProfileResult.ExpressionProfile.DisplayName;

                    // Personal status
                    NSMessageHandler.ContactService.AddressBook.Profile.PersonalMessage = response.GetProfileResult.ExpressionProfile.PersonalStatus;

                    // Display photo
                    if (null != response.GetProfileResult.ExpressionProfile.Photo)
                    {
                        string url = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;
                        NSMessageHandler.ContactService.AddressBook.Profile.Photo.DateModified = response.GetProfileResult.ExpressionProfile.Photo.DateModified;
                        NSMessageHandler.ContactService.AddressBook.Profile.Photo.ResourceID = response.GetProfileResult.ExpressionProfile.Photo.ResourceID;

                        if (NSMessageHandler.ContactService.AddressBook.Profile.Photo.PreAthURL != url)
                        {
                            NSMessageHandler.ContactService.AddressBook.Profile.Photo.PreAthURL = url;
                            if (!url.StartsWith("http"))
                            {
                                url = "http://blufiles.storage.msn.com" + url;  //I found it http://byfiles.storage.msn.com is also ok
                            }

                            // Don't urlencode t= :))
                            Uri uri = new Uri(url + "?t=" + System.Web.HttpUtility.UrlEncode(nsMessageHandler.Tickets[Iniproperties.StorageTicket].Substring(2)));

                            HttpWebRequest fwr = (HttpWebRequest)WebRequest.Create(uri);
                            fwr.Proxy = webProxy;

                            Stream stream = fwr.GetResponse().GetResponseStream();
                            SerializableMemoryStream ms = new SerializableMemoryStream();
                            byte[] data = new byte[8192];
                            int read;
                            while ((read = stream.Read(data, 0, data.Length)) > 0)
                            {
                                ms.Write(data, 0, read);
                            }
                            stream.Close();
                            NSMessageHandler.ContactService.AddressBook.Profile.Photo.DisplayImage = ms;
                        }

                        System.Drawing.Image fileImage = System.Drawing.Image.FromStream(NSMessageHandler.ContactService.AddressBook.Profile.Photo.DisplayImage);
                        DisplayImage displayImage = new DisplayImage();
                        displayImage.Image = fileImage;

                        nsMessageHandler.Owner.DisplayImage = displayImage;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.IndexOf("Alias does not exist") != -1)
                    {
                        CreateProfile();
                    }

                    if (Settings.TraceSwitch.TraceError)
                        Trace.WriteLine(ex.Message);
                }
            }
            return NSMessageHandler.ContactService.AddressBook.Profile;
        }

        public void UpdateProfile(string displayName, string personalStatus)
        {
            OwnerProfile profile = NSMessageHandler.ContactService.AddressBook.Profile;

            if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.StorageTicket) &&
                (profile.DisplayName != displayName || profile.PersonalMessage != personalStatus))
            {
                NSMessageHandler.ContactService.AddressBook.Profile.DisplayName = displayName;
                NSMessageHandler.ContactService.AddressBook.Profile.PersonalMessage = personalStatus;
                if (nsMessageHandler.Owner.RoamLiveProperty == RoamLiveProperty.Enabled)
                {
                    StorageService storageService = CreateStorageService("RoamingIdentityChanged");
                    storageService.UpdateProfileCompleted += delegate(object sender, UpdateProfileCompletedEventArgs e)
                    {
                        storageService = sender as StorageService;
                        if (!e.Cancelled && e.Error == null)
                        {
                            // And get profile again
                            NSMessageHandler.ContactService.AddressBook.Profile = GetProfile();

                            // UpdateDynamicItem
                            if (nsMessageHandler.Tickets.ContainsKey(Iniproperties.ContactTicket))
                            {
                                ABServiceBinding abService = NSMessageHandler.ContactService.CreateABService("RoamingIdentityChanged");
                                abService.UpdateDynamicItemCompleted += delegate(object service, UpdateDynamicItemCompletedEventArgs ue)
                                {
                                    NSMessageHandler.ContactService.handleServiceHeader(((ABServiceBinding)service).ServiceHeaderValue, true);
                                    if (!ue.Cancelled && ue.Error == null)
                                    {
                                        NSMessageHandler.ContactService.AddressBook.Save();
                                    }
                                    else if (ue.Error != null)
                                    {
                                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateDynamic", ue.Error));
                                    }
                                };

                                UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                                updateDyItemRequest.abId = Guid.Empty.ToString();

                                PassportDynamicItem passportDyItem = new PassportDynamicItem();
                                passportDyItem.Type = "Passport";
                                passportDyItem.PassportName = NSMessageHandler.Owner.Mail;
                                passportDyItem.Changes = "Notifications";
                                passportDyItem.Notifications = new BaseDynamicItemTypeNotifications();
                                passportDyItem.Notifications.NotificationData = new NotificationDataType();
                                passportDyItem.Notifications.NotificationData.StoreService = new ServiceType();
                                passportDyItem.Notifications.NotificationData.StoreService.Info = new InfoType();
                                passportDyItem.Notifications.NotificationData.StoreService.Info.Handle = new HandleType();
                                passportDyItem.Notifications.NotificationData.StoreService.Info.Handle.Id = "0";
                                passportDyItem.Notifications.NotificationData.StoreService.Info.Handle.Type = ServiceFilterType.Profile;
                                passportDyItem.Notifications.NotificationData.StoreService.Info.Handle.ForeignId = "MyProfile";
                                passportDyItem.Notifications.NotificationData.StoreService.Info.IsBot = false;
                                passportDyItem.Notifications.NotificationData.StoreService.Info.InverseRequired = false;
                                passportDyItem.Notifications.NotificationData.StoreService.Changes = String.Empty;
                                passportDyItem.Notifications.NotificationData.Status = "Exist Access";
                                passportDyItem.Notifications.NotificationData.LastChanged = NSMessageHandler.ContactService.AddressBook.Profile.DateModified;

                                updateDyItemRequest.dynamicItems = new PassportDynamicItem[] { passportDyItem };

                                abService.UpdateDynamicItemAsync(updateDyItemRequest, new object());
                                return;
                            }
                        }
                        else if (e.Error != null)
                        {
                            OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateProfile", e.Error));
                        }
                    };

                    UpdateProfileRequestType request = new UpdateProfileRequestType();
                    request.profile = new UpdateProfileRequestTypeProfile();
                    request.profile.ResourceID = profile.ResourceID;
                    request.profile.ExpressionProfile = new ExpressionProfile();
                    request.profile.ExpressionProfile.FreeText = "Update";
                    request.profile.ExpressionProfile.DisplayName = displayName;
                    request.profile.ExpressionProfile.PersonalStatus = personalStatus;
                    request.profile.ExpressionProfile.Flags = 0;
                    storageService.UpdateProfileAsync(request, new object());
                }
                else
                {
                    NSMessageHandler.ContactService.AddressBook.Save();
                }
            }
        }

    }
};
