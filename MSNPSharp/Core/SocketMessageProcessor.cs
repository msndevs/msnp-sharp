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


    public abstract class SocketMessageProcessor : IDisposable
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
            if (ConnectingException != null)
                ConnectingException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a socket exception while retrieving data. See the inner exception for more information.", e)));
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
                ConnectionEstablished(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            if (!hasFiredDisconnectEvent)
            {
                hasFiredDisconnectEvent = true;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Disconnected", GetType().Name);

                if (ConnectionClosed != null)
                    ConnectionClosed(this, EventArgs.Empty);
            }
        }

        #endregion

        #endregion

        #region Members

        private ConnectivitySettings connectivitySettings;
        private bool hasFiredDisconnectEvent;
        private MessagePool messagePool;

        protected SocketMessageProcessor(ConnectivitySettings connectivitySettings, MessagePool messagePool)
        {
            ConnectivitySettings = connectivitySettings;
            MessagePool = messagePool;
        }

        ~SocketMessageProcessor()
        {
            Dispose(false);
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

        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Send(byte[] data, object userState);
        public virtual void Send(byte[] data)
        {
            Send(data, null);
        }

        public virtual void SendMessage(NetworkMessage message)
        {
            Send(message.GetBytes());
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                Disconnect();
            }

            // Free native resources
        }

        protected virtual void DispatchRawData(byte[] data)
        {
            // Read the messages,
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data, 0, data.Length)))
            {
                messagePool.BufferData(reader);
            }

            // and dispatch to handlers
            while (messagePool.MessageAvailable)
            {
                OnMessageReceived(new ByteEventArgs(messagePool.GetNextMessageData()));
            }
        }

        protected virtual void OnAfterRawDataSent(object userState)
        {
            if (userState != null)
            {
                if (userState is Array)
                {
                    foreach (object us in userState as object[])
                    {
                        OnSendCompleted(new ObjectEventArgs(us));
                    }
                }
                else
                {
                    OnSendCompleted(new ObjectEventArgs(userState));
                }
            }
        }
    }
};
