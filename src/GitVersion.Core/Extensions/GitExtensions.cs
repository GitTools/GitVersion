namespace GitVersion.Extensions;

/// <summary>Extension and utility methods that supplement Git-related operations.</summary>
public static class GitExtensions
{
    /// <summary>Writes a hint message explaining how to run <c>git log --graph</c> to visualise the repository history.</summary>
    public static void DumpGraphLog(Action<string>? writer = null, int? maxCommits = null)
    {
        var output = new StringBuilder();
        output.AppendLine($"Please run `git {CreateGitLogArgs(maxCommits)}` to see the git graph. This can help you troubleshoot any issues.");
        if (writer != null)
        {
            writer(output.ToString());
        }
        else
        {
            Console.Write(output.ToString());
        }
    }

    /// <summary>Builds the <c>git log</c> argument string for a decorated graph view, optionally limiting to <paramref name="maxCommits"/> commits.</summary>
    public static string CreateGitLogArgs(int? maxCommits)
    {
        var commits = maxCommits != null ? $" -n {maxCommits}" : null;
        return $"""log --graph --format="%h %cr %d" --decorate --date=relative --all --remotes=*{commits}""";
    }
}
