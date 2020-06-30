using System;
using System.Collections.Generic;

namespace GitVersion.Logging
{
    public static class LogExtensions
    {
        public static void Debug(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Debug, format, args);
        }

        public static void Debug(this ILog log, Verbosity verbosity, string format, params object[] args)
        {
            log?.Write(verbosity, LogLevel.Debug, format, args);
        }

        public static void Debug(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Debug, logAction);
        }

        public static void Debug(this ILog log, Verbosity verbosity, LogAction logAction)
        {
            log?.Write(verbosity, LogLevel.Debug, logAction);
        }

        public static void Warning(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Warn, format, args);
        }

        public static void Warning(this ILog log, Verbosity verbosity, string format, params object[] args)
        {
            log?.Write(verbosity, LogLevel.Warn, format, args);
        }

        public static void Warning(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Warn, logAction);
        }

        public static void Warning(this ILog log, Verbosity verbosity, LogAction logAction)
        {
            log?.Write(verbosity, LogLevel.Warn, logAction);
        }

        public static void Info(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Info, format, args);
        }

        public static void Info(this ILog log, Verbosity verbosity, string format, params object[] args)
        {
            log?.Write(verbosity, LogLevel.Info, format, args);
        }

        public static void Info(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Info, logAction);
        }

        public static void Info(this ILog log, Verbosity verbosity, LogAction logAction)
        {
            log?.Write(verbosity, LogLevel.Info, logAction);
        }

        public static void Verbose(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Verbose, format, args);
        }

        public static void Verbose(this ILog log, Verbosity verbosity, string format, params object[] args)
        {
            log?.Write(verbosity, LogLevel.Verbose, format, args);
        }

        public static void Verbose(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Verbose, logAction);
        }

        public static void Verbose(this ILog log, Verbosity verbosity, LogAction logAction)
        {
            log?.Write(verbosity, LogLevel.Verbose, logAction);
        }

        public static void Error(this ILog log, string format, params object[] args)
        {
            log?.Write(LogLevel.Error, format, args);
        }

        public static void Error(this ILog log, Verbosity verbosity, string format, params object[] args)
        {
            log?.Write(verbosity, LogLevel.Error, format, args);
        }

        public static void Error(this ILog log, LogAction logAction)
        {
            log?.Write(LogLevel.Error, logAction);
        }

        public static void Error(this ILog log, Verbosity verbosity, LogAction logAction)
        {
            log?.Write(verbosity, LogLevel.Error, logAction);
        }

        public static void Write(this ILog log, LogLevel level, string format, params object[] args)
        {
            if (log == null)
                return;

            var verbosity = GetVerbosityForLevel(level);
            if (verbosity > log.Verbosity)
            {
                return;
            }

            log.Write(verbosity, level, format, args);
        }

        public static void Write(this ILog log, Verbosity verbosity, LogLevel level, LogAction logAction)
        {
            if (log == null || logAction == null)
                return;

            if (verbosity > log.Verbosity)
            {
                return;
            }

            void ActionEntry(string format, object[] args) => log.Write(verbosity, level, format, args);
            logAction(ActionEntry);
        }

        public static void Write(this ILog log, LogLevel level, LogAction logAction)
        {
            if (log == null || logAction == null)
                return;

            var verbosity = GetVerbosityForLevel(level);
            if (verbosity > log.Verbosity)
            {
                return;
            }

            void ActionEntry(string format, object[] args) => log.Write(verbosity, level, format, args);
            logAction(ActionEntry);
        }

        public static IDisposable QuietVerbosity(this ILog log)
        {
            return log.WithVerbosity(Verbosity.Quiet);
        }

        public static IDisposable MinimalVerbosity(this ILog log)
        {
            return log.WithVerbosity(Verbosity.Minimal);
        }

        public static IDisposable NormalVerbosity(this ILog log)
        {
            return log.WithVerbosity(Verbosity.Normal);
        }

        public static IDisposable VerboseVerbosity(this ILog log)
        {
            return log.WithVerbosity(Verbosity.Verbose);
        }

        public static IDisposable DiagnosticVerbosity(this ILog log)
        {
            return log.WithVerbosity(Verbosity.Diagnostic);
        }

        public static IDisposable WithVerbosity(this ILog log, Verbosity verbosity)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }
            var lastVerbosity = log.Verbosity;
            log.Verbosity = verbosity;
            return Disposable.Create(() => log.Verbosity = lastVerbosity);
        }

        public static Verbosity GetVerbosityForLevel(LogLevel level) => VerbosityMaps[level];

        private static readonly IDictionary<LogLevel, Verbosity> VerbosityMaps = new Dictionary<LogLevel, Verbosity>
        {
            { LogLevel.Verbose, Verbosity.Verbose },
            { LogLevel.Debug, Verbosity.Diagnostic },
            { LogLevel.Info, Verbosity.Normal },
            { LogLevel.Warn, Verbosity.Minimal },
            { LogLevel.Error, Verbosity.Quiet },
            { LogLevel.Fatal, Verbosity.Quiet },
        };
    }
}
