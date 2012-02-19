using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Org.Mentalis.Network.ProxySocket
{
    sealed class HttpHandler : SocksHandler
    {
        public HttpHandler(ProxySocket server, string user, string pass)
            : base(server, user)
        {
            m_Password = pass;
        }

        public override void Negotiate(IPEndPoint remoteEP)
        {
            Negotiate(remoteEP.Address.ToString(), remoteEP.Port);
        }

        public override void Negotiate(string host, int port)
        {
            Server.Send(ConnectCommand(host, port));

            byte[] buffer = new byte[4096]; // 4K ought to be enough for anyone
            int received = 0;
            while (true)
            {
                received += Server.Receive(buffer, received, buffer.Length - received, SocketFlags.None);
                if (ResponseReady(buffer, received))
                {
                    // end of the header
                    break;
                }

                if (received == buffer.Length)
                {
                    throw new ProxyException("Unexpected HTTP proxy response");
                }
            }

            ParseResponse(buffer, received);
        }

        public override IAsyncProxyResult BeginNegotiate(IPEndPoint remoteEP, HandShakeComplete callback, IPEndPoint proxyEndPoint)
        {
            return BeginNegotiate(remoteEP.Address.ToString(), remoteEP.Port, callback, proxyEndPoint);
        }

        public override IAsyncProxyResult BeginNegotiate(string host, int port, HandShakeComplete callback, IPEndPoint proxyEndPoint)
        {
            // ProtocolComplete = callback;
            // Buffer = GetHostPortBytes(host, port);
            Server.BeginConnect(proxyEndPoint, delegate(IAsyncResult ar)
            {
                this.OnConnect(ar, callback, host, port);
            }, Server);
            IAsyncProxyResult AsyncResult = new IAsyncProxyResult();
            return AsyncResult;
        }

        private byte[] ConnectCommand(string host, int port)
        {
            if (String.IsNullOrEmpty(host))
                throw new ArgumentNullException("Null host");

            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException("Bad port");

            string template = 
                "CONNECT {0}:{1} HTTP/1.0\r\n" + 
                "Host: {0}:{1}\r\n";

            string command = String.Format(template, host, port);

            if (!String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Password))
            {
                command += "Proxy-Authorization: Basic " +
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password)) +
                    "\r\n";
            }

            command += "\r\n";

            byte[] request = Encoding.ASCII.GetBytes(command);

            return request;
        }

        private bool ResponseReady(byte[] buffer, int received)
        {
            return received >= 4
                    && buffer[received - 4] == '\r' && buffer[received - 3] == '\n'
                    && buffer[received - 2] == '\r' && buffer[received - 1] == '\n';
        }

        private void ParseResponse(byte[] buffer, int received)
        {
            // parse the response
            String response = Encoding.UTF8.GetString(buffer, 0, received);
            String[] responseTokens = response.Split(new char[] { ' ' }, 3);

            if (responseTokens.Length != 3)
            {
                throw new ProxyException("Unexpected HTTP proxy response");
            }
            if (!responseTokens[0].StartsWith("HTTP"))
            {
                throw new ProxyException("Unexpected HTTP proxy response");
            }

            int responseCode = int.Parse(responseTokens[1]);
            HttpStatusCode code = (HttpStatusCode)Enum.ToObject(typeof(HttpStatusCode), responseCode);

            switch (code)
            {
                case HttpStatusCode.OK:
                    return;

                default:
                    throw new ProxyException(String.Format("HTTP proxy error: {0}", code));
            }
        }

        private void OnConnect(IAsyncResult ar, HandShakeComplete ProtocolComplete, string host, int port)
        {
            try
            {
                Server.EndConnect(ar);
            }
            catch (Exception e)
            {
                ProtocolComplete(e);
                return;
            }
            try
            {
                byte[] command = ConnectCommand(host, port);

                Server.BeginSend(command, 0, command.Length, SocketFlags.None, delegate(IAsyncResult ar2)
                {
                    this.OnSent(ar2, ProtocolComplete);
                }, Server);
            }
            catch (Exception e)
            {
                ProtocolComplete(e);
            }
        }

        private void OnSent(IAsyncResult ar, HandShakeComplete ProtocolComplete)
        {
            try
            {
                Server.EndSend(ar);
            }
            catch (Exception e)
            {
                ProtocolComplete(e);
                return;
            }
            try
            {
                byte[] buffer = new byte[4096]; // 4K ought to be enough for anyone
                int received = 0;
                Server.BeginReceive(buffer, received, buffer.Length - received, SocketFlags.None, delegate(IAsyncResult ar2)
                {
                    this.OnReceive(ar2, ProtocolComplete, buffer, received);
                }, Server);
            }
            catch (Exception e)
            {
                ProtocolComplete(e);
            }
        }

        private void OnReceive(IAsyncResult ar, HandShakeComplete ProtocolComplete, byte[] buffer, int received)
        {
            try
            {
                int newlyReceived = Server.EndReceive(ar);
                if (newlyReceived <= 0)
                {
                    ProtocolComplete(new SocketException());
                    return;
                }
                received += newlyReceived;

                if (ResponseReady(buffer, received))
                {
                    // end of the header
                    ParseResponse(buffer, received);
                    // if no exception was thrown, then..
                    ProtocolComplete(null);
                }
                else if (received == buffer.Length)
                {
                    throw new ProxyException("Unexpected HTTP proxy response");
                }
                else
                {
                    Server.BeginReceive(buffer, received, buffer.Length - received, SocketFlags.None, delegate(IAsyncResult ar2)
                    {
                        this.OnReceive(ar2, ProtocolComplete, buffer, received);
                    }, Server);
                }
            }
            catch (Exception e)
            {
                ProtocolComplete(e);
            }
        }

        #region Properties

        private string Password
        {
            get
            {
                return m_Password;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                m_Password = value;
            }
        }

        private string m_Password;

        #endregion
    }
}
