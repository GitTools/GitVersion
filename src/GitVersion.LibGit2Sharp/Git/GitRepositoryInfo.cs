using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    internal class GitRepositoryInfo : IGitRepositoryInfo
    {
        private readonly IOptions<GitVersionOptions> options;
        private GitVersionOptions gitVersionOptions => options.Value;

        private readonly Lazy<string?> dynamicGitRepositoryPath;
        private readonly Lazy<string?> dotGitDirectory;
        private readonly Lazy<string?> gitRootPath;
        private readonly Lazy<string> projectRootDirectory;

        public GitRepositoryInfo(IOptions<GitVersionOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            dynamicGitRepositoryPath = new Lazy<string?>(GetDynamicGitRepositoryPath);
            dotGitDirectory = new Lazy<string?>(GetDotGitDirectory);
            gitRootPath = new Lazy<string?>(GetGitRootPath);
            projectRootDirectory = new Lazy<string>(GetProjectRootDirectory);
        }

        public string? DynamicGitRepositoryPath => dynamicGitRepositoryPath.Value;
        public string? DotGitDirectory => dotGitDirectory.Value;
        public string? GitRootPath => gitRootPath.Value;
        public string ProjectRootDirectory => projectRootDirectory.Value;

        private string? GetDynamicGitRepositoryPath()
        {
            var repositoryInfo = gitVersionOptions.RepositoryInfo;
            if (string.IsNullOrWhiteSpace(repositoryInfo.TargetUrl)) return null;

            var targetUrl = repositoryInfo.TargetUrl;
            var dynamicRepositoryClonePath = repositoryInfo.DynamicRepositoryClonePath;

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

        private string? GetDotGitDirectory()
        {
            var gitDirectory = !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath)
                ? DynamicGitRepositoryPath
                : GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            gitDirectory = gitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(gitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            return gitDirectory?.Contains(Path.Combine(".git", "worktrees")) == true
                ? Directory.GetParent(Directory.GetParent(gitDirectory).FullName).FullName
                : gitDirectory;
        }

        private string GetProjectRootDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DynamicGitRepositoryPath))
            {
                return gitVersionOptions.WorkingDirectory;
            }

            var gitDirectory = GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            if (string.IsNullOrEmpty(gitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            return new GitRepository(gitDirectory).WorkingDirectory;
        }

        private string? GetGitRootPath()
        {
            var isDynamicRepo = !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath);
            var rootDirectory = isDynamicRepo ? DotGitDirectory : ProjectRootDirectory;

            return rootDirectory;
        }

        private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
        {
            try
            {
                var gitRepository = new GitRepository(possiblePath);
                return gitRepository.Remotes.Any(r => r.Url == targetUrl);
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
