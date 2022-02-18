using HarmonyLib;
using Player;
using SNetwork;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(PlayerBackpackManager))]
    internal class PlayerBackpackManager_Patches
    {
        [HarmonyPatch(nameof(PlayerBackpackManager.GiveFavoriteGearToBot))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool GiveFavoriteGearToBotPrefix(SNet_Player bot, ref bool __result)
        {
            var botIndex = bot.PlayerSlotIndex();

            if (string.IsNullOrEmpty(GearManager_Patches.BotFavorites[InventorySlot.GearMelee][botIndex]))
            {
                PlayerBackpackManager.CopyGearToBot(bot, PlayerBackpackManager.LocalBackpack);
            } 
            else
            {
                PlayerBackpackManager.EquipBotGear(bot, GearManager_Patches.BotFavorites[InventorySlot.GearMelee][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, GearManager_Patches.BotFavorites[InventorySlot.GearStandard][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, GearManager_Patches.BotFavorites[InventorySlot.GearSpecial][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, GearManager_Patches.BotFavorites[InventorySlot.GearClass][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, GearManager_Patches.BotFavorites[InventorySlot.HackingTool][botIndex]);
            }

            __result = true;
            return false;
        }
    }
}
