using GitVersion.Helpers;

namespace GitVersion.Logging;

public static class LogExtensions
{
    extension(ILog log)
    {
        public void Debug(string format, params object?[] args) => log.Write(LogLevel.Debug, format, args);
        public void Debug(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Debug, format, args);
        public void Debug(LogAction logAction) => log.Write(LogLevel.Debug, logAction);
        public void Debug(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Debug, logAction);
        public void Warning(string format, params object?[] args) => log.Write(LogLevel.Warn, format, args);
        public void Warning(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Warn, format, args);
        public void Warning(LogAction logAction) => log.Write(LogLevel.Warn, logAction);
        public void Warning(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Warn, logAction);
        public void Info(string format, params object?[] args) => log.Write(LogLevel.Info, format, args);
        public void Info(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Info, format, args);
        public void Info(LogAction logAction) => log.Write(LogLevel.Info, logAction);
        public void Info(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Info, logAction);
        public void Verbose(string format, params object?[] args) => log.Write(LogLevel.Verbose, format, args);
        public void Verbose(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Verbose, format, args);
        public void Verbose(LogAction logAction) => log.Write(LogLevel.Verbose, logAction);
        public void Verbose(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Verbose, logAction);
        public void Error(string format, params object?[] args) => log.Write(LogLevel.Error, format, args);
        public void Error(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Error, format, args);
        public void Error(LogAction logAction) => log.Write(LogLevel.Error, logAction);
        public void Error(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Error, logAction);

        private void Write(LogLevel level, string format, params object?[] args)
        {
            var verbosity = GetVerbosityForLevel(level);
            if (verbosity > log.Verbosity)
            {
                return;
            }

            log.Write(verbosity, level, format, args);
        }

        private void Write(Verbosity verbosity, LogLevel level, LogAction? logAction)
        {
            if (logAction == null)
                return;

            if (verbosity > log.Verbosity)
            {
                return;
            }

            logAction(ActionEntry);
            return;

            void ActionEntry(string format, object[] args) => log.Write(verbosity, level, format, args);
        }

        private void Write(LogLevel level, LogAction? logAction)
        {
            if (logAction == null)
                return;

            var verbosity = GetVerbosityForLevel(level);
            if (verbosity > log.Verbosity)
            {
                return;
            }

            logAction(ActionEntry);
            return;

            void ActionEntry(string format, object[] args) => log.Write(verbosity, level, format, args);
        }

        public IDisposable QuietVerbosity() => log.WithVerbosity(Verbosity.Quiet);
        public IDisposable MinimalVerbosity() => log.WithVerbosity(Verbosity.Minimal);
        public IDisposable NormalVerbosity() => log.WithVerbosity(Verbosity.Normal);
        public IDisposable VerboseVerbosity() => log.WithVerbosity(Verbosity.Verbose);
        public IDisposable DiagnosticVerbosity() => log.WithVerbosity(Verbosity.Diagnostic);

        private IDisposable WithVerbosity(Verbosity verbosity)
        {
            ArgumentNullException.ThrowIfNull(log);
            var lastVerbosity = log.Verbosity;
            log.Verbosity = verbosity;
            return Disposable.Create(() => log.Verbosity = lastVerbosity);
        }
    }

    private static Verbosity GetVerbosityForLevel(LogLevel level) => VerbosityMaps[level];

    private static readonly Dictionary<LogLevel, Verbosity> VerbosityMaps = new()
    {
        { LogLevel.Verbose, Verbosity.Verbose },
        { LogLevel.Debug, Verbosity.Diagnostic },
        { LogLevel.Info, Verbosity.Normal },
        { LogLevel.Warn, Verbosity.Minimal },
        { LogLevel.Error, Verbosity.Quiet },
        { LogLevel.Fatal, Verbosity.Quiet }
    };
}
