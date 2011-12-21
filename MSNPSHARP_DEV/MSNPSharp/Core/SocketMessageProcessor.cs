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

    public abstract class SocketMessageProcessor : IMessageProcessor
    {
        protected ConnectivitySettings connectivitySettings = new ConnectivitySettings();
        protected MessagePool messagePool = null;
        private bool hasFiredDisconnectEvent = false;
        private List<IMessageHandler> messageHandlers = new List<IMessageHandler>();

        public delegate void MessageReceiver(byte[] data);
        protected MessageReceiver messageReceiver;

        #region events

        public event EventHandler<EventArgs> ConnectionEstablished;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ExceptionEventArgs> ConnectingException;
        public event EventHandler<ExceptionEventArgs> ConnectionException;

        public event EventHandler<ObjectEventArgs> SendCompleted;

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

        #endregion

        public SocketMessageProcessor(ConnectivitySettings connectivitySettings, 
            MessageReceiver messageReceiver,
            MessagePool messagePool)
        {
            ConnectivitySettings = connectivitySettings;
            this.messageReceiver = messageReceiver;
            MessagePool = messagePool;
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

        public abstract void SendSocketData(byte[] data);
        public abstract void SendSocketData(byte[] data, object userState);

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

        public abstract bool Connected
        {
            get;
        }

        public List<IMessageHandler> MessageHandlers
        {
            get
            {
                return messageHandlers;
            }
        }

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

        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void SendMessage(NetworkMessage message);

    }
};
