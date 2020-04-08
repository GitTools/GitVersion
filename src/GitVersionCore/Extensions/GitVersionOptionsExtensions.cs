using System.IO;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class GitVersionOptionsExtensions
    {
        public static bool IsDynamicGitRepository(this GitVersionOptions gitVersionOptions) => !string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.DynamicGitRepositoryPath);

        public static string GetDotGitDirectory(this GitVersionOptions gitVersionOptions)
        {
            var gitDirectory = gitVersionOptions.IsDynamicGitRepository() ? gitVersionOptions.RepositoryInfo.DynamicGitRepositoryPath : Repository.Discover(gitVersionOptions.WorkingDirectory);

            gitDirectory = gitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(gitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + gitDirectory);

            return gitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(gitDirectory).FullName).FullName
                : gitDirectory;
        }

        public static string GetProjectRootDirectory(this GitVersionOptions gitVersionOptions)
        {
            if (gitVersionOptions.IsDynamicGitRepository())
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
