using System.IO;
using GitVersion.MSBuildTask.Tasks;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MSBuildTask.Tests
{
    [TestFixture]
    public class GenerateGitVersionInformationTest : TestTaskBase
    {
        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFile()
        {
            using var fixture = CreateLocalRepositoryFixture();

            var task = new GenerateGitVersionInformation
            {
                SolutionDirectory = fixture.RepositoryPath,
                ProjectFile = fixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain(@"FullSemVer = ""1.2.4+1""");
        }

        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunningInBuildServer()
        {
            using var fixture = CreateRemoteRepositoryFixture();

            var task = new GenerateGitVersionInformation
            {
                SolutionDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
                ProjectFile = fixture.LocalRepositoryFixture.RepositoryPath,
            };

            var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain(@"FullSemVer = ""1.0.1+1""");
        }
    }
}
