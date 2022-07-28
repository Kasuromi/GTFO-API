using System;
using UnityEngine;

namespace GTFO.API.Resources
{
    /// <summary>
    /// Container for API statuses
    /// </summary>
    public class ApiStatusInfo
    {
        /// <summary>
        /// If the API handler script has been created
        /// </summary>
        public bool Created { get; internal set; } = false;

        /// <summary>
        /// If the API handler is ready to be used
        /// </summary>
        public bool Ready { get; internal set; } = false;
    }

    /// <summary>
    /// Contains all of the usable API status information
    /// </summary>
    public static class APIStatus
    {
        /// <summary>
        /// Status info for the <see cref="AssetAPI"/>
        /// </summary>
        public static ApiStatusInfo Asset { get; internal set; } = new();

        /// <summary>
        /// Status info for the <see cref="GameDataAPI"/>
        /// </summary>
        public static ApiStatusInfo GameData { get; internal set; } = new();

        /// <summary>
        /// Status info for the <see cref="Il2CppAPI"/>
        /// </summary>
        public static ApiStatusInfo Il2Cpp { get; internal set; } = new();

        /// <summary>
        /// Status info for the <see cref="LevelAPI"/>
        /// </summary>
        public static ApiStatusInfo Level { get; internal set; } = new();

        /// <summary>
        /// Status info for the <see cref="NetworkAPI"/>
        /// </summary>
        public static ApiStatusInfo Network { get; internal set; } = new();

        /// <summary>
        /// Status info for the <see cref="PrefabAPI"/>
        /// </summary>
        public static ApiStatusInfo Prefab { get; internal set; } = new();

        internal static GameObject ScriptHolder
        {
            get
            {
                if (s_ScriptHolder == null)
                {
                    s_ScriptHolder = new();
                    s_ScriptHolder.name = "GTFO-API Script Holder";
                    s_ScriptHolder.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DontDestroyOnLoad(s_ScriptHolder);
                }
                return s_ScriptHolder;
            }
        }

        internal static void CreateApi<T>(string apiName) where T : Component
        {
            ApiStatusInfo statusInfo = (ApiStatusInfo)typeof(APIStatus).GetProperty(apiName)?.GetValue(null);
            if (statusInfo == null) throw new ArgumentException($"Couldn't find API status for {apiName}", nameof(apiName));
            if (statusInfo.Created) return;

            T existingComp = ScriptHolder.GetComponent<T>();
            if (existingComp != null) return;

            APILogger.Verbose("Core", $"Creating API {apiName}");
            ScriptHolder.AddComponent<T>();
            statusInfo.Created = true;
        }

        private static GameObject s_ScriptHolder;
    }
}
