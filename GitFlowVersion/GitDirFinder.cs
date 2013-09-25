namespace GitFlowVersion
{
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
                var parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }
                currentDirectory = parent.FullName;
            }
            return null;
        }
    }
}