using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web.Services.Protocols;
using System.Net;

using MSNPSharp.Core;
using MSNPSharp.MSNWS.MSNRSIService;
using MSNPSharp.MSNWS.MSNOIMStoreService;

namespace MSNPSharp
{
    internal class OIMUserState
    {
        public int RecursiveCall = 0;
        private readonly int oimcount;
        private readonly string account = String.Empty;
        public OIMUserState(int oimCount, string account)
        {
            this.oimcount = oimCount;
            this.account = account;
        }
    }

    public class OIMService : MSNService
    {
        NSMessageHandler nsMessageHandler = null;
        WebProxy webProxy = null;

        public OIMService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
            if (nsMessageHandler.ConnectivitySettings != null && nsMessageHandler.ConnectivitySettings.WebProxy != null)
            {
                webProxy = nsMessageHandler.ConnectivitySettings.WebProxy;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get { return nsMessageHandler; }
        }

        internal void ProcessOIM(MSGMessage message)
        {
            string xmlstr = message.MimeHeader["Mail-Data"];
            if ("too-large" == xmlstr)
            {
                string[] TandP = nsMessageHandler.Tickets[Iniproperties.WebTicket].Split(new string[] { "t=", "&p=" }, StringSplitOptions.None);
                RSIService rsiService = new RSIService();
                rsiService.Proxy = webProxy;
                rsiService.Timeout = Int32.MaxValue;
                rsiService.PassportCookieValue = new PassportCookie();
                rsiService.PassportCookieValue.t = TandP[1];
                rsiService.PassportCookieValue.p = TandP[2];
                rsiService.GetMetadataCompleted += delegate(object sender, GetMetadataCompletedEventArgs e)
                {
                    if (!e.Cancelled && e.Error == null)
                    {
                        if (Settings.TraceSwitch.TraceVerbose)
                            Trace.WriteLine("GetMetadata completed.");

                        processOIMS(e.Result);
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(sender,
                            new ServiceOperationFailedEventArgs("ProcessOIM", e.Error));
                    }
                    ((IDisposable)sender).Dispose();
                    return;
                };
                rsiService.GetMetadataAsync(new GetMetadataRequestType(), new object());
                return;
            }
            processOIMS(xmlstr);
        }

        private void processOIMS(string xmldata)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xmldata);
            XmlNodeList xnodlst = xdoc.GetElementsByTagName("M");
            List<string> guidstodelete = new List<string>();
            int oimdeletecount = xnodlst.Count;

            Regex regmsg = new Regex("\n\n[^\n]+");
            Regex regsenderdata = new Regex("From:(?<encode>=.*=)<(?<mail>.+)>\n");

            string[] TandP = nsMessageHandler.Tickets[Iniproperties.WebTicket].Split(new string[] { "t=", "&p=" }, StringSplitOptions.None);

            foreach (XmlNode m in xnodlst)
            {
                string rt = DateTime.Now.ToString();
                Int32 size = 0;
                String email = String.Empty;
                String guid = String.Empty;
                String message = String.Empty;

                foreach (XmlNode a in m)
                {
                    switch (a.Name)
                    {
                        case "RT":
                            rt = a.InnerText;
                            break;

                        case "SZ":
                            size = Convert.ToInt32(a.InnerText);
                            break;

                        case "E":
                            email = a.InnerText;
                            break;

                        case "I":
                            guid = a.InnerText;
                            break;
                    }
                }

                RSIService rsiService = new RSIService();
                rsiService.Proxy = webProxy;
                rsiService.Timeout = Int32.MaxValue;
                rsiService.PassportCookieValue = new PassportCookie();
                rsiService.PassportCookieValue.t = TandP[1];
                rsiService.PassportCookieValue.p = TandP[2];
                rsiService.GetMessageCompleted += delegate(object service, GetMessageCompletedEventArgs e)
                {
                    if (!e.Cancelled && e.Error == null)
                    {
                        Match mch = regsenderdata.Match(e.Result.GetMessageResult);

                        string strencoding = "utf-8";
                        if (mch.Groups["encode"].Value != String.Empty)
                        {
                            strencoding = mch.Groups["encode"].Value.Split('?')[1];
                        }
                        Encoding encode = Encoding.GetEncoding(strencoding);
                        message = encode.GetString(Convert.FromBase64String(regmsg.Match(e.Result.GetMessageResult).Value.Trim()));

                        OIMReceivedEventArgs orea = new OIMReceivedEventArgs(rt, email, message);
                        nsMessageHandler.OnOIMReceived(this, orea);
                        if (orea.IsRead)
                        {
                            guidstodelete.Add(guid);
                        }
                        if (0 == --oimdeletecount && guidstodelete.Count > 0)
                        {
                            DeleteOIMMessages(guidstodelete.ToArray());
                        }
                    }
                    else if (e.Error != null)
                    {
                        OnServiceOperationFailed(rsiService,
                            new ServiceOperationFailedEventArgs("ProcessOIM", e.Error));
                    }
                    return;
                };

                GetMessageRequestType request = new GetMessageRequestType();
                request.messageId = guid;
                request.alsoMarkAsRead = false;
                rsiService.GetMessageAsync(request, new object());
            }
        }

        private void DeleteOIMMessages(string[] guids)
        {
            string[] TandP = nsMessageHandler.Tickets[Iniproperties.WebTicket].Split(new string[] { "t=", "&p=" }, StringSplitOptions.None);

            RSIService rsiService = new RSIService();
            rsiService.Proxy = webProxy;
            rsiService.Timeout = Int32.MaxValue;
            rsiService.PassportCookieValue = new PassportCookie();
            rsiService.PassportCookieValue.t = TandP[1];
            rsiService.PassportCookieValue.p = TandP[2];
            rsiService.DeleteMessagesCompleted += delegate(object service, DeleteMessagesCompletedEventArgs e)
            {
                if (!e.Cancelled && e.Error == null)
                {
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("DeleteMessages completed.");
                }
                else if (e.Error != null)
                {
                    OnServiceOperationFailed(rsiService,
                            new ServiceOperationFailedEventArgs("ProcessOIM", e.Error));
                }
                ((IDisposable)service).Dispose();
                return;
            };

            DeleteMessagesRequestType request = new DeleteMessagesRequestType();
            request.messageIds = guids;
            rsiService.DeleteMessagesAsync(request, new object());
        }

        private string _RunGuid = Guid.NewGuid().ToString();

        /// <summary>
        /// Send an offline message to a contact.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="msg"></param>
        public void SendOIMMessage(string account, string msg)
        {
            Contact contact = nsMessageHandler.ContactList[account];
            if (contact != null && contact.OnAllowedList)
            {
                StringBuilder messageTemplate = new StringBuilder(
                    "MIME-Version: 1.0\r\n"
                  + "Content-Type: text/plain; charset=UTF-8\r\n"
                  + "Content-Transfer-Encoding: base64\r\n"
                  + "X-OIM-Message-Type: OfflineMessage\r\n"
                  + "X-OIM-Run-Id: {{run_id}}\r\n"
                  + "X-OIM-Sequence-Num: {seq-num}\r\n"
                  + "\r\n"
                  + "{base64_msg}\r\n"
                );

                messageTemplate.Replace("{base64_msg}", Convert.ToBase64String(Encoding.UTF8.GetBytes(msg), Base64FormattingOptions.InsertLineBreaks));
                messageTemplate.Replace("{seq-num}", contact.OIMCount.ToString());
                messageTemplate.Replace("{run_id}", _RunGuid);

                string message = messageTemplate.ToString();

                OIMStoreService oimService = new OIMStoreService();
                oimService.Proxy = webProxy;
                oimService.FromValue = new From();
                oimService.FromValue.memberName = nsMessageHandler.Owner.Mail;
                oimService.FromValue.friendlyName = "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(nsMessageHandler.Owner.Name)) + "?=";
                oimService.FromValue.buildVer = "8.5.1302";
                oimService.FromValue.msnpVer = "MSNP15";
                oimService.FromValue.lang = System.Globalization.CultureInfo.CurrentCulture.Name;
                oimService.FromValue.proxy = "MSNMSGR";

                oimService.ToValue = new To();
                oimService.ToValue.memberName = account;

                oimService.TicketValue = new Ticket();
                oimService.TicketValue.passport = nsMessageHandler.Tickets[Iniproperties.OIMTicket];
                oimService.TicketValue.lockkey = nsMessageHandler.Tickets.ContainsKey(Iniproperties.LockKey) ? nsMessageHandler.Tickets[Iniproperties.LockKey] : String.Empty;
                oimService.TicketValue.appid = nsMessageHandler.Credentials.ClientID;

                oimService.Sequence = new SequenceType();
                oimService.Sequence.Identifier = new AttributedURI();
                oimService.Sequence.Identifier.Value = "http://messenger.msn.com";
                oimService.Sequence.MessageNumber = 1;

                OIMUserState userstate = new OIMUserState(contact.OIMCount, account);
                oimService.StoreCompleted += delegate(object service, StoreCompletedEventArgs e)
                {
                    oimService = service as OIMStoreService;
                    if (e.Cancelled == false && e.Error == null)
                    {
                        SequenceAcknowledgmentAcknowledgmentRange range = oimService.SequenceAcknowledgmentValue.AcknowledgmentRange[0];
                        if (range.Lower == 1 && range.Upper == 1)
                        {
                            contact.OIMCount++; // Sent successfully.
                            if (Settings.TraceSwitch.TraceVerbose)
                                Trace.WriteLine("A OIM Messenger has been sent, runId = " + _RunGuid);
                        }
                    }
                    else if (e.Error != null && e.Error is SoapException)
                    {
                        SoapException soapexp = e.Error as SoapException;
                        if (soapexp.Code.Name == "AuthenticationFailed")
                        {
                            nsMessageHandler.Tickets[Iniproperties.LockKey] = QRYFactory.CreateQRY(nsMessageHandler.Credentials.ClientID, nsMessageHandler.Credentials.ClientCode, soapexp.Detail.InnerText);
                            oimService.TicketValue.lockkey = nsMessageHandler.Tickets[Iniproperties.LockKey];
                        }
                        else if (soapexp.Code.Name == "SenderThrottleLimitExceeded")
                        {
                            if (Settings.TraceSwitch.TraceVerbose)
                                Trace.WriteLine("OIM: SenderThrottleLimitExceeded. Waiting 11 seconds...");

                            System.Threading.Thread.Sleep(11111); // wait 11 seconds.
                        }
                        if (userstate.RecursiveCall++ < 5)
                        {
                            oimService.StoreAsync(MessageType.text, message, userstate); // Call this delegate again.
                            return;
                        }
                        OnServiceOperationFailed(oimService,
                            new ServiceOperationFailedEventArgs("SendOIMMessage", e.Error));
                    }
                };
                oimService.StoreAsync(MessageType.text, message, userstate);
            }
        }

    }
}
