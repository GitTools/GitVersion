namespace GitVersion.Testing.Internal;

/// <summary>
///     Runs the real `git` executable with a deterministic, machine-configuration-free environment.
/// </summary>
internal static class GitCliRunner
{
    /// <summary>
    ///     A path that is guaranteed not to contain a git configuration file, used to make sure the
    ///     user's global configuration never leaks into test repositories.
    /// </summary>
    private static readonly string EmptyGlobalConfigPath =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gitversion-testing-empty-gitconfig");

    private static readonly string[] IsolationArguments =
    [
        "-c", "commit.gpgsign=false",
        "-c", "tag.gpgsign=false",
        "-c", "gc.auto=0",
        "-c", "core.autocrlf=false",
        "-c", "protocol.file.allow=always"
    ];

    public static string Run(string workingDirectory, IReadOnlyCollection<string> arguments, Signature? author = null, Signature? committer = null)
    {
        var exitCode = TryRun(workingDirectory, arguments, out var output, out var error, author, committer);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} (in '{workingDirectory}') exited with code {exitCode}:{SysEnv.NewLine}{output}{SysEnv.NewLine}{error}");
        }

        return output;
    }

    public static int TryRun(string workingDirectory, IReadOnlyCollection<string> arguments, out string output, out string error, Signature? author = null, Signature? committer = null)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var exitCode = ProcessHelper.Run(
            o => outputBuilder.AppendLine(o),
            e => errorBuilder.AppendLine(e),
            null,
            "git",
            [.. IsolationArguments, .. arguments],
            workingDirectory,
            [.. GetEnvironmentVariables(author, committer)]);

        output = outputBuilder.ToString();
        error = errorBuilder.ToString();
        return exitCode;
    }

    private static IEnumerable<KeyValuePair<string, string?>> GetEnvironmentVariables(Signature? author, Signature? committer)
    {
        yield return new("GIT_CONFIG_NOSYSTEM", "1");
        yield return new("GIT_CONFIG_GLOBAL", EmptyGlobalConfigPath);
        yield return new("GIT_TERMINAL_PROMPT", "0");

        if (author is not null)
        {
            yield return new("GIT_AUTHOR_NAME", author.Name);
            yield return new("GIT_AUTHOR_EMAIL", author.Email);
            yield return new("GIT_AUTHOR_DATE", FormatDate(author.When));
        }

        if (committer is not null)
        {
            yield return new("GIT_COMMITTER_NAME", committer.Name);
            yield return new("GIT_COMMITTER_EMAIL", committer.Email);
            yield return new("GIT_COMMITTER_DATE", FormatDate(committer.When));
        }
    }

    private static string FormatDate(DateTimeOffset when) => when.ToString("yyyy-MM-dd'T'HH:mm:sszzz");
}
