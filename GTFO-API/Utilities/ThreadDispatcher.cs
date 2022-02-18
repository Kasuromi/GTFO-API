using System;
using GTFO.API.Utilities.Impl;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// Utility class for dispatching threads on the Unity main thread
    /// </summary>
    public static class ThreadDispatcher
    {
        /// <summary>
        /// Queues an action up to be executed on the next frame
        /// </summary>
        /// <param name="action">Action to execute on unity's main thread</param>
        public static void Dispatch(Action action) => ThreadDispatcher_Impl.Instance.EnqueueAction(action);
    }
}
