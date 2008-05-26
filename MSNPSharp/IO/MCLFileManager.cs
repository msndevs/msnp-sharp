namespace MSNPSharp.IO
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using IOFile = System.IO.File;
    using System.Diagnostics;

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

    public static class MCLFileManager
    {
        private static Dictionary<string, MCLInfo> storage = new Dictionary<string, MCLInfo>(0);

        private static bool _hiddenSave;
        private static Timer timer;

        public static void Save(MCLFile file, bool hiddensave)
        {
            if (timer == null)
                timer = new Timer(new TimerCallback(SaveImpl));
            timer.Change(2000, Timeout.Infinite);                     //Prevent user call this in a heigh frequency
            storage[file.FileName.ToLower(System.Globalization.CultureInfo.InvariantCulture)] = new MCLInfo(file);
            _hiddenSave = hiddensave;
        }

        private static void SaveImpl(object state)
        {
            if (storage.Count == 0)
                return;

            try
            {
                foreach (MCLInfo mclinfo in storage.Values)
                {
                    if (_hiddenSave)
                        mclinfo.File.SaveAndHide();
                    else
                        mclinfo.File.Save();
                    if (Settings.TraceSwitch.TraceVerbose)
                        Trace.WriteLine("MCL files saved.");
                }
            }
            catch (Exception)
            {
            }
            ((Timer)state).Dispose();
            timer = null;

        }

        /// <summary>
        /// Get the file from disk or from the storage cache, so the newest file is read only once from the disk.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="noCompress">Use file compression or not</param>
        /// <returns></returns>
        public static MCLFile GetFile(string filePath, bool noCompress)
        {
            filePath = filePath.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            lock (storage)
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
