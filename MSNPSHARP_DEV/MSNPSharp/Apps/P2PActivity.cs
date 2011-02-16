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
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.Apps
{
    using MSNPSharp;
    using MSNPSharp.P2P;
    using MSNPSharp.Core;

    /// <summary>
    /// A P2P application which can be used in P2P activities.
    /// </summary>
    /// <remarks>P2P activities can be defined as examples, including scene images, emoticons and display image's etc...</remarks>
    [P2PApplication(0, "6A13AF9C-5308-4F35-923A-67E8DDA40C2F")]
    public class P2PActivity : P2PApplication
    {
        private bool sending = false;
        private string activityData = String.Empty;
        private string activityName = String.Empty;

        public string ActivityName
        {
            get
            {
                return activityName;
            }
        }


        public P2PActivity(P2PSession p2pSess)
            : base(p2pSess.Version, p2pSess.Remote, p2pSess.RemoteContactEndPointID)
        {
            try
            {
                byte[] byts = Convert.FromBase64String(p2pSess.Invitation.BodyValues["Context"].Value);
                string activityUrl = System.Text.Encoding.Unicode.GetString(byts);
                string[] activityProperties = activityUrl.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (activityProperties.Length >= 3)
                {
                    uint.TryParse(activityProperties[0], out applicationId);
                    activityName = activityProperties[2];
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "An error occured while parsing activity context, error info: " +
                    ex.Message, GetType().Name);
            }

            sending = false;
        }

        /// <summary>
        /// P2PActivity constructor.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="applicationID"></param>
        /// <param name="activityName"></param>
        /// <param name="activityData"></param>
        public P2PActivity(Contact remote, uint applicationID, string activityName, string activityData)
            : base(remote.P2PVersionSupported, remote, remote.SelectRandomEPID())
        {
            this.applicationId = applicationID;
            this.activityName = activityName;
            this.activityData = activityData;

            sending = true;
        }

        public override string InvitationContext
        {
            get
            {
                string activityID = applicationId + ";1;" + activityName;
                byte[] contextData = UnicodeEncoding.Unicode.GetBytes(activityID);
                return Convert.ToBase64String(contextData, 0, contextData.Length);
            }
        }

        public override bool ValidateInvitation(SLPMessage invitation)
        {
            bool ret = base.ValidateInvitation(invitation);

            if (ret)
            {
                try
                {
                    byte[] byts = Convert.FromBase64String(invitation.BodyValues["Context"].Value);
                    string activityUrl = System.Text.Encoding.Unicode.GetString(byts);
                    string[] activityProperties = activityUrl.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (activityProperties.Length >= 3)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "An error occured while parsing activity context, error info: " +
                        ex.Message, GetType().Name);
                }
            }
            return ret;
        }

        public override void Start()
        {
            base.Start();

            if (sending)
            {
                if (activityData != string.Empty && activityData != null)
                {
                    activityData += "\0";
                    int urlLength = Encoding.Unicode.GetByteCount(activityData);


                    // Data prep
                    MemoryStream urlDataStream = new MemoryStream();

                    P2PDataMessage prepData = new P2PDataMessage(P2PVersion);

                    byte[] header = (P2PVersion == P2PVersion.P2PV1) ?
                        new byte[] { 0x80, 0x00, 0x00, 0x00 }
                        :
                        new byte[] { 0x80, 0x3f, 0x14, 0x05 };

                    urlDataStream.Write(header, 0, header.Length);

                    urlDataStream.Write(BitUtility.GetBytes((ushort)0x08, true), 0, sizeof(ushort));  //data type: 0x08: string
                    urlDataStream.Write(BitUtility.GetBytes(urlLength, true), 0, sizeof(int));
                    urlDataStream.Write(Encoding.Unicode.GetBytes(activityData), 0, urlLength);

                    urlDataStream.Seek(0, SeekOrigin.Begin);

                    byte[] urlData = urlDataStream.ToArray();

                    urlDataStream.Close();

                    prepData.InnerBody = urlData;


                    if (P2PVersion == P2PVersion.P2PV2)
                    {
                        prepData.V2Header.TFCombination = TFCombination.First;
                    }

                    SendMessage(prepData);

                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        "Data sent...", GetType().Name);

                }

            }
        }

        public override bool ProcessData(P2PBridge bridge, byte[] data, bool reset)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "DATA RECEIVED: " + P2PMessage.DumpBytes(data, 128, true), GetType().Name);

            return true;
        }

    }
};
