using System;
using System.Text;
using GitVersion.Logging;

namespace GitVersion.App.Tests
{
    public class TestConsoleAdapter : IConsole
    {
        private readonly StringBuilder sb;
        public TestConsoleAdapter(StringBuilder sb)
        {
            this.sb = sb;
        }
        public void WriteLine(string msg)
        {
            this.sb.AppendLine(msg);
        }

        public void WriteLine()
        {
            this.sb.AppendLine();
        }

        public void Write(string msg)
        {
            this.sb.Append(msg);
        }

        public override string ToString()
        {
            return this.sb.ToString();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public IDisposable UseColor(ConsoleColor consoleColor)
        {
            return Disposable.Empty;
        }
    }
}
