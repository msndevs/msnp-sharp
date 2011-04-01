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

    /// <summary>
    /// Msn protocol speaking
    /// </summary>
    public enum MsnProtocol
    {
        MSNP21 = 21
    }

    /// <summary>
    /// Specifies the type of proxy servers that can be used
    /// </summary>
    public enum ProxyType
    {
        /// <summary>No proxy server.</summary>
        None,
        /// <summary>A SOCKS4[A] proxy server.</summary>
        Socks4,
        /// <summary>A SOCKS5 proxy server.</summary>
        Socks5
    }

    /// <summary>
    /// Specifieds the type of a notification message.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// A message a remote contact send from a mobile device.
        /// </summary>
        Mobile = 0,
        /// <summary>
        /// A calendar reminder.
        /// </summary>
        Calendar = 1,
        /// <summary>
        /// An alert notification.
        /// </summary>
        Alert = 2
    }

    /// <summary>
    /// Specifies the online presence state
    /// </summary>
    public enum PresenceStatus
    {
        /// <summary>
        /// Unknown presence state.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Contact is offline (or a remote contact is hidden).
        /// </summary>
        Offline,
        /// <summary>
        /// The client owner is hidden.
        /// </summary>
        Hidden,
        /// <summary>
        /// The contact is online.
        /// </summary>
        Online,
        /// <summary>
        /// The contact is away.
        /// </summary>
        Away,
        /// <summary>
        /// The contact is busy.
        /// </summary>
        Busy,
        /// <summary>
        /// The contact will be right back.
        /// </summary>
        BRB,
        /// <summary>
        /// The contact is out to lunch.
        /// </summary>
        Lunch,
        /// <summary>
        /// The contact is on the phone.
        /// </summary>
        Phone,
        /// <summary>
        /// The contact is idle.
        /// </summary>
        Idle
    }

    /// <summary>
    /// Defines why a user has (been) signed off.
    /// </summary>
    /// <remarks>
    /// <b>OtherClient</b> is used when this account has signed in from another location. <b>ServerDown</b> is used when the msn server is going down.
    /// </remarks>
    public enum SignedOffReason
    {
        /// <summary>
        /// None.
        /// </summary>
        None,
        /// <summary>
        /// User logged in on the other client.
        /// </summary>
        OtherClient,
        /// <summary>
        /// Server went down.
        /// </summary>
        ServerDown
    }

    /// <summary>
    /// Roles used in the messenger network
    /// </summary>
    [FlagsAttribute]
    public enum RoleLists
    {
        /// <summary>
        /// No msn list
        /// </summary>
        None = 0,
        /// <summary>
        /// All contacts in your contactlist. You can send messages to those people.
        /// </summary>
        Forward = 1,
        /// <summary>
        /// All contacts who are allowed to see your status.
        /// </summary>
        Allow = 2,
        /// <summary>
        /// All contacts who you have blocked.
        /// </summary>
        //[Obsolete("", true)]
        Block = 4,
        /// <summary>
        /// All contacts who have you on their contactlist.
        /// </summary>
        //[Obsolete("",true)]
        Reverse = 8,
        /// <summary>
        /// All pending (for approval) contacts.
        /// </summary>
        Pending = 16,
        /// <summary>
        /// Forward+Block
        /// </summary>
        HideCompat = 5,
        /// <summary>
        /// Application contact
        /// </summary>
        ApplicationContact = 32,
        /// <summary>
        /// Show me offline but you can receive/send offline messages.
        /// </summary>
        Hide = 64
    }

    /// <summary>
    /// The functions a (remote) client supports.
    /// </summary>
    [Flags]
    public enum ClientCapabilities : long
    {
        None = 0x00,
        OnlineViaMobile = 0x01,
        OnlineViaTexas = 0x02,
        SupportsGifInk = 0x04,
        SupportsIsfInk = 0x08,
        WebCamDetected = 0x10,
        SupportsChunking = 0x20,
        MobileEnabled = 0x40,
        WebWatchEnabled = 0x80,
        SupportsActivities = 0x100,
        OnlineViaWebIM = 0x200,
        MobileDevice = 0x400,
        OnlineViaFederatedInterface = 0x800,
        HasSpace = 0x1000,
        IsMceUser = 0x2000,
        SupportsDirectIM = 0x4000,
        SupportsWinks = 0x8000,
        SupportsSharedSearch = 0x10000,
        IsBot = 0x20000,
        SupportsVoiceIM = 0x40000,
        SupportsSChannel = 0x80000,
        SupportsSipInvite = 0x100000,
        SupportsMultipartyMedia = 0x200000,
        SupportsSDrive = 0x400000,
        SupportsPageModeMessaging = 0x800000,
        HasOneCare = 0x1000000,
        SupportsTurn = 0x2000000,
        SupportsDirectBootstrapping = 0x4000000,
        UsingAlias = 0x8000000,

        /// <summary>
        /// MSNC1
        /// </summary>
        AppVersion60 = 0x10000000,
        /// <summary>
        /// MSNC2
        /// </summary>
        AppVersion61 = 0x20000000,
        /// <summary>
        /// MSNC3
        /// </summary>
        AppVersion62 = 0x30000000,
        /// <summary>
        /// MSNC4
        /// </summary>
        AppVersion70 = 0x40000000,
        /// <summary>
        /// MSNC5
        /// </summary>
        AppVersion75 = 0x50000000,
        /// <summary>
        /// MSNC6
        /// </summary>
        AppVersion80 = 0x60000000,
        /// <summary>
        ///MSNC7
        /// </summary>
        AppVersion81 = 0x70000000,
        /// <summary>
        /// MSNC8 (MSNP15)
        /// </summary>
        AppVersion85 = 0x80000000,
        /// <summary>
        /// MSNC9 (MSNP16)
        /// </summary>
        AppVersion90 = 0x90000000,
        /// <summary>
        /// MSNC10 - MSN 14.0, Wave 3 (MSNP18)
        /// </summary>
        AppVersion2009 = 0xA0000000,
        /// <summary>
        /// MSNC11 - MSN 15.0, Wave 4 (MSNP21)
        /// </summary>
        AppVersion2011 = 0xB0000000,

        AppVersion____ = 0xC0000000,
        AppVersion2___ = 0xD0000000,
        AppVersion20__ = 0xE0000000,

        /// <summary>
        /// Mask for MSNC
        /// </summary>
        AppVersionMask = 0xF0000000,

        DefaultIM = SupportsChunking | SupportsActivities | SupportsWinks | AppVersion2011,
        DefaultPE = SupportsTurn | SupportsDirectBootstrapping
    }

    [Flags]
    public enum ClientCapabilitiesEx : long
    {
        None = 0x00,
        IsSmsOnly = 0x01,
        SupportsVoiceOverMsnp = 0x02,
        SupportsUucpSipStack = 0x04,
        SupportsApplicationMessages = 0x08,
        RTCVideoEnabled = 0x10,
        SupportsPeerToPeerV2 = 0x20,
        IsAuthenticatedWebIMUser = 0x40,
        Supports1On1ViaGroup = 0x80,
        SupportsOfflineIM = 0x100,
        SupportsSharingVideo = 0x200,
        SupportsNudges = 0x400,   // (((:)))
        CircleVoiceIMEnabled = 0x800,
        SharingEnabled = 0x1000,
        MobileSuspendIMFanoutDisable = 0x2000,
        _0x4000 = 0x4000,
        SupportsPeerToPeerMixerRelay = 0x8000,
        _0x10000 = 0x10000,
        ConvWindowFileTransfer = 0x20000,
        VideoCallSupports16x9 = 0x40000,
        SupportsPeerToPeerEnveloping = 0x80000,
        _0x100000 = 0x100000,
        _0x200000 = 0x200000,
        YahooIMDisabled = 0x400000,
        SIPTunnelVersion2 = 0x800000,
        VoiceClipSupportsWMAFormat = 0x1000000,
        VoiceClipSupportsCircleIM = 0x2000000,
        SupportsSocialNewsObjectTypes = 0x4000000,
        CustomEmoticonsCapable = 0x8000000,
        SupportsUTF8MoodMessages = 0x10000000,
        FTURNCapable = 0x20000000,
        SupportsP4Activity = 0x40000000,
        SupportsMultipartyConversations = 0x80000000,

        DefaultIM = Supports1On1ViaGroup | SupportsOfflineIM | SupportsSharingVideo | SupportsNudges | SharingEnabled | ConvWindowFileTransfer | SIPTunnelVersion2 | CustomEmoticonsCapable | SupportsUTF8MoodMessages | SupportsMultipartyConversations,
        DefaultPE = _0x4000 | SupportsPeerToPeerMixerRelay | _0x10000 | SupportsPeerToPeerEnveloping | _0x100000 | SupportsSocialNewsObjectTypes | SupportsP4Activity
    }

    /// <summary>
    /// The text decorations messenger sends with a message
    /// </summary>
    [FlagsAttribute]
    public enum TextDecorations
    {
        /// <summary>
        /// No decoration.
        /// </summary>
        None = 0,
        /// <summary>
        /// Bold.
        /// </summary>
        Bold = 1,
        /// <summary>
        /// Italic.
        /// </summary>
        Italic = 2,
        /// <summary>
        /// Underline.
        /// </summary>
        Underline = 4,
        /// <summary>
        /// Strike-trough.
        /// </summary>
        Strike = 8
    }

    /// <summary>
    /// Types of media used on UBX command
    /// </summary>
    public enum MediaType
    {
        None = 0,
        Music = 1,
        Games = 2,
        Office = 3
    }

    /// <summary>
    /// A charset that can be used in a message.
    /// </summary>
    public enum MessageCharSet
    {
        /// <summary>
        /// ANSI
        /// </summary>
        Ansi = 0,
        /// <summary>
        /// Default charset.
        /// </summary>
        Default = 1,
        /// <summary>
        /// Symbol.
        /// </summary>
        Symbol = 2,
        /// <summary>
        /// Mac.
        /// </summary>
        Mac = 77,
        /// <summary>
        /// Shiftjis.
        /// </summary>
        Shiftjis = 128,
        /// <summary>
        /// Hangeul.
        /// </summary>
        Hangeul = 129,
        /// <summary>
        /// Johab.
        /// </summary>
        Johab = 130,
        /// <summary>
        /// GB2312.
        /// </summary>
        GB2312 = 134,
        /// <summary>
        /// Chines Big 5.
        /// </summary>
        ChineseBig5 = 136,
        /// <summary>
        /// Greek.
        /// </summary>
        Greek = 161,
        /// <summary>
        /// Turkish.
        /// </summary>
        Turkish = 162,
        /// <summary>
        /// Vietnamese.
        /// </summary>
        Vietnamese = 163,
        /// <summary>
        /// Hebrew.
        /// </summary>
        Hebrew = 177,
        /// <summary>
        /// Arabic.
        /// </summary>
        Arabic = 178,
        /// <summary>
        /// Baltic.
        /// </summary>
        Baltic = 186,
        /// <summary>
        /// Russian.
        /// </summary>
        Russian = 204,
        /// <summary>
        /// Thai.
        /// </summary>
        Thai = 222,
        /// <summary>
        /// Eastern Europe.
        /// </summary>
        EastEurope = 238,
        /// <summary>
        /// OEM.
        /// </summary>
        Oem = 255
    }

    /// <summary>
    /// Instant message address info type.
    /// </summary>
    public enum IMAddressInfoType : int
    {
        /// <summary>
        /// No client
        /// </summary>
        None = 0,

        /// <summary>
        /// Passport address
        /// </summary>
        WindowsLive = 1,

        /// <summary>
        /// Office Communicator address
        /// </summary>
        OfficeCommunicator = 2,

        /// <summary>
        /// Telephone number
        /// </summary>
        Telephone = 4,

        /// <summary>
        /// Mobile network
        /// </summary>
        MobileNetwork = 8,

        /// <summary>
        /// MSN group
        /// </summary>
        Circle = 9,

        /// <summary>
        /// Temporary group
        /// </summary>
        TemporaryGroup = 10,

        /// <summary>
        /// CID
        /// </summary>
        Cid = 11,

        /// <summary>
        /// Connect
        /// </summary>
        Connect = 13,

        /// <summary>
        /// Remote network, like FB
        /// </summary>
        RemoteNetwork = 14,

        /// <summary>
        /// Smtp
        /// </summary>
        Smtp = 16,

        /// <summary>
        /// Yahoo! address
        /// </summary>
        Yahoo = 32
    }

    /// <summary>
    /// Specifies an error a MSN Server can send.
    /// </summary>	
    public enum MSNError
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,
        /// <summary>
        /// Syntax error.
        /// </summary>
        SyntaxError = 200,
        /// <summary>
        /// Invalid parameter.
        /// </summary>
        InvalidParameter = 201,
        /// <summary>
        /// Invalid federated user
        /// </summary>
        InvalidFederatedUser = 203,
        /// <summary>
        /// Invalid contact network
        /// </summary>
        UnroutableUser = 204,
        /// <summary>
        /// Invalid user.
        /// </summary>
        InvalidUser = 205,
        /// <summary>
        /// Missing domain.
        /// </summary>
        MissingDomain = 206,
        /// <summary>
        /// The user is already logged in.
        /// </summary>
        AlreadyLoggedIn = 207,
        /// <summary>
        /// The username specified is invalid.
        /// </summary>
        InvalidUsername = 208,
        /// <summary>
        /// The full username specified is invalid.
        /// </summary>
        InvalidFullUsername = 209,
        /// <summary>
        /// User's contact list is full.
        /// </summary>
        UserListFull = 210,
        /// <summary>
        /// Invalid Name Request.
        /// </summary>		
        InvalidNameRequest = 213,
        /// <summary>
        /// User is already specified.
        /// </summary>		
        UserAlreadyThere = 215,
        /// <summary>
        /// User is already on the list.
        /// </summary>
        UserAlreadyOnList = 216,
        /// <summary>
        /// User is not accepting instant messages.
        /// </summary>
        NotAcceptingIMs = 217,
        /// <summary>
        /// Already in stated mode.
        /// </summary>
        AlreadyInMode = 218,
        /// <summary>
        /// User is in opposite (conflicting) list.
        /// </summary>
        UserInOppositeList = 219,
        /// <summary>
        /// NotAcceptingPages
        /// </summary>
        NotAcceptingPages = 220,
        /// <summary>
        /// Too Many Groups.
        /// </summary>
        TooManyGroups = 223,
        /// <summary>
        /// Invalid Group.
        /// </summary>
        InvalidGroup = 224,
        /// <summary>
        /// Principal not in group.
        /// </summary>
        PrincipalNotInGroup = 225,
        /// <summary>
        /// Principal not in group.
        /// </summary>
        GroupNotEmpty = 227,
        /// <summary>
        /// Contactgroup name already exists.
        /// </summary>
        ContactGroupNameExists = 228,
        /// <summary>
        /// Group name too long.
        /// </summary>
        GroupNameTooLong = 229,
        /// <summary>
        /// Cannot remove group zero
        /// </summary>
        CannotRemoveGroupZero = 230,
        /// <summary>
        /// InvalidMsisdn
        /// </summary>
        InvalidMsisdn = 232,
        /// <summary>
        /// UnknownMsisdn
        /// </summary>
        UnknownMsisdn = 233,
        /// <summary>
        /// UnknownKeitaiDomain
        /// </summary>
        UnknownKeitaiDomain = 234,
        /// <summary>
        /// If <d/> domain element specified in <ml/> mail list, at least one <c/> contact  must be exists
        /// </summary>
        EmptyDomainElement = 240,
        /// <summary>
        /// ADL/RML commands accept FL(1)/AL(2)/BL(4) BUT RL(8)/PL(16).
        /// </summary>
        InvalidXmlData = 241,
        /// <summary>
        /// Switchboard request failed.
        /// </summary>
        SwitchboardFailed = 280,
        /// <summary>
        /// Switchboard transfer failed.
        /// </summary>
        SwitchboardTransferFailed = 281,
        /// <summary>
        /// Unknown P2P application.
        /// </summary>
        UnknownP2PApp = 282,
        /// <summary>
        /// UnknownUunApp
        /// </summary>
        UnknownUunApp = 283,
        /// <summary>
        /// MessageTooLong
        /// </summary>
        MessageTooLong = 285,
        /// <summary>
        /// SmsJustOutOfFunds
        /// </summary>
        SmsJustOutOfFunds = 290,
        /// <summary>
        /// Required field is missing.
        /// </summary>
        MissingRequiredField = 300,
        /// <summary>
        /// User is not logged in.
        /// </summary>
        NotLoggedIn = 302,
        /// <summary>
        /// Error accessing contact list.
        /// </summary>
        ErrorAccessingContactList = 402,
        /// <summary>
        /// Error accessing contact list.
        /// </summary>
        AddressBookError = 403,
        /// <summary>
        /// SmsSubscriptionRequired
        /// </summary>
        SmsSubscriptionRequired = 413,
        /// <summary>
        /// SmsSubscriptionDisabled
        /// </summary>
        SmsSubscriptionDisabled = 414,
        /// <summary>
        /// SmsOutOfFunds
        /// </summary>
        SmsOutOfFunds = 415,
        /// <summary>
        /// SmsDisabledMarket
        /// </summary>
        SmsDisabledMarket = 416,
        /// <summary>
        /// SmsDisabledGlobal
        /// </summary>
        SmsDisabledGlobal = 417,
        /// <summary>
        /// TryAgainLater
        /// </summary>
        TryAgainLater = 418,
        /// <summary>
        /// NoMarketSpecified
        /// </summary>
        NoMarketSpecified = 419,
        /// <summary>
        /// Invalid account permissions.
        /// </summary>
        InvalidAccountPermissions = 420,
        /// <summary>
        /// Internal server error.
        /// </summary>
        InternalServerError = 500,
        /// <summary>
        /// Databaseserver error.
        /// </summary>
        DatabaseServerError = 501,
        /// <summary>
        /// Command Disabled.
        /// </summary>
        CommandDisabled = 502,
        /// <summary>
        /// UpsDown
        /// </summary>
        UpsDown = 504,
        /// <summary>
        /// FederatedPartnerError
        /// </summary>
        FederatedPartnerError = 508,
        /// <summary>
        /// PageModeMessageError
        /// </summary>
        PageModeMessageError = 509,
        /// <summary>
        /// File operation failed. 
        /// </summary>
        FileOperationFailed = 510,
        /// <summary>
        /// DetailedError. 
        /// </summary>
        DetailedError = 511,
        /// <summary>
        /// Memory allocation failure.
        /// </summary>
        MemoryAllocationFailed = 520,
        /// <summary>
        /// Challenge response failed.
        /// </summary>
        ChallengeResponseFailed = 540,

        SmsAccountMuted = 550,
        SmsAccountDisabled = 551,
        SmsAccountMaxed = 552,
        SmsInternalServerError = 580,
        SmsCarrierInvalid = 590,
        SmsCarrierNoRoute = 591,
        SmsCarrierErrored = 592,
        SmsAddressMappingFull = 593,
        SmsIncorrectSourceCountry = 594,
        SmsMobileCacheFull = 595,
        SmsIncorrectFormat = 596,
        SmsInvalidText = 597,
        SmsMessageTooLong = 598,


        /// <summary>
        /// Server is busy.
        /// </summary>
        ServerIsBusy = 600,
        /// <summary>
        /// Server is unavailable.
        /// </summary>
        ServerIsUnavailable = 601,
        /// <summary>
        /// Nameserver is down.
        /// </summary>
        NameServerDown = 602,
        /// <summary>
        /// Database connection failed.
        /// </summary>
        DatabaseConnectionFailed = 603,
        /// <summary>
        /// Server is going down.
        /// </summary>
        ServerGoingDown = 604,
        /// <summary>
        /// Server is unavailable.
        /// </summary>
        ServerUnavailable = 605,
        /// <summary>
        /// PagingUnavailable
        /// </summary>
        PagingUnavailable = 606,
        /// <summary>
        /// Connection creation failed.
        /// </summary>
        CouldNotCreateConnection = 700,
        /// <summary>
        /// Bad CVR parameters sent.
        /// </summary>
        BadCVRParameters = 710,

        SessionRestricted = 711,
        SessionOverloaded = 712,
        UserTooActive = 713,
        NotExpected = 715,
        BadFriendFile = 717,
        UserRestricted = 718,
        SessionFederated = 719,
        UserFederated = 726,
        NotExpectedCVR = 731,
        RoamingLogoff = 733,
        TooManyEndpoints = 734,

        /// <summary>
        /// Changing too rapdly.
        /// </summary>
        RateLimitExceeded = 800,
        /// <summary>
        /// Server too busy.
        /// </summary>
        ServerTooBusy = 910,
        /// <summary>
        /// Authentication failed.
        /// </summary>
        AuthenticationFailed = 911,
        /// <summary>
        /// Action is not allowed when user is offline.
        /// </summary>
        NotAllowedWhenOffline = 913,
        /// <summary>
        /// New users are not accepted.
        /// </summary>
        NotAcceptingNewUsers = 920,
        /// <summary>
        /// Timed out.
        /// </summary>
        TimedOut = 921,
        /// <summary>
        /// Kids without parental consent.
        /// </summary>
        KidsWithoutParentalConsent = 923,
        /// <summary>
        /// Passport not yet verified.
        /// </summary>
        PassportNotYetVerified = 924,

        ManagedUserLimitedAccessWrongClient = 926,
        managedUserAccessDenied = 927,
        AuthError = 928,

        /// <summary>
        /// Account not on this server
        /// </summary>
        DomainReserved = 931,
        /// <summary>
        /// The ADL command indicates some invalid contact to server.
        /// </summary>
        InvalidContactList = 933,
        /// <summary>
        /// Invalid signature
        /// </summary>
        InvalidSignature = 935
    }

    /// <summary>
    /// Custom emoticon type.
    /// </summary>
    public enum EmoticonType
    {
        /// <summary>
        /// Emoticon that is a static picture
        /// </summary>
        StaticEmoticon,
        /// <summary>
        /// Emoticon that will display as a animation.
        /// </summary>
        AnimEmoticon
    }

    /// <summary>
    /// CacheKey for webservices
    /// </summary>
    [Serializable()]
    public enum CacheKeyType
    {
        /// <summary>
        /// CacheKey for contact service, which url is ***.omega.contacts.msn.com
        /// </summary>
        OmegaContactServiceCacheKey,

        /// <summary>
        /// CacheKey for profile storage service, which url is ***.storage.msn.com
        /// </summary>
        StorageServiceCacheKey
    }

    /// <summary>
    /// The current p2p version used in p2p bridge.
    /// </summary>
    public enum P2PVersion
    {
        None = 0,
        P2PV1 = 1,
        P2PV2 = 2
    }

    /// <summary>
    /// Mime header key constants.
    /// </summary>
    public static class MIMEHeaderStrings
    {
        public const string From = "From";
        public const string To = "To";
        public const string Via = "Via";
        public const string Routing = "Routing";
        public const string Reliability = "Reliability";
        public const string Stream = "Stream";
        public const string Segment = "Segment";

        public const string Messaging = "Messaging";
        public const string Publication = "Publication";
        public const string Notification = "Notification";

        /// <summary>
        /// The value is: Content-Length
        /// </summary>
        public const string Content_Length = "Content-Length";
        /// <summary>
        /// The value is: Content-Type
        /// </summary>
        public const string Content_Type = "Content-Type";
        /// <summary>
        /// The value is: Content-Transfer-Encoding
        /// </summary>
        public const string Content_Transfer_Encoding = "Content-Transfer-Encoding";
        /// <summary>
        /// The value is: Message-Type
        /// </summary>
        public const string Message_Type = "Message-Type";
        /// <summary>
        /// The value is: Message-Subtype
        /// </summary>
        public const string Message_Subtype = "Message-Subtype";
        /// <summary>
        /// The value is: MIME-Version
        /// </summary>
        public const string MIME_Version = "MIME-Version";
        public const string TypingUser = "TypingUser";
        /// <summary>
        /// The value is: X-MMS-IM-Format
        /// </summary>
        public const string X_MMS_IM_Format = "X-MMS-IM-Format";
        public const string NotifType = "NotifType";
        /// <summary>
        /// The value is: P4-Context
        /// </summary>
        public const string P4_Context = "P4-Context";
        /// <summary>
        /// The value is: Max-Forwards
        /// </summary>
        public const string Max_Forwards = "Max-Forwards";
        public const string Uri = "Uri";
        /// <summary>
        /// The key string for charset header 
        /// </summary>
        public const string CharSet = " charset"; // Don't delete space
        public const string EPID = "epid";
        public const string Path = "path";
        public const string ServiceChannel = "Service-Channel";

        public const string Options = "Options";
        public const string Flags = "Flags";
        public const string Pipe = "Pipe";
        public const string BridgingOffsets = "Bridging-Offsets";
        

        internal const string KeyParam = ";";

        /// <summary>
        /// The separator of key-value pair in MIME header 
        /// </summary>
        public const string KeyValueSeparator = ": ";

    }

    internal enum ReturnState : uint
    {
        None = 0,
        ProcessNextContact = 1,
        RequestCircleAddressBook = 2,

        /// <summary>
        /// Tell the caller initialize the circle first, then recall the UpdateContact with Recall scenario.
        /// </summary>
        LoadAddressBookFromFile = 4,

        UpdateError = 8
    }

    internal enum Scenario : uint
    {
        None = 0,

        /// <summary>
        /// Restoring contacts from mcl file.
        /// </summary>
        Restore = 1,
        Initial = 2,
        DeltaRequest = 4,

        /// <summary>
        /// Processing the new added circles.
        /// </summary>
        NewCircles = 8,

        /// <summary>
        /// Processing the modified circles.
        /// </summary>
        ModifiedCircles = 16,

        /// <summary>
        /// Send the initial ADL command for contacts.
        /// </summary>
        SendInitialContactsADL = 32,

        /// <summary>
        /// Send the initial ADL command for circles.
        /// </summary>
        SendInitialCirclesADL = 64,

        ContactServeAPI = 128,

        InternalCall = 256,

        SendServiceADL = 512
    }

    internal enum InternalOperationReturnValues
    {
        Succeed,
        NoExpressionProfile,
        ProfileNotExist,
        RequestFailed,
        AddImageFailed,
        AddRelationshipFailed,
        AddImageRelationshipFailed
    }

    /// <summary>
    /// The reason that fires <see cref="Contact.DisplayImageChanged"/> event.
    /// </summary>
    public enum DisplayImageChangedType
    {
        None,

        /// <summary>
        /// The <see cref="DisplayImage"/> is just recreate from file.
        /// </summary>
        Restore,

        /// <summary>
        /// The <see cref="DisplayImage"/> is just transmitted from the remote user. 
        /// </summary>
        TransmissionCompleted,

        /// <summary>
        /// Remote user notified it has its <see cref="DisplayImage"/> changed.
        /// </summary>
        UpdateTransmissionRequired,
    }

    public enum PlaceChangedReason
    {
        None,
        SignedIn,
        SignedOut
    }

    /// <summary>
    /// The special account of remote network's gateways, i.e. FaceBook and LinkedIn 
    /// </summary>
    public static class RemoteNetworkGateways
    {
        /// <summary>
        /// FaceBook Gateway account. 
        /// </summary>
        public const string FaceBookGatewayAccount = "fb";

        public const string WindowsLiveGateway = "wl";
        public const string LinkedInGateway = "li";
    }

    /// <summary>
    /// The keys for MIME Content Headers. 
    /// </summary>
    public static class MIMEContentHeaders
    {
        /// <summary>
        /// The key string for Content-Length header 
        /// </summary>
        public const string ContentLength = MIMEHeaderStrings.Content_Length;

        /// <summary>
        /// The key string for Content-Type header 
        /// </summary>
        public const string ContentType = MIMEHeaderStrings.Content_Type;

        /// <summary>
        /// The key string for charset header 
        /// </summary>
        public const string CharSet = MIMEHeaderStrings.CharSet;

        public const string Publication = MIMEHeaderStrings.Publication;
        public const string Messaging = MIMEHeaderStrings.Messaging;
        public const string Notification = MIMEHeaderStrings.Notification;


        public const string URI = MIMEHeaderStrings.Uri;
        public const string MessageType = MIMEHeaderStrings.Message_Type;

        public const string MSIMFormat = MIMEHeaderStrings.X_MMS_IM_Format;
        public const string ContentTransferEncoding = MIMEHeaderStrings.Content_Transfer_Encoding;

        public const string Pipe = MIMEHeaderStrings.Pipe;
        public const string BridgingOffsets = MIMEHeaderStrings.BridgingOffsets;
    }

    /// <summary>
    /// The keys for MIME Reouting Header of MultiMimeMessage. 
    /// </summary>
    public static class MIMERoutingHeaders
    {
        public const string Routing = MIMEHeaderStrings.Routing;
        public const string From = MIMEHeaderStrings.From;
        public const string To = MIMEHeaderStrings.To;
        public const string EPID = MIMEHeaderStrings.EPID;
        public const string Path = MIMEHeaderStrings.Path;
        public const string Options = MIMEHeaderStrings.Options;
        public const string Via = MIMEHeaderStrings.Via;

        /// <summary>
        /// The service that this message should sent through. 
        /// </summary>
        public const string ServiceChannel = MIMEHeaderStrings.ServiceChannel;

    }

    /// <summary>
    /// Keys for MIME header, reliability parts. 
    /// </summary>
    public static class MIMEReliabilityHeaders
    {
        public const string Reliability = MIMEHeaderStrings.Reliability;
        public const string Stream = MIMEHeaderStrings.Stream;
        public const string Segment = MIMEHeaderStrings.Segment;
        public const string Flags = MIMEHeaderStrings.Flags;
    }

    public static class MIMEContentTransferEncoding
    {
        public const string Binary = "binary";
    }


    public static class MessageTypes
    {
        public const string Text = "Text";
        public const string Nudge = "Nudge";
        public const string Wink = "Wink";
        public const string CustomEmoticon = "CustomEmoticon";
        public const string ControlTyping = "Control/Typing";
        public const string Data = "Data";
        public const string SignalP2P = "Signal/P2P";
        public const string SignalCloseIMWindow = "Signal/CloseIMWindow";

        public const string SignalMarkIMWindowRead = "Signal/MarkIMWindowRead";
        public const string SignalTurn = "Signal/Turn";
        public const string SignalAudioMeta = "Signal/AudioMeta";
        public const string SignalAudioTunnel = "Signal/AudioTunnel";
    }

    #region P2PFlag

    /// <summary>
    /// Defines the type of P2P message.
    /// </summary>
    [Flags]
    public enum P2PFlag : uint
    {
        /// <summary>
        /// Normal (protocol) message.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Negative Ack
        /// </summary>
        NegativeAck = 0x1,
        /// <summary>
        /// Acknowledgement message.
        /// </summary>
        Acknowledgement = 0x2,
        /// <summary>
        /// Required Ack
        /// </summary>
        RequireAck = 0x4,
        /// <summary>
        /// Messages notifies a binary error.
        /// </summary>
        Error = 0x8,
        /// <summary>
        /// File
        /// </summary>
        File = 0x10,
        /// <summary>
        /// Messages defines a msn object.
        /// </summary>
        Data = 0x20,
        /// <summary>
        /// Close session
        /// </summary>
        CloseSession = 0x40,
        /// <summary>
        /// Tlp error
        /// </summary>
        TlpError = 0x80,
        /// <summary>
        /// Direct handshake
        /// </summary>
        DirectHandshake = 0x100,
        /// <summary>
        /// Messages for info data, such as INVITE, 200 OK, 500 INTERNAL ERROR
        /// </summary>
        MSNSLPInfo = 0x01000000,
        /// <summary>
        /// Messages defines data for a filetransfer.
        /// </summary>
        FileData = MSNSLPInfo | P2PFlag.Data | P2PFlag.File,
        /// <summary>
        /// Messages defines data for a MSNObject transfer.
        /// </summary>
        MSNObjectData = MSNSLPInfo | P2PFlag.Data
    }

    #endregion

    #region P2PConst

    internal static class P2PConst
    {
        /// <summary>
        /// The guid used in invitations for a filetransfer.
        /// </summary>
        public const string FileTransferGuid = "{5D3E02AB-6190-11D3-BBBB-00C04F795683}";

        /// <summary>
        /// The guid used in invitations for a user display transfer.
        /// </summary>
        public const string UserDisplayGuid = "{A4268EEC-FEC5-49E5-95C3-F126696BDBF6}";

        /// <summary>
        /// The guid used in invitations for a share photo.
        /// </summary>
        public const string SharePhotoGuid = "{41D3E74E-04A2-4B37-96F8-08ACDB610874}";

        /// <summary>
        /// The guid used in invitations for an activity.
        /// </summary>
        public const string ActivityGuid = "{6A13AF9C-5308-4F35-923A-67E8DDA40C2F}";

        /// <summary>
        /// Footer for a msn DisplayImage p2pMessage.
        /// </summary>
        public const uint DisplayImageFooter12 = 12;

        /// <summary>
        /// Footer for a filetransfer p2pMessage.
        /// </summary>
        public const uint FileTransFooter2 = 2;

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        public const uint CustomEmoticonFooter11 = 11;

        /// <summary>
        /// Footer for a msn object p2pMessage.
        /// </summary>
        public const uint DisplayImageFooter1 = 1;

        /// <summary>
        /// Footer for a msn CustomEmoticon p2pMessage.
        /// </summary>
        public const uint CustomEmoticonFooter1 = 1;

        /// <summary>
        /// The value of protocol version field of Peer info TLV.
        /// </summary>
        public const ushort ProtocolVersion = 512;

        /// <summary>
        /// The value of implementation ID field of Peer info TLV.
        /// </summary>
        public const ushort ImplementationID = 0;

        /// <summary>
        /// The value of version field of Peer info TLV.
        /// </summary>
        public const ushort PeerInfoVersion = 3584;

        /// <summary>
        /// The unknown field of Peer info TLV.
        /// </summary>
        public const ushort PeerInfoReservedField = 0;

        /// <summary>
        /// The value of capabilities field of Peer info TLV.
        /// </summary>
        public const uint Capabilities = 271;
    }

    #endregion

    #region OperationCode

    public enum OperationCode : byte
    {
        /// <summary>
        /// Nothing required
        /// </summary>
        None = 0x0,

        /// <summary>
        /// This is a SYN message.
        /// </summary>
        SYN = 0x1,

        /// <summary>
        /// Required ACK.
        /// </summary>
        RAK = 0x2
    }

    internal static class MSNSLPRequestMethod
    {
        public const string INVITE = "INVITE";
        public const string BYE = "BYE";
        public const string ACK = "ACK";
    }

    #endregion
};
