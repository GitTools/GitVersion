using System;
using GitVersion.Logging;

namespace GitVersionCore.Tests
{
    public class TestLogAppender : ILogAppender
    {
        private readonly Action<string> logAction;

        public TestLogAppender(Action<string> logAction)
        {
            this.logAction = logAction;
        }
        public void WriteTo(LogLevel level, string message)
        {
            logAction(message);
        }
    }
}
