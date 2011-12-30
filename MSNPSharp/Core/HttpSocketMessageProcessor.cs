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
        private volatile bool sending = false;

        private bool opened = false; // first call to server has Action=open
        private bool verCommand = false;
        private bool cvrCommand = false;
        private bool usrCommand = false;
        private byte[] openCommand = null;

        private string sessionID;
        private string gatewayIP;
        private string host;
        private WebProxy webProxy;

        private Queue<byte[]> sendingQueue = new Queue<byte[]>();
        private System.Timers.Timer pollTimer = new System.Timers.Timer(2000);
        private object _lock = new object();

        public HttpSocketMessageProcessor(ConnectivitySettings connectivitySettings,
            MessageReceiver messageReceiver, MessagePool messagePool)
            : base(connectivitySettings, messageReceiver, messagePool)
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
            action = HttpPollAction.Poll;

            lock (_lock)
            {
                while (sendingQueue.Count > 0)
                    buffer = NetworkMessage.AppendArray(buffer, sendingQueue.Dequeue());

                if (buffer.Length > 0)
                    action = HttpPollAction.None;
            }

            SendSocketData(buffer);
        }

        private string GenerateURI()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("http://");
            sb.Append(gatewayIP);
            sb.Append("/gateway/gateway.dll?");

            switch (action)
            {
                case HttpPollAction.Open:
                    sb.Append("Action=open&");
                    sb.Append("Server=NS&");
                    sb.Append("IP=" + ConnectivitySettings.Host);
                    break;
                case HttpPollAction.Poll:
                    sb.Append("Action=poll&Lifespan=3&");
                    sb.Append("SessionID=" + SessionID);
                    break;
                case HttpPollAction.None:
                    sb.Append("SessionID=" + SessionID);
                    break;
            }

            return sb.ToString();
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

            sendingQueue = new Queue<byte[]>();
            opened = false;
            verCommand = false;
            cvrCommand = false;
            usrCommand = false;
            openCommand = null;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public override void SendMessage(NetworkMessage message)
        {
            //int transid = NSMessageProcessor.IncreaseTransactionID();
            SendSocketData(message.GetBytes() /*, transid */);
        }

        public override void SendSocketData(byte[] data)
        {
            //int transid = NSMessageProcessor.IncreaseTransactionID();
            SendSocketData(data, null/*transid*/);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void SendSocketData(byte[] data, object userState)
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

            lock (_lock)
            {
                if (sending)
                {
                    sendingQueue.Enqueue(data);
                    action = HttpPollAction.None;
                    return;
                }

                if (pollTimer.Enabled)
                    pollTimer.Stop();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateURI());
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

                sending = true;
                action = HttpPollAction.None;

                StreamState streamState = new StreamState(request, null, data);
                request.BeginGetRequestStream(EndGetRequestStream, streamState);
            }
        }

        private void EndGetRequestStream(IAsyncResult ar)
        {
            StreamState streamState = (StreamState)ar.AsyncState;
            HttpWebRequest request = (HttpWebRequest)streamState.Request;
            byte[] dataToSend = streamState.Buffer;
            try
            {
                Stream stream = request.EndGetRequestStream(ar);
                StreamState streamState2 = new StreamState(request, stream, null);
                stream.BeginWrite(dataToSend, 0, dataToSend.Length, RequestStreamEndWrite, streamState2);
            }
            catch (WebException we)
            {
                HandleWebException(we);
            }
        }

        private void RequestStreamEndWrite(IAsyncResult ar)
        {
            StreamState streamState = (StreamState)ar.AsyncState;
            HttpWebRequest request = (HttpWebRequest)streamState.Request;
            Stream stream = streamState.Stream;
            try
            {
                stream.EndWrite(ar);
                stream.Close();

                request.BeginGetResponse(EndGetResponseCallback, request);
            }
            catch (WebException we)
            {
                HandleWebException(we);
            }
        }

        private void EndGetResponseCallback(IAsyncResult ar)
        {
            HttpWebRequest request = (HttpWebRequest)ar.AsyncState;

            lock (_lock)
            {
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
                    int responseLength = (int)response.ContentLength;

                    foreach (string str in response.Headers.AllKeys)
                    {
                        switch (str)
                        {
                            case "X-MSN-Host":
                                host = response.Headers.Get(str);
                                break;

                            case "X-MSN-Messenger":
                                string text = response.Headers.Get(str);

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

                    Stream responseStream = response.GetResponseStream();
                    HttpResponseState httpState = new HttpResponseState(request, response, responseStream, responseLength);
                    responseStream.BeginRead(httpState.Buffer, httpState.Offset, responseLength - httpState.Offset, ResponseStreamEndRead, httpState);
                }
                catch (WebException we)
                {
                    HandleWebException(we);
                }
            }
        }

        private void ResponseStreamEndRead(IAsyncResult ar)
        {
            HttpResponseState state = (HttpResponseState)ar.AsyncState;

            try
            {
                state.Offset += state.ResponseStream.EndRead(ar);

                if (state.Offset < state.Buffer.Length)
                {
                    state.ResponseStream.BeginRead(state.Buffer, state.Offset, state.Buffer.Length - state.Offset, ResponseStreamEndRead, state);
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
                if (state.Offset == state.Buffer.Length)
                {
                    sending = false;
                    try
                    {
                        state.ResponseStream.Close();
                        state.Response.Close();
                    }
                    catch (Exception exc)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "HTTP Response Stream close error: " + exc.StackTrace, GetType().Name);
                    }
                }
            }

            OnSendCompleted(new ObjectEventArgs(0));

            using (BinaryReader reader = new BinaryReader(new MemoryStream(state.Buffer, 0, state.Buffer.Length)))
            {
                messagePool.BufferData(reader);
            }

            while (messagePool.MessageAvailable)
            {
                byte[] incomingMessage = messagePool.GetNextMessageData();
                messageReceiver(incomingMessage);
            }

            lock (_lock)
            {
                if (connected && (!sending) && (!pollTimer.Enabled))
                {
                    pollTimer.Start();
                }
                else if (!connected)
                {
                    // All content is read. It is time to fire event if not connected..
                    Disconnect();
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

        private class HttpResponseState
        {
            public WebRequest Request;
            public WebResponse Response;
            public Stream ResponseStream;
            public int Offset = 0;
            public byte[] Buffer;

            public HttpResponseState(WebRequest request, WebResponse response, Stream responseStream, int length)
            {
                this.Request = request;
                this.Response = response;
                this.ResponseStream = responseStream;
                this.Buffer = new byte[length];
            }
        }

        private class StreamState
        {
            public WebRequest Request;
            public Stream Stream;
            public byte[] Buffer;

            public StreamState(WebRequest request, Stream stream, byte[] buffer)
            {
                this.Request = request;
                this.Stream = stream;
                this.Buffer = buffer;
            }
        }
    }
};
