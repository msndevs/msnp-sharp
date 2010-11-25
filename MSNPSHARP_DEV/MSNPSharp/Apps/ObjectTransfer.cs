using System;
using System.IO;
using System.Text;
using System.Drawing;
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
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(Remote.UserTileLocation));
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
            msnObject = new MSNObject();
            msnObject.SetContext(remote.UserTileLocation, false);


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

            /*
            MSNObject obj = MsnObject.Load(invite.Session, EncodingUtility.Base64DecodeSafe(invite.MIMEBody["Context"].Value));

            if (obj != null)
            {
                // Invite is valid if the objects creator is the local account & the object has data
                ret &= (obj.Creator == invite.Session.Account) && (obj.Data != null) && (obj.Data.Length > 0);
            }
            else
            {
                // Sometimes (especially with bots) the invite doesn't give us the object in the context
                // so we accept that and send them the account display pic if we have one

                ret &= invite.Session.Account.DisplayImage is MsnDisplayImage;
            }
            */
            return ret;
        }

        public override void Start()
        {
            base.Start();

            if (Sending)
            {
                ushort packNum = base.P2PSession.IncreaseDataPacketNumber();

                P2PDataMessage p2pData = new P2PDataMessage(P2PVersion);
                p2pData.WritePreparationBytes();

                if (P2PVersion == P2PVersion.P2PV2)
                {
                    p2pData.V2Header.TFCombination = TFCombination.First;
                    p2pData.V2Header.PackageNumber = packNum;
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Data prep sent", GetType().Name);
                SendMessage(p2pData);

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
                }
                else if (P2PVersion == P2PVersion.P2PV2)
                {
                    msg.V2Header.TFCombination = TFCombination.MsnObject | TFCombination.First;
                    msg.V2Header.PackageNumber = packNum;
                }

                msg.InnerBody = allData;

                SendMessage(msg, delegate(P2PMessage ack)
                {
                    Abort();
                    OnTransferFinished(EventArgs.Empty);
                });
            }
            else
            {
                objStream = new MemoryStream();
            }
        }

        public override bool ProcessP2PMessage(P2PBridge bridge, P2PMessage msg)
        {
            if (msg.InnerBody.Length == 4 && (BitUtility.ToInt32(msg.InnerBody, 0, true) == 0))
            {
                // Data prep
                return true;
            }
            else if (msg.Header.SessionId == P2PSession.SessionId && msg.InnerBody.Length > 0)
            {
                objStream.Write(msg.InnerBody, 0, msg.InnerBody.Length);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    String.Format("Received {0} / {1}", objStream.Length, msnObject.Size), GetType().Name);

                if (objStream.Length == msnObject.Size)
                {
                    // Finished transfer
                    byte[] data = new byte[msnObject.Size];

                    objStream.Seek(0, SeekOrigin.Begin);
                    objStream.Read(data, 0, data.Length);

                    string dataSha = Convert.ToBase64String(new SHA1Managed().ComputeHash(data));

                    if (dataSha != msnObject.Sha)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "Object hash doesn't match data hash, data invalid", GetType().Name);

                        return false;
                    }

                    MemoryStream ms = new MemoryStream(data);
                    ms.Position = 0;

                    // Data CHECKSUM is ok, update MsnObject
                    if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                    {
                        DisplayImage newDisplayImage = new DisplayImage(Remote.Mail.ToLowerInvariant(), ms);
                        Remote.SetDisplayImageAndFireDisplayImageChangedEvent(newDisplayImage);

                        msnObject = newDisplayImage;
                    }
                    else if (msnObject.ObjectType == MSNObjectType.Emoticon)
                    {
                        // TODO ((Emoticon)msnObject).Image = Image.FromStream(objStream);
                    }

                    objStream.Close();
                    OnTransferFinished(EventArgs.Empty);
                    P2PSession.Close();
                }

                return true;
            }

            return false;
        }
    }
};
