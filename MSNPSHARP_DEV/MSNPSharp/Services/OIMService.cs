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
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Security.Authentication;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNRSIService;

    #region EventArgs

    [Serializable()]
    public class OIMSendCompletedEventArgs : EventArgs
    {
        private Exception error;
        private string sender = string.Empty;
        private string receiver = string.Empty;
        private string message = string.Empty;
        private ulong sequence;

        /// <summary>
        /// OIM sequence number (OIMCount)
        /// </summary>
        public ulong Sequence
        {
            get
            {
                return sequence;
            }
        }

        /// <summary>
        /// Message content
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }

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

        /// <summary>
        /// OIM sender's email.
        /// </summary>
        public string Sender
        {
            get
            {
                return sender;
            }
        }

        /// <summary>
        /// OIM receiver's email.
        /// </summary>
        public string Receiver
        {
            get
            {
                return receiver;
            }
        }

        public OIMSendCompletedEventArgs()
        {
        }

        public OIMSendCompletedEventArgs(string senderAccount, string receiverAccount, ulong seq, string content, Exception err)
        {
            sender = senderAccount;
            receiver = receiverAccount;
            sequence = seq;
            message = content;
            error = err;
        }
    }


    [Serializable()]
    public class OIMReceivedEventArgs : EventArgs
    {
        private bool isRead = true;
        private Guid guid = Guid.Empty;
        private DateTime receivedTime = DateTime.Now;
        private string email;
        private string nickName;
        private string message;

        /// <summary>
        /// The date and time you receive this message.
        /// </summary>
        public DateTime ReceivedTime
        {
            get
            {
                return receivedTime;
            }
        }

        /// <summary>
        /// Sender account.
        /// </summary>
        public string Email
        {
            get
            {
                return email;
            }
        }

        /// <summary>
        /// Sender nickname.
        /// </summary>
        public string NickName
        {
            get
            {
                return nickName;
            }
        }

        /// <summary>
        /// Text message.
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }

        /// <summary>
        /// Message ID
        /// </summary>
        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        /// <summary>
        /// Set this to true if you don't want to receive this message
        /// next time you login.
        /// </summary>
        public bool IsRead
        {
            get
            {
                return isRead;
            }
            set
            {
                isRead = value;
            }
        }

        public OIMReceivedEventArgs(DateTime rcvTime, Guid g, string account, string nick, string msg)
        {
            receivedTime = rcvTime;
            guid = g;
            email = account;
            nickName = nick;
            message = msg;
        }
    }
    #endregion

    /// <summary>
    /// Provides webservice operation for offline messages
    /// </summary>
    public class OIMService : MSNService
    {
        /// <summary>
        /// Occurs when receive an OIM.
        /// </summary>
        public event EventHandler<OIMReceivedEventArgs> OIMReceived;

        /// <summary>
        /// Fires after an OIM was sent.
        /// </summary>
        public event EventHandler<OIMSendCompletedEventArgs> OIMSendCompleted;

        public OIMService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }



        internal void ProcessOIM(MimeMessage message, bool initial)
        {
            if (OIMReceived == null)
                return;

            string xmlstr = message.MimeHeader["Mail-Data"];
            if ("too-large" == xmlstr && NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState getMetadataObject = new MsnServiceState(PartnerScenario.None, "GetMetadata", true);
                RSIService rsiService = (RSIService)CreateService(MsnServiceType.RSI, getMetadataObject);
                rsiService.GetMetadataCompleted += delegate(object sender, GetMetadataCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(rsiService, MsnServiceType.RSI, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (e.Cancelled)
                        return;

                    if (e.Error == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "GetMetadata completed.", GetType().Name);

                        if (e.Result != null && e.Result is Array)
                        {
                            foreach (XmlNode m in (XmlNode[])e.Result)
                            {
                                if (m.Name == "MD")
                                {
                                    processOIMS(((XmlNode)m).ParentNode.InnerXml, initial);
                                    break;
                                }
                            }
                        }
                    }
                };

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(rsiService, MsnServiceType.RSI, getMetadataObject, new GetMetadataRequestType()));
                return;
            }
            processOIMS(xmlstr, initial);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmldata"></param>
        /// <param name="initial">if true, get all oims and sort by receive date</param>
        private void processOIMS(string xmldata, bool initial)
        {
            if (OIMReceived == null)
                return;

            if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                return;

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(xmldata);
            XmlNodeList xnodlst = xdoc.GetElementsByTagName("M");
            List<string> guidstodelete = new List<string>();
            List<OIMReceivedEventArgs> initialOIMS = new List<OIMReceivedEventArgs>();
            int oimdeletecount = xnodlst.Count;

            foreach (XmlNode m in xnodlst)
            {
                DateTime rt = DateTime.Now;
                Guid guid = Guid.Empty;
                String email = String.Empty;
                String friendlyName = String.Empty;
                String message = String.Empty;

                foreach (XmlNode a in m)
                {
                    switch (a.Name)
                    {
                        case "RT":
                            rt = XmlConvert.ToDateTime(a.InnerText, XmlDateTimeSerializationMode.RoundtripKind);
                            break;

                        case "E":
                            email = a.InnerText;
                            break;

                        case "N":
                            friendlyName = a.InnerText;
                            break;

                        case "I":
                            guid = new Guid(a.InnerText);
                            break;
                    }
                }

                MsnServiceState getMessageObject = new MsnServiceState(PartnerScenario.None, "GetMessage", true);
                RSIService rsiService = (RSIService)CreateService(MsnServiceType.RSI, getMessageObject);
                rsiService.GetMessageCompleted += delegate(object service, GetMessageCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(rsiService, MsnServiceType.RSI, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (!e.Cancelled && e.Error == null)
                    {
                        if (friendlyName != String.Empty && friendlyName.Contains("?"))
                        {
                            string[] fn = friendlyName.Split('?');
                            Encoding encode = Encoding.UTF8;
                            try
                            {
                                encode = Encoding.GetEncoding(fn[1]);
                            }
                            catch (Exception)
                            {
                                encode = Encoding.UTF8;
                            }

                            if (fn[2].ToLowerInvariant() == "b")
                            {
                                friendlyName = encode.GetString(Convert.FromBase64String(fn[3]));
                            }
                            else if (fn[2].ToLowerInvariant() == "q")
                            {
                                friendlyName = MSNHttpUtility.QPDecode(fn[3], encode);
                            }
                        }

                        MimeDictionary headers = new MimeDictionary(Encoding.UTF8.GetBytes(e.Result.GetMessageResult));
                        int msgindex = e.Result.GetMessageResult.IndexOf("\n\n");
                        if (msgindex != -1)
                        {
                            string msgstr = e.Result.GetMessageResult.Substring(msgindex);

                            Encoding encoding = Encoding.UTF8;
                            try
                            {
                                encoding = headers[MIMEContentHeaders.ContentType].HasAttribute("charset") ? Encoding.GetEncoding(headers[MIMEContentHeaders.ContentType]["charset"]) : Encoding.UTF8;
                            }
                            catch (Exception)
                            {
                                encoding = Encoding.UTF8;
                            }

                            if (headers["Content-Transfer-Encoding"].Value.ToLowerInvariant().StartsWith("q"))
                            {
                                message = MSNHttpUtility.QPDecode(msgstr, encoding);
                            }
                            else if (headers["Content-Transfer-Encoding"].Value.ToLowerInvariant().StartsWith("b"))
                            {
                                message = encoding.GetString(Convert.FromBase64String(msgstr));
                            }

                            OIMReceivedEventArgs orea = new OIMReceivedEventArgs(rt, guid, email, friendlyName, message);

                            if (initial)
                            {
                                initialOIMS.Add(orea);

                                // Is this the last OIM?
                                if (initialOIMS.Count == oimdeletecount)
                                {
                                    initialOIMS.Sort(CompareDates);
                                    foreach (OIMReceivedEventArgs ea in initialOIMS)
                                    {
                                        OnOIMReceived(this, ea);
                                        if (ea.IsRead)
                                        {
                                            guidstodelete.Add(ea.Guid.ToString());
                                        }
                                        if (0 == --oimdeletecount && guidstodelete.Count > 0)
                                        {
                                            DeleteOIMMessages(guidstodelete.ToArray());
                                        }
                                    }
                                }
                            }
                            else
                            {
                                OnOIMReceived(this, orea);
                                if (orea.IsRead)
                                {
                                    guidstodelete.Add(guid.ToString());
                                }
                                if (0 == --oimdeletecount && guidstodelete.Count > 0)
                                {
                                    DeleteOIMMessages(guidstodelete.ToArray());
                                }
                            }
                        }
                    }
                    return;
                };

                GetMessageRequestType request = new GetMessageRequestType();
                request.messageId = guid.ToString();
                request.alsoMarkAsRead = false;

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(rsiService, MsnServiceType.RSI, getMessageObject, request));
            }
        }

        private static int CompareDates(OIMReceivedEventArgs x, OIMReceivedEventArgs y)
        {
            return x.ReceivedTime.CompareTo(y.ReceivedTime);
        }

        private void DeleteOIMMessages(string[] guids)
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                return;

            MsnServiceState deleteMessagesObject = new MsnServiceState(PartnerScenario.None, "DeleteMessages", true);
            RSIService rsiService = (RSIService)CreateService(MsnServiceType.RSI, deleteMessagesObject);
            rsiService.DeleteMessagesCompleted += delegate(object service, DeleteMessagesCompletedEventArgs e)
            {
                OnAfterCompleted(new ServiceOperationEventArgs(rsiService, MsnServiceType.RSI, e));

                if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                    return;

                if (!e.Cancelled && e.Error == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "DeleteMessages completed.", GetType().Name);
                }
            };

            DeleteMessagesRequestType request = new DeleteMessagesRequestType();
            request.messageIds = guids;
            RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(rsiService, MsnServiceType.RSI, deleteMessagesObject, request));
        }

        /// <summary>
        /// Send an offline message to a contact.
        /// </summary>
        /// <param name="account">Target user</param>
        /// <param name="msg">Plain text message</param>
        public void SendOIMMessage(string account, string msg)
        {
            SendOIMMessage(NSMessageHandler.ContactList.GetContact(account), new TextMessage(msg));
        }

        /// <summary>
        /// Send an offline message to a contact.
        /// </summary>
        /// <param name="receiver">Target user</param>
        /// <param name="msg"><see cref="TextMessage"/> to send</param>
        public void SendOIMMessage(Contact receiver, TextMessage msg)
        {
            if (receiver != null)
            {
                Exception err = null;
                try
                {
                    NSMessageHandler.SendOIMMessage(receiver, msg);
                }
                catch (Exception exp)
                {
                    err = exp;
                }

                OnOIMSendCompleted(this,
                                new OIMSendCompletedEventArgs(
                                NSMessageHandler.Owner.Account,
                                receiver.Account,
                                0,
                                msg.Text,
                                err));
            }
        }

        protected virtual void OnOIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (OIMReceived != null)
            {
                OIMReceived(sender, e);
            }
        }

        protected virtual void OnOIMSendCompleted(object sender, OIMSendCompletedEventArgs e)
        {
            if (OIMSendCompleted != null)
            {
                OIMSendCompleted(sender, e);
            }
        }
    }
};
