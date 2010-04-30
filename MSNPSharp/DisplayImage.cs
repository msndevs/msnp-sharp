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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    
    [Serializable()]
    public class DisplayImage : MSNObject
    {
        private Image image = null;
        private bool isDefaultImage = false;
        private Image defaultImage = Properties.Resources.WLXLarge_default.Clone() as Image;
        private static string defaultLocation = "MSNPSharpDefault";

        public bool IsDefaultImage
        {
            get { return isDefaultImage; }
            internal set
            {
                isDefaultImage = value;
            }
        }

        public DisplayImage()
        {
            ObjectType = MSNObjectType.UserDisplay;
            Location = "dotmsn.png";
        }

        public DisplayImage(string creator)
        {
            ObjectType = MSNObjectType.UserDisplay;
            Location = "dotmsn.png";
            Creator = creator;
        }

        internal DisplayImage(string creator, bool isDefault)
        {
            ObjectType = MSNObjectType.UserDisplay;

            if (isDefault)
            {
                Location = defaultLocation;
                Image = defaultImage;
            }

            isDefaultImage = isDefault;
            Creator = creator;

            RetrieveImage();
        }

        public DisplayImage(string creator, Stream input, string location)
            : base(creator, input, MSNObjectType.UserDisplay, location)
        {
            RetrieveImage();
        }

        public static DisplayImage CreateDefaultImage(string creator)
        {
            return new DisplayImage(creator, true);
        }

        public Image Image
        {
            get
            {
                lock (SyncObject)
                    return image == null ? null : image.Clone() as Image;
            }
            set
            {
                lock (SyncObject)
                {
                    image = value;
                    UpdateStream();
                }
            }
        }

        private void UpdateStream()
        {
            image.Save(DataStream, image.RawFormat);
            Size = (int)DataStream.Length;
            Sha = GetStreamHash(DataStream);
        }

        public void RetrieveImage()
        {
            Stream input = DataStream;

            if (input != null)
            {
                lock (input)
                {
                    input.Position = 0;
                    if (input.Length > 0)
                    {
                        lock (SyncObject)
                            image = System.Drawing.Image.FromStream(input);
                    }

                    input.Position = 0;
                }
            }
        }
    }
};
