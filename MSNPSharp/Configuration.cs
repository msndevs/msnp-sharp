#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;

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
        /// Don't use compression when saving addressbook files.
        /// </summary>
        public static bool NoCompress = false;

        /// <summary>
        /// Don't save addressbook files.
        /// </summary>
        public static bool NoSave;

        private static string savepath = Path.GetFullPath(".");
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

        private static int msnTicketLifeTime = 20;
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
                    throw new ArgumentOutOfRangeException("value");

                    msnTicketLifeTime = value;
            }
        }

        private static int msnTicketsCleanupInterval = 5;
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
                    throw new ArgumentOutOfRangeException("value");

                msnTicketsCleanupInterval = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        static Settings()
        {
            TraceSwitch.Level = TraceLevel.Error;
        }
    }
};