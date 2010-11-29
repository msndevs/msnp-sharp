using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    using MSNPSharp;
    using MSNPSharp.Core;

    public enum DCNonceType
    {
        None = 0,
        Plain = 1,
        Sha1 = 2
    }

    partial class P2PSession
    {
        private const int directNegotiationTimeout = 10000;
        private Timer _directNegotiationTimer;

        public static Guid ParseDCNonce(MimeDictionary bodyValues, out DCNonceType dcNonceType)
        {
            dcNonceType = DCNonceType.None;
            Guid nonce = Guid.Empty;

            if (bodyValues.ContainsKey("Hashed-Nonce"))
            {
                nonce = new Guid(bodyValues["Hashed-Nonce"].Value);
                dcNonceType = DCNonceType.Sha1;
            }
            else if (bodyValues.ContainsKey("Nonce"))
            {
                nonce = new Guid(bodyValues["Nonce"].Value);
                dcNonceType = DCNonceType.Plain;
            }

            return nonce;
        }

        private void ProcessDirectInvite(SLPMessage slp)
        {
            if (slp.ContentType == "application/x-msnmsgr-transreqbody")
                ProcessDirectReqInvite(slp);
            else if (slp.ContentType == "application/x-msnmsgr-transrespbody")
                ProcessDirectRespInvite(slp);
        }

        private void DirectNegotiationSuccessful()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was successful", GetType().Name);

            if (_directNegotiationTimer != null)
            {
                _directNegotiationTimer.Dispose();
                _directNegotiationTimer = null;
            }
        }

        private void DirectNegotiationFailed()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was unsuccessful", GetType().Name);

            if (_directNegotiationTimer != null)
            {
                _directNegotiationTimer.Dispose();
                _directNegotiationTimer = null;
            }

            if (p2pBridge != null)
                p2pBridge.ResumeSending(this);
        }

        private void DirectNegotiationTimedOut(object p2pSess)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation timed out", GetType().Name);

            DirectNegotiationFailed();
        }

        internal void SendDirectInvite()
        {
            // Only send the direct invite if we're currently using an SBBridge
            if (!(p2pBridge is SBBridge))
                return;

            string connectionType = "Unknown-Connect";
            string netId = "2042264281"; // unknown variable
            IPEndPoint localEndPoint = nsMessageHandler.LocalEndPoint;
            IPEndPoint externalEndPoint = nsMessageHandler.ExternalEndPoint;

            #region Check and determine connectivity

            if (localEndPoint == null || externalEndPoint == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "LocalEndPoint or ExternalEndPoint are not set. Connection type will be set to unknown.", GetType().Name);
            }
            else
            {
                if (localEndPoint.Address.Equals(externalEndPoint.Address))
                {
                    if (localEndPoint.Port == externalEndPoint.Port)
                        connectionType = "Direct-Connect";
                    else
                        connectionType = "Port-Restrict-NAT";
                }
                else
                {
                    if (localEndPoint.Port == externalEndPoint.Port)
                    {
                        netId = "0";
                        connectionType = "IP-Restrict-NAT";
                    }
                    else
                        connectionType = "Symmetric-NAT";
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connection type set to " + connectionType + " for session " + sessionId, GetType().Name);
            }

            #endregion

            // Create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.Target = RemoteContactEPIDString;
            slpMessage.Source = LocalContactEPIDString;
            slpMessage.CSeq = 0;
            slpMessage.CallId = invitation.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transreqbody";

            slpMessage.BodyValues["Bridges"] = "TCPv1 SBBridge";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["NetID"] = netId;
            slpMessage.BodyValues["Conn-Type"] = connectionType;
            slpMessage.BodyValues["TCP-Conn-Type"] = connectionType;
            slpMessage.BodyValues["UPnPNat"] = "false"; // UPNP Enabled
            slpMessage.BodyValues["ICF"] = "false"; // Firewall enabled
            slpMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Req";

            Send(WrapSLPMessage(slpMessage));

            P2PMessage ping = new P2PMessage(Version);
            Send(ping);

            _directNegotiationTimer = new Timer(new TimerCallback(DirectNegotiationTimedOut), this, directNegotiationTimeout, directNegotiationTimeout);
            // Stop sending until we receive a response to the direct invite or the timeout expires
            p2pBridge.StopSending(this);
        }

        private void ProcessDirectReqInvite(SLPMessage message)
        {
            if (message.BodyValues.ContainsKey("Bridges") &&
                message.BodyValues["Bridges"].ToString().Contains("TCPv1"))
            {
                SLPStatusMessage slpMessage = new SLPStatusMessage(RemoteContactEPIDString, 200, "OK");
                slpMessage.Target = RemoteContactEPIDString;
                slpMessage.Source = LocalContactEPIDString;
                slpMessage.Branch = message.Branch;
                slpMessage.CSeq = 1;
                slpMessage.CallId = message.CallId;
                slpMessage.MaxForwards = 0;
                slpMessage.ContentType = "application/x-msnmsgr-transrespbody";
                slpMessage.BodyValues["Bridge"] = "TCPv1";

                // Initial NonceType. Remote side prefers this. But I prefer Nonce :)
                DCNonceType dcNonceType;
                Guid nonce = ParseDCNonce(message.BodyValues, out dcNonceType);

                if (dcNonceType == DCNonceType.None)
                {
                    nonce = Guid.NewGuid();
                }

                bool hashed = false;
                string nonceFieldName = "Nonce";

                // Find host by name
                IPAddress ipAddress = nsMessageHandler.LocalEndPoint.Address;
                int port;

                if (false == ipAddress.Equals(nsMessageHandler.ExternalEndPoint.Address) ||
                    (0 == (port = GetNextDirectConnectionPort(ipAddress))))
                {
                    slpMessage.BodyValues["Listening"] = "false";
                    slpMessage.BodyValues[nonceFieldName] = Guid.Empty.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    // Let's listen
                    Remote.DirectBridge = ListenForDirectConnection(ipAddress, port, nonce, hashed);

                    _directNegotiationTimer = new Timer(new TimerCallback(DirectNegotiationTimedOut), this, directNegotiationTimeout, directNegotiationTimeout);

                    slpMessage.BodyValues["Listening"] = "true";
                    slpMessage.BodyValues["Capabilities-Flags"] = "1";
                    slpMessage.BodyValues["IPv6-global"] = string.Empty;
                    slpMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Resp";
                    slpMessage.BodyValues["UPnPNat"] = "false";

                    slpMessage.BodyValues["NeedConnectingEndpointInfo"] = "false";
                    slpMessage.BodyValues["Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues["TCP-Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues[nonceFieldName] = nonce.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                    slpMessage.BodyValues["IPv4Internal-Addrs"] = ipAddress.ToString();
                    slpMessage.BodyValues["IPv4Internal-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // check if client is behind firewall (NAT-ted)
                    // if so, send the public ip also the client, so it can try to connect to that ip
                    if (!nsMessageHandler.ExternalEndPoint.Address.Equals(nsMessageHandler.LocalEndPoint.Address))
                    {
                        slpMessage.BodyValues["IPv4External-Addrs"] = nsMessageHandler.ExternalEndPoint.Address.ToString();
                        slpMessage.BodyValues["IPv4External-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                P2PMessage p2pMessage = WrapSLPMessage(slpMessage);
                Send(p2pMessage);
            }
            else
            {
                DirectNegotiationFailed();
            }
        }

        private void ProcessDirectRespInvite(SLPMessage message)
        {
            MimeDictionary bodyValues = message.BodyValues;

            // Check the protocol
            if (bodyValues.ContainsKey("Bridge") &&
                bodyValues["Bridge"].ToString() == "TCPv1" &&
                bodyValues.ContainsKey("Listening") &&
                bodyValues["Listening"].ToString().ToLowerInvariant().IndexOf("true") >= 0)
            {
                DCNonceType dcNonceType;
                Guid nonce = ParseDCNonce(message.BodyValues, out dcNonceType);

                if (dcNonceType == DCNonceType.Sha1)
                {
                    // Always needed
                    //properties.RemoteNonce = nonce;
                    //properties.DCNonceType = dcNonceType;
                }

                if (dcNonceType == DCNonceType.Plain)
                {
                    // Only needed for listening side
                    //properties.Nonce = nonce;
                    //properties.DCNonceType = dcNonceType;
                }

                IPEndPoint selectedPoint = SelectIPEndPoint(bodyValues);

                if (selectedPoint != null)
                {
                    // We must connect to the remote client
                    ConnectivitySettings settings = new ConnectivitySettings();
                    settings.Host = selectedPoint.Address.ToString();
                    settings.Port = selectedPoint.Port;

                    Remote.DirectBridge = CreateDirectConnection(settings.Host, settings.Port, nonce, (dcNonceType == DCNonceType.Sha1));
                    return;
                }
            }

            DirectNegotiationFailed();
        }

        private TCPv1Bridge ListenForDirectConnection(IPAddress host, int port, Guid nonce, bool hashed)
        {
            ConnectivitySettings cs = new ConnectivitySettings();
            if (NSMessageHandler.ConnectivitySettings.LocalHost == string.Empty)
            {
                cs.LocalHost = host.ToString();
                cs.LocalPort = port;
            }
            else
            {
                cs.LocalHost = NSMessageHandler.ConnectivitySettings.LocalHost;
                cs.LocalPort = NSMessageHandler.ConnectivitySettings.LocalPort;
            }

            TCPv1Bridge tcpBridge = new TCPv1Bridge(cs, Version, nonce, hashed, this);

            tcpBridge.Listen(IPAddress.Parse(cs.LocalHost), cs.LocalPort);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Listening on " + cs.LocalHost + ":" + cs.LocalPort.ToString(System.Globalization.CultureInfo.InvariantCulture), GetType().Name);

            return tcpBridge;
        }

        private TCPv1Bridge CreateDirectConnection(string host, int port, Guid nonce, bool hashed)
        {
            TCPv1Bridge tcpBridge = new TCPv1Bridge(new ConnectivitySettings(host, port), Version, nonce, hashed, this);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Trying to setup direct connection with remote host " + host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture), GetType().Name);

            tcpBridge.Connect();

            return tcpBridge;
        }

        private int GetNextDirectConnectionPort(IPAddress ipAddress)
        {
            int portAvail = 0;

            if (Settings.PublicPortPriority == PublicPortPriority.First)
            {
                portAvail = TryPublicPorts(ipAddress);

                if (portAvail != 0)
                {
                    return portAvail;
                }
            }

            if (portAvail == 0)
            {
                // Don't try all ports
                for (int p = 1119, maxTry = 100;
                    p <= IPEndPoint.MaxPort && maxTry < 1;
                    p++, maxTry--)
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        //s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        s.Bind(new IPEndPoint(ipAddress, p));
                        //s.Bind(new IPEndPoint(IPAddress.Any, p));
                        s.Close();
                        return p;
                    }
                    catch (SocketException ex)
                    {
                        // Permission denied
                        if (ex.ErrorCode == 10048 /*Address already in use*/ ||
                            ex.ErrorCode == 10013 /*Permission denied*/)
                        {
                            p += 100;
                        }
                        continue; // throw;
                    }
                    catch (System.Security.SecurityException)
                    {
                        break;
                    }
                }
            }

            if (portAvail == 0 && Settings.PublicPortPriority == PublicPortPriority.Last)
            {
                portAvail = TryPublicPorts(ipAddress);
            }

            return portAvail;
        }

        private int TryPublicPorts(IPAddress localIP)
        {
            foreach (int p in Settings.PublicPorts)
            {
                Socket s = null;
                try
                {
                    s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    s.Bind(new IPEndPoint(localIP, p));
                    return p;
                }
                catch (SocketException)
                {
                }
                finally
                {
                    if (s != null)
                        s.Close();
                }
            }
            return 0;
        }

        private IPEndPoint SelectIPEndPoint(MimeDictionary bodyValues)
        {
            List<IPAddress> ipAddrs = new List<IPAddress>();
            string[] addrs = new string[0];
            int port = 0;

            if (bodyValues.ContainsKey("IPv4External-Addrs") || bodyValues.ContainsKey("srddA-lanretxE4vPI"))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Using external IP addresses", GetType().Name);

                if (bodyValues.ContainsKey("IPv4External-Addrs"))
                {
                    addrs = bodyValues["IPv4External-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    port = int.Parse(bodyValues["IPv4External-Port"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    char[] revHost = bodyValues["srddA-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    port = int.Parse(new string(revPort), System.Globalization.CultureInfo.InvariantCulture);
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                       String.Format("{0} external IP addresses found, with port {1}", addrs.Length, port), GetType().Name);

                for (int i = 0; i < addrs.Length; i++)
                {
                    IPAddress ip;
                    if (IPAddress.TryParse(addrs[i], out ip))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i], GetType().Name);

                        ipAddrs.Add(ip);

                        if (ip.Equals(nsMessageHandler.LocalEndPoint.Address))
                        {
                            // External IP matches our own, clearing external IPs
                            //addrs = new string[0];
                            //ipAddrs.Clear();
                            break;
                        }
                    }
                }
            }

            if ((ipAddrs.Count == 0) &&
                (bodyValues.ContainsKey("IPv4Internal-Addrs") || bodyValues.ContainsKey("srddA-lanretnI4vPI")))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Using internal IP addresses", GetType().Name);

                if (bodyValues.ContainsKey("IPv4Internal-Addrs"))
                {
                    addrs = bodyValues["IPv4Internal-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    port = int.Parse(bodyValues["IPv4Internal-Port"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    char[] revHost = bodyValues["srddA-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    port = int.Parse(new string(revPort), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (addrs.Length == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                   "Unable to find any remote IP addresses", GetType().Name);

                // Failed
                return null;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                      String.Format("{0} internal IP addresses found, with port {1}",
                      addrs.Length, port), GetType().Name);

            for (int i = 0; i < addrs.Length; i++)
            {
                IPAddress ip;
                if (IPAddress.TryParse(addrs[i], out ip))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i], GetType().Name);

                    ipAddrs.Add(ip);
                }
            }

            if (addrs.Length == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                   "Unable to find any remote IP addresses", GetType().Name);

                // Failed
                return null;
            }

            // Try to find the correct IP
            IPAddress ipAddr = null;
            byte[] localBytes = nsMessageHandler.LocalEndPoint.Address.GetAddressBytes();

            foreach (IPAddress ip in ipAddrs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // This is an IPv4 address
                    // Check if the first 3 octets match our local IP address
                    // If so, make use of that address (it's on our LAN)

                    byte[] bytes = ip.GetAddressBytes();

                    if ((bytes[0] == localBytes[0]) && (bytes[1] == localBytes[1]) && (bytes[2] == localBytes[2]))
                    {
                        ipAddr = ip;
                        break;
                    }
                }
            }

            if (ipAddr == null)
                ipAddr = ipAddrs[0];

            return new IPEndPoint(ipAddr, port);
        }
    }
};
