using GitVersion.MsBuild.Tasks;
using GitVersion.MsBuild.Tests.Helpers;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.MsBuild.Tests.Tasks;

[TestFixture]
public class WriteVersionInfoTest : TestTaskBase
{
    private string GitHubEnvFilePath { get; set; } = Path.GetTempFileName();

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (File.Exists(GitHubEnvFilePath))
        {
            File.Delete(GitHubEnvFilePath);
        }
    }

    [Test]
    public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutput()
    {
        var task = new WriteVersionInfoToBuildLog();

        using var result = ExecuteMsBuildTask(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Log.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
    }

    [Test]
    public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputInAzurePipeline()
    {
        var task = new WriteVersionInfoToBuildLog();

        using var result = ExecuteMsBuildTaskInAzurePipeline(task);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
    }


    [TestCase("2021-02-14.1")]
    public void WriteVersionInfoTaskShouldNotUpdateBuildNumberInAzurePipeline(string buildNumber)
    {
        var task = new WriteVersionInfoToBuildLog();
        const string content = "update-build-number: false";

        using var result = ExecuteMsBuildTaskInAzurePipeline(task, buildNumber, content);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Log.ShouldNotContain("##vso[build.updatebuildnumber]");
    }


    [TestCase("2021-02-14.1-$(GITVERSION.FullSemVer)", "2021-02-14.1-1.0.1+1", Ignore = "#2552 - GitVersion.MsBuild does not set Azure DevOps build number")]
    [TestCase("2021-02-14.1-$(GITVERSION.SemVer)", "2021-02-14.1-1.0.1", Ignore = "#2552 - GitVersion.MsBuild does not set Azure DevOps build number")]
    [TestCase("2021-02-14.1-$(GITVERSION.minor)", "2021-02-14.1-0", Ignore = "#2552 - GitVersion.MsBuild does not set Azure DevOps build number")]
    [TestCase("2021-02-14.1-$(GITVERSION_MAJOR)", "2021-02-14.1-1", Ignore = "#2552 - GitVersion.MsBuild does not set Azure DevOps build number")]
    [TestCase("2021-02-14.1", "1.0.1+1", Ignore = "#2552 - GitVersion.MsBuild does not set Azure DevOps build number")]
    public void WriteVersionInfoTaskShouldUpdateBuildNumberInAzurePipeline(string buildNumber, string expected)
    {
        var task = new WriteVersionInfoToBuildLog();

        using var result = ExecuteMsBuildTaskInAzurePipeline(task, buildNumber);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        result.Log.ShouldContain($"##vso[build.updatebuildnumber]{expected}");
    }

    [Test]
    public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputInGitHubActions()
    {
        var task = new WriteVersionInfoToBuildLog();

        using var result = ExecuteMsBuildTaskInGitHubActions(task, GitHubEnvFilePath);

        result.Success.ShouldBe(true);
        result.Errors.ShouldBe(0);
        string content = File.ReadAllText(GitHubEnvFilePath);
        content.ShouldContain("GitVersion_SemVer=1.0.1");
    }

    [Test]
    public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutputWhenRunWithMsBuild()
    {
        const string taskName = nameof(WriteVersionInfoToBuildLog);

        using var result = ExecuteMsBuildExe(project => AddWriteVersionInfoToBuildLogTask(project, taskName, taskName));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();
        result.Output.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
    }

    [Test]
    public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputWhenRunWithMsBuildInAzurePipeline()
    {
        const string taskName = nameof(WriteVersionInfoToBuildLog);

        using var result = ExecuteMsBuildExeInAzurePipeline(project => AddWriteVersionInfoToBuildLogTask(project, taskName, taskName));

        result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
        result.MsBuild.Count.ShouldBeGreaterThan(0);
        result.MsBuild.OverallSuccess.ShouldBe(true);
        result.MsBuild.ShouldAllBe(x => x.Succeeded);
        result.Output.ShouldNotBeNullOrWhiteSpace();
        result.Output.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
    }

    private static void AddWriteVersionInfoToBuildLogTask(ProjectCreator project, string targetToRun, string taskName)
    {
        var assemblyFileLocation = typeof(GitVersionTaskBase).Assembly.Location;
        project.UsingTaskAssemblyFile(taskName, assemblyFileLocation)
            .Property("GenerateAssemblyInfo", "false")
            .Target(targetToRun, beforeTargets: "CoreCompile;GetAssemblyVersion;GenerateNuspec")
            .Task(taskName, parameters: new Dictionary<string, string?>
            {
                { "SolutionDirectory", "$(MSBuildProjectDirectory)" },
                { "VersionFile", "$(MSBuildProjectDirectory)/gitversion.json" }
            })
            .Target(MsBuildExeFixture.OutputTarget, dependsOnTargets: targetToRun);
    }
}
