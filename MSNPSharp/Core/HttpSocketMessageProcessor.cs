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
using System.Collections;
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

    internal enum HttpWebRequestStatus
    {
        None,
        GeneratingUri,
        WebRequestCreated,
        BeginGetRequestStream,
        EndGetRequestStream,
        BeginRequestStreamWrite,
        EndRequestStreamWrite,
        BeginGetResponse,
        EndGetResponse,
        GetResponseStream,
        BeginResponseStreamRead,
        EndResponseStreamRead
    }

    /// <summary>
    /// HTTP polling transport layer.
    /// Reference in http://www.hypothetic.org/docs/msn/sitev2.0/general/http_connections.php.
    /// </summary>
    public class HttpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        public const int MaxAllowedPacket = Int16.MaxValue;

        private HttpPollAction action = HttpPollAction.None;

        private volatile bool connected;
        private bool useLifespan; // Don't set lifespan until SignedIn
        // We can send another web request if it is NONE
        private volatile HttpWebRequestStatus httpWebRequestStatus = HttpWebRequestStatus.None;

        private bool opened; // first call to server has Action=open
        private byte[] openCommand = new byte[0];
        private BitArray openState = new BitArray(3); // VER,CVR,USR

        private string sessionID;
        private string gatewayIP;
        private string host;

        private Queue<QueueState> sendingQueue = new Queue<QueueState>();
        private System.Timers.Timer pollTimer = new System.Timers.Timer(2000);
        private object syncObject;

        public HttpSocketMessageProcessor(ConnectivitySettings connectivitySettings, MessagePool messagePool)
            : base(connectivitySettings, messagePool)
        {
            gatewayIP = connectivitySettings.Host;
            host = gatewayIP;
            pollTimer.Elapsed += pollTimer_Elapsed;
            pollTimer.AutoReset = true;
        }


        public override bool Connected
        {
            get
            {
                return connected;
            }
        }

        internal bool UseLifespan
        {
            get
            {
                return useLifespan;
            }
            set
            {
                useLifespan = value;
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

        private object SyncObject
        {
            get
            {
                if (syncObject == null)
                {
                    Interlocked.CompareExchange(ref syncObject, new object(), null);
                }

                return syncObject;
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private void pollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            byte[] buffer = new byte[0];
            List<object> userStates = new List<object>();

            lock (SyncObject)
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

                    if (buffer.Length > MaxAllowedPacket)
                        break;
                }

                action = (buffer.Length > 0) ? HttpPollAction.None : HttpPollAction.Poll;
            }

            Send(buffer, userStates.ToArray());
        }

        private string GenerateURI(HttpPollAction pollAction)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("http://");
            stringBuilder.Append(gatewayIP);
            stringBuilder.Append("/gateway/gateway.dll?");

            switch (pollAction)
            {
                case HttpPollAction.Open:
                    stringBuilder.Append("Action=open&");
                    stringBuilder.Append("Server=NS&");
                    stringBuilder.Append("IP=" + ConnectivitySettings.Host);
                    break;

                case HttpPollAction.Poll:
                    stringBuilder.Append("Action=poll&");
                    if (useLifespan)
                    {
                        stringBuilder.Append("Lifespan=3&");
                    }
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

        private byte[] Open(byte[] data)
        {
            if (data.Length > 4 && data[3] == ' ')
            {
                // Concat data to the end of OpenCommand
                openCommand = NetworkMessage.AppendArray(openCommand, data);

                // Be fast...
                switch ((char)data[0])
                {
                    case 'V':
                        openState[0] = (data[1] == 'E' && data[2] == 'R');
                        break;

                    case 'C':
                        openState[1] = (data[1] == 'V' && data[2] == 'R');
                        break;

                    case 'U':
                        openState[2] = (data[1] == 'S' && data[2] == 'R');
                        break;
                }
            }

            if (openState.Get(0) && openState.Get(1) && openState.Get(2))
            {
                opened = true;
                action = HttpPollAction.Open;
                return openCommand;
            }

            return null; // Connection has not been established yet
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

            httpWebRequestStatus = HttpWebRequestStatus.None;
            sendingQueue = new Queue<QueueState>();
            openCommand = new byte[0];
            openState.SetAll(false);
            opened = false;
            useLifespan = false;
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
        public override void Send(byte[] outgoingData, object userState)
        {
            if (opened || (null != (outgoingData = Open(outgoingData))))
            {
                if (httpWebRequestStatus != HttpWebRequestStatus.None)
                {
                    lock (SyncObject)
                    {
                        sendingQueue.Enqueue(new QueueState(outgoingData, userState));
                        action = HttpPollAction.None;
                    }
                }
                else
                {
                    httpWebRequestStatus = HttpWebRequestStatus.GeneratingUri;
                    string uri = GenerateURI(action);

                    lock (SyncObject)
                    {
                        action = HttpPollAction.None;

                        if (pollTimer.Enabled)
                        {
                            pollTimer.Stop();
                        }
                    }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                    httpWebRequestStatus = HttpWebRequestStatus.WebRequestCreated;
                    ConnectivitySettings.SetupWebRequest(request);
                    request.Accept = "*/*";
                    request.Method = "POST";
                    request.Timeout = 10000;
                    request.KeepAlive = true;
                    request.AllowAutoRedirect = false;
                    request.AllowWriteStreamBuffering = false;
                    request.ContentLength = outgoingData.Length;
                    request.Headers.Add("Pragma", "no-cache");

                    // Bypass msnp blockers
                    request.ContentType = "text/html; charset=UTF-8";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.7 (KHTML, like Gecko) Chrome/16.0.912.63 Safari/535.7";
                    request.Headers.Add("X-Requested-Session-Content-Type", "text/html");

                    // Enable GZIP
                    request.AutomaticDecompression = Settings.EnableGzipCompressionForWebServices ? DecompressionMethods.GZip : DecompressionMethods.None;

                    HttpState httpState = new HttpState(request, outgoingData, userState, null, null);
                    httpWebRequestStatus = HttpWebRequestStatus.BeginGetRequestStream;
                    request.BeginGetRequestStream(EndGetRequestStreamCallback, httpState);
                }
            }
        }

        private void EndGetRequestStreamCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            try
            {
                httpWebRequestStatus = HttpWebRequestStatus.EndGetRequestStream;
                httpState.Stream = httpState.Request.EndGetRequestStream(ar);

                httpWebRequestStatus = HttpWebRequestStatus.BeginRequestStreamWrite;
                httpState.Stream.BeginWrite(httpState.OutgoingData, 0, httpState.OutgoingData.Length, RequestStreamEndWriteCallback, httpState);
            }
            catch (WebException we)
            {
                HandleWebException(we, httpState);
            }
        }

        private void RequestStreamEndWriteCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            try
            {
                httpWebRequestStatus = HttpWebRequestStatus.EndRequestStreamWrite;
                httpState.Stream.EndWrite(ar);
                httpState.Stream.Close();
                httpState.Stream = null;
                // We must re-send the data when connection error is not fatal.
                // So, don't set httpState.OutgoingData = null;

                httpWebRequestStatus = HttpWebRequestStatus.BeginGetResponse;
                httpState.Request.BeginGetResponse(EndGetResponseCallback, httpState);
            }
            catch (WebException we)
            {
                HandleWebException(we, httpState);
            }
        }

        private void EndGetResponseCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            try
            {
                httpWebRequestStatus = HttpWebRequestStatus.EndGetResponse;
                httpState.Response = httpState.Request.EndGetResponse(ar);
                httpState.Request = null;

                lock (SyncObject)
                {
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
                }

                httpState.OutgoingData = null; // Response is OK and the our data was sent successfuly.
                httpWebRequestStatus = HttpWebRequestStatus.GetResponseStream;
                httpState.Stream = httpState.Response.GetResponseStream();
                httpState.IncomingBuffer = new byte[8192];
                httpState.ResponseReadStream = new MemoryStream();

                httpWebRequestStatus = HttpWebRequestStatus.BeginResponseStreamRead;
                httpState.Stream.BeginRead(httpState.IncomingBuffer, 0, httpState.IncomingBuffer.Length, ResponseStreamEndReadCallback, httpState);
            }
            catch (WebException we)
            {
                HandleWebException(we, httpState);
            }
        }

        private void ResponseStreamEndReadCallback(IAsyncResult ar)
        {
            HttpWebRequestStatus httpWebRequestStatusForDebug = httpWebRequestStatus;
            bool dataIsSentButKeepAliveErrorOccured = false;
            HttpState httpState = (HttpState)ar.AsyncState;
            int read = 0;
            try
            {
                httpWebRequestStatus = HttpWebRequestStatus.EndResponseStreamRead;
                httpWebRequestStatusForDebug = httpWebRequestStatus;
                read = httpState.Stream.EndRead(ar);

                if (read > 0)
                {
                    httpState.ResponseReadStream.Write(httpState.IncomingBuffer, 0, read);

                    httpWebRequestStatus = HttpWebRequestStatus.BeginResponseStreamRead;
                    httpWebRequestStatusForDebug = httpWebRequestStatus;
                    httpState.Stream.BeginRead(httpState.IncomingBuffer, 0, httpState.IncomingBuffer.Length, ResponseStreamEndReadCallback, httpState);
                    return;
                }
            }
            catch (WebException we)
            {
                dataIsSentButKeepAliveErrorOccured = HandleWebException(we, httpState);
            }
            catch (ObjectDisposedException ode)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    "HTTP Response Stream disposed: " + ode.StackTrace, GetType().Name);
            }
            finally
            {
                if (read == 0)
                {
                    httpWebRequestStatus = HttpWebRequestStatus.None;
                    try
                    {
                        httpState.Stream.Close();
                        httpState.Response.Close();
                    }
                    catch (Exception exc)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "HTTP Response Stream close error: " + exc.StackTrace, GetType().Name);
                    }
                    finally
                    {
                        httpState.Stream = null;
                        httpState.Response = null;
                    }
                }
            }

            OnAfterRawDataSent(httpState.UserState);

            if (httpState.ResponseReadStream != null)
            {
                // Don't catch exceptions here.
                httpState.ResponseReadStream.Flush();
                byte[] rawData = httpState.ResponseReadStream.ToArray();
                DispatchRawData(rawData);
                httpState.ResponseReadStream.Close();
                httpState.ResponseReadStream = null;
            }

            lock (SyncObject)
            {
                if (connected && (httpWebRequestStatus == HttpWebRequestStatus.None) && (!pollTimer.Enabled))
                {
                    pollTimer.Start();
                }
                else if (!connected)
                {
                    // All content is read. It is time to fire event if not connected..
                    OnDisconnected();
                }
            }

            if (dataIsSentButKeepAliveErrorOccured)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    "Outgoing data was sent but an error occured while reading response data = " + httpWebRequestStatusForDebug, GetType().Name);
            }
        }

        private bool HandleWebException(WebException we, HttpState httpState)
        {
            switch (we.Status)
            {
                case WebExceptionStatus.KeepAliveFailure:
                    {
                        // When this happens, re-send the last packet.
                        httpWebRequestStatus = HttpWebRequestStatus.None;

                        if (httpState.OutgoingData != null)
                        {
                            Send(httpState.OutgoingData, httpState.UserState);
                        }
                        else
                        {
                            // It seems outgoing data was sent, but an error occured while reading http response.
                            return true;
                        }
                    }
                    break;

                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                    {
                        OnConnectingException(we);
                        goto default;
                    }


                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.PipelineFailure:
                case WebExceptionStatus.SecureChannelFailure:
                    {
                        OnConnectionException(we);
                        goto default;
                    }

                case WebExceptionStatus.SendFailure:
                case WebExceptionStatus.ProtocolError:
                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.RequestCanceled:
                case WebExceptionStatus.Timeout:
                case WebExceptionStatus.UnknownError:
                default:
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

            return false;
        }

        private class HttpState
        {
            internal byte[] OutgoingData;
            internal WebRequest Request;
            internal Object UserState;

            internal byte[] IncomingBuffer;
            internal WebResponse Response;
            internal Stream Stream;
            internal MemoryStream ResponseReadStream;

            internal HttpState(WebRequest request, byte[] outgoingData, object userState,
                WebResponse response, Stream stream)
            {
                this.Request = request;
                this.OutgoingData = outgoingData;
                this.UserState = userState;

                this.Response = response;
                this.Stream = stream;
            }
        }

        private class QueueState
        {
            internal byte[] Data;
            internal Object UserState;

            internal QueueState(byte[] data, object userState)
            {
                this.Data = data;
                this.UserState = userState;
            }
        }
    }
};
