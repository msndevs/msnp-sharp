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
    using MSNPSharp.Apps;
    using MSNPSharp.Core;
    using MSNPSharp.P2P;

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
        }

        public ContactStatusChangedEventArgs(Contact contact, PresenceStatus oldStatus, PresenceStatus newStatus)
            : base(oldStatus, newStatus)
        {
            this.contact = contact;
            this.via = null;
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
    /// Base class for all message received event args.
    /// </summary>
    [Serializable()]
    public class BaseMessageReceivedEventArgs : BaseContactEventArgs
    {
        /// <summary>
        /// The sender.
        /// </summary>
        public Contact Sender
        {
            get
            {
                return contact;
            }
        }

        internal BaseMessageReceivedEventArgs(Contact sender)
            : base(sender)
        {
        }
    }


    /// <summary>
    /// Used as event argument when a textual message is send.
    /// </summary>
    [Serializable()]
    public class TextMessageEventArgs : BaseMessageReceivedEventArgs
    {
        /// <summary>
        /// </summary>
        private TextMessage message;

        /// <summary>
        /// The message send.
        /// </summary>
        public TextMessage Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        public TextMessageEventArgs(TextMessage message, Contact sender)
            : base(sender)
        {
            Message = message;
        }
    }

    [Serializable()]
    public class WinkEventArgs : BaseMessageReceivedEventArgs
    {
        private Wink wink;

        public Wink Wink
        {
            get
            {
                return wink;
            }
            set
            {
                wink = value;
            }
        }

        public WinkEventArgs(Contact contact, Wink wink)
            : base(contact)
        {
            this.wink = wink;
        }
    }

    /// <summary>
    /// Used as event argument when a emoticon definition is send.
    /// </summary>
    [Serializable()]
    public class EmoticonDefinitionEventArgs : BaseMessageReceivedEventArgs
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="emoticon"></param>
        public EmoticonDefinitionEventArgs(Contact sender, Emoticon emoticon)
            : base(sender)
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
    /// Used as event argument when an answer to a ping is received.
    /// </summary>
    [Serializable()]
    public class PingAnswerEventArgs : EventArgs
    {
        /// <summary>
        /// The number of seconds to wait before sending another PNG, 
        /// and is reset to 50 every time a command is sent to the server. 
        /// In environments where idle connections are closed after a short time, 
        /// you should send a command to the server (even if it's just a PNG) at least this often.
        /// Note: MSNPSharp does not handle this! E.g. if you experience unexpected connection dropping call the Ping() method.
        /// </summary>
        public int SecondsToWait
        {
            get
            {
                return secondsToWait;
            }
            set
            {
                secondsToWait = value;
            }
        }

        /// <summary>
        /// </summary>
        private int secondsToWait;


        /// <summary>
        /// </summary>
        /// <param name="seconds"></param>
        public PingAnswerEventArgs(int seconds)
        {
            SecondsToWait = seconds;
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
        /// <summary>
        /// </summary>
        private MSNError msnError;

        /// <summary>
        /// The error that occurred
        /// </summary>
        public MSNError MSNError
        {
            get
            {
                return msnError;
            }
            set
            {
                msnError = value;
            }
        }

        /// <summary>
        /// Constructory.
        /// </summary>
        /// <param name="msnError"></param>
        public MSNErrorEventArgs(MSNError msnError)
        {
            this.msnError = msnError;
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

        protected MessageArrivedEventArgs(Contact contact, Contact originalSender)
        {
            this.sender = contact;
            this.originalSender = originalSender;
        }
    }

    [Serializable]
    public class NudgeArrivedEventArgs : MessageArrivedEventArgs
    {
        public NudgeArrivedEventArgs(Contact contact, Contact originalSender)
            : base(contact, originalSender)
        {
        }
    }

    [Serializable]
    public class TypingArrivedEventArgs : MessageArrivedEventArgs
    {
        public TypingArrivedEventArgs(Contact contact, Contact originalSender)
            : base(contact, originalSender)
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

        public TextMessageArrivedEventArgs(Contact sender, TextMessage textMessage, Contact originalSender)
            : base(sender, originalSender)
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

        public EmoticonArrivedEventArgs(Contact sender, Emoticon emoticon, Contact circle)
            : base(sender, circle)
        {
            this.emoticon = emoticon;
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

    /// <summary>
    /// Used to notify client programmer after the MSN data object transfer completed.
    /// </summary>
    public class MSNObjectDataTransferCompletedEventArgs : EventArgs
    {
        private MSNObject clientData = null;
        private bool aborted = false;
        private Contact remote = null;
        private Guid remoteEndPointID = Guid.Empty;

        /// <summary>
        /// Another site associated with this object's transfer.
        /// </summary>
        public Contact RemoteContact
        {
            get
            {
                return remote;
            }
        }

        /// <summary>
        /// The location of remote contact.
        /// </summary>
        public Guid RemoteContactEndPointID
        {
            get
            {
                return remoteEndPointID;
            }
        }

        /// <summary>
        /// Transfer failed.
        /// </summary>
        public bool Aborted
        {
            get
            {
                return aborted;
            }
        }

        /// <summary>
        /// The target msnobject.
        /// </summary>
        public MSNObject ClientData
        {
            get
            {
                return clientData;
            }
        }

        protected MSNObjectDataTransferCompletedEventArgs()
            : base()
        {
        }

        public MSNObjectDataTransferCompletedEventArgs(MSNObject clientdata, bool abort, Contact remoteContact, Guid remoteEPID)
        {
            if (clientdata == null)
                throw new ArgumentNullException("clientdata");

            clientData = clientdata;
            aborted = abort;
            remote = remoteContact;
            remoteEndPointID = remoteEPID;
        }
    }
};
