using System;
using System.IO;
using System.Text;
using GitTools.Testing;
using GitVersion;
using NUnit.Framework;
using Shouldly;
using GitVersion.Helpers;
using GitVersionExe.Tests.Helpers;

namespace GitVersionExe.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class ExecCmdLineArgumentTest
    {
        [Test]
        public void RunExecViaCommandLine()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                var buildFile = Path.Combine(fixture.RepositoryPath, "RunExecViaCommandLine.csproj");
                File.Delete(buildFile);
                const string buildFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Target Name=""OutputResults"">
    <Message Text=""GitVersion_FullSemVer: $(GitVersion_FullSemVer)""/>
  </Target>
</Project>";
                File.WriteAllText(buildFile, buildFileContent);
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, SpecifiedArgumentRunner.BuildTool, "RunExecViaCommandLine.csproj /target:OutputResults");

                result.ExitCode.ShouldBe(0, result.Log);
                result.Log.ShouldContain("GitVersion_FullSemVer: 1.2.4+1");
            }
        }


        [Test]
        public void InvalidArgumentsExitCodeShouldNotBeZero()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /invalid-argument");

                result.ExitCode.ShouldBe(1);
                result.Output.ShouldContain("Could not parse command line parameter '/invalid-argument'");
            }
        }

        [Test]
        public void LogPathContainsForwardSlash()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: @" /l ""/some/path""", logToFile: false);

                result.ExitCode.ShouldBe(0);
                result.Output.ShouldContain(@"""MajorMinorPatch"":""1.2.4""");
            }
        }

        [Theory]
        [TestCase("", "INFO [")]
        [TestCase("-verbosity Info", "INFO [")]
        [TestCase("-verbosity Error", "")]
        public void CheckBuildServerVerbosityConsole(string verbosityArg, string expectedOutput)
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeATaggedCommit("1.2.3");
                fixture.MakeACommit();

                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: $@" {verbosityArg} -output buildserver /l ""/some/path""", logToFile: false);

                result.ExitCode.ShouldBe(0);
                result.Output.ShouldContain(expectedOutput);
            }
        }

        [Test]
        public void WorkingDirectoryWithoutGitFolderCrashesWithInformativeMessage()
        {
            var results = GitVersionHelper.ExecuteIn(Environment.SystemDirectory, null, isTeamCity: false, logToFile: false);
            results.Output.ShouldContain("Can't find the .git directory in");
        }

        [Test]
        public void WorkingDirectoryDoesNotExistCrashesWithInformativeMessage()
        {
            var workingDirectory = Path.Combine(PathHelper.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            var executable = PathHelper.GetExecutable();

            var output = new StringBuilder();
            var args = PathHelper.GetExecutableArgs($" /targetpath {workingDirectory} ");

            Console.WriteLine("Executing: {0} {1}", executable, args);
            Console.WriteLine();

            var exitCode = ProcessHelper.Run(
                s => output.AppendLine(s),
                s => output.AppendLine(s),
                null,
                executable,
                args,
                PathHelper.GetCurrentDirectory());

            exitCode.ShouldNotBe(0);
            var outputString = output.ToString();
            outputString.ShouldContain($"The working directory '{workingDirectory}' does not exist.", () => outputString);
        }
    }
}
