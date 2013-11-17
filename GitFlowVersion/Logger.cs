namespace GitFlowVersion
{
    using System;

    public static class Logger
    {
        [ThreadStatic] public static Action<string> WriteInfo;

        static Logger()
        {
            Reset();
        }
        public static void Reset()
        {
            WriteInfo = s => { throw new Exception("Logger not defined."); };
        }
    }
}