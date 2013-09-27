namespace GitFlowVersion
{
    using System;

    public static class Logger
    {
        public static Action<string> WriteInfo;

        static Logger()
        {
            Reset();
        }
        public static void Reset()
        {
            WriteInfo = x => Console.Out.WriteLine(x);
        }
    }
}