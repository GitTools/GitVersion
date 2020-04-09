using System.IO;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.BuildAgents
{
    [TestFixture]
    public class EnvRunTests : TestBase
    {
        private const string EnvVarName = "ENVRUN_DATABASE";
        private string mFilePath;
        private IEnvironment environment;
        private EnvRun buildServer;

        [SetUp]
        public void SetEnvironmentVariableForTest()
        {
            var sp = ConfigureServices(services =>
            {
                services.AddSingleton<EnvRun>();
            });
            environment = sp.GetService<IEnvironment>();
            buildServer = sp.GetService<EnvRun>();

            // set environment variable and create an empty envrun file to indicate that EnvRun is running...
            mFilePath = Path.Combine(Path.GetTempPath(), "envrun.db");
            environment.SetEnvironmentVariable(EnvVarName, mFilePath);
            File.OpenWrite(mFilePath).Dispose();
        }

        [TearDown]
        public void ClearEnvironmentVariableForTest()
        {
            environment.SetEnvironmentVariable(EnvVarName, null);
            File.Delete(mFilePath);
        }

        [Test]
        public void CanApplyToCurrentContext()
        {
            var applys = buildServer.CanApplyToCurrentContext();
            applys.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContextEnvironmentVariableNotSet()
        {
            environment.SetEnvironmentVariable(EnvVarName, null);
            var applys = buildServer.CanApplyToCurrentContext();
            applys.ShouldBeFalse();
        }

        [TestCase("1.2.3")]
        [TestCase("1.2.3-rc4")]
        public void GenerateSetVersionMessage(string fullSemVer)
        {
            var vars = new TestableVersionVariables(fullSemVer: fullSemVer);
            var version = buildServer.GenerateSetVersionMessage(vars);
            version.ShouldBe(fullSemVer);
        }

        [TestCase("Version", "1.2.3", "@@envrun[set name='GitVersion_Version' value='1.2.3']")]
        [TestCase("Version", "1.2.3-rc4", "@@envrun[set name='GitVersion_Version' value='1.2.3-rc4']")]
        public void GenerateSetParameterMessage(string name, string value, string expected)
        {
            var output = buildServer.GenerateSetParameterMessage(name, value);
            output.ShouldHaveSingleItem();
            output[0].ShouldBe(expected);
        }
    }
}
