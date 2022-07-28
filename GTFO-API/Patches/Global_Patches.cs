using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Globals;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(Global))]
    internal class Global_Patches
    {
        [HarmonyPatch(nameof(Global.OnLevelCleanup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void LevelCleanup_Postfix()
        {
            LevelAPI.LevelCleanup();
        }
    }
}
