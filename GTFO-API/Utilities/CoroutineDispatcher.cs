using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTFO.API.Utilities.Impl;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// Utility class for dispatching Coroutine without making new monobehaviour
    /// </summary>
    public static class CoroutineDispatcher
    {
        /// <summary>
        /// Start Coroutine (persistent between sessions)
        /// </summary>
        /// <param name="routine">Coroutine to Run</param>
        public static void StartCoroutine(IEnumerator routine)
        {
            CoroutineDispatcher_Impl.Instance.RunCoroutine(routine);
        }

        /// <summary>
        /// Start InLevel Coroutine that will be stopped automatically when you stop playing level
        /// </summary>
        /// <param name="routine">Coroutine to Run</param>
        public static void StartInLevelCoroutine(IEnumerator routine)
        {
            CoroutineDispatcher_Impl.Instance.RunInLevelCoroutine(routine);
        }
    }
}
