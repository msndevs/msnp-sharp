using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Org.Mentalis.Network.ProxySocket
{
    sealed class HttpHandler
    {
        private enum HttpResponseCodes
        {
            None = 0,
            Continue = 100,
            SwitchingProtocols = 101,
            OK = 200,
            Created = 201,
            Accepted = 202,
            NonAuthoritiveInformation = 203,
            NoContent = 204,
            ResetContent = 205,
            PartialContent = 206,
            MultipleChoices = 300,
            MovedPermanetly = 301,
            Found = 302,
            SeeOther = 303,
            NotModified = 304,
            UserProxy = 305,
            TemporaryRedirect = 307,
            BadRequest = 400,
            Unauthorized = 401,
            PaymentRequired = 402,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            NotAcceptable = 406,
            ProxyAuthenticantionRequired = 407,
            RequestTimeout = 408,
            Conflict = 409,
            Gone = 410,
            PreconditionFailed = 411,
            RequestEntityTooLarge = 413,
            RequestURITooLong = 414,
            UnsupportedMediaType = 415,
            RequestedRangeNotSatisfied = 416,
            ExpectationFailed = 417,
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HTTPVersionNotSupported = 505
        }

        public HttpHandler(ProxySocket server, string user, string pass)
        {
            m_Server = server;
            m_Username = user;
            m_Password = pass;
        }

        public void Negotiate(IPEndPoint remoteEP)
        {
            Negotiate(remoteEP.Address.ToString(), remoteEP.Port);
        }

        private byte[] ConnectCommand(string host, int port)
        {
            if (String.IsNullOrEmpty(host))
                throw new ArgumentNullException("Null host");

            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException("Bad port");

            string template = "CONNECT {0}:{1} HTTP/1.0 \r\nHOST {0}:{1}\r\n\r\n";
            string command = String.Format(template, host, port);
            byte[] request = ASCIIEncoding.ASCII.GetBytes(command);

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
            HttpResponseCodes code = (HttpResponseCodes)Enum.ToObject(typeof(HttpResponseCodes), responseCode);

            switch (code)
            {
                case HttpResponseCodes.OK:
                    return;

                default:
                    throw new ProxyException(String.Format("HTTP proxy error: {0}", code));
            }
        }

        public void Negotiate(string host, int port)
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

        public IAsyncProxyResult BeginNegotiate(IPEndPoint remoteEP, HandShakeComplete callback, IPEndPoint proxyEndPoint)
        {
            return BeginNegotiate(remoteEP.Address.ToString(), remoteEP.Port, callback, proxyEndPoint);
        }

        public IAsyncProxyResult BeginNegotiate(string host, int port, HandShakeComplete callback, IPEndPoint proxyEndPoint)
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

        public Socket Server
        {
            get
            {
                return m_Server;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Server = value;
            }
        }

        public string Username
        {
            get
            {
                return m_Username;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Username = value;
            }
        }

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

        private Socket m_Server;
        private string m_Username;
        private string m_Password;

        #endregion
    }
}
