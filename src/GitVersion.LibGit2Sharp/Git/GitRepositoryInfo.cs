using System;
using System.IO;
using System.Linq;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    internal class GitRepositoryInfo : IGitRepositoryInfo
    {
        private readonly IOptions<GitVersionOptions> options;
        private GitVersionOptions gitVersionOptions => options.Value;

        private Lazy<string> dotGitDirectory;
        private Lazy<string> projectRootDirectory;
        private Lazy<string> dynamicGitRepositoryPath;
        private Lazy<string> gitRootPath;

        public GitRepositoryInfo(IOptions<GitVersionOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            dynamicGitRepositoryPath = new Lazy<string>(GetDynamicGitRepositoryPath);
            dotGitDirectory = new Lazy<string>(GetDotGitDirectory);
            projectRootDirectory = new Lazy<string>(GetProjectRootDirectory);
            gitRootPath = new Lazy<string>(GetGitRootPath);
        }

        public string DotGitDirectory => dotGitDirectory.Value;
        public string ProjectRootDirectory => projectRootDirectory.Value;
        public string DynamicGitRepositoryPath => dynamicGitRepositoryPath.Value;
        public string GitRootPath => gitRootPath.Value;

        private string GetDynamicGitRepositoryPath()
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

        private string GetDotGitDirectory()
        {
            var _dotGitDirectory = !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath)
                ? DynamicGitRepositoryPath
                : GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            _dotGitDirectory = _dotGitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(_dotGitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            return _dotGitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(_dotGitDirectory).FullName).FullName
                : _dotGitDirectory;
        }

        private string GetProjectRootDirectory()
        {
            if (!string.IsNullOrWhiteSpace(DynamicGitRepositoryPath))
            {
                return gitVersionOptions.WorkingDirectory;
            }

            var _dotGitDirectory = GitRepository.Discover(gitVersionOptions.WorkingDirectory);

            if (string.IsNullOrEmpty(_dotGitDirectory))
                throw new DirectoryNotFoundException("Cannot find the .git directory");

            return new GitRepository(new NullLog(), _dotGitDirectory).WorkingDirectory;
        }

        private string GetGitRootPath()
        {
            var isDynamicRepo = !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath);
            var rootDirectory = isDynamicRepo ? DotGitDirectory : ProjectRootDirectory;

            return rootDirectory;
        }

        private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
        {
            try
            {
                return new GitRepository(new NullLog(), possiblePath).GitRepoHasMatchingRemote(targetUrl);
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
