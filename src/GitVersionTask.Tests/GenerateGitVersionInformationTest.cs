using System.IO;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using NUnit.Framework;
using Shouldly;

namespace GitVersionTask.Tests
{
    [TestFixture]
    public class GenerateGitVersionInformationTest : TestTaskBase
    {
        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFile()
        {
            var task = new GenerateGitVersionInformation();

            using var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""2""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""4""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.2.4""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.2.4+1""");
        }

        [Test]
        public void GenerateGitVersionInformationTaskShouldCreateFileInBuildServer()
        {
            var task = new GenerateGitVersionInformation();

            using var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.GitVersionInformationFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.GitVersionInformationFilePath);
            fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""0""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.0.1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.0.1+1""");
        }
    }
}
