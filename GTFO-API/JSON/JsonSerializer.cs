using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GTFO.API.JSON.Converters;
using NativeSerializer = System.Text.Json.JsonSerializer;

namespace GTFO.API.JSON
{
    /// <summary>
    /// Wrapper class for System Json serializer
    /// </summary>
    public static class JsonSerializer
    {
        private static JsonSerializerOptions s_defaultSerializerSettings;

        /// <summary>
        /// Obtains the serialization options used by PersistentData
        /// </summary>
        /// <returns>The serialization options used by PersistentData</returns>
        public static JsonSerializerOptions DefaultSerializerSettings
        {
            get
            {
                if (s_defaultSerializerSettings == null)
                {
                    s_defaultSerializerSettings = new()
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        IncludeFields = false,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        IgnoreReadOnlyProperties = true
                    };
                    s_defaultSerializerSettings.Converters.Add(new JsonStringEnumConverter());
                    s_defaultSerializerSettings.Converters.Add(new Vector2Converter());
                    s_defaultSerializerSettings.Converters.Add(new Vector3Converter());
                    s_defaultSerializerSettings.Converters.Add(new ColorConverter());
                }

                return s_defaultSerializerSettings;
            }
        }

        /// <summary>
        /// Converts the object specified into a JSON string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <returns>A JSON string representation of the value.</returns>
        public static string Serialize(object value, JsonSerializerOptions options = null)
        {
            if (options == null)
                options = DefaultSerializerSettings;

            return NativeSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Parses the text representing a single JSON value into an instance of the type specified by a generic type parameter.
        /// </summary>
        /// <typeparam name="T">The target type of the JSON value.</typeparam>
        /// <param name="json">The JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <returns></returns>
        public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
        {
            if (options == null)
                options = DefaultSerializerSettings;

            return NativeSerializer.Deserialize<T>(json, options);
        }
    }
}
