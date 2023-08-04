using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GameData;
using Localization;

namespace GTFO.API;
#nullable enable

partial class LocalizationAPI
{
    private sealed class Entry
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

        private bool TryGetStringInAnyLanguage([NotNullWhen(true)] out string? value)
        {
            for (int i = 0; i < m_ValuesByLanguage.Length; i++)
            {
                value = m_ValuesByLanguage[i];
                if (value is not null)
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        private bool TryGetStringInLanguage(Language language, [NotNullWhen(true)] out string? value)
        {
            int index = (int)language - 1;
            value = m_ValuesByLanguage[index];
            return value is not null;
        }

        public bool TryGetString(Language language, FallbackValueOptions options, [NotNullWhen(true)] out string? value)
        {
            if (TryGetStringInLanguage(language, out value))
            {
                return true;
            }

            if (options.UseFallbackLanguage && TryGetStringInLanguage(options.FallbackLanguage.Value, out value))
            {
                return true;
            }

            if (options.UseAnyLanguage && TryGetStringInAnyLanguage(out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <exception cref="KeyNotFoundException">
        /// No localization value could be found and
        /// <paramref name="options"/>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
        /// is <see langword="false"/>.
        /// </exception>
        private string GetStringForTextDB(Language language, string key, FallbackValueOptions options)
        {
            if (this.TryGetString(language, options, out string? value))
            {
                return value;
            }

            ValidateUseKey(key, language, options, "GenerateTextDB");
            return key;

        }

        [MemberNotNull(nameof(TextBlockId))]
        public void GenerateTextDataBlock(string key, TextDBOptions options, bool force = false)
        {
            m_Options = options;
            GenerateTextDataBlock(key, force);
        }

        /// <exception cref="KeyNotFoundException">
        /// No localization value could be found and
        /// <see cref="TextDBOptions.FallbackOptions">FallbackOptions</see>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
        /// is <see langword="false"/>.
        /// </exception>
        [MemberNotNull(nameof(TextBlockId))]
        public void GenerateTextDataBlock(string key, bool force = false)
        {
            if (TextBlockId.HasValue && !force)
            {
                return;
            }

            FallbackValueOptions fallbackValueOptions = m_Options.FallbackOptions ?? FallbackValueOptions.AnyLangOrKey;

            TextDataBlock block = new TextDataBlock()
            {
                CharacterMetaData = m_Options.CharacterMetadataId ?? 1U,
                internalEnabled = true,
                ExportVersion = 1,
                ImportVersion = 1,
                Description = string.Empty,
                English = GetStringForTextDB(Language.English, key, fallbackValueOptions),
                French = GetStringForTextDB(Language.French, key, fallbackValueOptions),
                Italian = GetStringForTextDB(Language.Italian, key, fallbackValueOptions),
                German = GetStringForTextDB(Language.German, key, fallbackValueOptions),
                Spanish = GetStringForTextDB(Language.Spanish, key, fallbackValueOptions),
                Russian = GetStringForTextDB(Language.Russian, key, fallbackValueOptions),
                Portuguese_Brazil = GetStringForTextDB(Language.Portuguese_Brazil, key, fallbackValueOptions),
                Polish = GetStringForTextDB(Language.Polish, key, fallbackValueOptions),
                Japanese = GetStringForTextDB(Language.Japanese, key, fallbackValueOptions),
                Korean = GetStringForTextDB(Language.Korean, key, fallbackValueOptions),
                Chinese_Traditional = GetStringForTextDB(Language.Chinese_Traditional, key, fallbackValueOptions),
                Chinese_Simplified = GetStringForTextDB(Language.Chinese_Simplified, key, fallbackValueOptions),
                name = key,
                MachineTranslation = false,
                SkipLocalization = false,
                // have GTFO autogenerate a valid persistent id
                persistentID = 1
            };

            TextDataBlock.AddBlock(block);

            TextBlockId = block.persistentID;
        }

        public bool TryGetString(Language language, string key, FallbackValueOptions options, out string? value)
        {
            if (TryGetString(language, options, out value))
            {
                return true;
            }

            if (options.UseKey)
            {
                //? Maybe log warning here too?
                value = key;
                return true;
            }
            else
            {
                value = null;
                return false;
            }

        }

        /// <exception cref="KeyNotFoundException">
        /// No localization value could be found and
        /// <paramref name="options"/>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
        /// is <see langword="false"/>.
        /// </exception>
        public string GetString(Language language, string key, FallbackValueOptions options)
        {
            if (TryGetString(language, options, out string? value))
            {
                return value;
            }

            ValidateUseKey(key, language, options, nameof(GetString));
            return key;
        }

        /// <exception cref="KeyNotFoundException">
        /// No localization value could be found and
        /// <paramref name="options"/>.<see cref="FallbackValueOptions.UseKey">UseKey</see>
        /// is <see langword="false"/>.
        /// </exception>
        public string FormatString(Language language, string key, FallbackValueOptions options, object?[] args)
        {
            if (TryGetString(language, options, out string? value))
            {
                return string.Format(value, args);
            }

            ValidateUseKey(key, language, options, nameof(FormatString));
            return key;
        }

        public bool HasValueInLanguage(Language language)
        {
            ValidateLanguage(language);

            return m_ValuesByLanguage[((int)language - 1)] is not null;
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
