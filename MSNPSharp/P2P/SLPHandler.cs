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

        internal bool HandleP2PSessionSignal(P2PBridge bridge, P2PMessage p2pMessage, SLPMessage slp, P2PSession session)
        {
            if (slp is SLPRequestMessage)
            {
                SLPRequestMessage slpRequest = slp as SLPRequestMessage;

                if (slpRequest.ContentType == "application/x-msnmsgr-sessionclosebody" &&
                    slpRequest.Method == "BYE")
                {
                    if (p2pMessage.Version == P2PVersion.P2PV1)
                    {
                        P2PMessage byeAck = p2pMessage.CreateAcknowledgement();
                        byeAck.V1Header.Flags = P2PFlag.CloseSession;
                        session.Send(byeAck);
                    }
                    else if (p2pMessage.Version == P2PVersion.P2PV2)
                    {
                        SLPRequestMessage slpMessage = new SLPRequestMessage(session.RemoteContactEPIDString, "BYE");
                        slpMessage.Target = session.RemoteContactEPIDString;
                        slpMessage.Source = session.LocalContactEPIDString;
                        slpMessage.Branch = session.Invitation.Branch;
                        slpMessage.CallId = session.Invitation.CallId;
                        slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";
                        slpMessage.BodyValues["SessionID"] = session.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        session.Send(session.WrapSLPMessage(slpMessage));
                    }

                    session.OnClosed(new ContactEventArgs(session.Remote));

                    return true;
                }
                else if (slpRequest.ContentType == "application/x-msnmsgr-sessionreqbody" &&
                    slpRequest.Method == "INVITE")
                {
                    SLPStatusMessage slpMessage = new SLPStatusMessage(session.RemoteContactEPIDString, 500, "Internal Error");
                    slpMessage.Target = session.RemoteContactEPIDString;
                    slpMessage.Source = session.LocalContactEPIDString;
                    slpMessage.Branch = session.Invitation.Branch;
                    slpMessage.CallId = session.Invitation.CallId;
                    slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
                    slpMessage.BodyValues["SessionID"] = session.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    P2PMessage errorMessage = session.WrapSLPMessage(slpMessage);
                    bridge.Send(null, session.Remote, session.RemoteContactEndPointID, errorMessage, null);
                    return true;
                }
                else
                {
                    if (slpRequest.ContentType == "application/x-msnmsgr-transreqbody" ||
                        slpRequest.ContentType == "application/x-msnmsgr-transrespbody" ||
                        slpRequest.ContentType == "application/x-msnmsgr-transdestaddrupdate")
                    {
                        P2PSession.ProcessDirectInvite(slpRequest, nsMessageHandler, session); // Direct connection invite
                        return true;
                    }
                }
            }
            else if (slp is SLPStatusMessage)
            {
                SLPStatusMessage slpStatus = slp as SLPStatusMessage;

                if (slpStatus.Code == 200) // OK
                {
                    if (slpStatus.ContentType == "application/x-msnmsgr-transrespbody")
                    {
                        P2PSession.ProcessDirectInvite(slpStatus, nsMessageHandler, session);
                    }
                    else
                    {
                        session.OnActive(EventArgs.Empty);
                        session.Application.Start();
                    }

                    return true;
                }
                else if (slpStatus.Code == 603) // Decline
                {
                    session.OnClosed(new ContactEventArgs(session.Remote));

                    return true;
                }
                else if (slpStatus.Code == 500) // Internal Error
                {
                    return true;
                }
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                 String.Format("Unhandled SLP Message in session:---->>\r\n{0}", p2pMessage.ToString()), GetType().Name);

            session.OnError(EventArgs.Empty);
            return true;
        }

        internal bool CheckSLPMessage(P2PBridge bridge, Contact source, Guid sourceGuid, P2PMessage msg, SLPMessage slp)
        {
            string src = source.Account.ToLowerInvariant();
            string target = nsMessageHandler.Owner.Account;

            if (slp.FromEmailAccount.ToLowerInvariant() != src)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received message from '{0}', differing from source '{1}'", slp.Source, src), GetType().Name);

                return false;
            }
            else if (slp.ToEmailAccount.ToLowerInvariant() != target)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("Received P2P message intended for '{0}', not us '{1}'", slp.Target, target), GetType().Name);

                if (slp.FromEmailAccount == target)
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

        internal void SendSLPStatus(P2PBridge bridge, P2PMessage msg, Contact dest, Guid destGuid, int code, string phrase)
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
                if (msgSLP.BodyValues.ContainsKey("SessionID"))
                {
                    slp.BodyValues["SessionID"] = msgSLP.BodyValues["SessionID"];
                }

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
