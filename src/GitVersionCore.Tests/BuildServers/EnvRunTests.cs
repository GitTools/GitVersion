using System;
using System.IO;
using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class EnvRunTests : TestBase
    {
        private const string EnvVarName = "ENVRUN_DATABASE";
        private string mFilePath;

        [SetUp]
        public void SetEnvironmentVariableForTest()
        {
            // set environment variable and create an empty envrun file to indicate that EnvRun is running...
            mFilePath = Path.Combine(Path.GetTempPath(), "envrun.db");
            Environment.SetEnvironmentVariable(EnvVarName, mFilePath, EnvironmentVariableTarget.Process);
            File.OpenWrite(mFilePath).Dispose();
        }

        [TearDown]
        public void ClearEnvironmentVariableForTest()
        {
            Environment.SetEnvironmentVariable(EnvVarName, null, EnvironmentVariableTarget.Process);
            File.Delete(mFilePath);
        }

        [Test]
        public void CanApplyToCurrentContext()
        {
            EnvRun envrun = new EnvRun();
            bool applys = envrun.CanApplyToCurrentContext();
            applys.ShouldBeTrue();
        }

        [Test]
        public void CanApplyToCurrentContext_EnvironmentVariableNotSet()
        {
            Environment.SetEnvironmentVariable(EnvVarName, null, EnvironmentVariableTarget.Process);
            EnvRun envrun = new EnvRun();
            bool applys = envrun.CanApplyToCurrentContext();
            applys.ShouldBeFalse();
        }

        [TestCase("1.2.3")]
        [TestCase("1.2.3-rc4")]
        public void GenerateSetVersionMessage(string fullSemVer)
        {
            EnvRun envrun = new EnvRun();
            var vars = new TestableVersionVariables(fullSemVer: fullSemVer);
            var version = envrun.GenerateSetVersionMessage(vars);
            version.ShouldBe(fullSemVer);
        }

        [TestCase("Version", "1.2.3",     "@@envrun[set name='GitVersion_Version' value='1.2.3']")]
        [TestCase("Version", "1.2.3-rc4", "@@envrun[set name='GitVersion_Version' value='1.2.3-rc4']")]
        public void GenerateSetParameterMessage(string name, string value, string expected)
        {
            EnvRun envrun = new EnvRun();
            var output = envrun.GenerateSetParameterMessage(name, value);
            output.ShouldHaveSingleItem();
            output[0].ShouldBe(expected);
        }

    }
}
