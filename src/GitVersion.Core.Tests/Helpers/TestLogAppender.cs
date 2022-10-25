using GitVersion.Logging;

namespace GitVersion.Core.Tests.Helpers;

public class TestLogAppender : ILogAppender
{
    private readonly Action<string> logAction;

    public TestLogAppender(Action<string> logAction) => this.logAction = logAction;
    public void WriteTo(LogLevel level, string message) => this.logAction(message);
}
