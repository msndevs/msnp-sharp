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

    public class TcpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        #region Members

        private byte[] socketBuffer = new byte[8192];
        private IPEndPoint proxyEndPoint;
        private ProxySocket socket;

        public TcpSocketMessageProcessor(ConnectivitySettings connectivitySettings, MessagePool messagePool)
            : base(connectivitySettings, messagePool)
        {
        }

        ~TcpSocketMessageProcessor()
        {
            Dispose(false);
        }

        #endregion

        #region Properties

        public override bool Connected
        {
            get
            {
                if (socket == null)
                    return false;

                lock (socket)
                {
                    return IsSocketConnected(socket);
                }
            }
        }

        protected IPEndPoint ProxyEndPoint
        {
            get
            {
                return proxyEndPoint;
            }
            set
            {
                proxyEndPoint = value;
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return socket.LocalEndPoint;
            }
        }

        public virtual EndPoint RemoteEndPoint
        {
            get
            {
                return socket.RemoteEndPoint;
            }
        }

        #endregion

        #region Methods

        public virtual ProxySocket GetPreparedSocket(IPAddress address, int port)
        {
            //Creates the Socket for sending data over TCP.
            ProxySocket socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // incorporate the connection settings like proxy's						
            // Note: ProxyType is in MSNPSharp namespace, ProxyTypes in ProxySocket namespace.
            if (ConnectivitySettings.ProxyType != ProxyType.None)
            {
                // set the proxy type
                switch (ConnectivitySettings.ProxyType)
                {
                    case ProxyType.Socks4:
                        socket.ProxyType = ProxyTypes.Socks4;
                        break;

                    case ProxyType.Socks5:
                        socket.ProxyType = ProxyTypes.Socks5;
                        break;

                    case ProxyType.Http:
                        socket.ProxyType = ProxyTypes.Http;
                        break;

                    case ProxyType.None:
                        socket.ProxyType = ProxyTypes.None;
                        break;
                }

                socket.ProxyUser = ConnectivitySettings.ProxyUsername;
                socket.ProxyPass = ConnectivitySettings.ProxyPassword;

                // resolve the proxy host
                if (proxyEndPoint == null)
                {
                    bool worked = false;
                    int retries = 0;
                    Exception exp = null;

                    //we retry a few times, because dns resolve failure is quite common
                    do
                    {
                        try
                        {
                            System.Net.IPAddress ipAddress = Network.DnsResolve(ConnectivitySettings.ProxyHost);

                            // assign to the connection object so other sockets can make use of it quickly
                            proxyEndPoint = new IPEndPoint(ipAddress, ConnectivitySettings.ProxyPort);

                            worked = true;
                        }
                        catch (Exception e)
                        {
                            retries++;
                            exp = e;
                        }
                    } while (!worked && retries < 3);

                    if (!worked)
                        throw new ConnectivityException("DNS Resolve for the proxy server failed: " + ConnectivitySettings.ProxyHost + " failed.", exp);
                }

                socket.ProxyEndPoint = proxyEndPoint;
            }
            else
                socket.ProxyType = ProxyTypes.None;

            //Send operations will timeout of confirmation is not received within 3000 milliseconds.
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 3000);

            //Socket will linger for 2 seconds after close is called.
            LingerOption lingerOption = new LingerOption(true, 2);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);

            try
            {
                socket.Bind(new IPEndPoint(address, port));
            }
            catch (SocketException ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "An error occured while trying to bind to a local address, error code: " + ex.ErrorCode + ".");
            }

            return socket;
        }

        /// <summary>
        /// Show whether the socket is connected at a certain moment.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns>true if socket is connected, false if socket is disconnected.</returns>
        public static bool IsSocketConnected(Socket socket)
        {
            bool returnValue = false;

            if (socket != null)
            {
                // Socket.Connected doesn't tell us if the socket is actually connected...
                // http://msdn2.microsoft.com/en-us/library/system.net.sockets.socket.connected.aspx

                bool disposed = false;
                bool blocking = socket.Blocking;

                try
                {
                    socket.Blocking = false;

                    int pollWait = 1;

                    if (socket.Poll(pollWait, SelectMode.SelectRead) && socket.Available == 0)
                    {
                        returnValue = false;
                    }
                    else
                    {
                        returnValue = true;
                    }

                }
                catch (SocketException ex)
                {
                    // 10035 == WSAEWOULDBLOCK
                    if (ex.NativeErrorCode.Equals(10035))
                        returnValue = true;
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                    returnValue = false;
                }
                finally
                {
                    if (!disposed)
                    {
                        socket.Blocking = blocking;
                    }
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Connect to the target through ConnectivitySettins.
        /// </summary>
        /// <exception cref="InvalidOperationException">Socket already connected.</exception>
        public override void Connect()
        {
            if (socket != null && Connected)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Connect() called, but already a socket available.", GetType().Name);

                // If you have to fail, fail noisily and as soon as possible.
                throw new InvalidOperationException("Socket already connected.");
            }

            try
            {
                // Create a socket
                socket = GetPreparedSocket((ConnectivitySettings.LocalHost == string.Empty) ? IPAddress.Any : IPAddress.Parse(ConnectivitySettings.LocalHost), ConnectivitySettings.LocalPort);

                IPAddress hostIP = null;

                if (ConnectivitySettings.EndPoints != null && ConnectivitySettings.EndPoints.Length > 0)
                {
                    int port = ConnectivitySettings.EndPoints[0].Port;
                    IPAddress[] addresses = new IPAddress[ConnectivitySettings.EndPoints.Length];
                    for (int i = 0; i < addresses.Length; i++)
                    {
                        addresses[i] = ConnectivitySettings.EndPoints[i].Address;
                    }

                    ((ProxySocket)socket).BeginConnect(addresses, port, new AsyncCallback(EndConnectCallback), socket);
                }
                else if (IPAddress.TryParse(ConnectivitySettings.Host, out hostIP))
                {
                    // start connecting				
                    ((ProxySocket)socket).BeginConnect(new System.Net.IPEndPoint(IPAddress.Parse(ConnectivitySettings.Host), ConnectivitySettings.Port), new AsyncCallback(EndConnectCallback), socket);
                }
                else
                {
                    ((ProxySocket)socket).BeginConnect(ConnectivitySettings.Host, ConnectivitySettings.Port, new AsyncCallback(EndConnectCallback), socket);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Connecting exception: " + e.ToString(), GetType().Name);
                OnConnectingException(e);

                // re-throw the exception since the exception is thrown while in a blocking call
                throw; //RethrowToPreserveStackDetails (without e)
            }
        }

        protected virtual void EndConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (socket == null)
                {
                    OnDisconnected();
                    return;
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "End Connect Callback", GetType().Name);

                ((ProxySocket)socket).EndConnect(ar);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "End Connect Callback Daarna", GetType().Name);

                OnConnected();

                // Begin receiving data
                BeginDataReceive(socket);
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "** EndConnectCallback exception **" + e.ToString(), GetType().Name);
                OnConnectingException(e);
            }
        }

        public virtual void BeginDataReceive(Socket socket)
        {
            try
            {
                socketBuffer = new byte[socketBuffer.Length];
                socket.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), socket);
            }
            catch (ObjectDisposedException)
            {
                OnDisconnected();
            }
        }

        protected virtual void EndReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Debug.Assert(MessagePool != null, "Field messagepool must be defined in derived class of SocketMessageProcessor.");

                Socket socket = (Socket)ar.AsyncState;
                int count = socket.EndReceive(ar);

                if (count == 0)
                {
                    // No data is received. We are disconnected.
                    OnDisconnected();
                    return;
                }

                byte[] buffer = new byte[count];
                Array.Copy(socketBuffer, 0, buffer, 0, count);
                DispatchRawData(buffer);

                // start a new read				
                BeginDataReceive(socket);
            }
            catch (SocketException e)
            {
                // close the socket upon a exception
                if (socket != null && Connected)
                    socket.Close();

                OnDisconnected();
                OnConnectingException(e);
            }
            catch (ObjectDisposedException)
            {
                // the connection is closed
                OnDisconnected();
            }
            catch (Exception e)
            {
                // close the socket upon a exception
                if (socket != null && Connected)
                    socket.Close();

                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString() + "\r\n" + e.StackTrace + "\r\n", GetType().Name);

                OnDisconnected();
                OnConnectionException(e);
            }
        }

        public override void SendMessage(NetworkMessage message)
        {
            Send(message.GetBytes());
        }

        public override void Send(byte[] data, object userState)
        {
            if (socket == null || !Connected)
            {
                // the connection is closed
                OnDisconnected();
                return;
            }

            SendSocketData(socket, data, userState);
        }

        public void SendSocketData(Socket psocket, byte[] data, object userState)
        {
            try
            {
                if (psocket != null && IsSocketConnected(psocket))
                {
                    lock (psocket)
                    {
                        SocketSendState state = new SocketSendState(psocket, userState);
                        psocket.BeginSend(data, 0, data.Length, SocketFlags.None, EndSendCallback, state);
                    }
                }
                else
                {
                    OnDisconnected();
                }
            }
            catch (SocketException sex)
            {
                if (sex.NativeErrorCode != 10035)  //10035: WSAEWOULDBLOCK
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error while sending network message. Error message: " + sex.Message);
                    OnDisconnected();
                }

                return;
            }
            catch (ObjectDisposedException)
            {
                // the connection is closed
                OnDisconnected();
            }
            catch (Exception e)
            {
                throw new MSNPSharpException("Error while sending network message. See the inner exception for more details.", e);
            }
        }

        protected virtual void EndSendCallback(IAsyncResult ar)
        {
            SocketSendState state = (SocketSendState)ar.AsyncState;
            Socket socket = state.Socket;
            try
            {
                socket.EndSend(ar);

                if (state.UserData != null)
                {
                    OnSendCompleted(new ObjectEventArgs(state.UserData));
                }
            }
            catch (SocketException sex)
            {
                if (sex.NativeErrorCode != 10035)  //10035: WSAEWOULDBLOCK
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error while sending network message. Error message: " + sex.Message);
                    OnDisconnected();
                }
            }
            catch (ObjectDisposedException)
            {
                // the connection is closed
                OnDisconnected();
            }
            catch (Exception e)
            {
                throw new MSNPSharpException("Error while sending network message. See the inner exception for more details.", e);
            }
        }

        public override void Disconnect()
        {
            // clean up the socket properly
            if (socket != null)
            {
                try
                {
                    if (Connected)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Close();
                }

                socket = null;

                // We don't need to call OnDisconnect here since EndReceiveCallback will be call automatically later on. (This is not valid if disconnected remotelly)
                // We need to call OnDisconnect after EndReceiveCallback if disconnected locally.
            }
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                Disconnect();
            }

            // Free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private class SocketSendState
        {
            public Socket Socket;
            public object UserData;

            public SocketSendState(Socket socket, object userData)
            {
                this.Socket = socket;
                this.UserData = userData;
            }
        }
    }
};
