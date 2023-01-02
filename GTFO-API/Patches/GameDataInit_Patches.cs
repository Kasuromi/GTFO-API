using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Unity.IL2CPP;
using GameData;
using GTFO.API.Impl;
using GTFO.API.Resources;
using HarmonyLib;
using UnityEngine.Analytics;
using _StringUtils = global::GTFO.API.Utilities.StringUtils;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GameDataInit))]
    internal class GameDataInit_Patches
    {
        private struct PluginWhitelistInfo
        {
            public string GUID;
            public string Name;
            public string Version;
            public string Checksum;
        }
        private static PluginWhitelistInfo[] FetchPluginWhitelist()
        {
            // NOTE: I was forced to add this by Dak because of complaints 
            // that GTFO-API disables progression by forcing the rundown id to 1
            // This is here against my will. Let that be known.
            using HttpClient httpClient = new();
            using Stream stream = httpClient.GetStreamAsync($"https://raw.githubusercontent.com/GTFO-Modding/GTFO-API-PluginWhitelist/main/whitelist.txt").GetAwaiter().GetResult();
            using StreamReader reader = new(stream);
            string[] whitelistData = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

            PluginWhitelistInfo[] whitelist = new PluginWhitelistInfo[whitelistData.Length];

            for (int i = 0; i < whitelist.Length; i++)
            {
                string[] info = whitelistData[i].Split(":");
                whitelist[i] = new()
                {
                    GUID = info[0],
                    Name = info[1],
                    Version = info[2],
                    Checksum = info[3].TrimEnd('\r')
                };
            }

            return whitelist;
        }
        [HarmonyPatch(nameof(GameDataInit.Initialize))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Initialize_Postfix()
        {
            Analytics.enabled = false;
            GameDataAPI.InvokeGameDataInit();

            if (APIStatus.Network.Created) return;
            APIStatus.CreateApi<NetworkAPI_Impl>(nameof(APIStatus.Network));
        }
        private static void RemoveRequirementFromList(Il2CppSystem.Collections.Generic.List<ExpeditionInTierData> list)
        {
            foreach (var expedition in list)
            {
                if (!expedition.Enabled)
                    continue;

                switch (expedition.Accessibility)
                {
                    case eExpeditionAccessibility.Normal:
                    case eExpeditionAccessibility.UnlockedByExpedition:
                    case eExpeditionAccessibility.UseCustomProgressionLock:
                        expedition.Accessibility = eExpeditionAccessibility.AlwaysAllow;
                        break;
                }
            }
        }
    }
}
