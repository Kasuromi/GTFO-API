using System;
using System.Collections.Concurrent;
using System.IO;
using AssetShards;
using BepInEx;
using GTFO.API.Attributes;
using GTFO.API.Impl;
using GTFO.API.Resources;
using UnityEngine;

namespace GTFO.API
{
    [API("Asset")]
    public static class AssetAPI
    {
        /// <summary>
        /// Status info for the <see cref="AssetAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.Asset;

        /// <summary>
        /// Invoked when the game's startup assets have been fully loaded
        /// </summary>
        public static event Action OnStartupAssetsLoaded;

        /// <summary>
        /// Invoked when all external asset bundles have been loaded (Not invoked if no bundles)
        /// </summary>
        public static event Action OnAssetBundlesLoaded;

        /// <summary>
        /// Invoked when the internal handler is ready
        /// </summary>
        public static event Action OnImplReady;

        /// <summary>
        /// Checks if an asset is already registered in the <see cref="AssetAPI"/>
        /// </summary>
        /// <param name="assetName">Name of the asset to check</param>
        /// <returns>Whether the asset is registered or not</returns>
        public static bool ContainsAsset(string assetName)
        {
            if (!APIStatus.Asset.Ready)
                return s_RegistryCache.ContainsKey(assetName);

            return AssetShardManager.s_loadedAssetsLookup.ContainsKey(assetName);
        }

        /// <summary>
        /// Obtains an asset from the currently loaded asset shards
        /// </summary>
        /// <param name="path">The path to the asset to use</param>
        /// <returns>The <see cref="UnityEngine.Object"/> of the asset requested or null if it's not loaded</returns>
        public static UnityEngine.Object GetLoadedAsset(string path)
        {
            string upperPath = path.ToUpper();
            APILogger.Verbose($"Asset", $"Requested Asset: {upperPath}");
            try
            {
                if (!APIStatus.Asset.Ready)
                {
                    if (s_RegistryCache.TryGetValue(upperPath, out UnityEngine.Object asset))
                        return asset;
                }
                return AssetShardManager.GetLoadedAsset(upperPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Registers an asset into the asset shard lookup
        /// </summary>
        /// <param name="name">The name/path of the asset to use as a lookup</param>
        /// <param name="gameObject">The <see cref="UnityEngine.Object"/> that will be used</param>
        /// <exception cref="ArgumentException">The name is already registered</exception>
        public static void RegisterAsset(string name, UnityEngine.Object gameObject)
        {
            string upperName = name.ToUpper();
            if (!APIStatus.Asset.Ready)
            {
                if (s_RegistryCache.ContainsKey(upperName)) throw new ArgumentException($"The asset with {upperName} has already been registered.", nameof(name));
                s_RegistryCache.TryAdd(upperName, gameObject);
                return;
            }
            AssetAPI_Impl.Instance.RegisterAsset(upperName, gameObject);
        }

        /// <summary>
        /// Registers an <see cref="AssetBundle"/>'s assets into the asset registry
        /// <para>Use <seealso cref="LoadAndRegisterAssetBundle(string)"/> if you want to load from a path</para>
        /// </summary>
        /// <param name="bundle">The bundle to register</param>
        public static void RegisterAssetBundle(AssetBundle bundle)
        {
            string[] assetNames = bundle.AllAssetNames();
            APILogger.Verbose($"Asset", $"Bundle names: [{string.Join(", ", assetNames)}]");
            foreach (string assetName in assetNames)
            {
                UnityEngine.Object asset = bundle.LoadAsset(assetName);
                if (asset != null) RegisterAsset(assetName, asset);
                else APILogger.Warn("Asset", $"Skipping asset {assetName}");
            }
        }

        /// <summary>
        /// Loads an asset bundle by path and registers its assets into the asset registry
        /// </summary>
        /// <param name="pathToBundle">The location of the asset bundle</param>
        /// <exception cref="Exception">Failed to load the bundle</exception>
        public static void LoadAndRegisterAssetBundle(string pathToBundle)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(pathToBundle);
            if (bundle == null) throw new Exception($"Failed to load asset bundle");
            RegisterAssetBundle(bundle);
        }

        /// <summary>
        /// Loads an asset bundle from bytes and registers its assets into the asset registry
        /// </summary>
        /// <param name="bundleBytes">The raw bytes of the asset bundle</param>
        /// <exception cref="Exception">Failed to load the bundle</exception>
        public static void LoadAndRegisterAssetBundle(byte[] bundleBytes)
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(bundleBytes);
            if (bundle == null) throw new Exception($"Failed to load asset bundle");
            RegisterAssetBundle(bundle);
        }

        /// <summary>
        /// Clones an asset into a new one with a different name
        /// </summary>
        /// <param name="assetName">The original asset name</param>
        /// <param name="copyName">The name it should be cloned into</param>
        /// <returns>The <see cref="UnityEngine.Object"/> of the asset requested or null if it's not loaded</returns>
        /// <exception cref="ArgumentException">The name you're trying to copy into is already registered</exception>
        public static UnityEngine.Object InstantiateAsset(string assetName, string copyName)
        {
            if (ContainsAsset(copyName)) throw new ArgumentException($"The asset you're trying to copy into is already registered", nameof(copyName));
            RegisterAsset(
                copyName, 
                UnityEngine.Object.Instantiate(
                    GetLoadedAsset(assetName) ?? throw new ArgumentException($"Couldn't find an asset with the name '{assetName}'", nameof(assetName))
                )
            );

            return GetLoadedAsset(copyName);
        }

        /// <summary>
        /// Clones an asset into a new one with a different name
        /// </summary>
        /// <typeparam name="TAsset">Type of asset to extract</typeparam>
        /// <param name="assetName">The original asset name</param>
        /// <param name="copyName">The name it should be cloned into</param>
        /// <returns>The asset requested as <typeparamref name="TAsset"/> or null if it's not loaded</returns>
        /// <exception cref="ArgumentException">The name you're trying to copy into is already registered</exception>
        /// <exception cref="InvalidCastException">The asset cannot be cast to <typeparamref name="TAsset"/></exception>
        public static TAsset InstantiateAsset<TAsset>(string assetName, string copyName) where TAsset : UnityEngine.Object
            => InstantiateAsset(assetName, copyName)?.Cast<TAsset>();

        /// <summary>
        /// Attempts to clone an asset into a new one with a different name
        /// </summary>
        /// <typeparam name="TAsset">Type of asset to extract</typeparam>
        /// <param name="assetName">The original asset name</param>
        /// <param name="copyName">The name it should be cloned into</param>
        /// <param name="clonedObj"></param>
        /// <returns>If the asset was successfully copied and cast to <typeparamref name="TAsset"/></returns>
        /// <exception cref="ArgumentException">The name you're trying to copy into is already registered</exception>
        public static bool TryInstantiateAsset<TAsset>(string assetName, string copyName, out TAsset clonedObj) where TAsset : UnityEngine.Object
        {
            clonedObj = InstantiateAsset(assetName, copyName)?.TryCast<TAsset>();
            return clonedObj != null;
        }

        private static void OnAssetsLoaded()
        {
            if (!APIStatus.Asset.Created)
            {
                APIStatus.CreateApi<AssetAPI_Impl>(nameof(APIStatus.Asset));
            }
            OnStartupAssetsLoaded?.Invoke();
        }

        internal static void InvokeImplReady() => OnImplReady?.Invoke();

        internal static void Setup()
        {
            AssetShardManager.add_OnStartupAssetsLoaded((Action)OnAssetsLoaded);

            OnImplReady += LoadAssetBundles;
        }

        private static void LoadAssetBundles()
        {
            string assetBundlesDir = Path.Combine(Paths.ConfigPath, "Assets", "AssetBundles");
            if (!Directory.Exists(assetBundlesDir))
                Directory.CreateDirectory(assetBundlesDir);

            string[] bundlePaths = Directory.GetFiles(assetBundlesDir, "*", SearchOption.AllDirectories);
            if (bundlePaths.Length == 0) return;

            for (int i = 0; i < bundlePaths.Length; i++)
            {
                try
                {
                    LoadAndRegisterAssetBundle(bundlePaths[i]);
                }
                catch(Exception ex)
                {
                    APILogger.Warn(nameof(AssetAPI), $"Failed to load asset bundle '{bundlePaths[i]}' ({ex.Message})");
                }
            }
            OnAssetBundlesLoaded?.Invoke();
        }

        internal static ConcurrentDictionary<string, UnityEngine.Object> s_RegistryCache = new();
    }
}
