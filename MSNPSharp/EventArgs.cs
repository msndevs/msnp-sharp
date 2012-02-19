#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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
    using MSNPSharp.Apps;
    using MSNPSharp.Core;
    using MSNPSharp.P2P;
    using MSNPSharp.LiveConnectAPI.Atom;
    using MSNPSharp.MSNWS.MSNABSharingService;

    /// <summary>
    /// Used for the relationship with your friend on your social network.
    /// </summary>
    [Serializable]
    public class FriendshipStatusChangedEventArgs : EventArgs
    {
        private readonly RoleId oldStatus;
        private readonly RoleId newStatus;
        private readonly Contact contact;

        public RoleId OldStatus
        {
            get
            {
                return oldStatus;
            }
        }

        public RoleId NewStatus
        {
            get
            {
                return newStatus;
            }
        }

        public Contact Contact
        {
            get
            {
                return contact;
            }
        }

        public FriendshipStatusChangedEventArgs(Contact contact, RoleId oldStatus, RoleId newStatus)
        {
            this.contact = contact;
            this.oldStatus = oldStatus;
            this.newStatus = newStatus;
        }
    }


    /// <summary>
    /// Used when a contact changed its status.
    /// </summary>
    [Serializable]
    public class StatusChangedEventArgs : EventArgs
    {
        private PresenceStatus oldStatus;
        private PresenceStatus newStatus;

        public PresenceStatus OldStatus
        {
            get
            {
                return oldStatus;
            }
        }

        public PresenceStatus NewStatus
        {
            get
            {
                return newStatus;
            }
        }

        public StatusChangedEventArgs(PresenceStatus oldStatus, PresenceStatus newStatus)
        {
            this.oldStatus = oldStatus;
            this.newStatus = newStatus;
        }
    }

    /// <summary>
    /// Used when contact changed its status.
    /// </summary>
    [Serializable]
    public class ContactStatusChangedEventArgs : StatusChangedEventArgs
    {
        private Contact contact;
        private Contact via;

        /// <summary>
        /// The contact who changed its status.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }
        }

        /// <summary>
        /// Circle, temporary group or external network if it isn't null.
        /// </summary>
        public Contact Via
        {
            get
            {
                return via;
            }
        }

        public ContactStatusChangedEventArgs(Contact contact, Contact via,
                                            PresenceStatus oldStatus, PresenceStatus newStatus)
            : base(oldStatus, newStatus)
        {
            this.contact = contact;
            this.via = via;

            if (via == null && contact != null)
                via = contact.Via;
        }

        public ContactStatusChangedEventArgs(Contact contact, PresenceStatus oldStatus, PresenceStatus newStatus)
            : base(oldStatus, newStatus)
        {
            this.contact = contact;
            this.via = contact.Via;
        }
    }

    /// <summary>
    /// Used when any contect event occured.
    /// </summary>
    [Serializable]
    public class BaseContactEventArgs : EventArgs
    {
        protected Contact contact = null;

        public BaseContactEventArgs(Contact contact)
        {
            this.contact = contact;
        }
    }

    [Serializable()]
    public class ContactEventArgs : BaseContactEventArgs
    {
        /// <summary>
        /// The contact raise the event.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }
            set
            {
                contact = value;
            }
        }

        public ContactEventArgs(Contact contact)
            : base(contact)
        {
        }
    }

    [Serializable]
    public class GroupChatParticipationEventArgs : EventArgs
    {
        private Contact contact;
        private Contact via;

        /// <summary>
        /// The contact joined/left. Use this contact to chat 1 on 1.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }
        }

        /// <summary>
        /// Circle or temporary group. Use this contact for multiparty chat.
        /// </summary>
        public Contact Via
        {
            get
            {
                return via;
            }
        }

        public GroupChatParticipationEventArgs(Contact contact, Contact via)
        {
            this.contact = contact;
            this.via = via;
        }
    }

    /// <summary>
    /// Use when user's sign in places changed.
    /// </summary>
    public class PlaceChangedEventArgs : EventArgs
    {
        private PlaceChangedReason reason = PlaceChangedReason.None;
        private EndPointData epData;

        public PlaceChangedReason Reason
        {
            get
            {
                return reason;
            }
        }

        public EndPointData EndPointData
        {
            get
            {
                return epData;
            }
        }

        public string PlaceName
        {
            get
            {
                PrivateEndPointData pep = epData as PrivateEndPointData;

                if (pep != null)
                {
                    return pep.Name;
                }
                return String.Empty;
            }
        }

        private PlaceChangedEventArgs()
            : base()
        {
        }

        public PlaceChangedEventArgs(EndPointData ep, PlaceChangedReason action)
            : base()
        {
            this.epData = ep;
            this.reason = action;
        }
    }



    /// <summary>
    /// Used in events where a exception is raised. Via these events the client programmer
    /// can react on these exceptions.
    /// </summary>
    [Serializable()]
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private Exception _exception;

        /// <summary>
        /// The exception that was raised
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e"></param>
        public ExceptionEventArgs(Exception e)
        {
            _exception = e;
        }
    }

    /// <summary>
    /// Used as event argument when a emoticon definition is send.
    /// </summary>
    [Serializable()]
    public class EmoticonDefinitionEventArgs : MessageArrivedEventArgs
    {
        /// <summary>
        /// </summary>
        private Emoticon emoticon;

        /// <summary>
        /// The emoticon which is defined
        /// </summary>
        public Emoticon Emoticon
        {
            get
            {
                return emoticon;
            }
            set
            {
                emoticon = value;
            }
        }

        public EmoticonDefinitionEventArgs(Contact contact, Contact originalSender, RoutingInfo routingInfo, Emoticon emoticon)
            : base(contact, originalSender, routingInfo)
        {
            this.emoticon = emoticon;
        }
    }

    /// <summary>
    /// Used when a list (FL, Al, BL, RE) is received via synchronize or on request.
    /// </summary>
    [Serializable()]
    public class ListReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private RoleLists affectedList = RoleLists.None;

        /// <summary>
        /// The list which was send by the server
        /// </summary>
        public RoleLists AffectedList
        {
            get
            {
                return affectedList;
            }
            set
            {
                affectedList = value;
            }
        }

        /// <summary>
        /// Constructory.
        /// </summary>
        /// <param name="affectedList"></param>
        public ListReceivedEventArgs(RoleLists affectedList)
        {
            AffectedList = affectedList;
        }
    }

    /// <summary>
    /// Used when the local user is signed off.
    /// </summary>
    [Serializable()]
    public class SignedOffEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private SignedOffReason signedOffReason;

        /// <summary>
        /// The list which was send by the server
        /// </summary>
        public SignedOffReason SignedOffReason
        {
            get
            {
                return signedOffReason;
            }
            set
            {
                signedOffReason = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="signedOffReason"></param>
        public SignedOffEventArgs(SignedOffReason signedOffReason)
        {
            this.signedOffReason = signedOffReason;
        }
    }

    /// <summary>
    /// Used as event argument when any contact list mutates.
    /// </summary>
    [Serializable()]
    public class ListMutateEventArgs : ContactEventArgs
    {
        /// <summary>
        /// </summary>
        private RoleLists affectedList = RoleLists.None;

        /// <summary>
        /// The list which mutated.
        /// </summary>
        public RoleLists AffectedList
        {
            get
            {
                return affectedList;
            }
            set
            {
                affectedList = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="affectedList"></param>
        public ListMutateEventArgs(Contact contact, RoleLists affectedList)
            : base(contact)
        {
            AffectedList = affectedList;
        }
    }

    /// <summary>
    /// Used as event argument when msn sends us an error.
    /// </summary>
    [Serializable()]
    public class MSNErrorEventArgs : EventArgs
    {
        private MSNError msnError;
        private string description = string.Empty;

        /// <summary>
        /// The error description
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }

        /// <summary>
        /// The error that occurred
        /// </summary>
        public MSNError MSNError
        {
            get
            {
                return msnError;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MSNErrorEventArgs(MSNError msnError, string description)
        {
            this.msnError = msnError;
            this.description = description;
        }
    }

    [Serializable]
    public class MultipartyCreatedEventArgs : EventArgs
    {
        private Contact group;

        public Contact Group
        {
            get
            {
                return group;
            }
        }

        public MultipartyCreatedEventArgs(Contact group)
        {
            this.group = group;
        }
    }

    [Serializable]
    public abstract class MessageArrivedEventArgs : EventArgs
    {
        private Contact sender;
        private Contact originalSender;
        private RoutingInfo routingInfo;

        /// <summary>
        /// The sender of message (type can be contact, circle or temporary group)
        /// </summary>
        public Contact Sender
        {
            get
            {
                return sender;
            }
        }

        /// <summary>
        /// The original sender of message (client type is contact and can chat invidually)
        /// </summary>
        public Contact OriginalSender
        {
            get
            {
                return originalSender;
            }
        }

        public RoutingInfo RoutingInfo
        {
            get
            {
                return routingInfo;
            }
        }

        protected MessageArrivedEventArgs(Contact contact, Contact originalSender, RoutingInfo routingInfo)
        {
            this.sender = contact;
            this.originalSender = originalSender;
            this.routingInfo = routingInfo;
        }
    }

    [Serializable]
    public class NudgeArrivedEventArgs : MessageArrivedEventArgs
    {
        public NudgeArrivedEventArgs(Contact contact, Contact originalSender, RoutingInfo routingInfo)
            : base(contact, originalSender, routingInfo)
        {
        }
    }

    [Serializable]
    public class TypingArrivedEventArgs : MessageArrivedEventArgs
    {
        public TypingArrivedEventArgs(Contact contact, Contact originalSender, RoutingInfo routingInfo)
            : base(contact, originalSender, routingInfo)
        {
        }
    }

    [Serializable]
    public class TextMessageArrivedEventArgs : MessageArrivedEventArgs
    {
        private TextMessage textMessage = null;

        /// <summary>
        /// The text message received.
        /// </summary>
        public TextMessage TextMessage
        {
            get
            {
                return textMessage;
            }
        }

        public TextMessageArrivedEventArgs(Contact sender, TextMessage textMessage, Contact originalSender, RoutingInfo routingInfo)
            : base(sender, originalSender, routingInfo)
        {
            this.textMessage = textMessage;
        }
    }

    [Serializable]
    public class EmoticonArrivedEventArgs : MessageArrivedEventArgs
    {
        private Emoticon emoticon;

        /// <summary>
        /// The emoticon data received.
        /// </summary>
        public Emoticon Emoticon
        {
            get
            {
                return emoticon;
            }
        }

        public EmoticonArrivedEventArgs(Contact sender, Emoticon emoticon, Contact circle, RoutingInfo routingInfo)
            : base(sender, circle, routingInfo)
        {
            this.emoticon = emoticon;
        }
    }

    public class WinkEventArgs : MessageArrivedEventArgs
    {
        private Wink wink;

        public Wink Wink
        {
            get
            {
                return wink;
            }
        }

        public WinkEventArgs(Contact contact, Wink wink, RoutingInfo routingInfo)
            : base(contact, null, routingInfo)
        {
            this.wink = wink;
        }
    }

    /// <summary>
    /// Base class for circle event arg.
    /// </summary>
    public class BaseCircleEventArgs : EventArgs
    {
        protected Contact circle = null;
        internal BaseCircleEventArgs(Contact circle)
        {
            this.circle = circle;
        }
    }

    /// <summary>
    /// Used as event argument when a <see cref="Circle"/> is affected.
    /// </summary>
    [Serializable()]
    public class CircleEventArgs : BaseCircleEventArgs
    {
        protected Contact remoteMember = null;

        /// <summary>
        /// The affected contact group
        /// </summary>
        public Contact Circle
        {
            get
            {
                return circle;
            }
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        internal CircleEventArgs(Contact circle)
            : base(circle)
        {
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="remote">The affected Contact.</param>
        internal CircleEventArgs(Contact circle, Contact remote)
            : base(circle)
        {
            remoteMember = remote;
        }
    }

    /// <summary>
    /// Used when a event related to circle member operaion fired.
    /// </summary>
    [Serializable()]
    public class CircleMemberEventArgs : CircleEventArgs
    {
        /// <summary>
        /// The contact member raise the event.
        /// </summary>
        public Contact Member
        {
            get
            {
                return remoteMember;
            }
        }

        internal CircleMemberEventArgs(Contact circle, Contact member)
            : base(circle, member)
        {
        }
    }

    /// <summary>
    /// Event argument used when a user's <see cref="DisplayImage"/> property has been changed.
    /// </summary>
    public class DisplayImageChangedEventArgs : EventArgs
    {
        private bool callFromContactManager = false;
        private DisplayImageChangedType status = DisplayImageChangedType.None;
        private DisplayImage newDisplayImage = null;

        public DisplayImage NewDisplayImage
        {
            get
            {
                return newDisplayImage;
            }
        }

        /// <summary>
        ///  The reason that fires <see cref="Contact.DisplayImageChanged"/> event.
        /// </summary>
        public DisplayImageChangedType Status
        {
            get
            {
                return status;
            }
        }

        /// <summary>
        /// Whether we need to do display image synchronize.
        /// </summary>
        internal bool CallFromContactManager
        {
            get
            {
                return callFromContactManager;
            }
        }

        private DisplayImageChangedEventArgs()
        {
        }

        internal DisplayImageChangedEventArgs(DisplayImage dispImage, DisplayImageChangedType type, bool needSync)
        {
            status = type;
            callFromContactManager = needSync;
            newDisplayImage = dispImage;
        }

        internal DisplayImageChangedEventArgs(DisplayImageChangedEventArgs arg, bool needSync)
        {
            status = arg.Status;
            callFromContactManager = needSync;
            newDisplayImage = arg.NewDisplayImage;
        }

        public DisplayImageChangedEventArgs(DisplayImage dispImage, DisplayImageChangedType type)
        {
            status = type;
            callFromContactManager = false;
            newDisplayImage = dispImage;
        }
    }

    /// <summary>
    /// Event argument used when a user's <see cref="SceneImage"/> property has been changed.
    /// </summary>
    public class SceneImageChangedEventArgs : EventArgs
    {
        private bool callFromContactManager = false;
        private bool isDefault = false;
        private DisplayImageChangedType status = DisplayImageChangedType.None;
        private SceneImage newSceneImage = null;

        public SceneImage NewSceneImage
        {
            get
            {
                return newSceneImage;
            }
        }

        /// <summary>
        ///  The reason that fires <see cref="Contact.SceneImageChanged"/> event.
        /// </summary>
        public DisplayImageChangedType Status
        {
            get
            {
                return status;
            }
        }

        /// <summary>
        ///  If the user has chosen to set his scene image to default.
        /// </summary>
        public bool IsDefault
        {
            get
            {
                return isDefault;
            }
        }

        /// <summary>
        /// Whether we need to synchronize the scene image.
        /// </summary>
        internal bool CallFromContactManager
        {
            get
            {
                return callFromContactManager;
            }
        }

        private SceneImageChangedEventArgs()
        {
        }

        internal SceneImageChangedEventArgs(SceneImage sceneImage, DisplayImageChangedType type, bool needSync)
        {
            status = type;
            callFromContactManager = needSync;
            newSceneImage = sceneImage;
        }

        public SceneImageChangedEventArgs(DisplayImageChangedType type, bool isDefault)
        {
            status = type;
            callFromContactManager = false;
            newSceneImage = null;

            this.isDefault = isDefault;
        }
    }
    
    public class CloseIMWindowEventArgs : EventArgs
    {
        private Contact sender = null;
        private EndPointData senderEndPoint = null;
        
        private Contact receiver = null;
        private EndPointData receiverEndPoint = null;
        
        private Contact[] parties = null;

        public Contact[] Parties {
            get {
                return this.parties;
            }
        
            private set {
                parties = value;
            }
        }
        
        public Contact Receiver {
            get {
                return this.receiver;
            }
            private set {
                receiver = value;
            }
        }

        public EndPointData ReceiverEndPoint {
            get {
                return this.receiverEndPoint;
            }
            private set {
                receiverEndPoint = value;
            }
        }

        public Contact Sender {
            get {
                return this.sender;
            }
            private set {
                sender = value;
            }
        }

        public EndPointData SenderEndPoint {
            get {
                return this.senderEndPoint;
            }
            private set {
                senderEndPoint = value;
            }
        }
        
        public CloseIMWindowEventArgs(Contact sender, EndPointData senderEndPoint,
                                      Contact receiver, EndPointData receiverEndPoint,
                                      Contact[] parties)
            : base()
        {
            Sender = sender;
            SenderEndPoint = senderEndPoint;
            
            Receiver = receiver;
            ReceiverEndPoint = receiverEndPoint;
            
            Parties = parties;
        }
        
    }

    internal class AtomRequestSucceedEventArgs : EventArgs
    {
        private entryType entry = null;

        /// <summary>
        /// The return entry of atom request.
        /// </summary>
        public entryType Entry
        {
            get { return entry; }
            private set { entry = value; }
        }

        public AtomRequestSucceedEventArgs(entryType entry)
        {
            Entry = entry;
        }
    }

    public class PersonalStatusChangedEventArgs : EventArgs
    {
        private string oldStatusText = string.Empty;

        /// <summary>
        /// The previous personal status.
        /// </summary>
        public string OldStatusText
        {
            get { return oldStatusText; }
            private set { oldStatusText = value; }
        }

        private string newStatusText = string.Empty;

        /// <summary>
        /// Current sttaus text after changed.
        /// </summary>
        public string NewStatusText
        {
            get { return newStatusText; }
            private set { newStatusText = value; }
        }

        public PersonalStatusChangedEventArgs(string oldText, string newText)
        {
            OldStatusText = oldText;
            NewStatusText = newText;
        }
    }
};
