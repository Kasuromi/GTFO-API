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
        private static JsonSerializerOptions s_DefaultSerializerSettings;

        /// <summary>
        /// Obtains the serialization options used by PersistentData
        /// </summary>
        /// <returns>The serialization options used by PersistentData</returns>
        public static JsonSerializerOptions DefaultSerializerSettings
        {
            get
            {
                if (s_DefaultSerializerSettings == null)
                {
                    s_DefaultSerializerSettings = new()
                    {
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        IncludeFields = false,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        IgnoreReadOnlyProperties = true
                    };
                    s_DefaultSerializerSettings.Converters.Add(new JsonStringEnumConverter());
                    s_DefaultSerializerSettings.Converters.Add(new Vector2Converter());
                    s_DefaultSerializerSettings.Converters.Add(new Vector3Converter());
                    s_DefaultSerializerSettings.Converters.Add(new ColorConverter());
                }

                return s_DefaultSerializerSettings;
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
        /// Parses the JSON text into an instance type of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The target type of the JSON value.</typeparam>
        /// <param name="json">The JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <returns><typeparamref name="T"/> with deserialized JSON data</returns>
        public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
        {
            if (options == null)
                options = DefaultSerializerSettings;

            return NativeSerializer.Deserialize<T>(json, options);
        }
    }
}
