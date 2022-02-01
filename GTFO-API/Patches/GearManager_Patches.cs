using Gear;
using HarmonyLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GearManager))]
    internal class GearManager_Patches
    {
        [HarmonyPatch(nameof(GearManager.RegisterGearInSlotAsEquipped))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RegisterGearInSlotAsEquipped_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(GearManager.RegisterBotGearInSlotAsEquipped))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RegisterBotGearInSlotAsEquipped_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(GearManager.SaveFavoritesData))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SaveFavoritesData_Prefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(GearManager.LoadOfflineGearDatas))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void LoadOfflineGearDatas_Postfix()
        {
            //Force players to equip the defaults for the currently loaded mod
            GearManager.FavoritesData = new GearFavoritesData();
            GearManager.BotFavoritesData = new BotFavoritesData();
        }
    }
}
