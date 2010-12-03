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

        internal static void ProcessDirectInvite(
            SLPMessage slp,
            NSMessageHandler nsMessageHandler,
            P2PSession startupSession)
        {
            if (slp.ContentType == "application/x-msnmsgr-transreqbody")
                ProcessDirectReqInvite(slp, nsMessageHandler, startupSession);
            else if (slp.ContentType == "application/x-msnmsgr-transrespbody")
                ProcessDirectRespInvite(slp, nsMessageHandler, startupSession);
        }

        private void DirectNegotiationSuccessful()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was successful", GetType().Name);
        }

        internal void DirectNegotiationFailed()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was unsuccessful", GetType().Name);

            if (p2pBridge != null)
                p2pBridge.ResumeSending(this);
        }

        internal static void SendDirectInvite(
            NSMessageHandler nsMessageHandler,
            P2PBridge p2pBridge,
            P2PSession p2pSession)
        {
            // Only send the direct invite if we're currently using an SBBridge or UUNBridge
            if (!(p2pBridge is SBBridge) && !(p2pBridge is UUNBridge))
                return;

            string connectionType = "Unknown-Connect";
            string netId = "2042264281"; // unknown variable
            IPEndPoint localEndPoint = nsMessageHandler.LocalEndPoint;
            IPEndPoint externalEndPoint = nsMessageHandler.ExternalEndPoint;

            #region Check and determine connectivity

            if (localEndPoint == null || externalEndPoint == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "LocalEndPoint or ExternalEndPoint are not set. Connection type will be set to unknown.");
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

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    String.Format("Connection type set to {0} for session {1}", connectionType, p2pSession.SessionId.ToString()));
            }

            #endregion

            // Create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(p2pSession.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.Source = p2pSession.LocalContactEPIDString;
            slpMessage.CSeq = 0;
            slpMessage.CallId = p2pSession.Invitation.CallId;
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

            Contact remote = p2pSession.Remote;
            P2PVersion ver = p2pSession.Version;
            P2PMessage p2pMessage = new P2PMessage(ver);
            p2pMessage.InnerMessage = slpMessage;

            if (ver == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }
            else if (ver == P2PVersion.P2PV1)
            {
                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }

            p2pBridge.Send(p2pSession, remote, p2pSession.RemoteContactEndPointID, p2pMessage, null);

            // Wait a bit, otherwise SLP message queued when called p2pBridge.StopSending(this);
            Thread.CurrentThread.Join(900);

            // Stop sending until we receive a response to the direct invite or the timeout expires
            p2pBridge.StopSending(p2pSession);
        }

        private static void ProcessDirectReqInvite(
            SLPMessage message,
            NSMessageHandler nsMessageHandler,
            P2PSession startupSession)
        {
            if (message.BodyValues.ContainsKey("Bridges") &&
                message.BodyValues["Bridges"].ToString().Contains("TCPv1"))
            {
                SLPStatusMessage slpMessage = new SLPStatusMessage(message.Source, 200, "OK");
                slpMessage.Target = message.Source;
                slpMessage.Source = message.Target;
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


                Contact remote = nsMessageHandler.ContactList.GetContact(message.FromEmailAccount, ClientType.PassportMember);
                Guid remoteGuid = message.FromEndPoint;

                P2PVersion ver = (message.FromEndPoint != Guid.Empty && message.ToEndPoint != Guid.Empty)
                    ? P2PVersion.P2PV2 : P2PVersion.P2PV1;


                if (false == ipAddress.Equals(nsMessageHandler.ExternalEndPoint.Address) ||
                    (0 == (port = GetNextDirectConnectionPort(ipAddress))))
                {
                    slpMessage.BodyValues["Listening"] = "false";
                    slpMessage.BodyValues[nonceFieldName] = Guid.Empty.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {

                    // Let's listen
                    remote.DirectBridge = ListenForDirectConnection(remote, nsMessageHandler, ver, startupSession, ipAddress, port, nonce, hashed);

                    //NEWP2P,XXX,TODO:_directNegotiationTimer = new Timer(new TimerCallback(DirectNegotiationTimedOut), this, directNegotiationTimeout, directNegotiationTimeout);

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

                P2PMessage p2pMessage = new P2PMessage(ver);
                p2pMessage.InnerMessage = slpMessage;

                if (ver == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.TFCombination = TFCombination.First;
                }
                else if (ver == P2PVersion.P2PV1)
                {
                    p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                }

                if (startupSession != null)
                    startupSession.Send(p2pMessage);
                else
                    nsMessageHandler.UUNBridge.Send(null, remote, remoteGuid, p2pMessage, null);
            }
            else
            {
                if (startupSession != null)
                    startupSession.DirectNegotiationFailed();
            }
        }

        private static void ProcessDirectRespInvite(
            SLPMessage message,
            NSMessageHandler nsMessageHandler,
            P2PSession startupSession)
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

                IPEndPoint selectedPoint = SelectIPEndPoint(bodyValues, nsMessageHandler);

                if (selectedPoint != null)
                {
                    P2PVersion ver = (message.FromEndPoint != Guid.Empty && message.ToEndPoint != Guid.Empty)
                        ? P2PVersion.P2PV2 : P2PVersion.P2PV1;

                    Contact remote = nsMessageHandler.ContactList.GetContact(message.FromEmailAccount, ClientType.PassportMember);
                    Guid remoteGuid = message.FromEndPoint;

                    // We must connect to the remote client
                    ConnectivitySettings settings = new ConnectivitySettings();
                    settings.Host = selectedPoint.Address.ToString();
                    settings.Port = selectedPoint.Port;

                    remote.DirectBridge = CreateDirectConnection(remote, ver, settings.Host, settings.Port, nonce, (dcNonceType == DCNonceType.Sha1), nsMessageHandler, startupSession);
                    return;
                }
            }

            if (startupSession != null)
                startupSession.DirectNegotiationFailed();
        }

        private static TCPv1Bridge ListenForDirectConnection(
            Contact remote,
            NSMessageHandler nsMessageHandler,
            P2PVersion ver,
            P2PSession startupSession,
            IPAddress host, int port, Guid nonce, bool hashed)
        {
            ConnectivitySettings cs = new ConnectivitySettings();
            if (nsMessageHandler.ConnectivitySettings.LocalHost == string.Empty)
            {
                cs.LocalHost = host.ToString();
                cs.LocalPort = port;
            }
            else
            {
                cs.LocalHost = nsMessageHandler.ConnectivitySettings.LocalHost;
                cs.LocalPort = nsMessageHandler.ConnectivitySettings.LocalPort;
            }

            TCPv1Bridge tcpBridge = new TCPv1Bridge(cs, ver, nonce, hashed, startupSession, nsMessageHandler, remote);

            tcpBridge.Listen(IPAddress.Parse(cs.LocalHost), cs.LocalPort);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Listening on " + cs.LocalHost + ":" + cs.LocalPort.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return tcpBridge;
        }

        private static TCPv1Bridge CreateDirectConnection(Contact remote, P2PVersion ver, string host, int port, Guid nonce, bool hashed, NSMessageHandler nsMessageHandler, P2PSession startupSession)
        {
            TCPv1Bridge tcpBridge = new TCPv1Bridge(new ConnectivitySettings(host, port), ver, nonce, hashed, startupSession, nsMessageHandler, remote);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Trying to setup direct connection with remote host " + host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture));

            tcpBridge.Connect();

            return tcpBridge;
        }

        private static int GetNextDirectConnectionPort(IPAddress ipAddress)
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

        private static int TryPublicPorts(IPAddress localIP)
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

        private static IPEndPoint SelectIPEndPoint(MimeDictionary bodyValues, NSMessageHandler nsMessageHandler)
        {
            List<IPAddress> ipAddrs = new List<IPAddress>();
            string[] addrs = new string[0];
            int port = 0;

            if (bodyValues.ContainsKey("IPv4External-Addrs") || bodyValues.ContainsKey("srddA-lanretxE4vPI"))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Using external IP addresses");

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
                       String.Format("{0} external IP addresses found, with port {1}", addrs.Length, port));

                for (int i = 0; i < addrs.Length; i++)
                {
                    IPAddress ip;
                    if (IPAddress.TryParse(addrs[i], out ip))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i]);

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
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Using internal IP addresses");

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
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Unable to find any remote IP addresses");

                // Failed
                return null;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                      String.Format("{0} internal IP addresses found, with port {1}",
                      addrs.Length, port));

            for (int i = 0; i < addrs.Length; i++)
            {
                IPAddress ip;
                if (IPAddress.TryParse(addrs[i], out ip))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i]);

                    ipAddrs.Add(ip);
                }
            }

            if (addrs.Length == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Unable to find any remote IP addresses");

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
