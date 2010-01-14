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

namespace MSNPSharp.Core
{
    using System;
    using System.Reflection;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Defines the way in which dotMSN core objects are created. No more Used by MSNPSharp core classes.
    /// Override these types to use custom-made handlers or processor.
    /// </summary>
    [Obsolete("No more used by MSNPSharp core classes.")]
    public sealed class Factory
    {
        private static Type nameserverHandler = typeof(NSMessageHandler);
        private static Type nameserverProcessor = typeof(NSMessageProcessor);
        private static Type switchboardHandler = typeof(SBMessageHandler);
        private static Type yimmessageHandler = typeof(YIMMessageHandler);
        private static Type switchboardProcessor = typeof(SBMessageProcessor);
        private static Type contact = typeof(Contact);
        private static Type circle = typeof(Circle);
        private static Type p2pHandler = typeof(P2PHandler);
        private static Type p2pTransferSession = typeof(P2PTransferSession);
        private static Type p2pMessageSession = typeof(P2PMessageSession);
        private static Type msnslpHandler = typeof(MSNSLPHandler);

        /// <summary>
        /// The type used to create nameserver handler objects.
        /// </summary>
        public static Type NameserverHandler
        {
            get
            {
                return nameserverHandler;
            }
            set
            {
                nameserverHandler = value;
            }
        }

        /// <summary>
        /// The type used to create nameserver processor objects.
        /// </summary>
        public static Type NameserverProcessor
        {
            get
            {
                return nameserverProcessor;
            }
            set
            {
                nameserverProcessor = value;
            }
        }

        /// <summary>
        /// The type used to create switchboard handler objects.
        /// </summary>
        public static Type SwitchboardHandler
        {
            get
            {
                return switchboardHandler;
            }
            set
            {
                switchboardHandler = value;
            }
        }

        /// <summary>
        /// The type used to create Yahoo Messenger message handler objects.
        /// </summary>
        public static Type YIMMessageHandler
        {
            get
            {
                return yimmessageHandler;
            }
            set
            {
                yimmessageHandler = value;
            }
        }

        /// <summary>
        /// The type used to create nameserver processor objects.
        /// </summary>
        public static Type SwitchboardProcessor
        {
            get
            {
                return switchboardProcessor;
            }
            set
            {
                switchboardProcessor = value;
            }
        }

        /// <summary>
        /// The type used to create contact objects.
        /// </summary>
        public static Type Contact
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
        /// The type used to create circle objects.
        /// </summary>
        public static Type Circle
        {
            get { return circle; }
            set { circle = value; }
        }

        /// <summary>
        /// The type used to create P2P Handler objects.
        /// </summary>
        public static Type P2PHandler
        {
            get
            {
                return p2pHandler;
            }
            set
            {
                p2pHandler = value;
            }
        }

        /// <summary>
        /// The type used to create P2P transfer session objects.
        /// </summary>
        public static Type P2PTransferSession
        {
            get
            {
                return p2pTransferSession;
            }
            set
            {
                p2pTransferSession = value;
            }
        }

        /// <summary>
        /// The type used to create P2P message session objects.
        /// </summary>
        public static Type P2PMessageSession
        {
            get
            {
                return p2pMessageSession;
            }
            set
            {
                p2pMessageSession = value;
            }
        }

        /// <summary>
        /// The type used to create MSNSLP Handler objects.
        /// </summary>
        public static Type MSNSLPHandler
        {
            get
            {
                return msnslpHandler;
            }
            set
            {
                msnslpHandler = value;
            }
        }



        /// <summary>
        /// Creates a default msnslpHandler.
        /// </summary>
        /// <returns></returns>
        public static MSNSLPHandler CreateMSNSLPHandler()
        {
            return (MSNSLPHandler)Activator.CreateInstance(msnslpHandler, true);
        }

        /// <summary>
        /// Creates a default p2p handler.
        /// </summary>
        /// <returns></returns>
        public static P2PHandler CreateP2PHandler()
        {
            return (P2PHandler)Activator.CreateInstance(p2pHandler, true);
        }

        /// <summary>
        /// Creates a default p2p transfer session handler.
        /// </summary>
        /// <returns></returns>
        public static P2PTransferSession CreateP2PTransferSession()
        {
            return (P2PTransferSession)Activator.CreateInstance(p2pTransferSession, true);
        }

        /// <summary>
        /// Creates a default p2p message session handler.
        /// </summary>
        /// <returns></returns>
        public static P2PMessageSession CreateP2PMessageSession()
        {
            return (P2PMessageSession)Activator.CreateInstance(p2pMessageSession, true);
        }

        /// <summary>
        /// Creates a default contact.
        /// </summary>
        /// <returns></returns>
        public static Contact CreateContact()
        {
            return (Contact)Activator.CreateInstance(contact, true);
        }

        /// <summary>
        /// Creates a default circle.
        /// </summary>
        /// <returns></returns>
        public static Circle CreateCircle()
        {
            return (Circle)Activator.CreateInstance(circle, true);
        }

        /// <summary>
        /// Creates a default switchboard handler.
        /// </summary>
        /// <returns></returns>
        public static SBMessageProcessor CreateSwitchboardProcessor()
        {
            return (SBMessageProcessor)Activator.CreateInstance(switchboardProcessor, true);
        }

        /// <summary>
        /// Creates a default switchboard handler.
        /// </summary>
        /// <returns></returns>
        public static SBMessageHandler CreateSwitchboardHandler()
        {
            return (SBMessageHandler)Activator.CreateInstance(switchboardHandler, true);
        }

        /// <summary>
        /// Creates a default Yahoo Messenger message handler.
        /// </summary>
        /// <returns></returns>
        public static YIMMessageHandler CreateYIMMessageHandler()
        {
            return (YIMMessageHandler)Activator.CreateInstance(yimmessageHandler, true);
        }

        /// <summary>
        /// Creates a default nameserver handler.
        /// </summary>
        /// <returns></returns>
        public static NSMessageHandler CreateNameserverHandler()
        {
            return (NSMessageHandler)Activator.CreateInstance(nameserverHandler, true);
        }

        /// <summary>
        /// Creates a default nameserver processor.
        /// </summary>
        /// <returns></returns>
        public static NSMessageProcessor CreateNameserverProcessor()
        {
            return (NSMessageProcessor)Activator.CreateInstance(nameserverProcessor, true);
        }

        /// <summary>
        /// No instances required.
        /// </summary>
        private Factory()
        {
        }
    }
};
