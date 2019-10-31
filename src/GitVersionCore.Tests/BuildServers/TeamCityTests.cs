using NUnit.Framework;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class TeamCityTests : TestBase
    {
        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
            log = new NullLog();
        }
        
        [Test]
        public void DevelopBranch()
        {
            var versionBuilder = new TeamCity(environment, log);
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
            var tcVersion = versionBuilder.GenerateSetVersionMessage(vars);
            Assert.AreEqual("##teamcity[buildNumber '0.0.0-Unstable4']", tcVersion);
        }

        [Test]
        public void EscapeValues()
        {
            var versionBuilder = new TeamCity(environment, log);
            var tcVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
            Assert.AreEqual("##teamcity[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[0]);
            Assert.AreEqual("##teamcity[setParameter name='system.GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[1]);
        }

    }
}
