namespace GitVersion
{
    using System;

    class ConsoleAdapter : IConsole
    {
        public void WriteLine(string msg)
        {
            throw new NotImplementedException();
        }

        public void WriteLine()
        {
            throw new NotImplementedException();
        }

        public void Write(string msg)
        {
            throw new NotImplementedException();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
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