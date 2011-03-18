#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
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
        private Timer directNegotiationTimer = null;

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

        internal static void ProcessDirectInvite(SLPMessage slp, NSMessageHandler ns, P2PSession startupSession)
        {
            switch (slp.ContentType)
            {
                case "application/x-msnmsgr-transreqbody":
                    ProcessDCReqInvite(slp, ns, startupSession);
                    break;

                case "application/x-msnmsgr-transrespbody":
                    ProcessDCRespInvite(slp, ns, startupSession);
                    break;

                case "application/x-msnmsgr-transdestaddrupdate":
                    ProcessDirectAddrUpdate(slp, ns, startupSession);
                    break;
            }
        }

        private void DirectNegotiationSuccessful()
        {
            if (directNegotiationTimer != null)
            {
                directNegotiationTimer.Dispose();
                directNegotiationTimer = null;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was successful", GetType().Name);
        }

        private void DirectNegotiationTimedOut(object p2pSession)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation timed out", GetType().Name);

            DirectNegotiationFailed();
        }

        internal void DirectNegotiationFailed()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Direct connection negotiation was unsuccessful", GetType().Name);

            if (directNegotiationTimer != null)
            {
                directNegotiationTimer.Dispose();
                directNegotiationTimer = null;
            }

            if (p2pBridge != null)
                p2pBridge.ResumeSending(this);
        }

        private static string ConnectionType(NSMessageHandler nsMessageHandler, out int netId)
        {
            string connectionType = "Unknown-Connect";
            netId = 0;
            IPEndPoint localEndPoint = nsMessageHandler.LocalEndPoint;
            IPEndPoint externalEndPoint = nsMessageHandler.ExternalEndPoint;

            if (localEndPoint == null || externalEndPoint == null)
            {
            }
            else
            {
                netId = BitConverter.ToInt32(localEndPoint.Address.GetAddressBytes(), 0);

                if (Settings.DisableP2PDirectConnections)
                {
                    connectionType = "Firewall";
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
                            netId = 0;
                            connectionType = "IP-Restrict-NAT";
                        }
                        else
                            connectionType = "Symmetric-NAT";
                    }
                }
            }

            return connectionType;
        }

        internal static bool SendDirectInvite(
            NSMessageHandler nsMessageHandler,
            P2PSession p2pSession)
        {
            // Skip if we're currently using a TCPBridge.
            if (p2pSession != null && p2pSession.Remote.DirectBridge != null && p2pSession.Remote.DirectBridge.IsOpen)
                return false;

            int netId;
            string connectionType = ConnectionType(nsMessageHandler, out netId);
            Contact remote = p2pSession.Remote;
            P2PVersion ver = p2pSession.Version;
            P2PMessage p2pMessage = new P2PMessage(ver);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                String.Format("Connection type set to {0} for session {1}", connectionType, p2pSession.SessionId.ToString()));

            // Create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(p2pSession.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.Source = p2pSession.LocalContactEPIDString;
            slpMessage.CSeq = 0;
            slpMessage.CallId = p2pSession.Invitation.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transreqbody";

            slpMessage.BodyValues["Bridges"] = "TCPv1 SBBridge";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["NetID"] = netId.ToString(CultureInfo.InvariantCulture);
            slpMessage.BodyValues["Conn-Type"] = connectionType;
            slpMessage.BodyValues["TCP-Conn-Type"] = connectionType;
            slpMessage.BodyValues["UPnPNat"] = "false"; // UPNP Enabled
            slpMessage.BodyValues["ICF"] = (connectionType == "Firewall").ToString(); // Firewall enabled
            slpMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Req";

            // We support Hashed-Nonce ( 2 way handshake )
            remote.GenerateNewDCKeys();
            slpMessage.BodyValues["Hashed-Nonce"] = remote.dcLocalHashedNonce.ToString("B").ToUpper(CultureInfo.InvariantCulture);

            p2pMessage.InnerMessage = slpMessage;

            if (ver == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }
            else if (ver == P2PVersion.P2PV1)
            {
                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }

            p2pSession.SetupDCTimer();
            nsMessageHandler.SDGBridge.Send(null, remote, p2pSession.RemoteContactEndPointID, p2pMessage, null);
            return true;
        }

        private static void ProcessDCReqInvite(SLPMessage message, NSMessageHandler ns, P2PSession startupSession)
        {
            if (startupSession != null && startupSession.Bridge != null &&
                startupSession.Bridge is TCPv1Bridge)
            {
                return; // We are using a dc bridge already. Don't allow second one.
            }

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

                Guid remoteGuid = message.FromEndPoint;
                Contact remote = ns.ContactList.GetContactWithCreate(message.FromEmailAccount, IMAddressInfoType.WindowsLive);

                DCNonceType dcNonceType;
                Guid remoteNonce = ParseDCNonce(message.BodyValues, out dcNonceType);
                if (remoteNonce == Guid.Empty) // Plain
                    remoteNonce = remote.dcPlainKey;

                bool hashed = (dcNonceType == DCNonceType.Sha1);
                string nonceFieldName = hashed ? "Hashed-Nonce" : "Nonce";
                Guid myHashedNonce = hashed ? remote.dcLocalHashedNonce : remoteNonce;
                Guid myPlainNonce = remote.dcPlainKey;
                if (dcNonceType == DCNonceType.Sha1)
                {
                    // Remote contact supports Hashed-Nonce
                    remote.dcType = dcNonceType;
                    remote.dcRemoteHashedNonce = remoteNonce;
                }
                else
                {
                    remote.dcType = DCNonceType.Plain;
                    myPlainNonce = remote.dcPlainKey = remote.dcLocalHashedNonce = remote.dcRemoteHashedNonce = remoteNonce;
                }

                // Find host by name
                IPAddress ipAddress = ns.LocalEndPoint.Address;
                int port;

                P2PVersion ver = (message.FromEndPoint != Guid.Empty && message.ToEndPoint != Guid.Empty)
                    ? P2PVersion.P2PV2 : P2PVersion.P2PV1;

                if (Settings.DisableP2PDirectConnections ||
                    false == ipAddress.Equals(ns.ExternalEndPoint.Address) ||
                    (0 == (port = GetNextDirectConnectionPort(ipAddress))))
                {
                    slpMessage.BodyValues["Listening"] = "false";
                    slpMessage.BodyValues[nonceFieldName] = Guid.Empty.ToString("B").ToUpper(CultureInfo.InvariantCulture);
                }
                else
                {
                    // Let's listen
                    remote.DirectBridge = ListenForDirectConnection(remote, ns, ver, startupSession, ipAddress, port, myPlainNonce, remoteNonce, hashed);

                    slpMessage.BodyValues["Listening"] = "true";
                    slpMessage.BodyValues["Capabilities-Flags"] = "1";
                    slpMessage.BodyValues["IPv6-global"] = string.Empty;
                    slpMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Resp";
                    slpMessage.BodyValues["UPnPNat"] = "false";

                    slpMessage.BodyValues["NeedConnectingEndpointInfo"] = "true";

                    slpMessage.BodyValues["Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues["TCP-Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues[nonceFieldName] = myHashedNonce.ToString("B").ToUpper(CultureInfo.InvariantCulture);
                    slpMessage.BodyValues["IPv4Internal-Addrs"] = ipAddress.ToString();
                    slpMessage.BodyValues["IPv4Internal-Port"] = port.ToString(CultureInfo.InvariantCulture);

                    // check if client is behind firewall (NAT-ted)
                    // if so, send the public ip also the client, so it can try to connect to that ip
                    if (!ns.ExternalEndPoint.Address.Equals(ns.LocalEndPoint.Address))
                    {
                        slpMessage.BodyValues["IPv4External-Addrs"] = ns.ExternalEndPoint.Address.ToString();
                        slpMessage.BodyValues["IPv4External-Port"] = port.ToString(CultureInfo.InvariantCulture);
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
                {
                    startupSession.SetupDCTimer();
                    startupSession.Bridge.Send(null, startupSession.Remote, startupSession.RemoteContactEndPointID, p2pMessage, null);
                }
                else
                {
                    ns.SDGBridge.Send(null, remote, remoteGuid, p2pMessage, null);
                }
            }
            else
            {
                if (startupSession != null)
                    startupSession.DirectNegotiationFailed();
            }
        }

        private void SetupDCTimer()
        {
            directNegotiationTimer = new Timer(new TimerCallback(DirectNegotiationTimedOut), this, 17000, 17000);
        }

        private static void ProcessDCRespInvite(SLPMessage message, NSMessageHandler ns, P2PSession startupSession)
        {
            MimeDictionary bodyValues = message.BodyValues;

            // Check the protocol
            if (bodyValues.ContainsKey("Bridge") &&
                bodyValues["Bridge"].ToString() == "TCPv1" &&
                bodyValues.ContainsKey("Listening") &&
                bodyValues["Listening"].ToString().ToLowerInvariant().IndexOf("true") >= 0)
            {
                Contact remote = ns.ContactList.GetContactWithCreate(message.FromEmailAccount, IMAddressInfoType.WindowsLive);
                Guid remoteGuid = message.FromEndPoint;

                DCNonceType dcNonceType;
                Guid remoteNonce = ParseDCNonce(message.BodyValues, out dcNonceType);
                bool hashed = (dcNonceType == DCNonceType.Sha1);
                Guid replyGuid = hashed ? remote.dcPlainKey : remoteNonce;

                IPEndPoint[] selectedPoint = SelectIPEndPoint(bodyValues, ns);

                if (selectedPoint != null && selectedPoint.Length > 0)
                {
                    P2PVersion ver = (message.FromEndPoint != Guid.Empty && message.ToEndPoint != Guid.Empty)
                        ? P2PVersion.P2PV2 : P2PVersion.P2PV1;

                    // We must connect to the remote client
                    ConnectivitySettings settings = new ConnectivitySettings();
                    settings.EndPoints = selectedPoint;
                    remote.DirectBridge = CreateDirectConnection(remote, ver, settings, replyGuid, remoteNonce, hashed, ns, startupSession);

                    bool needConnectingEndpointInfo;
                    if (bodyValues.ContainsKey("NeedConnectingEndpointInfo") &&
                        bool.TryParse(bodyValues["NeedConnectingEndpointInfo"], out needConnectingEndpointInfo) &&
                        needConnectingEndpointInfo == true)
                    {
                        IPEndPoint ipep = ((TCPv1Bridge)remote.DirectBridge).LocalEndPoint;

                        string desc = "stroPdnAsrddAlanretnI4vPI";
                        char[] rev = ipep.ToString().ToCharArray();
                        Array.Reverse(rev);
                        string ipandport = new string(rev);

                        SLPRequestMessage slpResponseMessage = new SLPRequestMessage(message.Source, MSNSLPRequestMethod.ACK);
                        slpResponseMessage.Source = message.Target;
                        slpResponseMessage.Via = message.Via;
                        slpResponseMessage.CSeq = 0;
                        slpResponseMessage.CallId = Guid.Empty;
                        slpResponseMessage.MaxForwards = 0;
                        slpResponseMessage.ContentType = @"application/x-msnmsgr-transdestaddrupdate";

                        slpResponseMessage.BodyValues[desc] = ipandport;
                        slpResponseMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Updated-Connecting-Port";

                        P2PMessage msg = new P2PMessage(ver);
                        msg.InnerMessage = slpResponseMessage;

                        ns.SDGBridge.Send(null, remote, remoteGuid, msg, null);
                    }

                    return;
                }
            }

            if (startupSession != null)
                startupSession.DirectNegotiationFailed();
        }

        private static void ProcessDirectAddrUpdate(
            SLPMessage message,
            NSMessageHandler nsMessageHandler,
            P2PSession startupSession)
        {
            Contact from = nsMessageHandler.ContactList.GetContactWithCreate(message.FromEmailAccount, IMAddressInfoType.WindowsLive);
            IPEndPoint[] ipEndPoints = SelectIPEndPoint(message.BodyValues, nsMessageHandler);

            if (from.DirectBridge != null && ipEndPoints != null && ipEndPoints.Length > 0)
            {
                ((TCPv1Bridge)from.DirectBridge).OnDestinationAddressUpdated(new DestinationAddressUpdatedEventHandler(ipEndPoints));
            }
        }

        private static TCPv1Bridge ListenForDirectConnection(
            Contact remote,
            NSMessageHandler nsMessageHandler,
            P2PVersion ver,
            P2PSession startupSession,
            IPAddress host, int port, Guid replyGuid, Guid remoteNonce, bool hashed)
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

            TCPv1Bridge tcpBridge = new TCPv1Bridge(cs, ver, replyGuid, remoteNonce, hashed, startupSession, nsMessageHandler, remote);

            tcpBridge.Listen(IPAddress.Parse(cs.LocalHost), cs.LocalPort);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Listening on " + cs.LocalHost + ":" + cs.LocalPort.ToString(CultureInfo.InvariantCulture));

            return tcpBridge;
        }

        private static TCPv1Bridge CreateDirectConnection(Contact remote, P2PVersion ver, ConnectivitySettings cs, Guid replyGuid, Guid remoteNonce, bool hashed, NSMessageHandler nsMessageHandler, P2PSession startupSession)
        {
            IPEndPoint ipep = cs.EndPoints[0];

            TCPv1Bridge tcpBridge = new TCPv1Bridge(cs, ver, replyGuid, remoteNonce, hashed, startupSession, nsMessageHandler, remote);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Trying to setup direct connection with remote host " + ipep.Address + ":" + ipep.Port.ToString(CultureInfo.InvariantCulture));

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

        private static IPEndPoint[] SelectIPEndPoint(MimeDictionary bodyValues, NSMessageHandler nsMessageHandler)
        {
            List<IPEndPoint> externalPoints = new List<IPEndPoint>();
            List<IPEndPoint> internalPoints = new List<IPEndPoint>();
            bool nat = false;

            #region External

            if (bodyValues.ContainsKey("IPv4External-Addrs") || bodyValues.ContainsKey("srddA-lanretxE4vPI") ||
                bodyValues.ContainsKey("IPv4ExternalAddrsAndPorts") || bodyValues.ContainsKey("stroPdnAsrddAlanretxE4vPI"))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Using external IP addresses");

                if (bodyValues.ContainsKey("IPv4External-Addrs"))
                {
                    string[] addrs = bodyValues["IPv4External-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] ports = bodyValues["IPv4External-Port"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (addrs.Length > 0 && ports.Length > 0)
                    {
                        IPAddress ip;
                        int port = 0;
                        int.TryParse(ports[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                        for (int i = 0; i < addrs.Length; i++)
                        {
                            if (IPAddress.TryParse(addrs[i], out ip))
                            {
                                if (i < ports.Length)
                                    int.TryParse(ports[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                                if (port > 0)
                                    externalPoints.Add(new IPEndPoint(ip, port));
                            }
                        }
                    }
                }
                else if (bodyValues.ContainsKey("IPv4ExternalAddrsAndPorts"))
                {
                    string[] addrsAndPorts = bodyValues["IPv4ExternalAddrsAndPorts"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    IPAddress ip;
                    foreach (string str in addrsAndPorts)
                    {
                        string[] addrAndPort = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (IPAddress.TryParse(addrAndPort[0], out ip))
                            externalPoints.Add(new IPEndPoint(ip, int.Parse(addrAndPort[1])));
                    }
                }
                else if (bodyValues.ContainsKey("srddA-lanretxE4vPI"))
                {
                    nat = true;

                    char[] revHost = bodyValues["srddA-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    string[] addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    string[] ports = new string(revPort).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (addrs.Length > 0 && ports.Length > 0)
                    {
                        IPAddress ip;
                        int port = 0;
                        int.TryParse(ports[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                        for (int i = 0; i < addrs.Length; i++)
                        {
                            if (IPAddress.TryParse(addrs[i], out ip))
                            {
                                if (i < ports.Length)
                                    int.TryParse(ports[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                                if (port > 0)
                                    externalPoints.Add(new IPEndPoint(ip, port));
                            }
                        }
                    }
                }
                else if (bodyValues.ContainsKey("stroPdnAsrddAlanretxE4vPI"))
                {
                    nat = true;

                    char[] rev = bodyValues["stroPdnAsrddAlanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(rev);
                    string[] addrsAndPorts = new string(rev).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    IPAddress ip;
                    foreach (string str in addrsAndPorts)
                    {
                        string[] addrAndPort = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                        if (IPAddress.TryParse(addrAndPort[0], out ip))
                            externalPoints.Add(new IPEndPoint(ip, int.Parse(addrAndPort[1])));
                    }
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                       String.Format("{0} external IP addresses found:", externalPoints.Count));

                foreach (IPEndPoint ipep in externalPoints)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + ipep.ToString());
                }
            }

            #endregion

            #region Internal

            if (bodyValues.ContainsKey("IPv4Internal-Addrs") || bodyValues.ContainsKey("srddA-lanretnI4vPI") ||
                bodyValues.ContainsKey("IPv4InternalAddrsAndPorts") || bodyValues.ContainsKey("stroPdnAsrddAlanretnI4vPI"))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Using internal IP addresses");

                if (bodyValues.ContainsKey("IPv4Internal-Addrs"))
                {
                    string[] addrs = bodyValues["IPv4Internal-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] ports = bodyValues["IPv4Internal-Port"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (addrs.Length > 0 && ports.Length > 0)
                    {
                        IPAddress ip;
                        int port = 0;
                        int.TryParse(ports[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                        for (int i = 0; i < addrs.Length; i++)
                        {
                            if (IPAddress.TryParse(addrs[i], out ip))
                            {
                                if (i < ports.Length)
                                    int.TryParse(ports[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                                if (port > 0)
                                    internalPoints.Add(new IPEndPoint(ip, port));
                            }
                        }
                    }
                }
                else if (bodyValues.ContainsKey("IPv4InternalAddrsAndPorts"))
                {
                    string[] addrsAndPorts = bodyValues["IPv4InternalAddrsAndPorts"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    IPAddress ip;
                    foreach (string str in addrsAndPorts)
                    {
                        string[] addrAndPort = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (IPAddress.TryParse(addrAndPort[0], out ip))
                            internalPoints.Add(new IPEndPoint(ip, int.Parse(addrAndPort[1])));
                    }
                }
                else if (bodyValues.ContainsKey("srddA-lanretnI4vPI"))
                {
                    nat = true;

                    char[] revHost = bodyValues["srddA-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    string[] addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    string[] ports = new string(revPort).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (addrs.Length > 0 && ports.Length > 0)
                    {
                        IPAddress ip;
                        int port = 0;
                        int.TryParse(ports[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                        for (int i = 0; i < addrs.Length; i++)
                        {
                            if (IPAddress.TryParse(addrs[i], out ip))
                            {
                                if (i < ports.Length)
                                    int.TryParse(ports[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out port);

                                if (port > 0)
                                    internalPoints.Add(new IPEndPoint(ip, port));
                            }
                        }
                    }
                }
                else if (bodyValues.ContainsKey("stroPdnAsrddAlanretnI4vPI"))
                {
                    nat = true;

                    char[] rev = bodyValues["stroPdnAsrddAlanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(rev);
                    string[] addrsAndPorts = new string(rev).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    IPAddress ip;
                    foreach (string str in addrsAndPorts)
                    {
                        string[] addrAndPort = str.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                        if (IPAddress.TryParse(addrAndPort[0], out ip))
                            internalPoints.Add(new IPEndPoint(ip, int.Parse(addrAndPort[1])));
                    }
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                       String.Format("{0} internal IP addresses found:", internalPoints.Count));

                foreach (IPEndPoint ipep in internalPoints)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + ipep.ToString());
                }
            }

            #endregion

            if (externalPoints.Count == 0 && internalPoints.Count == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Unable to find any remote IP addresses");

                // Failed
                return null;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Is NAT: " + nat);

            List<IPEndPoint> ret = new List<IPEndPoint>();

            // Try to find the correct IP
            byte[] localBytes = nsMessageHandler.LocalEndPoint.Address.GetAddressBytes();

            foreach (IPEndPoint ipep in internalPoints)
            {
                if (ipep.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // This is an IPv4 address
                    // Check if the first 3 octets match our local IP address
                    // If so, make use of that address (it's on our LAN)

                    byte[] bytes = ipep.Address.GetAddressBytes();

                    if ((bytes[0] == localBytes[0]) && (bytes[1] == localBytes[1]) && (bytes[2] == localBytes[2]))
                    {
                        ret.Add(ipep);
                        break;
                    }
                }
            }

            if (ret.Count == 0)
            {
                foreach (IPEndPoint ipep in internalPoints)
                {
                    if (!ret.Contains(ipep))
                        ret.Add(ipep);
                }
            }

            foreach (IPEndPoint ipep in externalPoints)
            {
                if (!ret.Contains(ipep))
                    ret.Add(ipep);
            }

            return ret.ToArray();
        }
    }
};
