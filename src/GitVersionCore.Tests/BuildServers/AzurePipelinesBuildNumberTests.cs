using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class AzurePipelinesBuildNumberTests : TestBase
    {
        private readonly string key = "BUILD_BUILDNUMBER";
        private readonly string logPrefix = "##vso[build.updatebuildnumber]";
        private AzurePipelines versionBuilder;

        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
            log = new NullLog();
            versionBuilder = new AzurePipelines(environment, log);
        }

        [TearDown]
        public void TearDownAzurePipelinesBuildNumberTest()
        {
            environment.SetEnvironmentVariable(key, null);
        }


        [TestCase("$(GitVersion.FullSemVer)", "1.0.0", "1.0.0")]
        [TestCase("$(GITVERSION_FULLSEMVER)", "1.0.0", "1.0.0")]
        [TestCase("$(GitVersion.FullSemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
        [TestCase("$(GITVERSION_FULLSEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
        public void AzurePipelinesBuildNumberWithFullSemVer(string buildNumberFormat, string myFullSemVer, string expectedBuildNumber)
        {
            environment.SetEnvironmentVariable(key, buildNumberFormat);
            var vars = new TestableVersionVariables(fullSemVer: myFullSemVer);
            var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
            logMessage.ShouldBe(logPrefix + expectedBuildNumber);
        }


        [TestCase("$(GitVersion.SemVer)", "1.0.0", "1.0.0")]
        [TestCase("$(GITVERSION_SEMVER)", "1.0.0", "1.0.0")]
        [TestCase("$(GitVersion.SemVer)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
        [TestCase("$(GITVERSION_SEMVER)-Build.1234", "1.0.0", "1.0.0-Build.1234")]
        public void AzurePipelinesBuildNumberWithSemVer(string buildNumberFormat, string mySemVer, string expectedBuildNumber)
        {
            environment.SetEnvironmentVariable(key, buildNumberFormat);
            var vars = new TestableVersionVariables(semVer: mySemVer);
            var logMessage = versionBuilder.GenerateSetVersionMessage(vars);
            logMessage.ShouldBe(logPrefix + expectedBuildNumber);
        }

    }
}
