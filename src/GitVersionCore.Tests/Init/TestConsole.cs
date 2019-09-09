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
            Logger.WriteInfo(msg + Environment.NewLine);
        }

        public void WriteLine()
        {
            Logger.WriteInfo(Environment.NewLine);
        }

        public void Write(string msg)
        {
            Logger.WriteInfo(msg);
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