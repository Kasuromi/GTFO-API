using System.IO;
using BepInEx;
using System.Text.RegularExpressions;
using System.Text.Json;
using JsonSerializer = GTFO.API.JSON.JsonSerializer;
using System.Reflection;
using System.Linq;
using System.Text.Json.Serialization;

namespace GTFO.API.Utilities
{
    /// <summary>
    /// Utility class used to easily store configuration and data on disk in JSON format
    /// </summary>
    /// <typeparam name="T">The data type to store on disk</typeparam>
    public class PersistentData<T> where T : PersistentData<T>, new()
    {
        private const string VERSION_REGEX = @"""PersistentDataVersion"": ""(.+?)""";

        private static T s_CurrentData;

        /// <summary>
        /// The current data instance, loaded automatically when first accessed
        /// </summary>
        public static T CurrentData
        {
            get
            {
                if (s_CurrentData != null)
                {
                    return s_CurrentData;
                }
                else
                {
                    s_CurrentData = Load();
                    return s_CurrentData;
                }
            }
            set
            {
                s_CurrentData = value;
            }
        }

        /// <summary>
        /// The default data path on disk. Relative paths will be interpreted as a pattern to match in plugins.
        /// </summary>
        protected static string persistentPath
        {
            get
            {
                return Path.Combine("PersistentData", typeof(T).Assembly.GetName().Name, $"{typeof(T).Name}.json");
            }
        }

        private static readonly string s_fullPath = GetFullPath();

        private static string GetFullPath()
        {
            string TPersistentPath = persistentPath;
            PropertyInfo TPersistentPathProperty = typeof(T).GetProperty(nameof(persistentPath), BindingFlags.Static | BindingFlags.NonPublic);

            if (TPersistentPathProperty != null)
            {
                TPersistentPath = (string)TPersistentPathProperty.GetValue(null, null);
            }

            if (Path.IsPathFullyQualified(TPersistentPath))
            {
                return TPersistentPath;
            }

            string fileName = Path.GetFileName(TPersistentPath);
            string[] files = Directory.GetFiles(Paths.PluginPath, fileName, SearchOption.AllDirectories);

            string result = files.FirstOrDefault(f => f.EndsWith(TPersistentPath));

            if (string.IsNullOrEmpty(result))
            {
                APILogger.Verbose("JSON", $"Couldn't find existing data for {typeof(T).Name}");
                return Path.Combine(Paths.PluginPath, TPersistentPath);
            }

            return result;
        }

        /// <summary>
        /// The version of the stored data
        /// </summary>
        public virtual string PersistentDataVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Set to true if a JSON exception occurred when deserializing a loaded file
        /// </summary>
        [JsonIgnore]
        public bool LoadingFailed { get; private set; }

        /// <summary>
        /// Loads the stored data from the default path and creates default if it didn't exist
        /// </summary>
        /// <returns>The stored data or default if it didn't exist</returns>
        public static T Load()
        {
            return Load(s_fullPath);
        }

        /// <summary>
        /// Loads the stored data from the specified path and creates default if it didn't exist
        /// </summary>
        /// <param name="path">The path to load from</param>
        /// <returns>The stored data or default if it didn't exist</returns>
        public static T Load(string path)
        {
            APILogger.Verbose($"JSON", $"Loading {typeof(T).Name} from {path}");

            T res = new();

            if (File.Exists(path))
            {
                string contents = File.ReadAllText(path);

                string version = "1.0.0";

                Match match = Regex.Match(contents, VERSION_REGEX);
                if (match.Success)
                {
                    version = $"{match.Groups[1].Value}";
                }

                if (version != res.PersistentDataVersion)
                {
                    APILogger.Warn("JSON", $"{typeof(T).Name} PersistentDataVersion mismatch: expected {res.PersistentDataVersion}, got {version}");

                    File.WriteAllText($"{Path.ChangeExtension(path, null)}-{version}.json", contents);
                    res.Save(path);
                    return res;
                }

                T deserialized;

                try
                {
                    deserialized = JsonSerializer.Deserialize<T>(contents);
                }
                catch (JsonException exception)
                {
                    APILogger.Error("JSON", $"Failed to deserialize {typeof(T).Name}\n{exception}");

                    res.LoadingFailed = true;
                    return res;
                }

                res = deserialized;
            }
            else
            {
                res.Save(path);
            }

            return res;
        }

        /// <summary>
        /// Saves this data to the default path
        /// </summary>
        public void Save()
        {
            Save(s_fullPath);
        }

        /// <summary>
        /// Saves this data to the specified path
        /// </summary>
        /// <param name="path">The path to save to</param>
        public void Save(string path)
        {
            string contents = JsonSerializer.Serialize((T)this);
            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, contents);
        }
    }
}
