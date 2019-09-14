using NUnit.Framework;
using Shouldly;
using GitVersion.Helpers;

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

            using (Logger.AddLoggersTemporarily(Action, Action, Action, Action))
                Logger.Info($"{protocol}://{username}:{password}@workspace.visualstudio.com/DefaultCollection/_git/CAS");

            s.Contains(password).ShouldBe(false);
        }

        [Test]
        public void UsernameWithoutPassword()
        {
            var s = string.Empty;
            void Action(string info) => s = info;
            const string repoUrl = "http://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";
            using (Logger.AddLoggersTemporarily(Action, Action, Action, Action))
                Logger.Info(repoUrl);

            s.Contains(repoUrl).ShouldBe(true);
        }
    }
}
