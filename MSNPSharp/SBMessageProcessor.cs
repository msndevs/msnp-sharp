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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    using System.Text;

    public class SBMessageProcessor : SocketMessageProcessor
    {
        int transactionID = 1;

        public event EventHandler<ExceptionEventArgs> HandlerException;

        protected internal SBMessageProcessor()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);

            MessagePool = new SBMessagePool();
        }

        public int TransactionID
        {
            get
            {
                return transactionID;
            }
            set
            {
                transactionID = value;
            }
        }

        private void SendMessage(TextMessage message)
        {
            //First, we have to check whether the content of the message is not to big for one message
            //There's a maximum of 1600 bytes per message, let's play safe and take 1400 bytes
            UTF8Encoding encoding = new UTF8Encoding();

            if (encoding.GetByteCount(message.Text) > 1400)
            {
                //we'll have to use multi-packets messages
                Guid guid = Guid.NewGuid();
                byte[] text = encoding.GetBytes(message.Text);
                int chunks = Convert.ToInt32(Math.Ceiling((double)text.GetUpperBound(0) / (double)1400));
                for (int i = 0; i < chunks; i++)
                {
                    SBMessage sbMessage = new SBMessage();
                    MSGMessage msgMessage = new MSGMessage();

                    //Clone the message
                    TextMessage chunkMessage = (TextMessage)message.Clone();

                    //Get the part of the message that we are going to send
                    if (text.GetUpperBound(0) - (i * 1400) > 1400)
                        chunkMessage.Text = encoding.GetString(text, i * 1400, 1400);
                    else
                        chunkMessage.Text = encoding.GetString(text, i * 1400, text.GetUpperBound(0) - (i * 1400));

                    //Add the correct headers
                    msgMessage.MimeHeader.Add("Message-ID", "{" + guid.ToString() + "}");

                    if (i == 0)
                        msgMessage.MimeHeader.Add("Chunks", Convert.ToString(chunks));
                    else
                        msgMessage.MimeHeader.Add("Chunk", Convert.ToString(i));

                    sbMessage.InnerMessage = msgMessage;

                    msgMessage.InnerMessage = chunkMessage;

                    //send it over the network
                    DeliverToNetwork(sbMessage);
                }
            }
            else
            {
                SBMessage sbMessage = new SBMessage();
                MSGMessage msgMessage = new MSGMessage();

                sbMessage.InnerMessage = msgMessage;

                msgMessage.InnerMessage = message;

                // send it over the network
                DeliverToNetwork(sbMessage);
            }
        }

        private void SendMessage(TypingMessage message)
        {
            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "U";

            sbMessage.InnerMessage = message;
            DeliverToNetwork(sbMessage);
        }

        private void SendMessage(SBP2PMessage message)
        {
            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "D";

            sbMessage.InnerMessage = message;
            DeliverToNetwork(sbMessage);
        }

        protected int IncreaseTransactionID()
        {
            transactionID++;
            return transactionID;
        }        

        protected override void OnMessageReceived(byte[] data)
        {
            // first get the general expected switchboard message
            SBMessage message = new SBMessage();

            message.ParseBytes(data);

            // send the message
            DispatchMessage(message);
        }

        protected virtual void DispatchMessage(NetworkMessage message)
        {
            // copy the messageHandlers array because the collection can be 
            // modified during the message handling. (Handlers are registered/unregistered)
            IMessageHandler[] handlers = MessageHandlers.ToArray();

            // now give the handlers the opportunity to handle the message
            foreach (IMessageHandler handler in handlers)
            {
                try
                {
                    handler.HandleMessage(this, message);
                }
                catch (Exception e)
                {
                    OnHandlerException(e);
                }
            }
        }

        protected virtual void OnHandlerException(Exception e)
        {
            MSNPSharpException MSNPSharpException = new MSNPSharpException("An exception occured while handling a switchboard message. See inner exception for more details.", e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, MSNPSharpException.InnerException.ToString() + "\r\nStacktrace:\r\n" + MSNPSharpException.InnerException.StackTrace.ToString(), GetType().Name);

            if (HandlerException != null)
                HandlerException(this, new ExceptionEventArgs(MSNPSharpException));
        }

        protected virtual void DeliverToNetwork(SBMessage sbMessage)
        {
            sbMessage.TransactionID = IncreaseTransactionID();

            if (sbMessage.InnerMessage == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + sbMessage.ToDebugString() + "\r\n", GetType().Name);
            }
            else
            {
                if (!(sbMessage.InnerMessage is SBP2PMessage))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + sbMessage.ToDebugString() + "\r\n", GetType().Name);
                }
            }

            int x = 0;

            if (sbMessage.CommandValues.Count > 1)
                int.TryParse(sbMessage.CommandValues[1].ToString(), out x);

            Debug.Assert(x < 1500, "?");



            // prepare the message
            sbMessage.PrepareMessage();

            // convert to bytes and send it over the socket
            SendSocketData(sbMessage.GetBytes());
        }

        

        public override void SendMessage(NetworkMessage message)
        {
            if (message is TextMessage)
            {
                SendMessage(message as TextMessage);
            }
            else if (message is TypingMessage)
            {
                SendMessage(message as TypingMessage);
            }
            else if (message is SBP2PMessage)
            {
                SendMessage(message as SBP2PMessage);
            }
            else if (message is MSGMessage)
            {
                SBMessage sbMessage = new SBMessage();

                sbMessage.InnerMessage = message;
                DeliverToNetwork(sbMessage);
            }
            else if (message is SBMessage)
            {
                DeliverToNetwork(message as SBMessage);
            }
            else
            {
                SBMessage sbMessage = new SBMessage();
                MSGMessage msgMessage = new MSGMessage();

                msgMessage.InnerMessage = message;
                sbMessage.InnerMessage = msgMessage;

                DeliverToNetwork(sbMessage);
            }
        }

        public override void Disconnect()
        {

            base.Disconnect();
        }
    }
};