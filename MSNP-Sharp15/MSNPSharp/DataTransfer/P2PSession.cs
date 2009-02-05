using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.DataTransfer
{
    public class P2PSession : IDisposable
    {

        public event EventHandler<ContactEventArgs> Closing;
        public event EventHandler<ContactEventArgs> Closed;
        public event EventHandler<EventArgs> Error;

        private uint sessionId;

        public uint SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                sessionId = value;
            }
        }

        private Owner local;

        public Owner Local
        {
            get
            {
                return local;
            }

        }
        private Contact remote;

        public Contact Remote
        {
            get
            {
                return remote;
            }
        }



        public void Accept()
        {
        }

        public void Decline()
        {
        }

        public void Close()
        {
        }


        public void Dispose()
        {
        }
    }
};
