using System.Collections.Generic;
using System.IO;
using GitVersion.MSBuildTask;
using GitVersion.MSBuildTask.Tasks;
using GitVersionTask.Tests.Helpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework;
using Shouldly;

namespace GitVersionTask.Tests
{
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

            using var result = ExecuteMsBuildTaskInBuildServer(task);

            result.Success.ShouldBe(true);
            result.Errors.ShouldBe(0);
            result.Task.AssemblyInfoTempFilePath.ShouldNotBeNull();

            var fileContent = File.ReadAllText(result.Task.AssemblyInfoTempFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
        }

        [Test]
        [Category(NoMono)]
        public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuild()
        {
            const string taskName = nameof(UpdateAssemblyInfo);
            const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

            using var result = ExecuteMsBuildExe(project =>
            {
                AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();

            var generatedFilePath = Path.Combine(Path.GetDirectoryName(result.ProjectPath), "AssemblyInfo.g.cs");
            result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

            var fileContent = File.ReadAllText(generatedFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.2.4.0"")]");
        }

        [Test]
        [Category(NoMono)]
        public void UpdateAssemblyInfoTaskShouldCreateFileWhenRunWithMsBuildInBuildServer()
        {
            const string taskName = nameof(UpdateAssemblyInfo);
            const string outputProperty = nameof(UpdateAssemblyInfo.AssemblyInfoTempFilePath);

            using var result = ExecuteMsBuildExeInBuildServer(project =>
            {
                AddUpdateAssemblyInfoTask(project, taskName, taskName, outputProperty);
            });

            result.ProjectPath.ShouldNotBeNullOrWhiteSpace();
            result.MsBuild.Count.ShouldBeGreaterThan(0);
            result.MsBuild.OverallSuccess.ShouldBe(true);
            result.MsBuild.ShouldAllBe(x => x.Succeeded);
            result.Output.ShouldNotBeNullOrWhiteSpace();

            var generatedFilePath = Path.Combine(Path.GetDirectoryName(result.ProjectPath), "AssemblyInfo.g.cs");
            result.Output.ShouldContain($"{outputProperty}: {generatedFilePath}");

            var fileContent = File.ReadAllText(generatedFilePath);
            fileContent.ShouldContain(@"[assembly: AssemblyVersion(""1.0.1.0"")]");
        }

        private static void AddUpdateAssemblyInfoTask(ProjectCreator project, string targetToRun, string taskName, string outputProperty)
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
                    { "CompileFiles", "@(Compile)" },
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
