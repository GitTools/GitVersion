using System;
using System.Text;

namespace GitVersion.Logging
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
            sb.AppendLine(msg);
        }

        public void WriteLine()
        {
            sb.AppendLine();
        }

        public void Write(string msg)
        {
            sb.Append(msg);
        }

        public override string ToString()
        {
            return sb.ToString();
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
