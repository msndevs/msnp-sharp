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
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Represents a single MSNSLPMessage. Usually this message is contained in a P2P Message.
    /// </summary>
    [Serializable()]
    public class MSNSLPMessage : NetworkMessage
    {
        /// <summary>
        /// Defaults to UTF8
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return encoding;
            }
            set
            {
                encoding = value;
            }
        }

        /// <summary>
        /// </summary>
        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        /// <summary>
        /// </summary>
        private string version = "MSNSLP/1.0";


        /// <summary>
        /// </summary>
        public int MaxForwards
        {
            get
            {
                return maxForwards;
            }
            set
            {
                maxForwards = value;
            }
        }

        /// <summary>
        /// </summary>
        private int maxForwards;

        /// <summary>
        /// </summary>
        public string To
        {
            get
            {
                return to;
            }
            set
            {
                to = value;
            }
        }

        /// <summary>
        /// </summary>
        private string to;

        /// <summary>
        /// </summary>
        public string From
        {
            get
            {
                return from;
            }
            set
            {
                from = value;
            }
        }

        /// <summary>
        /// </summary>
        private string from;

        /// <summary>
        /// The contact that send the message.
        /// </summary>
        public string FromMail
        {
            get
            {
                return From.Replace("<msnmsgr:", "").Replace(">", "");
            }
        }
        /// <summary>
        /// The contact that receives the message.
        /// </summary>
        public string ToMail
        {
            get
            {
                return To.Replace("<msnmsgr:", "").Replace(">", "");
            }
        }

        /// <summary>
        /// </summary>
        public string Via
        {
            get
            {
                return via;
            }
            set
            {
                via = value;
            }
        }

        /// <summary>
        /// </summary>
        private string via;


        /// <summary>
        /// The current branch this message applies to.
        /// </summary>
        public string Branch
        {
            get
            {
                return Via.Substring(Via.IndexOf("{"));
            }
        }

        /// <summary>
        /// The sequence count of this message.
        /// </summary>
        public int CSeq
        {
            get
            {
                return cSeq;
            }
            set
            {
                cSeq = value;
            }
        }

        /// <summary>
        /// </summary>
        private int cSeq;

        /// <summary>
        /// </summary>
        public string CallId
        {
            get
            {
                return callId;
            }
            set
            {
                callId = value;
            }
        }

        /// <summary>
        /// </summary>
        private string callId;

        /// <summary>
        /// </summary>
        public string ContentType
        {
            get
            {
                return contentType;
            }
            set
            {
                contentType = value;
            }
        }

        /// <summary>
        /// </summary>
        private string contentType;

        /// <summary>
        /// </summary>
        public int ContentLength
        {
            get
            {
                return contentLength;
            }
            set
            {
                contentLength = value;
            }
        }

        /// <summary>
        /// </summary>
        private int contentLength;

        /// <summary>
        /// </summary>
        public string Body
        {
            get
            {
                return body;
            }
            set
            {
                body = value;
            }
        }

        /// <summary>
        /// </summary>
        private string body;

        /// <summary>
        /// </summary>
        public string StartLine
        {
            get
            {
                return startLine;
            }
            set
            {
                startLine = value;
            }
        }

        /// <summary>
        /// </summary>
        private string startLine;

        /// <summary>
        /// Builds the entire message and returns it as a byte array. Ready to be used in a P2P Message.
        /// This function adds the 0x00 at the end of the message.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append(StartLine);
            builder.Append("\r\n");
            builder.Append("To: ");
            builder.Append(To);
            builder.Append("\r\n");
            builder.Append("From: ");
            builder.Append(From);
            builder.Append("\r\n");
            builder.Append("Via: ");
            builder.Append(Via);
            builder.Append("\r\n");
            builder.Append("CSeq: ");
            builder.Append(CSeq.ToString(CultureInfo.InvariantCulture));
            builder.Append(" \r\n");
            builder.Append("Call-ID: ");
            builder.Append(CallId);
            builder.Append("\r\n");
            builder.Append("Max-Forwards: ");
            builder.Append(MaxForwards.ToString(CultureInfo.InvariantCulture));
            builder.Append("\r\n");
            builder.Append("Content-Type: ");
            builder.Append(ContentType);
            builder.Append("\r\n");
            builder.Append("Content-Length: ");
            builder.Append((Encoding.GetByteCount(Body) + 1).ToString(CultureInfo.InvariantCulture));
            builder.Append("\r\n");
            builder.Append("\r\n");
            builder.Append(Body);
            foreach (string key in MessageValues.Keys)
            {
                builder.Append(key).Append(": ").Append(MessageValues[key]);
            }

            // get the bytes			
            byte[] message = Encoding.GetBytes(builder.ToString());

            // add the additional 0x00
            byte[] totalMessage = new byte[message.Length + 1];
            message.CopyTo(totalMessage, 0);
            totalMessage[message.Length] = 0x00;

            return totalMessage;
        }

        /// <summary>
        /// Contains all name/value combinations of non-header fields in the message
        /// </summary>
        public Hashtable MessageValues
        {
            get
            {
                return messageValues;
            }
            //set { messageValues = value;}
        }

        /// <summary>
        /// </summary>
        private Hashtable messageValues = new Hashtable(16);

        /// <summary>
        /// Parses an MSNSLP message and stores the values in the object's fields.
        /// </summary>
        /// <param name="data">The messagedata to parse</param>			
        public override void ParseBytes(byte[] data)
        {
            // get the lines
            MessageValues.Clear();
            string[] lines = Encoding.GetString(data).Split('\n');
            StartLine = lines[0];
            foreach (string line in lines)
            {
                int indexOfColon = line.IndexOf(':', 0);
                if (indexOfColon > 0)
                {
                    // extract the key and value from the string
                    string name = line.Substring(0, indexOfColon);
                    string val = line.Substring(indexOfColon + 2, line.Length - indexOfColon - 3);
                    switch (name)
                    {
                        case "To":
                            {
                                To = val;
                                break;
                            }
                        case "From":
                            {
                                From = val;
                                break;
                            }
                        case "Via":
                            {
                                Via = val;
                                break;
                            }
                        case "CSeq":
                            {
                                CSeq = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            }
                        case "Call-ID":
                            {
                                CallId = val;
                                break;
                            }
                        case "Max-Forwards":
                            {
                                MaxForwards = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            }
                        case "Content-Type":
                            {
                                ContentType = val;
                                break;
                            }
                        case "Content-Length":
                            {
                                ContentLength = int.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            }
                        default:
                            {
                                MessageValues.Add(name, val);
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Textual presentation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Body == null)
                Body = "";
            if (To == "")
            {
                int b = 0;
            }
            return "[MSNSLPMessage] " + System.Text.Encoding.UTF8.GetString(this.GetBytes()) + "\r\n";
        }
    }
};
