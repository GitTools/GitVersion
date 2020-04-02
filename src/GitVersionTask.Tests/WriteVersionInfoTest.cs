using System.Collections.Generic;
using System.Linq;
using GitVersion.BuildServers;
using GitVersion.MSBuildTask.Tasks;
using GitVersionTask.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class WriteVersionInfoTest : TestTaskBase
    {
        [Test]
        public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutputIfNotRunningInBuildServer()
        {
            using var fixture = CreateLocalRepositoryFixture();

            var task = new WriteVersionInfoToBuildLog
            {
                SolutionDirectory = fixture.RepositoryPath,
            };

            var msbuildFixture = new MsBuildFixture();
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Log.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
        }

        [Test]
        public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutput()
        {
            using var fixture = CreateRemoteRepositoryFixture();

            var task = new WriteVersionInfoToBuildLog
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            };

            var env = new Dictionary<string, string>
            {
                { AzurePipelines.EnvironmentVariableName, "true" }
            };

            var msbuildFixture = new MsBuildFixture();
            msbuildFixture.WithEnv(env.ToArray());
            var result = msbuildFixture.Execute(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);

            result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
        }
    }
}
