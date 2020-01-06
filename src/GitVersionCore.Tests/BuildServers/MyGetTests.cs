using NUnit.Framework;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class MyGetTests : TestBase
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
            var versionBuilder = new MyGet(environment, log);
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
            var message = versionBuilder.GenerateSetVersionMessage(vars);
            Assert.AreEqual(null, message);
        }

        [Test]
        public void EscapeValues()
        {
            var versionBuilder = new MyGet(environment, log);
            var message = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
            Assert.AreEqual("##myget[setParameter name='GitVersion.Foo' value='0.8.0-unstable568 Branch:|'develop|' Sha:|'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb|'']", message[0]);
        }

        [Test]
        public void BuildNumber()
        {
            var versionBuilder = new MyGet(environment, log);
            var message = versionBuilder.GenerateSetParameterMessage("LegacySemVerPadded", "0.8.0-unstable568");
            Assert.AreEqual("##myget[buildNumber '0.8.0-unstable568']", message[1]);
        }
    }
}
