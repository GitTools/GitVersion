using System;
using System.IO;
using System.Text;
using GitTools.Testing;
using GitVersion.Helpers;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class ExecCmdLineArgumentTest
    {
        [Test]
        public void RunExecViaCommandLine()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var buildFile = Path.Combine(fixture.RepositoryPath, "RunExecViaCommandLine.csproj");
            File.Delete(buildFile);

            File.WriteAllText(buildFile, MsBuildProjectArgTest.TestProject);
            var exec = "dotnet";
            var execArgs = "msbuild RunExecViaCommandLine.csproj /target:OutputResults";
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, exec, execArgs);

            result.ExitCode.ShouldBe(0, result.Log);
            result.Log.ShouldContain("GitVersion_FullSemVer: 1.2.4+1");
        }

        [Test]
        public void InvalidArgumentsExitCodeShouldNotBeZero()
        {
            using var fixture = new EmptyRepositoryFixture();
            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " /invalid-argument");

            result.ExitCode.ShouldNotBe(0);
            result.Output.ShouldContain("Could not parse command line parameter '/invalid-argument'");
        }

        [Test]
        public void LogPathContainsForwardSlash()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: @" /l ""/tmp/path""", logToFile: false);

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldContain(@"""MajorMinorPatch"":""1.2.4""");
        }

        [Theory]
        [TestCase("", "INFO [")]
        [TestCase("-verbosity NORMAL", "INFO [")]
        [TestCase("-verbosity quiet", "")]
        public void CheckBuildServerVerbosityConsole(string verbosityArg, string expectedOutput)
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("1.2.3");
            fixture.MakeACommit();

            var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: $@" {verbosityArg} -output buildserver /l ""/tmp/path""", logToFile: false);

            result.ExitCode.ShouldBe(0);
            result.Output.ShouldContain(expectedOutput);
        }

        [Test]
        public void WorkingDirectoryWithoutGitFolderFailsWithInformativeMessage()
        {
            var result = GitVersionHelper.ExecuteIn(Environment.SystemDirectory, arguments: null, logToFile: false);

            result.ExitCode.ShouldNotBe(0);
            result.Output.ShouldContain("Can't find the .git directory in");
        }

        [Test]
        public void WorkingDirectoryDoesNotExistFailsWithInformativeMessage()
        {
            var workingDirectory = Path.Combine(PathHelper.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            var executable = PathHelper.GetExecutable();

            var output = new StringBuilder();
            var args = PathHelper.GetExecutableArgs($" /targetpath {workingDirectory} ");

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
