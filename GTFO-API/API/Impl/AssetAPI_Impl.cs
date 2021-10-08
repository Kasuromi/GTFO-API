using System;
using UnityEngine;

namespace GTFO.API.Impl
{
    internal class AssetAPI_Impl : MonoBehaviour
    {
        public AssetAPI_Impl(IntPtr intPtr) : base(intPtr) { }

        public static AssetAPI_Impl Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    AssetAPI_Impl existing = FindObjectOfType<AssetAPI_Impl>();
                    if (existing != null) s_Instance = existing;
                }
                return s_Instance;
            }
        }

        private static AssetAPI_Impl s_Instance;
    }
}
