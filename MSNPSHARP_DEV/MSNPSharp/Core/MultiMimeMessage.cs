#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
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
        private string contentKey = "Messaging";

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

        public MultiMimeMessage(string to, string from)
        {
            To = to;
            From = from;

            Stream = 0;
            Segment = 0;

            contentHeaders["Content-Length"] = "0";
            contentHeaders["Content-Type"] = "Text/plain; charset=UTF-8";
        }

        public MultiMimeMessage(byte[] data)
        {
            ParseBytes(data);
        }

        public MimeValue To
        {
            get
            {
                return routingHeaders["To"];
            }
            set
            {
                routingHeaders["To"] = value;
            }
        }

        public MimeValue From
        {
            get
            {
                return routingHeaders["From"];
            }
            set
            {
                routingHeaders["From"] = value;
            }
        }


        public long Stream
        {
            get
            {
                return long.Parse(reliabilityHeaders["Stream"], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                reliabilityHeaders["Stream"] = value.ToString();
            }
        }

        public long Segment
        {
            get
            {
                return long.Parse(reliabilityHeaders["Segment"], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                reliabilityHeaders["Segment"] = value.ToString();
            }
        }


        public MimeValue ContentType
        {
            get
            {
                return contentHeaders["Content-Type"];
            }
            set
            {
                contentHeaders["Content-Type"] = value;
            }
        }

        public override byte[] GetBytes()
        {
            if (InnerBody == null)
                InnerBody = new byte[0];

            contentHeaders["Content-Length"] = InnerBody.Length.ToString();

            StringBuilder sb = new StringBuilder(128);
            sb.AppendLine("Routing: 1.0");
            foreach (string key in routingHeaders.Keys)
            {
                if (key != "Routing")
                {
                    sb.AppendLine(key + ": " + routingHeaders[key].ToString());
                }
            }
            sb.AppendLine();

            sb.AppendLine("Reliability: 1.0");
            foreach (string key in reliabilityHeaders.Keys)
            {
                if (key != "Reliability")
                {
                    sb.AppendLine(key + ": " + reliabilityHeaders[key].ToString());
                }
            }
            sb.AppendLine();

            sb.AppendLine(ContentKey + ": 1.0");
            foreach (string key in contentHeaders.Keys)
            {
                if (key != ContentKey)
                {
                    sb.AppendLine(key + ": " + contentHeaders[key].ToString());
                }
            }

            sb.AppendLine();

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
            contentKey = contentHeaders.ContainsKey("Publication") ? "Publication" : "Messaging";

            int bodyLen = data.Length - routerHeaderEnd - reliabilityHeaderEnd - messagingHeaderEnd;
            int contentLen = bodyLen;

            if ((bodyLen > 0)
                ||
                (contentHeaders.ContainsKey("Content-Length") &&
                 int.TryParse(contentHeaders["Content-Length"], out contentLen) && contentLen > 0 &&
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

        public override string ToString()
        {
            return Encoding.UTF8.GetString(GetBytes());
        }
    }
};
