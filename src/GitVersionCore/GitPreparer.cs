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
        private readonly IOptions<Arguments> options;

        private const string DefaultRemoteName = "origin";
        private string dotGitDirectory;
        private string projectRootDirectory;

        public string GetTargetUrl() => options.Value.TargetUrl;

        public string GetWorkingDirectory() => options.Value.TargetPath.TrimEnd('/', '\\');

        public string GetDotGitDirectory() => dotGitDirectory ??= GetDotGitDirectoryInternal();

        public string GetProjectRootDirectory() => projectRootDirectory ??= GetProjectRootDirectoryInternal();

        private bool IsDynamicGitRepository => !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath);
        private string DynamicGitRepositoryPath;

        public GitPreparer(ILog log, IEnvironment environment, IOptions<Arguments> options)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Prepare(bool normalizeGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false)
        {
            var arguments = options.Value;
            var authentication = new AuthenticationInfo
            {
                Username = arguments.Authentication?.Username,
                Password = arguments.Authentication?.Password
            };
            if (!string.IsNullOrWhiteSpace(GetTargetUrl()))
            {
                var tempRepositoryPath = CalculateTemporaryRepositoryPath(GetTargetUrl(), arguments.DynamicRepositoryLocation);

                DynamicGitRepositoryPath = CreateDynamicRepository(tempRepositoryPath, authentication, GetTargetUrl(), currentBranch);
            }
            else
            {
                if (normalizeGitDirectory)
                {
                    if (shouldCleanUpRemotes)
                    {
                        CleanupDuplicateOrigin();
                    }

                    NormalizeGitDirectory(authentication, currentBranch, GetDotGitDirectoryInternal(), IsDynamicGitRepository);
                }
            }
        }

        private string GetDotGitDirectoryInternal()
        {
            var gitDirectory = IsDynamicGitRepository ? DynamicGitRepositoryPath : Repository.Discover(GetWorkingDirectory());

            gitDirectory = gitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(gitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + gitDirectory);

            return gitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(gitDirectory).FullName).FullName
                : gitDirectory;
        }

        public string GetProjectRootDirectoryInternal()
        {
            log.Info($"IsDynamicGitRepository: {IsDynamicGitRepository}");
            if (IsDynamicGitRepository)
            {
                log.Info($"Returning Project Root as {GetWorkingDirectory()}");
                return GetWorkingDirectory();
            }

            var dotGitDirectory = Repository.Discover(GetWorkingDirectory());

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            using var repo = new Repository(dotGitDirectory);
            var result = repo.Info.WorkingDirectory;
            log.Info($"Returning Project Root from DotGitDirectory: {dotGitDirectory} - {result}");
            return result;
        }

        private void CleanupDuplicateOrigin()
        {
            var remoteToKeep = DefaultRemoteName;

            using var repo = new Repository(GetDotGitDirectoryInternal());

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
                if (!Directory.Exists(targetPath))
                {
                    CloneRepository(repositoryUrl, gitDirectory, auth);
                }
                else
                {
                    log.Info("Git repository already exists");
                }
                NormalizeGitDirectory(auth, targetBranch, gitDirectory, true);
                return gitDirectory;
            }
        }

        private void NormalizeGitDirectory(AuthenticationInfo auth, string targetBranch, string gitDirectory, bool isDynamicRepository)
        {
            using (log.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
            {
                // Normalize (download branches) before using the branch
                GitRepositoryHelper.NormalizeGitDirectory(log, environment, gitDirectory, auth, options.Value.NoFetch, targetBranch, isDynamicRepository);
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
