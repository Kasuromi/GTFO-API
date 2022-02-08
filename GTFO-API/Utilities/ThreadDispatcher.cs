using System;
using System.Collections.Generic;
using System.Text;
using GTFO.API.Utilities.Impl;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// Unity Main Thread Dispatcher
    /// </summary>
    public static class ThreadDispatcher
    {
        /// <summary>
        /// Enqueue Action that want to excute on main unity thread
        /// </summary>
        /// <param name="action">Action to dispatch to main unity thread</param>
        public static void Enqueue(Action action)
        {
            ThreadDispatcher_Impl.Enqueue(action);
        }
    }
}
