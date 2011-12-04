using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSNPSharp.Core
{
    using MSNPSharp;
    using System.Diagnostics;
    using System.Net;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// HTTP polling transport layer.
    /// </summary>
    /// Reference in http://www.hypothetic.org/docs/msn/sitev2.0/general/http_connections.php.
    public class HttpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        private bool connected = false;
        private bool opened = false; // first call to server has Action=open
        private bool verCommand = false;
        private bool cvrCommand = false;
        private bool usrCommand = false;
        private byte[] openCommand = null;

        private string sessionID;
        private string gatewayIP;

        private byte[] socketBuffer = new byte[8192];

        public string OpenUrl
        {
            get
            {
                return "http://gateway.messenger.hotmail.com/gateway/gateway.dll?Action=open&Server=NS&IP=messenger.hotmail.com";
            }
        }

        public string PollUrl
        {
            get
            {
                return String.Format(
                        "http://{0}/gateway/gateway.dll?Action=poll&LifeSpan=1&SessionID={1}",
                        GatewayIP,
                        SessionID);
            }
        }

        public string CommandUrl
        {
            get
            {
                return String.Format(
                        "http://{0}/gateway/gateway.dll?SessionID={1}",
                        GatewayIP,
                        SessionID);
            }
        }

        public override bool Connected
        {
            get { return connected; }
        }

        

        private string SessionID
        {
            get { lock (this) { return sessionID; } }
            set { lock (this) { sessionID = value; } }
        }
        
        private string GatewayIP
        {
            get { lock (this) { return gatewayIP; } }
            set { lock (this) { gatewayIP = value; } }
        }

        public HttpSocketMessageProcessor(ConnectivitySettings connectivitySettings, 
            MessageReceiver messageReceiver,
            MessagePool messagePool)
            : base(connectivitySettings, messageReceiver, messagePool)
        {
        }

        public override void Connect()
        {
            // don't have to do anything here
            OnConnected();
        }

        public override void Disconnect()
        {
            // nothing to do
            if (Connected)
                OnDisconnected();
        }

        public override void SendMessage(NetworkMessage message)
        {
            // TODO: WTF?
            throw new NotSupportedException();
        }

        public override void SendSocketData(byte[] data)
        {
            SendSocketData(data, null);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void SendSocketData(byte[] data, object userState)
        {
            // the simple and normal case
            if (opened)
            {
                Trace.WriteLine("***** Send socket data: opened.");

                WebRequest request = WebRequest.Create(OpenUrl);
                request.Proxy = null; // TODO: don't disable proxy
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.ContentLength = data.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                request.BeginGetResponse(EndGetResponseCallback, 
                    new HttpResponseState(request, null, null, userState, false));

                return;
            }

            Trace.WriteLine("***** Send socket data: not yet opened.");

            // connection has not been established yet; concat data to the end of OpenCommand
            if (openCommand == null)
            {
                openCommand = (byte[]) data.Clone();
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
                // we've collected enought to get started. Let's go
                WebRequest request = WebRequest.Create(OpenUrl);
                request.Proxy = null; // TODO: don't disable proxy
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.ContentLength = openCommand.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(openCommand, 0, openCommand.Length);
                requestStream.Close();

                request.BeginGetResponse(EndGetResponseCallback,
                    new HttpResponseState(request, null, null, userState, true));

                openCommand = null;
            }
            else
            {
                Trace.WriteLine("First batch of commands not complete; waiting for more commands.");
            }
        }

        protected virtual void EndGetResponseCallback(IAsyncResult ar)
        {
            HttpResponseState state = (HttpResponseState)ar.AsyncState;
            state.response = state.request.EndGetResponse(ar);

            string[] messengerHeaders = state.response.Headers.GetValues("X-MSN-Messenger");
            foreach (var messengerHeader in messengerHeaders)
            {
                foreach (string token in messengerHeader.Split(new char[] { ';', ' ' }))
                {
                    if (token.StartsWith("SessionID="))
                    {
                        SessionID = token.Split(new char[] { '=' }, 2)[1];
                        Trace.WriteLine("Detected new sessionID: " + SessionID);
                    }
                    else if (token.StartsWith("GW-IP="))
                    {
                        GatewayIP = token.Split(new char[] { '=' }, 2)[1];
                        Trace.WriteLine("Detected new gateway IP: " + GatewayIP);
                    }
                    else if (!token.Equals(""))
                    {
                        Trace.WriteLine("Unknown token " + token);
                    }
                }
            }

            opened = true;
            state.responseStream = state.response.GetResponseStream();
            state.responseStream.BeginRead(state.buffer, 0, state.buffer.Length, EndRead, state);
        }

        protected virtual void EndRead(IAsyncResult ar)
        {
            HttpResponseState state = (HttpResponseState)ar.AsyncState;

            int read = state.responseStream.EndRead(ar);

            if (read == 0)
            {
                state.responseStream.Close();
                state.response.Close();
                state.buffer = null;

                if (state.pollWhenDone)
                {
                    Trace.WriteLine("***** Launching next poll.. *****");

                    WebRequest request = WebRequest.Create(PollUrl);
                    request.Proxy = null; // TODO: don't disable proxy
                    request.Method = "POST";
                    request.ContentType = "text/xml; charset=utf-8";
                    request.ContentLength = 0;

                    Stream requestStream = request.GetRequestStream();
                    // no data in stream
                    requestStream.Close();

                    request.BeginGetResponse(EndGetResponseCallback,
                        new HttpResponseState(request, null, null, null, true));
                }
                else
                {
                    Trace.WriteLine("***** Not launching next poll.. *****");
                }

                return;
            }

            Trace.WriteLine("***** Read Response *****");
            Trace.WriteLine(Encoding.ASCII.GetString(state.buffer, 0, read));

            using (BinaryReader reader = new BinaryReader(new MemoryStream(state.buffer, 0, read)))
            {
                messagePool.BufferData(reader);
            }
            while (messagePool.MessageAvailable)
            {
                // retrieve the message
                byte[] incomingMessage = messagePool.GetNextMessageData();

                // call the virtual method to perform polymorphism, descendant classes can take care of it
                messageReceiver(incomingMessage);
            }

            state.responseStream.BeginRead(state.buffer, 0, state.buffer.Length, EndRead, state);
        }

        public void Dispose()
        {
            // TODO
        }

        private class HttpResponseState
        {
            public bool pollWhenDone;
            public WebRequest request;
            public WebResponse response;
            public Stream responseStream;

            public object userData;

            public byte[] buffer;

            public HttpResponseState(WebRequest request, WebResponse response, Stream responseStream, 
                object userData,
                bool pollWhenDone)
            {
                this.request = request;
                this.response = response;
                this.responseStream = responseStream;
                this.userData = userData;
                this.buffer = new byte[8192];
                this.pollWhenDone = pollWhenDone;
            }
        }
    }
}
