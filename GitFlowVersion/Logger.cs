namespace GitFlowVersion
{
    using System;

    public static class Logger
    {
        [ThreadStatic] public static Action<string> WriteInfo;
        [ThreadStatic] public static Action<string> WriteWarning;

        static Logger()
        {
            Reset();
        }
        public static void Reset()
        {
            WriteInfo = s => { throw new Exception("Logger not defined."); };
            WriteWarning = s => { throw new Exception("Logger not defined."); };
        }
    }
}