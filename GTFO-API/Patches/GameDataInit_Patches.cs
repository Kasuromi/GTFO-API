using GameData;
using GTFO.API.Impl;
using GTFO.API.Resources;
using HarmonyLib;
using UnityEngine.Analytics;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GameDataInit))]
    internal class GameDataInit_Patches
    {
        [HarmonyPatch(nameof(GameDataInit.Initialize))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Initialize_Postfix()
        {
            Analytics.enabled = false;

            GameSetupDataBlock setupBlock = GameDataBlockBase<GameSetupDataBlock>.GetBlock(1);
            if (setupBlock?.RundownIdToLoad != 1)
            {
                APILogger.Verbose(nameof(GameDataInit_Patches), $"RundownIdToLoad was {setupBlock.RundownIdToLoad}. Setting to 1");
                RundownDataBlock.RemoveBlockByID(1);

                RundownDataBlock block = RundownDataBlock.GetBlock(setupBlock.RundownIdToLoad);
                block.persistentID = 1;
                block.name = $"MOVEDBYAPI_{block.name}";

                block.UseTierUnlockRequirements = false;
                RemoveRequirementFromList(block.TierA);
                RemoveRequirementFromList(block.TierB);
                RemoveRequirementFromList(block.TierC);
                RemoveRequirementFromList(block.TierD);
                RemoveRequirementFromList(block.TierE);

                RundownDataBlock.RemoveBlockByID(setupBlock.RundownIdToLoad);
                RundownDataBlock.AddBlock(block, -1);

                setupBlock.RundownIdToLoad = 1;
            }

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
