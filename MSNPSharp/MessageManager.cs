#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp;
    using MSNPSharp.P2P;
    using MSNPSharp.Apps;

    public class MessageManager : IDisposable
    {
        #region Events

        public event EventHandler<TypingArrivedEventArgs> TypingMessageReceived;
        public event EventHandler<NudgeArrivedEventArgs> NudgeReceived;
        public event EventHandler<TextMessageArrivedEventArgs> TextMessageReceived;

        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;
        public event EventHandler<WinkEventArgs> WinkDefinitionReceived;

        public event EventHandler<EmoticonArrivedEventArgs> EmoticonReceived;
        public event EventHandler<WinkEventArgs> WinkReceived;

        #endregion

        #region Fields and Properties

        private NSMessageHandler _nsHandler;
        private bool autoRequestObjects = true;

        /// <summary>
        /// The <see cref="NSMessageHandler"/> instance this manager connected to.
        /// </summary>
        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return _nsHandler;
            }
        }

        /// <summary>
        /// Indicates whether emoticons and winks from remote contacts are automatically retrieved
        /// </summary>
        public bool AutoRequestObjects
        {
            get
            {
                return autoRequestObjects;
            }
            set
            {
                autoRequestObjects = value;
            }
        }

        #endregion

        #region .ctor

        private MessageManager()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MessageManager(NSMessageHandler nsHandler)
        {
            this._nsHandler = nsHandler;

            AttachNSEvents();
        }

        public void Dispose()
        {
            if (_nsHandler != null)
            {
                DetachNSEvents();
                _nsHandler = null;
            }
        }

        private void AttachNSEvents()
        {
            _nsHandler.TypingMessageReceived += (OnTypingMessageReceived);
            _nsHandler.NudgeReceived += (OnNudgeReceived);
            _nsHandler.TextMessageReceived += (OnTextMessageReceived);

            _nsHandler.EmoticonDefinitionReceived += (OnEmoticonDefinitionReceived);
            _nsHandler.WinkDefinitionReceived += (OnWinkDefinitionReceived);
        }

        private void DetachNSEvents()
        {
            _nsHandler.TypingMessageReceived -= (OnTypingMessageReceived);
            _nsHandler.NudgeReceived -= (OnNudgeReceived);
            _nsHandler.TextMessageReceived -= (OnTextMessageReceived);

            _nsHandler.EmoticonDefinitionReceived -= (OnEmoticonDefinitionReceived);
            _nsHandler.WinkDefinitionReceived -= (OnWinkDefinitionReceived);
        }

        protected virtual void OnTypingMessageReceived(object sender, TypingArrivedEventArgs e)
        {
            if (TypingMessageReceived != null)
                TypingMessageReceived(this, e);
        }

        protected virtual void OnNudgeReceived(object sender, NudgeArrivedEventArgs e)
        {
            if (NudgeReceived != null)
                NudgeReceived(this, e);
        }

        protected virtual void OnTextMessageReceived(object sender, TextMessageArrivedEventArgs e)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, e);
        }


        protected virtual void OnEmoticonReceived(object sender, EmoticonArrivedEventArgs e)
        {
            if (EmoticonReceived != null)
                EmoticonReceived(this, e);
        }

        protected virtual void OnEmoticonDefinitionReceived(object sender, EmoticonDefinitionEventArgs e)
        {
            if (!autoRequestObjects)
                return;

            MSNObject existing = MSNObjectCatalog.GetInstance().Get(e.Emoticon.CalculateChecksum());
            if (existing == null)
            {
                e.Sender.Emoticons[e.Emoticon.Sha] = e.Emoticon;

                // create a session and send the invitation
                ObjectTransfer emoticonTransfer = _nsHandler.P2PHandler.RequestMsnObject(e.Sender, e.Emoticon);
                emoticonTransfer.TransferAborted += (emoticonTransfer_TransferAborted);
                emoticonTransfer.TransferFinished += (emoticonTransfer_TransferFinished);

                MSNObjectCatalog.GetInstance().Add(e.Emoticon);

                if (EmoticonDefinitionReceived != null)
                    EmoticonDefinitionReceived(this, e);
            }
            else
            {
                if (EmoticonDefinitionReceived != null)
                    EmoticonDefinitionReceived(this, e);

                //If exists, fire the event.
                OnEmoticonReceived(this, new EmoticonArrivedEventArgs(e.Sender, existing as Emoticon, null, e.RoutingInfo));
            }
        }

        private void emoticonTransfer_TransferAborted(object objectTransfer, ContactEventArgs e)
        {
            ObjectTransfer session = objectTransfer as ObjectTransfer;
            session.TransferAborted -= (emoticonTransfer_TransferAborted);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon aborted", GetType().Name);
        }

        private void emoticonTransfer_TransferFinished(object objectTransfer, EventArgs e)
        {
            ObjectTransfer session = objectTransfer as ObjectTransfer;
            session.TransferFinished -= (emoticonTransfer_TransferFinished);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon received", GetType().Name);

            OnEmoticonReceived(this, new EmoticonArrivedEventArgs(session.Remote, session.Object as Emoticon, null, null));
        }

        protected virtual void OnWinkReceived(object sender, WinkEventArgs e)
        {
            if (WinkReceived != null)
                WinkReceived(this, e);
        }

        protected virtual void OnWinkDefinitionReceived(object sender, WinkEventArgs e)
        {
            if (!autoRequestObjects)
                return;

            MSNObject existing = MSNObjectCatalog.GetInstance().Get(e.Wink.CalculateChecksum());
            if (existing == null)
            {
                // create a session and send the invitation
                ObjectTransfer winkTransfer = _nsHandler.P2PHandler.RequestMsnObject(e.Sender, e.Wink);
                winkTransfer.TransferAborted += (winkTransfer_TransferAborted);
                winkTransfer.TransferFinished += (winkTransfer_TransferFinished);

                MSNObjectCatalog.GetInstance().Add(e.Wink);

                if (WinkDefinitionReceived != null)
                    WinkDefinitionReceived(this, e);
            }
            else
            {
                if (WinkDefinitionReceived != null)
                    WinkDefinitionReceived(this, new WinkEventArgs(e.Sender, existing as Wink, e.RoutingInfo));

                //If exists, fire the event.
                OnWinkReceived(this, new WinkEventArgs(e.Sender, existing as Wink, e.RoutingInfo));
            }
        }

        private void winkTransfer_TransferAborted(object objectTransfer, ContactEventArgs e)
        {
            ObjectTransfer session = objectTransfer as ObjectTransfer;
            session.TransferAborted -= (winkTransfer_TransferAborted);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Wink aborted", GetType().Name);
        }

        private void winkTransfer_TransferFinished(object objectTransfer, EventArgs e)
        {
            ObjectTransfer session = objectTransfer as ObjectTransfer;
            session.TransferFinished -= (winkTransfer_TransferFinished);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Wink received", GetType().Name);

            OnWinkReceived(this, new WinkEventArgs(session.Remote, session.Object as Wink, null));
        }


        #endregion

        #region Public methods


        public void SendNudge(Contact remote)
        {
            remote.SendNudge();
        }

        public void SendTypingMessage(Contact remote)
        {
            remote.SendTypingMessage();
        }

        public void SendTextMessage(Contact remote, TextMessage message)
        {
            remote.SendMessage(message);
        }

        public void SendEmoticonDefinitions(Contact remote, List<Emoticon> emoticons, EmoticonType icontype)
        {
            remote.SendEmoticonDefinitions(emoticons, icontype);
        }

        #endregion

        #region Deprecated

        [Obsolete("Switchboard conversation is deprecated. http://code.google.com/p/msnp-sharp/issues/detail?id=263", true)]
        public object GetID(Contact contact)
        {
            return null;
        }

        #endregion
    }
};
