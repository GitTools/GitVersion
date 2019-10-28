using System;
using System.IO;
using System.Linq;
using GitVersion.Helpers;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitPreparer : IGitPreparer
    {
        private readonly ILog log;
        private readonly IEnvironment environment;
        private readonly string dynamicRepositoryLocation;
        private readonly AuthenticationInfo authentication;
        private readonly bool noFetch;

        private const string DefaultRemoteName = "origin";
        private string dynamicGitRepositoryPath;

        public GitPreparer(ILog log, IEnvironment environment, IOptions<Arguments> options)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.environment = environment;
            var arguments = options.Value;

            TargetUrl = arguments.TargetUrl;
            WorkingDirectory = arguments.TargetPath.TrimEnd('/', '\\');

            dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            authentication = new AuthenticationInfo
            {
                Username = arguments.Authentication?.Username,
                Password = arguments.Authentication?.Password
            };

            noFetch = arguments.NoFetch;
        }

        public void Prepare(bool normalizeGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false)
        {
            if (string.IsNullOrWhiteSpace(TargetUrl))
            {
                if (!normalizeGitDirectory) return;
                using (log.IndentLog($"Normalizing git directory for branch '{currentBranch}'"))
                {
                    if (shouldCleanUpRemotes)
                    {
                        CleanupDuplicateOrigin();
                    }
                    GitRepositoryHelper.NormalizeGitDirectory(log, environment, GetDotGitDirectory(), authentication, noFetch, currentBranch, IsDynamicGitRepository());
                }
                return;
            }

            var tempRepositoryPath = CalculateTemporaryRepositoryPath(TargetUrl, dynamicRepositoryLocation);

            dynamicGitRepositoryPath = CreateDynamicRepository(tempRepositoryPath, authentication, TargetUrl, currentBranch);
        }

        public TResult WithRepository<TResult>(Func<IRepository, TResult> action)
        {
            using IRepository repo = new Repository(GetDotGitDirectory());
            return action(repo);
        }
        
        public string GetDotGitDirectory()
        {
            var dotGitDirectory = IsDynamicGitRepository() ? dynamicGitRepositoryPath : Repository.Discover(WorkingDirectory);

            dotGitDirectory = dotGitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + WorkingDirectory);

            if (dotGitDirectory.Contains(Path.Combine(".git", "worktrees")))
                return Directory.GetParent(Directory.GetParent(dotGitDirectory).FullName).FullName;

            return dotGitDirectory;
        }

        public string GetProjectRootDirectory()
        {
            log.Info($"IsDynamicGitRepository: {IsDynamicGitRepository()}");
            if (IsDynamicGitRepository())
            {
                log.Info($"Returning Project Root as {WorkingDirectory}");
                return WorkingDirectory;
            }

            var dotGitDirectory = Repository.Discover(WorkingDirectory);

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {WorkingDirectory}");

            using var repo = new Repository(dotGitDirectory);
            var result = repo.Info.WorkingDirectory;
            log.Info($"Returning Project Root from DotGitDirectory: {dotGitDirectory} - {result}");
            return result;
        }

        public string TargetUrl { get; }

        public string WorkingDirectory { get; }

        private bool IsDynamicGitRepository() => !string.IsNullOrWhiteSpace(dynamicGitRepositoryPath);

        private void CleanupDuplicateOrigin()
        {
            var remoteToKeep = DefaultRemoteName;

            var repo = new Repository(GetDotGitDirectory());

            // check that we have a remote that matches defaultRemoteName if not take the first remote
            if (!repo.Network.Remotes.Any(remote => remote.Name.Equals(DefaultRemoteName, StringComparison.InvariantCultureIgnoreCase)))
            {
                remoteToKeep = repo.Network.Remotes.First().Name;
            }

            var duplicateRepos = repo.Network
                                     .Remotes
                                     .Where(remote => !remote.Name.Equals(remoteToKeep, StringComparison.InvariantCultureIgnoreCase))
                                     .Select(remote => remote.Name);

            // remove all remotes that are considered duplicates
            foreach (var repoName in duplicateRepos)
            {
                repo.Network.Remotes.Remove(repoName);
            }
        }

        private static string CalculateTemporaryRepositoryPath(string targetUrl, string dynamicRepositoryLocation)
        {
            var userTemp = dynamicRepositoryLocation ?? Path.GetTempPath();
            var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
            var possiblePath = Path.Combine(userTemp, repositoryName);

            // Verify that the existing directory is ok for us to use
            if (Directory.Exists(possiblePath))
            {
                if (!GitRepoHasMatchingRemote(possiblePath, targetUrl))
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
            }

            return possiblePath;
        }

        private static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
        {
            try
            {
                using var repository = new Repository(possiblePath);
                return repository.Network.Remotes.Any(r => r.Url == targetUrl);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string CreateDynamicRepository(string targetPath, AuthenticationInfo auth, string repositoryUrl, string targetBranch)
        {
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }

            using (log.IndentLog($"Creating dynamic repository at '{targetPath}'"))
            {
                var gitDirectory = Path.Combine(targetPath, ".git");
                if (Directory.Exists(targetPath))
                {
                    log.Info("Git repository already exists");
                    using (log.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
                    {
                        GitRepositoryHelper.NormalizeGitDirectory(log, environment, gitDirectory, auth, noFetch, targetBranch, true);
                    }

                    return gitDirectory;
                }

                CloneRepository(repositoryUrl, gitDirectory, auth);

                using (log.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
                {
                    // Normalize (download branches) before using the branch
                    GitRepositoryHelper.NormalizeGitDirectory(log, environment, gitDirectory, auth, noFetch, targetBranch, true);
                }

                return gitDirectory;
            }
        }

        private void CloneRepository(string repositoryUrl, string gitDirectory, AuthenticationInfo auth)
        {
            Credentials credentials = null;

            if (auth != null)
            {
                if (!string.IsNullOrWhiteSpace(auth.Username) && !string.IsNullOrWhiteSpace(auth.Password))
                {
                    log.Info($"Setting up credentials using name '{auth.Username}'");

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = auth.Username,
                        Password = auth.Password
                    };
                }
            }

            try
            {
                using (log.IndentLog($"Cloning repository from url '{repositoryUrl}'"))
                {
                    var cloneOptions = new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    };

                    var returnedPath = Repository.Clone(repositoryUrl, gitDirectory, cloneOptions);
                    log.Info($"Returned path after repository clone: {returnedPath}");
                }
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new Exception("Unauthorized: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new Exception("Forbidden: Possibly Incorrect username/password");
                }
                if (message.Contains("404"))
                {
                    throw new Exception("Not found: The repository was not found");
                }

                throw new Exception("There was an unknown problem with the Git repository you provided", ex);
            }
        }
    }
}
