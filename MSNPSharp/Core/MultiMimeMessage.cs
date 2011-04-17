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
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;

namespace MSNPSharp.Core
{
    /// <summary>
    /// MultiMime message for SDG/NFY/PUT/DEL commands.
    /// </summary>
    public class MultiMimeMessage : NetworkMessage
    {
        private MimeDictionary routingHeaders = new MimeDictionary();
        private MimeDictionary reliabilityHeaders = new MimeDictionary();
        private MimeDictionary contentHeaders = new MimeDictionary();

        private string contentKey = MIMEContentHeaders.Messaging;
        private string contentKeyVersion = "1.0";

        public MimeDictionary RoutingHeaders
        {
            get
            {
                return routingHeaders;
            }
        }

        public MimeDictionary ReliabilityHeaders
        {
            get
            {
                return reliabilityHeaders;
            }
        }

        public MimeDictionary ContentHeaders
        {
            get
            {
                return contentHeaders;
            }
        }

        public string ContentKey
        {
            get
            {
                return contentKey;
            }
            set
            {
                contentKey = value;
            }
        }

        public string ContentKeyVersion
        {
            get
            {
                return contentKeyVersion;
            }
            set
            {
                contentKeyVersion = value;
            }
        }

        public MultiMimeMessage()
        {
        }

        public MultiMimeMessage(string to, string from)
        {
            To = to;
            From = from;

            contentHeaders[MIMEContentHeaders.ContentLength] = "0";
            contentHeaders[MIMEContentHeaders.ContentType] = "Text/plain";
            contentHeaders[MIMEContentHeaders.ContentType][MIMEContentHeaders.CharSet] = "UTF-8";
        }

        public MultiMimeMessage(byte[] data)
        {
            ParseBytes(data);
        }

        public MimeValue To
        {
            get
            {
                return routingHeaders[MIMERoutingHeaders.To];
            }
            set
            {
                routingHeaders[MIMERoutingHeaders.To] = value;
            }
        }

        public MimeValue From
        {
            get
            {
                return routingHeaders[MIMERoutingHeaders.From];
            }
            set
            {
                routingHeaders[MIMERoutingHeaders.From] = value;
            }
        }


        public long Stream
        {
            get
            {
                return reliabilityHeaders.ContainsKey(MIMEReliabilityHeaders.Stream) ?
                    long.Parse(reliabilityHeaders[MIMEReliabilityHeaders.Stream], System.Globalization.CultureInfo.InvariantCulture) : 0;
            }
            set
            {
                reliabilityHeaders[MIMEReliabilityHeaders.Stream] = value.ToString();
            }
        }

        public long Segment
        {
            get
            {
                return reliabilityHeaders.ContainsKey(MIMEReliabilityHeaders.Segment) ?
                    long.Parse(reliabilityHeaders[MIMEReliabilityHeaders.Segment], System.Globalization.CultureInfo.InvariantCulture) : 0;
            }
            set
            {
                reliabilityHeaders[MIMEReliabilityHeaders.Segment] = value.ToString();
            }
        }


        public MimeValue ContentType
        {
            get
            {
                return contentHeaders[MIMEContentHeaders.ContentType];
            }
            set
            {
                contentHeaders[MIMEContentHeaders.ContentType] = value;
            }
        }

        public override byte[] GetBytes()
        {
            if (InnerBody == null)
                InnerBody = new byte[0];

            contentHeaders[MIMEContentHeaders.ContentLength] = InnerBody.Length.ToString();

            StringBuilder sb = new StringBuilder(128);
            
            //Do not use append line, because under *nix the line break is \n but MSN need \r\n
            //sb.AppendLine(MIMERoutingHeaders.Routing + MIMEHeaderStrings.KeyValueSeparator + "1.0");
            sb.Append(MIMERoutingHeaders.Routing + MIMEHeaderStrings.KeyValueSeparator + "1.0");
            sb.Append("\r\n");
            
            foreach (string key in routingHeaders.Keys)
            {
                if (key != MIMERoutingHeaders.Routing)
                {
                    sb.Append(key + MIMEHeaderStrings.KeyValueSeparator + routingHeaders[key].ToString());
                    sb.Append("\r\n");
                }
            }
            sb.Append("\r\n");

            sb.Append(MIMEReliabilityHeaders.Reliability + MIMEHeaderStrings.KeyValueSeparator + "1.0");
            sb.Append("\r\n");
            
            foreach (string key in reliabilityHeaders.Keys)
            {
                if (key != MIMEReliabilityHeaders.Reliability)
                {
                    sb.Append(key + MIMEHeaderStrings.KeyValueSeparator + reliabilityHeaders[key].ToString());
                    sb.Append("\r\n");
                }
            }
            sb.Append("\r\n");

            sb.Append(ContentKey + MIMEHeaderStrings.KeyValueSeparator + ContentKeyVersion);
            sb.Append("\r\n");
            foreach (string key in contentHeaders.Keys)
            {
                if (key != ContentKey)
                {
                    sb.Append(key + MIMEHeaderStrings.KeyValueSeparator + contentHeaders[key].ToString());
                    sb.Append("\r\n");
                }
            }

            sb.Append("\r\n");

            return (InnerBody.Length > 0) ?
                AppendArray(Encoding.UTF8.GetBytes(sb.ToString()), InnerBody)
                :
                Encoding.UTF8.GetBytes(sb.ToString());
        }

        public override void ParseBytes(byte[] data)
        {
            routingHeaders.Clear();
            int routerHeaderEnd = routingHeaders.Parse(data);

            byte[] reliabilityData = new byte[data.Length - routerHeaderEnd];
            Array.Copy(data, routerHeaderEnd, reliabilityData, 0, reliabilityData.Length);
            reliabilityHeaders.Clear();
            int reliabilityHeaderEnd = reliabilityHeaders.Parse(reliabilityData);

            byte[] messagingData = new byte[data.Length - routerHeaderEnd - reliabilityHeaderEnd];
            Array.Copy(reliabilityData, reliabilityHeaderEnd, messagingData, 0, messagingData.Length);
            contentHeaders.Clear();
            int messagingHeaderEnd = contentHeaders.Parse(messagingData);
            contentKey = contentHeaders.ContainsKey(MIMEContentHeaders.Publication)
                ? MIMEContentHeaders.Publication
                : (contentHeaders.ContainsKey(MIMEContentHeaders.Notification) ? MIMEContentHeaders.Notification : MIMEContentHeaders.Messaging);

            int bodyLen = data.Length - routerHeaderEnd - reliabilityHeaderEnd - messagingHeaderEnd;
            int contentLen = bodyLen;

            if ((bodyLen > 0)
                ||
                (contentHeaders.ContainsKey(MIMEContentHeaders.ContentLength) &&
                 int.TryParse(contentHeaders[MIMEContentHeaders.ContentLength], out contentLen) && contentLen > 0 &&
                 contentLen <= bodyLen /*don't allow buffer overflow*/))
            {
                InnerBody = new byte[contentLen];
                Array.Copy(messagingData, messagingHeaderEnd, InnerBody, 0, InnerBody.Length);
            }
            else
            {
                InnerBody = new byte[0];
            }

        }

        public override void CreateFromParentMessage(NetworkMessage containerMessage)
        {
            base.CreateFromParentMessage(containerMessage);

            if (ParentMessage.InnerBody != null)
            {
                ParseBytes(ParentMessage.InnerBody);
            }
        }

        public override string ToString()
        {
            string contentEncoding = string.Empty;
            string debugString = string.Empty;

            if (ContentHeaders.ContainsKey(MIMEContentHeaders.ContentTransferEncoding) && InnerBody != null)
            {
                contentEncoding = ContentHeaders[MIMEContentHeaders.ContentTransferEncoding].Value;
            }

            byte[] readableBinaries = GetBytes();
            switch (contentEncoding)
            {
                case MIMEContentTransferEncoding.Binary:
                    int payLoadLength = 0;
                    payLoadLength = InnerBody.Length;
                    byte[] headers = new byte[readableBinaries.Length - payLoadLength];
                    Array.Copy(readableBinaries, headers, headers.Length);

                    if (InnerBody != null && InnerMessage == null)
                    {
                        if (ContentHeaders.ContainsKey(MIMEContentHeaders.BridgingOffsets))
                        {
                            debugString = Encoding.UTF8.GetString(headers) + "\r\nMulti-Package Binary Data: {Length: " + payLoadLength + "}";
                        }
                        else
                        {
                            debugString = Encoding.UTF8.GetString(headers) + "\r\nUnknown Binary Data: {Length: " + payLoadLength + "}";
                        }
                    }



                    if (InnerBody != null && InnerMessage != null)
                    {
                        debugString = Encoding.UTF8.GetString(headers) + "\r\n" + InnerMessage.ToString();
                    }
                    break;
                default:
                    debugString = Encoding.UTF8.GetString(readableBinaries);
                    break;
            }
            return debugString;
        }
    }
};
