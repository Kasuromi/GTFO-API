using System;
using GTFO.API.Components;
using HarmonyLib;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;

namespace GTFO.API.Patches
{
    // These entire patches wrap Item virtual calls so we can invoke managed space stuff
    [HarmonyPatch(typeof(Item))]
    internal class Item_Patches
    {
        [HarmonyPatch(nameof(Item.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static void OnDespawn_Prefix(Item __instance)
        {
            IntPtr classPtr = IL2CPP.il2cpp_object_get_class(__instance.Pointer);
            if (RuntimeSpecificsStore.IsInjected(classPtr))
            {
                ConsumableInstance consumable = (ConsumableInstance)ClassInjectorBase.GetMonoObjectFromIl2CppPointer(__instance.Pointer);
                if (consumable == null)
                {
                    // Note: For the time being ConsumableInstance is the only injected type that uses the Item class
                    // Correct for this when implementing more types or rename ConsumableInstance to ItemWrapper
                    APILogger.Error($"Item_Patches", $"Something broke and ConsumableInstance is null! :(");
                    return;
                }

                consumable.OnPreDespawn();
            }
        }

        [HarmonyPatch(nameof(Item.OnDespawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void OnDespawn_Postfix(Item __instance)
        {
            IntPtr classPtr = IL2CPP.il2cpp_object_get_class(__instance.Pointer);
            if (RuntimeSpecificsStore.IsInjected(classPtr))
            {
                ConsumableInstance consumable = (ConsumableInstance)ClassInjectorBase.GetMonoObjectFromIl2CppPointer(__instance.Pointer);
                if (consumable == null)
                {
                    // Note: For the time being ConsumableInstance is the only injected type that uses the Item class
                    // Correct for this when implementing more types or rename ConsumableInstance to ItemWrapper
                    APILogger.Error($"Item_Patches", $"Something broke and ConsumableInstance is null! :(");
                    return;
                }

                consumable.OnPostDespawn();
            }
        }
    }
}
