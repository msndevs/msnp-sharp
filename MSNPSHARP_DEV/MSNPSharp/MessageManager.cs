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

namespace MSNPSharp.Utilities
{
    public class MessageManager : IDisposable
    {
        #region Events

        public event EventHandler<TypingArrivedEventArgs> TypingMessageReceived;
        public event EventHandler<NudgeArrivedEventArgs> NudgeReceived;
        public event EventHandler<TextMessageArrivedEventArgs> TextMessageReceived;

        #endregion

        #region Fields and Properties

        private NSMessageHandler _nsHandler;

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
            _nsHandler.TypingMessageReceived += (_nsHandler_TypingMessageReceived);
            _nsHandler.NudgeReceived += (_nsHandler_NudgeReceived);
            _nsHandler.TextMessageReceived += (_nsHandler_TextMessageReceived);
        }

        private void DetachNSEvents()
        {
            _nsHandler.TypingMessageReceived -= (_nsHandler_TypingMessageReceived);
            _nsHandler.NudgeReceived -= (_nsHandler_NudgeReceived);
            _nsHandler.TextMessageReceived -= (_nsHandler_TextMessageReceived);
        }

        private void _nsHandler_TypingMessageReceived(object sender, TypingArrivedEventArgs e)
        {
            if (TypingMessageReceived != null)
                TypingMessageReceived(this, e);
        }

        private void _nsHandler_NudgeReceived(object sender, NudgeArrivedEventArgs e)
        {
            if (NudgeReceived != null)
                NudgeReceived(this, e);
        }

        private void _nsHandler_TextMessageReceived(object sender, TextMessageArrivedEventArgs e)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, e);
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
    }
};
