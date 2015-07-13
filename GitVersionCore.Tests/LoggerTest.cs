using NUnit.Framework;
using System;
using GitVersion;
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
            Logger.SetLoggers(action, action, action);

            Logger.WriteInfo(string.Format("{0}://{1}:{2}@workspace.visualstudio.com/DefaultCollection/_git/CAS",protocol,username,password));

            Assert.IsFalse(s.Contains(password));
        }

        [Test]
        public void UsernameWithoutPassword()
        {
            var s = string.Empty;
            Action<string> action = info => { s = info; };
            Logger.SetLoggers(action, action, action);

            const string repoUrl = "http://username@workspace.visualstudio.com/DefaultCollection/_git/CAS";
            Logger.WriteInfo(repoUrl);

            Assert.IsTrue(s.Contains(repoUrl));
        }

    }
}
