namespace GitVersion
{
    using System;

    public static class Logger
    {
        static string indent = string.Empty;

        public static Action<string> WriteInfo { get; private set; }
        public static Action<string> WriteWarning { get; private set; }
        public static Action<string> WriteError { get; private set; }

        static Logger()
        {
            Reset();
        }

        public static IDisposable IndentLog(string operationDescription)
        {
            indent = indent + "  ";
            WriteInfo("Begin: " + operationDescription);
            return new ActionDisposable(() =>
            {
                indent = indent.Substring(0, indent.Length - 2);
                WriteInfo("End: " + operationDescription);
            });
        }

        public static void SetLoggers(Action<string> info, Action<string> warn, Action<string> error)
        {
            WriteInfo = LogMessage(info, "INFO");
            WriteWarning = LogMessage(warn, "WARN");
            WriteError = LogMessage(error, "ERROR");
        }

        static Action<string> LogMessage(Action<string> logAction, string level)
        {
            return s => logAction(string.Format("{0}{1} [{2:MM/dd/yy H:mm:ss:ff}] {3}", indent, level, DateTime.Now, s));
        }

        public static void Reset()
        {
            WriteInfo = s => { throw new Exception("Logger not defined."); };
            WriteWarning = s => { throw new Exception("Logger not defined."); };
            WriteError = s => { throw new Exception("Logger not defined."); };
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