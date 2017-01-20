namespace GitVersion
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public static class Logger
    {
        static readonly Regex ObscurePasswordRegex = new Regex("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
        static string indent = string.Empty;

        static Logger()
        {
            Reset();
        }

        public static bool IsDebugEnabled;
        public static bool IsInfoEnabled;
        public static bool IsWarningEnabled;
        public static bool IsErrorEnabled;

        public static Action<string> WriteDebug { get; private set; }
        public static Action<string> WriteInfo { get; private set; }
        public static Action<string> WriteWarning { get; private set; }
        public static Action<string> WriteError { get; private set; }

        public static IDisposable IndentLog(string operationDescription)
        {
            if (!IsInfoEnabled) return new ActionDisposable(() => { });
            var start = DateTime.Now;
            WriteInfo("Begin: " + operationDescription);
            indent = indent + "  ";
            return new ActionDisposable(() =>
            {
                indent = indent.Substring(0, indent.Length - 2);
                WriteInfo(string.Format(CultureInfo.InvariantCulture, "End: {0} (Took: {1:N}ms)", operationDescription, DateTime.Now.Subtract(start).TotalMilliseconds));
            });
        }

        static Action<string> ObscurePassword(Action<string> info)
        {
            Action<string> logAction = s =>
            {
                s = ObscurePasswordRegex.Replace(s, "$1$2:*******@");
                info(s);
            };
            return logAction;
        }

        public static void SetLoggers(VerbosityLevel verbosity, Action<string> debug, Action<string> info, Action<string> warn, Action<string> error)
        {
            if (verbosity >= VerbosityLevel.Debug)
            {
                if (debug == null) throw new ArgumentNullException("debug");
                IsDebugEnabled = true;
                WriteDebug = LogMessage(ObscurePassword(debug), "DEBUG");
            }
            else {
                WriteDebug = (m) => { };
            }
            if (verbosity >= VerbosityLevel.Info)
            {
                if (info == null) throw new ArgumentNullException("Info");
                IsInfoEnabled = true;
                WriteInfo = LogMessage(ObscurePassword(info), "INFO");
            }
            else
            {
                WriteInfo = (m) => { };
            }
            if (verbosity >= VerbosityLevel.Warn)
            {
                if (warn == null) throw new ArgumentNullException("Warn");
                IsWarningEnabled = true;
                WriteWarning = LogMessage(ObscurePassword(warn), "WARN");
            }
            else
            {
                WriteWarning = (m) => { };
            }
            if (verbosity >= VerbosityLevel.Error)
            {
                if (error == null) throw new ArgumentNullException("Error");
                IsErrorEnabled = true;
                WriteError = LogMessage(ObscurePassword(error), "ERROR");
            }
            else
            {
                WriteError = (m) => { };
            }
        }

        public static IDisposable AddLoggersTemporarily(Action<string> debug, Action<string> info, Action<string> warn, Action<string> error)
        {
            var currentDebug = WriteDebug;
            var currentInfo = WriteInfo;
            var currentWarn = WriteWarning;
            var currentError = WriteError;
            SetLoggers(VerbosityLevel.Debug, s =>
            {
                debug(s);
                currentDebug(s);
            }, s =>
            {
                info(s);
                currentInfo(s);
            }, s =>
            {
                warn(s);
                currentWarn(s);
            }, s =>
            {
                error(s);
                currentError(s);
            });

            return new ActionDisposable(() =>
            {
                WriteDebug = currentDebug;
                WriteInfo = currentInfo;
                WriteWarning =  currentWarn;
                WriteError = currentError;
            });
        }

        static Action<string> LogMessage(Action<string> logAction, string level)
        {
            return s => logAction(string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, s));
        }

        public static void Reset()
        {
            WriteDebug = s => { throw new Exception("Debug logger not defined. Attempted to log: " + s); };
            WriteInfo = s => { throw new Exception("Info logger not defined. Attempted to log: " + s); };
            WriteWarning = s => { throw new Exception("Warning logger not defined. Attempted to log: " + s); };
            WriteError = s => { throw new Exception("Error logger not defined. Attempted to log: " + s); };
        }

        class ActionDisposable : IDisposable
        {
            readonly Action action;

            public ActionDisposable(Action action)
            {
                this.action = action;
            }

            public void Dispose()
            {
                action();
            }
        }
    }
}