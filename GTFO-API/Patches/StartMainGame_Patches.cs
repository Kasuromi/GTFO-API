using GameData;
using Globals;
using HarmonyLib;
using UnityEngine.Analytics;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(StartMainGame))]
    internal class StartMainGame_Patches
    {
        [HarmonyPatch(nameof(StartMainGame.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void StartMainGame_Postfix()
        {
            Analytics.enabled = false;
            if (Global.RundownIdToLoad != 1)
            {
                APILogger.Verbose(nameof(StartMainGame_Patches), $"RundownIdToLoad was {Global.RundownIdToLoad}. Setting to 1");
                RundownDataBlock.RemoveBlockByID(1);

                RundownDataBlock block = RundownDataBlock.GetBlock(Global.RundownIdToLoad);
                block.persistentID = 1;
                block.name = $"MOVEDBYAPI_{block.name}";

                RundownDataBlock.RemoveBlockByID(Global.RundownIdToLoad);
                RundownDataBlock.AddBlock(block);

                Global.RundownIdToLoad = 1;
            }
        }
    }
}
