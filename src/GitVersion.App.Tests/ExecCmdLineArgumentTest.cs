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

    [Test]
    public void CommitArgumentUsesRequestedCommitWhenCacheExists()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        var requestedCommit = fixture.MakeACommit();
        var headCommit = fixture.MakeACommit();

        var environment = new KeyValuePair<string, string?>(
            "GITVERSION_ARGUMENT_PARSER_VERSION", "v7");
        var showVariableArgument = "--show-variable";
        var commitArgument = "--commit";
        var workingDirectory = fixture.RepositoryPath;
        var targetPathArgument = string.Empty;

        var headResult = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {showVariableArgument} Sha", false, environment);
        var requestedCommitResult = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {commitArgument} {requestedCommit} {showVariableArgument} Sha", false, environment);

        headResult.ExitCode.ShouldBe(0);
        headResult.Output!.Trim().ShouldBe(headCommit);
        requestedCommitResult.ExitCode.ShouldBe(0);
        requestedCommitResult.Output!.Trim().ShouldBe(requestedCommit);
    }

    [TestCase("", true)]
    [TestCase("--verbosity NORMAL", true)]
    [TestCase("--verbosity quiet", false)]
    public void CheckBuildServerVerbosityConsole(string verbosityArgument, bool expectInformation)
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var environment = new KeyValuePair<string, string?>(
            "GITVERSION_ARGUMENT_PARSER_VERSION", "v7");
        var workingDirectory = fixture.RepositoryPath;
        var targetPathArgument = string.Empty;
        var outputArguments = "--output buildserver --log-file console";

        var result = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {verbosityArgument} {outputArguments}", false, environment);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        if (expectInformation)
        {
            result.Output.ShouldContain("INFO");
        }
        else
        {
            result.Output.ShouldNotContain("INFO");
        }
    }

    [Test]
    public void VerboseVerbosityIncludesDebugOutput()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.2.3");
        fixture.MakeACommit();

        var environment = new KeyValuePair<string, string?>(
            "GITVERSION_ARGUMENT_PARSER_VERSION", "v7");
        var workingDirectory = fixture.RepositoryPath;
        var targetPathArgument = string.Empty;
        var verbosityArgument = "--verbosity verbose";
        var outputArguments = "--no-cache --output buildserver --log-file console";

        var result = GitVersionHelper.ExecuteIn(workingDirectory,
            $"{targetPathArgument} {verbosityArgument} {outputArguments}", false, environment);

        result.ExitCode.ShouldBe(0);
        result.Output.ShouldNotBeNull();
        result.Output.ShouldContain("DBUG");
    }

    [Test]
    public void WorkingDirectoryWithoutGitFolderFailsWithInformativeMessage()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var result = GitVersionHelper.ExecuteIn(directory.FullName, null, false);

            result.ExitCode.ShouldNotBe(0);
            result.Output.ShouldNotBeNull();
            result.Output.ShouldContain("Cannot find the .git directory");
        }
        finally
        {
            directory.Delete(true);
        }
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
