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

    /// <summary>
    /// HTTP polling transport layer.
    /// Reference in http://www.hypothetic.org/docs/msn/sitev2.0/general/http_connections.php.
    /// </summary>
    public class HttpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        public const int MaxAllowedPacket = Int16.MaxValue;

        private HttpPollAction action = HttpPollAction.None;

        private volatile bool connected;
        private volatile bool isWebRequestInProcess; // We can't send another web request if this is true
        private bool useLifespan; // Don't set lifespan until SignedIn

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

            isWebRequestInProcess = false;
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
                if (isWebRequestInProcess)
                {
                    lock (SyncObject)
                    {
                        sendingQueue.Enqueue(new QueueState(outgoingData, userState));
                        action = HttpPollAction.None;
                    }
                }
                else
                {
                    isWebRequestInProcess = true;
                    HttpPollAction oldAction = action;

                    lock (SyncObject)
                    {
                        action = HttpPollAction.None;

                        if (pollTimer.Enabled)
                        {
                            pollTimer.Stop();
                        }
                    }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateURI(oldAction));
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

                    // Don't block current thread.
                    HttpState httpState = new HttpState(request, outgoingData, userState, oldAction);
                    request.BeginGetRequestStream(EndGetRequestStreamCallback, httpState);
                }
            }
        }

        private void EndGetRequestStreamCallback(IAsyncResult ar)
        {
            HttpState httpState = (HttpState)ar.AsyncState;
            try
            {
                using (Stream stream = httpState.Request.EndGetRequestStream(ar))
                {
                    stream.Write(httpState.OutgoingData, 0, httpState.OutgoingData.Length);
                    // We must re-send the data when connection error is not fatal.
                    // So, don't set here: httpState.OutgoingData = null;
                }

                WebResponse response = httpState.Request.GetResponse();
                httpState.Request = null;

                #region Read Headers

                lock (SyncObject)
                {
                    foreach (string header in response.Headers.AllKeys)
                    {
                        switch (header)
                        {
                            case "X-MSN-Host":
                                host = response.Headers.Get(header);
                                break;

                            case "X-MSN-Messenger":
                                string text = response.Headers.Get(header);

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

                #endregion

                MemoryStream responseReadStream = new MemoryStream();
                int read = 0;
                try
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        byte[] readBuffer = new byte[8192];
                        do
                        {
                            read = responseStream.Read(readBuffer, 0, readBuffer.Length);
                            if (read > 0)
                            {
                                responseReadStream.Write(readBuffer, 0, read);
                            }
                        }
                        while (read > 0);

                        // We read all incoming data and no error occured.
                        // Now, it is time to set null to not resend the data.
                        httpState.OutgoingData = null;

                        response.Close();
                        response = null;
                    }
                }
                catch (IOException ioe)
                {
                    if (ioe.InnerException is WebException)
                        throw ioe.InnerException;

                    throw ioe;
                }
                finally
                {
                    isWebRequestInProcess = false;
                }

                OnAfterRawDataSent(httpState.UserState);

                // Don't catch exceptions here.
                responseReadStream.Flush();
                byte[] rawData = responseReadStream.ToArray();
                DispatchRawData(rawData);
                responseReadStream.Close();
                responseReadStream = null;
            }
            catch (WebException we)
            {
                HandleWebException(we, httpState);
            }
            finally
            {
                lock (SyncObject)
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
        }

        private void HandleWebException(WebException we, HttpState httpState)
        {
            HttpStatusCode statusCode = GetErrorCode(we.Response as HttpWebResponse);

            // Not has an INTERNET connection (resolve error),
            // or the net cable was unplugged after http request was sent (connect failure)
            // This generates Disconnect event and stops.
            if (we.Status == WebExceptionStatus.NameResolutionFailure ||
                we.Status == WebExceptionStatus.ProxyNameResolutionFailure ||
                we.Status == WebExceptionStatus.ConnectFailure)
            {
                isWebRequestInProcess = false;
                Disconnect();

                OnConnectingException(we);
                return;
            }

            // If the session corrupted (BadRequest), 
            // or the first request (Open) is timed out then reconnect: We passed resolve/connect failures.
            // IMPORTANT: Don't send httpState.OutgoingData again...
            if (statusCode == HttpStatusCode.BadRequest ||
                (we.Status == WebExceptionStatus.Timeout && httpState.PollAction == HttpPollAction.Open))
            {
                Disconnect();
                Connect();
                return;
            }

            switch (we.Status)
            {
                case WebExceptionStatus.KeepAliveFailure:
                case WebExceptionStatus.ProtocolError:
                case WebExceptionStatus.Timeout:
                    {
                        // When this happened, re-send the last packet.
                        isWebRequestInProcess = false;

                        if (httpState.OutgoingData != null && httpState.OutgoingData.Length > 0)
                        {
                            Send(httpState.OutgoingData, httpState.UserState);
                        }
                        else
                        {
                            // It seems outgoing data was sent, but an error occured while reading http response.
                            return;
                        }
                    }
                    break;


                case WebExceptionStatus.ConnectFailure:
                case WebExceptionStatus.ConnectionClosed:
                case WebExceptionStatus.PipelineFailure:
                case WebExceptionStatus.SecureChannelFailure:
                    {
                        OnConnectionException(we);
                        goto default;
                    }

                case WebExceptionStatus.SendFailure:

                case WebExceptionStatus.ReceiveFailure:
                case WebExceptionStatus.RequestCanceled:

                case WebExceptionStatus.UnknownError:
                default:
                    {

                        OnDisconnected();

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "HTTP Error: " + we.ToString(), GetType().Name);


                        break;
                    }
            }
        }

        private HttpStatusCode GetErrorCode(HttpWebResponse wr)
        {
            HttpStatusCode ret = HttpStatusCode.OK;
            if (wr != null)
            {
                try
                {
                    ret = wr.StatusCode;
                }
                catch (Exception exp)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "HTTP Error: " + exp.ToString(), GetType().Name);
                }
            }
            return ret;
        }

        private class HttpState
        {
            internal WebRequest Request;
            internal byte[] OutgoingData;
            internal Object UserState;
            internal HttpPollAction PollAction;

            internal HttpState(WebRequest request, byte[] outgoingData, object userState, HttpPollAction pollAction)
            {
                this.Request = request;
                this.OutgoingData = outgoingData;
                this.UserState = userState;
                this.PollAction = pollAction;
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
