using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    [P2PApplication(2, "5D3E02AB-6190-11D3-BBBB-00C04F795683")]
    public class P2PFileTransferApplication : P2PApplication
    {
        public event EventHandler Progressed;

        Stream data;
        bool sending;

        static P2PFileTransferApplication()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Loaded", "P2PFileTransferApplication");
        }

        public P2PFileTransferApplication(P2PSession session)
            : base(session)
        {
            sending = false;
        }

        public P2PFileTransferApplication(Contact remote, Stream data, string filename)
            : base(remote)
        {
            sending = true;
        }

        public P2PFileTransferApplication(Contact remote, string filename)
            : this(remote, File.OpenRead(filename), Path.GetFileName(filename))
        {
        }

        


        public override string InvitationContext
        {
            get
            {
                return String.Empty;
            }
        }

        public long Transferred
        {
            get
            {
                if (Sending)
                    return data.Position;

                return data.Length;
            }
        }

        public bool Sending
        {
            get
            {
                return sending;
            }
        }

        public override void HandleMessage(IMessageProcessor sender, P2PMessage p2pMessage)
        {


            


        }
    }


};
