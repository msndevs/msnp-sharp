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

namespace MSNPSharp
{
    using MSNPSharp.Core;

    public static class MemberRole
    {
        public const string Allow = "Allow";
        public const string Block = "Block";
        public const string Reverse = "Reverse";
        public const string Pending = "Pending";
        public const string Hide = "Hide";
        public const string Admin = "Admin";
        public const string Contributor = "Contributor";
        public const string ProfileGeneral = "ProfileGeneral";
        public const string ProfilePersonalContact = "ProfilePersonalContact";
        public const string ProfileProfessionalContact = "ProfileProfessionalContact";
        public const string ProfileSocial = "ProfileSocial";
        public const string ProfileExpression = "ProfileExpression";
        public const string ProfileEducation = "ProfileEducation";
        public const string ProfilePicture = "ProfilePicture";
        public const string ProfileStatus = "ProfileStatus";
        public const string ProfilePage = "ProfilePage";
        public const string OneWayRelationship = "OneWayRelationship";
        public const string TwoWayRelationship = "TwoWayRelationship";
        public const string WebProfileList = "WebProfileList";
        public const string ApplicationRead = "ApplicationRead";
        public const string ApplicationWrite = "ApplicationWrite";
    }

    /// <summary>
    /// Membership type. The values of fields in this class is just as the same as their names.
    /// </summary>
    public static class MembershipType
    {
        public const string Passport = "Passport";
        public const string Email = "Email";
        public const string Phone = "Phone";
        public const string Role = "Role";
        public const string Service = "Service";
        public const string Everyone = "Everyone";
        public const string Partner = "Partner";
        public const string Domain = "Domain";
        public const string Circle = "Circle";
        public const string ExternalID = "ExternalID";
    }

    public static class MessengerContactType
    {
        public const string Me = "Me";
        public const string Regular = "Regular";
        public const string Messenger = "Messenger";
        public const string Live = "Live";
        public const string LivePending = "LivePending";
        public const string LiveRejected = "LiveRejected";
        public const string LiveDropped = "LiveDropped";
        public const string Circle = "Circle";
    }

    public static class ServiceFilterType
    {
        public const string Messenger = "Messenger";
        public const string IMAvailability = "IMAvailability";
        public const string Invitation = "Invitation";
        public const string SocialNetwork = "SocialNetwork";
        public const string Profile = "Profile";
        public const string Folder = "Folder";
        public const string Event = "Event";
        public const string OfficeLiveWebNotification = "OfficeLiveWebNotification";
        public const string CommunityQuestionAnswer = "CommunityQuestionAnswer";
    }

    [Flags]
    public enum ServiceShortNames
    {
        /// <summary>
        /// Peer to Peer (PE)
        /// </summary>
        PE = 1,
        /// <summary>
        /// Instant Messaging (IM)
        /// </summary>
        IM = 2,
        /// <summary>
        /// Private Data (for owner endpoint places)
        /// </summary>
        PD = 4,
        CM = 8,
        /// <summary>
        /// Profile (PF)
        /// </summary>
        PF = 16,
    }

    public static class ContactPhoneTypes
    {
        public const string ContactPhonePersonal = "ContactPhonePersonal";
        public const string ContactPhoneBusiness = "ContactPhoneBusiness";
        public const string ContactPhoneMobile = "ContactPhoneMobile";
        public const string ContactPhonePager = "ContactPhonePager";
        public const string ContactPhoneOther = "ContactPhoneOther";
        public const string ContactPhoneFax = "ContactPhoneFax";
        public const string Personal2 = "Personal2";
        public const string Business2 = "Business2";
        public const string BusinessFax = "BusinessFax";
        public const string BusinessMobile = "BusinessMobile";
        public const string Company = "Company";
    }

    /// <summary>
    /// Property string for <see cref="MSNPSharp.MSNWS.MSNABSharingService.ContactType"/>
    /// </summary>
    public static class PropertyString
    {
        public const string propertySeparator = " ";
        public const string Email = "Email";
        public const string IsMessengerEnabled = "IsMessengerEnabled";
        public const string Capability = "Capability";
        public const string Number = "Number";
        public const string Comment = "Comment";
        public const string DisplayName = "DisplayName";
        public const string Annotation = "Annotation";
        public const string IsMessengerUser = "IsMessengerUser";
        public const string MessengerMemberInfo = "MessengerMemberInfo";
        public const string ContactType = "ContactType";
        public const string ContactEmail = "ContactEmail";
        public const string ContactPhone = "ContactPhone";
        public const string GroupName = "GroupName";
        public const string HasSpace = "HasSpace";
    }

    public enum MsnServiceType
    {
        AB,
        Sharing,
        Storage,
        RSI,
        OIMStore,
        WhatsUp,
        Directory
    }

    public enum PartnerScenario
    {
        None,
        Initial,
        Timer,
        BlockUnblock,
        GroupSave,
        GeneralDialogApply,
        ContactSave,
        ContactMsgrAPI,
        MessengerPendingList,
        PrivacyApply,
        NewCircleDuringPull,
        CircleInvite,
        CircleIdAlert,
        CircleStatus,
        CircleSave,
        CircleLeave,
        JoinedCircleDuringPush,
        ABChangeNotifyAlert,
        RoamingSeed,
        RoamingIdentityChanged
    }

    /// <summary>
    /// Constants for webservice parameter.
    /// </summary>
    public static class WebServiceConstants
    {
        /// <summary>
        /// The messenger's default addressbook Id: 00000000-0000-0000-0000-000000000000.
        /// </summary>
        public const string MessengerIndividualAddressBookId = "00000000-0000-0000-0000-000000000000";

        /// <summary>
        /// The guid for messenger group(not circle): C8529CE2-6EAD-434d-881F-341E17DB3FF8.
        /// </summary>
        public const string MessengerGroupType = "C8529CE2-6EAD-434d-881F-341E17DB3FF8";

        /// <summary>
        /// The default time for requesting the full membership and addressbook list: 0001-01-01T00:00:00.0000000.
        /// </summary>
        public const string ZeroTime = "0001-01-01T00:00:00.0000000";

        public static string[] XmlDateTimeFormats = new string[]{
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:sszzz"
        };

        public const string NullDomainTag = "$null";


    }

    /// <summary>
    /// Different string for Name property of <see cref="MSNPSharp.MSNWS.MSNABSharingService.Annotation"/>
    /// </summary>
    public static class AnnotationNames
    {
        /// <summary>
        /// The value is: MSN.IM.InviteMessage
        /// </summary>
        public const string MSN_IM_InviteMessage = "MSN.IM.InviteMessage";

        /// <summary>
        /// The value is: MSN.IM.MPOP
        /// </summary>
        [Obsolete("",true)]
        public const string MSN_IM_MPOP = "MSN.IM.MPOP";

        /// <summary>
        /// The value is: MSN.IM.BLP
        /// </summary>
        [Obsolete("",true)]
        public const string MSN_IM_BLP = "MSN.IM.BLP";

        /// <summary>
        /// The value is: MSN.IM.GTC
        /// </summary>
        [Obsolete("", true)]
        public const string MSN_IM_GTC = "MSN.IM.GTC";

        /// <summary>
        /// The value is: MSN.IM.RoamLiveProperties
        /// </summary>
        [Obsolete("", true)]
        public const string MSN_IM_RoamLiveProperties = "MSN.IM.RoamLiveProperties";

        /// <summary>
        /// The value is: MSN.IM.MBEA
        /// </summary>
        [Obsolete("", true)]
        public const string MSN_IM_MBEA = "MSN.IM.MBEA";

        /// <summary>
        /// The value is: MSN.IM.Display
        /// </summary>
        public const string MSN_IM_Display = "MSN.IM.Display";

        /// <summary>
        /// The value is: MSN.IM.BuddyType
        /// </summary>
        public const string MSN_IM_BuddyType = "MSN.IM.BuddyType";

        /// <summary>
        /// The value is: AB.NickName
        /// </summary>
        public const string AB_NickName = "AB.NickName";

        /// <summary>
        /// The value is: AB.Profession
        /// </summary>
        public const string AB_Profession = "AB.Profession";

        /// <summary>
        /// The value is: Live.Locale
        /// </summary>
        public const string Live_Locale = "Live.Locale";

        /// <summary>
        /// The value is: Live.Profile.Expression.LastChanged
        /// </summary>
        public const string Live_Profile_Expression_LastChanged = "Live.Profile.Expression.LastChanged";

        /// <summary>
        /// The value is: Live.Passport.Birthdate
        /// </summary>
        public const string Live_Passport_Birthdate = "Live.Passport.Birthdate";
    }


    /// <summary>
    /// The type of addressbook.
    /// </summary>
    public static class AddressBookType
    {
        /// <summary>
        /// Circle.
        /// </summary>
        public const string Group = "Group";

        /// <summary>
        /// Default addressbook.
        /// </summary>
        public const string Individual = "Individual";
    }

    /// <summary>
    /// The relationship between a contact and circle.
    /// </summary>
    public enum CirclePersonalMembershipRole : int
    {
        None = 0,

        /// <summary>
        /// The contact is the circle admin, the value of RelationshipRole field in NetworkInfoType is 1.
        /// </summary>
        Admin = 1,

        /// <summary>
        /// The contact is a circle co-admin, the value of RelationshipRole field in NetworkInfoType is 2.
        /// </summary>
        AssistantAdmin = 2,

        /// <summary>
        /// The contact is a circle member, the value of RelationshipRole field in NetworkInfoType is 3.
        /// </summary>
        Member = 3,

        /// <summary>
        /// The contact is pending to the circle, the value of RelationshipRole field in NetworkInfoType is 4.
        /// </summary>
        StatePendingOutbound = 4
    }

    /// <summary>
    /// This is the value of different domain type of Network info list.
    /// </summary>
    internal static class DomainIds
    {
        /// <summary>
        /// Domain id for Windows Live addressbook in NetworkInfo.
        /// </summary>
        public const int WindowsLiveDomain = 1;

        /// <summary>
        /// Domain ID for facebook in NetworkInfo.
        /// </summary>
        public const int FaceBookDomain = 7;
        public const int ZUNEDomain = 3;

        public const int LinkedInDomain = 8;
        /// <summary>
        /// The domain ID for MySpace.
        /// </summary>
        public const int MySpaceDomain = 9;
    }

    /// <summary>
    /// The values in this class might be different from <see cref="RemoteNetworkGateways"/>
    /// </summary>
    public static class SourceId
    {
        public const string WindowsLive = "WL";
        /// <summary>
        /// The source Id for facebook, "FB".
        /// </summary>
        public const string FaceBook = "FB";
        /// <summary>
        /// The source Id for MySpace, "MYSP".
        /// </summary>
        public const string MySpace = "MYSP";
        /// <summary>
        /// The source Id for LinkedIn, "LI".
        /// </summary>
        public const string LinkedIn = "LI";
    }

    /// <summary>
    /// The addressbook relationship types.
    /// </summary>
    internal static class RelationshipTypes
    {
        /// <summary>
        /// The network info relationship is for individual addressbook (default addressbook).
        /// </summary>
        public const int IndividualAddressBook = 3;

        /// <summary>
        /// The network info relationship is for group addressbook (circle addressbook).
        /// </summary>
        public const int CircleGroup = 5;
    }

    /// <summary>
    /// Indicates the status of  contact in an addressbook.
    /// </summary>
    internal enum RelationshipState : uint
    {
        None = 0,

        /// <summary>
        /// The remote circle owner invite you to join,, pending your response.
        /// </summary>
        WaitingResponse = 1,

        /// <summary>
        /// The contact is deleted by one of the domain owners.
        /// </summary>
        Left = 2,

        /// <summary>
        /// The contact is in the circle's addressbook list.
        /// </summary>
        Accepted = 3,

        /// <summary>
        /// The contact already left the circle.
        /// </summary>
        Rejected = 4
    }
};
