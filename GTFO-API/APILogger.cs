using BepInEx.Logging;

namespace GTFO.API
{
    internal static class APILogger
    {
        private static readonly ManualLogSource _logger;

        static APILogger()
        {
            _logger = new ManualLogSource("GTFO-API");
            Logger.Sources.Add(_logger);
        }
        private static string Format(string module, object msg) => $"[{module}]: {msg}";

        public static void Info(string module, object data) => _logger.LogMessage(Format(module, data));
        public static void Verbose(string module, object data)
        {
#if DEBUG
            _logger.LogDebug(Format(module, data));
#endif
        }
        public static void Debug(string module, object data) => _logger.LogDebug(Format(module, data));
        public static void Warn(string module, object data) => _logger.LogWarning(Format(module, data));
        public static void Error(string module, object data) => _logger.LogError(Format(module, data));
    }
}
