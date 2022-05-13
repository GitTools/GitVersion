using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Helpers;
using GitVersion.OutputVariables;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MsBuild.Tests.Tasks;

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

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

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
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuild()
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);

        using var result = ExecuteMsBuildExe(project => AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), "GitVersionInformation.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""2""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""4""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.2.4""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.2.4+1""");
    }

    [Test]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildInBuildServer()
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);

        using var result = ExecuteMsBuildExeInAzurePipeline(project => AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), "GitVersionInformation.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""0""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.0.1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.0.1+1""");
    }

    [Test]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExist()
    {
        var task = new GenerateGitVersionInformation { IntermediateOutputPath = Guid.NewGuid().ToString("N") };

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
    public void GenerateGitVersionInformationTaskShouldCreateFileInBuildServerWhenIntermediateOutputPathDoesNotExist()
    {
        var task = new GenerateGitVersionInformation { IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

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
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExist()
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        using var result = ExecuteMsBuildExe(project => AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, Path.Combine("$(MSBuildProjectDirectory)", randDir)));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, "GitVersionInformation.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""2""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""4""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.2.4""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.2.4+1""");
    }

    [Test]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExistInBuildServer()
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        using var result = ExecuteMsBuildExeInAzurePipeline(project => AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, Path.Combine("$(MSBuildProjectDirectory)", randDir)));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, "GitVersionInformation.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain($@"{nameof(VersionVariables.Major)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Minor)} = ""0""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.Patch)} = ""1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.MajorMinorPatch)} = ""1.0.1""");
        fileContent.ShouldContain($@"{nameof(VersionVariables.FullSemVer)} = ""1.0.1+1""");
    }

    private static void AddGenerateGitVersionInformationTask(ProjectCreator project, string targetToRun, string taskName, string outputProperty, string intermediateOutputPath = "$(MSBuildProjectDirectory)")
    {
        var assemblyFileLocation = typeof(GitVersionTaskBase).Assembly.Location;
        project.UsingTaskAssemblyFile(taskName, assemblyFileLocation)
            .Property("GenerateAssemblyInfo", "false")
            .Target(targetToRun, beforeTargets: "CoreCompile;GetAssemblyVersion;GenerateNuspec")
            .Task(taskName, parameters: new Dictionary<string, string?>
            {
                { "SolutionDirectory", "$(MSBuildProjectDirectory)" },
                { "VersionFile", "$(MSBuildProjectDirectory)/gitversion.json" },
                { "ProjectFile", "$(MSBuildProjectFullPath)" },
                { "IntermediateOutputPath", intermediateOutputPath },
                { "Language", "$(Language)" }
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
