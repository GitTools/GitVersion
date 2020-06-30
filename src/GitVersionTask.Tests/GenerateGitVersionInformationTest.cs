using System.Collections.Generic;
using System.IO;
using GitVersion.MSBuildTask;
using GitVersion.MSBuildTask.Tasks;
using GitVersion.OutputVariables;
using GitVersionTask.Tests.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
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

        [Test]
        [Category(NoMono)]
        public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuild()
        {
            const string taskName = nameof(GenerateGitVersionInformation);
            const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);

            using var result = ExecuteMsBuildExe(project =>
            {
                AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();

            var generatedFilePath = Path.Combine(Path.GetDirectoryName(result.ProjectPath), "GitVersionInformation.g.cs");
            result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

            var fileContent = File.ReadAllText(generatedFilePath);
            fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""2""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""4""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.2.4""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.2.4+1""");
        }

        [Test]
        [Category(NoMono)]
        public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildInBuildServer()
        {
            const string taskName = nameof(GenerateGitVersionInformation);
            const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);

            using var result = ExecuteMsBuildExeInBuildServer(project =>
            {
                AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();

            var generatedFilePath = Path.Combine(Path.GetDirectoryName(result.ProjectPath), "GitVersionInformation.g.cs");
            result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

            var fileContent = File.ReadAllText(generatedFilePath);
            fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""0""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.0.1""");
            fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.0.1+1""");
        }

        private static void AddGenerateGitVersionInformationTask(ProjectCreator project, string targetToRun, string taskName, string outputProperty)
        {
            var assemblyFileLocation = typeof(GitVersionTaskBase).Assembly.Location;
            project.UsingTaskAssemblyFile(taskName, assemblyFileLocation)
                .Property("GenerateAssemblyInfo", "false")
                .Target(targetToRun, beforeTargets: "CoreCompile;GetAssemblyVersion;GenerateNuspec")
                .Task(taskName, parameters: new Dictionary<string, string>
                {
                    { "SolutionDirectory", "$(MSBuildProjectDirectory)" },
                    { "NoFetch", "false" },
                    { "NoNormalize", "false" },
                    { "ProjectFile", "$(MSBuildProjectFullPath)" },
                    { "IntermediateOutputPath", "$(MSBuildProjectDirectory)" },
                    { "Language", "$(Language)" },
                })
                    .TaskOutputProperty(outputProperty, outputProperty)
                .ItemGroup()
                    .ItemCompile($"$({outputProperty})")
                    .ItemInclude("FileWrites", $"$({outputProperty})")
                    .ItemInclude("_GeneratedCodeFiles", $"$({outputProperty})")
                .Target(MsBuildExeFixture.OutputTarget, dependsOnTargets: targetToRun)
                    .TaskMessage($"{outputProperty}: $({outputProperty})", MessageImportance.High);
        }
    }
}
