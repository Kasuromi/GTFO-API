using BepInEx;
using BepInEx.IL2CPP;
using GTFO.API.Impl;
using GTFO.API.Patches;
using GTFO.API.Patches.Native;
using GTFO.API.Utilities.Impl;
using GTFO.API.Wrappers;
using HarmonyLib;
using UnhollowerRuntimeLib;

namespace GTFO.API
{
    [BepInPlugin("dev.gtfomodding.gtfo-api", "GTFO-API", VersionInfo.Version)]
    internal class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            APILogger.Verbose("Core", "Registering API Implementations");
            ClassInjector.RegisterTypeInIl2Cpp<NetworkAPI_Impl>();
            ClassInjector.RegisterTypeInIl2Cpp<AssetAPI_Impl>();

            APILogger.Verbose("Core", "Registering Wrappers");
            ClassInjector.RegisterTypeInIl2Cpp<ItemWrapped>();

            APILogger.Verbose("Core", "Registering Utilities Implementations");
            ClassInjector.RegisterTypeInIl2Cpp<ThreadDispatcher_Impl>();

            APILogger.Verbose("Core", "Applying Patches");
            m_Harmony = new Harmony("dev.gtfomodding.gtfo-api");
            m_Harmony.PatchAll();

            AssetAPI.Setup();

            APILogger.Verbose("Core", "Plugin Load Complete");

            APILogger.Warn("GTFO-API", "Syringes are currently disabled in this version");
            //new SyringeFirstPerson_Patch().Apply();
        }

        private Harmony m_Harmony;
    }
}
