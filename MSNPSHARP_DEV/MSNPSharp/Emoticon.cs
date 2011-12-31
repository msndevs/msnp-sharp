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
using System.IO;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    [Serializable()]
    public class Emoticon : DisplayImage
    {
        private string shortcut;
        private bool dataReady = false;

        
        
        public Emoticon()
        {
            ObjectType = MSNObjectType.Emoticon;
            Location = Guid.NewGuid().ToString();
        }

        public Emoticon(string creator, string shortcut)
        {
            ObjectType = MSNObjectType.Emoticon;
            Location = Guid.NewGuid().ToString();
            Creator = creator;
            Shortcut = shortcut;
        }

        public Emoticon(string creator, MemoryStream input, string location, string shortcut)
            : base(creator, input, location)
        {
            ObjectType = MSNObjectType.Emoticon;
            Shortcut = shortcut;
        }

        /// <summary>
        /// The string that will be replaced by the emoticons.
        /// </summary>
        public string Shortcut
        {
            get
            {
                return shortcut;
            }
            set
            {
                shortcut = value;
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether all data have been filled to the DataStrem.
        /// </summary>
        /// <value>
        /// <c>true</c> if data ready; otherwise, <c>false</c>.
        /// </value>
        internal bool DataReady 
        {
            get 
            {
                return this.dataReady;
            }
            
            set 
            {
                dataReady = value;
            }
        }
    }
};