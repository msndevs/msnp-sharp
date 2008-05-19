#region Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
/*
Copyright (c) 2002-2005, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net)
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

namespace MSNPSharp.Core
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using MSNPSharp;
    using MSNPSharp.DataTransfer;

    [Serializable()]
    public class MSGMessage : NetworkMessage
    {
        StrDictionary mimeHeader;

        public MSGMessage()
        {
            mimeHeader = new StrDictionary();
            mimeHeader.Add("MIME-Version", "1.0");
        }

        public MSGMessage(NetworkMessage message)
        {
            ParseBytes(message.InnerBody);
            message.InnerMessage = this;
        }

        public StrDictionary MimeHeader
        {
            get
            {
                return mimeHeader;
            }
        }

        public override byte[] GetBytes()
        {
            StringBuilder builder = new StringBuilder();

            foreach (StrKeyValuePair entry in MimeHeader)
                builder.Append(entry.Key).Append(": ").Append(entry.Value).Append("\r\n");

            builder.Append("\r\n");

            if (InnerMessage != null)
                return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), InnerMessage.GetBytes());

            return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        }

        protected StrDictionary ParseMime(IEnumerator enumerator)
        {
            StrDictionary table = new StrDictionary();

            StringBuilder name = new StringBuilder();
            StringBuilder val = new StringBuilder();
            StringBuilder active = name;

            while (enumerator.MoveNext())
            {
                if ((byte)enumerator.Current == 13)
                {
                    // no name specified -> end of header (presumably \r\n\r\n)
                    if (name.Length == 0)
                    {
                        enumerator.MoveNext();
                        return table;
                    }

                    string strname = name.ToString();

                    if (!table.ContainsKey(strname))
                        table.Add(strname, val.ToString());

                    name.Length = 0;
                    val.Length = 0;
                    active = name;
                }
                else if ((byte)enumerator.Current == 58) //:
                {
                    if (active == name)
                    {
                        active = val;
                        enumerator.MoveNext();
                    }
                }
                else if ((byte)enumerator.Current != 10)
                {
                    active.Append(Convert.ToChar(enumerator.Current, System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            return table;
        }

        public override void ParseBytes(byte[] data)
        {
            // parse the header
            IEnumerator enumerator = data.GetEnumerator();
            mimeHeader = ParseMime(enumerator);

            // get the rest of the message
            MemoryStream memStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memStream);
            while (enumerator.MoveNext())
                writer.Write((byte)enumerator.Current);
            InnerBody = memStream.ToArray();
            memStream.Close();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (StrKeyValuePair entry in MimeHeader)
            {
                builder.Append(entry.Key).Append(": ").Append(entry.Value).Append("\r\n");
            }
            builder.Append("\r\n");
            return "[MSGMessage] " + builder.ToString();
        }
    }
};
