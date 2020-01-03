using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class AzurePipelinesTests : TestBase
    {
        private readonly string key = "BUILD_BUILDNUMBER";

        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetEnvironmentVariableForTest()
        {
            environment = new TestEnvironment();
            log = new NullLog();
            environment.SetEnvironmentVariable(key, "Some Build_Value $(GitVersion_FullSemVer) 20151310.3 $(UnknownVar) Release");
        }

        [TearDown]
        public void ClearEnvironmentVariableForTest()
        {
            environment.SetEnvironmentVariable(key, null);
        }

        [Test]
        public void DevelopBranch()
        {
            var versionBuilder = new AzurePipelines(environment, log);
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Unstable4");
            var vsVersion = versionBuilder.GenerateSetVersionMessage(vars);

            vsVersion.ShouldBe("##vso[build.updatebuildnumber]Some Build_Value 0.0.0-Unstable4 20151310.3 $(UnknownVar) Release");
        }

        [Test]
        public void EscapeValues()
        {
            var versionBuilder = new AzurePipelines(environment, log);
            var vsVersion = versionBuilder.GenerateSetParameterMessage("Foo", "0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");

            vsVersion[0].ShouldBe("##vso[task.setvariable variable=GitVersion.Foo;isOutput=true]0.8.0-unstable568 Branch:'develop' Sha:'ee69bff1087ebc95c6b43aa2124bd58f5722e0cb'");
        }

        [Test]
        public void MissingEnvShouldNotBlowUp()
        {
            environment.SetEnvironmentVariable(key, null);

            var versionBuilder = new AzurePipelines(environment, log);
            var semver = "0.0.0-Unstable4";
            var vars = new TestableVersionVariables(fullSemVer: semver);
            var vsVersion = versionBuilder.GenerateSetVersionMessage(vars);
            vsVersion.ShouldBe(semver);
        }
    }
}
