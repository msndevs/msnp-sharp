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
using System.Globalization;
using MSNPSharp.MSNSecurityTokenService;
using MSNPSharp.SOAP;
using System.Web.Services.Protocols;

namespace MSNPSharp
{
    public enum Iniproperties
    {
        BrandID,
        OIMTicket,
        BinarySecret,
        Ticket,
        WebTicket,
        ContactTicket,
        LockKey,
        AddressBookCacheKey,
        SharingServiceCacheKey
    }

    public class SingleSignOn
    {
        private string user;
        private string pass;
        private string policy;
        private int authId = 0;
        private List<RequestSecurityTokenType> auths = new List<RequestSecurityTokenType>(0);
        private WebProxy webProxy = null;

        public WebProxy WebProxy
        {
            get { return webProxy; }
            set { webProxy = value; }
        }

        public SingleSignOn(string username, string password, string policy)
        {
            this.user = username;
            this.pass = password;
            this.policy = policy;

        }

        public void AuthenticationAdd(string domain, string policyref)
        {
            RequestSecurityTokenType requestToken = new RequestSecurityTokenType();
            requestToken.Id = "RST" + authId.ToString();
            requestToken.RequestType = RequestTypeOpenEnum.httpschemasxmlsoaporgws200404securitytrustIssue;
            requestToken.AppliesTo = new AppliesTo();
            requestToken.AppliesTo.EndpointReference = new EndpointReferenceType();
            requestToken.AppliesTo.EndpointReference.Address = domain;
            if (policyref != null)
            {
                requestToken.PolicyReference = new PolicyReference();
                requestToken.PolicyReference.URI = policyref;
            }

            auths.Add(requestToken);

            authId++;
        }

        public void AddDefaultAuths()
        {
            AuthenticationAdd(@"http://Passport.NET/tb", null);
            AuthenticationAdd("messengerclear.live.com", policy);
            AuthenticationAdd("messenger.msn.com", "?id=507");
            AuthenticationAdd("contacts.msn.com", "?fs=1&id=24000&kv=9&rn=93S9SWWw&tw=0&ver=2.1.6000.1");//("?fs=1&id=24000&kv=9&rn=93S9SWWw&tw=0&ver=2.1.6000.1").Replace(@"&", @"&amp;"));
            AuthenticationAdd("messengersecure.live.com", "MBI_SSL");
            AuthenticationAdd("livecontacts.live.com", "MBI");
        }

        public string Authenticate(string nonce, out Dictionary<Iniproperties, string> tickets)
        {
            MSNSecurityServiceSoapClient securService = new MSNSecurityServiceSoapClient(); //It is a hack
            securService.Proxy = webProxy;
            securService.AuthInfo = new AuthInfoType();
            securService.AuthInfo.Id = "PPAuthInfo";
            securService.AuthInfo.HostingApp = "{7108E71A-9926-4FCB-BCC9-9A9D3F32E423}";
            securService.AuthInfo.BinaryVersion = "4";
            securService.AuthInfo.Cookies = string.Empty;
            securService.AuthInfo.UIVersion = "1";
            securService.AuthInfo.RequestParams = "AQAAAAIAAABsYwQAAAAxMDMz";

            securService.Security = new SecurityHeaderType();
            securService.Security.UsernameToken = new UsernameTokenType();
            securService.Security.UsernameToken.Id = "user";
            securService.Security.UsernameToken.Username = new AttributedString();
            securService.Security.UsernameToken.Username.Value = user;
            securService.Security.UsernameToken.Password = new PasswordString();
            securService.Security.UsernameToken.Password.Value = pass;

            if (user.Split('@').Length > 1)
            {
                if (user.Split('@')[1].ToLower(CultureInfo.InvariantCulture) == "msn.com")
                {
                    securService.Url = @"https://msnia.login.live.com/pp550/RST.srf";
                }
            }
            else
            {
                throw new AuthenticationException("Invalid account");
            }

            RequestMultipleSecurityTokensType mulToken = new RequestMultipleSecurityTokensType();
            mulToken.Id = "RSTS";
            mulToken.RequestSecurityToken = auths.ToArray();
            RequestSecurityTokenResponseType[] result = null;
            try
            {
                result = securService.RequestMultipleSecurityTokens(mulToken);
            }
            catch (Exception ex)
            {
                MSNPSharpException sexp= new MSNPSharpException(ex.Message + ". See innerexception for detail.",ex);
                sexp.Data["Code"] = securService.pp.reqstatus;  //Error code
                throw sexp;
            }

            tickets = new Dictionary<Iniproperties, string>(0);
            if (securService.pp != null && securService.pp.credProperties != null)
            {
                foreach (credPropertyType credproperty in securService.pp.credProperties)
                {
                    if (credproperty.Name == "MainBrandID")
                    {
                        tickets[Iniproperties.BrandID] = credproperty.Value;
                        break;
                    }
                }
            }
            foreach (RequestSecurityTokenResponseType token in result)
            {
                switch (token.AppliesTo.EndpointReference.Address)
                {
                    case "messenger.msn.com":
                        tickets[Iniproperties.WebTicket] = token.RequestedSecurityToken.InnerText;
                        break;
                    case "messengersecure.live.com":
                        tickets[Iniproperties.OIMTicket] = token.RequestedSecurityToken.InnerText;
                        break;
                    case "contacts.msn.com":
                        tickets[Iniproperties.ContactTicket] = token.RequestedSecurityToken.InnerText;
                        break;
                    case "messengerclear.live.com":
                        tickets[Iniproperties.Ticket] = token.RequestedSecurityToken.InnerText;
                        tickets[Iniproperties.BinarySecret] = token.RequestedProofToken.BinarySecret.Value;
                        break;
                }
            }

            MBI mbi = new MBI();
            return tickets[Iniproperties.Ticket] + " " + mbi.Encrypt(tickets[Iniproperties.BinarySecret], nonce);
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
