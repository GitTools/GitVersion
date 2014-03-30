namespace GitVersion
{
    using System;

    public static class Logger
    {
        public static Action<string> WriteInfo;
        public static Action<string> WriteWarning;
        public static Action<string> WriteError;

        static Logger()
        {
            Reset();
        }

        public static void Reset()
        {
            WriteInfo = s => { throw new Exception("Logger not defined."); };
            WriteWarning = s => { throw new Exception("Logger not defined."); };
            WriteError = s => { throw new Exception("Logger not defined."); };
        }
    }
}