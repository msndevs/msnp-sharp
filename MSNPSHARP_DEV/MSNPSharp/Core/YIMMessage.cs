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

namespace MSNPSharp.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Collections;
    using System.Globalization;

    /// <summary>
    /// Yahoo Messenger Message
    /// </summary>
    [Serializable()]
    public class YIMMessage : MSNMessage
    {
        string _user = "";
        string _msgtype = ((uint)TextMessageType.Text).ToString();
        string _clienttype = ((int)ClientType.EmailMember).ToString();

        MsnProtocol _protocol = MsnProtocol.MSNP18;

        string _dstuser = "";
        string _dstclienttype = ((int)ClientType.PassportMember).ToString();

        public YIMMessage(NSMessage message, MsnProtocol protocol)
            : base("UBM", (ArrayList)message.CommandValues.Clone())
        {
            _protocol = protocol;
            _user = message.CommandValues[0].ToString();

            if (message.CommandValues.Count > 4)
            {
                _msgtype = message.CommandValues[4].ToString();
                _dstuser = message.CommandValues[2].ToString();
                _dstclienttype = message.CommandValues[3].ToString();
            }


            Command = "UBM";

            message.Command = "";
            message.CommandValues.Clear();
            InnerMessage = new MSGMessage(message);
            InnerBody = GetBytes();
        }

        public YIMMessage(string command, string[] commandValues, MsnProtocol protocol)
            : base(command, new ArrayList(commandValues))
        {
            _protocol = protocol;
            _user = commandValues[0];
            _clienttype = commandValues[1];

            if (command.ToLowerInvariant() == "UBM" && commandValues.Length > 4)
            {
                _msgtype = commandValues[4].ToString();
                _dstuser = commandValues[2].ToString();
                _dstclienttype = commandValues[3].ToString();
            }
            else
            {
                _msgtype = commandValues[2].ToString();
            }


        }

        public YIMMessage(string command, ArrayList commandValues, MsnProtocol protocol)
            : base(command, commandValues)
        {
            _protocol = protocol;
            _user = commandValues[0].ToString();
            _clienttype = commandValues[1].ToString();

            if (command.ToLowerInvariant() == "UBM" && commandValues.Count > 4)
            {
                _msgtype = commandValues[4].ToString();
                _dstuser = commandValues[2].ToString();
                _dstclienttype = commandValues[3].ToString();
            }
            else
            {
                _msgtype = commandValues[2].ToString();
            }

        }

        public override byte[] GetBytes()
        {
            byte[] contents = null;

            //FIXME: maybe move this to SBMessage?
            if (InnerMessage != null)
            {
                contents = InnerMessage.GetBytes();

                // prepare a default MSG message if an inner message is specified
                if (CommandValues.Count == 0)
                {
                    if (Command != "UBM")
                        CommandValues.Add(TransactionID.ToString());

                    CommandValues.Add(_user);
                    CommandValues.Add(_clienttype);
                    
                    if (_dstuser != "" && Command == "UBM")
                    {
                        CommandValues.Add(_dstuser);
                        CommandValues.Add(_dstclienttype);
                    }

                    CommandValues.Add(_msgtype);
                    CommandValues.Add(contents.Length.ToString(CultureInfo.InvariantCulture));
                }
            }

            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);

            if (CommandValues.Count > 0)
            {
                foreach (string val in CommandValues)
                {
                    builder.Append(' ');
                    builder.Append(val);
                }

                builder.Append("\r\n");
            }

            if (InnerMessage != null)
                return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), contents);
            else
                return System.Text.Encoding.UTF8.GetBytes(builder.ToString());

        }

        public override void PrepareMessage()
        {
            CommandValues.Clear();
            if (InnerMessage != null)
                InnerMessage.PrepareMessage();
        }

        public override string ToDebugString()
        {
            GetBytes();
            byte[] contents = null;

            //FIXME: maybe move this to SBMessage?
            if (InnerMessage != null)
            {
                contents = InnerMessage.GetBytes();
            }

            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);

            if (CommandValues.Count > 0)
            {
                foreach (string val in CommandValues)
                {
                    builder.Append(' ');
                    builder.Append(val);
                }

                builder.Append("\r\n");
            }

            if (InnerMessage != null)
                return System.Text.Encoding.UTF8.GetString(AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), contents));
            else
                return System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(builder.ToString()));
        }

        public new ArrayList CommandValues
        {
            get
            {
                return base.CommandValues;
            }
            set
            {
                base.CommandValues = value;
            }
        }

        public new string Command
        {
            get
            {
                return base.Command;
            }
            set
            {
                base.Command = value;
            }
        }

        public new MSGMessage InnerMessage
        {
            get
            {
                return (MSGMessage)base.InnerMessage;
            }
            set
            {
                base.InnerMessage = value;
            }
        }

        public new int TransactionID
        {
            get
            {
                return base.TransactionID;
            }
            set
            {
                base.TransactionID = value;
            }
        }
    }
};
