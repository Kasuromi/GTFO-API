using System;
using System.Resources;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using GameData;
using GTFO.API.Attributes;
using Localization;
using GTFO.API.Resources;

#nullable enable
namespace GTFO.API;

[API("Localization")]
public static class LocalizationAPI
{
    /// <summary>
    /// Status info for the <see cref="LocalizationAPI"/>
    /// </summary>
    public static ApiStatusInfo Status => APIStatus.Localization;

    // this api stores localizations by first key name (e.g. 'test.name')
    // then by language (e.g. 'English'), and finally the value
    // (e.g. 'Cool Value')

    private static readonly Dictionary<string, LocalizationEntry> s_Entries = new();

    private static readonly List<string> s_EntriesToGenerateTextDBs = new();
    private static bool s_GameDataInitialized = false;

    internal static void Setup()
    {
        GameDataAPI.OnGameDataInitialized += OnGameDataInitialized;

        Status.Created = true;
        Status.Ready = true;
    }

    internal static void OnGameDataInitialized()
    {
        s_GameDataInitialized = true;
        foreach (string entry in s_EntriesToGenerateTextDBs)
        {
            if (!s_Entries.TryGetValue(entry, out LocalizationEntry? localizationEntry))
            {
                continue;
            }
            localizationEntry.GenerateTextDataBlock(entry, force: true);
        }
    }

    /// <summary>
    /// Gets the current language GTFO is using.
    /// </summary>
    public static Language CurrentLanguage
    {
        get
        {
            //? There might be a better way to get this.
            return (Language)CellSettingsManager.SettingsData.Accessibility.Language.Value;
        }
    }

    /// <summary>
    /// Get a localization string using the specified key for the current language,
    /// and formats it with the specified <paramref name="args"/>, or if not found
    /// returns <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="args">
    /// The arguments to format the string with.
    /// </param>
    /// <returns>
    /// The found localization value for the current language formatted with
    /// <paramref name="args"/>, or <paramref name="key"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public static string FormatString(string key, params object?[] args)
    {
        if (!TryGetString(key, out string? value))
        {
            return key;
        }
        return string.Format(value, args);
    }

    /// <summary>
    /// Get a localization string using the specified key for the current language,
    /// or if not found returns <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <returns>
    /// The found localization value for the current language, or <paramref name="key"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public static string GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGetString(key, out string? value))
        {
            return key;
        }
        return value;
    }

    /// <summary>
    /// Attempts to get a localization string using the specified key for the current language.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="value">
    /// The found value. Won't be null if this method returns <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> was found;
    /// <see langword="false"/>, otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryGetString(string key, [NotNullWhen(true)] out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!s_Entries.TryGetValue(key, out LocalizationEntry? entry))
        {
            value = null;
            return false;
        }

        return entry.TryGetString(CurrentLanguage, out value);
    }

    /// <summary>
    /// Loads localizations from the calling assembly's resources. 
    /// </summary>
    /// <param name="baseName">
    /// The base resource name. Should be in the format <c>AssemblyName.Path.Resource</c>,
    /// where <c>Path</c> is a dot-seperated path to the resource from the root project
    /// folder.
    /// <para>
    /// Exclude file extensions (like <c>.resx</c>) or language tags (like <c>.en</c>)
    /// from the name.
    /// </para>
    /// </param>
    /// <param name="generateTextDataBlocks">
    /// Whether or not to generate text data blocks for each localization entry found.
    /// </param>
    /// <exception cref="AggregateException">
    /// Thrown if exceptions occur whilst loading a resource set. This will
    /// only be thrown after loading all possible resource sets.
    /// </exception>
    public static void LoadFromResources(string baseName, bool generateTextDataBlocks = false)
        => LoadFromResources(baseName, Assembly.GetCallingAssembly(), generateTextDataBlocks);

    /// <summary>
    /// Loads localizations from the specified assembly's resources.
    /// </summary>
    /// <param name="baseName">
    /// The base resource name. Should be in the format <c>AssemblyName.Path.Resource</c>,
    /// where <c>Path</c> is a dot-seperated path to the resource from the root project
    /// folder.
    /// </param>
    /// <param name="assembly">
    /// The assembly's resources to use.
    /// </param>
    /// <param name="generateTextDataBlocks">
    /// Whether or not to generate text data blocks for each localization entry found.
    /// </param>
    /// <exception cref="AggregateException">
    /// Thrown if exceptions occur whilst loading a resource set. This will
    /// only be thrown after loading all possible resource sets.
    /// </exception>
    public static void LoadFromResources(string baseName, Assembly assembly, bool generateTextDataBlocks = false)
    {
        ResourceManager resourceManager = new(baseName, assembly);

        //? We could refactor this later to just use the cultures from
        //? Localization.Language, but for now we allow all cultures
        //? instead of only Neutral cultures.

        List<Exception> allExceptions = new();

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            bool isNeutral = culture.IsNeutralCulture;
            Language language = GetLanguage(culture);
            if (language == default)
            {
                continue;
            }

            ResourceSet? resourceSet;
            try
            {
                resourceSet = resourceManager.GetResourceSet(culture, true, true);
            }
            catch (MissingManifestResourceException)
            {
                continue;
            }
            catch (Exception ex)
            {
                allExceptions.Add(ex);
                continue;
            }

            if (resourceSet is null)
            {
                continue;
            }

            foreach (DictionaryEntry resourceEntry in resourceSet)
            {
                if (resourceEntry.Key is not string key ||
                    resourceEntry.Value is not string value)
                {
                    continue;
                }

                ref LocalizationEntry? entry = ref CollectionsMarshal.GetValueRefOrAddDefault(s_Entries, key, out bool existing);

                entry ??= new LocalizationEntry();
                // neutral languages take priority over non-neutral languages.
                entry.AddValue(language, value, force: isNeutral);

                //? optimization: there might be a better ordering of
                //?               the parameters.
                if (generateTextDataBlocks && !existing)
                {
                    // add anyways, as all entries will have
                    // their datablock generated again when reloading
                    s_EntriesToGenerateTextDBs.Add(key);
                    if (s_GameDataInitialized)
                    {
                        entry.GenerateTextDataBlock(key);
                    }
                }
            }
        }

        resourceManager.ReleaseAllResources();

        if (allExceptions.Count > 0)
        {
            throw new AggregateException(allExceptions);
        }
    }

    private static Language GetLanguage(CultureInfo info)
    {
        while (!info.IsNeutralCulture)
        {
            info = info.Parent;
            // an infinite loop occurs here with an empty culture info,
            // so this helps fix that.
            if (info.Name.Length == 0)
            {
                return default;
            }
        }

        return info.Name switch
        {
            "en" => Language.English,
            "fr" => Language.French,
            "it" => Language.Italian,
            "de" => Language.German,
            "es" => Language.Spanish,
            "ru" => Language.Russian,
            "pt" => Language.Portuguese_Brazil,
            "pl" => Language.Polish,
            "ja" => Language.Japanese,
            "ko" => Language.Korean,
            "zh-Hans" => Language.Chinese_Simplified,
            "zh-Hant" => Language.Chinese_Traditional,
            _ => default
        };
    }

    private static void UpdateTextDataBlock(TextDataBlock block, Language language, string text)
    {
        switch (language)
        {
            case Language.English:
                block.English = text;
                break;
            case Language.French:
                block.French = text;
                break;
            case Language.Italian:
                block.Italian = text;
                break;
            case Language.German:
                block.German = text;
                break;
            case Language.Spanish:
                block.Spanish = text;
                break;
            case Language.Russian:
                block.Russian = text;
                break;
            case Language.Portuguese_Brazil:
                block.Portuguese_Brazil = text;
                break;
            case Language.Polish:
                block.Polish = text;
                break;
            case Language.Japanese:
                block.Japanese = text;
                break;
            case Language.Korean:
                block.Korean = text;
                break;
            case Language.Chinese_Traditional:
                block.Chinese_Traditional = text;
                break;
            case Language.Chinese_Simplified:
                block.Chinese_Simplified = text;
                break;
        }
    }

    private sealed class LocalizationEntry
    {
        // length is the maximum value of a Language (right now is
        // Chinese_Simplified with a value of 12)
        private readonly string?[] m_ValuesByLanguage = new string[12];

        // the text datablock is not generated unless told
        // to manually do so on creation, or when specifically called up.
        private uint? m_TextBlockId;

        public string GetStringOrDefault(Language language, string defaultValue)
        {
            int index = (int)language - 1;
            if (index < 0 || index >= m_ValuesByLanguage.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return m_ValuesByLanguage[index] ?? defaultValue;
        }

        public bool TryGetString(Language language, [NotNullWhen(true)] out string? value)
        {
            int index = (int)language - 1;
            if (index < 0 || index >= m_ValuesByLanguage.Length)
            {
                value = null;
                return false;
            }

            value = m_ValuesByLanguage[index];
            return value is not null;
        }

        public void GenerateTextDataBlock(string key, bool force = false)
        {
            if (m_TextBlockId.HasValue && !force)
            {
                return;
            }

            TextDataBlock block = new TextDataBlock()
            {
                CharacterMetaData = 1,
                internalEnabled = true,
                ExportVersion = 1,
                ImportVersion = 1,
                Description = string.Empty,
                English = GetStringOrDefault(Language.English, key),
                French = GetStringOrDefault(Language.French, key),
                Italian = GetStringOrDefault(Language.Italian, key),
                German = GetStringOrDefault(Language.German, key),
                Spanish = GetStringOrDefault(Language.Spanish, key),
                Russian = GetStringOrDefault(Language.Russian, key),
                Portuguese_Brazil = GetStringOrDefault(Language.Portuguese_Brazil, key),
                Polish = GetStringOrDefault(Language.Polish, key),
                Japanese = GetStringOrDefault(Language.Japanese, key),
                Korean = GetStringOrDefault(Language.Korean, key),
                Chinese_Traditional = GetStringOrDefault(Language.Chinese_Traditional, key),
                Chinese_Simplified = GetStringOrDefault(Language.Chinese_Simplified, key),
                name = key,
                MachineTranslation = false,
                SkipLocalization = false,
                // have GTFO autogenerate a valid persistent id
                persistentID = 1
            };

            TextDataBlock.AddBlock(block);

            m_TextBlockId = block.persistentID;
        }

        public void AddValue(Language language, string value, bool force = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            int index = (int)language - 1;
            if (index < 0 || index >= m_ValuesByLanguage.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            ref string? current = ref m_ValuesByLanguage[index];
            if (current is not null && !force) return;

            current = value;

            if (!m_TextBlockId.HasValue) return;


            // update text data block
            TextDataBlock? block = TextDataBlock.GetBlock(m_TextBlockId.Value);
            if (block == null) return;

            UpdateTextDataBlock(block, language, current);
        }
    }
}

#nullable restore
