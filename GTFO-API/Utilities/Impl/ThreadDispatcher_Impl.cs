using System;
using System.Collections.Generic;
using System.Text;
using AssetShards;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace GTFO.API.Utilities.Impl
{
    internal class ThreadDispatcher_Impl : MonoBehaviour
    {
        public ThreadDispatcher_Impl(IntPtr intPtr) : base(intPtr) { }

        static ThreadDispatcher_Impl()
        {
            AssetShardManager.add_OnStartupAssetsLoaded((Action)OnAssetsLoaded);
        }

        [HideFromIl2Cpp]
        private static void OnAssetsLoaded()
        {
            if (s_Instance == null)
            {
                GameObject dispatcher = new();
                var dispatcherComp = dispatcher.AddComponent<ThreadDispatcher_Impl>();
                dispatcher.name = "GTFO-API Thread Dispatcher";
                dispatcher.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(dispatcher);

                s_Instance = dispatcherComp;
            }
        }

        [HideFromIl2Cpp]
        internal static void Enqueue(Action action)
        {
            lock (s_LoopQueue)
            {
                s_LoopQueue.Enqueue(action);
            }
        }

        internal void Update()
        {
            lock (s_LoopQueue)
            {
                while (s_LoopQueue.Count > 0)
                {
                    s_LoopQueue.Dequeue()?.Invoke();
                }
            }
        }

        private static ThreadDispatcher_Impl s_Instance = null;
        private static readonly Queue<Action> s_LoopQueue = new();
    }
}
