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

namespace MSNPSharp.DataTransfer
{
    using System;
    using System.Collections;
    using System.IO;
    using MSNPSharp.Core;
    using MSNPSharp;

    /// <summary>
    /// Buffers incomplete P2PMessage and releases them when the message is fully received.
    /// </summary>
    public class P2PMessagePool
    {
        /// <summary>
        /// </summary>
        private Hashtable messageStreamsV1 = new Hashtable();
        private Hashtable messageStreamsV2 = new Hashtable();

        /// <summary>
        /// </summary>
        private Queue availableMessagesV1 = new Queue(1);
        private Queue availableMessagesV2 = new Queue(1);

        /// <summary>
        /// Constructor
        /// </summary>
        public P2PMessagePool()
        {

        }

        /// <summary>
        /// Buffers the incoming raw data internal.This method is often used after receiving incoming data from a socket or another source.
        /// </summary>
        /// <param name="message">The message to buffer.</param>
        public void BufferMessage(P2PMessage message)
        {
            // assume the start of a p2p message

            if (message.Version == P2PVersion.P2PV1)
            {
                if (message.Header.IsAcknowledgement ||
                    message.Header.MessageSize == 0 ||
                    message.Header.MessageSize == message.Header.TotalSize)
                {
                    availableMessagesV1.Enqueue(message);
                }
                else
                {
                    // if this message already exists in the buffer append the current p2p message to the buffer
                    if (messageStreamsV1.ContainsKey(message.Header.Identifier))
                    {
                        ((MemoryStream)messageStreamsV1[message.Header.Identifier]).Write(message.InnerBody, 0, message.InnerBody.Length);
                    }
                    else
                    {
                        MemoryStream bufferStream = new MemoryStream();
                        messageStreamsV1.Add(message.Header.Identifier, bufferStream);
                        bufferStream.Write(message.InnerBody, 0, message.InnerBody.Length);
                    }

                    // check whether this is the last message
                    if (message.V1Header.Offset + message.Header.MessageSize == message.Header.TotalSize)
                    {
                        // set the correct fields to match the whole message
                        message.V1Header.Offset = 0;
                        message.Header.MessageSize = (uint)message.Header.TotalSize;
                        MemoryStream bufferStream = (MemoryStream)messageStreamsV1[message.Header.Identifier];

                        // set the inner body to the whole message
                        message.InnerBody = bufferStream.ToArray();

                        // and make it available for the client
                        availableMessagesV1.Enqueue(message);

                        // remove the old memorystream buffer, and clear up resources in the hashtable
                        bufferStream.Close();
                        messageStreamsV1.Remove(message.Header.Identifier);
                    }
                }
            }

            if (message.Version == P2PVersion.P2PV2)
            {
                if (message.Header.IsAcknowledgement ||
                    message.InnerBody == null ||
                    message.InnerBody.Length == 0)
                {
                    availableMessagesV2.Enqueue(message);
                }
                else
                {
                    // if this message already exists in the buffer append the current p2p message to the buffer

                    // XXX TODO?
                    byte[] innerBytes = message.GetBytes();
                    if (messageStreamsV2.ContainsKey(message.Header.Identifier))
                    {
                        ((MemoryStream)messageStreamsV2[message.Header.Identifier]).Write(innerBytes, 0, innerBytes.Length);
                    }
                    else
                    {
                        MemoryStream bufferStream = new MemoryStream();
                        messageStreamsV2.Add(message.Header.Identifier, bufferStream);
                        bufferStream.Write(innerBytes, 0, innerBytes.Length);
                    }

                    //// check whether this is the last message
                    //if (message.Offset + message.MessageSize == message.TotalSize)
                    //{
                    //    // set the correct fields to match the whole message
                    //    message.Offset = 0;
                    //    message.MessageSize = (uint)message.TotalSize;
                    //    MemoryStream bufferStream = (MemoryStream)messageStreams[message.Identifier];

                    //    // set the inner body to the whole message
                    //    message.InnerBody = bufferStream.ToArray();

                    //    // and make it available for the client
                    //    availableMessages.Enqueue(message);

                    //    // remove the old memorystream buffer, and clear up resources in the hashtable
                    //    bufferStream.Close();
                    //    messageStreams.Remove(message.Identifier);
                    //}
                }
            }
        }

        /// <summary>
        /// Defines whether there is a message available to retrieve
        /// </summary>
        public bool MessageAvailable(P2PVersion version)
        {
            if (version == P2PVersion.P2PV2)
                return availableMessagesV2.Count > 0;
            return availableMessagesV1.Count > 0;
        }

        /// <summary>
        /// Retrieves the next p2p message from the buffer.
        /// </summary>
        /// <returns></returns>
        public P2PMessage GetNextMessage(P2PVersion msgVersion)
        {
            System.Diagnostics.Debug.Assert(availableMessagesV1.Count > 0, "No p2p messages available in queue");
            if (msgVersion == P2PVersion.P2PV2)
                return availableMessagesV2.Dequeue() as P2PMessage;

            return availableMessagesV1.Dequeue() as P2PMessage;
        }
    }
};
