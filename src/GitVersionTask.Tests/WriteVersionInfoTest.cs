using GitVersion.MSBuildTask.Tasks;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class WriteVersionInfoTest : TestTaskBase
    {
        [Test]
        public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutput()
        {
            var task = new WriteVersionInfoToBuildLog();

            var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Log.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
        }

        [Test]
        public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputInBuildServer()
        {
            var task = new WriteVersionInfoToBuildLog();

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);

            result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
        }
    }
}
