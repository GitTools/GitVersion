using GitVersion.App.Tests.Helpers;
using GitVersion.Helpers;

namespace GitVersion.App.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class ExecCmdLineArgumentTest
{
    [Test]
    public void InvalidArgumentsExitCodeShouldNotBeZero()
    {
        using var fixture = new EmptyRepositoryFixture();
        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, arguments: " --invalid-argument");

        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("Could not parse command line parameter '--invalid-argument'");
    }

    [Test]
    public void LogPathContainsForwardSlash()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath,
            """ --log-file "/tmp/path" """, false);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain(
            """
                    "MajorMinorPatch": "1.2.4"
                    """);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CommitArgumentUsesRequestedCommitWhenCacheExists(bool useLegacyParser)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        var requestedCommit = fixture.MakeACommit();
        var headCommit = fixture.MakeACommit();

        var environment = new KeyValuePair<string, string?>(
            "GITVERSION_USE_V6_ARGUMENT_PARSER", useLegacyParser ? "true" : null);
        var showVariableArgument = useLegacyParser ? "-showvariable" : "--show-variable";
        var commitArgument = useLegacyParser ? "-c" : "--commit";
        var workingDirectory = useLegacyParser ? null : fixture.RepositoryPath;
        var targetPathArgument = useLegacyParser ? $" \"{fixture.RepositoryPath}\"" : string.Empty;

        var headResult = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {showVariableArgument} Sha", false, environment);
        var requestedCommitResult = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {commitArgument} {requestedCommit} {showVariableArgument} Sha", false, environment);

        headResult.ExitCode.ShouldBe(0);
        headResult.Output!.Trim().ShouldBe(headCommit);
        requestedCommitResult.ExitCode.ShouldBe(0);
        requestedCommitResult.Output!.Trim().ShouldBe(requestedCommit);
    }

    [Theory]
    [TestCase("", "INFO")]
    [TestCase("--verbosity NORMAL", "INFO")]
    [TestCase("--verbosity quiet", "")]
    public void CheckBuildServerVerbosityConsole(string verbosityArg, string expectedOutput)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath,
            $""" {verbosityArg} --output buildserver --log-file "/tmp/path" """, false);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain(expectedOutput);
    }

    [Test]
    public void WorkingDirectoryWithoutGitFolderFailsWithInformativeMessage()
    {
        var workingDirectory = FileSystemHelper.Path.GetTempPathLegacy();
        var result = GitVersionHelper.ExecuteIn(workingDirectory, null, false);

        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("Cannot find the .git directory");
    }

    [TestCase(" --help")]
    [TestCase(" --version")]
    public void WorkingDirectoryWithoutGitFolderDoesNotFailForVersionAndHelp(string argument)
    {
        var result = GitVersionHelper.ExecuteIn(workingDirectory: null, arguments: argument);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
    }

    [Test]
    public void WorkingDirectoryWithoutCommitsFailsWithInformativeMessage()
    {
        using var fixture = new EmptyRepositoryFixture();

        var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, " --log-file console", false);

        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("No commits found on the current branch.");
    }

    [Test]
    public void WorkingDirectoryDoesNotExistFailsWithInformativeMessage()
    {
        var workingDirectory = FileSystemHelper.Path.Combine(FileSystemHelper.Path.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
        var executable = ExecutableHelper.DotNetExecutable;

        var output = new StringBuilder();
        var args = ExecutableHelper.GetExecutableArgs($" --target-path {workingDirectory} ");

        var exitCode = ProcessHelper.Run(
            s => output.AppendLine(s),
            s => output.AppendLine(s),
            null,
            executable,
            args,
            FileSystemHelper.Path.GetCurrentDirectory());

        exitCode.ShouldNotBe(0);
        var outputString = output.ToString();
        outputString.ShouldContain($"The working directory '{workingDirectory}' does not exist.", Case.Insensitive, outputString);
    }
}
