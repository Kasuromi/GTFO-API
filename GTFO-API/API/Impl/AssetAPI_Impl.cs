using System;
using AssetShards;
using GTFO.API.Resources;
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

        private void Awake()
        {
            s_Instance = this;
            APIStatus.Asset.Ready = true;
            foreach(var cachedRegister in AssetAPI.s_RegistryCache)
            {
                RegisterAsset(cachedRegister.Key, cachedRegister.Value);
            }
            AssetAPI.s_RegistryCache.Clear();
        }

        public void RegisterAsset(string name, UnityEngine.Object gameObject)
        {
            string upperName = name.ToUpper();
            if (AssetShardManager.s_loadedAssetsLookup.ContainsKey(upperName))
                throw new ArgumentException($"The asset with {name} has already been registered.", nameof(name));
            APILogger.Verbose(nameof(AssetAPI_Impl), $"Registering asset: {name}");
            AssetShardManager.s_loadedAssetsLookup.Add(upperName, gameObject);
        }

        private static AssetAPI_Impl s_Instance;
    }
}
