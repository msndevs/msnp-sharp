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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace MSNPSharp
{
    [Serializable()]
    public class CircleList : IEnumerable
    {
        private List<Circle> list = new List<Circle>();

        [NonSerialized]
        private NSMessageHandler nsMessageHandler = null;

        [NonSerialized]
        private object syncRoot;

        public object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    object newobj = new object();
                    Interlocked.CompareExchange(ref syncRoot, newobj, null);
                }
                return syncRoot;
            }
        }

        internal void AddCircle(Circle circle)
        {
            if (this[circle.AddressBookId] == null)
            {
                lock (SyncRoot)
                    list.Add(circle);
            }
            else
            {
                lock (SyncRoot)
                    list[list.IndexOf(circle)] = circle;
            }
        }

        internal void RemoveCircle(Circle circle)
        {
            lock (SyncRoot)
            {
                list.Remove(circle);
            }
        }

        internal CircleList(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
        }

        /// <summary>
        /// Create a new <see cref="Circle"/> and add it into list.
        /// </summary>
        /// <param name="name"></param>
        public virtual void Add(string name)
        {
            if (nsMessageHandler == null)
                throw new MSNPSharpException("No nameserver handler defined");

            nsMessageHandler.ContactService.CreateCircle(name);
        }

        public Circle GetByName(string name)
        {
            foreach (Circle circle in list)
            {
                if (circle.Name == name)
                    return circle;
            }

            return null;
        }

        /// <summary>
        /// Find <see cref="Circle"/> by circleId, if circle not found, return null.
        /// </summary>
        /// <param name="abId">Circle id</param>
        /// <returns></returns>
        public Circle this[Guid abId]
        {
            get
            {
                foreach (Circle circle in list)
                {
                    if (circle.AddressBookId.CompareTo(abId) == 0)
                        return circle;
                }
                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public void Clear()
        {
            lock (SyncRoot)
                list.Clear();
        }

        public int Count
        {
            get { return list.Count; }
        }
    }
}
