using NUnit.Framework;
using System;
using GitVersion;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class LoggerTest
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
            using (Logger.AddLoggersTemporarily(action, action, action))
                Logger.WriteInfo(string.Format("{0}://{1}:{2}@workspace.visualstudio.com/DefaultCollection/_git/CAS",protocol,username,password));

            s.Contains(password).ShouldBe(false);
        }

        [Test]
        public void UsernameWithoutPassword()
        {
            var s = string.Empty;
            Action<string> action = info => { s = info; };
            const string repoUrl = "http://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";
            using (Logger.AddLoggersTemporarily(action, action, action))
                Logger.WriteInfo(repoUrl);

            s.Contains(repoUrl).ShouldBe(true);
        }
    }
}
