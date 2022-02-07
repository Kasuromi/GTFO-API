using Gear;
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
            GearManager.FavoritesData = new GearFavoritesData();
            GearManager.BotFavoritesData = new BotFavoritesData();
        }

        //GearManager.SaveBotFavoritesData is inlined and cannot be patched out
        //This method calls it, so we will prevent saving here
        [HarmonyPatch(nameof(GearManager.RegisterBotGearInSlotAsEquipped))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RegisterBotGearInSlotAsEquipped_Prefix(GearIDRange idRange, InventorySlot slot, int slotIndex)
        {
            APILogger.Verbose(nameof(GearManager_Patches), $"Registering equipped gear for bot[{slotIndex}]: {idRange.PlayfabItemInstanceId}");
            switch (slot)
            {
                case InventorySlot.GearMelee:
                    GearManager.BotFavoritesData.LastEquipped_Melee[slotIndex] = (Il2CppSystem.String)new string(idRange.PlayfabItemInstanceId);
                    break;

                case InventorySlot.GearStandard:
                    GearManager.BotFavoritesData.LastEquipped_Standard[slotIndex] = (Il2CppSystem.String)new string(idRange.PlayfabItemInstanceId);
                    break;

                case InventorySlot.GearSpecial:
                    GearManager.BotFavoritesData.LastEquipped_Special[slotIndex] = (Il2CppSystem.String)new string(idRange.PlayfabItemInstanceId);
                    break;

                case InventorySlot.GearClass:
                    GearManager.BotFavoritesData.LastEquipped_Class[slotIndex] = (Il2CppSystem.String)new string(idRange.PlayfabItemInstanceId); 
                    break;

                case InventorySlot.HackingTool:
                    GearManager.BotFavoritesData.LastEquipped_HackingTool[slotIndex] = (Il2CppSystem.String)new string(idRange.PlayfabItemInstanceId);
                    break;

            }
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
