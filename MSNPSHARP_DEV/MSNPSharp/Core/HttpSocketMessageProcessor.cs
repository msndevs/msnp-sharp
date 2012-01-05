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
using System.Text;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MSNPSharp.Core
{
    using MSNPSharp;

    internal enum HttpPollAction
    {
        /// <summary>
        /// Send and receive data. We have data to send and we want to receive data immediately (no lifespan).
        /// </summary>
        None,
        /// <summary>
        /// Open connection to the name server.
        /// </summary>
        Open,
        /// <summary>
        /// We don't have data to send. Receive data only and wait until Lifespan elapsed for the available data.
        /// </summary>
        Poll
    };

    /// <summary>
    /// HTTP polling transport layer.
    /// Reference in http://www.hypothetic.org/docs/msn/sitev2.0/general/http_connections.php.
    /// </summary>
    public class HttpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        private HttpPollAction action = HttpPollAction.None;

        private volatile bool connected = false;
        private volatile bool isWebRequestInProcess = false; // We can't send another web request if this is true
        private WebRequest lastRequest = null; // To abort web request

        private bool opened = false; // first call to server has Action=open
        private bool verCommand = false;
        private bool cvrCommand = false;
        private bool usrCommand = false;
        private byte[] openCommand = null;

        private string sessionID;
        private string gatewayIP;
        private string host;
        private WebProxy webProxy;

        private Queue<QueueState> sendingQueue = new Queue<QueueState>();
        private System.Timers.Timer pollTimer = new System.Timers.Timer(2000);
        private object syncObject = new object();

        public HttpSocketMessageProcessor(ConnectivitySettings connectivitySettings, MessagePool messagePool)
            : base(connectivitySettings, messagePool)
        {
            gatewayIP = connectivitySettings.Host;
            host = gatewayIP;
            pollTimer.Elapsed += pollTimer_Elapsed;
            pollTimer.AutoReset = true;

            webProxy = connectivitySettings.WebProxy;
        }

        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
        }

        public override bool Connected
        {
            get
            {
                return connected;
            }
        }

        private string SessionID
        {
            get
            {
                return sessionID;
            }
            set
            {
                sessionID = value;
            }
        }

        private string GatewayIP
        {
            get
            {
                return gatewayIP;
            }
            set
            {
                gatewayIP = value;
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private void pollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            byte[] buffer = new byte[0];
            List<object> userStates = new List<object>();
            action = HttpPollAction.Poll;

            lock (syncObject)
            {
                while (sendingQueue.Count > 0)
                {
                    QueueState queueState = sendingQueue.Dequeue();

                    if (queueState.UserState is Array)
                    {
                        foreach (object us in queueState.UserState as object[])
                        {
                            if (!userStates.Contains(us))
                                userStates.Add(us);
                        }
                    }
                    else
                    {
                        if (!userStates.Contains(queueState.UserState))
                            userStates.Add(queueState.UserState);
                    }

                    buffer = NetworkMessage.AppendArray(buffer, queueState.Data);
                }

                if (buffer.Length > 0)
                    action = HttpPollAction.None;
            }

            Send(buffer, userStates.ToArray());
        }

        private string GenerateURI()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("http://");
            stringBuilder.Append(gatewayIP);
            stringBuilder.Append("/gateway/gateway.dll?");

            switch (action)
            {
                case HttpPollAction.Open:
                    stringBuilder.Append("Action=open&");
                    stringBuilder.Append("Server=NS&");
                    stringBuilder.Append("IP=" + ConnectivitySettings.Host);
                    break;
                case HttpPollAction.Poll:
                    stringBuilder.Append("Action=poll&Lifespan=3&");
                    stringBuilder.Append("SessionID=" + SessionID);
                    break;
                case HttpPollAction.None:
                    stringBuilder.Append("SessionID=" + SessionID);
                    break;
            }

            return stringBuilder.ToString();
        }

        public override void Connect()
        {
            // don't have to do anything here
            connected = true;
            OnConnected();
        }

        public override void Disconnect()
        {
            // nothing to do
            if (Connected)
            {
                connected = false;

                if (pollTimer.Enabled)
                    pollTimer.Stop();

                OnDisconnected();
            }

            sendingQueue = new Queue<QueueState>();
            opened = false;
            verCommand = false;
            cvrCommand = false;
            usrCommand = false;
            openCommand = null;

            if (isWebRequestInProcess && lastRequest != null)
            {
                try
                {
                    lastRequest.Abort();
                }
                catch (Exception)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "HTTP Request is ABORTED", GetType().Name);
                }
                lastRequest = null;
            }

            isWebRequestInProcess = false;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public override void SendMessage(NetworkMessage message)
        {
            Send(message.GetBytes());
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Send(byte[] data, object userState)
        {
            // connection has not been established yet; concat data to the end of OpenCommand
            if (!opened)
            {
                if (openCommand == null)
                {
                    openCommand = (byte[])data.Clone();
                }
                else
                {
                    byte[] NewOpenCommand = new byte[openCommand.Length + data.Length];
                    openCommand.CopyTo(NewOpenCommand, 0);
                    data.CopyTo(NewOpenCommand, openCommand.Length);
                    openCommand = NewOpenCommand;
                }

                if (data.Length > 4 && data[3] == ' ')
                {
                    if (data[0] == 'V' && data[1] == 'E' && data[2] == 'R')
                        verCommand = true;
                    else if (data[0] == 'C' && data[1] == 'V' && data[2] == 'R')
                        cvrCommand = true;
                    else if (data[0] == 'U' && data[1] == 'S' && data[2] == 'R')
                        usrCommand = true;
                }

                if (verCommand && cvrCommand && usrCommand)
                {
                    opened = true;
                    data = openCommand;
                    action = HttpPollAction.Open;
                }
                else
                {
                    return;
                }
            }

            lock (syncObject)
            {
                if (isWebRequestInProcess)
                {
                    sendingQueue.Enqueue(new QueueState(data, userState));
                    action = HttpPollAction.None;
                    return;
                }

                if (pollTimer.Enabled)
                    pollTimer.Stop();

                isWebRequestInProcess = true;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateURI());
                lastRequest = request;
                action = HttpPollAction.None;

                request.Timeout = 5000;
                request.Method = "POST";
                request.Accept = "*/*";
                request.AllowAutoRedirect = false;
                request.AllowWriteStreamBuffering = false;
                request.KeepAlive = true;
                request.ContentLength = data.Length;
                request.Headers.Add("Pragma", "no-cache");

                // Bypass msnp blockers
                request.ContentType = "text/html; charset=UTF-8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.7 (KHTML, like Gecko) Chrome/16.0.912.63 Safari/535.7";
                request.Headers.Add("X-Requested-Session-Content-Type", "text/html");

                request.ServicePoint.Expect100Continue = false;

                if (webProxy != null)
                {
                    request.Proxy = webProxy;
                }

                HttpState httpState = new HttpState(request, null, null, data, userState);
                request.BeginGetRequestStream(EndGetRequestStreamCallback, httpState);
            }
        }

        private void EndGetRequestStreamCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            HttpWebRequest request = (HttpWebRequest)httpState.Request;
            byte[] dataToSend = httpState.SendBuffer;
            try
            {
                httpState.Stream = request.EndGetRequestStream(ar);
                httpState.Stream.BeginWrite(dataToSend, 0, dataToSend.Length, RequestStreamEndWriteCallback, httpState);
            }
            catch (WebException we)
            {
                HandleWebException(we);
            }
        }

        private void RequestStreamEndWriteCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            HttpWebRequest request = (HttpWebRequest)httpState.Request;
            Stream stream = httpState.Stream;
            try
            {
                stream.EndWrite(ar);
                stream.Close();
                stream = null;
                httpState.Stream = null;

                request.BeginGetResponse(EndGetResponseCallback, httpState);
            }
            catch (WebException we)
            {
                HandleWebException(we);
            }
        }

        private void EndGetResponseCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            HttpWebRequest request = (HttpWebRequest)httpState.Request;

            lock (syncObject)
            {
                try
                {
                    httpState.Response = (HttpWebResponse)request.EndGetResponse(ar);
                    int responseLength = (int)httpState.Response.ContentLength;

                    foreach (string header in httpState.Response.Headers.AllKeys)
                    {
                        switch (header)
                        {
                            case "X-MSN-Host":
                                host = httpState.Response.Headers.Get(header);
                                break;

                            case "X-MSN-Messenger":
                                string text = httpState.Response.Headers.Get(header);

                                string[] parts = text.Split(';');
                                foreach (string part in parts)
                                {
                                    string[] elements = part.Split('=');
                                    switch (elements[0].Trim())
                                    {
                                        case "SessionID":
                                            SessionID = elements[1];
                                            break;
                                        case "GW-IP":
                                            GatewayIP = elements[1];
                                            break;
                                        case "Session":
                                            if ("close" == elements[1])
                                            {
                                                // Session is closed... OUT or SignoutFromHere() was sent.
                                                // We will receive 400 error if we send a new data.
                                                // So, fire event after all content read.
                                                connected = false;
                                            }
                                            break;
                                        case "Action":
                                            break;
                                    }
                                }
                                break;
                        }
                    }

                    httpState.Stream = httpState.Response.GetResponseStream();
                    httpState.ReceiveBuffer = new byte[responseLength];
                    httpState.ReceiveBufferOffset = 0;

                    httpState.Stream.BeginRead(httpState.ReceiveBuffer, httpState.ReceiveBufferOffset, httpState.ReceiveBuffer.Length - httpState.ReceiveBufferOffset, ResponseStreamEndReadCallback, httpState);
                }
                catch (WebException we)
                {
                    HandleWebException(we);
                }
            }
        }

        private void ResponseStreamEndReadCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            object userState = httpState.UserState;

            try
            {
                httpState.ReceiveBufferOffset += httpState.Stream.EndRead(ar);

                if (httpState.ReceiveBufferOffset < httpState.ReceiveBuffer.Length)
                {
                    httpState.Stream.BeginRead(httpState.ReceiveBuffer, httpState.ReceiveBufferOffset, httpState.ReceiveBuffer.Length - httpState.ReceiveBufferOffset, ResponseStreamEndReadCallback, httpState);
                    return;
                }
            }
            catch (WebException we)
            {
                HandleWebException(we);
            }
            catch (ObjectDisposedException ode)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    "HTTP Response Stream disposed: " + ode.StackTrace, GetType().Name);
            }
            finally
            {
                if (httpState.ReceiveBufferOffset == httpState.ReceiveBuffer.Length)
                {
                    isWebRequestInProcess = false;
                    lastRequest = null;
                    try
                    {
                        httpState.Stream.Close();
                        httpState.Response.Close();
                        httpState.Stream = null;
                        httpState.Response = null;
                    }
                    catch (Exception exc)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "HTTP Response Stream close error: " + exc.StackTrace, GetType().Name);
                    }
                }
            }

            OnAfterRawDataSent(userState);

            DispatchRawData(httpState.ReceiveBuffer);

            lock (syncObject)
            {
                if (connected && (!isWebRequestInProcess) && (!pollTimer.Enabled))
                {
                    pollTimer.Start();
                }
                else if (!connected)
                {
                    // All content is read. It is time to fire event if not connected..
                    OnDisconnected();
                }
            }
        }

        private void HandleWebException(WebException we)
        {
            switch (we.Status)
            {
                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.KeepAliveFailure:
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.ProtocolError:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.RequestCanceled:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.UnknownError:
                    {

                        OnDisconnected();

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "HTTP Error: " + we.ToString(), GetType().Name);

                        HttpWebResponse wr = (HttpWebResponse)we.Response;
                        if (wr != null)
                        {
                            try
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                                    "HTTP Status: " + ((int)wr.StatusCode) + " " + wr.StatusCode + " " + wr.StatusDescription, GetType().Name);
                            }
                            catch (Exception exp)
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                    "HTTP Error: " + exp.ToString(), GetType().Name);
                            }
                        }
                        break;
                    }
            }
        }

        private class HttpState
        {
            public WebRequest Request;
            public WebResponse Response;
            public Stream Stream;
            public byte[] SendBuffer;

            internal byte[] ReceiveBuffer;
            internal int ReceiveBufferOffset = 0;
            internal Object UserState;

            public HttpState(WebRequest request, WebResponse response,
                Stream stream, byte[] sendBuffer, object userState)
            {
                this.Request = request;
                this.Response = response;
                this.Stream = stream;
                this.SendBuffer = sendBuffer;
                this.ReceiveBuffer = new byte[0];
                this.UserState = userState;
            }
        }

        private class QueueState
        {
            public byte[] Data;
            public Object UserState;

            public QueueState(byte[] data, object userState)
            {
                this.Data = data;
                this.UserState = userState;
            }
        }
    }
};
