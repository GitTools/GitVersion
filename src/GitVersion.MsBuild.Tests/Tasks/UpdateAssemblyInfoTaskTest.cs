using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MsBuild.Tests.Tasks;

[TestFixture]
public class UpdateAssemblyInfoTaskTest : TestTaskBase
{
    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFile()
    {
        var task = new UpdateAssemblyInfo();

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileInBuildServer()
    {
        var task = new UpdateAssemblyInfo();

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuild()
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

        using var result = ExecuteMsBuildExe(project => AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), "AssemblyInfo.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildInBuildServer()
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

        using var result = ExecuteMsBuildExeInAzurePipeline(project => AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), "AssemblyInfo.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExist()
    {
        var task = new UpdateAssemblyInfo { IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExistInBuildServer()
    {
        var task = new UpdateAssemblyInfo { IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExist()
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        using var result = ExecuteMsBuildExe(project => AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, Path.Combine("$(MSBuildProjectDirectory)", randDir)));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, "AssemblyInfo.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
    }

    [Test]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExistInBuildServer()
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        using var result = ExecuteMsBuildExeInAzurePipeline(project => AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, Path.Combine("$(MSBuildProjectDirectory)", randDir)));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, "AssemblyInfo.g.cs");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
    }

    private static void AddUpdateAssemblyInfoTask(ProjectCreator project, string targetToRun, string taskName, string outputProperty, string intermediateOutputPath = "$(MSBuildProjectDirectory)")
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
                { "Language", "$(Language)" },
                { "CompileFiles", "@(Compile)" }
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
