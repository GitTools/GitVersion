namespace GitFlowVersion
{
    using System.IO;
    using LibGit2Sharp;

    public class GitDirFinder
    {
        public static string TreeWalkForGitDir(string currentDirectory)
        {
            var gitDirectory = Repository.Discover(currentDirectory);

            if (gitDirectory != null)
            {
                return gitDirectory.TrimEnd(new []{ Path.DirectorySeparatorChar });
            }

            return null;
        }
    }
}