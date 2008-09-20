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

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

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
    /// Used as event argument when a textual message is send.
    /// </summary>
    [Serializable()]
    public class TextMessageEventArgs : EventArgs
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

        private Contact sender;

        /// <summary>
        /// The sender of the message.
        /// </summary>
        public Contact Sender
        {
            get
            {
                return sender;
            }
            set
            {
                sender = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        public TextMessageEventArgs(TextMessage message, Contact sender)
        {
            Message = message;
            Sender = sender;
        }
    }

    [Serializable()]
    public class WinkEventArgs : EventArgs
    {
        private Contact sender;

        /// <summary>
        /// The sender of the message.
        /// </summary>
        public Contact Sender
        {
            get
            {
                return sender;
            }
            set
            {
                sender = value;
            }
        }

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
        {
            this.sender = contact;
            this.wink = wink;
        }
    }

    /// <summary>
    /// Used as event argument when a emoticon definition is send.
    /// </summary>
    [Serializable()]
    public class EmoticonDefinitionEventArgs : EventArgs
    {
        private Contact sender;

        /// <summary>
        /// The sender of the message.
        /// </summary>
        public Contact Sender
        {
            get
            {
                return sender;
            }
            set
            {
                sender = value;
            }
        }

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
        {
            this.sender = sender;
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
    public class ListMutateEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private Contact contact;

        /// <summary>
        /// The affected contact.
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
        {
            Contact = contact;
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
};
