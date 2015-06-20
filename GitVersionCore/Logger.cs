namespace GitVersion
{
    using System;

    public static class Logger
    {
        public static Action<string> WriteInfo { get; private set; }
        public static Action<string> WriteWarning { get; private set; }
        public static Action<string> WriteError { get; private set; }

        static Logger()
        {
            Reset();
        }

        public static void SetLoggers(Action<string> info, Action<string> warn, Action<string> error)
        {
            WriteInfo = info;
            WriteWarning = warn;
            WriteError = error;
        }

        public static void Reset()
        {
            WriteInfo = s => { throw new Exception("Logger not defined."); };
            WriteWarning = s => { throw new Exception("Logger not defined."); };
            WriteError = s => { throw new Exception("Logger not defined."); };
        }
    }
}