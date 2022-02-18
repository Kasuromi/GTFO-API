using Gear;
using GTFO.API.Resources;
using HarmonyLib;
using Player;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GearManager))]
    internal class GearManager_Patches
    {
        [HarmonyPatch(nameof(GearManager.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void Setup_Postfix()
        {
            PersistentData.BotFavorites = new()
            {
                [InventorySlot.GearMelee] = new string[4],
                [InventorySlot.GearStandard] = new string[4],
                [InventorySlot.GearSpecial] = new string[4],
                [InventorySlot.GearClass] = new string[4],
                [InventorySlot.HackingTool] = new string[4]
            };

            GearManager.FavoritesData = new();
        }

        [HarmonyPatch(nameof(GearManager.RegisterBotGearInSlotAsEquipped))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RegisterBotGearInSlotAsEquipped_Prefix(GearIDRange idRange, InventorySlot slot, int slotIndex)
        {
            PersistentData.BotFavorites[slot][slotIndex] = idRange.ToJSON();
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
