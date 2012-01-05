#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    public class NSMessageProcessor : IMessageProcessor
    {
        #region Events

        public event EventHandler<EventArgs> ConnectionEstablished;
        protected virtual void OnConnectionEstablished(object sender, EventArgs e)
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(sender, e);
        }

        public event EventHandler<EventArgs> ConnectionClosed;
        protected virtual void OnConnectionClosed(object sender, EventArgs e)
        {
            if (ConnectionClosed != null)
                ConnectionClosed(sender, e);
        }

        public event EventHandler<ExceptionEventArgs> ConnectingException;
        protected virtual void OnConnectingException(object sender, ExceptionEventArgs e)
        {
            if (ConnectingException != null)
                ConnectingException(sender, e);
        }

        public event EventHandler<ExceptionEventArgs> ConnectionException;
        protected virtual void OnConnectionException(object sender, ExceptionEventArgs e)
        {
            if (ConnectionException != null)
                ConnectionException(sender, e);
        }

        public event EventHandler<ObjectEventArgs> SendCompleted;
        protected virtual void OnSendCompleted(object sender, ObjectEventArgs e)
        {
            if (SendCompleted != null)
                SendCompleted(sender, e);
        }

        public event EventHandler<ExceptionEventArgs> HandlerException;
        protected virtual void OnHandlerException(object sender, ExceptionEventArgs e)
        {
            if (HandlerException != null)
                HandlerException(sender, e);
        }

        #endregion

        private int transactionID = 0;
        private SocketMessageProcessor processor;

        protected internal NSMessageProcessor(ConnectivitySettings connectivitySettings)
        {
            if (connectivitySettings.HttpPoll)
            {
                Processor = new HttpSocketMessageProcessor(connectivitySettings, new NSMessagePool());
            }
            else
            {
                Processor = new TcpSocketMessageProcessor(connectivitySettings, new NSMessagePool());
            }
        }

        public int TransactionID
        {
            get
            {
                return transactionID;
            }
        }

        public bool Connected
        {
            get
            {
                return (processor.Connected);
            }
        }

        public SocketMessageProcessor Processor
        {
            get
            {
                return processor;
            }
            set
            {
                if (processor != null)
                {
                    processor.ConnectionEstablished -= OnConnectionEstablished;
                    processor.ConnectionClosed -= OnConnectionClosed;
                    processor.ConnectingException -= OnConnectingException;
                    processor.ConnectionException -= OnConnectionException;
                    processor.SendCompleted -= OnSendCompleted;
                    processor.MessageReceived -= OnMessageReceived;
                }

                processor = value;

                if (processor != null)
                {
                    processor.ConnectionEstablished += OnConnectionEstablished;
                    processor.ConnectionClosed += OnConnectionClosed;
                    processor.ConnectingException += OnConnectingException;
                    processor.ConnectionException += OnConnectionException;
                    processor.SendCompleted += OnSendCompleted;
                    processor.MessageReceived += OnMessageReceived;
                }
            }
        }

        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return Processor.ConnectivitySettings;
            }
            set
            {
                Processor.ConnectivitySettings = value;
            }
        }

        /// <summary>
        /// Reset the transactionID to zero.
        /// </summary>
        internal void ResetTransactionID()
        {
            Interlocked.Exchange(ref transactionID, 0);
        }

        protected internal int IncreaseTransactionID()
        {
            return Interlocked.Increment(ref transactionID);
        }

        protected void OnMessageReceived(object sender, ByteEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming NS command...", GetType().Name);

            NSMessage message = new NSMessage();
            message.ParseBytes(e.Bytes);

            DispatchMessage(message);
        }

        public void SendMessage(NetworkMessage message)
        {
            SendMessage(message, IncreaseTransactionID());
        }

        public virtual void SendMessage(NetworkMessage message, int transactionID)
        {
            NSMessage nsMessage = message as NSMessage;

            if (nsMessage == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "Cannot use this Message Processor to send a " + message.GetType().ToString() + " message.", GetType().Name);
                return;
            }

            nsMessage.TransactionID = transactionID;
            nsMessage.PrepareMessage();

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "Outgoing message:\r\n" + nsMessage.ToDebugString() + "\r\n", GetType().Name);

            // convert to bytes and send it over the socket
            Processor.Send(nsMessage.GetBytes(), transactionID);
        }

        public void Connect()
        {
            Processor.Connect();
        }

        public void Disconnect()
        {
            if (Connected)
                SendMessage(new NSMessage("OUT", new string[] { }));

            // OUT disconnects
            // Processor.Disconnect();
        }

        public void RegisterHandler(IMessageHandler handler)
        {
            Processor.RegisterHandler(handler);
        }

        public void UnregisterHandler(IMessageHandler handler)
        {
            Processor.UnregisterHandler(handler);
        }

        protected virtual void DispatchMessage(NetworkMessage message)
        {
            // copy the messageHandlers array because the collection can be 
            // modified during the message handling. (Handlers are registered/unregistered)
            IMessageHandler[] handlers = Processor.MessageHandlers.ToArray();

            // now give the handlers the opportunity to handle the message
            foreach (IMessageHandler handler in handlers)
            {
                try
                {
                    //I think the person who first write this make a big mistake, C# is NOT C++,
                    //message class passes as reference, one change, all changed.
                    //Mabe we need to review all HandleMessage calling.
                    ICloneable imessageClone = message as ICloneable;
                    if (imessageClone != null)
                    {
                        handler.HandleMessage(this, imessageClone.Clone() as NSMessage);
                    }
                }
                catch (Exception e)
                {
                    OnHandlerException(this, new ExceptionEventArgs(new MSNPSharpException(
                        "An exception occured while handling a nameserver message. See inner exception for more details.", e)));
                }
            }
        }
    }
};
