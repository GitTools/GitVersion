using System.Collections.Generic;
using GitVersion.MSBuildTask;
using GitVersion.MSBuildTask.Tasks;
using GitVersionTask.Tests.Helpers;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework;
using Shouldly;

namespace GitVersionTask.Tests
{
    [TestFixture]
    public class WriteVersionInfoTest : TestTaskBase
    {
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
        public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputInBuildServer()
        {
            var task = new WriteVersionInfoToBuildLog();

            using var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Log.ShouldContain("##vso[task.setvariable variable=GitVersion.FullSemVer]1.0.1+1");
        }

        [Test]
        [Category(NoMono)]
        public void WriteVersionInfoTaskShouldNotLogOutputVariablesToBuildOutputWhenRunWithMsBuild()
        {
            const string taskName = nameof(WriteVersionInfoToBuildLog);

            using var result = ExecuteMsBuildExe(project =>
            {
                AddWriteVersionInfoToBuildLogTask(project, taskName, taskName);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();
            result.Output.ShouldNotContain("##vso[task.setvariable variable=GitVersion.FullSemVer]");
        }

        [Test]
        [Category(NoMono)]
        public void WriteVersionInfoTaskShouldLogOutputVariablesToBuildOutputWhenRunWithMsBuildInBuildServer()
        {
            const string taskName = nameof(WriteVersionInfoToBuildLog);

            using var result = ExecuteMsBuildExeInBuildServer(project =>
            {
                AddWriteVersionInfoToBuildLogTask(project, taskName, taskName);
            });

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
                .Task(taskName, parameters: new Dictionary<string, string>
                {
                    { "SolutionDirectory", "$(MSBuildProjectDirectory)" },
                    { "NoFetch", "false" },
                    { "NoNormalize", "false" }
                })
                .Target(MsBuildExeFixture.OutputTarget, dependsOnTargets: targetToRun);
        }
    }
}
