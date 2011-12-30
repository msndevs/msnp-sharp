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
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using Org.Mentalis.Network.ProxySocket;

namespace MSNPSharp.Core
{
    using MSNPSharp;

    public class ObjectEventArgs : EventArgs
    {
        private object _object;

        public ObjectEventArgs(object obj)
        {
            _object = obj;
        }

        public object Object
        {
            get
            {
                return _object;
            }
        }
    };

    public class ByteEventArgs : EventArgs
    {
        private byte[] bytes;

        public ByteEventArgs(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public byte[] Bytes
        {
            get
            {
                return bytes;
            }
        }
    };


    public abstract class SocketMessageProcessor : IMessageProcessor
    {
        #region Events

        public event EventHandler<EventArgs> ConnectionEstablished;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ExceptionEventArgs> ConnectingException;
        public event EventHandler<ExceptionEventArgs> ConnectionException;

        public event EventHandler<ObjectEventArgs> SendCompleted;
        public event EventHandler<ByteEventArgs> MessageReceived;

        #region Event triggers

        protected virtual void OnConnectingException(Exception e)
        {
            if (ConnectionException != null)
                ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a socket exception while retrieving data. See the inner exception for more information.", e)));
        }

        protected virtual void OnConnectionException(Exception e)
        {
            if (ConnectionException != null)
                ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a general exception while retrieving data. See the inner exception for more information.", e)));
        }

        protected virtual void OnSendCompleted(ObjectEventArgs e)
        {
            if (SendCompleted != null)
                SendCompleted(this, e);
        }

        protected virtual void OnMessageReceived(ByteEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived(this, e);
        }

        protected virtual void OnConnected()
        {
            hasFiredDisconnectEvent = false;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connected", GetType().Name);

            if (ConnectionEstablished != null)
                ConnectionEstablished(this, new EventArgs());
        }

        protected virtual void OnDisconnected()
        {
            if (hasFiredDisconnectEvent)
            {
                return;
            }
            else
            {
                hasFiredDisconnectEvent = true;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Disconnected", GetType().Name);

            if (ConnectionClosed != null)
                ConnectionClosed(this, new EventArgs());
        }

        #endregion

        #endregion

        #region Members

        protected ConnectivitySettings connectivitySettings = new ConnectivitySettings();
        private List<IMessageHandler> messageHandlers = new List<IMessageHandler>();
        private bool hasFiredDisconnectEvent = false;
        protected MessagePool messagePool = null;

        public SocketMessageProcessor(ConnectivitySettings connectivitySettings, MessagePool messagePool)
        {
            ConnectivitySettings = connectivitySettings;
            MessagePool = messagePool;
        }

        #endregion

        #region Properties

        public abstract bool Connected
        {
            get;
        }

        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return connectivitySettings;
            }

            set
            {
                if (Connected)
                {
                    string errorString = "Cannot set the ConnectivitySettings property of a connected " + GetType().ToString() + ".";
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, errorString);
                    throw new InvalidOperationException(errorString);
                }

                connectivitySettings = value;
            }
        }

        public List<IMessageHandler> MessageHandlers
        {
            get
            {
                return messageHandlers;
            }
        }

        protected MessagePool MessagePool
        {
            get
            {
                return messagePool;
            }
            set
            {
                messagePool = value;
            }
        }

        #endregion

        #region IMessageProcessor members

        public abstract void SendMessage(NetworkMessage message);

        public virtual void RegisterHandler(IMessageHandler handler)
        {
            if (handler != null && !messageHandlers.Contains(handler))
            {
                lock (messageHandlers)
                {
                    messageHandlers.Add(handler);
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                       handler.ToString() + " added to handler list.", GetType().Name);
                }
            }
        }

        public virtual void UnregisterHandler(IMessageHandler handler)
        {
            if (handler != null)
            {
                lock (messageHandlers)
                {
                    while (messageHandlers.Remove(handler))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            handler.ToString() + " removed from handler list.", GetType().Name);
                    }
                }
            }
        }

        #endregion

        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void SendSocketData(byte[] data);
        public abstract void SendSocketData(byte[] data, object userState);

        protected virtual void DispatchRawData(byte[] data)
        {
            // read the messages and dispatch to handlers
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data, 0, data.Length)))
            {
                messagePool.BufferData(reader);
            }

            while (messagePool.MessageAvailable)
            {
                // retrieve the message
                byte[] incomingMessage = messagePool.GetNextMessageData();
                // call the virtual method to perform polymorphism, descendant classes can take care of it
                OnMessageReceived(new ByteEventArgs(incomingMessage));
            }
        }
    }
};
