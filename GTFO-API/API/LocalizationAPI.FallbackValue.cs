using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GameData;
using Localization;

namespace GTFO.API;
#nullable enable

partial class LocalizationAPI
{
    /// <summary>
    /// Exception message for not being able to find a localization value
    /// <list type="table">
    /// <item>
    /// <term><c>{0}</c></term>
    /// <description>language</description>
    /// </item>
    /// <item>
    /// <term><c>{1}</c></term>
    /// <description>key</description>
    /// </item>
    /// </list>
    /// </summary>
    private static readonly string STR_NoValueFoundExceptionMsg = "No localization value exists for key '{1}' in language '{0}'.";
    /// <summary>
    /// Message for logging when using a key as a localization value.
    /// <list type="table">
    /// <item>
    /// <term><c>{0}</c></term>
    /// <description>language</description>
    /// </item>
    /// <item>
    /// <term><c>{1}</c></term>
    /// <description>key</description>
    /// </item>
    /// <item>
    /// <term><c>{2}</c></term>
    /// <description>category</description>
    /// </item>
    /// </list>
    /// </summary>
    private static readonly string STR_UseKeyGeneric = "{2}: No localization value exists for key '{1}' in language '{0}', defaulting to key.";

    /// <summary>
    /// The flags of <see cref="FallbackValueOptions"/>.
    /// </summary>
    /// <remarks>
    /// <i>A warning will be logged if the key is forced to be returned.</i>
    /// </remarks>
    [Flags]
    public enum FallbackValueFlags
    {
        /// <summary>
        /// Strictest option - no fallback will be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Enables use of returning a specified fallback language's value.
        /// </summary>
        FallbackLanguage = 1,

        /// <summary>
        /// Enables use of returning a value of any language that has one.
        /// </summary>
        AnyLanguage = 2,

        /// <summary>
        /// Enables returning the key if a value is not found.
        /// </summary>
        /// <remarks>
        /// <i>A warning will be logged if the key is forced to be returned.</i>
        /// </remarks>
        Key = 4,

        /// <summary>
        /// Enables use of returning a specified fallback language's value, or a
        /// value of any language that has one.
        /// </summary>
        FallbackOrAnyLanguage = FallbackLanguage | AnyLanguage,

        /// <summary>
        /// Enables use of returning a specified fallback language's value, or
        /// returning the key if a value is not found.
        /// </summary>
        /// <remarks>
        /// <i>A warning will be logged if the key is forced to be returned.</i>
        /// </remarks>
        FallbackLanguageOrKey = FallbackLanguage | Key,

        /// <summary>
        /// Enables use of returning a value of any language that has one, or
        /// returning the key if a value is not found.
        /// </summary>
        /// <remarks>
        /// <i>A warning will be logged if the key is forced to be returned.</i>
        /// </remarks>
        AnyLanguageOrKey = AnyLanguage | Key,

        /// <summary>
        /// Enables use of returning a specified fallback language's value, a
        /// value of any language that has one, or returning the key if a value is
        /// not found.
        /// </summary>
        /// <remarks>
        /// <i>A warning will be logged if the key is forced to be returned.</i>
        /// </remarks>
        FallbackOrAnyLanguageOrKey = FallbackLanguage | AnyLanguage | Key,
    }

    /// <summary>
    /// The options for fallback values when translating. A value is found using the
    /// specified table:
    /// <list type="table">
    /// <item>
    /// <term>
    /// If no entry with the key is found
    /// </term>
    /// <description>
    /// If this has the flag <see cref="FallbackValueFlags.Key"/>, return the key; 
    /// otherwise throw a <see cref="KeyNotFoundException"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If a value exists for the current language
    /// </term>
    /// <description>
    /// Return that value.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If a value exists for a specified fallback language<c>*</c>
    /// </term>
    /// <description>
    /// Return that value.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If a value exists for any language<c>**</c>
    /// </term>
    /// <description>
    /// Return that value.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Otherwise
    /// </term>
    /// <description>
    /// If this has the flag <see cref="FallbackValueFlags.Key"/>, return the key; 
    /// otherwise throw a <see cref="KeyNotFoundException"/>.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <c>*</c> Requires this to have the flag
    /// <see cref="FallbackValueFlags.FallbackLanguage"/>.
    /// </para>
    /// <para>
    /// <c>**</c> Requires this to have the flag
    /// <see cref="FallbackValueFlags.AnyLanguage"/>. Looping starts at
    /// <see cref="Language.English"/>, and ends at
    /// <see cref="Language.Chinese_Simplified"/>.
    /// </para>
    /// </summary>
    public readonly struct FallbackValueOptions : IEquatable<FallbackValueOptions>
    {
        /// <summary>
        /// Initializes this fallback options instance with the specified
        /// <paramref name="flags"/> and <paramref name="fallbackLanguage"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="flags"/> includes the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/>, then
        /// <paramref name="fallbackLanguage"/> is required to not be
        /// <see langword="null"/>.
        /// </remarks>
        /// <param name="flags">
        /// The fallback flags.
        /// </param>
        /// <param name="fallbackLanguage">
        /// The fallback language to use. Only used when specifying the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="fallbackLanguage"/> isn't a valid language.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="flags"/> includes the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/>, and
        /// <paramref name="fallbackLanguage"/> is <see langword="null"/>.
        /// </exception>
        public FallbackValueOptions(FallbackValueFlags flags, Language? fallbackLanguage = null)
        {
            if (flags.HasFlag(FallbackValueFlags.FallbackLanguage))
            {
                if (!fallbackLanguage.HasValue)
                {
                    throw new ArgumentNullException(nameof(fallbackLanguage), $"A fallback language is required if specifying the flag {nameof(FallbackValueFlags.FallbackLanguage)}");
                }
                ValidateLanguage(fallbackLanguage.Value, nameof(fallbackLanguage));
            }

            Flags = flags;
            FallbackLanguage = fallbackLanguage;
        }

        /// <summary>
        /// The flags for this fallback options instance.
        /// </summary>
        public FallbackValueFlags Flags { get; }
        /// <summary>
        /// The fallback language for this fallback options instance.
        /// </summary>
        /// <remarks>
        /// Not <see langword="null"/> if <see cref="Flags"/> includes the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/>.
        /// </remarks>
        public Language? FallbackLanguage { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Flags, this.FallbackLanguage ?? default);
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is FallbackValueOptions options)
            {
                return this.Equals(options);
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(FallbackValueOptions other)
        {
            return this.Flags == other.Flags &&
                this.FallbackLanguage == other.FallbackLanguage;
        }

        /// <summary>
        /// Whether or not to use the key as a fallback value.
        /// </summary>
        public bool UseKey => Flags.HasFlag(FallbackValueFlags.Key);

        /// <summary>
        /// Whether or not to use the value of the
        /// <see cref="FallbackLanguage">Fallback Language</see> as a fallback value.
        /// </summary>
        [MemberNotNullWhen(true, nameof(FallbackLanguage))]
        public bool UseFallbackLanguage => Flags.HasFlag(FallbackValueFlags.FallbackLanguage);
        /// <summary>
        /// Whether or not to use any language's value as a fallback value.
        /// </summary>
        public bool UseAnyLanguage => Flags.HasFlag(FallbackValueFlags.AnyLanguage);

        /// <summary>
        /// Returns a new instance of this where the localization key can be used
        /// as a fallback value.
        /// </summary>
        /// <returns>
        /// A clone of this with the flag <see cref="FallbackValueFlags.Key"/>.
        /// </returns>
        public FallbackValueOptions IncludeKey()
        {
            return new(this.Flags | FallbackValueFlags.Key, this.FallbackLanguage);
        }
        /// <summary>
        /// Returns a new instance of this where the localization key cannot be used
        /// as a fallback value.
        /// </summary>
        /// <returns>
        /// A clone of this without the flag <see cref="FallbackValueFlags.Key"/>.
        /// </returns>
        public FallbackValueOptions ExcludeKey()
        {
            return new(this.Flags & ~FallbackValueFlags.Key, this.FallbackLanguage);
        }

        /// <summary>
        /// Returns a new instance of this where the value of the specified
        /// fallback language can be used as a fallback value.
        /// </summary>
        /// <param name="language">The fallback language.</param>
        /// <returns>
        /// A clone of this with the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/>, and the fallback
        /// language <paramref name="language"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="language"/> is not a valid language.
        /// </exception>
        public FallbackValueOptions IncludeFallbackLanguage(Language language)
        {
            ValidateLanguage(language);
            return new(this.Flags | FallbackValueFlags.FallbackLanguage, language);
        }

        /// <summary>
        /// Returns a new instance of this without the value of a a fallback
        /// language being able to be used as a fallback value.
        /// </summary>
        /// <returns>
        /// A clone of this without the flag
        /// <see cref="FallbackValueFlags.FallbackLanguage"/> and without
        /// a fallback value.
        /// </returns>
        public FallbackValueOptions ExcludeFallbackLanguage()
        {
            return new(this.Flags & ~FallbackValueFlags.FallbackLanguage, null);
        }

        /// <summary>
        /// Returns a new instance of this where the value of any language can be
        /// used as a fallback value.
        /// </summary>
        /// <returns>
        /// A clone of this with the flag <see cref="FallbackValueFlags.AnyLanguage"/>.
        /// </returns>
        public FallbackValueOptions IncludeAnyLanguage()
        {
            return new(this.Flags | FallbackValueFlags.AnyLanguage, this.FallbackLanguage);
        }

        /// <summary>
        /// Returns a new instance of this where the value of any language cannot be
        /// used as a fallback value, unless it's a fallback language and this has the
        /// flag <see cref="FallbackValueFlags.FallbackLanguage"/>.
        /// </summary>
        /// <returns>
        /// A clone of this without the flag
        /// <see cref="FallbackValueFlags.AnyLanguage"/>.
        /// </returns>
        public FallbackValueOptions ExcludeAnyLanguage()
        {
            return new(this.Flags & ~FallbackValueFlags.AnyLanguage, this.FallbackLanguage);
        }

        /// <summary>
        /// Combines the flags/fallback language of this with the other
        /// options specified.
        /// </summary>
        /// <param name="other">The other options.</param>
        /// <returns>
        /// The combined version of <see langword="this"/> and
        /// <paramref name="other"/>.
        /// </returns>
        public FallbackValueOptions Combine(FallbackValueOptions other)
        {
            return new FallbackValueOptions(flags: Flags | other.Flags,
                fallbackLanguage: FallbackLanguage ?? other.FallbackLanguage);
        }

        /// <inheritdoc/>
        public static bool operator ==(FallbackValueOptions left, FallbackValueOptions right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FallbackValueOptions left, FallbackValueOptions right)
        {
            return !(left == right);
        }

        /// <inheritdoc cref="FallbackValueFlags.None"/>
        public static readonly FallbackValueOptions None = new();
        /// <inheritdoc cref="FallbackValueFlags.Key"/>
        public static readonly FallbackValueOptions Key = new(FallbackValueFlags.Key);

        /// <inheritdoc cref="FallbackValueFlags.FallbackLanguage"/>
        /// <param name="fallbackLanguage">The fallback language to use.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="fallbackLanguage"/> is not a valid language.
        /// </exception>
        public static FallbackValueOptions FallbackLang(Language fallbackLanguage) => new(FallbackValueFlags.FallbackLanguage, fallbackLanguage);

        /// <inheritdoc cref="FallbackValueFlags.AnyLanguage"/>
        public static readonly FallbackValueOptions AnyLang = new(FallbackValueFlags.AnyLanguage);

        /// <inheritdoc cref="FallbackValueFlags.FallbackOrAnyLanguage"/>
        /// <param name="fallbackLanguage">The fallback language to use.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="fallbackLanguage"/> is not a valid language.
        /// </exception>
        public static FallbackValueOptions FallbackOrAnyLang(Language fallbackLanguage) => new(FallbackValueFlags.FallbackOrAnyLanguage, fallbackLanguage);

        /// <inheritdoc cref="FallbackValueFlags.FallbackLanguageOrKey"/>
        /// <param name="fallbackLanguage">The fallback language to use.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="fallbackLanguage"/> is not a valid language.
        /// </exception>
        public static FallbackValueOptions FallbackLangOrKey(Language fallbackLanguage) => new(FallbackValueFlags.FallbackLanguageOrKey, fallbackLanguage);

        /// <inheritdoc cref="FallbackValueFlags.AnyLanguageOrKey"/>
        public static readonly FallbackValueOptions AnyLangOrKey = new(FallbackValueFlags.AnyLanguageOrKey);

        /// <inheritdoc cref="FallbackValueFlags.FallbackOrAnyLanguageOrKey"/>
        /// <param name="fallbackLanguage">The fallback language to use.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="fallbackLanguage"/> is not a valid language.
        /// </exception>
        public static FallbackValueOptions FallbackOrAnyLangOrKey(Language fallbackLanguage) => new(FallbackValueFlags.FallbackOrAnyLanguageOrKey, fallbackLanguage);
    }

    /// <summary>
    /// Options for generating the text data block.
    /// </summary>
    public struct TextDBOptions
    {
        private FallbackValueOptions? m_FallbackOptions;
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
        /// The fallback options to use when no value exists for a specific
        /// language. If not specified, defaults to
        /// <c><see cref="FallbackValueOptions.AnyLangOrKey"/></c>
        /// </summary>
        public FallbackValueOptions? FallbackOptions
        {
            readonly get => m_FallbackOptions;
            set => m_FallbackOptions = value;
        }
    }
}

#nullable restore
