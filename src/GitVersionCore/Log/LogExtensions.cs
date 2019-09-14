using GitVersion.Helpers;

namespace GitVersion.Log
{
    public static class LogExtensions
    {
        public static void Debug(this ILog log, string format, params object[] args)
        {
            log?.Write(VerbosityLevel.Debug, format, args);
        }

        public static void Debug(this ILog log, LogAction logAction)
        {
            log?.Write(VerbosityLevel.Debug, logAction);
        }

        public static void Warning(this ILog log, string format, params object[] args)
        {
            log?.Write(VerbosityLevel.Warn, format, args);
        }

        public static void Warning(this ILog log, LogAction logAction)
        {
            log?.Write(VerbosityLevel.Warn, logAction);
        }

        public static void Info(this ILog log, string format, params object[] args)
        {
            log?.Write(VerbosityLevel.Info, format, args);
        }

        public static void Info(this ILog log, LogAction logAction)
        {
            log?.Write(VerbosityLevel.Info, logAction);
        }

        public static void Error(this ILog log, string format, params object[] args)
        {
            log?.Write(VerbosityLevel.Error, format, args);
        }

        public static void Error(this ILog log, LogAction logAction)
        {
            log?.Write(VerbosityLevel.Error, logAction);
        }

        public static void Write(this ILog log, VerbosityLevel level, LogAction logAction)
        {
            if (log == null || logAction == null)
                return;

            void ActionEntry(string format, object[] args) => log.Write(level, format, args);
            logAction(ActionEntry);
        }
    }
}
