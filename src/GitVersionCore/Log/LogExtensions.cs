using GitVersion.Helpers;

namespace GitVersion.Log
{
    public static class LogExtensions
    {
        public static void Debug(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Debug, format, args);
        }

        public static void Debug(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Debug, logAction);
        }

        public static void Warning(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Warn, format, args);
        }

        public static void Warning(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Warn, logAction);
        }

        public static void Info(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Info, format, args);
        }

        public static void Info(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Info, logAction);
        }

        public static void Error(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Error, format, args);
        }

        public static void Error(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Error, logAction);
        }

        public static void Write(this ILog log, LogLevel level, LogAction logAction)
        {
            if (log == null || logAction == null)
                return;

            void ActionEntry(string format, object[] args) => log.Write(level, format, args);
            logAction(ActionEntry);
        }
    }
}
