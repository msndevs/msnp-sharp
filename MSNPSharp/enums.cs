#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
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

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// Msn protocol speaking
    /// </summary>
    public enum MsnProtocol
    {
        MSNP18 = 18
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
        Block = 4,
        /// <summary>
        /// All contacts who have you on their contactlist.
        /// </summary>
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
    /// Defines the privacy mode of the owner of the contactlist
    /// <list type="bullet">
    /// <item>AllExceptBlocked - Allow all contacts to send you messages except those on your blocked list</item>
    /// <item>NoneButAllowed - Reject all messages except those from people on your allow list</item></list>
    /// </summary>
    public enum PrivacyMode
    {
        /// <summary>
        /// Unknown privacy mode.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Allow all contacts to send you messages except those on your blocked list.
        /// </summary>
        AllExceptBlocked = 1,
        /// <summary>
        /// Reject all messages except those from people on your allow list.
        /// </summary>
        NoneButAllowed = 2
    }

    /// <summary>
    /// Defines the way MSN handles with new contacts
    /// <list type="bullet">
    /// <item>PromptOnAdd - Notify the clientprogram when a contact adds you and let the program handle the response itself</item>
    /// <item>AutomaticAdd - When someone adds you MSN will automatically add them on your list</item>
    /// </list>
    /// </summary>
    public enum NotifyPrivacy
    {
        /// <summary>
        /// Unknown notify privacy.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Notify the clientprogram when a contact adds you and let the program handle the response itself.
        /// </summary>
        PromptOnAdd = 1,
        /// <summary>
        /// When someone adds you MSN will automatically add them on your list.
        /// </summary>
        AutomaticAdd = 2
    }

    /// <summary>
    /// Use the same display picture and personal message wherever I sign in.
    /// </summary>
    public enum RoamLiveProperty
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Enabled
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Disabled
        /// </summary>
        Disabled = 2
    }

    /// <summary>
    /// Whether the contact list owner has Multiple Points of Presence Support (MPOP) that is owner connect from multiple places.
    /// </summary>
    public enum MPOP
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified,

        /// <summary>
        /// When the same user sign in at another place, sign the owner out.
        /// </summary>
        AutoLogoff,

        /// <summary>
        /// When the same user sign in at another place, keep the owner sign in.
        /// </summary>
        KeepOnline
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

        Default = SupportsChunking | SupportsWinks | SupportsSipInvite | SupportsDirectBootstrapping | AppVersion2009
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
        _0x8000000 = 0x8000000,
        _0x10000000 = 0x10000000,
        FTURNCapable = 0x20000000,
        _0x40000000 = 0x40000000,
        SupportsMultipartyConversations = 0x80000000,

        Default = SupportsPeerToPeerV2 | RTCVideoEnabled | SupportsOfflineIM | SupportsNudges
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
    /// The state of contact in a conversation.
    /// </summary>
    public enum ContactConversationState
    {
        None,
        /// <summary>
        /// The contact is invited, but not join in yet.
        /// </summary>
        Invited,
        /// <summary>
        /// The contact is in the conversation.
        /// </summary>
        Joined,
        /// <summary>
        /// The contact has left the conversation.
        /// </summary>
        Left
    }

    /// <summary>
    /// Types of different conversations.
    /// </summary>
    [Flags]
    public enum ConversationType : long
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,
        /// <summary>
        /// MSN user conversation.
        /// </summary>
        SwitchBoard = 1,
        /// <summary>
        /// Yahoo Messenger conversation
        /// </summary>
        YIM = 2,
        /// <summary>
        /// A conversation that contains more than 2 users.
        /// </summary>
        MutipleUsers = 4,
        /// <summary>
        /// A conversation use for chatting.
        /// </summary>
        Chat = 8
    }

    /// <summary>
    /// Types of text messages send through switchboard.
    /// </summary>
    public enum NetworkMessageType : int
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None = 0,

        /// <summary>
        /// Plain text message
        /// </summary>
        Text = 1,

        /// <summary>
        /// User typing message
        /// </summary>
        Typing = 2,

        /// <summary>
        /// A nudge message
        /// </summary>
        Nudge = 3,

        /// <summary>
        /// The emoticon data.
        /// </summary>
        Emoticon,
        /// <summary>
        /// The object data.
        /// </summary>
        MSNObject
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


    public static class MemberRole
    {
        public const string Allow = "Allow";
        public const string Block = "Block";
        public const string Reverse = "Reverse";
        public const string Pending = "Pending";
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
        public const string Invitation = "Invitation";
        public const string SocialNetwork = "SocialNetwork";
        public const string Profile = "Profile";
        public const string Folder = "Folder";
        public const string Event = "Event";
        public const string OfficeLiveWebNotification = "OfficeLiveWebNotification";
        public const string CommunityQuestionAnswer = "CommunityQuestionAnswer";
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

    /// <summary>
    /// Scheme string for PUT command.
    /// </summary>
    public static class CircleString
    {
        /// <summary>
        /// The default windows live circle host domain: live.com.
        /// </summary>
        public const string DefaultHostDomain = "live.com";

        /// <summary>
        /// The default sender of join circle invitation email: Windows Live.
        /// </summary>
        public const string CircleInvitationEmailSender = "Windows Live";

        /// <summary>
        /// The extended-flags property of join circle invation email notification message.
        /// </summary>
        public const string InvitationEmailExtendedFlags = "ab=0|i=0|e=0";

        public const string InvitationEmailExtendedFlagsByWeb = "ab=0|i=51|e=0";

        /// <summary>
        /// The "via" property string of a circle group member. The value of this constant is ";via=9:".
        /// </summary>
        public const string ViaCircleGroupSplitter = @";via=9:";
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
        public const string MSN_IM_MPOP = "MSN.IM.MPOP";

        /// <summary>
        /// The value is: MSN.IM.BLP
        /// </summary>
        public const string MSN_IM_BLP = "MSN.IM.BLP";

        /// <summary>
        /// The value is: MSN.IM.GTC
        /// </summary>
        public const string MSN_IM_GTC = "MSN.IM.GTC";

        /// <summary>
        /// The value is: MSN.IM.RoamLiveProperties
        /// </summary>
        public const string MSN_IM_RoamLiveProperties = "MSN.IM.RoamLiveProperties";

        /// <summary>
        /// The value is: MSN.IM.MBEA
        /// </summary>
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
    /// Mime header key constants.
    /// </summary>
    public static class MimeHeaderStrings
    {
        public const string From = "From";
        public const string To = "To";
        public const string Routing = "Routing";
        public const string Reliability = "Reliability";
        public const string Stream = "Stream";
        public const string Segment = "Segment";
        public const string Messaging = "Messaging";
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

        internal const string KeyParam = ";";

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
    /// The parse option for full account identifier.
    /// </summary>
    internal enum AccountParseOption
    {
        None = 0,

        /// <summary>
        /// Tell the parser this is a full circle account. For example: 1:user@hotmail.com;via=9:guid@live.com
        /// </summary>
        ParseAsFullCircleAccount = 1,

        /// <summary>
        /// Tell the parser this is a client type and account combination. For example: 1:user@hotmail.com
        /// </summary>
        ParseAsClientTypeAndAccount = 2
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

        InternalCall = 256
    }

    /// <summary>
    /// This is the value of different domain type of Network info list.
    /// </summary>
    internal static class DomainIds
    {
        /// <summary>
        /// Domain id for Windows Live addressbook in NetworkInfo.
        /// </summary>
        public const int WLDomain = 1;

        /// <summary>
        /// Domain ID for facebook in NetworkInfo.
        /// </summary>
        public const int FBDomain = 7;
        public const int ZUNEDomain = 3;
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

    public enum RosterProperties
    {
        None,
        Name,
        ClientCapacityString,
        ClientCapabilities,
        ClientCapabilitiesEx,
        Status
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

    #region Enums: MsnServiceType and PartnerScenario

    public enum MsnServiceType
    {
        AB,
        Sharing,
        Storage,
        RSI,
        OIMStore,
        WhatsUp
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

    #endregion

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

    internal enum SessionCloseState : int
    {
        None = 2,
        TimeWait = 1,
        Close = 0
    }

    internal static class MSNSLPRequestMethod
    {
        public const string INVITE = "INVITE";
        public const string BYE = "BYE";
        public const string ACK = "ACK";
    }

    #endregion

    internal enum ConversationState : int
    {
        /// <summary>
        /// Default state
        /// </summary>
        None = 0,

        /// <summary>
        /// The conversation object created. User not yet been invited.
        /// </summary>
        ConversationCreated = 1,

        /// <summary>
        /// Conversation has sent a request to create a switchboard for it.
        /// </summary>
        SwitchboardRequestSent = 2,

        /// <summary>
        /// One remote user has joined into the conversation. There're two users in the conversation now (including Owner).
        /// </summary>
        OneRemoteUserJoined = 3,

        /// <summary>
        /// The switchboard has ended.
        /// </summary>
        SwitchboardEnded = 4,

        /// <summary>
        /// The conversation already ended.
        /// </summary>
        ConversationEnded = 5
    }
};
