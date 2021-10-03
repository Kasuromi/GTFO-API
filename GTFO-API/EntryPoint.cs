using BepInEx;
using BepInEx.IL2CPP;
using GTFO.API.Impl;
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

            APILogger.Verbose("Core", "Applying Patches");
            m_Harmony = new Harmony("dev.gtfomodding.gtfo-api");
            m_Harmony.PatchAll();

            APILogger.Verbose("Core", "Plugin Load Complete");
        }

        private Harmony m_Harmony;
    }
}
