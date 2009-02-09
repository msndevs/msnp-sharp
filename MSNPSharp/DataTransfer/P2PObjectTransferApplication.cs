using System;
using System.IO;
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

        private uint dataPreparationAck;
        private MSNObject msnObject;
        private MemoryStream objStream;
        private bool sending;

        public MSNObject MsnObject
        {
            get
            {
                return msnObject;
            }
        }

        public MemoryStream ObjectStream
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
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(MsnObject.Context));
            }
        }

        public P2PObjectTransferApplication(P2PSession session)
            : base(session)
        {
            msnObject = new MSNObject();
            msnObject.ParseContext(session.Invite.BodyValues["Context"].Value, true);

            sending = true;
            Session.LocalIdentifier -= 4;
        }

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
                P2PDataMessage p2pDataMessage = new P2PDataMessage();
                p2pDataMessage.WritePreparationBytes();

                p2pDataMessage.SessionId = Session.SessionId;
                Session.IncreaseLocalIdentifier();

                p2pDataMessage.Identifier = Session.LocalIdentifier;
                p2pDataMessage.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);

                // store the ack identifier so we can accept the acknowledge later on
                dataPreparationAck = p2pDataMessage.AckSessionId;

                MessageProcessor.SendMessage(p2pDataMessage);
            }
            else
            {
                objStream = new MemoryStream();
            }
        }


        public override void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PBridge bridge = sender as P2PBridge;
            P2PMessage p2pMessage = message as P2PMessage;

            if ((objStream.Length == 0) && (p2pMessage.InnerBody.Length == 4) && BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
            {
                // Data prep
                if (Sending)
                {
                    Debug.Assert(p2pMessage.AckIdentifier == dataPreparationAck, "not data prep?");
                }
            }
            else if ((p2pMessage.Flags & P2PFlag.Data) == P2PFlag.Data || p2pMessage.Flags == P2PFlag.Normal)
            {
                objStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, String.Format("Received {0} / {1}", objStream.Length, msnObject.Size), GetType().Name);

                if (objStream.Length == msnObject.Size)
                {
                    byte[] data = new byte[msnObject.Size];

                    objStream.Seek(0, SeekOrigin.Begin);
                    objStream.Read(data, 0, data.Length);

                    string dataSha = Convert.ToBase64String(new SHA1Managed().ComputeHash(data));

                    if (dataSha == msnObject.Sha)
                    {
                        msnObject = new MSNObject(Session.Remote.Mail, objStream, MSNObjectType.Unknown, "");
                        OnTransferFinished(EventArgs.Empty);
                        Session.Close();
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "MsnObject hash doesn't match data hash, data invalid!", GetType().Name);
                    }
                }
            }
        }
    }
};
