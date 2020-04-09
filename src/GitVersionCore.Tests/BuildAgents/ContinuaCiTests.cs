using System;
using GitVersion.BuildAgents;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitVersionCore.Tests.BuildAgents
{
    [TestFixture]
    public class ContinuaCiTests : TestBase
    {
        private IServiceProvider sp;

        [SetUp]
        public void SetUp()
        {
            sp = ConfigureServices(services =>
            {
                services.AddSingleton<ContinuaCi>();
            });
        }

        [Test]
        public void GenerateBuildVersion()
        {
            var buildServer = sp.GetService<ContinuaCi>();
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
            var continuaCiVersion = buildServer.GenerateSetVersionMessage(vars);
            Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
        }
    }
}
