using System;
using System.Collections.Generic;
using GitVersion.Logging;

namespace GitVersionCore.Tests.Helpers
{
    public class TestConsole : IConsole
    {
        private readonly Queue<string> responses;
        private readonly ILog log;

        public TestConsole(params string[] responses)
        {
            log = new NullLog();
            this.responses = new Queue<string>(responses);
        }

        public void WriteLine(string msg)
        {
            log.Info(msg + Environment.NewLine);
        }

        public void WriteLine()
        {
            log.Info(Environment.NewLine);
        }

        public void Write(string msg)
        {
            log.Info(msg);
        }

        public string ReadLine()
        {
            return responses.Dequeue();
        }

        public IDisposable UseColor(ConsoleColor consoleColor)
        {
            return new NoOpDisposable();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
