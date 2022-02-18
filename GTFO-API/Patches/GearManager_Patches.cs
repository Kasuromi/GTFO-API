using System.Collections.Generic;
using Gear;
using HarmonyLib;
using Player;
using UnhollowerBaseLib.Attributes;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GearManager))]
    internal class GearManager_Patches
    {
        [HideFromIl2Cpp]
        public static Dictionary<InventorySlot, string[]> BotFavorites { get; set; }

        [HarmonyPatch(nameof(GearManager.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Setup_Postfix()
        {
            BotFavorites = new Dictionary<InventorySlot, string[]>
            {
                { InventorySlot.GearMelee, new string[4] },
                { InventorySlot.GearStandard, new string[4] },
                { InventorySlot.GearSpecial, new string[4] },
                { InventorySlot.GearClass, new string[4] },
                { InventorySlot.HackingTool, new string[4] }
            };

            GearManager.FavoritesData = new GearFavoritesData();
        }

        [HarmonyPatch(nameof(GearManager.RegisterBotGearInSlotAsEquipped))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RegisterBotGearInSlotAsEquipped_Prefix(GearIDRange idRange, InventorySlot slot, int slotIndex)
        {
            BotFavorites[slot][slotIndex] = idRange.ToJSON();
            return false;
        }

        [HarmonyPatch(nameof(GearManager.SaveFavoritesData))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SaveFavoritesData_Prefix()
        {
            return false;
        }
    }
}
