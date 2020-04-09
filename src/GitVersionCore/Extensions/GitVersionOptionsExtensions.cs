using System.IO;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class GitVersionOptionsExtensions
    {
        private static bool IsDynamicGitRepository(GitVersionOptions gitVersionOptions) => !string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.DynamicGitRepositoryPath);

        public static string GetDotGitDirectory(this GitVersionOptions gitVersionOptions)
        {
            var dotGitDirectory = IsDynamicGitRepository(gitVersionOptions)
                ? gitVersionOptions.RepositoryInfo.DynamicGitRepositoryPath
                : Repository.Discover(gitVersionOptions.WorkingDirectory);

            dotGitDirectory = dotGitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            return dotGitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(dotGitDirectory).FullName).FullName
                : dotGitDirectory;
        }

        public static string GetProjectRootDirectory(this GitVersionOptions gitVersionOptions)
        {
            if (IsDynamicGitRepository(gitVersionOptions))
            {
                return gitVersionOptions.WorkingDirectory;
            }

            var dotGitDirectory = Repository.Discover(gitVersionOptions.WorkingDirectory);

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            using var repo = new Repository(dotGitDirectory);
            return repo.Info.WorkingDirectory;
        }
    }
}
