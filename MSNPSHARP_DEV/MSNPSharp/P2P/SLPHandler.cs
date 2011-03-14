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
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    public class SLPHandler : IDisposable
    {
        private NSMessageHandler nsMessageHandler = null;

        protected internal SLPHandler(NSMessageHandler nsHandler)
        {
            this.nsMessageHandler = nsHandler;
        }

        public virtual void Dispose()
        {
        }

        internal P2PSession ProcessSLPMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg, SLPMessage slp)
        {
            SLPRequestMessage req = slp as SLPRequestMessage;

            if (req != null && req.Method == "INVITE" &&
                req.ContentType == "application/x-msnmsgr-sessionreqbody")
            {
                // Start a new session
                return new P2PSession(req, msg, nsMessageHandler, bridge);
            }

            return null;
        }

        internal bool CheckSLPMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg, SLPMessage slp)
        {
            string src = source.Account.ToLowerInvariant();
            string target = nsMessageHandler.Owner.Account.ToLowerInvariant();

            if (msg.Version == P2PVersion.P2PV2)
            {
                src += ";" + sourceGuid.ToString("B").ToLowerInvariant();
                target += ";" + NSMessageHandler.MachineGuid.ToString("B").ToLowerInvariant();
            }

            if (slp.Source.ToLowerInvariant() != src)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received message from '{0}', differing from source '{1}'", slp.Source, src), GetType().Name);

                return false;
            }
            else if (slp.Target.ToLowerInvariant() != target)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received P2P message intended for '{0}', not us '{1}'", slp.Target, target), GetType().Name);

                if (slp.Source == target)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        "We received a message from ourselves?", GetType().Name);
                }
                else
                {
                    SendSLPStatus(bridge, msg, source, sourceGuid, 404, "Not Found");
                }

                return false;
            }

            return true;
        }

        private void SendSLPStatus(P2PBridge bridge, P2PMessage msg, Contact dest, Guid destGuid, int code, string phrase)
        {
            string target = dest.Account.ToLowerInvariant();

            if (msg.Version == P2PVersion.P2PV2)
            {
                target += ";" + destGuid.ToString("B");
            }

            SLPMessage slp = new SLPStatusMessage(target, code, phrase);

            if (msg.IsSLPData)
            {
                SLPMessage msgSLP = msg.InnerMessage as SLPMessage;
                slp.Branch = msgSLP.Branch;
                slp.CallId = msgSLP.CallId;
                slp.Source = msgSLP.Target;
                slp.ContentType = msgSLP.ContentType;
            }
            else
                slp.ContentType = "null";

            P2PMessage response = new P2PMessage(msg.Version);
            response.InnerMessage = slp;

            if (msg.Version == P2PVersion.P2PV1)
            {
                response.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }
            else if (msg.Version == P2PVersion.P2PV2)
            {
                response.V2Header.OperationCode = (byte)OperationCode.None;
                response.V2Header.TFCombination = TFCombination.First;
            }

            bridge.Send(null, dest, destGuid, response, null);
        }


    }
};
