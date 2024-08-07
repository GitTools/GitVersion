namespace GitVersion.Extensions;

public static class GitExtensions
{
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

    public static string CreateGitLogArgs(int? maxCommits)
    {
        var commits = maxCommits != null ? $" -n {maxCommits}" : null;
        return $"""log --graph --format="%h %cr %d" --decorate --date=relative --all --remotes=*{commits}""";
    }
}
