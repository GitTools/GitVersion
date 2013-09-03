using System.IO;

public class GitDirFinder
{

    public static string TreeWalkForGitDir(string currentDirectory)
    {
        while (true)
        {
            var gitDir = Path.Combine(currentDirectory, @".git");
            if (Directory.Exists(gitDir))
            {
                return gitDir;
            }
            try
            {
                var parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }
                currentDirectory = parent.FullName;
            }
            catch
            {
                // trouble with tree walk.
                return null;
            }
        }
        return null;
    }
}