﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LevelGeneration;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(Builder))]
    internal class Builder_Patches
    {
        [HarmonyPatch(nameof(Builder.Build))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void BuildStart_Postfix() => LevelAPI.BuildStart();

        [HarmonyPatch(nameof(Builder.BuildDone))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void BuildDone_Postfix() => LevelAPI.BuildDone();
    }
}
