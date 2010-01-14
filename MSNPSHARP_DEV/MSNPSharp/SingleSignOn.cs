#region Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2010, Bas Geertsema, Xih Solutions
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
using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MSNPSharp
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;
    using MSNPSharp.IO;

    [Flags]
    public enum SSOTicketType
    {
        None = 0x00,
        Clear = 0x01,
        Contact = 0x02,
        OIM = 0x04,
        Storage = 0x10,
        Web = 0x20,
        WhatsUp = 0x40
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

        [NonSerialized]
        private SerializableDictionary<SSOTicketType, SSOTicket> ssoTickets = new SerializableDictionary<SSOTicketType, SSOTicket>();
        private SerializableDictionary<CacheKeyType, string> cacheKeys = new SerializableDictionary<CacheKeyType, string>(0);

        [NonSerialized]
        private int hashcode;
        [NonSerialized]
        internal int DeleteTick;

        public MSNTicket()
        {
        }

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

            internal set
            {
                type = value;
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

        internal static void Authenticate(
            NSMessageHandler nsMessageHandler,
            string policy,
            EventHandler onSuccess,
            EventHandler<ExceptionEventArgs> onError)
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

                    if (onSuccess != null)
                    {
                        onSuccess(nsMessageHandler, EventArgs.Empty);
                    }
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Request new tickets: " + expiredtickets, "SingleSignOnManager");

                    SingleSignOn sso = new SingleSignOn(nsMessageHandler.Credentials.Account, nsMessageHandler.Credentials.Password, policy);
                    if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
                        sso.WebProxy = nsMessageHandler.ConnectivitySettings.WebProxy;

                    sso.AddAuths(expiredtickets);

                    if (onSuccess == null && onError == null)
                    {
                        sso.Authenticate(ticket, false);
                        cache[hashcode] = ticket;
                        nsMessageHandler.MSNTicket = ticket;
                    }
                    else
                    {
                        try
                        {
                            sso.Authenticate(ticket, true,
                                delegate(object sender, EventArgs e)
                                {
                                    cache[hashcode] = ticket;
                                    nsMessageHandler.MSNTicket = ticket;

                                    if (onSuccess != null)
                                    {
                                        onSuccess(nsMessageHandler, e);
                                    }
                                },
                                delegate(object sender, ExceptionEventArgs e)
                                {
                                    if (onError != null)
                                    {
                                        onError(nsMessageHandler, e);
                                    }
                                });
                        }
                        catch (Exception error)
                        {
                            if (onError != null)
                            {
                                onError(nsMessageHandler, new ExceptionEventArgs(error));
                            }
                        }
                    }
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
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Re-new ticket: " + renew, "SingleSignOnManager");

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
            requestToken.RequestType = RequestTypeOpenEnum.httpschemasxmlsoaporgws200502trustIssue;
            requestToken.AppliesTo = new AppliesTo();
            requestToken.AppliesTo.EndpointReference = new EndpointReferenceType();
            requestToken.AppliesTo.EndpointReference.Address = new AttributedURIType();
            requestToken.AppliesTo.EndpointReference.Address.Value = domain;

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
            AddAuths(SSOTicketType.Clear | SSOTicketType.Contact | SSOTicketType.OIM | SSOTicketType.Storage | SSOTicketType.Web | SSOTicketType.WhatsUp);
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

                    case SSOTicketType.Clear:
                        AuthenticationAdd("messengerclear.live.com", policy);
                        break;

                    case SSOTicketType.Storage:
                        AuthenticationAdd("storage.msn.com", "MBI");
                        break;

                    case SSOTicketType.Web:
                        AuthenticationAdd("messenger.msn.com", "?id=507");
                        break;

                    case SSOTicketType.WhatsUp:
                        AuthenticationAdd("sup.live.com", "MBI");
                        break;
                }
            }
        }


        public void Authenticate(MSNTicket msnticket, bool async)
        {
            Authenticate(msnticket, async, null, null);
        }

        public void Authenticate(MSNTicket msnticket, bool async, EventHandler onSuccess, EventHandler<ExceptionEventArgs> onError)
        {
            SecurityTokenService securService = new SecurityTokenService();
            // securService.EnableDecompression = true; // Fails on Mono.
            securService.Timeout = 60000;
            securService.Proxy = webProxy;
            securService.AuthInfo = new AuthInfoType();
            securService.AuthInfo.Id = "PPAuthInfo";
            securService.AuthInfo.HostingApp = "{7108E71A-9926-4FCB-BCC9-9A9D3F32E423}";
            securService.AuthInfo.BinaryVersion = "5";
            securService.AuthInfo.Cookies = string.Empty;
            securService.AuthInfo.UIVersion = "1";
            securService.AuthInfo.RequestParams = "AQAAAAIAAABsYwQAAAAyMDUy";

            securService.Security = new SecurityHeaderType();
            securService.Security.UsernameToken = new UsernameTokenType();
            securService.Security.UsernameToken.Id = "user";
            securService.Security.UsernameToken.Username = new AttributedString();
            securService.Security.UsernameToken.Username.Value = user;
            securService.Security.UsernameToken.Password = new PasswordString();
            securService.Security.UsernameToken.Password.Value = pass;

            DateTime now = DateTime.Now.ToUniversalTime();
            DateTime begin = (new DateTime(1970, 1, 1));   //Already UTC time, no need to convert
            TimeSpan span = now - begin;

            securService.Security.Timestamp = new TimestampType();
            securService.Security.Timestamp.Id = "Timestamp";
            securService.Security.Timestamp.Created = new AttributedDateTime();
            securService.Security.Timestamp.Created.Value = XmlConvert.ToString(now, "yyyy-MM-ddTHH:mm:ssZ");
            securService.Security.Timestamp.Expires = new AttributedDateTime();
            securService.Security.Timestamp.Expires.Value = XmlConvert.ToString(now.AddMinutes(Settings.MSNTicketLifeTime), "yyyy-MM-ddTHH:mm:ssZ");

            securService.MessageID = new AttributedURIType();
            securService.MessageID.Value = ((int)span.TotalSeconds).ToString();

            securService.ActionValue = new Action();
            securService.ActionValue.MustUnderstand = true;
            securService.ActionValue.Value = @"http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";

            securService.ToValue = new To();
            securService.ToValue.MustUnderstand = true;
            securService.ToValue.Value = @"HTTPS://login.live.com:443//RST2.srf";  //There are two "/" here

            if (user.Split('@').Length > 1)
            {
                if (user.Split('@')[1].ToLower(CultureInfo.InvariantCulture) == "msn.com")
                {
                    securService.Url = @"https://msnia.login.live.com/RST2.srf";
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
                        if (e.Error != null)
                        {
                            MSNPSharpException sexp = new MSNPSharpException(e.Error.Message + ". See innerexception for detail.", e.Error);
                            if (securService.pp != null)
                                sexp.Data["Code"] = securService.pp.reqstatus;  //Error code

                            if (onError == null)
                            {
                                throw sexp;
                            }
                            else
                            {
                                onError(this, new ExceptionEventArgs(sexp));
                            }

                            return;
                        }

                        GetTickets(e.Result, securService, msnticket);

                        if (onSuccess != null)
                        {
                            onSuccess(this, EventArgs.Empty);
                        }
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

        private void GetTickets(RequestSecurityTokenResponseType[] result, SecurityTokenService securService, MSNTicket msnticket)
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
                switch (token.AppliesTo.EndpointReference.Address.Value)
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
                    case "storage.msn.com":
                        ticketype = SSOTicketType.Storage;
                        break;
                    case "sup.live.com":
                        ticketype = SSOTicketType.WhatsUp;
                        break;
                }

                SSOTicket ssoticket = new SSOTicket(ticketype);
                if (token.AppliesTo != null)
                    ssoticket.Domain = token.AppliesTo.EndpointReference.Address.Value;
                if (token.RequestedSecurityToken.BinarySecurityToken != null)
                    ssoticket.Ticket = token.RequestedSecurityToken.BinarySecurityToken.Value;
                if (token.RequestedProofToken != null && token.RequestedProofToken.BinarySecret != null)
                {
                    ssoticket.BinarySecret = token.RequestedProofToken.BinarySecret.Value;
                }
                if (token.Lifetime != null)
                {
                    ssoticket.Created = XmlConvert.ToDateTime(token.Lifetime.Created.Value, "yyyy-MM-ddTHH:mm:ssZ");
                    ssoticket.Expires = XmlConvert.ToDateTime(token.Lifetime.Expires.Value, "yyyy-MM-ddTHH:mm:ssZ");
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