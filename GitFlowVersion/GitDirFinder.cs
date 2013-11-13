namespace GitFlowVersion
{
    using System.IO;
    using LibGit2Sharp;

    public class GitDirFinder
    {
        public static string TreeWalkForGitDir(string currentDirectory)
        {
            string gitDir = Repository.Discover(currentDirectory);

            if (gitDir != null)
            {
                return gitDir.TrimEnd(new []{ Path.DirectorySeparatorChar });
            }

            return null;
        }
    }
}