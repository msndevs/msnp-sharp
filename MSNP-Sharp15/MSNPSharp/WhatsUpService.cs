using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    public class GetWhatsUpCompletedEventArgs : EventArgs
    {
        private Exception error;
        private GetContactsRecentActivityResultType response;

        /// <summary>
        /// InnerException
        /// </summary>
        public Exception Error
        {
            get
            {
                return error;
            }
        }

        public GetContactsRecentActivityResultType Response
        {
            get
            {
                return response;
            }
        }

        protected GetWhatsUpCompletedEventArgs()
        {
        }

        public GetWhatsUpCompletedEventArgs(Exception err, GetContactsRecentActivityResultType resp)
        {
            error = err;
            response = resp;
        }
    }


    public class WhatsUpService : MSNService
    {

        private string feedUrl = string.Empty;

        /// <summary>
        /// RSS feed url for what's up service.
        /// </summary>
        public string FeedUrl
        {
            get { return feedUrl; }
        }

        public event EventHandler<GetWhatsUpCompletedEventArgs> GetWhatsUpCompleted;


        public WhatsUpService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }

        internal WhatsUpServiceBinding CreateWhatsUpService(string partnerScenario)
        {
            SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.WhatsUp);

            WhatsUpServiceBinding wuService = new WhatsUpServiceBinding();
            wuService.Proxy = WebProxy;
            wuService.Timeout = Int32.MaxValue;
            wuService.UserAgent = Properties.Resources.WebServiceUserAgent;
            wuService.Url = "http://sup.live.com/whatsnew/whatsnewservice.asmx";
            wuService.WNApplicationHeaderValue = new WNApplicationHeader();
            wuService.WNApplicationHeaderValue.ApplicationId = "3B119D87-1D76-4474-91AD-0D7267E86D04";
            wuService.WNAuthHeaderValue = new WNAuthHeader();
            wuService.WNAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.WhatsUp].Ticket;

            return wuService;
        }

        public void GetWhatsUp()
        {
            GetWhatsUp(50);
        }

        /// <summary>
        /// Get the recent activities of your contacts.
        /// </summary>
        /// <param name="count">Max activity count, must be larger than zero and less than 50.</param>
        public void GetWhatsUp(int count)
        {
            if (count > 50)
            {
                count = 50;
            }else if(count < 0)
            {
                count = 0;
            }

            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                WhatsUpServiceBinding wuService = CreateWhatsUpService(":)");
                wuService.GetContactsRecentActivityCompleted += delegate(object sender, GetContactsRecentActivityCompletedEventArgs e)
                {
                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(e.Error, null));
                        OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetContactsRecentActivity", e.Error));

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.Message, GetType().Name);
                        return;
                    }

                    if (e.Result.GetContactsRecentActivityResult != null)
                    {
                        feedUrl = e.Result.GetContactsRecentActivityResult.FeedUrl;
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(null, e.Result.GetContactsRecentActivityResult));
                    }
                    else
                    {
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(null, null));
                    }
                };

                if (count > 200)
                    count = 200;

                GetContactsRecentActivityRequestType request = new GetContactsRecentActivityRequestType();
                request.entityHandle = new entityHandle();
                request.entityHandle.Cid = Convert.ToInt64(NSMessageHandler.Owner.CID);
                request.locales = new string[] { System.Globalization.CultureInfo.CurrentCulture.Name };
                request.count = count;

                wuService.GetContactsRecentActivityAsync(request, new object());
            }
        }

        protected virtual void OnGetWhatsUpCompleted(object sender, GetWhatsUpCompletedEventArgs e)
        {
            if (GetWhatsUpCompleted != null)
            {
                GetWhatsUpCompleted(sender, e);
            }
        }

    }
};
