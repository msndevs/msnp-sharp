using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace MSNPSharp.Core
{
    class Network
    {
        public static IPAddress DnsResolve(String host)
        {
            System.Net.IPHostEntry ipHostEntry = System.Net.Dns.GetHostEntry(host);
            foreach (IPAddress address in ipHostEntry.AddressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // return the ipv4 address
                    return address;
                }
            }
            if (ipHostEntry.AddressList.Length > 0)
            {
                // no ipv4 address; default to first entry
                return ipHostEntry.AddressList[0];
            }
            else
            {
                return null;
            }
        }
    }
}
