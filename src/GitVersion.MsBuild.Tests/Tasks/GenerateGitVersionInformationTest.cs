using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Helpers;
using GitVersion.OutputVariables;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace GitVersion.MsBuild.Tests.Tasks;

[TestFixture]
public class GenerateGitVersionInformationTest : TestTaskBase
{
    private const string regexPattern = """.*{0}.*=.*"{1}".*""";
    private static readonly object[] Languages =
    [
        new object[] { "C#" },
        new object[] { "F#" },
        new object[] { "VB" }
    ];

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFile(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation { Language = language };

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileInBuildServer(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation { Language = language };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "0"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.0.1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.0.1-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuild(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);

        using var result = ExecuteMsBuildExe(project =>
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language), language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildInBuildServer(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);

        using var result = ExecuteMsBuildExeInAzurePipeline(project =>
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language), language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "0"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.0.1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.0.1-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenIntermediateOutputPathDoesNotExist(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation { Language = language, IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
        FileSystemHelper.Directory.DeleteDirectory(task.IntermediateOutputPath);
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileInBuildServerWhenIntermediateOutputPathDoesNotExist(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation { Language = language, IntermediateOutputPath = Guid.NewGuid().ToString("N") };

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "0"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.0.1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.0.1-1"));
        FileSystemHelper.Directory.DeleteDirectory(task.IntermediateOutputPath);
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExist(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExe(project =>
        {
            var intermediateOutputPath = FileSystemHelper.Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath);
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), randDir, $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndIntermediateOutputPathDoesNotExistInBuildServer(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExeInAzurePipeline(project =>
        {
            var intermediateOutputPath = FileSystemHelper.Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath);
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), randDir, $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "0"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.0.1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.0.1-1"));
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndUseProjectNamespaceIsSpecifiedAndRootNamespaceIsSet(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExe(project =>
        {
            var intermediateOutputPath = FileSystemHelper.Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath).Property("UseProjectNamespaceForGitVersionInformation", "True").Property("RootNamespace", "Test.Root");
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), randDir, $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        TestContext.Out.WriteLine(fileContent);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
        fileContent.ShouldContain("namespace Test.Root");
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWhenRunWithMsBuildAndUseProjectNamespaceIsSpecifiedAndRootNamespaceIsNotSet(string language)
    {
        const string taskName = nameof(GenerateGitVersionInformation);
        const string outputProperty = nameof(GenerateGitVersionInformation.GitVersionInformationFilePath);
        var randDir = Guid.NewGuid().ToString("N");

        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        using var result = ExecuteMsBuildExeInAzurePipeline(project =>
        {
            var intermediateOutputPath = FileSystemHelper.Path.Combine("$(MSBuildProjectDirectory)", randDir);
            AddGenerateGitVersionInformationTask(project, taskName, taskName, outputProperty, language, intermediateOutputPath).Property("UseProjectNamespaceForGitVersionInformation", "True");
        }, language);

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();

        var generatedFilePath = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetDirectoryName(result.ProjectPath), randDir, $"GitVersionInformation.g.{extension}");
        result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

        var fileContent = this.FileSystem.File.ReadAllText(generatedFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "0"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.0.1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.0.1-1"));
        fileContent.ShouldContain("namespace App");
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWithUseProjectNamespaceSetAndRootNamespaceUnSet(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation
        {
            Language = language,
            UseProjectNamespaceForGitVersionInformation = "true",
            ProjectFile = "App.Project.csproj"
        };
        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));
        fileContent.ShouldContain("namespace App.Project");
    }

    [TestCaseSource(nameof(Languages))]
    public void GenerateGitVersionInformationTaskShouldCreateFileWithUseProjectNamespaceSetAndRootNamespaceIsSet(string language)
    {
        var extension = AssemblyInfoFileHelper.GetFileExtension(language);
        var task = new GenerateGitVersionInformation
        {
            Language = language,
            UseProjectNamespaceForGitVersionInformation = "true",
            ProjectFile = "App.Project.csproj",
            RootNamespace = "App.Project.RootNamespace"
        };
        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Task.GitVersionInformationFilePath.ShouldNotBeNull();
        result.Task.GitVersionInformationFilePath.ShouldMatch($@"GitVersionInformation.*\.g\.{extension}");

        var fileContent = this.FileSystem.File.ReadAllText(result.Task.GitVersionInformationFilePath);
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Major), "1"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Minor), "2"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.Patch), "4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.MajorMinorPatch), "1.2.4"));
        fileContent.ShouldMatch(string.Format(regexPattern, nameof(GitVersionVariables.FullSemVer), "1.2.4-1"));

        fileContent.ShouldContain("namespace App.Project.RootNamespace");
    }

    private static ProjectCreator AddGenerateGitVersionInformationTask(ProjectCreator project, string targetToRun, string taskName,
                                                             string outputProperty, string language,
                                                             string intermediateOutputPath = "$(MSBuildProjectDirectory)")
    {
        var assemblyFileLocation = typeof(GitVersionTaskBase).Assembly.Location;
        return project.UsingTaskAssemblyFile(taskName, assemblyFileLocation)
            .Property("ManagePackageVersionsCentrally", "false")
            .Property("GenerateAssemblyInfo", "false")
            .Property("Language", language)
            .Target(targetToRun, beforeTargets: "CoreCompile;GetAssemblyVersion;GenerateNuspec")
            .Task(taskName, parameters: new Dictionary<string, string?>
            {
                { "SolutionDirectory", "$(MSBuildProjectDirectory)" },
                { "VersionFile", "$(MSBuildProjectDirectory)/gitversion.json" },
                { "ProjectFile", "$(MSBuildProjectFullPath)" },
                { "Language", "$(Language)" },
                { "IntermediateOutputPath", intermediateOutputPath },
                { "UseProjectNamespaceForGitVersionInformation", "$(UseProjectNamespaceForGitVersionInformation)" },
                { "RootNamespace", "$(RootNamespace)" }
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
