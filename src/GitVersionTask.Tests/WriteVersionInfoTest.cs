using GitVersion.MSBuildTask.Tasks;
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

            var result = ExecuteMsBuildTask(task);

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

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);

            result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
        }
    }
}
