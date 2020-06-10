using System;

namespace GitVersion.Logging
{
    public interface IConsole
    {
        void WriteLine(string msg);
        void WriteLine();
        void Write(string msg);
        string ReadLine();
        IDisposable UseColor(ConsoleColor consoleColor);
    }
}
