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
using System.Runtime.CompilerServices;

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
        EventAPI.OnAssetsLoaded += OnGameAssetsLoaded;

        Status.Created = true;
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

    internal static void OnGameAssetsLoaded()
    {
        // current language should be available now.
        Status.Ready = true;
    }

    internal static void LanguageChanged()
    {
        OnLanguageChange?.Invoke();
    }

    /// <summary>
    /// An event called when the game's current language is changed.
    /// </summary>
    public static event Action? OnLanguageChange;

    /// <summary>
    /// Gets the current language GTFO is using.
    /// </summary>
    public static Language CurrentLanguage
        => Text.TextLocalizationService.CurrentLanguage;

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
        ValidateLocalizationKey(key);

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
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    public static string GetString(string key)
    {
        ValidateLocalizationKey(key);

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
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    public static bool TryGetString(string key, [NotNullWhen(true)] out string? value)
    {
        ValidateLocalizationKey(key);

        if (!s_Entries.TryGetValue(key, out LocalizationEntry? entry))
        {
            value = null;
            return false;
        }

        return entry.TryGetString(CurrentLanguage, out value);
    }

    /// <summary>
    /// Whether or not this localization api has the given localization key
    /// </summary>
    /// <param name="key">
    /// The localization key to test.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a localization entry with key <paramref name="key"/>
    /// was found; <see langword="false"/>, otherwise.
    /// </returns>
    public static bool HasKey([NotNullWhen(true)] string? key)
    {
        return !string.IsNullOrWhiteSpace(key) && s_Entries.ContainsKey(key);
    }

    /// <summary>
    /// Generates a text data block for the specific localization key
    /// if not done so already.
    /// </summary>
    /// <param name="key">
    /// The localization entry key.
    /// </param>
    /// <param name="textDataBlockOptions">
    /// The options for generating the text data block. If <see langword="null"/>,
    /// this method will use the default text data block options.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No such localization entry with key <paramref name="key"/>
    /// exists.
    /// </exception>
    /// <returns>The text data block id for the key.</returns>
    public static uint GenerateTextBlock(string key, TextDBOptions? textDataBlockOptions = null)
    {
        ValidateLocalizationKey(key);

        // keynotfound exception passed up from s_Entries
        LocalizationEntry entry = s_Entries[key];

        if (entry.TextBlockId.HasValue)
        {
            return entry.TextBlockId.Value;
        }

        if (textDataBlockOptions.HasValue)
        {
            entry.GenerateTextDataBlock(key, textDataBlockOptions.Value);
        }
        else
        {
            entry.GenerateTextDataBlock(key);
        }
        s_EntriesToGenerateTextDBs.Add(key);

        return entry.TextBlockId.Value;
    }

    /// <summary>
    /// Attempts to get the text data block id of the given localization
    /// key.
    /// </summary>
    /// <param name="key">
    /// The localization key.
    /// </param>
    /// <param name="blockId">
    /// The found datablock id, or <c>0</c> if not found.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="blockId"/> was found;
    /// <see langword="false"/>, otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    public static bool TryGetTextBlockId(string key, out uint blockId)
    {
        ValidateLocalizationKey(key);

        if (!s_Entries.TryGetValue(key, out LocalizationEntry? entry) ||
            !entry.TextBlockId.HasValue)
        {
            blockId = 0;
            return false;
        }

        blockId = entry.TextBlockId.Value;
        return true;
    }

    /// <summary>
    /// Manually adds a localization entry.
    /// </summary>
    /// <param name="key">
    /// The localization key.
    /// </param>
    /// <param name="language">
    /// The language of the value.
    /// </param>
    /// <param name="value">
    /// The value for the localization key <paramref name="key"/>.
    /// </param>
    /// <param name="textDataBlockOptions">
    /// The options for generating the text data block. If <see langword="null"/>,
    /// this method wont generate text data blocks.
    /// </param>
    /// <remarks>
    /// This method will not override existing values!
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace, or <paramref name="language"/>
    /// isn't a valid language.
    /// </exception>
    public static void AddEntry(string key, Language language, string value, TextDBOptions? textDataBlockOptions = null)
    {
        ValidateLocalizationKey(key);
        ValidateLanguage(language);

        value ??= string.Empty;

        ref LocalizationEntry? entry = ref CollectionsMarshal.GetValueRefOrAddDefault(s_Entries, key, out bool existing);

        entry ??= new LocalizationEntry();
        entry.AddValue(language, value);

        //? optimization: there might be a better ordering of
        //?               the parameters.
        if (textDataBlockOptions.HasValue && !existing)
        {
            // add anyways, as all entries will have
            // their datablock generated again when reloading
            s_EntriesToGenerateTextDBs.Add(key);
            if (s_GameDataInitialized)
            {
                entry.GenerateTextDataBlock(key, textDataBlockOptions.Value);
            }
        }
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
    /// <param name="textDataBlockOptions">
    /// The options for generating the text data block. If <see langword="null"/>,
    /// this method wont generate text data blocks.
    /// </param>
    /// <exception cref="AggregateException">
    /// Thrown if exceptions occur whilst loading a resource set. This will
    /// only be thrown after loading all possible resource sets.
    /// </exception>
    public static void LoadFromResources(string baseName, TextDBOptions? textDataBlockOptions = null)
        => LoadFromResources(baseName, Assembly.GetCallingAssembly(), textDataBlockOptions);

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
    /// <param name="textDataBlockOptions">
    /// The options for generating the text data block. If <see langword="null"/>,
    /// this method wont generate text data blocks.
    /// </param>
    /// <exception cref="AggregateException">
    /// Thrown if exceptions occur whilst loading a resource set. This will
    /// only be thrown after loading all possible resource sets.
    /// </exception>
    public static void LoadFromResources(string baseName, Assembly assembly, TextDBOptions? textDataBlockOptions = null)
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
                if (textDataBlockOptions.HasValue && !existing)
                {
                    // add anyways, as all entries will have
                    // their datablock generated again when reloading
                    s_EntriesToGenerateTextDBs.Add(key);
                    if (s_GameDataInitialized)
                    {
                        entry.GenerateTextDataBlock(key, textDataBlockOptions.Value);
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

    private static void ValidateLocalizationKey([NotNull] string key, [CallerArgumentExpression(nameof(key))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(key, paramName);
        if (string.IsNullOrWhiteSpace(paramName))
        {
            throw new ArgumentException("Localization key cannot be empty/whitespace", paramName ?? nameof(key));
        }
    }

    private static void ValidateLanguage(Language language, [CallerArgumentExpression(nameof(language))] string? paramName = null)
    {
        if (language < Language.English || language > Language.Chinese_Simplified)
        {
            throw new ArgumentException($"'{language}' is not a valid language", paramName ?? nameof(language));
        }
    }

    private static Language GetLanguage(CultureInfo info)
    {
        while (!info.IsNeutralCulture)
        {
            info = info.Parent;
            // an infinite loop occurs here with an empty culture info,
            // so this helps fix that.
            if (string.IsNullOrEmpty(info.Name))
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

    /// <summary>
    /// Options for generating the text data block.
    /// </summary>
    public struct TextDBOptions
    {
        private Language? m_FallbackLanguage;
        private uint? m_CharacterMetadataId;

        /// <summary>
        /// The <see cref="TextCharacterMetaDataBlock"/> Id to use
        /// for the text data block. If not specified, defaults to <c>1</c>.
        /// </summary>
        public uint? CharacterMetadataId
        {
            readonly get => m_CharacterMetadataId;
            set => m_CharacterMetadataId = value;
        }

        /// <summary>
        /// The language to fallback to if a language in <see cref="TextDataBlock"/>
        /// has no value. If <see langword="null"/>, will use the first found language
        /// that has a value. If the fallback language has no value, then the
        /// localization key itself will be used.
        /// </summary>
        /// <remarks>
        /// To find the first language with a value, first <see cref="Language.English"/>
        /// is used, then it's incremented by one until reaching
        /// <see cref="Language.Chinese_Simplified"/>.
        /// </remarks>
        /// <exception cref="ArgumentException" accessor="set">
        /// Attempting to set to an invalid language.
        /// </exception>
        public Language? FallbackLanguage
        {
            readonly get => m_FallbackLanguage;
            set
            {
                if (!value.HasValue)
                {
                    m_FallbackLanguage = value;
                    return;
                }

                Language language = value.Value;
                if (language < Language.English || language > Language.Chinese_Simplified)
                {
                    throw new ArgumentException($"'{language}' isn't a valid language.", nameof(value));
                }

                m_FallbackLanguage = language;
            }
        }
    }

    private sealed class LocalizationEntry
    {
        // length is the maximum value of a Language (right now is
        // Chinese_Simplified with a value of 12)
        private readonly string?[] m_ValuesByLanguage = new string[12];

        // A local definition of the options for text data block generated.
        // Used when gamedata re-initializes, as there would be no access
        // to a TextDBOptions object.
        private TextDBOptions m_Options;

        // the text datablock is not generated unless told
        // to manually do so on creation, or when specifically called up.
        public uint? TextBlockId { get; private set; }

        private string TryGetStringInLanguageOrDefault(Language language, string defaultValue)
        {
            return TryGetStringInLanguage(language) ?? defaultValue;
        }

        private string? TryGetStringInAnyLanguage()
        {
            for (int i = 0; i < m_ValuesByLanguage.Length; i++)
            {
                string? value = m_ValuesByLanguage[i];
                if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }

        private string? TryGetStringInLanguage(Language language, bool fromFallback = false)
        {
            int index = (int)language - 1;
            if (index < 0 || index >= m_ValuesByLanguage.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            string? value = m_ValuesByLanguage[index];
            if (value is not null || fromFallback)
            {
                return value;
            }

            if (m_Options.FallbackLanguage.HasValue)
            {
                return TryGetStringInLanguage(m_Options.FallbackLanguage.Value, fromFallback: true);
            }
            else
            {
                return TryGetStringInAnyLanguage();
            }
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

        [MemberNotNull(nameof(TextBlockId))]
        public void GenerateTextDataBlock(string key, TextDBOptions options, bool force = false)
        {
            m_Options = options;
            GenerateTextDataBlock(key, force);
        }

        [MemberNotNull(nameof(TextBlockId))]
        public void GenerateTextDataBlock(string key, bool force = false)
        {
            if (TextBlockId.HasValue && !force)
            {
                return;
            }

            TextDataBlock block = new TextDataBlock()
            {
                CharacterMetaData = m_Options.CharacterMetadataId ?? 1U,
                internalEnabled = true,
                ExportVersion = 1,
                ImportVersion = 1,
                Description = string.Empty,
                English = TryGetStringInLanguageOrDefault(Language.English, key),
                French = TryGetStringInLanguageOrDefault(Language.French, key),
                Italian = TryGetStringInLanguageOrDefault(Language.Italian, key),
                German = TryGetStringInLanguageOrDefault(Language.German, key),
                Spanish = TryGetStringInLanguageOrDefault(Language.Spanish, key),
                Russian = TryGetStringInLanguageOrDefault(Language.Russian, key),
                Portuguese_Brazil = TryGetStringInLanguageOrDefault(Language.Portuguese_Brazil, key),
                Polish = TryGetStringInLanguageOrDefault(Language.Polish, key),
                Japanese = TryGetStringInLanguageOrDefault(Language.Japanese, key),
                Korean = TryGetStringInLanguageOrDefault(Language.Korean, key),
                Chinese_Traditional = TryGetStringInLanguageOrDefault(Language.Chinese_Traditional, key),
                Chinese_Simplified = TryGetStringInLanguageOrDefault(Language.Chinese_Simplified, key),
                name = key,
                MachineTranslation = false,
                SkipLocalization = false,
                // have GTFO autogenerate a valid persistent id
                persistentID = 1
            };

            TextDataBlock.AddBlock(block);

            TextBlockId = block.persistentID;
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

            if (!TextBlockId.HasValue) return;


            // update text data block
            TextDataBlock? block = TextDataBlock.GetBlock(TextBlockId.Value);
            if (block == null) return;

            UpdateTextDataBlock(block, language, current);
        }
    }
}

#nullable restore
