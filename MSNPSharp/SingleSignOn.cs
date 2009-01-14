#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
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
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MSNPSharp
{
    using MSNPSharp.SOAP;
    using MSNPSharp.MSNWS.MSNSecurityTokenService;
    using MSNPSharp.IO;

    [Flags]
    public enum SSOTicketType
    {
        None = 0x00,
        Clear = 0x01,
        Contact = 0x02,
        OIM = 0x04,
        Spaces = 0x08,
        Storage = 0x10,
        Web = 0x20
    }

    public enum ExpiryState
    {
        NotExpired,
        WillExpireSoon,
        Expired
    }

    #region MSNTicket

    [Serializable]
    public sealed class MSNTicket
    {
        public static readonly MSNTicket Empty = new MSNTicket(null);

        private string policy = "MBI_KEY_OLD";
        private string mainBrandID = "MSFT";
        private string oimLockKey = String.Empty;
        private string abServiceCacheKey = String.Empty;
        private string sharingServiceCacheKey = String.Empty;
        [NonSerialized]
        private SerializableDictionary<SSOTicketType, SSOTicket> ssoTickets = new SerializableDictionary<SSOTicketType, SSOTicket>();
        private SerializableDictionary<CacheKeyType, string> cacheKeys = new SerializableDictionary<CacheKeyType, string>(0);

        [NonSerialized]
        private int hashcode;
        [NonSerialized]
        internal int DeleteTick;

        internal MSNTicket(Credentials creds)
        {
            if (creds != null)
            {
                hashcode = (creds.Account.ToLowerInvariant() + creds.Password).GetHashCode();
                DeleteTick = unchecked(Environment.TickCount + (Settings.MSNTicketLifeTime * 60000)); // in minutes
            }
        }

        #region Properties

        #region CacheKey


        private void InitializeCacheKeys()
        {
            if (!cacheKeys.ContainsKey(CacheKeyType.OmegaContactServiceCacheKey))
            {
                cacheKeys.Add(CacheKeyType.OmegaContactServiceCacheKey, String.Empty);
            }

            if (!cacheKeys.ContainsKey(CacheKeyType.StorageServiceCacheKey))
            {
                cacheKeys.Add(CacheKeyType.StorageServiceCacheKey, String.Empty);
            }
        }

        /// <summary>
        /// CacheKeys for webservices.
        /// </summary>
        public SerializableDictionary<CacheKeyType, string> CacheKeys
        {
            get
            {
                InitializeCacheKeys();
                return cacheKeys;
            }
            set
            {
                cacheKeys = value;
            }
        }

        #endregion

        public SerializableDictionary<SSOTicketType, SSOTicket> SSOTickets
        {
            get
            {
                return ssoTickets;
            }
            set
            {
                ssoTickets = value;
            }
        }

        public string Policy
        {
            get
            {
                return policy;
            }
            set
            {
                policy = value;
            }
        }

        public string MainBrandID
        {
            get
            {
                return mainBrandID;
            }
            set
            {
                mainBrandID = value;
            }
        }

        public string OIMLockKey
        {
            get
            {
                return oimLockKey;
            }
            set
            {
                oimLockKey = value;
            }
        }

        public string ABServiceCacheKey
        {
            get
            {
                return abServiceCacheKey;
            }
            set
            {
                abServiceCacheKey = value;
            }
        }

        public string SharingServiceCacheKey
        {
            get
            {
                return sharingServiceCacheKey;
            }
            set
            {
                sharingServiceCacheKey = value;
            }
        }

        #endregion

        public ExpiryState Expired(SSOTicketType tt)
        {
            if (SSOTickets.ContainsKey(tt))
            {
                if (SSOTickets[tt].Expires < DateTime.Now)
                    return ExpiryState.Expired;

                return (SSOTickets[tt].Expires < DateTime.Now.AddMinutes(1)) ? ExpiryState.WillExpireSoon : ExpiryState.NotExpired;
            }
            return ExpiryState.Expired;
        }

        public override int GetHashCode()
        {
            return hashcode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return GetHashCode() == ((MSNTicket)obj).GetHashCode();
        }
    }

    #endregion

    #region SSOTicket

    public class SSOTicket
    {
        private String domain = String.Empty;
        private String ticket = String.Empty;
        private String binarySecret = String.Empty;
        private DateTime created = DateTime.MinValue;
        private DateTime expires = DateTime.MinValue;
        private SSOTicketType type = SSOTicketType.None;

        internal SSOTicket()
        {
        }

        public SSOTicket(SSOTicketType tickettype)
        {
            type = tickettype;
        }

        public String Domain
        {
            get
            {
                return domain;
            }
            set
            {
                domain = value;
            }
        }

        public String Ticket
        {
            get
            {
                return ticket;
            }
            set
            {
                ticket = value;
            }
        }

        public String BinarySecret
        {
            get
            {
                return binarySecret;
            }
            set
            {
                binarySecret = value;
            }
        }

        public DateTime Created
        {
            get
            {
                return created;
            }
            set
            {
                created = value;
            }
        }

        public DateTime Expires
        {
            get
            {
                return expires;
            }
            set
            {
                expires = value;
            }
        }

        public SSOTicketType TicketType
        {
            get
            {
                return type;
            }
        }
    }

    #endregion

    #region SingleSignOnManager

    internal static class SingleSignOnManager
    {
        private static Dictionary<int, MSNTicket> cache = new Dictionary<int, MSNTicket>();
        private static DateTime nextCleanup = NextCleanupTime();
        private static object syncObject;
        private static object SyncObject
        {
            get
            {
                if (syncObject == null)
                {
                    object newobj = new object();
                    Interlocked.CompareExchange(ref syncObject, newobj, null);
                }

                return syncObject;
            }
        }

        private static DateTime NextCleanupTime()
        {
            return DateTime.Now.AddMinutes(Settings.MSNTicketsCleanupInterval);
        }

        private static void CheckCleanup()
        {
            if (nextCleanup < DateTime.Now)
            {
                lock (SyncObject)
                {
                    if (nextCleanup < DateTime.Now)
                    {
                        nextCleanup = NextCleanupTime();
                        int tickcount = Environment.TickCount;
                        List<int> cachestodelete = new List<int>();
                        foreach (MSNTicket t in cache.Values)
                        {
                            if (t.DeleteTick != 0 && t.DeleteTick < tickcount)
                            {
                                cachestodelete.Add(t.GetHashCode());
                            }
                        }
                        if (cachestodelete.Count > 0)
                        {
                            foreach (int i in cachestodelete)
                            {
                                cache.Remove(i);
                            }
                            GC.Collect();
                        }
                    }
                }
            }
        }

        internal static void Authenticate(NSMessageHandler nsMessageHandler, string policy)
        {
            CheckCleanup();

            if (nsMessageHandler != null)
            {
                int hashcode = (nsMessageHandler.Credentials.Account.ToLowerInvariant() + nsMessageHandler.Credentials.Password).GetHashCode();
                MSNTicket ticket = cache.ContainsKey(hashcode) ? cache[hashcode] : new MSNTicket(nsMessageHandler.Credentials);
                SSOTicketType[] ssos = (SSOTicketType[])Enum.GetValues(typeof(SSOTicketType));
                SSOTicketType expiredtickets = SSOTicketType.None;

                foreach (SSOTicketType ssot in ssos)
                {
                    if (ExpiryState.NotExpired != ticket.Expired(ssot))
                        expiredtickets |= ssot;
                }

                if (expiredtickets == SSOTicketType.None)
                {
                    nsMessageHandler.MSNTicket = ticket;
                }
                else
                {
                    SingleSignOn sso = new SingleSignOn(nsMessageHandler.Credentials.Account, nsMessageHandler.Credentials.Password, policy);
                    if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
                        sso.WebProxy = nsMessageHandler.ConnectivitySettings.WebProxy;

                    sso.AddAuths(expiredtickets);
                    sso.Authenticate(ticket, false);

                    cache[hashcode] = ticket;
                    nsMessageHandler.MSNTicket = ticket;
                }
            }
        }

        internal static void RenewIfExpired(NSMessageHandler nsMessageHandler, SSOTicketType renew)
        {
            CheckCleanup();

            if (nsMessageHandler != null)
            {
                int hashcode = (nsMessageHandler.Credentials.Account.ToLowerInvariant() + nsMessageHandler.Credentials.Password).GetHashCode();
                MSNTicket ticket = cache.ContainsKey(hashcode) ? cache[hashcode] : new MSNTicket(nsMessageHandler.Credentials);
                ExpiryState es = ticket.Expired(renew);

                if (ExpiryState.NotExpired != es)
                {
                    SingleSignOn sso = new SingleSignOn(nsMessageHandler.Credentials.Account, nsMessageHandler.Credentials.Password, ticket.Policy);
                    if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
                        sso.WebProxy = nsMessageHandler.ConnectivitySettings.WebProxy;

                    sso.AddAuths(renew);

                    if (es == ExpiryState.WillExpireSoon)
                    {
                        sso.Authenticate(ticket, true);
                    }
                    else
                    {
                        sso.Authenticate(ticket, false);
                        cache[hashcode] = ticket;
                    }
                }

                nsMessageHandler.MSNTicket = ticket;
            }
        }
    }

    #endregion

    #region SingleSignOn

    public class SingleSignOn
    {
        private string user;
        private string pass;
        private string policy;
        private int authId;
        private List<RequestSecurityTokenType> auths = new List<RequestSecurityTokenType>(0);
        private WebProxy webProxy;

        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
            set
            {
                webProxy = value;
            }
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
            AuthenticationAdd("contacts.msn.com", "MBI");
            AuthenticationAdd("messengersecure.live.com", "MBI_SSL");
            AuthenticationAdd("spaces.live.com", "MBI");
            AuthenticationAdd("storage.msn.com", "MBI");
        }

        public void AddAuths(SSOTicketType ssott)
        {
            AuthenticationAdd("http://Passport.NET/tb", null);

            SSOTicketType[] ssos = (SSOTicketType[])Enum.GetValues(typeof(SSOTicketType));

            foreach (SSOTicketType sso in ssos)
            {
                switch (sso & ssott)
                {
                    case SSOTicketType.Contact:
                        AuthenticationAdd("contacts.msn.com", "MBI");
                        break;

                    case SSOTicketType.OIM:
                        AuthenticationAdd("messengersecure.live.com", "MBI_SSL");
                        break;

                    case SSOTicketType.Spaces:
                        AuthenticationAdd("spaces.live.com", "MBI");
                        break;

                    case SSOTicketType.Clear:
                        AuthenticationAdd("messengerclear.live.com", policy);
                        break;

                    case SSOTicketType.Storage:
                        AuthenticationAdd("storage.msn.com", "MBI");
                        break;

                    case SSOTicketType.Web:
                        AuthenticationAdd("messenger.msn.com", "?id=507");
                        break;
                }
            }
        }

        public void Authenticate(MSNTicket msnticket, bool async)
        {
            MSNSecurityServiceSoapClient securService = new MSNSecurityServiceSoapClient(); //It is a hack
            securService.Timeout = 60000;
            securService.Proxy = webProxy;
            securService.AuthInfo = new AuthInfoType();
            securService.AuthInfo.Id = "PPAuthInfo";
            securService.AuthInfo.HostingApp = Properties.Resources.HostingApp;
            securService.AuthInfo.BinaryVersion = "4";
            securService.AuthInfo.Cookies = string.Empty;
            securService.AuthInfo.UIVersion = "1";
            securService.AuthInfo.RequestParams = "AQAAAAIAAABsYwQAAAAxMDU1";

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

            if (async)
            {
                securService.RequestMultipleSecurityTokensCompleted += delegate(object sender, RequestMultipleSecurityTokensCompletedEventArgs e)
                {
                    if (!e.Cancelled)
                    {
                        securService = sender as MSNSecurityServiceSoapClient;
                        if (e.Error != null)
                        {
                            MSNPSharpException sexp = new MSNPSharpException(e.Error.Message + ". See innerexception for detail.", e.Error);
                            if (securService.pp != null)
                                sexp.Data["Code"] = securService.pp.reqstatus;  //Error code

                            throw sexp;
                        }

                        GetTickets(e.Result, securService, msnticket);
                    }
                };
                securService.RequestMultipleSecurityTokensAsync(mulToken, new object());
            }
            else
            {
                RequestSecurityTokenResponseType[] result = null;
                try
                {
                    result = securService.RequestMultipleSecurityTokens(mulToken);
                }
                catch (Exception ex)
                {
                    MSNPSharpException sexp = new MSNPSharpException(ex.Message + ". See innerexception for detail.", ex);
                    if (securService.pp != null)
                        sexp.Data["Code"] = securService.pp.reqstatus;  //Error code

                    throw sexp;
                }

                GetTickets(result, securService, msnticket);
            }
        }

        private static void GetTickets(RequestSecurityTokenResponseType[] result, MSNSecurityServiceSoapClient securService, MSNTicket msnticket)
        {
            if (securService.pp != null && securService.pp.credProperties != null)
            {
                foreach (credPropertyType credproperty in securService.pp.credProperties)
                {
                    if (credproperty.Name == "MainBrandID")
                    {
                        msnticket.MainBrandID = credproperty.Value;
                        break;
                    }
                }
            }

            foreach (RequestSecurityTokenResponseType token in result)
            {
                SSOTicketType ticketype = SSOTicketType.None;
                switch (token.AppliesTo.EndpointReference.Address)
                {
                    case "messenger.msn.com":
                        ticketype = SSOTicketType.Web;
                        break;
                    case "messengersecure.live.com":
                        ticketype = SSOTicketType.OIM;
                        break;
                    case "contacts.msn.com":
                        ticketype = SSOTicketType.Contact;
                        break;
                    case "messengerclear.live.com":
                        ticketype = SSOTicketType.Clear;
                        break;
                    case "spaces.live.com":
                        ticketype = SSOTicketType.Spaces;
                        break;
                    case "storage.msn.com":
                        ticketype = SSOTicketType.Storage;
                        break;
                }

                SSOTicket ssoticket = new SSOTicket(ticketype);
                ssoticket.Domain = token.AppliesTo.EndpointReference.Address;
                ssoticket.Ticket = token.RequestedSecurityToken.InnerText;
                if (token.RequestedProofToken != null && token.RequestedProofToken.BinarySecret != null)
                {
                    ssoticket.BinarySecret = token.RequestedProofToken.BinarySecret.Value;
                }
                if (token.LifeTime != null)
                {
                    // Format : "yyyy-MM-ddTHH:mm:ssZ"
                    ssoticket.Created = XmlConvert.ToDateTime(token.LifeTime.Created.Value, "yyyy-MM-ddTHH:mm:ssZ");
                    ssoticket.Expires = XmlConvert.ToDateTime(token.LifeTime.Expires.Value, "yyyy-MM-ddTHH:mm:ssZ");
                }

                msnticket.SSOTickets[ticketype] = ssoticket;
            }

        }
    }

    #endregion

    #region MBI

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
            byte[] iv = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            RNGCryptoServiceProvider.Create().GetBytes(iv);
            byte[] fillbyt = new byte[8] { 8, 8, 8, 8, 8, 8, 8, 8 };
            TripleDES des3 = TripleDES.Create();
            des3.Mode = CipherMode.CBC;
            byte[] desinput = CombinByte(Encoding.ASCII.GetBytes(nonce), fillbyt);
            byte[] deshash = new byte[72];
            des3.CreateEncryptor(key3, iv).TransformBlock(desinput, 0, desinput.Length, deshash, 0);
            return Convert.ToBase64String(CombinByte(CombinByte(CombinByte(tagMSGRUSRKEY_struct, iv), hash), deshash));
        }

        private static byte[] Derive_Key(byte[] key, byte[] magic)
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

        private static byte[] CombinByte(byte[] front, byte[] follow)
        {
            byte[] byt = new byte[front.Length + follow.Length];
            front.CopyTo(byt, 0);
            follow.CopyTo(byt, front.Length);
            return byt;
        }
    }

    #endregion
};
