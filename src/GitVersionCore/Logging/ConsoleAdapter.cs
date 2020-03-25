using System;

namespace GitVersion.Logging
{
    internal class ConsoleAdapter : IConsole
    {
        public void WriteLine(string msg)
        {
            Console.WriteLine(msg);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void Write(string msg)
        {
            Console.Write(msg);
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
