using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
        internal const int RETRY_COUNT = 5;
        internal const float RETRY_INTERVAL = 0.1f;

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

        /// <summary>
        /// Read File Content safely from file
        /// </summary>
        /// <param name="filepath">File path to Read all Content</param>
        /// <param name="onReaded">Callback when it readed all content</param>
        public static void TryReadFileContent(string filepath, Action<string> onReaded)
        {
            CoroutineDispatcher.StartCoroutine(GetFileStream(filepath, RETRY_COUNT, RETRY_INTERVAL, (stream) =>
            {
                try
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    onReaded?.Invoke(reader.ReadToEnd());
                }
                catch { }
            }));
        }

        private static IEnumerator GetFileStream(string filepath, int retryCount, float retryInterval, Action<FileStream> onFileStreamOpened)
        {
            retryCount = Math.Max(retryCount, 1);
            retryInterval = Math.Max(retryInterval, 0.0f);

            var wait = new WaitForSecondsRealtime(retryInterval);
            for(int i = 0; i < retryCount; i++)
            {
                try
                {
                    var stream = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    onFileStreamOpened?.Invoke(stream);
                    stream.Close();
                    yield break;
                }
                catch { }

                yield return wait;
            }
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
