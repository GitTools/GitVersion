using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GitVersion.Helpers
{
    public static class Logger
    {
        private static readonly Regex ObscurePasswordRegex = new Regex("(https?://)(.+)(:.+@)", RegexOptions.Compiled);
        private static string indent = string.Empty;

        static Logger()
        {
            Reset();
        }

        public static Action<string> Debug { get; private set; }
        public static Action<string> Info { get; private set; }
        public static Action<string> Warning { get; private set; }
        public static Action<string> Error { get; private set; }

        public static IDisposable IndentLog(string operationDescription)
        {
            var start = DateTime.Now;
            Info("Begin: " + operationDescription);
            indent += "  ";
            return new ActionDisposable(() =>
            {
                var length = indent.Length - 2;
                indent = length > 0 ? indent.Substring(0, length) : indent;
                Info(string.Format(CultureInfo.InvariantCulture, "End: {0} (Took: {1:N}ms)", operationDescription, DateTime.Now.Subtract(start).TotalMilliseconds));
            });
        }

        public static void SetLoggers(Action<string> debug, Action<string> info, Action<string> warn, Action<string> error)
        {
            if (debug == null) throw new ArgumentNullException(nameof(debug));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (warn == null) throw new ArgumentNullException(nameof(warn));
            if (error == null) throw new ArgumentNullException(nameof(error));

            Debug = LogMessage(ObscurePassword(debug), "DEBUG");
            Info = LogMessage(ObscurePassword(info), "INFO");
            Warning = LogMessage(ObscurePassword(warn), "WARN");
            Error = LogMessage(ObscurePassword(error), "ERROR");
        }

        public static IDisposable AddLoggersTemporarily(Action<string> debug, Action<string> info, Action<string> warn, Action<string> error)
        {
            var currentDebug = Debug;
            var currentInfo = Info;
            var currentWarn = Warning;
            var currentError = Error;
            SetLoggers(s =>
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
                Debug = currentDebug;
                Info = currentInfo;
                Warning =  currentWarn;
                Error = currentError;
            });
        }

        private static Action<string> ObscurePassword(Action<string> info)
        {
            void LogAction(string s)
            {
                s = ObscurePasswordRegex.Replace(s, "$1$2:*******@");
                info(s);
            }

            return LogAction;
        }

        private static Action<string> LogMessage(Action<string> logAction, string level)
        {
            return s => logAction(string.Format(CultureInfo.InvariantCulture, "{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, s));
        }

        private static void Reset()
        {
            Debug = s => throw new Exception("Debug logger not defined. Attempted to log: " + s);
            Info = s => throw new Exception("Info logger not defined. Attempted to log: " + s);
            Warning = s => throw new Exception("Warning logger not defined. Attempted to log: " + s);
            Error = s => throw new Exception("Error logger not defined. Attempted to log: " + s);
        }

        private class ActionDisposable : IDisposable
        {
            private readonly Action action;

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
