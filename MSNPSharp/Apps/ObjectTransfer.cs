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
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MSNPSharp.Apps
{
    using MSNPSharp;
    using MSNPSharp.P2P;
    using MSNPSharp.Core;

    [P2PApplication(12, "A4268EEC-FEC5-49E5-95C3-F126696BDBF6")]
    public class ObjectTransfer : P2PApplication
    {
        private bool sending;
        private MSNObject msnObject;
        private Stream objStream;

        public override bool AutoAccept
        {
            get
            {
                return true;
            }
        }

        public override string InvitationContext
        {
            get
            {

                if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                {
                    msnObject.SetContext(Remote.UserTileLocation, false);
                }
                else if (msnObject.ObjectType == MSNObjectType.Scene)
                {
                    msnObject.SetContext(Remote.SceneContext, false);
                }

                return Convert.ToBase64String(Encoding.UTF8.GetBytes(msnObject.ContextPlain));
            }
        }

        public bool Sending
        {
            get
            {
                return sending;
            }
        }

        public MSNObject Object
        {
            get
            {
                return msnObject;
            }
        }

        /// <summary>
        /// We are sender
        /// </summary>
        /// <param name="p2pSession"></param>
        public ObjectTransfer(P2PSession p2pSession)
            : base(p2pSession)
        {
            msnObject = new MSNObject();
            msnObject.SetContext(p2pSession.Invitation.BodyValues["Context"].Value, true);

            if (msnObject.ObjectType == MSNObjectType.UserDisplay ||
                msnObject.ObjectType == MSNObjectType.Unknown)
            {
                msnObject = NSMessageHandler.ContactList.Owner.DisplayImage;
                objStream = NSMessageHandler.ContactList.Owner.DisplayImage.OpenStream();
            }
            else if (msnObject.ObjectType == MSNObjectType.Scene)
            {
                msnObject = NSMessageHandler.ContactList.Owner.SceneImage;
                objStream = NSMessageHandler.ContactList.Owner.SceneImage.OpenStream();
            }
            else if (msnObject.ObjectType == MSNObjectType.Emoticon &&
                Local.Emoticons.ContainsKey(msnObject.Sha))
            {
                msnObject = Local.Emoticons[msnObject.Sha];
                objStream = ((Emoticon)msnObject).OpenStream();
            }

            sending = true;

            if (p2pSession.Invitation.BodyValues.ContainsKey("AppID"))
                applicationId = uint.Parse(p2pSession.Invitation.BodyValues["AppID"]);
        }

        /// <summary>
        /// We are receiver
        /// </summary>
        public ObjectTransfer(MSNObject obj, Contact remote)
            : base(remote.P2PVersionSupported, remote, remote.SelectRandomEPID())
        {
            msnObject = obj;

            if (msnObject.ObjectType == MSNObjectType.UserDisplay)
            {
                msnObject = new DisplayImage();
                applicationId = 12;
                msnObject.SetContext(remote.UserTileLocation, false);
            }
            else if (msnObject.ObjectType == MSNObjectType.Scene)
            {
                msnObject = new SceneImage();
                applicationId = 12;
                msnObject.SetContext(remote.SceneContext, false);
            }
            else if (msnObject.ObjectType == MSNObjectType.Emoticon)
            {
                applicationId = 11;
            }
            else
            {
                applicationId = 1;
            }

            sending = false;
        }

        public override void SetupInviteMessage(SLPMessage slp)
        {
            slp.BodyValues["RequestFlags"] = "18";

            base.SetupInviteMessage(slp);
        }

        public override bool ValidateInvitation(SLPMessage invite)
        {
            bool ret = base.ValidateInvitation(invite);

            if (ret)
            {
                MSNObject validObject = new MSNObject();
                validObject.SetContext(invite.BodyValues["Context"].Value, true);

                if (validObject.ObjectType == MSNObjectType.UserDisplay ||
                    validObject.ObjectType == MSNObjectType.Unknown)
                {
                    msnObject = Local.DisplayImage;
                    objStream = Local.DisplayImage.OpenStream();
                    ret |= true;
                }
                else if (validObject.ObjectType == MSNObjectType.Scene)
                {
                    msnObject = Local.SceneImage;
                    objStream = Local.SceneImage.OpenStream();
                    ret |= true;
                }
                else if (validObject.ObjectType == MSNObjectType.Emoticon &&
                    Local.Emoticons.ContainsKey(validObject.Sha))
                {
                    msnObject = Local.Emoticons[msnObject.Sha];
                    objStream = ((Emoticon)msnObject).OpenStream();

                    ret |= true;
                }
            }

            return ret;
        }

        public override void Start()
        {
            base.Start();

            if (Sending)
            {
                ushort packNum = base.P2PSession.IncreaseDataPacketNumber();

                // Data prep
                P2PDataMessage prepData = new P2PDataMessage(P2PVersion);
                prepData.WritePreparationBytes();

                if (P2PVersion == P2PVersion.P2PV2)
                {
                    prepData.V2Header.TFCombination = TFCombination.First;
                }

                SendMessage(prepData);
                Thread.CurrentThread.Join(900);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    "Data prep sent. Sending whole data...", GetType().Name);

                // All chunks
                byte[] allData = new byte[msnObject.Size];
                lock (objStream)
                {
                    using (Stream s = objStream)
                    {
                        s.Position = 0;
                        s.Read(allData, 0, allData.Length);
                    }
                }

                P2PDataMessage msg = new P2PDataMessage(P2PVersion);
                if (P2PVersion == P2PVersion.P2PV1)
                {
                    msg.V1Header.Flags = P2PFlag.Data;
                    msg.V1Header.AckSessionId = (uint)new Random().Next(50, int.MaxValue);
                }
                else if (P2PVersion == P2PVersion.P2PV2)
                {
                    msg.V2Header.TFCombination = TFCombination.MsnObject | TFCombination.First;
                    msg.V2Header.PackageNumber = packNum;
                }

                msg.InnerBody = allData;

                if (P2PVersion == P2PVersion.P2PV1)
                {
                    SendMessage(msg, delegate(P2PMessage ack)
                    {
                        OnTransferFinished(EventArgs.Empty);
                        // Close after remote client sends BYE.
                    });
                }
                else
                {
                    SendMessage(msg, null);

                    // Register the ACKHandler
                    P2PMessage rak = new P2PMessage(P2PVersion);
                    SendMessage(rak, delegate(P2PMessage ack)
                    {
                        OnTransferFinished(EventArgs.Empty);
                        // Close after remote client sends BYE.
                    });
                }
            }
            else
            {
                objStream = new MemoryStream();
            }
        }

        public override bool ProcessData(P2PBridge bridge, byte[] data, bool reset)
        {
            if (sending)
            {
                // We are sender but remote client want to kill me :)
                return false;
            }

            if (reset)
            {
                // Data prep or TFCombination.First
                objStream.SetLength(0);
            }

            if (data.Length > 0)
            {
                objStream.Write(data, 0, data.Length);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    String.Format("Received {0} / {1}", objStream.Length, msnObject.Size), GetType().Name);

                if (objStream.Length == msnObject.Size)
                {
                    // Finished transfer
                    byte[] allData = new byte[msnObject.Size];

                    objStream.Seek(0, SeekOrigin.Begin);
                    objStream.Read(allData, 0, allData.Length);

                    string dataSha = Convert.ToBase64String(new SHA1Managed().ComputeHash(allData));

                    if (dataSha != msnObject.Sha)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "Object hash doesn't match data hash, data invalid", GetType().Name);

                        return false;
                    }

                    MemoryStream ms = new MemoryStream(allData);
                    ms.Position = 0;

                    // Data CHECKSUM is ok, update MsnObject
                    if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                    {
                        DisplayImage newDisplayImage = new DisplayImage(Remote.Account.ToLowerInvariant(), ms);
                        Remote.SetDisplayImageAndFireDisplayImageChangedEvent(newDisplayImage);

                        msnObject = newDisplayImage;
                    }
                    else if (msnObject.ObjectType == MSNObjectType.Scene)
                    {
                        SceneImage newSceneImage = new SceneImage(Remote.Account.ToLowerInvariant(), ms);
                        Remote.SetSceneImageAndFireSceneImageChangedEvent(newSceneImage);

                        msnObject = newSceneImage;
                    }
                    else if (msnObject.ObjectType == MSNObjectType.Emoticon)
                    {
                        ((Emoticon)msnObject).Image = Image.FromStream(objStream);
                    }

                    objStream.Close();
                    OnTransferFinished(EventArgs.Empty);

                    if (P2PSession != null)
                        P2PSession.Close(); // Send first BYE
                }
            }
            return true;
        }
    }
};
