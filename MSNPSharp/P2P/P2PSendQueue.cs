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
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    #region P2PSendItem

    public struct P2PSendItem
    {
        public Contact Remote;
        public Guid RemoteGuid;
        public P2PMessage P2PMessage;

        public P2PSendItem(Contact remote, Guid remoteGuid, P2PMessage message)
        {
            Remote = remote;
            RemoteGuid = remoteGuid;
            P2PMessage = message;
        }
    }

    #endregion

    #region P2PSendQueue

    public class P2PSendQueue : Queue<P2PSendItem>
    {
        public void Enqueue(Contact remote, Guid remoteGuid, P2PMessage message)
        {
            base.Enqueue(new P2PSendItem(remote, remoteGuid, message));
        }
    }

    #endregion

    #region P2PSendList

    public class P2PSendList : List<P2PSendItem>
    {
        public void Add(Contact remote, Guid remoteGuid, P2PMessage message)
        {
            lock (this)
            {
                base.Add(new P2PSendItem(remote, remoteGuid, message));
            }
        }

        public bool Contains(P2PMessage msg)
        {
            lock (this)
            {
                foreach (P2PSendItem item in this)
                {
                    if (item.P2PMessage == msg)
                        return true;
                }
            }

            return false;
        }

        public void Remove(P2PMessage msg)
        {
            lock (this)
            {
                P2PSendItem? removeItem = null;

                foreach (P2PSendItem item in this)
                {
                    if (item.P2PMessage == msg)
                    {
                        removeItem = item;
                        break;
                    }
                }

                if (removeItem != null)
                {
                    Remove(removeItem.Value);
                    return;
                }
            }

            throw new ArgumentException("msg not found in queue");
        }
    }
    #endregion
};
