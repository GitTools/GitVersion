using System.IO;
using GitVersion.MSBuildTask.Tasks;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class UpdateAssemblyInfoTaskTest : TestTaskBase
    {
        [Test]
        public void UpdateAssemblyInfoTaskShouldCreateFile()
        {
            using var fixture = CreateLocalRepositoryFixture();

            var task = new UpdateAssemblyInfo
            {
                SolutionDirectory = fixture.RepositoryPath,
                ProjectFile = fixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
        }

        [Test]
        public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunningInBuildServer()
        {
            using var fixture = CreateRemoteRepositoryFixture();

            var task = new UpdateAssemblyInfo
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
                ProjectFile = fixture.LocalRepositoryFixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
        }
    }
}
