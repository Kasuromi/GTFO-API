using System;
using System.Collections.Generic;
using System.Text;

namespace GTFO.API.Utilities
{
    public class PersistentData<T> where T : PersistentData<T>, new()
    {
        const string versionRegex = @"""PersistentDataVersion"": ""(.+?)""";

        protected static T currentData;

        public static T CurrentData
        {
            get
            {
                if (currentData != null)
                {
                    return currentData;
                }
                else
                {
                    currentData = Load();
                    return currentData;
                }
            }
            set
            {
                currentData = value;
            }
        }

        protected static string persistentPath
        {
            get
            {
                return Path.Combine(Paths.PluginPath, typeof(T).Assembly.GetName().Name, $"{typeof(T).Name}.json");
            }
        }

        public virtual string PersistentDataVersion { get; set; } = "1.0.0";

        public static T Load()
        {
            return Load(persistentPath);
        }

        public static T Load(string path)
        {
            T res = new();

            if (File.Exists(path))
            {
                var contents = File.ReadAllText(path);
                T deserialized;

                try
                {
                    deserialized = JsonSerializer.Deserialize<T>(contents, PersistentDataSettings.GetPersistentDataSettings());
                }
                catch (JsonException)
                {
                    APILogger.Warn("JSON", $"Failed to deserialize {typeof(T).Name}, replacing with default");

                    var version = "FAILED";

                    var match = Regex.Match(contents, versionRegex);
                    if (match.Success)
                    {
                        version = $"{match.Groups[1].Value}-FAILED";
                    }

                    File.WriteAllText($"{Path.ChangeExtension(path, null)}-{version}.json", contents);
                    deserialized = new();
                    deserialized.Save(path);
                }

                if (deserialized.PersistentDataVersion != res.PersistentDataVersion)
                {
                    deserialized.Save($"{Path.ChangeExtension(path, null)}-{deserialized.PersistentDataVersion}.json");
                    res.Save(path);
                }
                else
                    res = deserialized;
            }
            else
            {
                res.Save(path);
            }

            return res;
        }

        public void Save()
        {
            Save(persistentPath);
        }

        public void Save(string path)
        {
            var contents = JsonSerializer.Serialize((T)this, PersistentDataSettings.GetPersistentDataSettings());

            if (!File.Exists(path))
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);

            File.WriteAllText(path, contents);
        }
    }

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
