using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using AssetShards;
using BepInEx.Unity.IL2CPP.Utils;
using GTFO.API.API;
using GTFO.API.Resources;
using Il2CppInterop.Runtime.Attributes;
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
        
        private int _LoadingCount = 0;

        private readonly Stopwatch _SW = new();
        private int _DebugLoadingCount;

        private static readonly string COMPRESSED_STR = "-compressed";
        private static readonly int COMPRESSED_STR_LENGTH = COMPRESSED_STR.Length;

        [Conditional("DEBUG")]
        [HideFromIl2Cpp]
        public void DEBUG_BundleLoadingStarted(int loadingCount)
        {
            _SW.Restart();
            _DebugLoadingCount = loadingCount;
        }

        [Conditional("DEBUG")]
        [HideFromIl2Cpp]
        public void DEBUG_BundleLoadingFinished()
        {
            _SW.Stop();
            APILogger.Verbose($"Asset", $"Elapsed Time to loading {_DebugLoadingCount} bundles:");
            APILogger.Verbose($"Asset", $" - {_SW.Elapsed}! (or {_SW.ElapsedMilliseconds}ms)");
        }

        [HideFromIl2Cpp]
        public void LoadAssetBundle(string filePath, AssetLoadHandle loadHandle)
        {
            _LoadingCount++;
            this.StartCoroutine(DoLoadAssetBundle(filePath, loadHandle));
        }

        [HideFromIl2Cpp]
        private IEnumerator DoLoadAssetBundle(string filePath, AssetLoadHandle loadHandle)
        {
            AssetBundleCreateRequest loadReq = AssetBundle.LoadFromFileAsync(filePath);

            yield return loadReq;

            AssetBundle loadedBundle = loadReq.assetBundle;
            if (loadedBundle == null)
            {
                _LoadingCount--;
                APILogger.Warn($"Asset", $"Failed to load asset bundle: [{filePath}]");
                yield break;
            }


            APILogger.Warn($"Asset", $"Start Loading Bundle!");
            string[] assetNames = loadedBundle.AllAssetNames();
            int remainingAssets = assetNames.Length;
            int loadedCount = 0;

            foreach (string assetName in assetNames)
            {
                AssetBundleRequest loadAssetReq = loadedBundle.LoadAssetAsync(assetName);
                loadAssetReq.add_completed((Action<AsyncOperation>)((x) =>
                {
                    remainingAssets--;
                    loadedCount++;

                    UnityEngine.Object loadedAsset = loadAssetReq.asset;
                    if (loadedAsset == null)
                    {
                        APILogger.Warn("Asset", $"Skipping asset {assetName}");
                    }

                    RegisterAsset(assetName, loadedAsset);
                }));
                yield return null;
            }

            yield return new WaitUntil((Il2CppSystem.Func<bool>)(() =>
            {
                return remainingAssets <= 0;
            }));

            _LoadingCount--;
            loadHandle.SetCompleted();

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.EndsWith(COMPRESSED_STR, StringComparison.InvariantCultureIgnoreCase))
                fileName = fileName[^COMPRESSED_STR_LENGTH..]; //Trim "-Compressed" from bundle name

            loadHandle.AddLoadingText($"[Assets] <color=orange>{fileName}</color> has loaded!");

            if (_LoadingCount <= 0)
            {
                _LoadingCount = 0;
                loadHandle.AddLoadingText($"[Assets] All bundle has loaded!");
                AssetAPI.InvokeBundleLoaded();
                DEBUG_BundleLoadingFinished();
            }
            
            yield return null;
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
            AssetAPI.InvokeImplReady();
        }

        [HideFromIl2Cpp]
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
