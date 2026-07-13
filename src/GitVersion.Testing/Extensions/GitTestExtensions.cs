using GitVersion.Testing.Internal;

namespace GitVersion.Testing.Extensions;

public static class GitTestExtensions
{
    public static void ExecuteGitCmd(string gitCmd, string workingDirectory, Action<string>? writer = null)
    {
        var output = new StringBuilder();
        try
        {
            ProcessHelper.Run(
                o => output.AppendLine(o),
                e => output.AppendLineFormat("ERROR: {0}", e),
                null,
                "git",
                gitCmd,
                workingDirectory);
        }
        catch (FileNotFoundException exception) when (exception.FileName == "git")
        {
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
}
