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
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using IOFile = System.IO.File;

namespace MSNPSharp.IO
{
    internal class MCLInfo
    {
        private MCLFile file;
        private DateTime lastChange;

        public MCLInfo(MCLFile pfile)
        {
            file = pfile;
            if (IOFile.Exists(file.FileName))
            {
                lastChange = IOFile.GetLastWriteTime(file.FileName);
            }
        }

        /// <summary>
        /// Get whether the file was changed and refresh the <see cref="LastChange"/> property.
        /// </summary>
        /// <returns></returns>
        public bool Refresh()
        {
            if (file != null && IOFile.Exists(file.FileName))
            {
                bool changed = lastChange.CompareTo(IOFile.GetLastWriteTime(file.FileName)) != 0;
                lastChange = IOFile.GetLastWriteTime(file.FileName);
                return changed;
            }

            return false;
        }

        /// <summary>
        /// Inner file
        /// </summary>
        public MCLFile File
        {
            get
            {
                return file;
            }
        }

        /// <summary>
        /// Last written date
        /// </summary>
        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
        }
    }

    /// <summary>
    /// A caching file system to open files.
    /// </summary>
    public static class MCLFileManager
    {
        private static Dictionary<string, MCLInfo> storage = new Dictionary<string, MCLInfo>(0);
        private static object syncObject;

        private static object SyncObject
        {
            get
            {
                if (syncObject == null)
                {
                    object newobj = new object();
                    Interlocked.CompareExchange(ref syncObject, newobj, null);
                }
                return syncObject;
            }
        }

        /// <summary>
        /// Get the file from disk or from the storage cache, so the newest file is read only once from the disk.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="noCompress">Use file compression or not</param>
        /// <returns></returns>
        public static MCLFile GetFile(string filePath, bool noCompress)
        {
            filePath = filePath.ToLowerInvariant();

            if (!storage.ContainsKey(filePath))
            {
                lock (SyncObject)
                {
                    if (!storage.ContainsKey(filePath))
                    {
                        storage[filePath] = new MCLInfo(new MCLFile(filePath, noCompress));
                    }
                }
            }
            else
            {
                if (storage[filePath].Refresh())
                {
                    lock (SyncObject)
                    {
                        if (storage[filePath].Refresh())
                        {
                            storage[filePath] = new MCLInfo(new MCLFile(filePath, noCompress));
                        }
                    }
                }
            }

            return storage[filePath].File;
        }
    }
};
