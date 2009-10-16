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
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Buffers incompleted P2PMessage SLP messages. Data and control messages are ignored.
    /// </summary>
    public class P2PMessagePool
    {
        private Dictionary<uint, P2PMessage> splittedP2PV1Messages = new Dictionary<uint, P2PMessage>();
        private Dictionary<uint, P2PMessage> splittedP2PV2Messages = new Dictionary<uint, P2PMessage>();

        public P2PMessagePool()
        {
        }

        /// <summary>
        /// Buffers incompleted P2PMessage SLP messages. Ignores data and control messages. 
        /// </summary>
        /// <param name="p2pMessage"></param>
        /// <returns>true if the P2PMessage is buffering; false if the p2p message buffered or no need to buffer.</returns>
        public bool BufferMessage(P2PMessage p2pMessage)
        {
            // P2PV1 and P2PV2 check
            if (p2pMessage.Header.MessageSize == 0 || // Ack message or Unsplitted
                p2pMessage.Header.SessionId > 0) // Data message
            {

                return false; // No need to buffer
            }

            // P2PV2 pooling
            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                if ((p2pMessage.V2Header.TFCombination == TFCombination.First && p2pMessage.V2Header.DataRemaining == 0) || // Unsplitted SLP message or data preparation message
                    (p2pMessage.V2Header.TFCombination > TFCombination.First)) // Data message
                {

                    return false; // No need to buffer
                }

                // First splitted SLP message.
                if (p2pMessage.V2Header.TFCombination == TFCombination.First &&
                    p2pMessage.V2Header.DataRemaining > 0)
                {
                    lock (splittedP2PV2Messages)
                        splittedP2PV2Messages[p2pMessage.V2Header.Identifier + p2pMessage.V2Header.MessageSize] = p2pMessage;

                    return true; // Buffering
                }

                // Other splitted SLP messages
                if (p2pMessage.V2Header.TFCombination == TFCombination.None)
                {
                    lock (splittedP2PV2Messages)
                    {
                        if (splittedP2PV2Messages.ContainsKey(p2pMessage.V2Header.Identifier))
                        {
                            if (splittedP2PV2Messages[p2pMessage.V2Header.Identifier].V2Header.PackageNumber == p2pMessage.V2Header.PackageNumber)
                            {
                                uint originalIdentifier = p2pMessage.V2Header.Identifier;
                                P2PMessage lastMessage = splittedP2PV2Messages[p2pMessage.V2Header.Identifier];

                                long dataSize = lastMessage.V2Header.MessageSize - lastMessage.V2Header.DataPacketHeaderLength;
                                dataSize += (long)(lastMessage.V2Header.DataRemaining - p2pMessage.V2Header.DataRemaining);

                                lastMessage.V2Header.DataRemaining = p2pMessage.V2Header.DataRemaining;
                                lastMessage.V2Header.MessageSize = (uint)(dataSize + lastMessage.V2Header.DataPacketHeaderLength);

                                byte[] newPayLoad = new byte[lastMessage.InnerBody.Length + p2pMessage.InnerBody.Length];
                                byte[] newbytMessage = new byte[lastMessage.V2Header.GetBytes().Length + newPayLoad.Length + 4];

                                Array.Copy(lastMessage.InnerBody, newPayLoad, lastMessage.InnerBody.Length);
                                Array.Copy(p2pMessage.InnerBody, 0, newPayLoad, lastMessage.InnerBody.Length, p2pMessage.InnerBody.Length);

                                Array.Copy(lastMessage.V2Header.GetBytes(), newbytMessage, lastMessage.V2Header.GetBytes().Length);
                                Array.Copy(newPayLoad, 0, newbytMessage, lastMessage.V2Header.GetBytes().Length, newPayLoad.Length);

                                P2PMessage newMessage = new P2PMessage(P2PVersion.P2PV2);
                                newMessage.ParseBytes(newbytMessage);

                                uint newIdentifier = p2pMessage.V2Header.Identifier + p2pMessage.V2Header.MessageSize;
                                splittedP2PV2Messages[newIdentifier] = newMessage;
                                newMessage.V2Header.Identifier = newIdentifier;

                                if (newIdentifier != originalIdentifier)
                                {
                                    splittedP2PV2Messages.Remove(originalIdentifier);
                                }

                                if (newMessage.V2Header.DataRemaining > 0)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Buffering splitted messages :\r\n" + newMessage.ToDebugString());
                                    return true; // Buffering
                                }
                                else // Last part
                                {
                                    newMessage.V2Header.Identifier -= newMessage.V2Header.MessageSize;
                                    splittedP2PV2Messages.Remove(newIdentifier);

                                    p2pMessage.V2Header.TFCombination = TFCombination.First;
                                    p2pMessage.V2Header.Identifier = newMessage.V2Header.Identifier;
                                    p2pMessage.V2Header.DataRemaining = 0; // Remove data packet TLVs
                                    p2pMessage.InnerBody = newMessage.InnerBody;

                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                                        "A splitted message was combined :\r\n" +
                                        p2pMessage.ToDebugString());

                                    return false; // We have the whole message
                                }
                            }

                            // Invalid packet received!!! Ignore and delete it...
                            splittedP2PV2Messages.Remove(p2pMessage.V2Header.Identifier);

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                "INVALID P2PV2 PACKET received!!! Ignored and deleted:\r\n" +
                                p2pMessage.ToDebugString());

                            return false; // Invalid packet, don't kill me.
                        }
                    }
                }
            }
            else // P2PV1 pooling
            {
                if ((p2pMessage.V1Header.MessageSize == p2pMessage.V1Header.TotalSize) || // Whole data
                    ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data)) // Data message
                {
                    return false; // No need to buffer
                }

                lock (splittedP2PV1Messages)
                {
                    if (false == splittedP2PV1Messages.ContainsKey(p2pMessage.Header.Identifier))
                    {
                        byte[] totalPayload = new byte[p2pMessage.V1Header.TotalSize];
                        Array.Copy(p2pMessage.InnerBody, 0, totalPayload, (long)p2pMessage.V1Header.Offset, (long)p2pMessage.V1Header.MessageSize);
                        P2PMessage copyMessage = new P2PMessage(p2pMessage);

                        copyMessage.InnerBody = totalPayload;
                        copyMessage.V1Header.Offset = p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize;

                        splittedP2PV1Messages[p2pMessage.Header.Identifier] = copyMessage;
                        return true; // Buffering
                    }

                    P2PMessage totalMessage = splittedP2PV1Messages[p2pMessage.Header.Identifier];
                    if ((totalMessage.V1Header.TotalSize != p2pMessage.V1Header.TotalSize) ||
                        (p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize) > totalMessage.Header.TotalSize)
                    {
                        // Invalid packet received!!! Ignore and delete it...
                        splittedP2PV1Messages.Remove(p2pMessage.Header.Identifier);

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            "INVALID P2PV1 PACKET received!!! Ignored and deleted:\r\n" +
                            p2pMessage.ToDebugString());

                        return false; // Invalid packet, don't kill me.
                    }

                    Array.Copy(p2pMessage.InnerBody, 0, totalMessage.InnerBody, (long)p2pMessage.V1Header.Offset, (long)p2pMessage.V1Header.MessageSize);
                    totalMessage.V1Header.Offset = p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize;

                    if (totalMessage.V1Header.Offset == p2pMessage.V1Header.TotalSize)
                    {
                        p2pMessage.V1Header.Offset = 0;
                        p2pMessage.V1Header.MessageSize = (uint)p2pMessage.V1Header.TotalSize;
                        p2pMessage.InnerBody = totalMessage.InnerBody;

                        splittedP2PV1Messages.Remove(p2pMessage.Header.Identifier);

                        return false; // We have the whole message
                    }
                }
            }

            return false; // Invalid packet...
        }
    }
};
