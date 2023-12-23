using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using AssetShards;
using BepInEx;
using GTFO.API.API;
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
        /// Invoked when the base game's startup assets have been fully loaded
        /// </summary>
        public static event Action OnStartupAssetsLoaded;

        /// <summary>
        /// Invoked when all external asset bundles have been loaded (Not invoked if no bundles)
        /// </summary>
        public static event Action OnAssetBundlesLoaded;

        /// <summary>
        /// Invoken when loading custom assets are about to start
        /// </summary>
        public static event Action OnCustomAssetsLoading;

        /// <summary>
        /// Invoked when startup asset has fully loaded (Including custom bundles and base game assets)
        /// </summary>
        public static event Action OnStartupAssetsFullyLoaded;

        /// <summary>
        /// Invoked when the internal handler is ready
        /// </summary>
        public static event Action OnImplReady;

        /// <summary>
        /// Return true If every assetbundle and startup assets are loaded
        /// </summary>
        public static bool IsReadyForStartup
        {
            get
            {
                if (!Status.Ready)
                    return false;

                if (s_LoadBlockerHandles.Any(x => !x.IsCompleted))
                    return false;

                if (!s_StartupAssetsFullyLoaded)
                {
                    s_StartupAssetsFullyLoaded = true;
                    OnStartupAssetsFullyLoaded?.Invoke();
                }
                return true;
            }
        }

        /// <summary>
        /// Checks if an asset is already registered in the <see cref="AssetAPI"/>
        /// </summary>
        /// <param name="assetName">Name of the asset to check</param>
        /// <returns>Whether the asset is registered or not</returns>
        public static bool ContainsAsset(string assetName)
        {
            string upperName = assetName.ToUpper();
            if (!APIStatus.Asset.Ready)
                return s_RegistryCache.ContainsKey(upperName);

            return AssetShardManager.s_loadedAssetsLookup.ContainsKey(upperName);
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
        /// Obtains an asset from the currently loaded asset shards and casts it to <typeparamref name="TAsset"/>
        /// </summary>
        /// <typeparam name="TAsset">The type of asset to load</typeparam>
        /// <param name="path">The path to the asset to use</param>
        /// <returns><typeparamref name="TAsset"/> as the asset requested or null if it's not loaded</returns>
        /// <exception cref="InvalidCastException">The loaded asset cannot be cast to <typeparamref name="TAsset"/></exception>
        public static TAsset GetLoadedAsset<TAsset>(string path) where TAsset : UnityEngine.Object
            => GetLoadedAsset(path)?.Cast<TAsset>();

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

        /// <summary>
        /// Create new Load Job Handle
        /// </summary>
        /// <param name="newLoadHandle">Created Load Job Handle</param>
        /// <returns>true if handle has created, false if custom asset loading process is already done (after <see cref="AssetAPI.OnStartupAssetsFullyLoaded"/> has invoked)</returns>
        public static bool WantToWorkForStartupAssets(out AssetLoadHandle newLoadHandle)
        {
            if (s_StartupAssetsFullyLoaded)
            {
                APILogger.Error($"Asset", "Startup Assets are already loaded! Try load them before ");
                newLoadHandle = null;
                return false;
            }
                

            newLoadHandle = new();
            s_LoadBlockerHandles.Add(newLoadHandle);
            return true;
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

        internal static void InvokeBundleLoaded() => OnAssetBundlesLoaded?.Invoke();

        internal static void Setup()
        {
            EventAPI.OnAssetsLoaded += OnAssetsLoaded;
            OnImplReady += LoadCustomStartupAssets;
        }

        private static void LoadCustomStartupAssets()
        {
            OnCustomAssetsLoading?.Invoke();
            string assetBundleDir = Path.Combine(Paths.BepInExRootPath, "Assets", "AssetBundles");
            string assetBundlesDirOld = Path.Combine(Paths.ConfigPath, "Assets", "AssetBundles");
            LoadAssetBundles(assetBundleDir);
            LoadAssetBundles(assetBundlesDirOld, outdated: true);
        }

        private static bool LoadAssetBundles(string assetBundlesDir, bool outdated = false)
        {
            if (outdated)
            {
                if (Directory.Exists(assetBundlesDir))
                    APILogger.Warn(nameof(AssetAPI), "Storing asset bundles in the config path is deprecated and will be removed in a future version of GTFO-API. The path has been moved to 'BepInEx\\Assets\\AssetBundles'.");
                else return false;
            }

            if (!Directory.Exists(assetBundlesDir))
            {
                Directory.CreateDirectory(assetBundlesDir);
                return false;
            }

            string[] bundlePaths = Directory.GetFiles(assetBundlesDir, "*", SearchOption.AllDirectories)
                .Where(x => !x.EndsWith(".manifest", StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            if (bundlePaths.Length == 0) return false;

            for (int i = 0; i < bundlePaths.Length; i++)
            {
                WantToWorkForStartupAssets(out var assetLoadHandle);
                AssetAPI_Impl.Instance.LoadAssetBundle(bundlePaths[i], assetLoadHandle);
            }
            AssetAPI_Impl.Instance.DEBUG_BundleLoadingStarted(bundlePaths.Length);

            return true;
        }

        internal static readonly ConcurrentDictionary<string, UnityEngine.Object> s_RegistryCache = new();
        internal static readonly ConcurrentBag<AssetLoadHandle> s_LoadBlockerHandles = new();
        internal static bool s_StartupAssetsFullyLoaded = false;
    }
}
