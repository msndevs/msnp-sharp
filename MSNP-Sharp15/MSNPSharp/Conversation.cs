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
using System.IO;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    
    public class MSNObjectDataTransferCompletedEventArgs : EventArgs
    {
        private MSNObject clientData;
        private bool aborted;

        /// <summary>
        /// Transfer failed.
        /// </summary>
        public bool Aborted
        {
            get { return aborted; }
        }

        /// <summary>
        /// The target msnobject.
        /// </summary>
        public MSNObject ClientData
        {
            get { return clientData; }
        }

        protected MSNObjectDataTransferCompletedEventArgs()
            : base()
        {
        }

        public MSNObjectDataTransferCompletedEventArgs(MSNObject clientdata,bool abort)
        {
            if (clientdata == null)
                throw new NullReferenceException();
            clientData = clientdata;
            aborted = abort;
        }
    }

    public class ConversationEndEventArgs : EventArgs
    {
        private Conversation conversation = null;

        public Conversation Conversation
        {
            get { return conversation; }
        }

        protected ConversationEndEventArgs()
            : base()
        {
        }

        public ConversationEndEventArgs(Conversation convers)
        {
            conversation = convers;
        }
    }

    /// <summary>
    /// A facade to the underlying switchboard and YIM session.
    /// </summary>
    /// <remarks>
    /// Conversation implements a few features for the ease of the application programmer. It provides
    /// directly basic common functionality. However, if you need to perform more advanced actions, or catch
    /// other events you have to directly use the underlying switchboard handler, or switchboard processor.
    /// </b>
    /// Conversation automatically requests emoticons used by remote contacts.
    /// </remarks>
    public class Conversation
    {
        #region Private
        private Messenger _messenger = null;
        private SBMessageHandler _switchboard = Factory.CreateSwitchboardHandler();
        private YIMMessageHandler _yimHandler = Factory.CreateYIMMessageHandler();
        private bool sbInitialized = false;

        private bool yimInitialized = false;

        private bool autoRequestEmoticons = true;

        /// <summary>
        /// Automatically requests emoticons used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sbHandler_EmoticonDefinitionReceived(object sender, EmoticonDefinitionEventArgs e)
        {
            if (AutoRequestEmoticons == false)
                return;

            MSNObject existing = MSNObjectCatalog.GetInstance().Get(e.Emoticon.CalculateChecksum());
            if (existing == null)
            {
                e.Sender.Emoticons[e.Emoticon.Shortcut] = e.Emoticon;

                // create a session and send the invitation
                P2PMessageSession session = Messenger.P2PHandler.GetSession(Messenger.Nameserver.Owner.Mail, e.Sender.Mail);

                object handlerObject = session.GetHandler(typeof(MSNSLPHandler));
                if (handlerObject != null)
                {
                    MSNSLPHandler msnslpHandler = (MSNSLPHandler)handlerObject;

                    P2PTransferSession transferSession = msnslpHandler.SendInvitation(session.LocalContact, session.RemoteContact, e.Emoticon);
                    transferSession.DataStream = e.Emoticon.OpenStream();
                    transferSession.ClientData = e.Emoticon;

                    transferSession.TransferAborted += new EventHandler<EventArgs>(transferSession_TransferAborted);
                    transferSession.TransferFinished += new EventHandler<EventArgs>(transferSession_TransferFinished);

                    MSNObjectCatalog.GetInstance().Add(e.Emoticon);
                }
                else
                    throw new MSNPSharpException("No MSNSLPHandler was attached to the p2p message session. An emoticon invitation message could not be send.");
            }
            else
            {
                //If exists, fire the event.
                OnMSNObjectDataTransferCompleted(sender, new MSNObjectDataTransferCompletedEventArgs(existing, false));
            }
        }

        private void transferSession_TransferAborted(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon aborted", GetType().Name);
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs((sender as P2PTransferSession).ClientData as MSNObject, true));
        }

        private void transferSession_TransferFinished(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon received", GetType().Name);
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs((sender as P2PTransferSession).ClientData as MSNObject, false));

        }

        void _switchboard_AllContactsLeft(object sender, EventArgs e)
        {
            _switchboard.AllContactsLeft -= _switchboard_AllContactsLeft;
            End();
        }

        #endregion


        #region Protected

        protected void OnMSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            if (MSNObjectDataTransferCompleted != null)
                MSNObjectDataTransferCompleted(sender, e);
        }


        protected virtual void OnConversationEnded(Conversation conversation)
        {
            if (ConversationEnded != null)
            {
                ConversationEnded(this, new ConversationEndEventArgs(conversation));
            }
        }

        #endregion

        #region Public
        /// <summary>
        /// Fired when the data transfer for a MSNObject finished or aborted.
        /// </summary>
        public event EventHandler<MSNObjectDataTransferCompletedEventArgs> MSNObjectDataTransferCompleted;

        /// <summary>
        /// Occurs when a new conversation is ended (all contacts in the conversation have left or <see cref="Conversation.End()"/> is called).
        /// </summary>
        public event EventHandler<ConversationEndEventArgs> ConversationEnded;

        /// <summary>
        /// Indicates whether emoticons from remote contacts are automatically retrieved
        /// </summary>
        public bool AutoRequestEmoticons
        {
            get
            {
                return autoRequestEmoticons;
            }
            set
            {
                autoRequestEmoticons = value;
            }
        }

        /// <summary>
        /// Messenger that created the conversation
        /// </summary>
        public Messenger Messenger
        {
            get
            {
                return _messenger;
            }
            set
            {
                _messenger = value;
            }
        }

        /// <summary>
        /// Indicates whether the switchboard is available for sending messages.
        /// </summary>
        public bool SwitchBoardInitialized
        {
            get { return sbInitialized; }
        }

        /// <summary>
        /// Indicates whether the YIM Message Handler is available for sending messages.
        /// </summary>
        public bool YimHandlerInitialized
        {
            get { return yimInitialized; }
        }

        /// <summary>
        /// The switchboard processor. Sends and receives messages over the switchboard connection
        /// </summary>
        public SocketMessageProcessor SwitchboardProcessor
        {
            get
            {
                return (SocketMessageProcessor)_switchboard.MessageProcessor;
            }
            set
            {
                _switchboard.MessageProcessor = value;
            }
        }

        /// <summary>
        /// The switchboard handler. Handles incoming/outgoing messages.
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                return _switchboard;
            }
            set
            {
                _switchboard = value;
            }
        }

        /// <summary>
        /// Yahoo! Message handler.
        /// </summary>
        public YIMMessageHandler YIMHandler
        {
            get { return _yimHandler; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        /// <param name="sbHandler">The switchboard to interface to.</param>		
        public Conversation(Messenger parent, SBMessageHandler sbHandler)
        {
            if (sbHandler is YIMMessageHandler)
            {
                _yimHandler = sbHandler as YIMMessageHandler;
                sbInitialized = false;
                yimInitialized = true;
            }
            else
            {
                _switchboard = sbHandler;
                sbInitialized = true;
                yimInitialized = false;
            }
            _messenger = parent;
            _switchboard.EmoticonDefinitionReceived += new EventHandler<EmoticonDefinitionEventArgs>(sbHandler_EmoticonDefinitionReceived);
            _switchboard.AllContactsLeft += new EventHandler<EventArgs>(_switchboard_AllContactsLeft);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        public Conversation(Messenger parent)
        {
            _messenger = parent;
            sbInitialized = false;
            yimInitialized = false;
            _switchboard.EmoticonDefinitionReceived += new EventHandler<EmoticonDefinitionEventArgs>(sbHandler_EmoticonDefinitionReceived);
            _switchboard.AllContactsLeft +=new EventHandler<EventArgs>(_switchboard_AllContactsLeft);
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contactMail"></param>
        /// <param name="type"></param>
        public void Invite(string contactMail, ClientType type)
        {

            if (Switchboard != null)
            {
                if (type == ClientType.EmailMember)
                {
                    if (!yimInitialized)
                    {
                        _yimHandler.NSMessageHandler = Messenger.Nameserver;
                        _yimHandler.MessageProcessor = Messenger.Nameserver.MessageProcessor;
                        if (!Messenger.Nameserver.SwitchBoards.Contains(_yimHandler))
                        {
                            Messenger.Nameserver.SwitchBoards.Add(_yimHandler);
                        }
                        Messenger.Nameserver.MessageProcessor.RegisterHandler(_yimHandler);
                        yimInitialized = true;
                        _yimHandler.Invite(contactMail);
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "YIM hanlder inviting user: " + contactMail);
                    }
                    
                    return;
                }

                if (Switchboard.NSMessageHandler == null)
                    Switchboard.NSMessageHandler = Messenger.Nameserver;

                if (sbInitialized == false)
                {
                    Messenger.Nameserver.RequestSwitchboard(_switchboard, this);
                    _switchboard.SessionEstablished += delegate(object sender, EventArgs e)
                    {
                        Switchboard.Invite(contactMail);
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard inviting user: " + contactMail);
                    };

                    _switchboard.SessionClosed += delegate(object sender, EventArgs e)
                    {
                        sbInitialized = false;
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard session closed.");
                        return;
                    };

                    sbInitialized = true;
                    return;
                }
                
                Switchboard.Invite(contactMail);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard inviting user: " + contactMail);
            }
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contact">The remote contact to invite.</param>
        public void Invite(Contact contact)
        {
            Invite(contact.Mail, contact.ClientType);
        }

        /// <summary>
        /// End this conversation.
        /// </summary>
        public void End()
        {
            
            if (sbInitialized)
            {
                Switchboard.Close();
                sbInitialized = false;
            }

            if (yimInitialized)
            {
                YIMHandler.Close();
                yimInitialized = false;
            }

            OnConversationEnded(this);
        }

        #endregion

        
    }
};
