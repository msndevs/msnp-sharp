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
using System.IO;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;

    /// <summary>
    /// Binding public ports to support direct connections
    /// </summary>
    [Serializable]
    public enum PublicPortPriority
    {
        /// <summary>
        /// Don't bind public ports for direct connections.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bind public ports first for direct connections.
        /// </summary>
        First = 1,

        /// <summary>
        /// Bind public ports last for direct connections when other port failed.
        /// </summary>
        Last = 9
    }

    /// <summary>
    /// General configuration options.
    /// </summary>
    [Serializable()]
    public static class Settings
    {
        /// <summary>
        /// Defines the verbosity of the trace messages.
        /// </summary>
        public static TraceSwitch TraceSwitch = new TraceSwitch("MSNPSharp", "MSNPSharp switch");

        /// <summary>
        /// Trace soap request/response.
        /// </summary>
        public static bool TraceSoap = false;

        /// <summary>
        /// Ports for DC to try bind. These ports aren't firewalled by ISS.
        /// </summary>
        public static readonly ushort[] PublicPorts = new ushort[]
        {
            80,  // http
            21,  // ftp
            25,  // smtp
            110, // pop3
            443, // https
            8080 // webcache
        };

        /// <summary>
        /// Constructor.
        /// </summary>
        static Settings()
        {
            isMono = (null != Type.GetType("Mono.Runtime")); // http://www.mono-project.com/FAQ:_Technical

            TraceSwitch.Level = TraceLevel.Verbose;

            serializationType = MclSerialization.Compression | MclSerialization.Cryptography;

            enableGzipCompressionForWebServices = (isMono == false);
#if DEBUG
            TraceSoap = true;
#endif
        }

        private static PublicPortPriority publicPortPriority = PublicPortPriority.First;
        private static bool disableP2PDirectConnections = false;
        private static bool disableHttpPolling = false;

        private static string savepath = Path.GetFullPath(".");
        private static bool enableGzipCompressionForWebServices;
        private static MclSerialization serializationType;
        private static int msnTicketsCleanupInterval = 5;
        private static int msnTicketLifeTime = 20;
        private static bool noSave;
        private static bool isMono;

        /// <summary>
        /// Public port priority for direct connections. If the machine connecting internet is hosted
        /// for servers (http, ftp, smtp, pop3 etc.) you should set this to
        /// <see cref="MSNPSharp.PublicPortPriority.None"/>.
        /// </summary>
        public static PublicPortPriority PublicPortPriority
        {
            get
            {
                return publicPortPriority;
            }
            set
            {
                publicPortPriority = value;
            }
        }

        /// <summary>
        /// Disable P2P direct connections for transfers if direct connections fails or
        /// the machine connecting internet is behind NAT. Slow switchboard connections are used.
        /// </summary>
        public static bool DisableP2PDirectConnections
        {
            get
            {
                return Settings.disableP2PDirectConnections;
            }
            set
            {
                Settings.disableP2PDirectConnections = value;
            }
        }

        /// <summary>
        /// Never use HTTP polling feature
        /// </summary>
        public static bool DisableHttpPolling
        {
            get
            {
                return Settings.disableHttpPolling;
            }
            set
            {
                Settings.disableHttpPolling = value;
            }
        }

        /// <summary>
        /// Indicates whether the runtime framework is currently mono or not.
        /// </summary>
        public static bool IsMono
        {
            get
            {
                return isMono;
            }
        }

        /// <summary>
        /// File serialization type when saving.
        /// </summary>Compression saves spaces on disk, Encrypt protects your addressbook but eats some cpu<remarks>
        /// </remarks>
        public static MclSerialization SerializationType
        {
            get
            {
                return serializationType;
            }
            set
            {
                serializationType = value;
            }
        }

        /// <summary>
        /// Don't save addressbook files.
        /// </summary>
        public static bool NoSave
        {
            get
            {
                return noSave;
            }
            set
            {
                noSave = value;
            }
        }

        /// <summary>
        /// Save directory
        /// </summary>
        public static string SavePath
        {
            get
            {
                return savepath;
            }
            set
            {
                savepath = value;
            }
        }

        /// <summary>MSNTicket lifetime in minutes for the internal cache. Default is 20 minutes.</summary>
        /// <remarks>Keep small if the client will connect to the msn network for the short time.</remarks>
        public static int MSNTicketLifeTime
        {
            get
            {
                return msnTicketLifeTime;
            }
            set
            {
                if (value <= 0)
                    value = 20;

                msnTicketLifeTime = value;
            }
        }

        /// <summary>
        /// Run clean up code for the MSNTickets in every x minutes. Default is 5 minutes.
        /// </summary>
        public static int MSNTicketsCleanupInterval
        {
            get
            {
                return msnTicketsCleanupInterval;
            }
            set
            {
                if (value <= 0)
                    value = 5;

                msnTicketsCleanupInterval = value;
            }
        }

        /// <summary>
        /// Use Gzip compression for web services to save bandwidth.
        /// </summary>
        /// <remarks>Don't use this if you run .net framework on mono</remarks>
        public static bool EnableGzipCompressionForWebServices
        {
            get
            {
                return enableGzipCompressionForWebServices;
            }
            set
            {
                enableGzipCompressionForWebServices = value;
            }
        }
    }
};