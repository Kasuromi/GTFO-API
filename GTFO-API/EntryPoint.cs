using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;

namespace GTFO.API
{
    [BepInPlugin("dev.gtfomodding.gtfo-api", "GTFO-API", VersionInfo.Version)]
    public class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            APILogger.Verbose($"GTFO API Loading");
            m_Harmony = new Harmony("dev.gtfomodding.gtfo-api");
            m_Harmony.PatchAll();
        }

        private Harmony m_Harmony;
    }
}
