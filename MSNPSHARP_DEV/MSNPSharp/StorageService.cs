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
    using MSNPSharp.Core;

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
            expAttrib.Attachments = true;
            expAttrib.AttachmentsSpecified = true;
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
            if (NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile == false)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "No expression profile exists, create profile skipped.");
                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = Properties.Resources.WLXLarge_default;  //Set default
                NSMessageHandler.ContactList.Owner.DisplayImage = displayImage;

                return;
            }

            try
            {
                MsnServiceState serviceState = new MsnServiceState(PartnerScenario.RoamingSeed, "CreateProfile", false);
                StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);
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
                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, createRequest);
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
                    serviceState = new MsnServiceState(PartnerScenario.RoamingSeed, "AddMember", false);
                    SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, serviceState);
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
                        ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, MsnServiceType.Sharing, serviceState, addMemberRequest);
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
                alias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);

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
                MemoryStream mem = SerializableMemoryStream.FromImage(Properties.Resources.WLXLarge_default);
                photoStream.Data = mem.ToArray();
                createDocRequest.document.DocumentStreams = new PhotoStream[] { photoStream };

                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = Properties.Resources.WLXLarge_default;  //Set default
                NSMessageHandler.ContactList.Owner.DisplayImage = displayImage;

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
                expProf.DisplayName = NSMessageHandler.ContactList.Owner.NickName;
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
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.Initial);

                //7. FindDocuments Hmm....


                //8. UpdateDynamicItem
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
                {
                    serviceState = new MsnServiceState(PartnerScenario.RoamingSeed, "UpdateDynamicItem", false);
                    ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, serviceState);
                    abService.AllowAutoRedirect = true;

                    UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                    updateDyItemRequest.abId = Guid.Empty.ToString();

                    PassportDynamicItem passportDyItem = new PassportDynamicItem();
                    passportDyItem.Type = "Passport";
                    passportDyItem.PassportName = NSMessageHandler.ContactList.Owner.Mail;
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
                        ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, updateDyItemRequest);
                        abService.UpdateDynamicItem(updateDyItemRequest);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "UpdateDynamicItem error: You don't receive any contact updates, vice versa! " + ex.Message, GetType().Name);
                    }

                    //9. ABContactUpdate
                    ABContactUpdateRequestType abcontactUpdateRequest = new ABContactUpdateRequestType();
                    abcontactUpdateRequest.abId = Guid.Empty.ToString();

                    ContactType meContact = new ContactType();
                    meContact.propertiesChanged = PropertyString.Annotation; //"Annotation";

                    contactInfoType meinfo = new contactInfoType();
                    meinfo.contactType = MessengerContactType.Me;

                    Annotation anno = new Annotation();
                    anno.Name = AnnotationNames.MSN_IM_RoamLiveProperties;
                    anno.Value = "1";

                    meinfo.annotations = new Annotation[] { anno };
                    meContact.contactInfo = meinfo;
                    abcontactUpdateRequest.contacts = new ContactType[] { meContact };
                    try
                    {
                        serviceState = new MsnServiceState(PartnerScenario.ContactSave, "ABContactUpdate", false);
                        ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, abcontactUpdateRequest);
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

        private OwnerProfile GetProfileImpl(PartnerScenario scenario)
        {
            try
            {
                MsnServiceState serviceState = new MsnServiceState(scenario, "GetProfile", false);
                StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

                GetProfileRequestType request = new GetProfileRequestType();
                request.profileHandle = new Handle();
                request.profileHandle.Alias = new Alias();
                request.profileHandle.Alias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);
                request.profileHandle.Alias.NameSpace = "MyCidStuff";
                request.profileHandle.RelationshipName = "MyProfile";
                request.profileAttributes = new profileAttributes();
                request.profileAttributes.ExpressionProfileAttributes = CreateFullExpressionProfileAttributes();

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, request);
                GetProfileResponse response = storageService.GetProfile(request);

                if (response.GetProfileResult.ExpressionProfile == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Get profile cannot get expression profile of this owner.");
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = false;
                    NSMessageHandler.ContactService.Deltas.Profile.DisplayName = NSMessageHandler.ContactList.Owner.Name;
                    return NSMessageHandler.ContactService.Deltas.Profile;
                }
                else
                {
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = true;
                }

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
                        DisplayImage newDisplayImage = new DisplayImage();
                        newDisplayImage.Image = fileImage;

                        NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;
                    }
                    else
                    {
                        string requesturi = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;
                        if (requesturi.StartsWith("/"))
                        {
                            requesturi = "http://blufiles.storage.msn.com" + requesturi;  //I found it http://byfiles.storage.msn.com is also ok
                        }

                        // Don't urlencode t= :))
                        string usertitleURL = requesturi + "?t=" + System.Web.HttpUtility.UrlEncode(NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket.Substring(2));
                        SyncUserTile(usertitleURL,
                            delegate(object nullParam)
                            {
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = response.GetProfileResult.ExpressionProfile.Photo.Name;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.DateModified = response.GetProfileResult.ExpressionProfile.Photo.DateModified;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID = response.GetProfileResult.ExpressionProfile.Photo.ResourceID;
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.PreAthURL = response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams[0].PreAuthURL;

                                SerializableMemoryStream ms = new SerializableMemoryStream();
                                NSMessageHandler.ContactList.Owner.DisplayImage.Image.Save(ms, NSMessageHandler.ContactList.Owner.DisplayImage.Image.RawFormat);
                                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = ms;
                                NSMessageHandler.ContactService.Deltas.Save(true);
                            },
                            null,
                            delegate(object param)
                            {
                                Exception ex = param as Exception;
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Get DisplayImage error: " + ex.Message, GetType().Name);
                                if (NSMessageHandler.ContactList.Owner.UserTile != null)
                                {
                                    SyncUserTile(NSMessageHandler.ContactList.Owner.UserTile.AbsoluteUri, null, null, null);
                                }
                            });

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

        internal delegate void GetUsertitleByURLhandler(object param);

        internal void SyncUserTile(string usertitleURL, GetUsertitleByURLhandler callBackHandler, object param, GetUsertitleByURLhandler errorHandler)
        {
            try
            {
                Uri uri = new Uri(usertitleURL);

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

                        System.Drawing.Image fileImage = System.Drawing.Image.FromStream(ms);
                        DisplayImage newDisplayImage = new DisplayImage();
                        newDisplayImage.Image = fileImage;

                        NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;
                        if (callBackHandler != null)
                        {
                            callBackHandler(param);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (errorHandler != null)
                            errorHandler(ex);

                        return;
                    }

                }, fwr);
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                    errorHandler(e);

                return;
            }
        }


        private void UpdateProfileImpl(string displayName, string personalStatus, string freeText, int flags)
        {
            NSMessageHandler.ContactService.Deltas.Profile.DisplayName = displayName;
            NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = personalStatus;

            if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile &&
                NSMessageHandler.BotMode == false)
            {
                MsnServiceState serviceState = new MsnServiceState(PartnerScenario.RoamingIdentityChanged, "UpdateProfile", false);
                StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

                UpdateProfileRequestType request = new UpdateProfileRequestType();
                request.profile = new UpdateProfileRequestTypeProfile();
                request.profile.ResourceID = NSMessageHandler.ContactService.Deltas.Profile.ResourceID;
                request.profile.ExpressionProfile = new ExpressionProfile();
                request.profile.ExpressionProfile.FreeText = freeText;  //DONOT set any default value of this field in the xsd file, default value will make this field missing.
                request.profile.ExpressionProfile.DisplayName = displayName;
                request.profile.ExpressionProfile.PersonalStatus = personalStatus;
                request.profile.ExpressionProfile.FlagsSpecified = true;
                request.profile.ExpressionProfile.Flags = flags;   //DONOT set any default value of this field in the xsd file, default value will make this field missing.

                try
                {
                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, request);
                    storageService.UpdateProfile(request);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateProfile", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                    return;
                }

                // And get profile again
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.RoamingIdentityChanged);

                // UpdateDynamicItem
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
                {
                    serviceState = new MsnServiceState(PartnerScenario.RoamingIdentityChanged, "UpdateDynamicItem", false);
                    ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, serviceState);

                    UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                    updateDyItemRequest.abId = Guid.Empty.ToString();

                    PassportDynamicItem passportDyItem = new PassportDynamicItem();
                    passportDyItem.Type = "Passport";
                    passportDyItem.PassportName = NSMessageHandler.ContactList.Owner.Mail;
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
                        ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, updateDyItemRequest);
                        abService.UpdateDynamicItem(updateDyItemRequest);
                    }
                    catch (Exception ex2)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "You don't receive any contact updates, vice versa! " + ex2.Message, GetType().Name);
                        return;
                    }
                    finally
                    {
                        NSMessageHandler.ContactService.Deltas.Save();
                    }
                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Roaming disabled or invalid expression profile. Update skipped.");
                NSMessageHandler.ContactService.Deltas.Save();
            }

        }

        #endregion

        /// <summary>
        /// Get my profile. Display name, personal status and display photo.
        /// </summary>
        public OwnerProfile GetProfile()
        {
            if (NSMessageHandler.BotMode == false)
            {
                if (NSMessageHandler.ContactService.Deltas == null)
                {
                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                    return null;
                }

                if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled && NSMessageHandler.MSNTicket != MSNTicket.Empty)
                {
                    DateTime deltasProfileDateModified = WebServiceDateTimeConverter.ConvertToDateTime(NSMessageHandler.ContactService.Deltas.Profile.DateModified);
                    DateTime annotationLiveProfileExpressionLastChanged = WebServiceDateTimeConverter.ConvertToDateTime(NSMessageHandler.ContactService.AddressBook.MyProperties[AnnotationNames.Live_Profile_Expression_LastChanged]);

                    if ((annotationLiveProfileExpressionLastChanged == DateTime.MinValue) ||
                        (deltasProfileDateModified < annotationLiveProfileExpressionLastChanged))
                    {
                        return GetProfileImpl(PartnerScenario.Initial);
                    }
                }
            }
            else
            {
                if (NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage == null)
                {
                    SerializableMemoryStream serStream = new SerializableMemoryStream();
                    Properties.Resources.WLXLarge_default.Save(serStream, Properties.Resources.WLXLarge_default.RawFormat);

                    NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = serStream;
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = false;
                    NSMessageHandler.ContactService.Deltas.Save();
                }
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

            if (NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile == false)  //Non-expression id or provisioned account.
            {
                NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
                photo.Save(NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage, photo.RawFormat);
                NSMessageHandler.ContactService.Deltas.Save();

                Image fileImage = Image.FromStream(NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage);  //Make a copy.
                DisplayImage newDisplayImage = new DisplayImage();
                newDisplayImage.Image = fileImage;

                NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "No expression profile exists, new profile is saved locally.");
                return true;
            }

            if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage,
                    new MsnServiceState(PartnerScenario.RoamingIdentityChanged, "UpdateDocument", false));

                // 1. Getprofile
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.RoamingIdentityChanged);

                // 1.1 UpdateDocument
                if (!String.IsNullOrEmpty(NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID))
                {
                    UpdateDocumentRequestType request = new UpdateDocumentRequestType();
                    request.document = new Photo();
                    request.document.Name = NSMessageHandler.ContactService.Deltas.Profile.Photo.Name;
                    request.document.ResourceID = NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID;
                    request.document.DocumentStreams = new PhotoStream[] { new PhotoStream() };
                    request.document.DocumentStreams[0].DataSize = 0;
                    request.document.DocumentStreams[0].MimeType = "image/png";
                    request.document.DocumentStreams[0].DocumentStreamType = "UserTileStatic";
                    request.document.DocumentStreams[0].Data = SerializableMemoryStream.FromImage(photo).ToArray();

                    try
                    {
                        storageService.UpdateDocument(request);

                        // UpdateDynamicItem
                        UpdateProfileImpl(NSMessageHandler.ContactService.Deltas.Profile.DisplayName,
                                  NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage,
                                  "Update", 0); // 1= begin transaction, 0=commit transaction

                        NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
                        NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
                        NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage.Write(request.document.DocumentStreams[0].Data, 0, request.document.DocumentStreams[0].Data.Length);
                        NSMessageHandler.ContactService.Deltas.Save(true);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateDocument", ex));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Creating new document", GetType().Name);
                    }
                }

                // 2. UpdateProfile
                // To keep the order, we need a sync function.
                UpdateProfileImpl(NSMessageHandler.ContactService.Deltas.Profile.DisplayName,
                                  NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage,
                                  "Update", 1); // 1= begin transaction, 0=commit transaction

                Alias mycidAlias = new Alias();
                mycidAlias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);
                mycidAlias.NameSpace = "MyCidStuff";

                // 3. DeleteRelationships. If an error occurs, don't return, continue...
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
                    }
                }

                // 4. CreateDocument
                SerializableMemoryStream mem = SerializableMemoryStream.FromImage(photo);
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

                NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
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
                                  "Update", 0); // 1= begin transaction, 0=commit transaction

                return true;
            }

            return false;
        }
    }
};
