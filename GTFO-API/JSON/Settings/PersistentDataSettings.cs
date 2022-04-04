using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFO.API.JSON.Settings
{
    public static class PersistentDataSettings
    {
        private static JsonSerializerOptions persistentDataSettings;

        public static JsonSerializerOptions GetPersistentDataSettings()
        {
            if (persistentDataSettings == null)
            {
                persistentDataSettings = new()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    IncludeFields = false,
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    IgnoreReadOnlyProperties = true
                };
                persistentDataSettings.Converters.Add(new JsonStringEnumConverter());
            }

            return persistentDataSettings;
        }
    }
}
