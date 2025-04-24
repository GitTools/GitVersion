using GitVersion.Core.Tests.Helpers;
using GitVersion.Logging;

namespace GitVersion.Core.Tests;

[TestFixture]
public class LoggerTest : TestBase
{
    [Test]
    [TestCase("http")]
    [TestCase("https")]
    public void LoggerObscuresPassword(string protocol)
    {
        const string username = "username%40domain.com";
        const string password = "password";
        var s = string.Empty;

        var logAppender = new TestLogAppender(Action);
        var log = new Log(logAppender);

        log.Info($"{protocol}://{username}:{password}@workspace.visualstudio.com/DefaultCollection/_git/CAS");

        s.Contains(password).ShouldBe(false);
        return;

        void Action(string info) => s = info;
    }

    [Test]
    public void UsernameWithoutPassword()
    {
        var s = string.Empty;

        var logAppender = new TestLogAppender(Action);
        var log = new Log(logAppender);

        const string repoUrl = "https://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";

        log.Info(repoUrl);

        s.Contains(repoUrl).ShouldBe(true);
        return;

        void Action(string info) => s = info;
    }
}
