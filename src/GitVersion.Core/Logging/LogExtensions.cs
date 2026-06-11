using GitVersion.Helpers;

namespace GitVersion.Logging;

/// <summary>Extension methods on <see cref="ILog"/> that provide level-specific logging helpers (<c>Debug</c>, <c>Info</c>, <c>Warning</c>, <c>Error</c>) and verbosity-scope methods.</summary>
public static class LogExtensions
{
    extension(ILog log)
    {
        /// <summary>Writes a debug-level message.</summary>
        public void Debug(string format, params object?[] args) => log.Write(LogLevel.Debug, format, args);
        /// <summary>Writes a debug-level message at an explicit verbosity.</summary>
        public void Debug(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Debug, format, args);
        /// <summary>Writes a debug-level message produced by a lazy <see cref="LogAction"/>.</summary>
        public void Debug(LogAction logAction) => log.Write(LogLevel.Debug, logAction);
        /// <summary>Writes a debug-level message produced by a lazy <see cref="LogAction"/> at an explicit verbosity.</summary>
        public void Debug(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Debug, logAction);
        /// <summary>Writes a warning-level message.</summary>
        public void Warning(string format, params object?[] args) => log.Write(LogLevel.Warn, format, args);
        /// <summary>Writes a warning-level message at an explicit verbosity.</summary>
        public void Warning(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Warn, format, args);
        /// <summary>Writes a warning-level message produced by a lazy <see cref="LogAction"/>.</summary>
        public void Warning(LogAction logAction) => log.Write(LogLevel.Warn, logAction);
        /// <summary>Writes a warning-level message produced by a lazy <see cref="LogAction"/> at an explicit verbosity.</summary>
        public void Warning(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Warn, logAction);
        /// <summary>Writes an informational message.</summary>
        public void Info(string format, params object?[] args) => log.Write(LogLevel.Info, format, args);
        /// <summary>Writes an informational message at an explicit verbosity.</summary>
        public void Info(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Info, format, args);
        /// <summary>Writes an informational message produced by a lazy <see cref="LogAction"/>.</summary>
        public void Info(LogAction logAction) => log.Write(LogLevel.Info, logAction);
        /// <summary>Writes an informational message produced by a lazy <see cref="LogAction"/> at an explicit verbosity.</summary>
        public void Info(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Info, logAction);
        /// <summary>Writes a verbose-level message.</summary>
        public void Verbose(string format, params object?[] args) => log.Write(LogLevel.Verbose, format, args);
        /// <summary>Writes a verbose-level message at an explicit verbosity.</summary>
        public void Verbose(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Verbose, format, args);
        /// <summary>Writes a verbose-level message produced by a lazy <see cref="LogAction"/>.</summary>
        public void Verbose(LogAction logAction) => log.Write(LogLevel.Verbose, logAction);
        /// <summary>Writes a verbose-level message produced by a lazy <see cref="LogAction"/> at an explicit verbosity.</summary>
        public void Verbose(Verbosity verbosity, LogAction logAction) => log.Write(verbosity, LogLevel.Verbose, logAction);
        /// <summary>Writes an error-level message.</summary>
        public void Error(string format, params object?[] args) => log.Write(LogLevel.Error, format, args);
        /// <summary>Writes an error-level message at an explicit verbosity.</summary>
        public void Error(Verbosity verbosity, string format, params object?[] args) => log.Write(verbosity, LogLevel.Error, format, args);
        /// <summary>Writes an error-level message produced by a lazy <see cref="LogAction"/>.</summary>
        public void Error(LogAction logAction) => log.Write(LogLevel.Error, logAction);
        /// <summary>Writes an error-level message produced by a lazy <see cref="LogAction"/> at an explicit verbosity.</summary>
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

        /// <summary>Returns a scope that temporarily sets the log verbosity to <see cref="Verbosity.Quiet"/>.</summary>
        public IDisposable QuietVerbosity() => log.WithVerbosity(Verbosity.Quiet);
        /// <summary>Returns a scope that temporarily sets the log verbosity to <see cref="Verbosity.Minimal"/>.</summary>
        public IDisposable MinimalVerbosity() => log.WithVerbosity(Verbosity.Minimal);
        /// <summary>Returns a scope that temporarily sets the log verbosity to <see cref="Verbosity.Normal"/>.</summary>
        public IDisposable NormalVerbosity() => log.WithVerbosity(Verbosity.Normal);
        /// <summary>Returns a scope that temporarily sets the log verbosity to <see cref="Verbosity.Verbose"/>.</summary>
        public IDisposable VerboseVerbosity() => log.WithVerbosity(Verbosity.Verbose);
        /// <summary>Returns a scope that temporarily sets the log verbosity to <see cref="Verbosity.Diagnostic"/>.</summary>
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
