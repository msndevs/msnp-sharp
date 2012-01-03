#region
/*
Copyright (c) 2002-2012, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan, Chang Liu. 
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
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MSNPSharp.P2P
{
    public delegate void AckHandler(P2PMessage ack);

    #region P2PMessage EventArgs

    [Serializable]
    public class P2PMessageEventArgs : EventArgs
    {
        private P2PMessage p2pMessage;
        public P2PMessage P2PMessage
        {
            get
            {
                return p2pMessage;
            }
        }

        public P2PMessageEventArgs(P2PMessage p2pMessage)
        {
            this.p2pMessage = p2pMessage;
        }
    }

    [Serializable]
    public class P2PAckMessageEventArgs : P2PMessageEventArgs
    {
        private int deleteTick = 0;
        private AckHandler ackHandler;

        public AckHandler AckHandler
        {
            get
            {
                return ackHandler;
            }
        }

        public int DeleteTick
        {
            get
            {
                return deleteTick;
            }
            set
            {
                deleteTick = value;
            }
        }

        public P2PAckMessageEventArgs(P2PMessage p2pMessage, AckHandler ackHandler, int timeoutSec)
            : base(p2pMessage)
        {
            this.ackHandler = ackHandler;
            this.deleteTick = timeoutSec;

            if (timeoutSec != 0)
            {
                AddSeconds(timeoutSec);
            }
        }

        public void AddSeconds(int secs)
        {
            DeleteTick = unchecked(Environment.TickCount + (secs * 1000));
        }
    }

    [Serializable]
    public class P2PMessageSessionEventArgs : P2PMessageEventArgs
    {
        private P2PSession p2pSession;
        public P2PSession P2PSession
        {
            get
            {
                return p2pSession;
            }
        }

        public P2PMessageSessionEventArgs(P2PMessage p2pMessage, P2PSession p2pSession)
            : base(p2pMessage)
        {
            this.p2pSession = p2pSession;
        }
    }

    #endregion
 
    /// <summary>
    /// The base class for P2P Transport Layer. 
    /// </summary>   
    public abstract class P2PBridge : IDisposable
    {
        public const int DefaultTimeout = 10;
        
        /// <summary>
        /// The maximum time for a bridge to waiting for coneection. 
        /// </summary>
        public const int MaxTimeout = 180;

        #region Events & Fields

        /// <summary>
        /// Fired after this bridge was opened.
        /// </summary>
        public event EventHandler<EventArgs> BridgeOpened;
        /// <summary>
        /// Fired after the connect negotiation has been completed. 
        /// </summary>
        public event EventHandler<EventArgs> BridgeSynced;
        /// <summary>
        /// Fired after the bridge ends its lifecycle. 
        /// </summary>
        public event EventHandler<EventArgs> BridgeClosed;
        public event EventHandler<P2PMessageSessionEventArgs> BridgeSent;

        private static uint bridgeCount = 0;

        private uint sequenceId = 0;
        private uint syncId = 0;
        private ushort packageNo = 0;

        protected uint bridgeID = ++bridgeCount;
        protected int queueSize = 0;
        private NSMessageHandler nsMessageHandler = null;

        protected Dictionary<P2PSession, P2PSendQueue> sendQueues = new Dictionary<P2PSession, P2PSendQueue>();
        protected Dictionary<P2PSession, P2PSendList> sendingQueues = new Dictionary<P2PSession, P2PSendList>();
        protected List<P2PSession> stoppedSessions = new List<P2PSession>();

        private Dictionary<uint, P2PAckMessageEventArgs> ackHandlersV1 = new Dictionary<uint, P2PAckMessageEventArgs>();
        private Dictionary<uint, P2PAckMessageEventArgs> ackHandlersV2 = new Dictionary<uint, P2PAckMessageEventArgs>();
        private DateTime nextCleanup = DateTime.MinValue;
        private volatile int inCleanup = 0;
        private object syncObject;
        const int CleanupIntervalSecs = 5;

        #endregion

        #region Properties
        /// <summary>
        /// Check wether the bridge has been opened. 
        /// </summary>
        public abstract bool IsOpen
        {
            get;
        }
  
        /// <summary>
        /// The maximum data package szie for the bridge. 
        /// </summary>      
        public abstract int MaxDataSize
        {
            get;
        }

        public abstract Contact Remote
        {
            get;
        }
  
        /// <summary>
        /// Check whether the connect negotiation has been completed.
        /// You can transfer data only after a bridge after synced. 
        /// </summary>      
        public virtual bool Synced
        {
            get
            {
                return (0 != SyncId);
            }
        }

        public virtual uint SyncId
        {
            get
            {
                return syncId;
            }
            protected internal set
            {
                syncId = value;

                if (0 != value)
                {
                    OnBridgeSynced(EventArgs.Empty);
                }
            }
        }
  
        /// <summary>
        /// The current sequence ID for this bridge (Transport Layer). 
        /// </summary>
        public virtual uint SequenceId
        {
            get
            {
                return sequenceId;
            }
            internal protected set
            {
                sequenceId = value;
            }
        }

        public ushort PackageNo
        {
            get
            {
                return packageNo;
            }
            internal protected set
            {
                packageNo = value;
            }
        }
  
        /// <summary>
        /// The holding data queues to be sent in this bridge.
        /// </summary>      
        public virtual Dictionary<P2PSession, P2PSendQueue> SendQueues
        {
            get
            {
                return sendQueues;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public object SyncObject
        {
            get
            {
                if (syncObject == null)
                {
                    Interlocked.CompareExchange(ref syncObject, new object(), null);
                }

                return syncObject;
            }
        }

        #endregion

        /// <summary>
        /// P2PBridge constructor.
        /// </summary>
        protected P2PBridge(int queueSize, NSMessageHandler nsHandler)
        {
            this.queueSize = queueSize;
            this.sequenceId = (uint)new Random().Next(5000, int.MaxValue);
            this.nextCleanup = NextCleanupTime();
            this.nsMessageHandler = nsHandler;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} created", this.ToString()), GetType().Name);
        }

        /// <summary>
        /// Release the resources used by <see cref="P2PBridge"/>
        /// </summary>
        public virtual void Dispose()
        {
            lock (sendQueues)
                sendQueues.Clear();

            lock (sendingQueues)
                sendingQueues.Clear();

            lock (stoppedSessions)
                stoppedSessions.Clear();

            lock (ackHandlersV1)
                ackHandlersV1.Clear();

            lock (ackHandlersV2)
                ackHandlersV2.Clear();

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} disposed", this.ToString()), GetType().Name);
        }

        private DateTime NextCleanupTime()
        {
            return DateTime.Now.AddMinutes(CleanupIntervalSecs);
        }

        private void RunRakCleanupIfNecessary()
        {
            if (nextCleanup < DateTime.Now)
            {
                List<P2PMessage> raks = new List<P2PMessage>();

                if (nextCleanup < DateTime.Now)
                {
                    inCleanup++;
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        String.Format("Running RAK cleanup for {0}...", this.ToString()));

                    nextCleanup = NextCleanupTime();
                    int tickcount = Environment.TickCount + 2000; // Give +2 secs change...

                    List<uint> ackstodelete = new List<uint>();

                    lock (ackHandlersV1)
                    {
                        // P2Pv1
                        foreach (KeyValuePair<uint, P2PAckMessageEventArgs> pair in ackHandlersV1)
                        {
                            if (pair.Value.DeleteTick != 0 && pair.Value.DeleteTick < tickcount)
                            {
                                ackstodelete.Add(pair.Key);
                                raks.Add(pair.Value.P2PMessage);
                            }
                        }
                        foreach (uint i in ackstodelete)
                        {
                            ackHandlersV1.Remove(i);
                        }
                    }

                    ackstodelete.Clear();

                    lock (ackHandlersV2)
                    {
                        // P2Pv2
                        foreach (KeyValuePair<uint, P2PAckMessageEventArgs> pair in ackHandlersV2)
                        {
                            if (pair.Value.DeleteTick != 0 && pair.Value.DeleteTick < tickcount)
                            {
                                ackstodelete.Add(pair.Key);
                                raks.Add(pair.Value.P2PMessage);
                            }
                        }
                        foreach (uint i in ackstodelete)
                        {
                            ackHandlersV2.Remove(i);
                        }
                    }

                    GC.Collect();

                    inCleanup = 0;
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        String.Format("End RAK cleanup for {0}...", this.ToString()));
                }

                if (raks.Count > 0)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            String.Format("Unacked RAK packets for {0}...", this.ToString()));

                    foreach (P2PMessage rak in raks)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, rak.ToDebugString());
                    }
                }
            }
        }
  
        /// <summary>
        /// Check whether the P2P session can be transfer on the bridge. 
        /// </summary>
        /// <param name="session">
        /// The <see cref="P2PSession"/> needs to check.
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>      
        public virtual bool SuitableFor(P2PSession session)
        {
            Contact remote = Remote;

            return (session != null) && (remote != null) && (session.Remote.IsSibling(remote));
        }

        /// <summary>
        /// Check if the session is ready to begin sending packets.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual bool Ready(P2PSession session)
        {
            lock (stoppedSessions)
            {
                if (queueSize == 0)
                    return IsOpen && (!stoppedSessions.Contains(session));

                lock (sendingQueues)
                {
                    if (!sendingQueues.ContainsKey(session))
                        return IsOpen && SuitableFor(session) && (!stoppedSessions.Contains(session));

                    return IsOpen && (sendingQueues[session].Count < queueSize) && (!stoppedSessions.Contains(session));
                }
            }
        }
  
        /// <summary>
        /// Send a P2P Message to the specified P2PSession. 
        /// </summary>
        /// <param name="session">
        /// The application layer, which is a <see cref="P2PSession"/>.
        /// </param>
        /// <param name="remote">
        /// The receiver <see cref="Contact"/>.
        /// </param>
        /// <param name="remoteGuid">
        /// A <see cref="Guid"/>
        /// </param>
        /// <param name="msg">
        /// The <see cref="P2PMessage"/> to be sent.
        /// </param>      
        public virtual void Send(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg)
        {
            Send(session, remote, remoteGuid, msg, 0, null);
        }
  
        /// <summary>
        /// Send a P2P Message to the specified P2PSession. 
        /// </summary>
        /// <param name="session">
        /// The application layer, which is a <see cref="P2PSession"/>
        /// </param>
        /// <param name="remote">
        /// he receiver <see cref="Contact"/>
        /// </param>
        /// <param name="remoteGuid">
        /// A <see cref="Guid"/>
        /// </param>
        /// <param name="msg">
        /// he <see cref="P2PMessage"/> to be sent.
        /// </param>
        /// <param name="ackTimeout">
        /// The maximum time to wait for an ACK. <see cref="System.Int32"/>
        /// </param>
        /// <param name="ackHandler">
        /// The <see cref="AckHandler"/> to handle the ACK.
        /// </param>      
        public virtual void Send(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg, int ackTimeout, AckHandler ackHandler)
        {
            if (remote == null)
                throw new ArgumentNullException("remote");

            P2PMessage[] msgs = SetSequenceNumberAndRegisterAck(session, remote, msg, ackHandler, ackTimeout);

            if (session == null)
            {
                if (!IsOpen)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Send called with no session on a closed bridge", GetType().Name);

                    return;
                }

                // Bypass queueing
                foreach (P2PMessage m in msgs)
                {
                    SendOnePacket(null, remote, remoteGuid, m);
                }

                return;
            }

            if (!SuitableFor(session))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Send called with a session this bridge is not suitable for", GetType().Name);
                return;
            }

            lock (sendQueues)
            {
                if (!sendQueues.ContainsKey(session))
                    sendQueues[session] = new P2PSendQueue();
            }
            lock (sendQueues[session])
            {
                foreach (P2PMessage m in msgs)
                    sendQueues[session].Enqueue(remote, remoteGuid, m);
            }

            ProcessSendQueues();
        }

        private P2PMessage[] SetSequenceNumberAndRegisterAck(P2PSession session, Contact remote, P2PMessage p2pMessage, AckHandler ackHandler, int timeout)
        {
            if (p2pMessage.Header.Identifier == 0)
            {
                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    p2pMessage.Header.Identifier = ++sequenceId;
                }
                else if (p2pMessage.Version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.Identifier = sequenceId;
                }
            }

            if (p2pMessage.Version == P2PVersion.P2PV1 && p2pMessage.V1Header.AckSessionId == 0)
            {
                p2pMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }
            if (p2pMessage.Version == P2PVersion.P2PV2 && p2pMessage.V2Header.PackageNumber == 0)
            {
                p2pMessage.V2Header.PackageNumber = packageNo;
            }

            P2PMessage[] msgs = p2pMessage.SplitMessage(MaxDataSize);

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Correct local sequence no
                P2PMessage lastMsg = msgs[msgs.Length - 1];
                SequenceId = lastMsg.V2Header.Identifier + lastMsg.V2Header.MessageSize;
            }

            if (ackHandler != null)
            {
                P2PMessage firstMessage = msgs[0];
                RegisterAckHandler(new P2PAckMessageEventArgs(firstMessage, ackHandler, timeout));
            }

            if (session != null)
            {
                session.LocalIdentifier = SequenceId;
            }

            return msgs;
        }

        protected virtual void RegisterAckHandler(P2PAckMessageEventArgs e)
        {
            if (e.P2PMessage.Version == P2PVersion.P2PV2)
            {
                lock (ackHandlersV2)
                {
                    e.P2PMessage.V2Header.OperationCode |= (byte)OperationCode.RAK;
                    ackHandlersV2[e.P2PMessage.V2Header.Identifier + e.P2PMessage.Header.MessageSize] = e;
                }
            }
            else if (e.P2PMessage.Version == P2PVersion.P2PV1)
            {
                lock (ackHandlersV1)
                {
                    ackHandlersV1[e.P2PMessage.V1Header.AckSessionId] = e;
                }
            }
        }

        internal bool HandleACK(P2PMessage p2pMessage)
        {
            bool isAckOrNak = false;

            if (p2pMessage.Header.IsAcknowledgement || p2pMessage.Header.IsNegativeAck)
            {
                P2PAckMessageEventArgs e = null;
                uint ackNakId = 0;
                isAckOrNak = true;

                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    lock (ackHandlersV1)
                    {
                        if (ackHandlersV1.ContainsKey(p2pMessage.Header.AckIdentifier))
                        {
                            ackNakId = p2pMessage.Header.AckIdentifier;
                            e = ackHandlersV1[ackNakId];
                            ackHandlersV1.Remove(ackNakId);
                        }
                    }
                }
                else if (p2pMessage.Version == P2PVersion.P2PV2)
                {
                    lock (ackHandlersV2)
                    {
                        if (ackHandlersV2.ContainsKey(p2pMessage.Header.AckIdentifier))
                        {
                            ackNakId = p2pMessage.Header.AckIdentifier;
                            e = ackHandlersV2[ackNakId];
                            ackHandlersV2.Remove(ackNakId);
                        }
                        else if (ackHandlersV2.ContainsKey(p2pMessage.V2Header.NakIdentifier))
                        {
                            ackNakId = p2pMessage.V2Header.NakIdentifier;
                            e = ackHandlersV2[ackNakId];
                            ackHandlersV2.Remove(ackNakId);
                        }
                    }
                }

                if (ackNakId == 0)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                        String.Format("!!!!!! No AckHandler registered for ack/nak {0}:\r\n{1}", p2pMessage.Header.AckIdentifier, p2pMessage.ToDebugString()), GetType().Name);
                }
                else
                {
                    if (e != null && e.AckHandler != null)
                    {
                        e.AckHandler(p2pMessage);
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                            String.Format("!!!!!! No AckHandler pair for ack {0}\r\n{1}", ackNakId, p2pMessage.ToDebugString()), GetType().Name);
                    }
                }
            }

            return isAckOrNak;
        }

        #region HandleRAK

        internal bool HandleRAK(Contact source, Guid sourceGuid, P2PMessage msg)
        {
            bool requireAck = false;

            if (msg.Header.RequireAck)
            {
                requireAck = true;

                P2PMessage ack = msg.CreateAcknowledgement();

                if (ack.Header.RequireAck)
                {
                    // SYN
                    Send(null, source, sourceGuid, ack, DefaultTimeout, delegate(P2PMessage sync)
                    {
                        SyncId = sync.Header.AckIdentifier;

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            String.Format("SYNC completed for: {0}", this), GetType().Name);
                    });
                }
                else
                {
                    // ACK
                    Send(null, source, sourceGuid, ack);
                }
            }

            return requireAck;
        }

        #endregion

        #region

        public void ProcessP2PMessage(Contact source, Guid sourceGuid, P2PMessage p2pMessage)
        {
            // HANDLE RAK: RAKs are session independent and mustn't be quoted on bridges.
            bool requireAck = HandleRAK(source, sourceGuid, p2pMessage);

            // HANDLE ACK: ACK/NAK to our RAK message
            if (HandleACK(p2pMessage))
            {
                return;
            }

            // PASS TO P2PHandler
            if (nsMessageHandler.P2PHandler.ProcessP2PMessage(this, source, sourceGuid, p2pMessage))
            {
                return;
            }

            if (!requireAck)
            {
                // UNHANDLED P2P MESSAGE
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                    String.Format("*******Unhandled P2P message ****** \r\n{0}", p2pMessage.ToDebugString()), GetType().Name);

                // Keep RemoteID Synchronized, I think we must track remoteIdentifier here...
                // Send NAK??????

            }
        }


        #endregion

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void ProcessSendQueues()
        {
            lock (sendQueues)  // lock at the queue level, since it might be modified.
            {
                Dictionary<P2PSession, P2PSendQueue> sendQueuesCopy = new Dictionary<P2PSession, P2PSendQueue>(sendQueues);

                foreach (KeyValuePair<P2PSession, P2PSendQueue> pair in sendQueuesCopy)
                {
                    while (Ready(pair.Key) && (pair.Value.Count > 0))
                    {
                        P2PSendItem item = pair.Value.Dequeue();

                        if (!sendingQueues.ContainsKey(pair.Key))
                        {
                            lock (sendingQueues)
                                sendingQueues.Add(pair.Key, new P2PSendList());
                        }

                        lock (sendingQueues[pair.Key])
                            sendingQueues[pair.Key].Add(item);

                        SendOnePacket(pair.Key, item.Remote, item.RemoteGuid, item.P2PMessage);
                    }
                }

                bool moreQueued = false;
                foreach (KeyValuePair<P2PSession, P2PSendQueue> pair in sendQueues)
                {
                    if (pair.Value.Count > 0)
                    {
                        moreQueued = true;
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            String.Format("Queue holds {0} messages for session {1}", pair.Value.Count, pair.Key.SessionId), GetType().Name);
                    }
                }

                if (!moreQueued)
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Queues are all empty", GetType().Name);

                RunRakCleanupIfNecessary();
            }
        }

        protected abstract void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg);
        protected virtual void SendMultiPacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage[] sendList)
        {
            foreach (P2PMessage p2pMessage in sendList)
            {
                SendOnePacket(session, remote, remoteGuid, p2pMessage);
            }
        }

        /// <summary>
        /// Stop sending future messages to the specified <see cref="P2PSession"/>.
        /// </summary>
        /// <param name="session"></param>
        public virtual void StopSending(P2PSession session)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} stop sending for {1}", this.ToString(), session.SessionId), GetType().Name);
            lock (stoppedSessions)
            {
                if (!stoppedSessions.Contains(session))
                {

                    stoppedSessions.Add(session);
                }
                else
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Session is already in stopped list", GetType().Name);
            }
        }

        /// <summary>
        /// Continue to send future messages to the specified <see cref="P2PSession"/>.
        /// </summary>
        /// <param name="session"></param>
        public virtual void ResumeSending(P2PSession session)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} resume sending for {1}", this.ToString(), session.SessionId), GetType().Name);
            lock (stoppedSessions)
            {
                if (stoppedSessions.Contains(session))
                {
                    stoppedSessions.Remove(session);

                    ProcessSendQueues();
                }
                else
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Session not present in stopped list", GetType().Name);
            }
        }

        /// <summary>
        /// Move the specified <see cref="P2PSession"/> to the new <see cref="P2PBridge"/> specified.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="newBridge"></param>
        public virtual void MigrateQueue(P2PSession session, P2PBridge newBridge)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} migrating session {1} queue to new bridge {2}",
               this.ToString(), session.SessionId, (newBridge != null) ? newBridge.ToString() : "null"), GetType().Name);

            P2PSendQueue newQueue = new P2PSendQueue();
            lock (sendingQueues)
            {
                if (sendingQueues.ContainsKey(session))
                {
                    if (newBridge != null)
                    {
                        lock (sendingQueues[session])
                        {
                            foreach (P2PSendItem item in sendingQueues[session])
                                newQueue.Enqueue(item);
                        }
                    }

                    sendingQueues.Remove(session);
                }
            }

            lock (sendQueues)
            {
                if (sendQueues.ContainsKey(session))
                {
                    if (newBridge != null)
                    {
                        while (sendQueues[session].Count > 0)
                            newQueue.Enqueue(sendQueues[session].Dequeue());
                    }

                    sendQueues.Remove(session);
                }
            }

            lock (stoppedSessions)
            {
                if (stoppedSessions.Contains(session))
                {
                    stoppedSessions.Remove(session);
                }
            }

            if (newBridge != null)
                newBridge.AddQueue(session, newQueue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="queue"></param>
        public virtual void AddQueue(P2PSession session, P2PSendQueue queue)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} received queue for session {1}", this.ToString(), session.SessionId), GetType().Name);

            if (sendQueues.ContainsKey(session))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    "A queue is already present for this session, merging the queues", GetType().Name);

                lock (sendQueues[session])
                {
                    while (queue.Count > 0)
                        sendQueues[session].Enqueue(queue.Dequeue());
                }
            }
            else
            {
                lock (sendQueues)
                    sendQueues[session] = queue;
            }

            ProcessSendQueues();
        }

        protected virtual void OnBridgeOpened(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} opened", this.ToString()), GetType().Name);

            if (BridgeOpened != null)
                BridgeOpened(this, e);

            ProcessSendQueues();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnBridgeSynced(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("{0} synced, sync id: {1}", this.ToString(), SyncId), GetType().Name);

            if (BridgeSynced != null)
                BridgeSynced(this, e);
        }

        /// <summary>
        /// Called after the bridge was closed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBridgeClosed(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} closed", this.ToString()), GetType().Name);

            if (BridgeClosed != null)
                BridgeClosed(this, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBridgeSent(P2PMessageSessionEventArgs e)
        {
            P2PSession session = e.P2PSession;

            lock (sendingQueues)
            {
                if ((session != null) && sendingQueues.ContainsKey(session))
                {
                    if (sendingQueues[session].Contains(e.P2PMessage))
                    {
                        sendingQueues[session].Remove(e.P2PMessage);
                    }
                    else
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                            "Sent message not present in sending queue", GetType().Name);
                    }
                }
            }

            if (BridgeSent != null)
                BridgeSent(this, e);

            ProcessSendQueues();
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", bridgeID, GetType().Name);
        }
    }
};
