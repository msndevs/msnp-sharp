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

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Buffers incomplete P2PMessage and releases them when the message is fully received.
    /// </summary>
    public class P2PMessagePool
    {
        /// <summary>
        /// </summary>
        private Hashtable messageStreamsV1 = new Hashtable();
        private Dictionary<uint, P2PMessage> messageStreamsV2 = new Dictionary<uint, P2PMessage>();

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

            #region P2P Version 1 message pooling

            if (message.Version == P2PVersion.P2PV1)
            {
                if (message.Header.IsAcknowledgement ||
                    message.Header.MessageSize == 0 ||
                    message.Header.MessageSize == message.Header.TotalSize)
                {
                    lock (availableMessagesV1)
                        availableMessagesV1.Enqueue(message);
                }
                else
                {
                    lock (messageStreamsV1)
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
                            lock (availableMessagesV1)
                                availableMessagesV1.Enqueue(message);

                            // remove the old memorystream buffer, and clear up resources in the hashtable
                            bufferStream.Close();
                            messageStreamsV1.Remove(message.Header.Identifier);
                        }
                    }
                }
            }
            #endregion

            #region P2P Version 2 message pooling

            if (message.Version == P2PVersion.P2PV2)
            {
                // ACK, Unsplitted SLP messages, data preparation messages, p2p data messages, donot buffer.
                if ((message.V2Header.MessageSize == 0) ||
                    (message.V2Header.TFCombination == TFCombination.First && message.V2Header.DataRemaining == 0) ||
                    (message.V2Header.TFCombination > TFCombination.First))
                {
                    lock (availableMessagesV2)
                        availableMessagesV2.Enqueue(message);
                }
                else
                {
                    if (message.V2Header.TFCombination == TFCombination.First && message.V2Header.DataRemaining > 0)
                    {
                        lock (messageStreamsV2)
                        {
                            //First splitted SLP message.
                            messageStreamsV2[message.V2Header.Identifier + message.V2Header.MessageSize] = message;
                        }
                    }

                    if (message.V2Header.TFCombination == TFCombination.None)
                    {
                        lock (messageStreamsV2)
                        {
                            if (messageStreamsV2.ContainsKey(message.V2Header.Identifier))
                            {
                                if (messageStreamsV2[message.V2Header.Identifier].V2Header.PackageNumber == message.V2Header.PackageNumber)
                                {
                                    uint originalIdentifier = message.V2Header.Identifier;
                                    P2PMessage lastMessage = messageStreamsV2[message.V2Header.Identifier];

                                    long dataSize = lastMessage.V2Header.MessageSize - lastMessage.V2Header.DataPacketHeaderLength;
                                    dataSize += (long)(lastMessage.V2Header.DataRemaining - message.V2Header.DataRemaining);

                                    lastMessage.V2Header.DataRemaining = message.V2Header.DataRemaining;
                                    lastMessage.V2Header.MessageSize = (uint)(dataSize + lastMessage.V2Header.DataPacketHeaderLength);

                                    byte[] newPayLoad = new byte[lastMessage.InnerBody.Length + message.InnerBody.Length];
                                    byte[] newbytMessage = new byte[lastMessage.V2Header.GetBytes().Length + newPayLoad.Length + 4];

                                    Array.Copy(lastMessage.InnerBody, newPayLoad, lastMessage.InnerBody.Length);
                                    Array.Copy(message.InnerBody, 0, newPayLoad, lastMessage.InnerBody.Length, message.InnerBody.Length);

                                    Array.Copy(lastMessage.V2Header.GetBytes(), newbytMessage, lastMessage.V2Header.GetBytes().Length);
                                    Array.Copy(newPayLoad, 0, newbytMessage, lastMessage.V2Header.GetBytes().Length, newPayLoad.Length);

                                    P2PMessage newMessage = new P2PMessage(P2PVersion.P2PV2);
                                    newMessage.ParseBytes(newbytMessage);

                                    uint newIdentifier = message.V2Header.Identifier + message.V2Header.MessageSize;
                                    messageStreamsV2[newIdentifier] = newMessage;
                                    newMessage.V2Header.Identifier = newIdentifier;

                                    if (newIdentifier != originalIdentifier)
                                    {
                                        messageStreamsV2.Remove(originalIdentifier);
                                    }

                                    if (newMessage.V2Header.DataRemaining == 0)
                                    {
                                        newMessage.V2Header.Identifier -= newMessage.V2Header.MessageSize;
                                        messageStreamsV2.Remove(newIdentifier);

                                        lock (availableMessagesV2)
                                            availableMessagesV2.Enqueue(newMessage);

                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A splitted message was combined :\r\n" + newMessage.ToDebugString());
                                    }
                                    else
                                    {
                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Buffering splitted messages :\r\n" + newMessage.ToDebugString());
                                    }

                                }
                                else
                                {
                                    lock (availableMessagesV2)
                                        availableMessagesV2.Enqueue(message);  //Maybe there are errors, pass the message to the upper layer.
                                }
                            }
                            else
                            {
                                lock (availableMessagesV2)
                                    availableMessagesV2.Enqueue(message); //Maybe there are errors, pass the message to the upper layer.
                            }
                        }
                    }

                }
            #endregion

            }
        }

        /// <summary>
        /// Defines whether there is a message available to retrieve
        /// </summary>
        public bool MessageAvailable(P2PVersion version)
        {
            if (version == P2PVersion.P2PV2)
            {
                lock (availableMessagesV2)
                    return availableMessagesV2.Count > 0;
            }

            lock (availableMessagesV1)
                return availableMessagesV1.Count > 0;
        }

        /// <summary>
        /// Retrieves the next p2p message from the buffer.
        /// </summary>
        /// <returns></returns>
        public P2PMessage GetNextMessage(P2PVersion msgVersion)
        {

            if (msgVersion == P2PVersion.P2PV2)
            {
                lock (availableMessagesV2)
                {
                    System.Diagnostics.Debug.Assert(availableMessagesV2.Count > 0, "No p2pv2 messages available in queue");
                    return availableMessagesV2.Dequeue() as P2PMessage;
                }
            }

            lock (availableMessagesV1)
            {
                System.Diagnostics.Debug.Assert(availableMessagesV1.Count > 0, "No p2pv1 messages available in queue");
                return availableMessagesV1.Dequeue() as P2PMessage;
            }
        }
    }
};
