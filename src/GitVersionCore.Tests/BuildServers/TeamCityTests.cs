using NUnit.Framework;
using GitVersion.BuildServers;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class TeamCityTests : TestBase
    {
        [Test]
        public void Develop_branch()
        {
            var versionBuilder = new TeamCity();
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
            var tcVersion = versionBuilder.GenerateSetVersionMessage(vars);
            Assert.AreEqual("##teamcity[buildNumber '0.0.0-Unstable4']", tcVersion);
        }

        [Test]
        public void EscapeValues()
        {
            var versionBuilder = new TeamCity();
            var tcVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
            Assert.AreEqual("##teamcity[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[0]);
            Assert.AreEqual("##teamcity[setParameter name='system.GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", tcVersion[1]);
        }

    }
}