#region Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com>,Thiago M. Sayão <thiago.sayao@gmail.com>
/*
Copyright (c) 2007-2008 Pang Wu <freezingsoft@hotmail.com>,Thiago M. Sayão <thiago.sayao@gmail.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without 
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright 
notice, this list of conditions and the following disclaimer in the 
documentation and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its 
contributors may be used to endorse or promote products derived 
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE. */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Net;
using System.IO;
using System.Security.Authentication;
using System.Xml;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace MSNPSharp
{
    /*
     * I know.. using text sux.. i tried to use proxy classes to send the soap request, but the server just accepts this format..
     * Not my fault, blame M$ :)
     */
    public class SingleSignOn
    {
        string user;
        string pass;
        string policy;
        int auth_count = 1;

        List<string> auths;

        public SingleSignOn(string username, string password, string policy)
        {
            this.user = username;
            this.pass = password;
            this.policy = policy;

            auths = new List<string>();
        }

        public void AuthenticationAdd(string domain, string policyref)
        {
            string soapenvelop = "<wst:RequestSecurityToken Id=\"RST{0}\">"
                                + "<wst:RequestType>http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue</wst:RequestType>"
                                + "<wsp:AppliesTo>"
                                + "<wsa:EndpointReference>"
                                + "<wsa:Address>{1}</wsa:Address>"
                                + "</wsa:EndpointReference>"
                                + "</wsp:AppliesTo>"
                                + "<wsse:PolicyReference URI=\"{2}\"></wsse:PolicyReference></wst:RequestSecurityToken>";


            string auth = String.Format(soapenvelop, auth_count, domain, policyref);

            auths.Add(auth);

            auth_count++;
        }

        public void AddDefaultAuths()
        {
            AuthenticationAdd("messengerclear.live.com", policy);
            AuthenticationAdd("messenger.msn.com", "?id=507");
            AuthenticationAdd("contacts.msn.com", ("?fs=1&id=24000&kv=9&rn=93S9SWWw&tw=0&ver=2.1.6000.1").Replace(@"&", @"&amp;"));
            AuthenticationAdd("messengersecure.live.com", "MBI_SSL");
            AuthenticationAdd("livecontacts.live.com", "MBI");
        }

        public string Authenticate(string nonce, out Dictionary<string, string> tickets)
        {
            string soapenvelop = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>"
                                + "<Envelope xmlns=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:wsse=\"http://schemas.xmlsoap.org/ws/2003/06/secext\" xmlns:saml=\"urn:oasis:names:tc:SAML:1.0:assertion\" xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2002/12/policy\" xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" xmlns:wsa=\"http://schemas.xmlsoap.org/ws/2004/03/addressing\" xmlns:wssc=\"http://schemas.xmlsoap.org/ws/2004/04/sc\" xmlns:wst=\"http://schemas.xmlsoap.org/ws/2004/04/trust\">"
                                + "<Header>"
                                + "<ps:AuthInfo xmlns:ps=\"http://schemas.microsoft.com/Passport/SoapServices/PPCRL\" Id=\"PPAuthInfo\">"
                                + "<ps:HostingApp>{{7108E71A-9926-4FCB-BCC9-9A9D3F32E423}}</ps:HostingApp>"
                                + "<ps:BinaryVersion>4</ps:BinaryVersion>"
                                + "<ps:UIVersion>1</ps:UIVersion>"
                                + "<ps:Cookies></ps:Cookies>"
                                + "<ps:RequestParams>AQAAAAIAAABsYwQAAAAxMDMz</ps:RequestParams>"
                                + "</ps:AuthInfo>"
                                + "<wsse:Security><wsse:UsernameToken Id=\"user\">"
                                + "<wsse:Username>{0}</wsse:Username>"
                                + "<wsse:Password>{1}</wsse:Password>"
                                + "</wsse:UsernameToken></wsse:Security>"
                                + "</Header><Body>"
                                + "<ps:RequestMultipleSecurityTokens xmlns:ps=\"http://schemas.microsoft.com/Passport/SoapServices/PPCRL\" Id=\"RSTS\">"
                                + "<wst:RequestSecurityToken Id=\"RST0\">"
                                + "<wst:RequestType>http://schemas.xmlsoap.org/ws/2004/04/security/trust/Issue</wst:RequestType>"
                                + "<wsp:AppliesTo><wsa:EndpointReference>"
                                + "<wsa:Address>http://Passport.NET/tb</wsa:Address>"
                                + "</wsa:EndpointReference>"
                                + "</wsp:AppliesTo>"
                                + "</wst:RequestSecurityToken>{2}</ps:RequestMultipleSecurityTokens>"
                                + "</Body></Envelope>";


            string reqs = String.Join("", auths.ToArray());
            string env = String.Format(soapenvelop, user, pass, reqs);

            HttpWebRequest req;
            HttpWebResponse rsp;

            if (user.Split('@').Length > 1)
            {
                if (user.Split('@')[1].ToLower() == "msn.com")
                {
                    req = (HttpWebRequest)WebRequest.Create(@"https://msnia.login.live.com/pp550/RST.srf");
                }
                else
                {
                    req = (HttpWebRequest)WebRequest.Create("https://login.live.com/RST.srf");
                }
            }
            else
            {
                throw new AuthenticationException("Invalid account");
            }

            req.Accept = "text/*";
            req.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)";
            req.KeepAlive = true;

            byte[] dat = Encoding.UTF8.GetBytes(env);

            req.ContentLength = dat.LongLength;
            req.Method = "POST";
            req.ProtocolVersion = HttpVersion.Version11;

            Stream s = req.GetRequestStream();
            s.Write(dat, 0, dat.Length);
            s.Close();

            //try the request
            try
            {
                rsp = (HttpWebResponse)req.GetResponse();
            }
            catch
            {
                throw new AuthenticationException("Request error");
            }

            //load the xml
            XmlDocument xml = new XmlDocument();
            s = rsp.GetResponseStream();
            xml.Load(s);

            s.Close();
            rsp.Close();

            if (xml.InnerText.IndexOf("Invalid Request") != -1)
                throw new AuthenticationException("Invalid Request");

            string ticket = "", key = "", outticket = "";
            Dictionary<string, string> dic = new Dictionary<string, string>();
            try
            {
                XmlNodeList nodes = xml.GetElementsByTagName("wsse:BinarySecurityToken");
                XmlNodeList knodes = null;
                int nodecnt = 0;
                
                for (nodecnt = 0; nodecnt < nodes.Count; nodecnt++)
                {
                    if (nodes[nodecnt].Attributes[0].Value.ToLower() == "compact1")
                    {
                        ticket = nodes[nodecnt].InnerText;

                        knodes = xml.GetElementsByTagName("wst:BinarySecret");
                        key = knodes[nodecnt + 1].InnerText;
                        //break;
                    }
                    if (nodes[nodecnt].Attributes[0].Value.ToLower() == "pptoken3")
                    {
                        outticket = nodes[nodecnt].InnerText;
                    }

                    if (nodes[nodecnt].Attributes[0].Value.ToLower() == "pptoken2")
                    {
                        dic.Add("web_ticket", nodes[nodecnt].InnerText);
                    }

                    if (nodes[nodecnt].Attributes[0].Value.ToLower() == "compact4")
                    {
                        dic.Add("oim_ticket", nodes[nodecnt].InnerText);
                    }
                }
            }
            catch
            {
                throw new AuthenticationException("Invalid Response");
            }

            MBI mbi = new MBI();

 
            dic.Add("contact_ticket", outticket);
            dic.Add("ticket", ticket);
            dic.Add("BinarySecret", key);

            tickets = dic;

            return ticket + " " + mbi.Encrypt(key, nonce);

        }
    }

    /// <summary>
    /// MBI encrypt algorithm class
    /// </summary>
    public class MBI
    {

        private byte[] tagMSGRUSRKEY_struct = new byte[28]
        {

              //uStructHeaderSize = 28
              0x1c,0x00,0x00,0x00,

              //uCryptMode = 1
              0x01,0x00,0x00,0x00,

              //uCipherType = 0x6603
              0x03,0x66,0x00,0x00,

              //uHashType = 0x8004
              0x04,0x80,0x00,0x00,

              //uIVLen = 8
              0x08,0x00,0x00,0x00,

              //uHashLen = 20
              0x14,0x00,0x00,0x00,

              //uCipherLen = 72
              0x48,0x00,0x00,0x00
        };

        /// <summary>
        /// Get the encrypted string
        /// </summary>
        /// <param name="key">The BinarySecret</param>
        /// <param name="nonce">Nonce get from server</param>
        /// <returns></returns>
        public string Encrypt(string key, string nonce)
        {
            byte[] key1 = Convert.FromBase64String(key);
            byte[] key2 = Derive_Key(key1, Encoding.ASCII.GetBytes("WS-SecureConversationSESSION KEY HASH"));
            byte[] key3 = Derive_Key(key1, Encoding.ASCII.GetBytes("WS-SecureConversationSESSION KEY ENCRYPTION"));
            byte[] hash = (new HMACSHA1(key2)).ComputeHash(Encoding.ASCII.GetBytes(nonce));
            byte[] iv =new byte[8]{ 0,0,0,0,0,0,0,0 };
            RNGCryptoServiceProvider.Create().GetBytes(iv);
            byte[] fillbyt = new byte[8] { 8, 8, 8, 8, 8, 8, 8, 8 };
            TripleDES des3 = TripleDES.Create();
            des3.Mode = CipherMode.CBC;
            byte[] desinput = CombinByte(Encoding.ASCII.GetBytes(nonce), fillbyt);
            byte[] deshash = new byte[72];
            int descount = des3.CreateEncryptor(key3,iv).TransformBlock(desinput, 0, desinput.Length, deshash, 0);
            byte[] result = CombinByte(CombinByte(CombinByte(tagMSGRUSRKEY_struct, iv), hash), deshash);
            return Convert.ToBase64String(result);
        }

        private byte[] Derive_Key(byte[] key, byte[] magic)
        {
            HMACSHA1 hmac = new HMACSHA1(key);
            byte[] hash1 = hmac.ComputeHash(magic);
            byte[] hash2 = hmac.ComputeHash(CombinByte(hash1, magic));
            byte[] hash3 = hmac.ComputeHash(hash1);
            byte[] hash4 = hmac.ComputeHash(CombinByte(hash3, magic));
            byte[] outbyt = new byte[4];
            Array.Copy(hash4, outbyt, outbyt.Length);
            return CombinByte(hash2, outbyt);

        }

        private byte[] CombinByte(byte[] front, byte[] follow)
        {
            byte[] byt = new byte[front.Length + follow.Length];
            front.CopyTo(byt, 0);
            follow.CopyTo(byt, front.Length);
            return byt;
        }
 
    }
}
