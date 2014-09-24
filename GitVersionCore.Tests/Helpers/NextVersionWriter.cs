using System.IO;
using LibGit2Sharp;

public static class NextVersionWriter
{
    public static void AddNextVersionTxtFile(this IRepository repository, string version)
    {
        var nextVersionFile = Path.Combine(repository.Info.WorkingDirectory, "NextVersion.txt");
        File.WriteAllText(nextVersionFile, version);
    }
}