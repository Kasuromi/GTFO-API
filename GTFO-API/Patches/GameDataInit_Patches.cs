using GameData;
using GTFO.API.Impl;
using GTFO.API.Resources;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GameDataInit))]
    internal class GameDataInit_Patches
    {
        [HarmonyPatch(nameof(GameDataInit.Initialize))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static void Initialize_Prefix()
        {
            if (APIStatus.Network.Created) return;
            APIStatus.CreateApi<NetworkAPI_Impl>(nameof(APIStatus.Network));
        }
    }
}
