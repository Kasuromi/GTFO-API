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
        internal static readonly List<LiveEditListener> s_Listeners = new();

        /// <summary>
        /// Create the LiveEdit Listener and Allocate
        /// </summary>
        /// <param name="path">Base Path to Track the change</param>
        /// <param name="filter">File filter to filter the files ie) *.json</param>
        /// <param name="includeSubDir">Include Sub-directories?</param>
        public static LiveEditListener CreateListener(string path, string filter, bool includeSubDir)
        {
            LiveEditListener listener = new(path, filter, includeSubDir);
            s_Listeners.Add(listener);
            return listener;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("Interoperability", "CA1416:Platform Compatible on FileSystemWatcher", Justification = "GTFO is Windows only game")]
    public sealed class LiveEditListener : IDisposable
    {
        /// <summary>
        /// Event when File has Changed, Thread-safe
        /// </summary>
        public event LiveEditEventHandler FileChanged;
        /// <summary>
        /// Event when File has Deleted, Thread-safe
        /// </summary>
        public event LiveEditEventHandler FileDeleted;
        /// <summary>
        /// Event when File has Created, Thread-safe
        /// </summary>
        public event LiveEditEventHandler FileCreated;
        /// <summary>
        /// Event when File has Renamed, Thread-safe
        /// </summary>
        public event LiveEditEventHandler FileRenamed;

        private FileSystemWatcher m_Watcher = null;
        private bool m_Allocated = true;

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

            m_Watcher.Changed += (sender, e) => { ThreadDispatcher.Dispatch(() => { FileChanged?.Invoke(e); }); };
            m_Watcher.Deleted += (sender, e) => { ThreadDispatcher.Dispatch(() => { FileDeleted?.Invoke(e); }); };
            m_Watcher.Created += (sender, e) => { ThreadDispatcher.Dispatch(() => { FileCreated?.Invoke(e); }); };
            m_Watcher.Renamed += (sender, e) => { ThreadDispatcher.Dispatch(() => { FileRenamed?.Invoke(e); }); };
            m_Watcher.Error += (sender, e) =>
            {
                APILogger.Error(nameof(LiveEdit), $"Path: {path} error reported! - {e.GetException()}");
            };

            StartListen();
        }

        /// <summary>
        /// Dispose LiveEditListener
        /// </summary>
        public void Dispose()
        {
            if (m_Allocated)
            {
                LiveEdit.s_Listeners.Remove(this);
                m_Allocated = false;
            }
            
            if (m_Watcher != null)
            {
                StopListen();
                FileChanged = null;
                FileDeleted = null;
                FileCreated = null;
                FileRenamed = null;
                m_Watcher.Dispose();
            }

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
