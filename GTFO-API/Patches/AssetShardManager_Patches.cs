using AssetShards;
using GTFO.API.Impl;
using GTFO.API.Resources;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(AssetShardManager))]
    internal class AssetShardManager_Patches
    {
        [HarmonyPatch(nameof(AssetShardManager.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Setup_Postfix()
        {
            if (APIStatus.Asset.Created) return;
            APIStatus.CreateApi<AssetAPI_Impl>(nameof(APIStatus.Asset));
        }
    }
}
