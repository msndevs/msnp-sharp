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
        public MSNStorageService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }

        #region Internal implementation
        private StorageService CreateStorageService(String scenario)
        {
            SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Storage);

            StorageService storageService = new StorageService();
            storageService.Proxy = WebProxy;
            storageService.StorageApplicationHeaderValue = new StorageApplicationHeader();
#if MSNP18
            storageService.StorageApplicationHeaderValue.ApplicationID = Properties.Resources.ApplicationStrId;
#else
           storageService.StorageApplicationHeaderValue.ApplicationID = "Messenger Client 8.5";
#endif
            storageService.StorageApplicationHeaderValue.Scenario = scenario;
            storageService.StorageUserHeaderValue = new StorageUserHeader();
            storageService.StorageUserHeaderValue.Puid = 0;
            storageService.StorageUserHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket;
            storageService.AffinityCacheHeaderValue = new AffinityCacheHeader();
            storageService.AffinityCacheHeaderValue.CacheKey = NSMessageHandler.ContactService.Deltas.CacheKeys[CacheKeyType.StorageServiceCacheKey];
            return storageService;
        }

        private static ExpressionProfileAttributesType CreateFullExpressionProfileAttributes()
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
        /// 9 steps, what the hell!! If M$ change any protocol in their strageservice, it will be a disaster to find the difference.
        /// </summary>
        private void CreateProfile()
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || NSMessageHandler.ContactService.Deltas == null)
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
                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, "CreateProfile", createRequest);
                    CreateProfileResponse createResponse = storageService.CreateProfile(createRequest);
                    resId_Prof = createResponse.CreateProfileResult;
                    NSMessageHandler.ContactService.Deltas.Profile.ResourceID = resId_Prof;
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateProfile error: " + ex.Message, GetType().Name);
                }

                //2. ShareItem, share the profile.
                ShareItemRequestType shareItemRequest = new ShareItemRequestType();
                shareItemRequest.resourceID = resId_Prof;
                shareItemRequest.displayName = "Messenger Roaming Identity";
                try
                {
                    storageService.ShareItem(shareItemRequest);
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "ShareItem error: " + ex.Message, GetType().Name); //Item already shared.
                }

                //3. AddMember, add a ProfileExpression role member into the newly created profile and define messenger service.
                HandleType srvHandle = new HandleType();
                srvHandle.ForeignId = "MyProfile";
                srvHandle.Id = "0";
                srvHandle.Type = ServiceFilterType.Profile;
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
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
                        NSMessageHandler.ContactService.ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, "AddMember", addMemberRequest);
                        sharingService.AddMember(addMemberRequest);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "AddMember error: " + ex.Message, GetType().Name);
                    }
                }

                // [GetProfile], get the new ProfileExpression resource id.
                GetProfileRequestType getprofileRequest = new GetProfileRequestType();

                Alias alias = new Alias();
                alias.NameSpace = "MyCidStuff";
                alias.Name = Convert.ToString(NSMessageHandler.Owner.CID);

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
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "GetProfile error: " + ex.Message, GetType().FullName);
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
                MemoryStream mem = (SerializableMemoryStream)Properties.Resources.WLXLarge_default;
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
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateDocument error: " + ex.Message, GetType().Name);
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
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateRelationships error: " + ex.Message, GetType().Name);
                }

                //6.1 UpdateProfile
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
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "UpdateProfile error: " + ex.Message, GetType().Name);
                }

                // 6.2 Get Profile again to get notification.LastChanged
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl("Initial");

                //7. FindDocuments Hmm....


                //8. UpdateDynamicItem
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
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
                    notification.LastChanged = NSMessageHandler.ContactService.Deltas.Profile.DateModified;

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
                    passportDyItem.Notifications = new NotificationDataType[] { notification };
                    updateDyItemRequest.dynamicItems = new PassportDynamicItem[] { passportDyItem };
                    try
                    {
                        NSMessageHandler.ContactService.ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, "UpdateDynamicItem", updateDyItemRequest);
                        abService.UpdateDynamicItem(updateDyItemRequest);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "UpdateDynamicItem error: " + ex.Message, GetType().Name);
                    }

                    //9. ABContactUpdate
                    ABContactUpdateRequestType abcontactUpdateRequest = new ABContactUpdateRequestType();
                    abcontactUpdateRequest.abId = Guid.Empty.ToString();

                    ContactType meContact = new ContactType();
                    meContact.propertiesChanged = PropertyString.Annotation; //"Annotation";

                    contactInfoType meinfo = new contactInfoType();
                    meinfo.contactType = MessengerContactType.Me;

                    Annotation anno = new Annotation();
                    anno.Name = "MSN.IM.RoamLiveProperties";
                    anno.Value = "1";

                    meinfo.annotations = new Annotation[] { anno };
                    meContact.contactInfo = meinfo;
                    abcontactUpdateRequest.contacts = new ContactType[] { meContact };
                    try
                    {
                        NSMessageHandler.ContactService.ChangeCacheKeyAndPreferredHostForSpecifiedMethod(
                            abService, "ABContactUpdate", abcontactUpdateRequest);
                        abService.ABContactUpdate(abcontactUpdateRequest);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "ABContactUpdate error: " + ex.Message, GetType().Name);
                    }
                }

                //10. OK, there's no 10, that's all....
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private OwnerProfile GetProfileImpl(string scenario)
        {
            try
            {
                StorageService storageService = CreateStorageService(scenario);

                GetProfileRequestType request = new GetProfileRequestType();
                request.profileHandle = new Handle();
                request.profileHandle.Alias = new Alias();
                request.profileHandle.Alias.Name = Convert.ToString(NSMessageHandler.Owner.CID);
                request.profileHandle.Alias.NameSpace = "MyCidStuff";
                request.profileHandle.RelationshipName = "MyProfile";
                request.profileAttributes = new profileAttributes();
                request.profileAttributes.ExpressionProfileAttributes = CreateFullExpressionProfileAttributes();

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, "GetProfile", request);
                GetProfileResponse response = storageService.GetProfile(request);

                NSMessageHandler.ContactService.Deltas.Profile.DateModified = response.GetProfileResult.ExpressionProfile.DateModified;
                NSMessageHandler.ContactService.Deltas.Profile.ResourceID = response.GetProfileResult.ExpressionProfile.ResourceID;

                // Display name
                NSMessageHandler.ContactService.Deltas.Profile.DisplayName = response.GetProfileResult.ExpressionProfile.DisplayName;

                // Personal status
                NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = response.GetProfileResult.ExpressionProfile.PersonalStatus;

                // Display photo
                if (null != response.GetProfileResult.ExpressionProfile.Photo)
                {
                    if (NSMessageHandler.ContactService.Deltas.Profile.Photo.PreAthURL == response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL)
                    {
                        System.Drawing.Image fileImage = System.Drawing.Image.FromStream(NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage);
                        DisplayImage displayImage = new DisplayImage();
                        displayImage.Image = fileImage;

                        NSMessageHandler.Owner.DisplayImage = displayImage;
                    }
                    else
                    {
                        string requesturi = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;
                        if (requesturi.StartsWith("/"))
                        {
                            requesturi = "http://blufiles.storage.msn.com" + requesturi;  //I found it http://byfiles.storage.msn.com is also ok
                        }

                        // Don't urlencode t= :))
                        Uri uri = new Uri(requesturi + "?t=" + System.Web.HttpUtility.UrlEncode(NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket.Substring(2)));

                        HttpWebRequest fwr = (HttpWebRequest)WebRequest.Create(uri);
                        fwr.Proxy = WebProxy;
                        fwr.Timeout = 30000;
                        fwr.BeginGetResponse(delegate(IAsyncResult result)
                        {
                            try
                            {
                                Stream stream = ((WebRequest)result.AsyncState).EndGetResponse(result).GetResponseStream();
                                SerializableMemoryStream ms = new SerializableMemoryStream();
                                byte[] data = new byte[8192];
                                int read;
                                while ((read = stream.Read(data, 0, data.Length)) > 0)
                                {
                                    ms.Write(data, 0, read);
                                }
                                stream.Close();

                                NSMessageHandler.ContactService.Deltas.Profile.Photo.DateModified = response.GetProfileResult.ExpressionProfile.Photo.DateModified;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID = response.GetProfileResult.ExpressionProfile.Photo.ResourceID;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.PreAthURL = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = ms;
                                NSMessageHandler.ContactService.Deltas.Save();

                                System.Drawing.Image fileImage = System.Drawing.Image.FromStream(NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage);
                                DisplayImage displayImage = new DisplayImage();
                                displayImage.Image = fileImage;

                                NSMessageHandler.Owner.DisplayImage = displayImage;
                            }
                            catch (Exception ex)
                            {
                                if (ex is ThreadAbortException)
                                    return;

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "DisplayImage error: " + ex.Message, GetType().Name);
                            }

                        }, fwr);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLowerInvariant().Contains("does not exist"))
                {
                    CreateProfile();
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
            }

            return NSMessageHandler.ContactService.Deltas.Profile;
        }


        private void UpdateProfileImpl(string displayName, string personalStatus, string freeText, int flags)
        {
            OwnerProfile profile = NSMessageHandler.ContactService.Deltas.Profile;


            NSMessageHandler.ContactService.Deltas.Profile.DisplayName = displayName;
            NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = personalStatus;
            if (NSMessageHandler.Owner.RoamLiveProperty == RoamLiveProperty.Enabled)
            {
                StorageService storageService = CreateStorageService("RoamingIdentityChanged");

                UpdateProfileRequestType request = new UpdateProfileRequestType();
                request.profile = new UpdateProfileRequestTypeProfile();
                request.profile.ResourceID = profile.ResourceID;
                request.profile.ExpressionProfile = new ExpressionProfile();
                request.profile.ExpressionProfile.FreeText = freeText;  //DONOT set any default value of this field in the xsd file, default value will make this field missing.
                request.profile.ExpressionProfile.DisplayName = displayName;
                request.profile.ExpressionProfile.PersonalStatus = personalStatus;
                request.profile.ExpressionProfile.FlagsSpecified = true;
                request.profile.ExpressionProfile.Flags = flags;   //DONOT set any default value of this field in the xsd file, default value will make this field missing.

                try
                {
                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, "UpdateProfile", request);
                    storageService.UpdateProfile(request);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateProfile", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                    return;
                }

                // And get profile again
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl("RoamingIdentityChanged");

                // UpdateDynamicItem
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
                {
                    ABServiceBinding abService = NSMessageHandler.ContactService.CreateABService("RoamingIdentityChanged");

                    UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                    updateDyItemRequest.abId = Guid.Empty.ToString();

                    PassportDynamicItem passportDyItem = new PassportDynamicItem();
                    passportDyItem.Type = "Passport";
                    passportDyItem.PassportName = NSMessageHandler.Owner.Mail;
                    passportDyItem.Changes = "Notifications";
                    passportDyItem.Notifications = new NotificationDataType[] { new NotificationDataType() };
                    passportDyItem.Notifications[0].StoreService = new ServiceType();
                    passportDyItem.Notifications[0].StoreService.Info = new InfoType();
                    passportDyItem.Notifications[0].StoreService.Info.Handle = new HandleType();
                    passportDyItem.Notifications[0].StoreService.Info.Handle.Id = "0";
                    passportDyItem.Notifications[0].StoreService.Info.Handle.Type = ServiceFilterType.Profile;
                    passportDyItem.Notifications[0].StoreService.Info.Handle.ForeignId = "MyProfile";
                    passportDyItem.Notifications[0].StoreService.Info.IsBot = false;
                    passportDyItem.Notifications[0].StoreService.Info.InverseRequired = false;
                    passportDyItem.Notifications[0].StoreService.Changes = String.Empty;
                    passportDyItem.Notifications[0].Status = "Exist Access";
                    passportDyItem.Notifications[0].LastChanged = NSMessageHandler.ContactService.Deltas.Profile.DateModified;

                    updateDyItemRequest.dynamicItems = new PassportDynamicItem[] { passportDyItem };
                    try
                    {
                        NSMessageHandler.ContactService.ChangeCacheKeyAndPreferredHostForSpecifiedMethod(
                            abService, "UpdateDynamicItem", updateDyItemRequest);
                        abService.UpdateDynamicItem(updateDyItemRequest);
                    }
                    catch (Exception ex2)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateDynamicItem", ex2));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex2.Message, GetType().Name);
                        return;
                    }
                    NSMessageHandler.ContactService.handleServiceHeader(abService.ServiceHeaderValue, typeof(UpdateDynamicItemRequestType));
                    NSMessageHandler.ContactService.Deltas.Save();
                }

            }
            else
            {
                NSMessageHandler.ContactService.Deltas.Save();
            }

        }

        internal void ChangeCacheKeyAndPreferredHostForSpecifiedMethod(StorageService storageService, string methodName, object param)
        {
            if (NSMessageHandler != null && NSMessageHandler.ContactService != null)
            {
                NSMessageHandler.ContactService.ChangeCacheKeyAndPreferredHostForSpecifiedMethod(CacheKeyType.StorageServiceCacheKey, storageService, methodName, param);
                storageService.AffinityCacheHeaderValue.CacheKey = NSMessageHandler.ContactService.Deltas.CacheKeys[CacheKeyType.StorageServiceCacheKey];
            }
        }

        #endregion

        /// <summary>
        /// Get my profile. Display name, personal status and display photo.
        /// </summary>
        public OwnerProfile GetProfile()
        {
            if (NSMessageHandler.ContactService.Deltas == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                return null;
            }

            if (NSMessageHandler.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.MSNTicket != MSNTicket.Empty &&
                NSMessageHandler.ContactService.Deltas.Profile.DateModified < Convert.ToDateTime(NSMessageHandler.ContactService.AddressBook.MyProperties["lastchanged"]))
            {
                return GetProfileImpl("Initial");
            }

            return NSMessageHandler.ContactService.Deltas.Profile;
        }

        /// <summary>
        /// Update personal displayname and status in profile
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="personalStatus"></param>
        public void UpdateProfile(string displayName, string personalStatus)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("UpdateProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (NSMessageHandler.MSNTicket != MSNTicket.Empty &&
                (NSMessageHandler.ContactService.Deltas.Profile.DisplayName != displayName ||
                NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage != personalStatus))
            {
                UpdateProfileImpl(displayName, personalStatus, "Update", 0);
            }
        }


        /// <summary>
        /// Update the display photo of current user.
        /// <list type="bullet">
        /// <item>GetProfile with scenario = "RoamingIdentityChanged"</item>
        /// <item>UpdateProfile - Update the profile resource with Flags = 1</item>
        /// <item>DeleteRelationships - delete the photo resource</item>
        /// <item>DeleteRelationships - delete the expression profile resource (profile resource)</item>
        /// <item>CreateDocument</item>
        /// <item>CreateRelationships</item>
        /// <item>UpdateProfile - Update the profile resource again with Flags = 0</item>
        /// </list>
        /// </summary>
        /// <param name="photo">New photo to display</param>
        /// <param name="photoName">The resourcename</param>
        public bool UpdateProfile(Image photo, string photoName)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("UpdateProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                return false;
            }

            // 1. Getprofile
            NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl("RoamingIdentityChanged");

            // 2. UpdateProfile
            // To keep the order, we need a sync function.
            UpdateProfileImpl(NSMessageHandler.ContactService.Deltas.Profile.DisplayName,
                              NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage,
                              "Update", 1);
            if (NSMessageHandler.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                StorageService storageService = CreateStorageService("RoamingIdentityChanged");

                Alias mycidAlias = new Alias();
                mycidAlias.Name = Convert.ToString(NSMessageHandler.Owner.CID);
                mycidAlias.NameSpace = "MyCidStuff";

                // 3. DeleteRelationships
                if (!String.IsNullOrEmpty(NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID))
                {
                    // 3.1 UserTiles -> Photo
                    DeleteRelationshipsRequestType request = new DeleteRelationshipsRequestType();
                    request.sourceHandle = new Handle();
                    request.sourceHandle.RelationshipName = "/UserTiles";
                    request.sourceHandle.Alias = mycidAlias;
                    request.targetHandles = new Handle[] { new Handle() };
                    request.targetHandles[0].ResourceID = NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID;
                    try
                    {
                        storageService.DeleteRelationships(request);
                    }
                    catch (Exception ex)
                    {
                        OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("DeleteRelationships", ex));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                        return false;
                    }

                    //3.2 Profile -> Photo
                    request = new DeleteRelationshipsRequestType();
                    request.sourceHandle = new Handle();
                    request.sourceHandle.ResourceID = NSMessageHandler.ContactService.Deltas.Profile.ResourceID;
                    request.targetHandles = new Handle[] { new Handle() };
                    request.targetHandles[0].ResourceID = NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID;
                    try
                    {
                        storageService.DeleteRelationships(request);
                    }
                    catch (Exception ex)
                    {
                        OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("DeleteRelationships", ex));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                        return false;
                    }
                }

                // 4. CreateDocument
                SerializableMemoryStream mem = (SerializableMemoryStream)photo;
                CreateDocumentRequestType createDocRequest = new CreateDocumentRequestType();
                createDocRequest.relationshipName = "Messenger User Tile";
                createDocRequest.parentHandle = new Handle();
                createDocRequest.parentHandle.RelationshipName = "/UserTiles";
                createDocRequest.parentHandle.Alias = mycidAlias;
                createDocRequest.document = new Photo();
                createDocRequest.document.Name = photoName;
                createDocRequest.document.DocumentStreams = new PhotoStream[] { new PhotoStream() };
                createDocRequest.document.DocumentStreams[0].DataSize = 0;
                createDocRequest.document.DocumentStreams[0].MimeType = "png";
                createDocRequest.document.DocumentStreams[0].DocumentStreamType = "UserTileStatic";
                createDocRequest.document.DocumentStreams[0].Data = mem.ToArray();

                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = photo;  //Set to new photo
                NSMessageHandler.Owner.DisplayImage = displayImage;
                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage.Write(mem.ToArray(), 0, mem.ToArray().Length);
                NSMessageHandler.ContactService.Deltas.Save();
                string resId_Doc = String.Empty;
                try
                {
                    CreateDocumentResponseType createDocResponse = storageService.CreateDocument(createDocRequest);
                    resId_Doc = createDocResponse.CreateDocumentResult;
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("CreateDocument", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateDocument error: " + ex.Message, GetType().Name);
                    return false;
                }

                // 5. CreateRelationships, create a relationship between ProfileExpression role member and the new document.
                CreateRelationshipsRequestType createRelationshipRequest = new CreateRelationshipsRequestType();
                createRelationshipRequest.relationships = new Relationship[] { new Relationship() };
                createRelationshipRequest.relationships[0].RelationshipName = "ProfilePhoto";
                createRelationshipRequest.relationships[0].SourceType = "SubProfile"; //From SubProfile
                createRelationshipRequest.relationships[0].TargetType = "Photo";      //To Photo
                createRelationshipRequest.relationships[0].SourceID = NSMessageHandler.ContactService.Deltas.Profile.ResourceID;  //From Expression profile
                createRelationshipRequest.relationships[0].TargetID = resId_Doc;      //To Document                
                try
                {
                    storageService.CreateRelationships(createRelationshipRequest);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("CreateRelationships", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateRelationships error: " + ex.Message, GetType().Name);
                    return false;
                }

                //6. ok, done - Updateprofile again
                UpdateProfileImpl(NSMessageHandler.ContactService.Deltas.Profile.DisplayName,
                                  NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage,
                                  "Update", 0);

                return true;
            }

            return false;
        }
    }
};
