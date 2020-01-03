using NUnit.Framework;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class ContinuaCiTests : TestBase
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
        public void GenerateBuildVersion()
        {
            var versionBuilder = new ContinuaCi(environment, log);
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
            var continuaCiVersion = versionBuilder.GenerateSetVersionMessage(vars);
            Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
        }

    }
}
