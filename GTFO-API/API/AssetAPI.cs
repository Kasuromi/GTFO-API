using System;
using AssetShards;
using GTFO.API.Attributes;
using GTFO.API.Impl;
using GTFO.API.Resources;
using UnityEngine;

namespace GTFO.API
{
    // todo: move functionality to AssetAPI_Impl
    // and make that Impl depend on an AssetShardManager.Setup Postfix
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
        /// Obtains an asset from the currently loaded asset shards
        /// </summary>
        /// <param name="path">The path to the asset to use</param>
        /// <returns>The <see cref="UnityEngine.Object"/> of the asset requested or null if it's not loaded</returns>
        public static UnityEngine.Object GetLoadedAsset(string path)
        {
            APILogger.Verbose($"Asset", $"Requested Asset: {path.ToUpper()}");
            try
            {
                return AssetShardManager.GetLoadedAsset(path.ToUpper());
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
            if (AssetShardManager.s_loadedAssetsLookup.ContainsKey(upperName))
                throw new ArgumentException($"The asset with {name} has already been registered.", nameof(name));

            AssetShardManager.s_loadedAssetsLookup.Add(upperName, gameObject);
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
        public static void LoadAndRegisterAssetBundle(string pathToBundle)
        {
            RegisterAssetBundle(AssetBundle.LoadFromFile(pathToBundle));
        }

        /// <summary>
        /// Loads an asset bundle from bytes and registers its assets into the asset registry
        /// </summary>
        /// <param name="bundleBytes">The raw bytes of the asset bundle</param>
        public static void LoadAndRegisterAssetBundle(byte[] bundleBytes)
        {
            RegisterAssetBundle(AssetBundle.LoadFromMemory(bundleBytes));
        }

        /// <summary>
        /// Clones an asset into a new one with a different name
        /// </summary>
        /// <param name="assetName">The original asset name</param>
        /// <param name="copyName">The name it should be cloned into</param>
        /// <returns>The <see cref="UnityEngine.Object"/> of the asset requested or null if it's not loaded</returns>
        /// <exception cref="ArgumentException">The name is already registered</exception>
        public static UnityEngine.Object CloneAsset(string assetName, string copyName)
        {
            var originalAsset = GetLoadedAsset(assetName);
            var newAsset = UnityEngine.Object.Instantiate(originalAsset);
            RegisterAsset(copyName, newAsset);
            return GetLoadedAsset(copyName);
        }

        private static void OnAssetsLoaded()
        {
            if (!APIStatus.Asset.Created)
            {
                APIStatus.CreateApi<AssetAPI_Impl>(nameof(APIStatus.Asset));
            }
            OnStartupAssetsLoaded?.Invoke();
        }

        internal static void Setup()
        {
            AssetShardManager.add_OnStartupAssetsLoaded((Action)OnAssetsLoaded);
        }
    }
}
