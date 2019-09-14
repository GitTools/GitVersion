using System;
using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Helpers;

namespace GitVersionCore.Tests.Init
{
    public class TestConsole : IConsole
    {
        readonly Queue<string> responses;

        public TestConsole(params string[] responses)
        {
            this.responses = new Queue<string>(responses);
        }

        public void WriteLine(string msg)
        {
            Logger.Info(msg + Environment.NewLine);
        }

        public void WriteLine()
        {
            Logger.Info(Environment.NewLine);
        }

        public void Write(string msg)
        {
            Logger.Info(msg);
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