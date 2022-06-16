using System;
using System.IO;
using BepInEx;
using Gear;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GearManager))]
    internal static class GearManager_Patches
    {
        private static string FavoritesDirectory => Path.Combine(Paths.ConfigPath, "Favorites");
        private static string FavoritePath => Path.Combine(FavoritesDirectory, "Gear.json");
        private static string BotFavoritePath => Path.Combine(FavoritesDirectory, "BotGear.json");

        private static bool m_PatchApplied = false;

        [HarmonyPatch(nameof(GearManager.Setup))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static unsafe void Setup_Prefix()
        {
            if (m_PatchApplied)
                return;

            if (!Directory.Exists(FavoritesDirectory))
                Directory.CreateDirectory(FavoritesDirectory);

            var readMethod = nameof(CellJSON.ReadFromDisk);
            var saveMethod = nameof(CellJSON.SaveToDisk);

            APILogger.Verbose(nameof(GearManager_Patches), "Applying ReadFromDisk Patch");
            Il2CppAPI.CreateGenericDetour<CellJSON, ReadFromDiskDelegate>(readMethod, "T", new[]
            {
                typeof(string).FullName,
                typeof(bool).MakeByRefType().FullName
            }, new[] { typeof(GearFavoritesData) }, Patch_ReadFromDisk, out m_ReadFromDiskOriginal);

            APILogger.Verbose(nameof(GearManager_Patches), "Applying SaveToDisk Patch");
            Il2CppAPI.CreateGenericDetour<CellJSON, SaveToDiskDelegate>(saveMethod, typeof(void).FullName, new string[]
            {
                typeof(string).FullName,
                "T"
            }, new[] { typeof(GearFavoritesData) }, Patch_SaveToDisk, out m_SaveToDiskOriginal);

            m_PatchApplied = true;
        }

        private static ReadFromDiskDelegate m_ReadFromDiskOriginal;
        private unsafe delegate IntPtr ReadFromDiskDelegate(IntPtr path, byte* createNew, Il2CppMethodInfo* methodInfo);
        private static unsafe IntPtr Patch_ReadFromDisk(IntPtr path, byte* createNew, Il2CppMethodInfo* methodInfo)
        {
            string pathStr = new Il2CppSystem.String(path);

            if (pathStr.EndsWith("GTFO_Favorites.txt", StringComparison.OrdinalIgnoreCase))
                return m_ReadFromDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(FavoritePath), createNew, methodInfo);
            else if (pathStr.EndsWith("GTFO_BotFavorites.txt", StringComparison.OrdinalIgnoreCase))
                return m_ReadFromDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(BotFavoritePath), createNew, methodInfo);
            else
                return m_ReadFromDiskOriginal(path, createNew, methodInfo);
        }

        private static SaveToDiskDelegate m_SaveToDiskOriginal;
        private unsafe delegate void SaveToDiskDelegate(IntPtr path, IntPtr obj, Il2CppMethodInfo* methodInfo);
        private static unsafe void Patch_SaveToDisk(IntPtr path, IntPtr favData, Il2CppMethodInfo* methodInfo)
        {
            string pathStr = new Il2CppSystem.String(path);

            if (pathStr.EndsWith("GTFO_Favorites.txt", StringComparison.OrdinalIgnoreCase))
                m_SaveToDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(FavoritePath), favData, methodInfo);
            else if (pathStr.EndsWith("GTFO_BotFavorites.txt", StringComparison.OrdinalIgnoreCase))
            {
                m_SaveToDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(BotFavoritePath), favData, methodInfo);
                if (File.Exists(pathStr))
                    File.Delete(pathStr); //Remove Existing BotFavorite File
            }
            else
                m_SaveToDiskOriginal(path, favData, methodInfo);
        }
    }
}
