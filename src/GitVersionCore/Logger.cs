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

        public static Action<string> WriteInfo { get; private set; }
        public static Action<string> WriteWarning { get; private set; }
        public static Action<string> WriteError { get; private set; }

        public static IDisposable IndentLog(string operationDescription)
        {
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

        public static void SetLoggers(Action<string> info, Action<string> warn, Action<string> error)
        {
            if (info == null) throw new ArgumentNullException("info");
            if (warn == null) throw new ArgumentNullException("warn");
            if (error == null) throw new ArgumentNullException("error");

            WriteInfo = LogMessage(ObscurePassword(info), "INFO");
            WriteWarning = LogMessage(ObscurePassword(warn), "WARN");
            WriteError = LogMessage(ObscurePassword(error), "ERROR");
        }

        public static IDisposable AddLoggersTemporarily(Action<string> info, Action<string> warn, Action<string> error)
        {
            var currentInfo = WriteInfo;
            var currentWarn = WriteWarning;
            var currentError = WriteError;
            SetLoggers(s => {
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