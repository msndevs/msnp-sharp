#region Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2009, Bas Geertsema, Xih Solutions
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
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Web.Services.Protocols;

namespace MSNPSharp
{
    using MSNPSharp.MSNWS.MSNStorageService;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNRSIService;
    using MSNPSharp.MSNWS.MSNOIMStoreService;

    internal enum MsnServiceType
    {
        AB,
        Sharing,
        Storage,
        RSI,
        OIMStore
    }

    internal enum PartnerScenario
    {
        None,
        Initial,
        Timer,
        BlockUnblock,
        GroupSave,
        ContactSave,
        ContactMsgrAPI,
        MessengerPendingList,
        PrivacyApply,
        CircleSave,
        JoinedCircleDuringPush,
        ABChangeNotifyAlert,
        RoamingSeed,
        RoamingIdentityChanged
    }

    internal class MsnServiceObject
    {
        private string methodName;
        private PartnerScenario partnerScenario;

        /// <summary>
        /// Non-async call.
        /// </summary>
        /// <param name="scenario"></param>
        public MsnServiceObject(PartnerScenario scenario)
        {
            partnerScenario = scenario;
        }

        /// <summary>
        /// Async call.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="method">The method name without suffix Async. XXXAsync is called automatically.</param>
        public MsnServiceObject(PartnerScenario scenario, string method)
        {
            partnerScenario = scenario;
            methodName = method;
        }

        public bool IsAsync
        {
            get
            {
                return !String.IsNullOrEmpty(methodName);
            }
        }

        public string MethodName
        {
            get
            {
                return methodName;
            }
        }

        public PartnerScenario PartnerScenario
        {
            get
            {
                return partnerScenario;
            }
        }
    }


    #region ServiceOperationFailedEvent

    public class ServiceOperationFailedEventArgs : EventArgs
    {
        private string method;
        private Exception exc;

        public ServiceOperationFailedEventArgs(string methodname, Exception ex)
        {
            method = methodname;
            exc = ex;
        }

        public string Method
        {
            get
            {
                return method;
            }
        }
        public Exception Exception
        {
            get
            {
                return exc;
            }
        }
    }

    #endregion

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        /// <summary>
        /// Redirection host for service on *.omega.contacts.msn.com
        /// </summary>
        public const string ContactServiceRedirectionHost = @"byrdr.omega.contacts.msn.com";

        /// <summary>
        /// Redirection host for service on *.storage.msn.com
        /// </summary>
        public const string StorageServiceRedirectionHost = @"tkrdr.storage.msn.com";


        private Dictionary<SoapHttpClientProtocol, object> asyncStates = new Dictionary<SoapHttpClientProtocol, object>();

        private NSMessageHandler nsMessageHandler;
        private WebProxy webProxy;

        private MSNService()
        {
        }

        protected MSNService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
        }

        /// <summary>
        /// Fired when request to an async webservice method failed.
        /// </summary>
        public event EventHandler<ServiceOperationFailedEventArgs> ServiceOperationFailed;

        /// <summary>
        /// Fires ServiceOperationFailed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            if (ServiceOperationFailed != null)
                ServiceOperationFailed(sender, e);
        }


        protected void RunAsyncMethod(SoapHttpClientProtocol service)
        {
        }

        internal SoapHttpClientProtocol CreateService(MsnServiceType serviceType, MsnServiceObject asyncObject)
        {
            SoapHttpClientProtocol service = null;

            switch (serviceType)
            {
                case MsnServiceType.AB:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);

                    ABServiceBinding abService = new ABServiceBinding();
                    abService.Proxy = WebProxy;
                    abService.Timeout = Int32.MaxValue;
                    abService.UserAgent = Properties.Resources.WebServiceUserAgent;
                    abService.ABApplicationHeaderValue = new ABApplicationHeader();
                    abService.ABApplicationHeaderValue.ApplicationId = NSMessageHandler.Credentials.ClientInfo.ApplicationId;
                    abService.ABApplicationHeaderValue.IsMigration = false;
                    abService.ABApplicationHeaderValue.PartnerScenario = Convert.ToString(asyncObject.PartnerScenario);
                    abService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
                    abService.ABAuthHeaderValue = new ABAuthHeader();
                    abService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
                    abService.ABAuthHeaderValue.ManagedGroupRequest = false;

                    service = abService;
                    break;

                case MsnServiceType.Sharing:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);

                    SharingServiceBinding sharingService = new SharingServiceBinding();
                    sharingService.Proxy = WebProxy;
                    sharingService.Timeout = Int32.MaxValue;
                    sharingService.UserAgent = Properties.Resources.WebServiceUserAgent;
                    sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
                    sharingService.ABApplicationHeaderValue.ApplicationId = NSMessageHandler.Credentials.ClientInfo.ApplicationId;
                    sharingService.ABApplicationHeaderValue.IsMigration = false;
                    sharingService.ABApplicationHeaderValue.PartnerScenario = Convert.ToString(asyncObject.PartnerScenario);
                    sharingService.ABApplicationHeaderValue.BrandId = NSMessageHandler.MSNTicket.MainBrandID;
                    sharingService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
                    sharingService.ABAuthHeaderValue = new ABAuthHeader();
                    sharingService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
                    sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;

                    service = sharingService;
                    break;

                case MsnServiceType.Storage:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Storage);

                    StorageService storageService = new StorageService();
                    storageService.Proxy = WebProxy;
                    storageService.StorageApplicationHeaderValue = new StorageApplicationHeader();
                    storageService.StorageApplicationHeaderValue.ApplicationID = "Messenger Client 8.5";
                    storageService.StorageApplicationHeaderValue.Scenario = Convert.ToString(asyncObject.PartnerScenario);
                    storageService.StorageUserHeaderValue = new StorageUserHeader();
                    storageService.StorageUserHeaderValue.Puid = 0;
                    storageService.StorageUserHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket;
                    storageService.AffinityCacheHeaderValue = new AffinityCacheHeader();
                    storageService.AffinityCacheHeaderValue.CacheKey = NSMessageHandler.ContactService.Deltas.CacheKeys.ContainsKey(CacheKeyType.StorageServiceCacheKey)
                        ? NSMessageHandler.ContactService.Deltas.CacheKeys[CacheKeyType.StorageServiceCacheKey] : String.Empty;

                    service = storageService;
                    break;

                case MsnServiceType.RSI:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Web);

                    string[] TandP = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Web].Ticket.Split(new string[] { "t=", "&p=" }, StringSplitOptions.None);

                    RSIService rsiService = new RSIService();
                    rsiService.Proxy = WebProxy;
                    rsiService.Timeout = Int32.MaxValue;
                    rsiService.PassportCookieValue = new PassportCookie();
                    rsiService.PassportCookieValue.t = TandP[1];
                    rsiService.PassportCookieValue.p = TandP[2];

                    service = rsiService;
                    break;

                case MsnServiceType.OIMStore:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.OIM);

                    OIMStoreService oimService = new OIMStoreService();
                    oimService.Proxy = WebProxy;
                    oimService.TicketValue = new Ticket();
                    oimService.TicketValue.passport = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.OIM].Ticket;
                    oimService.TicketValue.lockkey = NSMessageHandler.MSNTicket.OIMLockKey;
                    oimService.TicketValue.appid = NSMessageHandler.Credentials.ClientID;

                    service = oimService;
                    break;
            }

            if (service != null)
            {
                // service.EnableDecompression = true; // Fails on Mono

                if (asyncObject != null && asyncObject.IsAsync)
                {
                    lock (asyncStates)
                        asyncStates[service] = asyncObject;
                }
            }

            return service;
        }

        private void CancelAndDisposeAysncMethods()
        {
            if (asyncStates.Count > 0)
            {
                lock (this)
                {
                    if (asyncStates.Count > 0)
                    {
                        Dictionary<SoapHttpClientProtocol, object> copyStates = new Dictionary<SoapHttpClientProtocol, object>(asyncStates);
                        asyncStates = new Dictionary<SoapHttpClientProtocol, object>();

                        foreach (KeyValuePair<SoapHttpClientProtocol, object> state in copyStates)
                        {
                            try
                            {
                                state.Key.GetType().InvokeMember("CancelAsync",
                                    System.Reflection.BindingFlags.InvokeMethod,
                                    null, state.Key,
                                    new object[] { state.Value });
                            }
                            catch (Exception error)
                            {
                                System.Diagnostics.Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "An error occured while canceling :\r\n" +
                                        "Service: " + state.Key.ToString() + "\r\n" +
                                        "State:   " + state.Value.ToString() + "\r\n" +
                                        "Message: " + error.Message);
                            }
                            finally
                            {
                                state.Key.Dispose();
                            }
                        }
                        copyStates.Clear();
                    }
                }
            }
        }


        protected void HandleServiceHeader(ServiceHeader sh, Type requestType)
        {
            if (null != sh && NSMessageHandler.ContactService != null && NSMessageHandler.ContactService.Deltas != null)
            {
                if (sh.CacheKeyChanged)
                {
                    NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey] = sh.CacheKey;
                }

                if (!String.IsNullOrEmpty(sh.PreferredHostName))
                {
                    NSMessageHandler.ContactService.PreferredHosts[requestType.ToString()] = sh.PreferredHostName;
                }

                NSMessageHandler.ContactService.Deltas.Save();
            }
        }

        public virtual void Clear()
        {
            CancelAndDisposeAysncMethods();
        }

        internal void DeleteCompletedObject(SoapHttpClientProtocol key)
        {
            lock (asyncStates)
                asyncStates.Remove(key);
        }
    }
};
