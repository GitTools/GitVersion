using GitVersion.Logging;

namespace GitVersion.Core.Tests.Helpers;

public class TestLogAppender(Action<string> logAction) : ILogAppender
{
    public void WriteTo(LogLevel level, string message) => logAction(message);
}
