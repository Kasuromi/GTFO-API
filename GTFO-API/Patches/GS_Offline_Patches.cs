using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GS_Offline))]
    internal class GS_Offline_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GS_Offline.Update))]
        static bool Prefix()
        {
            if (AssetAPI.IsReadyForStartup)
            {
                return true; //Run Original
            }

            return false; //Skip Original
        }
    }
}
