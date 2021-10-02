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
        private static string Format(object msg) => msg.ToString();

        public static void Info(object data) => _logger.LogMessage(Format(data));
        public static void Verbose(object data)
        {
#if DEBUG
            _logger.LogDebug(Format(data));
#endif
        }
        public static void Debug(object data) => _logger.LogDebug(Format(data));
        public static void Error(object data) => _logger.LogError(Format(data));
    }
}
