using HarmonyLib;
using Localization;

namespace GTFO.API.Patches;

[HarmonyPatch(typeof(CellSettingsApply), nameof(CellSettingsApply.ApplyLanguage))]
internal static class ApplyLanguage_Patches
{
    private static bool s_LanguageChanged;

    [HarmonyWrapSafe]
    public static void Prefix(int value)
    {
        s_LanguageChanged = Text.TextLocalizationService.CurrentLanguage != (Language)value;
    }

    [HarmonyWrapSafe]
    public static void Postfix()
    {
        if (s_LanguageChanged)
        {
            LocalizationAPI.LanguageChanged();
        }
    }
}
