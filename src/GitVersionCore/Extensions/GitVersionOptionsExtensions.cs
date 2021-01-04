using System;
using System.IO;
using System.Linq;

namespace GitVersion.Extensions
{
    public static class GitVersionOptionsExtensions
    {
        public static string GetDotGitDirectory(this GitVersionOptions gitVersionOptions)
        {
            var dotGitDirectory = !string.IsNullOrWhiteSpace(gitVersionOptions.DynamicGitRepositoryPath)
                ? gitVersionOptions.DynamicGitRepositoryPath
                : GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            dotGitDirectory = dotGitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            return dotGitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(dotGitDirectory).FullName).FullName
                : dotGitDirectory;
        }

        public static string GetProjectRootDirectory(this GitVersionOptions gitVersionOptions)
        {
            if (!string.IsNullOrWhiteSpace(gitVersionOptions.DynamicGitRepositoryPath))
            {
                return gitVersionOptions.WorkingDirectory;
            }

            var dotGitDirectory = GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            using var repository = new GitRepository(dotGitDirectory);
            return repository.WorkingDirectory;
        }

        public static string GetGitRootPath(this GitVersionOptions options)
        {
            var isDynamicRepo = !string.IsNullOrWhiteSpace(options.DynamicGitRepositoryPath);
            var rootDirectory = isDynamicRepo ? options.DotGitDirectory : options.ProjectRootDirectory;

            return rootDirectory;
        }

        public static string GetDynamicGitRepositoryPath(this GitVersionOptions gitVersionOptions)
        {
            if (string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.TargetUrl)) return null;

            var targetUrl = gitVersionOptions.RepositoryInfo.TargetUrl;
            var dynamicRepositoryClonePath = gitVersionOptions.RepositoryInfo.DynamicRepositoryClonePath;

            var userTemp = dynamicRepositoryClonePath ?? Path.GetTempPath();
            var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
            var possiblePath = Path.Combine(userTemp, repositoryName);

            // Verify that the existing directory is ok for us to use
            if (Directory.Exists(possiblePath) && !GitRepoHasMatchingRemote(possiblePath, targetUrl))
            {
                var i = 1;
                var originalPath = possiblePath;
                bool possiblePathExists;
                do
                {
                    possiblePath = string.Concat(originalPath, "_", i++.ToString());
                    possiblePathExists = Directory.Exists(possiblePath);
                } while (possiblePathExists && !GitRepoHasMatchingRemote(possiblePath, targetUrl));
            }

            var repositoryPath = Path.Combine(possiblePath, ".git");
            return repositoryPath;

        }

        private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
        {
            try
            {
                using IGitRepository repository = new GitRepository(possiblePath);
                return repository.GitRepoHasMatchingRemote(targetUrl);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
