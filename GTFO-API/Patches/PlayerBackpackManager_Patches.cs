using System.Collections.Generic;
using Gear;
using GTFO.API.Extensions;
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
            int index = bot.PlayerSlotIndex();

            if (string.IsNullOrEmpty(GearManager.BotFavoritesData.LastEquipped_Standard[index]))
            {
                APILogger.Debug(nameof(PlayerBackpackManager_Patches), "Bot does not have favorites yet, falling back to copying player's favorites");
                return true;
            }

            var favoriteIds = new List<string>() {
                    GearManager.BotFavoritesData.LastEquipped_Melee[index],
                    GearManager.BotFavoritesData.LastEquipped_Standard[index],
                    GearManager.BotFavoritesData.LastEquipped_Special[index],
                    GearManager.BotFavoritesData.LastEquipped_Class[index],
                    GearManager.BotFavoritesData.LastEquipped_HackingTool[index]
                }.ToIl2Cpp();

            foreach (var favoriteId in favoriteIds)
            {
                if (GearManager.Current.m_allGearPerInstanceKey.ContainsKey(favoriteId))
                {
                    PlayerBackpackManager.EquipBotGear(bot, GearManager.Current.m_allGearPerInstanceKey[favoriteId]);
                }
            }

            PlayerBackpackManager.ForceSyncBotInventory(bot);
            __result = true;
            return false;
        }
    }
}
