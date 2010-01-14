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

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// Used when contact changed its status.
    /// </summary>
    [Serializable()]
    public class ContactStatusChangedEventArgs : StatusChangedEventArgs
    {
        Contact contact;

        /// <summary>
        /// The contact who changed its status.
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

        public ContactStatusChangedEventArgs(Contact contact,
                                            PresenceStatus oldStatus)
            :base(oldStatus)
        {
            Contact = contact;
        }
    }

    /// <summary>
    /// Used when any contect event occured.
    /// </summary>
    [Serializable()]
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

    /// <summary>
    /// Used when a contact changed its status.
    /// </summary>
    [Serializable()]
    public class StatusChangedEventArgs : EventArgs
    {
        private PresenceStatus oldStatus;

        public PresenceStatus OldStatus
        {
            get
            {
                return oldStatus;
            }
            set
            {
                oldStatus = value;
            }
        }

        public StatusChangedEventArgs(PresenceStatus oldStatus)
        {
            OldStatus = oldStatus;
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
            :base(contact)
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
            :base(sender)
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
        private MSNLists affectedList = MSNLists.None;

        /// <summary>
        /// The list which was send by the server
        /// </summary>
        public MSNLists AffectedList
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
        public ListReceivedEventArgs(MSNLists affectedList)
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
        private MSNLists affectedList = MSNLists.None;

        /// <summary>
        /// The list which mutated.
        /// </summary>
        public MSNLists AffectedList
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
        public ListMutateEventArgs(Contact contact, MSNLists affectedList)
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

    /// <summary>
    /// Base class for circle event arg.
    /// </summary>
    public class BaseCircleEventArgs : EventArgs
    {
        protected Circle circle = null;
        internal BaseCircleEventArgs(Circle circle)
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
        public Circle Circle
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
        internal CircleEventArgs(Circle circle)
            :base(circle)
        {
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="remote">The affected Contact.</param>
        internal CircleEventArgs(Circle circle, Contact remote)
            :base(circle)
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

        internal CircleMemberEventArgs(Circle circle, Contact member)
            : base(circle, member)
        {
        }
    }

    [Serializable()]
    public class CircleStatusChangedEventArgs: StatusChangedEventArgs
    {
        protected Circle circle = null;

        /// <summary>
        /// The circle which changed its status.
        /// </summary>
        public Circle Circle
        {
            get { return circle; }
        }

        internal CircleStatusChangedEventArgs(Circle circle, PresenceStatus oldStatus)
            : base(oldStatus)
        {
            this.circle = circle;
        }
    }

    [Serializable()]
    public class CircleMemberStatusChanged : CircleStatusChangedEventArgs
    {
        private Contact circleMember = null;

        protected Contact CircleMember
        {
            get { return circleMember; }
        }

        internal CircleMemberStatusChanged(Circle circle, Contact member, PresenceStatus oldStatus)
            : base(circle, oldStatus)
        {
            circleMember = member;
        }

    }

    /// <summary>
    /// Event argument used for ContactService.JoinCircleInvitationReceived event.
    /// </summary>
    [Serializable()]
    public class JoinCircleInvitationEventArgs : CircleEventArgs
    {

        /// <summary>
        /// <see cref="Contact"/> who send this invitation.
        /// </summary>
        public CircleInviter Inviter
        {
            get { return remoteMember as CircleInviter; }
        }

        internal JoinCircleInvitationEventArgs(Circle circle, CircleInviter invitor)
            : base(circle)
        {
        }
    }

    /// <summary>
    /// Event argument used for receiving text messages from a circle.
    /// </summary>
    [Serializable()]
    public class CircleTextMessageEventArgs : TextMessageEventArgs
    {
        protected Contact triggerMember = null;

        public CircleTextMessageEventArgs(TextMessage textMessage, Circle sender, Contact triggerMember)
            : base(textMessage, sender)
        {
            this.triggerMember = triggerMember;
        }

        /// <summary>
        /// The circle message send from.
        /// </summary>
        public new Circle Sender
        {
            get
            {
                return base.Sender as Circle;
            }
        }

        /// <summary>
        /// The circle member who send this message.
        /// </summary>
        public Contact TriggerMember
        {
            get
            {
                return triggerMember;
            }
        }
    }
};
