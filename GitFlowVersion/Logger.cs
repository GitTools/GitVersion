namespace GitFlowVersion
{
    using System;

    public static class Logger
    {
        public static Action<string> Write = x => Console.Out.WriteLine(x);

        public static void Reset()
        {
            Write = x => Console.Out.WriteLine(x);
        }
    }
}