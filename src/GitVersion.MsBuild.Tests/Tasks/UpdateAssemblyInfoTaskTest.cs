using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace GitVersion.MsBuild.Tests.Tasks;

[TestFixture]
public class UpdateAssemblyInfoTaskTest : TestTaskBase
{
    private static readonly object[] Languages =
    [
        new object[] { "C#" },
        new object[] { "F#" },
        new object[] { "VB" }
    ];

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFile(string language)
    {
        var extension = FileHelper.GetFileExtension(language);
        var task = new UpdateAssemblyInfo { Language = language };

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();
        result.Task.AssemblyInfoTempFilePath.ShouldMatch($@"AssemblyInfo.*\.g\.{extension}");

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.2.4.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileInBuildServer(string language)
    {
        var extension = FileHelper.GetFileExtension(language);
        var task = new UpdateAssemblyInfo { Language = language };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();
        result.Task.AssemblyInfoTempFilePath.ShouldMatch($@"AssemblyInfo.*\.g\.{extension}");

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.0.1.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuild(string language)
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

        var extension = FileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExe(project =>
            AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, language), language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), $"AssemblyInfo.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.2.4.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildInBuildServer(string language)
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

        var extension = FileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExeInAzurePipeline(project =>
            AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, language), language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), $"AssemblyInfo.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.0.1.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExist(string language)
    {
        var extension = FileHelper.GetFileExtension(language);
        var task = new UpdateAssemblyInfo { Language = language, IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();
        result.Task.AssemblyInfoTempFilePath.ShouldMatch($@"AssemblyInfo.*\.g\.{extension}");

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.2.4.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExistInBuildServer(string language)
    {
        var extension = FileHelper.GetFileExtension(language);
        var task = new UpdateAssemblyInfo { Language = language, IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();
        result.Task.AssemblyInfoTempFilePath.ShouldMatch($@"AssemblyInfo.*\.g\.{extension}");

        var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.0.1.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExist(string language)
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = FileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExe(project =>
        {
            var intermediateOutputPath = Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath);
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, $"AssemblyInfo.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.2.4.0"")");
    }

    [TestCaseSource(nameof(Languages))]
    public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExistInBuildServer(string language)
    {
        const string taskName = nameof(UpdateAssemblyInfo);
        const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = FileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExeInAzurePipeline(project =>
        {
            var intermediateOutputPath = Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath);
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = PathHelper.Combine(Path.GetDirectoryName(result.ProjectPath), randDir, $"AssemblyInfo.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = File.ReadAllText(generatedFilePath);
        fileContent.ShouldContain(@"assembly: AssemblyVersion(""1.0.1.0"")");
    }

    private static void AddUpdateAssemblyInfoTask(ProjectCreator project, string targetToRun, string taskName,
                                                  string outputProperty, string language,
                                                  string intermediateOutputPath = "$(MSBuildProjectDirectory)")
    {
        var assemblyFileLocation = typeof(GitVersionTaskBase).Assembly.Location;
        project.UsingTaskAssemblyFile(taskName, assemblyFileLocation)
            .Property("ManagePackageVersionsCentrally", "false")
            .Property("GenerateAssemblyInfo", "false")
            .Property("Language", language)
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
