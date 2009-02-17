using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    [P2PApplication(1, "A4268EEC-FEC5-49E5-95C3-F126696BDBF6")]
    public class P2PObjectTransferApplication : P2PApplication
    {
        static P2PObjectTransferApplication()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Loaded", "P2PObjectTransferApplication");
        }

        private MSNObject msnObject;
        private Stream objStream;
        private bool sending;

        public MSNObject MsnObject
        {
            get
            {
                return msnObject;
            }
        }

        public Stream ObjectStream
        {
            get
            {
                return objStream;
            }
        }

        public bool Sending
        {
            get
            {
                return sending;
            }
        }

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
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(MSNObject.GetDecodeString(msnObject.OriginalContext)));
            }
        }

        /// <summary>
        /// We are sender
        /// </summary>
        /// <param name="session"></param>
        public P2PObjectTransferApplication(P2PSession session)
            : base(session)
        {
            msnObject = new MSNObject();
            msnObject.ParseContext(session.Invite.BodyValues["Context"].Value, true);

            if (msnObject.ObjectType == MSNObjectType.UserDisplay ||
                msnObject.ObjectType == MSNObjectType.Unknown)
            {
                msnObject = NSMessageHandler.Owner.DisplayImage;
                objStream = NSMessageHandler.Owner.DisplayImage.OpenStream();
                Session.LocalIdentifier -= 4;
            }
            else if (msnObject.ObjectType == MSNObjectType.Emoticon &&
                Local.Emoticons.ContainsKey(msnObject.Sha))
            {
                msnObject = Local.Emoticons[msnObject.Sha];
                objStream = ((Emoticon)msnObject).OpenStream();
            }

            if (session.Invite.BodyValues.ContainsKey("AppID"))
                applicationId = uint.Parse(session.Invite.BodyValues["AppID"]);

            sending = true;
        }

        /// <summary>
        /// We are receiver
        /// </summary>
        /// <param name="msnObj">Msn object requested</param>
        /// <param name="contact">Remote contact</param>
        public P2PObjectTransferApplication(MSNObject msnObj, Contact contact)
            : base(contact)
        {
            msnObject = msnObj;
            sending = false;
        }

        public override bool ValidateInvitation(SLPMessage invitation)
        {
            return base.ValidateInvitation(invitation);
        }

        public override void Start()
        {
            base.Start();

            if (Sending)
            {
                P2PDataMessage p2pData = new P2PDataMessage();
                p2pData.WritePreparationBytes();
                p2pData.Flags = P2PFlag.Data;
                p2pData.MessageSize = (uint)p2pData.InnerBody.Length;
                p2pData.TotalSize = p2pData.MessageSize;

                SendMessage(p2pData, delegate(P2PMessage ack)
                {
                    byte[] data = new byte[msnObject.Size];
                    using (Stream s = objStream)
                    {
                        s.Position = 0;
                        s.Read(data, 0, data.Length);
                    }
                    P2PDataMessage p2pMessage = new P2PDataMessage();
                    p2pMessage.Flags = P2PFlag.MSNObjectData;
                    p2pMessage.InnerBody = data;

                    p2pMessage.MessageSize = (uint)p2pMessage.InnerBody.Length;
                    p2pMessage.TotalSize = p2pMessage.MessageSize;

                    SendMessage(p2pMessage, delegate
                    {
                        OnTransferFinished(EventArgs.Empty);
                    });
                });
            }
            else
            {
                objStream = new MemoryStream();
            }
        }


        public override void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            if ((p2pMessage.InnerBody.Length == 4) && BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
            {
                

            }
            else if ((p2pMessage.Flags & P2PFlag.Data) == P2PFlag.Data)
            {
                objStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("Received {0} / {1}", objStream.Length, msnObject.Size), GetType().Name);

                if (objStream.Length == msnObject.Size)
                {
                    objStream.Seek(0, SeekOrigin.Begin);

                    if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                    {
                        ((DisplayImage)msnObject).Image = Image.FromStream(objStream);
                    }
                    else if (msnObject.ObjectType == MSNObjectType.Emoticon)
                    {
                        ((Emoticon)msnObject).Image = Image.FromStream(objStream);
                    }

                    objStream.Close();
                    OnTransferFinished(EventArgs.Empty);
                    Session.Close();
                }

            }
        }
    }
};
