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
public static partial class LocalizationAPI
{
    /// <summary>
    /// Status info for the <see cref="LocalizationAPI"/>
    /// </summary>
    public static ApiStatusInfo Status => APIStatus.Localization;

    // this api stores localizations by first key name (e.g. 'test.name')
    // then by language (e.g. 'English'), and finally the value
    // (e.g. 'Cool Value')

    private static readonly Dictionary<string, Entry> s_Entries = new();

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
            if (!s_Entries.TryGetValue(entry, out Entry? localizationEntry))
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
    /// Gets the localized value for specified key in the current language and
    /// formats it with the specified <paramref name="args"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="args">
    /// The arguments to format the string with.
    /// </param>
    /// <returns>
    /// The found localized value formatted with <paramref name="args"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found.
    /// </exception>
    public static string FormatString(string key, params object?[] args)
    {
        return FormatString(key, FallbackValueOptions.None, args);
    }

    /// <summary>
    /// Gets the localized value for specified key in the current language or
    /// if not found gets the localized value in the specified
    /// <paramref name="fallbackLanguage"/>, and formats it with the specified
    /// <paramref name="args"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="args">
    /// The arguments to format the string with.
    /// </param>
    /// <param name="fallbackLanguage">
    /// The fallback language to use if no value for the current language could be found.
    /// </param>
    /// <returns>
    /// The found localized value formatted with <paramref name="args"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found.
    /// </exception>
    public static string FormatString(string key, Language fallbackLanguage, params object?[] args)
    {
        return FormatString(key, FallbackValueOptions.FallbackLang(fallbackLanguage), args);
    }

    /// <summary>
    /// Gets the localized value for specified key in the current language or
    /// a fallback value using the specified <paramref name="options"/>, and 
    /// formats it with the specified <paramref name="args"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="args">
    /// The arguments to format the string with.
    /// </param>
    /// <param name="options">
    /// The fallback options if no value could be found.
    /// </param>
    /// <returns>
    /// The found localized value formatted with <paramref name="args"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found and
    /// <paramref name="options"/>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
    /// is <see langword="false"/>.
    /// </exception>
    public static string FormatString(string key, FallbackValueOptions options, params object?[] args)
    {
        ValidateLocalizationKey(key);

        Language language = CurrentLanguage;

        if (!s_Entries.TryGetValue(key, out Entry? entry))
        {
            ValidateUseKey(key, language, options, nameof(FormatString));
            return key;
        }

        return entry.FormatString(language, key, options, args);
    }

    /// <summary>
    /// Gets the localized value for the specified key in the current language.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <returns>
    /// The found localized value for the current language.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found.
    /// </exception>
    public static string GetString(string key)
        => GetString(key, FallbackValueOptions.None);

    /// <summary>
    /// Gets a localized value for the specified key in the current language, or
    /// if not found gets the localized value in the specified
    /// <paramref name="fallbackLanguage"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="fallbackLanguage">
    /// The fallback language to use if no value for the current language could be found.
    /// </param>
    /// <returns>
    /// The found localized value for the current language, or the found
    /// localized value for the fallback language.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace, or
    /// <paramref name="fallbackLanguage"/> isn't a valid language.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found.
    /// </exception>
    public static string GetString(string key, Language fallbackLanguage)
    {
        return GetString(key, FallbackValueOptions.FallbackLang(fallbackLanguage));
    }

    /// <summary>
    /// Gets the localized value for the specified key in the current language, or
    /// a fallback value using the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="options">
    /// The fallback options if no value could be found.
    /// </param>
    /// <returns>
    /// The found localized value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No localized value could be found and
    /// <paramref name="options"/>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
    /// is <see langword="false"/>.
    /// </exception>
    public static string GetString(string key, FallbackValueOptions options)
    {
        ValidateLocalizationKey(key);

        Language language = CurrentLanguage;

        if (!s_Entries.TryGetValue(key, out Entry? entry))
        {
            ValidateUseKey(key, language, options, nameof(GetString));
            return key;
        }

        return entry.GetString(language, key, options);
    }

    /// <summary>
    /// Attempts to get a localized value for the specified key in the
    /// current language.
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
        return TryGetString(key, FallbackValueOptions.None, out value);
    }



    /// <summary>
    /// Attempts to get a localized value for the specified key in the
    /// current language, or if not found gets the localized value in the
    /// specified <paramref name="fallbackLanguage"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="fallbackLanguage">
    /// The fallback language to use if no value for the current language could be found.
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
    /// <paramref name="key"/> is empty/whitespace, or
    /// <paramref name="fallbackLanguage"/> isn't a valid language.
    /// </exception>
    public static bool TryGetString(string key, Language fallbackLanguage, [NotNullWhen(true)] out string? value)
    {
        return TryGetString(key, FallbackValueOptions.FallbackLang(fallbackLanguage), out value);
    }

    /// <summary>
    /// Attempts to get a localized value for the specified key in the
    /// current language, or a fallback value using the specified
    /// <paramref name="options"/>.
    /// </summary>
    /// <param name="key">
    /// The case-sensitive localization entry key.
    /// </param>
    /// <param name="options">
    /// The fallback options if no value could be found.
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
    public static bool TryGetString(string key, FallbackValueOptions options, [NotNullWhen(true)] out string? value)
    {
        ValidateLocalizationKey(key);

        if (!s_Entries.TryGetValue(key, out Entry? entry))
        {
            if (options.UseKey)
            {
                //? Maybe log warning here too?
                //? Should we maybe return true here instead?
                value = key;
                return false;
            }
            value = null;
            return false;
        }

        return entry.TryGetString(CurrentLanguage, key, options, out value);
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
        Entry entry = s_Entries[key];

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

        if (!s_Entries.TryGetValue(key, out Entry? entry) ||
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

        ref Entry? entry = ref CollectionsMarshal.GetValueRefOrAddDefault(s_Entries, key, out bool existing);

        entry ??= new Entry();
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

                ref Entry? entry = ref CollectionsMarshal.GetValueRefOrAddDefault(s_Entries, key, out bool existing);

                entry ??= new Entry();
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
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentException($"'{language}' is not a valid language", paramName ?? nameof(language));
        }
    }

    private static void ValidateUseKey(string key, Language language, FallbackValueOptions options, string useCategory)
    {
        if (!options.UseKey)
        {
            throw new KeyNotFoundException(string.Format(STR_NoValueFoundExceptionMsg, language, key));
        }
        APILogger.Warn(nameof(LocalizationAPI), string.Format(STR_UseKeyGeneric, language, key, useCategory));
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
}

#nullable restore
