using GTFO.API.Resources;
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
        public static bool GiveFavoriteGearToBot_Prefix(SNet_Player bot, ref bool __result)
        {
            int botIndex = bot.PlayerSlotIndex();

            if (string.IsNullOrEmpty(RuntimeData.BotFavorites[InventorySlot.GearMelee][botIndex]))
            {
                PlayerBackpackManager.CopyGearToBot(bot, PlayerBackpackManager.LocalBackpack);
            }
            else
            {
                PlayerBackpackManager.EquipBotGear(bot, RuntimeData.BotFavorites[InventorySlot.GearMelee][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, RuntimeData.BotFavorites[InventorySlot.GearStandard][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, RuntimeData.BotFavorites[InventorySlot.GearSpecial][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, RuntimeData.BotFavorites[InventorySlot.GearClass][botIndex]);
                PlayerBackpackManager.EquipBotGear(bot, RuntimeData.BotFavorites[InventorySlot.HackingTool][botIndex]);
            }

            __result = true;
            return false;
        }
    }
}
