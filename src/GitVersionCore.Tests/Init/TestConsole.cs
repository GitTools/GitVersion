using System;
using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Logging;

namespace GitVersionCore.Tests.Init
{
    public class TestConsole : IConsole
    {
        readonly Queue<string> responses;
        private ILog log;

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

        class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
