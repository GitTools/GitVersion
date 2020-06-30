using System.Collections.Generic;
using System.Linq;
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
    public class GetVersionTaskTests : TestTaskBase
    {
        [Test]
        public void OutputsShouldMatchVariableProvider()
        {
            var taskProperties = typeof(GetVersion)
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(OutputAttribute), false).Any())
                .Select(p => p.Name);

            var variablesProperties = VersionVariables.AvailableVariables;

            taskProperties.ShouldBe(variablesProperties, ignoreOrder: true);
        }

        [Test]
        public void GetVersionTaskShouldReturnVersionOutputVariables()
        {
            var task = new GetVersion();

            using var result = ExecuteMsBuildTask(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.Major.ShouldBe("1");
            result.Task.Minor.ShouldBe("2");
            result.Task.Patch.ShouldBe("4");
            result.Task.MajorMinorPatch.ShouldBe("1.2.4");
            result.Task.FullSemVer.ShouldBe("1.2.4+1");
        }

        [Test]
        public void GetVersionTaskShouldReturnVersionOutputVariablesForBuildServer()
        {
            var task = new GetVersion();

            using var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.Major.ShouldBe("1");
            result.Task.Minor.ShouldBe("0");
            result.Task.Patch.ShouldBe("1");
            result.Task.MajorMinorPatch.ShouldBe("1.0.1");
            result.Task.FullSemVer.ShouldBe("1.0.1+1");
        }

        [TestCase(nameof(VersionVariables.Major), "1")]
        [TestCase(nameof(VersionVariables.Minor), "2")]
        [TestCase(nameof(VersionVariables.Patch), "4")]
        [TestCase(nameof(VersionVariables.MajorMinorPatch), "1.2.4")]
        [TestCase(nameof(VersionVariables.FullSemVer), "1.2.4+1")]
        [Category(NoMono)]
        public void GetVersionTaskShouldReturnVersionOutputVariablesWhenRunWithMsBuild(string outputProperty, string version)
        {
            const string taskName = nameof(GetVersion);

            using var result = ExecuteMsBuildExe(project =>
            {
                AddGetVersionTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();
            result.Output.ShouldContain($"GitVersion_{outputProperty}: {version}");
        }

        [TestCase(nameof(VersionVariables.Major), "1")]
        [TestCase(nameof(VersionVariables.Minor), "0")]
        [TestCase(nameof(VersionVariables.Patch), "1")]
        [TestCase(nameof(VersionVariables.MajorMinorPatch), "1.0.1")]
        [TestCase(nameof(VersionVariables.FullSemVer), "1.0.1+1")]
        [Category(NoMono)]
        public void GetVersionTaskShouldReturnVersionOutputVariablesWhenRunWithMsBuildInBuildServer(string outputProperty, string version)
        {
            const string taskName = nameof(GetVersion);

            using var result = ExecuteMsBuildExeInBuildServer(project =>
            {
                AddGetVersionTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();
            result.Output.ShouldContain($"GitVersion_{outputProperty}: {version}");
        }

        private static void AddGetVersionTask(ProjectCreator project, string targetToRun, string taskName, string outputProperty)
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
                    .TaskOutputProperty(outputProperty, $"GitVersion_{outputProperty}")
                .Target(MsBuildExeFixture.OutputTarget, dependsOnTargets: targetToRun)
                    .TaskMessage($"GitVersion_{outputProperty}: $(GitVersion_{outputProperty})", MessageImportance.High);
        }
    }
}
