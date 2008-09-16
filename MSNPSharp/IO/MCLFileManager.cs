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
            if (file != null)
            {
                if (IOFile.Exists(file.FileName))
                {
                    bool changed = !(lastChange.CompareTo(IOFile.GetLastWriteTime(file.FileName)) == 0);
                    lastChange = IOFile.GetLastWriteTime(file.FileName);
                    return changed;
                }
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
    /// A caching file system..
    /// </summary>
    public static class MCLFileManager
    {
        private static Dictionary<string, MCLInfo> storage = new Dictionary<string, MCLInfo>(0);

        private static bool hiddenSave;
        private static Timer timer;
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

        public static void Save(MCLFile file, bool hiddensave)
        {
            hiddenSave = hiddensave;

            if (timer == null)
            {
                lock (SyncObject)
                {
                    if (timer == null)
                    {
                        timer = new Timer(new TimerCallback(SaveImpl));
                        timer.Change(1000, Timeout.Infinite); //Prevent user call this in a heigh frequency
                    }
                }
            }

            storage[file.FileName.ToLowerInvariant()] = new MCLInfo(file);
        }

        private static void SaveImpl(object state)
        {
            if (storage.Count != 0)
            {
                lock (SyncObject)
                {
                    try
                    {
                        foreach (MCLInfo mclinfo in storage.Values)
                        {
                            if (hiddenSave)
                                mclinfo.File.SaveAndHide();
                            else
                                mclinfo.File.Save();
                        }

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, storage.Count + " MCL file(s) saved.", "MCLFileManager");
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, exception.Message, "MCLFileManager");
                    }
                    finally
                    {
                        ((Timer)state).Dispose();
                        timer = null;
                    }
                }
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
            lock (SyncObject)
            {
                if (!storage.ContainsKey(filePath))
                {
                    storage[filePath] = new MCLInfo(new MCLFile(filePath, noCompress));
                }
                else
                {
                    if (storage[filePath].Refresh())
                    {
                        storage[filePath] = new MCLInfo(new MCLFile(filePath, noCompress));
                    }
                }
            }
            return storage[filePath].File;
        }
    }
};
