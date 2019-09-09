using System;

namespace GitVersion.Configuration
{
    class ConsoleAdapter : IConsole
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

            return new DelegateDisposable(() => Console.ForegroundColor = old);
        }

        class DelegateDisposable : IDisposable
        {
            readonly Action dispose;

            public DelegateDisposable(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                dispose();
            }
        }
    }
}