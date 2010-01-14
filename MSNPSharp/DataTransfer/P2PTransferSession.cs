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
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// A single transfer of data within a p2p session. 
    /// </summary>
    /// <remarks>
    /// P2PTransferSession handles all messages with a specified session id in the p2p header.
    /// Optional a direct connection can be created. It will try to connect to the remote client or listening for incoming connections.
    /// If that succeeds and the local client is the sender of the data a seperate thread will be started to send data messages over the direct connection.
    /// However, if the direct connection fails it will send the data messages over the switchboard session. These latter messages go via the messenger servers and is therefore quite slow compared to direct connections
    /// but it is guaranteed to work even when both machines are behind a proxy, firewall or router.
    /// </remarks>
    public class P2PTransferSession : IMessageHandler, IMessageProcessor, IDisposable
    {
        #region Properties

        /// <summary>
        /// </summary>
        private bool autoCloseStream;

        /// <summary>
        /// Defines whether the stream is automatically closed after the transfer has finished or been aborted.
        /// </summary>
        public bool AutoCloseStream
        {
            get
            {
                return autoCloseStream;
            }
            set
            {
                autoCloseStream = value;
            }
        }

        /// <summary>
        /// </summary>
        private object clientData;

        /// <summary>
        /// This property can be used by the client-programmer to include application specific data
        /// </summary>
        public object ClientData
        {
            get
            {
                return clientData;
            }
            set
            {
                clientData = value;
            }
        }

        private uint dataPreparationAck = 0;

        /// <summary>
        /// Tracked to know when an acknowledgement for the (switchboards) data preparation message is received
        /// </summary>
        internal uint DataPreparationAck
        {
            get { return dataPreparationAck; }
            set { dataPreparationAck = value; }
        }


        /// <summary>
        /// Tracked to send the disconnecting message (0x40 flag) with the correct datamessage identifiers as it's acknowledge identifier. (protocol)
        /// </summary>
        private uint dataMessageIdentifier;

        /// <summary>
        /// </summary>
        private P2PMessageSession messageSession;

        /// <summary>
        /// The message session which keeps track of the local / remote message identifiers and redirects messages to this handler based on the session id
        /// </summary>
        public P2PMessageSession MessageSession
        {
            get
            {
                return messageSession;
            }
            set
            {
                messageSession = value;
                MessageProcessor = messageSession;
                messageSession.DirectConnectionEstablished += new EventHandler<EventArgs>(messageSession_DirectConnectionEstablished);
                messageSession.DirectConnectionFailed += new EventHandler<EventArgs>(messageSession_DirectConnectionFailed);
            }
        }


        private MSNSLPTransferProperties transferProperties;

        /// <summary>
        /// The transfer properties for this transfer session.
        /// </summary>
        public MSNSLPTransferProperties TransferProperties
        {
            get { return transferProperties; }
            private set { transferProperties = value; }
        }


        /// <summary>
        /// </summary>
        private uint messageFlag;

        /// <summary>
        /// This value is set in the flag field in a p2p header.
        /// </summary>
        /// <remarks>
        /// For filetransfers this value is for example 0x1000030
        /// </remarks>
        public uint MessageFlag
        {
            get
            {
                return messageFlag;
            }
            set
            {
                messageFlag = value;
            }
        }

        private uint messageFooter;

        /// <summary>
        /// This value is set in the footer field in a p2p header.
        /// </summary>
        public uint MessageFooter
        {
            get { return messageFooter; }
            set { messageFooter = value; }
        }


        /// <summary>
        /// The stream to read from when data is send, or to write to when data is received.
        /// </summary>
        private Stream dataStream = new MemoryStream();

        /// <summary>
        /// The stream to read from when data is send, or to write to when data is received. Default is a MemorySteam.
        /// </summary>
        /// <remarks>
        /// In the eventhandler, when an invitation is received, the client programmer must set this property in order to enable the transfer to succeed.
        /// In the case of the filetransfer, when the local client is the receiver, the incoming data is written to the specified datastream.
        /// In the case of the invitation for a msn object (display picture, emoticons, background), when the local client is the sender, the outgoing data is read from the specified datastream.
        /// </remarks>
        public Stream DataStream
        {
            get
            {
                return dataStream;
            }
            set
            {
                dataStream = value;
            }
        }


        /// <summary>
        /// </summary>
        private bool isSender;

        /// <summary>
        /// Defines whether the local client is sender or receiver
        /// </summary>
        public bool IsSender
        {
            get
            {
                return isSender;
            }
            set
            {
                isSender = value;
            }
        }

        private P2PVersion version = P2PVersion.P2PV1;

        public P2PVersion Version
        {
            get { return version; }
            set { version = value; }
        }

        private ushort dataPacketNumber = 0;

        /// <summary>
        /// The PackageNumber field used by p2pv2 messages.
        /// </summary>
        internal ushort DataPacketNumber
        {
            get
            {
                return dataPacketNumber;
            }

            set
            {
                dataPacketNumber = value;
            }
        }

        #endregion

        #region Public

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PTransferSession()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing p2p transfer session object", GetType().Name);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PTransferSession(P2PVersion ver, MSNSLPTransferProperties properties)
        {
            version = ver;
            TransferProperties = properties;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing p2p transfer session object, version = " + ver.ToString(), GetType().Name);
        }

        ~P2PTransferSession()
        {
            Dispose(false);
        }

        /// <summary>
        /// Get the next data package number for the SIP request text message, such as INVITE and BYE.
        /// </summary>
        /// <returns></returns>
        public static ushort GetNextSLPRequestDataPacketNumber(ushort baseDataPacketNumber)
        {
            if (baseDataPacketNumber < ushort.MaxValue)
                return ++baseDataPacketNumber;

                return baseDataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number to the SIP status text message, such as 200 OK and 603 Decline.
        /// </summary>
        /// <returns></returns>
        public static ushort GetNextSLPStatusDataPacketNumber(ushort baseDataPacketNumber)
        {
            if (baseDataPacketNumber > 0)
                return --baseDataPacketNumber;

            return baseDataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number for the SIP request text message, such as INVITE and BYE.
        /// </summary>
        /// <returns></returns>
        public ushort GetNextSLPRequestDataPacketNumber()
        {
            if (dataPacketNumber < ushort.MaxValue)
                return ++dataPacketNumber;

            return dataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number to the SIP status text message, such as 200 OK and 603 Decline.
        /// </summary>
        /// <returns></returns>
        public ushort GetNextSLPStatusDataPacketNumber()
        {
            if (dataPacketNumber > 0)
                return --dataPacketNumber;

            return dataPacketNumber;
        }


        /// <summary>
        /// Aborts the datatransfer, if available. This will send a P2P abort message and stop the sending thread.
        /// It will not close a direct connection. If AutoCloseStream is set to true, the datastream will be closed.
        /// <remarks>
        /// This function is called by internal.
        /// <para>If you want to abort the current transfer,call <see cref="MSNSLPHandler.CloseSession"/></para>
        /// </remarks>
        /// </summary>
        public void AbortTransfer()
        {
            if (transferThread != null && transferThread.ThreadState == System.Threading.ThreadState.Running)
            {
                AbortTransferThread();
                OnTransferAborted();
            }

            MessageSession.RemoveTransferSession(this);
            if (AutoCloseStream)
                DataStream.Close();
        }




        /// <summary>
        /// Starts a seperate thread to send the data in the stream to the remote client. It will first wait for a direct connection if tryDirectConnection is set to true.
        /// </summary>
        /// <remarks>
        /// This method will not open or close the specified datastream.
        /// </remarks>
        public void StartDataTransfer(bool tryDirectConnection)
        {
            if (transferThread != null)
            {
                throw new MSNPSharpException("Start of data transfer failed because there is already a thread sending the data.");
            }

            System.Diagnostics.Debug.Assert(TransferProperties.SessionId != 0, "Trying to initiate p2p data transfer but no session is specified");
            System.Diagnostics.Debug.Assert(dataStream != null, "Trying to initiate p2p data transfer but no session is specified");

            isSender = true;

            if (messageSession.DirectConnected == false &&
                messageSession.DirectConnectionAttempt == false &&
                tryDirectConnection == true)
            {

                waitingDirectConnection = true;
                return;
            }

            waitingDirectConnection = false;

            transferThreadStart = new ThreadStart(TransferDataEntry);
            transferThread = new Thread(transferThreadStart);
            transferThread.Start();
        }


        #region IMessageHandler Members
        /// <summary>
        /// </summary>
        private IMessageProcessor messageProcessor;
        /// <summary>
        /// The message processor to which p2p messages (this includes p2p data messages) will be send
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                messageProcessor = value;
            }
        }

        /// <summary>
        /// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Debug.Assert(p2pMessage != null, "Incoming message is not a P2PMessage", "");

            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                // Keep track of the remote identifier
                MessageSession.RemoteIdentifier = p2pMessage.Header.Identifier;

                #region P2P Version 1
                if (p2pMessage.V1Header.Flags == P2PFlag.TlpError)
                {
                    AbortTransfer();
                    return;
                }

                // check to see if our session data has been transferred correctly
                if (p2pMessage.Header.SessionId > 0 &&
                    p2pMessage.Header.IsAcknowledgement &&
                    p2pMessage.V1Header.AckSessionId == dataMessageIdentifier)
                {
                    // inform the handlers
                    OnTransferFinished();

                    return;
                }

                // check if it is a content message
                // if it is not a file transfer message, and the footer is not set to the corresponding value, ignore it.
                if (p2pMessage.Header.SessionId == TransferProperties.SessionId && p2pMessage.InnerBody.Length > 0)
                {
                    if (
                        /* m$n 7.5 (MSNC5) >=, footer: dp=12,emo=11,file=2 */
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data && p2pMessage.Footer == P2PConst.DisplayImageFooter12) ||
                        (p2pMessage.V1Header.Flags == P2PFlag.FileData && p2pMessage.Footer == P2PConst.FileTransFooter2) ||
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data && p2pMessage.Footer == (uint)P2PConst.CustomEmoticonFooter11) ||
                        /* m$n 7.0 (MSNC4) <=, footer is 1 (dp and custom emoticon) */
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data || p2pMessage.Footer == 1)
                       )
                    {
                        // indicates whether we must stream this message
                        bool writeToStream = true;

                        // check if it is a data preparation message send via the SB
                        if (p2pMessage.Header.TotalSize == 4 &&
                            p2pMessage.Header.MessageSize == 4
                            && BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
                        {
                            writeToStream = false;
                        }

                        if (writeToStream)
                        {
                            // store the data message identifier because we want to reference it if we abort the transfer
                            dataMessageIdentifier = p2pMessage.Header.Identifier;

                            if (DataStream == null)
                                throw new MSNPSharpException("Data was received in a P2P session, but no datastream has been specified to write to.");

                            if (DataStream.CanWrite)
                            {
                                if (DataStream.Length < (long)p2pMessage.V1Header.Offset + (long)p2pMessage.InnerBody.Length)
                                    DataStream.SetLength((long)p2pMessage.V1Header.Offset + (long)p2pMessage.InnerBody.Length);

                                DataStream.Seek((long)p2pMessage.V1Header.Offset, SeekOrigin.Begin);
                                DataStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);
                            }
                            // check for end of file transfer
                            if (p2pMessage.V1Header.Offset + p2pMessage.Header.MessageSize == p2pMessage.Header.TotalSize)
                            {
                                P2PMessage ack = p2pMessage.CreateAcknowledgement();
                                ack.Header.SessionId = p2pMessage.Header.SessionId;

                                SendMessage(ack);

                                if (AutoCloseStream)
                                    DataStream.Close();

                                OnTransferFinished();

                                // notify the remote client we close the session
                                SendDisconnectMessage();
                            }
                        }
                        // finished handling this message
                        return;
                    }
                }
                #endregion
            }

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Keep track of the remote identifier
                MessageSession.RemoteIdentifier = p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize;

                #region P2P Version 2

                if (p2pMessage.InnerBody.Length == 4 &&
                    p2pMessage.V2Header.TFCombination == TFCombination.First &&
                    BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
                {
                    //Data preperation message.
                    return;
                }

                // check if it is a content message
                // if it is not a file transfer message, and the footer is not set to the corresponding value, ignore it.
                if (p2pMessage.V2Header.MessageSize > 0 && p2pMessage.V2Header.SessionId == TransferProperties.SessionId)
                {
                    if (p2pMessage.V2Header.TFCombination == (TFCombination.MsnObject) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.MsnObject | TFCombination.First) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.FileTransfer) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.FileTransfer | TFCombination.First))
                    {
                        // store the data message identifier because we want to reference it if we abort the transfer
                        DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                        if (DataStream == null)
                            throw new MSNPSharpException("Data was received in a P2P session, but no datastream has been specified to write to.");

                        if (DataStream.CanWrite)
                        {
                            DataStream.Seek(0, SeekOrigin.End);
                            DataStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);
                        }
                        // check for end of file transfer
                        if (p2pMessage.V2Header.DataRemaining == 0)
                        {
                            if (AutoCloseStream)
                                DataStream.Close();

                            OnTransferFinished();

                            SendDisconnectMessage();
                        }
                        // finished handling this message


                        return;

                    }
                }

                #endregion
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                "P2P Info message received", GetType().Name);

            // It is not a datamessage. Extract the messages one-by-one and dispatch it to all handlers.
            IMessageHandler[] cpHandlers = handlers.ToArray();
            foreach (IMessageHandler handler in cpHandlers)
                handler.HandleMessage(this, p2pMessage);

        }


        #endregion

        #region IMessageProcessor Members


        /// <summary>
        /// Sends a message for this session to the message processor. If a direct connection is established,
        /// the p2p message is directly send to the message processor. If there is no direct connection available,
        /// it will wrap the incoming p2p message in a MSGMessage with the correct parameters.
        /// It also sets the identifiers and acknowledge session, provided they're not already set.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetworkMessage message)
        {
            P2PMessage p2pMessage = (P2PMessage)message;

            // Check whether it's already set. This is important to check for acknowledge messages.
            if (p2pMessage.Header.Identifier == 0)
            {
                if (Version == P2PVersion.P2PV1)
                {
                    MessageSession.IncreaseLocalIdentifier();
                    p2pMessage.Header.Identifier = MessageSession.LocalIdentifier;
                }

                if (Version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.Identifier = MessageSession.LocalIdentifier;
                    MessageSession.CorrectLocalIdentifier((int)p2pMessage.V2Header.MessageSize);
                }
            }

            #region P2P Version 1

            if (Version == P2PVersion.P2PV1)
            {
                if (p2pMessage.V1Header.AckSessionId == 0)
                {
                    p2pMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
                }

                // split up large messages which go to the SB
                if (MessageSession.DirectConnected == false &&
                    p2pMessage.Header.MessageSize > 1202)
                {
                    foreach (P2PMessage chunkMessage in p2pMessage.SplitMessage(1202))
                    {
                        //SBMessage sbMessage = WrapMessage(chunkMessage);

                        // now send it to propbably a SB processor
                        if (MessageProcessor != null)
                            MessageProcessor.SendMessage(chunkMessage);
                    }
                }
                else
                {
                    IMessageProcessor processor = null;
                    bool direct = false;
                    lock (this)
                    {
                        processor = MessageProcessor;
                        direct = MessageSession.DirectConnected;
                    }
                    // send a single message
                    if (direct)
                    {
                        // now send it to probably a P2PDirectProcessor
                        if (processor != null)
                            processor.SendMessage(p2pMessage);
                    }
                    else
                    {
                        // wrap the message before sending it to the SB processor
                        p2pMessage.PrepareMessage();
                        //SBMessage sbMessage = WrapMessage(p2pMessage);
                        if (processor != null)
                            processor.SendMessage(p2pMessage);
                    }
                }
            }

            #endregion
            
            if (Version == P2PVersion.P2PV2)
            {
                // split up large messages which go to the SB
                int totalSize = p2pMessage.InnerBody.Length;

                if (MessageSession.DirectConnected == false &&
                    p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    MessageSession.CorrectLocalIdentifier(-(int)p2pMessage.V2Header.MessageSize);

                    foreach (P2PMessage chunkMessage in p2pMessage.SplitMessage(1202))
                    {
                        MessageSession.LocalIdentifier = chunkMessage.V2Header.Identifier + chunkMessage.V2Header.MessageSize;

                        //SBMessage sbMessage = WrapMessage(chunkMessage);

                        //chunkMessage.V2Header.Identifier = MessageSession.LocalIdentifier;
                        //MessageSession.CorrectLocalIdentifier((int)chunkMessage.V2Header.MessageSize);

                        // now send it to propbably a SB processor
                        if (MessageProcessor != null)
                            MessageProcessor.SendMessage(chunkMessage);
                    }
                }
                else
                {
                    IMessageProcessor processor = null;
                    bool direct = false;
                    lock (this)
                    {
                        processor = MessageProcessor;
                        direct = MessageSession.DirectConnected;
                    }
                    // send a single message
                    if (direct)
                    {
                        // now send it to probably a P2PDirectProcessor
                        if (processor != null)
                            processor.SendMessage(p2pMessage);
                    }
                    else
                    {
                        // wrap the message before sending it to the SB processor
                        p2pMessage.PrepareMessage();
                        //SBMessage sbMessage = WrapMessage(p2pMessage);
                        if (processor != null)
                            processor.SendMessage(p2pMessage);
                    }
                }
            }
        }


        /// <summary>
        ///  Collection of handlers
        /// </summary>
        private List<IMessageHandler> handlers = new List<IMessageHandler>();

        /// <summary>
        /// Registers handlers for incoming p2p messages.
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            UnregisterHandler(handler);

            lock (handlers)
            {
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// Unregisters handlers.
        /// </summary>
        /// <param name="handler"></param>
        public void UnregisterHandler(IMessageHandler handler)
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }



        #endregion

        #endregion

        #region Protected

        /// <summary>
        /// Creates a message which is send directly after the last data message.
        /// </summary>
        /// <returns></returns>
        protected virtual SLPRequestMessage CreateClosingMessage()
        {
            SLPRequestMessage slpMessage = new SLPRequestMessage(TransferProperties.RemoteContactIDString, "BYE");
            slpMessage.ToMail = transferProperties.RemoteContactIDString;
            slpMessage.FromMail = transferProperties.LocalContactIDString;

            slpMessage.Branch = TransferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = TransferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";

            slpMessage.BodyValues["SessionID"] = TransferProperties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (transferProperties.DataType == DataTransferType.Activity)
            {
                slpMessage.BodyValues["Context"] = "dAMAgQ==";

            }

            return slpMessage;
        }

        /// <summary>
        /// Sends the remote client a p2p message with the 0x80 flag to abort.
        /// </summary>
        protected virtual void SendAbortMessage()
        {
            if (Version == P2PVersion.P2PV2)  //In p2pv2, just send a MSNSLP BYE instead.
            {
                P2PMessage p2pMessage = new P2PMessage(P2PVersion.P2PV2);
                p2pMessage.InnerMessage = CreateClosingMessage();

                p2pMessage.V2Header.TFCombination = TFCombination.First;
                p2pMessage.V2Header.PackageNumber = GetNextSLPRequestDataPacketNumber();
                MessageProcessor.SendMessage(p2pMessage);
            }

            if (Version == P2PVersion.P2PV1)
            {
                P2PMessage disconnectMessage = new P2PMessage(P2PVersion.P2PV1);
                disconnectMessage.V1Header.Flags = P2PFlag.TlpError;
                disconnectMessage.V1Header.SessionId = TransferProperties.SessionId;
                disconnectMessage.V1Header.AckSessionId = dataMessageIdentifier;
                MessageProcessor.SendMessage(disconnectMessage);
            }
        }

        /// <summary>
        /// The thread in which the data messages are send
        /// </summary>
        protected Thread TransferThread
        {
            get
            {
                return transferThread;
            }
            set
            {
                transferThread = value;
            }
        }

        /// <summary>
        /// Kickstart object to start the data transfer thread
        /// </summary>
        protected ThreadStart TransferThreadStart
        {
            get
            {
                return transferThreadStart;
            }
            set
            {
                transferThreadStart = value;
            }
        }

        /// <summary>
        /// Fires the TransferStarted event.
        /// </summary>
        protected virtual void OnTransferStarted()
        {
            if (TransferStarted != null)
                TransferStarted(this, new EventArgs());
        }

        /// <summary>
        /// Fires the TransferFinished event.
        /// </summary>
        protected virtual void OnTransferFinished()
        {
            if (TransferFinished != null)
                TransferFinished(this, new EventArgs());
        }

        /// <summary>
        /// Fires the TransferAborted event.
        /// </summary>
        protected virtual void OnTransferAborted()
        {
            if (TransferAborted != null)
                TransferAborted(this, new EventArgs());
        }

        bool abortThread;
        /// <summary>
        /// Entry point for the thread. This thread will send the data messages to the message processor.
        /// In case it is a direct connection P2PDCMessages will be send. If no direct connection is established
        /// P2PMessage objects are wrapped in a SBMessage object and send to the message processor. Which is in the latter case
        /// probably a SB processor.
        /// </summary>
        protected void TransferDataEntry()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Starting transfer thread", GetType().Name);

            OnTransferStarted();

            try
            {
                bool direct = false;

                // check whether we have a direct connection
                direct = MessageSession.DirectConnected;

                if (direct == false)
                {
                    // send the data preparation message
                    if (Version == P2PVersion.P2PV1 && 
                        (TransferProperties.DataType != DataTransferType.File && 
                        TransferProperties.DataType != DataTransferType.Unknown))
                    {
                        P2PDataMessage p2pDataMessage = new P2PDataMessage(P2PVersion.P2PV1);
                        p2pDataMessage.WritePreparationBytes();

                        p2pDataMessage.Header.SessionId = TransferProperties.SessionId;

                        MessageSession.IncreaseLocalIdentifier();
                        p2pDataMessage.Header.Identifier = MessageSession.LocalIdentifier;

                        p2pDataMessage.V1Header.AckSessionId = DataPreparationAck;
                        p2pDataMessage.Footer = MessageFooter;

                        MessageProcessor.SendMessage(p2pDataMessage);
                    }

                    if (Version == P2PVersion.P2PV2)
                    {
                        //First send a 0x08 0x02 prepare message.
                        P2PMessage prepareMessage = new P2PMessage(P2PVersion.P2PV2);
                        prepareMessage.V2Header.OperationCode = (byte)OperationCode.TransferPrepare;
                        MessageProcessor.SendMessage(prepareMessage);

                        if (TransferProperties.DataType != DataTransferType.File &&
                        TransferProperties.DataType != DataTransferType.Unknown)
                        {
                            //Then send data preparation message.
                            P2PDataMessage p2pDataMessage = new P2PDataMessage(P2PVersion.P2PV2);

                            p2pDataMessage.V2Header.OperationCode = (byte)OperationCode.None;
                            p2pDataMessage.V2Header.TFCombination = TFCombination.First;

                            p2pDataMessage.WritePreparationBytes();
                            p2pDataMessage.Header.SessionId = TransferProperties.SessionId;

                            MessageProcessor.SendMessage(p2pDataMessage);
                        }

                    }
                }

                if (Version == P2PVersion.P2PV1)
                {
                    MessageSession.IncreaseLocalIdentifier();
                }

                uint messageIdentifier = MessageSession.LocalIdentifier;

                // keep track of this identifier because it will be used in the disconnecting message.
                dataMessageIdentifier = messageIdentifier;

                long currentPosition = 0;
                long lastPosition = dataStream.Length;
                bool isFirstPacket = true;
                uint currentACK = 0;

                if (DataPreparationAck > 0)
                {
                    currentACK = DataPreparationAck;
                }
                else
                {
                    currentACK = (uint)new Random().Next(50000, int.MaxValue);
                }

                while (currentPosition < lastPosition && (!abortThread))
                {
                    if (Version == P2PVersion.P2PV1)
                    {
                        // send the data message
                        P2PDataMessage p2pDataMessage = new P2PDataMessage(P2PVersion.P2PV1);

                        p2pDataMessage.Header.SessionId = TransferProperties.SessionId;

                        p2pDataMessage.V1Header.Offset = (uint)currentPosition;
                        p2pDataMessage.Header.TotalSize = (uint)dataStream.Length;

                        // send the rest of the data
                        lock (dataStream)
                        {
                            dataStream.Seek(currentPosition, SeekOrigin.Begin);
                            int bytesWritten = p2pDataMessage.WriteBytes(dataStream, 1202);
                            currentPosition += bytesWritten;
                        }

                        p2pDataMessage.V1Header.Flags = (P2PFlag)MessageFlag;
                        p2pDataMessage.Footer = MessageFooter;
                        p2pDataMessage.Header.Identifier = messageIdentifier;
                        p2pDataMessage.V1Header.AckSessionId = currentACK;// (uint)new Random().Next(50000, int.MaxValue);

                        if (currentACK < uint.MaxValue)
                        {
                            currentACK++;
                        }
                        else
                        {
                            currentACK--;
                        }
                        MessageProcessor.SendMessage(p2pDataMessage);
                    }

                    if (Version == P2PVersion.P2PV2)
                    {
                        // send the data message
                        P2PDataMessage p2pDataMessage = new P2PDataMessage(P2PVersion.P2PV2);
                        p2pDataMessage.Header.SessionId = TransferProperties.SessionId;

                        // send the rest of the data
                        lock (dataStream)
                        {
                            dataStream.Seek(currentPosition, SeekOrigin.Begin);
                            int bytesWritten = p2pDataMessage.WriteBytes(dataStream, 1202);  //The official client set this to 1222.
                            currentPosition += bytesWritten;
                            if (lastPosition - currentPosition - 1 > 0)
                            {
                                p2pDataMessage.V2Header.DataRemaining = (ulong)(lastPosition - currentPosition);
                            }
                            p2pDataMessage.InnerBody = p2pDataMessage.InnerBody; //Refresh the MessageSize, placing MessageSize field to header is a bad desin.

                            p2pDataMessage.V2Header.PackageNumber = 1; //Always sets to 1.
                        }

                        if (isFirstPacket)
                        {
                            if (MessageFlag == (uint)P2PFlag.MSNObjectData)
                            {
                                p2pDataMessage.V2Header.TFCombination = (TFCombination.MsnObject | TFCombination.First);
                            }

                            if (MessageFlag == (uint)P2PFlag.FileData)
                            {
                                p2pDataMessage.V2Header.TFCombination = (TFCombination.FileTransfer | TFCombination.First);
                            }
                            isFirstPacket = false;
                        }
                        else
                        {
                            if (MessageFlag == (uint)P2PFlag.MSNObjectData)
                            {
                                p2pDataMessage.V2Header.TFCombination = TFCombination.MsnObject;
                            }

                            if (MessageFlag == (uint)P2PFlag.FileData)
                            {
                                p2pDataMessage.V2Header.TFCombination = TFCombination.FileTransfer;
                            }
                        }

                        Thread.Sleep(300);                        
                        MessageProcessor.SendMessage(p2pDataMessage);
                    }
                }
            }
            catch (System.Net.Sockets.SocketException sex)
            {
                if (sex.ErrorCode == 10053)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "You've closed a connection: " + sex.ToString(), GetType().Name);
                }

                abortThread = true;
                OnTransferAborted();
            }
            catch (ObjectDisposedException oex)
            {
                abortThread = true;
                MessageSession.CloseDirectConnection();

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Exception in transfer thread: " + oex.ToString(), GetType().Name);
            }
            //catch (Exception e)
            //{
            //    OnTransferAborted();

            //    if (Settings.TraceSwitch.TraceInfo)
            //        System.Diagnostics.Trace.WriteLine("Exception in transfer thread: " + e.ToString(), "P2PTransferSession");

            //    //SendAbortMessage();

            //    //throw new MSNPSharpException("Exception Occurred while sending p2p data. See inner exception for details.", e);
            //}
            finally
            {
                if (Version == P2PVersion.P2PV2)
                {
                    OnTransferFinished();
                    SendDisconnectMessage();
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Stopping transfer thread", GetType().Name);
            }
        }


        /// <summary>
        /// Sends the remote client a p2p message with the 0x40 flag to indicate we are going to close the connection.
        /// </summary>
        protected virtual void SendDisconnectMessage()
        {
            if (Version == P2PVersion.P2PV2)  //In p2pv2, just send a MSNSLP BYE instead.
            {
                P2PMessage p2pMessage = new P2PMessage(P2PVersion.P2PV2);
                p2pMessage.InnerMessage = CreateClosingMessage();

                p2pMessage.V2Header.TFCombination = TFCombination.First;
                p2pMessage.V2Header.PackageNumber = GetNextSLPRequestDataPacketNumber();
                MessageProcessor.SendMessage(p2pMessage);
            }

            if (Version == P2PVersion.P2PV1)
            {
                P2PMessage disconnectMessage = new P2PMessage(P2PVersion.P2PV1);
                disconnectMessage.V1Header.Flags = P2PFlag.CloseSession;
                disconnectMessage.Header.SessionId = TransferProperties.SessionId;
                disconnectMessage.V1Header.AckSessionId = dataMessageIdentifier; // aargh it took me long to figure this one out
                MessageProcessor.SendMessage(disconnectMessage);
            }
        }

        /// <summary>
        /// Aborts a running data transfer thread.
        /// </summary>
        protected virtual void AbortTransferThread()
        {
            if (transferThread != null && transferThread.ThreadState == System.Threading.ThreadState.Running)
            {
                //transferThread.Abort();
                abortThread = true;
            }
        }


        #endregion

        #region Private


        /// <summary>
        /// </summary>
        private Thread transferThread;

        /// <summary>
        /// </summary>
        private ThreadStart transferThreadStart;

        /// <summary>
        /// </summary>
        private bool waitingDirectConnection;

        /// <summary>
        /// Indicates whether the session is waiting for the result of a direct connection attempt
        /// </summary>
        protected bool WaitingDirectConnection
        {
            get
            {
                return waitingDirectConnection;
            }
            set
            {
                waitingDirectConnection = value;
            }
        }


        #endregion

        #region Events

        /// <summary>
        /// Occurs when the sending of data messages has started.
        /// </summary>
        public event EventHandler<EventArgs> TransferStarted;
        /// <summary>
        /// Occurs when the sending of data messages has finished.
        /// </summary>
        public event EventHandler<EventArgs> TransferFinished;
        /// <summary>
        /// Occurs when the transfer of data messages has been aborted.
        /// </summary>
        public event EventHandler<EventArgs> TransferAborted;

        #endregion

        /// <summary>
        /// Start the transfer session if it is waiting for a direct connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageSession_DirectConnectionEstablished(object sender, EventArgs e)
        {
            if (waitingDirectConnection == true)
            {
                waitingDirectConnection = false;
                StartDataTransfer(true);
            }
        }

        /// <summary>
        /// Start the transfer session if it is waiting for a direct connection. Because the direct connection attempt failed the transfer will be over the switchboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageSession_DirectConnectionFailed(object sender, EventArgs e)
        {
            if (waitingDirectConnection)
            {
                waitingDirectConnection = false;
                StartDataTransfer(false);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (dataStream != null)
                    dataStream.Dispose();
            }

            // Free native resources
        }

        #endregion
    }
};
