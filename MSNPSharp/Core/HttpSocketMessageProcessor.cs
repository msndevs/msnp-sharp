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
        None,
        Open,
        Poll
    };

    /// <summary>
    /// HTTP polling transport layer.
    /// Reference in http://www.hypothetic.org/docs/msn/sitev2.0/general/http_connections.php.
    /// </summary>
    public class HttpSocketMessageProcessor : SocketMessageProcessor, IDisposable
    {
        private HttpPollAction action = HttpPollAction.None;

        private bool connected = false;
        private bool sending = false;


        private bool opened = false; // first call to server has Action=open
        private bool verCommand = false;
        private bool cvrCommand = false;
        private bool usrCommand = false;
        private byte[] openCommand = null;

        private string sessionID;
        private string gatewayIP;

        private byte[] socketBuffer = new byte[8192];
        private System.Timers.Timer pollTimer = new System.Timers.Timer(2000);
        private object _lock = new object();


        public HttpSocketMessageProcessor(ConnectivitySettings connectivitySettings,
            MessageReceiver messageReceiver,
            MessagePool messagePool)
            : base(connectivitySettings, messageReceiver, messagePool)
        {
            gatewayIP = connectivitySettings.Host;
            pollTimer.Elapsed += pollTimer_Elapsed;
            pollTimer.AutoReset = true;
        }

        private void pollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            action = HttpPollAction.Poll;
            SendSocketData(new byte[0]);
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
                    sb.Append("Action=poll&");
                    sb.Append("SessionID=" + SessionID);
                    break;
                case HttpPollAction.None:
                    sb.Append("SessionID=" + SessionID);
                    break;
            }

            return sb.ToString();
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
                lock (this)
                {
                    return sessionID;
                }
            }
            set
            {
                lock (this)
                {
                    sessionID = value;
                }
            }
        }

        private string GatewayIP
        {
            get
            {
                lock (this)
                {
                    return gatewayIP;
                }
            }
            set
            {
                lock (this)
                {
                    gatewayIP = value;
                }
            }
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

                if (pollTimer.Enabled)
                    pollTimer.Stop();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GenerateURI());
                request.Method = "POST";
                request.Accept = "*/*";
                request.AllowAutoRedirect = false;
                request.AllowWriteStreamBuffering = false;
                request.KeepAlive = true;
                request.UserAgent = "MSMSGS";
                request.ContentType = "application/x-msn-messenger";
                request.ContentLength = data.Length;
                request.Headers.Add("Pragma", "no-cache");
                request.ServicePoint.Expect100Continue = false;
                //request.Proxy = null; // TODO: don't disable proxy

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                sending = true;
                action = HttpPollAction.None;

                request.BeginGetResponse(EndGetResponseCallback, request);
            }
        }

        private void EndGetResponseCallback(IAsyncResult ar)
        {
            bool wasSending = false;
            byte[] responseData = null;
            int responseLength = 0;

            lock (_lock)
            {
                HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);

                foreach (string str in response.Headers.AllKeys)
                {
                    switch (str)
                    {
                        case "Content-Length":
                            responseLength = Int32.Parse(response.Headers.Get(str));
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
                                        break;
                                    case "Action":
                                        break;
                                }
                            }
                            break;
                    }
                }



                responseData = new byte[responseLength];
                int responseRead = 0;

                Stream responseStream = response.GetResponseStream();

                while (responseRead < responseLength)
                {
                    byte[] buf = new byte[256];
                    int read = responseStream.Read(buf, 0, buf.Length);
                    Array.Copy(buf, 0, responseData, responseRead, read);
                    responseRead += read;
                }

                responseStream.Close();
                response.Close();

                wasSending = sending;
                sending = false;
            }

            Thread dispatchThread = new Thread(new ParameterizedThreadStart(DoDispatch));
            dispatchThread.Start(new object[] { responseData, wasSending });

            

            lock (_lock)
            {
                if ((!sending) && (!pollTimer.Enabled))
                    pollTimer.Start();
            }
        }

        private void DoDispatch(object param)
        {
            object[] _params = param as object[];
            byte[] responseData = _params[0] as byte[];
            bool wasSending = (bool)_params[1];

            if (wasSending)
            {
                OnSendCompleted(new ObjectEventArgs(0));
            }

            using (BinaryReader reader = new BinaryReader(new MemoryStream(responseData, 0, responseData.Length)))
            {
                messagePool.BufferData(reader);
            }

            while (messagePool.MessageAvailable)
            {
                byte[] incomingMessage = messagePool.GetNextMessageData();
                messageReceiver(incomingMessage);
            }

        }

        public void Dispose()
        {
            // TODO
        }
    }
};
