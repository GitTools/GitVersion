using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
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

            void Action(string info) => s = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            log.Info($"{protocol}://{username}:{password}@workspace.visualstudio.com/DefaultCollection/_git/CAS");

            s.Contains(password).ShouldBe(false);
        }

        [Test]
        public void UsernameWithoutPassword()
        {
            var s = string.Empty;

            void Action(string info) => s = info;

            var logAppender = new TestLogAppender(Action);
            var log = new Log(logAppender);

            const string repoUrl = "http://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";

            log.Info(repoUrl);

            s.Contains(repoUrl).ShouldBe(true);
        }
    }
}
