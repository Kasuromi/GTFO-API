using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// LiveEditEventHandler
    /// </summary>
    /// <param name="e">EventArgs</param>
    public delegate void LiveEditEventHandler(FileSystemEventArgs e);

    /// <summary>
    /// Utility class to make support of LiveEdit config file for plugins
    /// </summary>
    public static class LiveEdit
    {
        private static readonly List<LiveEditListener> m_Listeners = new ();

        /// <summary>
        /// Create the LiveEdit Listener and Allocate
        /// </summary>
        /// <param name="path">Base Path to Track the change</param>
        /// <param name="filter">File filter to filter the files ie) *.json</param>
        /// <param name="includeSubDir">Include Sub-directories?</param>
        public static LiveEditListener CreateListener(string path, string filter, bool includeSubDir)
        {
            var listener = new LiveEditListener(path, filter, includeSubDir);
            m_Listeners.Add(listener);
            return listener;
        }

        /// <summary>
        /// Deallocate the LiveEdit Listener
        /// </summary>
        /// <param name="listener">Listener to deallocate</param>
        public static void DeallocateListener(LiveEditListener listener)
        {
            if (listener == null)
            {
                APILogger.Error("LiveEdit", "DeallocateListener - listener was null!");
                return;
            }

            if (listener.m_Watcher == null)
            {
                APILogger.Error("LiveEdit", "DeallocateListener - listener was already deallocated!");
                return;
            }

            m_Listeners.Remove(listener);
            listener.Deallocate();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Interoperability", "CA1416:Platform Compatible on FileSystemWatcher", Justification = "GTFO is Windows only game")]
    public sealed class LiveEditListener
    {
        /// <summary>
        /// Event when File has Changed
        /// </summary>
        public event LiveEditEventHandler FileChanged;
        /// <summary>
        /// Event when File has Deleted
        /// </summary>
        public event LiveEditEventHandler FileDeleted;
        /// <summary>
        /// Event when File has Created
        /// </summary>
        public event LiveEditEventHandler FileCreated;
        /// <summary>
        /// Event when File has Renamed
        /// </summary>
        public event LiveEditEventHandler FileRenamed;

        internal FileSystemWatcher m_Watcher = null;

        private LiveEditListener() { }

        internal LiveEditListener(string path, string filter, bool includeSubDir)
        {
            m_Watcher = new ()
            {
                Path = path,
                Filter = filter,
                IncludeSubdirectories = includeSubDir,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            m_Watcher.Changed += (sender, e) => { FileChanged?.Invoke(e); };
            m_Watcher.Deleted += (sender, e) => { FileDeleted?.Invoke(e); };
            m_Watcher.Created += (sender, e) => { FileCreated?.Invoke(e); };
            m_Watcher.Renamed += (sender, e) => { FileRenamed?.Invoke(e); };
            m_Watcher.Error += (sender, e) =>
            {
                APILogger.Error("LiveEdit", $"Path: {path} error reported! - {e.GetException()}");
            };

            StartListen();
        }

        internal void Deallocate()
        {
            StopListen();
            m_Watcher.EnableRaisingEvents = false;
            FileChanged = null;
            FileDeleted = null;
            FileCreated = null;
            FileRenamed = null;
            m_Watcher.Dispose();
            m_Watcher = null;
        }

        /// <summary>
        /// Stop Listening LiveEdit
        /// </summary>
        public void StopListen()
        {
            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Start Listening LiveEdit (Default On LiveEdit Allocated)
        /// </summary>
        public void StartListen()
        {
            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = true;
            }
        }
    }
}
