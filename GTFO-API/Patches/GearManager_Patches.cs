using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.IL2CPP.Hook;
using Gear;
using HarmonyLib;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using UnhollowerRuntimeLib;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(GearManager), nameof(GearManager.Setup))]
    internal static class GearManager_Patches
    {
        private static string FavoritePath => Path.Combine(Paths.ConfigPath, "Favorite", "Gears.json");
        private static string BotFavoritePath => Path.Combine(Paths.ConfigPath, "Favorite", "BotGears.json");

        private unsafe delegate void SaveToDiskDelegate(IntPtr path, IntPtr obj, Il2CppMethodInfo* methodInfo);
        private unsafe delegate IntPtr ReadFromDiskDelegate(IntPtr path, byte* createNew, Il2CppMethodInfo* methodInfo);
        private static bool m_PatchApplied = false;

        private static SaveToDiskDelegate m_saveToDiskOriginal;
        private static ReadFromDiskDelegate m_readFromDiskOriginal;

        private static unsafe void Prefix()
        {
            if (m_PatchApplied)
                return;

            Directory.CreateDirectory(FavoritePath); //Create Favorite Subdir

            var readMethod = nameof(CellJSON.ReadFromDisk);
            var saveMethod = nameof(CellJSON.SaveToDisk);

            APILogger.Verbose("Core", "Applying ReadFromDisk Patch");
            CreateDetour<CellJSON, GearFavoritesData, ReadFromDiskDelegate>(isGeneric: true, readMethod, "T", new string[]
            {
                typeof(string).FullName,
                typeof(bool).MakeByRefType().FullName
            }, Patch_ReadFromDisk, out m_readFromDiskOriginal);
            APILogger.Verbose("Core", "Applying SaveToDisk Patch");
            CreateDetour<CellJSON, GearFavoritesData, SaveToDiskDelegate>(isGeneric: true, saveMethod, typeof(void).FullName, new string[]
            {
                typeof(string).FullName,
                "T"
            }, Patch_SaveToDisk, out m_saveToDiskOriginal);

            m_PatchApplied = true;
        }

        private static unsafe void CreateDetour<Class, G, D>(bool isGeneric, string methodName, string returnType, string[] paramTypes, D del, out D originalDel)
            where Class : Il2CppSystem.Object
            where G : Il2CppSystem.Object
            where D : Delegate
        {
            var genericTypePtr = Il2CppClassPointerStore<G>.NativeClassPtr;
            var classPtr = Il2CppClassPointerStore<Class>.NativeClassPtr;
            var methodPtr = IL2CPP.GetIl2CppMethod(classPtr, isGeneric, methodName, returnType, paramTypes);

            var methodInfo = new Il2CppSystem.Reflection.MethodInfo(IL2CPP.il2cpp_method_get_object(methodPtr, classPtr));
            var genericMethodInfo = methodInfo.MakeGenericMethod(new Il2CppReferenceArray<Il2CppSystem.Type>(new Il2CppSystem.Type[]
            {
                Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(genericTypePtr))
            }));

            var il2cppMethodInfo = UnityVersionHandler.Wrap((Il2CppMethodInfo*)IL2CPP.il2cpp_method_get_from_reflection(
                IL2CPP.Il2CppObjectBaseToPtrNotNull(genericMethodInfo)));

            FastNativeDetour.CreateAndApply(il2cppMethodInfo.MethodPointer, del, out originalDel);
        }

        #region NativePatch Delegates
        private static unsafe IntPtr Patch_ReadFromDisk(IntPtr path, byte* createNew, Il2CppMethodInfo* methodInfo)
        {
            var pathStr = (string)new Il2CppSystem.String(path);

            if (pathStr.EndsWith("GTFO_Favorites.txt", StringComparison.OrdinalIgnoreCase))
            {
                return m_readFromDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(FavoritePath), createNew, methodInfo);
            }
            else if (pathStr.EndsWith("GTFO_BotFavorites.txt", StringComparison.OrdinalIgnoreCase))
            {
                return m_readFromDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(BotFavoritePath), createNew, methodInfo);
            }
            else
            {
                return m_readFromDiskOriginal(path, createNew, methodInfo);
            }
        }

        private static unsafe void Patch_SaveToDisk(IntPtr path, IntPtr favData, Il2CppMethodInfo* methodInfo)
        {
            var pathStr = (string)new Il2CppSystem.String(path);

            if (pathStr.EndsWith("GTFO_Favorites.txt", StringComparison.OrdinalIgnoreCase))
            {
                m_saveToDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(FavoritePath), favData, methodInfo);
            }
            else if (pathStr.EndsWith("GTFO_BotFavorites.txt", StringComparison.OrdinalIgnoreCase))
            {
                m_saveToDiskOriginal(IL2CPP.ManagedStringToIl2Cpp(BotFavoritePath), favData, methodInfo);
                if (File.Exists(pathStr))
                {
                    File.Delete(pathStr); //Remove Existing BotFavorite File
                }
            }
            else
            {
                m_saveToDiskOriginal(path, favData, methodInfo);
            }
        }
        #endregion NativePatch Delegates
    }
}
