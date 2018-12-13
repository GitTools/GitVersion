namespace GitTools.Git
{
    using System.IO;
    using LibGit2Sharp;

    public class GitDirFinder
    {
        public static string TreeWalkForDotGitDir(string currentDirectory)
        {
            var gitDirectory = Repository.Discover(currentDirectory);

            if (gitDirectory != null)
            {
                return gitDirectory.TrimEnd(Path.DirectorySeparatorChar);
            }

            return null;
        }
    }
}