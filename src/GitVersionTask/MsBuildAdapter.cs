using System;
using GitVersionTask.MsBuild;

namespace GitVersion.Logging
{
    internal class MsBuildAdapter : IConsole
    {
        private readonly TaskLoggingHelper taskLog;

        public MsBuildAdapter(TaskLoggingHelper taskLog)
        {
            this.taskLog = taskLog;
        }

        public void WriteLine(string msg)
        {
            Write(msg);
            WriteLine();
        }

        public void WriteLine()
        {
            taskLog.LogMessage("\n");
        }

        public void Write(string msg)
        {
            taskLog.LogMessage(msg);
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public IDisposable UseColor(ConsoleColor consoleColor)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;

            return Disposable.Create(() => Console.ForegroundColor = old);
        }
    }
}
