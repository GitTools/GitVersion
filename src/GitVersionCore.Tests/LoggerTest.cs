using NUnit.Framework;
using System;
using GitVersion;
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
            Action<string> action = info => { s = info; };
            using (Logger.AddLoggersTemporarily(action, action, action, action))
                Logger.WriteInfo($"{protocol}://{username}:{password}@workspace.visualstudio.com/DefaultCollection/_git/CAS");

            s.Contains(password).ShouldBe(false);
        }

        [Test]
        public void UsernameWithoutPassword()
        {
            var s = string.Empty;
            Action<string> action = info => { s = info; };
            const string repoUrl = "http://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";
            using (Logger.AddLoggersTemporarily(action, action, action, action))
                Logger.WriteInfo(repoUrl);

            s.Contains(repoUrl).ShouldBe(true);
        }
    }
}
