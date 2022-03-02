using GitVersion.Helpers;

namespace GitVersion.Extensions;

public static class GitExtensions
{
    public static void DumpGraph(string workingDirectory, Action<string>? writer = null, int? maxCommits = null)
    {
        var output = new StringBuilder();
        try
        {
            ProcessHelper.Run(
                o => output.AppendLine(o),
                e => output.AppendLineFormat("ERROR: {0}", e),
                null,
                "git",
                CreateGitLogArgs(maxCommits),
                workingDirectory);
        }
        catch (FileNotFoundException exception)
        {
            if (exception.FileName != "git")
            {
                throw;
            }

            output.AppendLine("Could not execute 'git log' due to the following error:");
            output.AppendLine(exception.ToString());
        }

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
        return $@"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*{commits}";
    }
}
