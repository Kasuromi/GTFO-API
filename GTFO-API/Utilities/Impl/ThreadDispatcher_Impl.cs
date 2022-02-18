using System;
using System.Collections.Generic;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace GTFO.API.Utilities.Impl
{
    internal class ThreadDispatcher_Impl : MonoBehaviour
    {
        public ThreadDispatcher_Impl(IntPtr intPtr) : base(intPtr) { }

        public static ThreadDispatcher_Impl Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    ThreadDispatcher_Impl existing = FindObjectOfType<ThreadDispatcher_Impl>();
                    if (existing != null) s_Instance = existing;
                }
                return s_Instance;
            }
        }

        static ThreadDispatcher_Impl()
        {
            AssetAPI.OnStartupAssetsLoaded += OnAssetsLoaded;
        }

        private static void OnAssetsLoaded()
        {
            if (s_Instance != null) return;

            GameObject dispatcher = new();
            ThreadDispatcher_Impl dispatcherComp = dispatcher.AddComponent<ThreadDispatcher_Impl>();
            dispatcher.name = "GTFO-API Thread Dispatcher";
            dispatcher.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(dispatcher);

            s_Instance = dispatcherComp;
        }

        [HideFromIl2Cpp]
        internal void EnqueueAction(Action action)
        {
            lock (s_ActionQueue)
            {
                s_ActionQueue.Enqueue(action);
            }
        }

        internal void Update()
        {
            lock (s_ActionQueue)
            {
                while (s_ActionQueue.Count > 0)
                    s_ActionQueue.Dequeue()?.Invoke();
            }
        }

        private readonly Queue<Action> s_ActionQueue = new();
        private static ThreadDispatcher_Impl s_Instance = null;
    }
}
