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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Used in events where a P2PMessageSession object is created, or in another way affected.
    /// </summary>
    public class P2PSessionAffectedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PMessageSession session;

        /// <summary>
        /// The affected session
        /// </summary>
        public P2PMessageSession Session
        {
            get
            {
                return session;
            }
            set
            {
                session = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="affectedSession"></param>
        public P2PSessionAffectedEventArgs(P2PMessageSession affectedSession)
        {
            session = affectedSession;
        }
    }

    /// <summary>
    /// Handles incoming P2P messages from the switchboardserver.
    /// </summary>
    public partial class P2PHandler : IMessageHandler
    {
        /// <summary>
        /// </summary>
        private ArrayList messageSessions = new ArrayList();

        /// <summary>
        /// A list of all current p2p message sessions. Multiple threads can access this resource so make sure to lock this.
        /// </summary>
        public ArrayList MessageSessions
        {
            get
            {
                return messageSessions;
            }
        }


        /// <summary>
        /// Aborts and cleans up all running messagesessions and their transfersessions.
        /// </summary>
        public void ClearMessageSessions()
        {
            lock (MessageSessions)
            {
                foreach (P2PMessageSession session in MessageSessions)
                {
                    session.AbortAllTransfers();
                }
            }
            MessageSessions.Clear();
        }



        /// <summary>
        /// </summary>
        private List<SBMessageHandler> switchboardSessions = new List<SBMessageHandler>(0);

        /// <summary>
        /// A collection of all available switchboard sessions
        /// </summary>
        internal List<SBMessageHandler> SwitchboardSessions
        {
            get
            {
                return switchboardSessions;
            }
        }

        /// <summary>
        /// Occurs when a P2P session is created.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionCreated;

        /// <summary>
        /// Occurs when a P2P session is closed.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionClosed;

        /// <summary>
        /// Fires the SessionCreated event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionCreated(P2PMessageSession session)
        {
            if (SessionCreated != null)
                SessionCreated(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionClosed(P2PMessageSession session)
        {
            if (SessionClosed != null)
                SessionClosed(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Gets the p2p message session for which the remote identifier equals the identifier passed as a parameter.
        /// This is typically called when an incoming message is processed.
        /// </summary>
        /// <param name="remoteContact">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PSession GetSessionFromRemote(string remoteContact)
        {
            lock (sessions)
            {
                foreach (P2PSession session in sessions)
                {
                    if (session.Remote.Mail == remoteContact)
                        return session;
                }
            }
            return null;
        }


        /// <summary>
        /// Creates a session based on a message received from the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        protected P2PMessageSession CreateSessionFromRemote2(P2PMessage receivedMessage)
        {
            P2PMessageSession session = Factory.CreateP2PMessageSession();

            // generate a local base identifier. After the acknowledgement the locals client begins at
            // identifier - 4 as identifiers in the following messages. (Weird)
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));
            session.LocalIdentifier = (uint)(session.LocalBaseIdentifier);// - (ulong)session.LocalInitialCorrection);

            //session.ProcessorInvalid += session_ProcessorInvalid;

            // setup the remote identifier
            session.RemoteBaseIdentifier = receivedMessage.Identifier;
            session.RemoteIdentifier = receivedMessage.Identifier;

            return session;
        }


        /// <summary>
        /// Protected constructor.
        /// </summary>
        protected P2PHandler()
        {

        }

        #region IMessageHandler Members
        private IMessageProcessor messageProcessor;
        /// <summary>
        /// The message processor that will send the created P2P messages to the remote contact.
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



        #endregion



        /// <summary>
        /// Add a switchboard handler to the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void AddSwitchboardSession(SBMessageHandler session)
        {
            lock (SwitchboardSessions)
            {
                if (SwitchboardSessions.Contains(session) == false)
                    SwitchboardSessions.Add(session);
            }
        }

        /// <summary>
        /// Removes a switchboard handler from the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void RemoveSwitchboardSession(SBMessageHandler session)
        {
            int remainCount = 0;
            lock (SwitchboardSessions)
            {
                SwitchboardSessions.Remove(session);
                remainCount = SwitchboardSessions.Count;
            }
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A " + session.GetType().ToString() + " has been removed.");
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "There is/are " + remainCount.ToString() + " switchboard(s) remain(s) unclosed.");
        }



        /// <summary>
        /// Removes the messageprocessor from the specified messagesession, because it is invalid.
        /// </summary>
        protected virtual void RemoveSessionProcessor(P2PSession session)
        {
            session.MessageProcessor = null;
        }

        /// <summary>
        /// Sets the specified messageprocessor as the default messageprocessor for the message session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="processor"></param>
        protected virtual void AddSessionProcessor(P2PSession session, IMessageProcessor processor)
        {
            System.Threading.Thread.CurrentThread.Join(1000);
            session.MessageProcessor = processor;
        }





        /// <summary>
        /// Closes a message session.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void CloseMessageSession(P2PMessageSession session)
        {
            // make sure there are no sessions and references left
            session.AbortAllTransfers();
            session.CleanUp();

            // call the event
            OnSessionClosed(session);

            // and remove the session from the list
            lock (messageSessions)
            {
                messageSessions.Remove(session);
            }
        }

        /// <summary>
        /// Requests a new switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void session2_ProcessorInvalid(object sender, EventArgs e)
        {
            P2PMessageSession session = (P2PMessageSession)sender;

            // create a new switchboard to fill the hole
            SBMessageHandler sbHandler = GetSwitchboardSession(session.RemoteContact);

            // if the contact is offline, there is no need to request a new switchboard. close the session.
            if ((NSMessageHandler.ContactList[session.RemoteContact, ClientType.PassportMember]).Status == PresenceStatus.Offline)
            {
                CloseMessageSession(session);
                return;
            }

            // check whether the switchboard handler is valid and has a valid processor.
            // if that is the case, use that processor. Otherwise request a new session.
            if (sbHandler == null || sbHandler.MessageProcessor == null ||
                ((SocketMessageProcessor)sbHandler.MessageProcessor).Connected == false ||
                (sbHandler.Contacts.ContainsKey(session.RemoteContact) == false))
            {
                RequestSwitchboard(session.RemoteContact);
            }
            else
            {
                session.MessageProcessor = sbHandler.MessageProcessor;
            }
        }

        /// <summary>
        /// Removes the session from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            RemoveSwitchboardSession((SBMessageHandler)sender);
        }


    }
};
