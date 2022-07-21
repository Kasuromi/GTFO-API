using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameData;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(RundownManager))]
    internal class RundownManager_Patches
    {
        [HarmonyPatch(nameof(RundownManager.SetActiveExpedition))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SetActiveExpedition(pActiveExpedition expPackage, ExpeditionInTierData expTierData)
        {
            LevelAPI.ExpeditionUpdated(expPackage, expTierData);
        }
    }
}
