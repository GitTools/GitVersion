using System;
using System.IO;
using System.Linq;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    public class GitPreparer
    {
        private string targetUrl;
        private string dynamicRepositoryLocation;
        private AuthenticationInfo authentication;
        private bool noFetch;
        private string targetPath;

        private const string defaultRemoteName = "origin";

        public GitPreparer(string targetPath) : this(null, null, null, false, targetPath) { }

        public GitPreparer(Arguments arguments) : this(arguments.TargetUrl, arguments.DynamicRepositoryLocation, arguments.Authentication, arguments.NoFetch, arguments.TargetPath)
        {

        }

        public GitPreparer(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, bool noFetch, string targetPath)
        {
            this.targetUrl = targetUrl;
            this.dynamicRepositoryLocation = dynamicRepositoryLocation;
            this.authentication =
                new AuthenticationInfo
                {
                    Username = authentication?.Username,
                    Password = authentication?.Password
                };
            this.noFetch = noFetch;
            this.targetPath = targetPath.TrimEnd('/', '\\');
        }

        public string TargetUrl => targetUrl;

        public string WorkingDirectory => targetPath;

        public bool IsDynamicGitRepository => !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath);

        public string DynamicGitRepositoryPath { get; private set; }

        public void Initialise(bool normaliseGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                if (normaliseGitDirectory)
                {
                    using (Logger.IndentLog($"Normalizing git directory for branch '{currentBranch}'"))
                    {
                        if (shouldCleanUpRemotes)
                        {
                            CleanupDuplicateOrigin();
                        }
                        GitRepositoryHelper.NormalizeGitDirectory(GetDotGitDirectory(), authentication, noFetch, currentBranch, IsDynamicGitRepository);
                    }
                }
                return;
            }

            var tempRepositoryPath = CalculateTemporaryRepositoryPath(targetUrl, dynamicRepositoryLocation);

            DynamicGitRepositoryPath = CreateDynamicRepository(tempRepositoryPath, authentication, targetUrl, currentBranch, noFetch);
        }

        private void CleanupDuplicateOrigin()
        {
            var remoteToKeep = defaultRemoteName;

            var repo = new Repository(GetDotGitDirectory());

            // check that we have a remote that matches defaultRemoteName if not take the first remote
            if (!repo.Network.Remotes.Any(remote => remote.Name.Equals(defaultRemoteName, StringComparison.InvariantCultureIgnoreCase)))
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

        public TResult WithRepository<TResult>(Func<IRepository, TResult> action)
        {
            using (IRepository repo = new Repository(GetDotGitDirectory()))
            {
                return action(repo);
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
                using (var repository = new Repository(possiblePath))
                {
                    return repository.Network.Remotes.Any(r => r.Url == targetUrl);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetDotGitDirectory()
        {
            var dotGitDirectory = IsDynamicGitRepository ? DynamicGitRepositoryPath : Repository.Discover(targetPath);

            dotGitDirectory = dotGitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + targetPath);

            if (dotGitDirectory.Contains(Path.Combine(".git", "worktrees")))
                return Directory.GetParent(Directory.GetParent(dotGitDirectory).FullName).FullName;

            return dotGitDirectory;
        }

        public string GetProjectRootDirectory()
        {
            Logger.Info($"IsDynamicGitRepository: {IsDynamicGitRepository}");
            if (IsDynamicGitRepository)
            {
                Logger.Info($"Returning Project Root as {targetPath}");
                return targetPath;
            }

            var dotGitDirectory = Repository.Discover(targetPath);

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {targetPath}");

            using (var repo = new Repository(dotGitDirectory))
            {
                var result = repo.Info.WorkingDirectory;
                Logger.Info($"Returning Project Root from DotGitDirectory: {dotGitDirectory} - {result}");
                return result;
            }
        }

        private static string CreateDynamicRepository(string targetPath, AuthenticationInfo authentication, string repositoryUrl, string targetBranch, bool noFetch)
        {
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }

            using (Logger.IndentLog($"Creating dynamic repository at '{targetPath}'"))
            {
                var gitDirectory = Path.Combine(targetPath, ".git");
                if (Directory.Exists(targetPath))
                {
                    Logger.Info("Git repository already exists");
                    using (Logger.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
                    {
                        GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch, true);
                    }

                    return gitDirectory;
                }

                CloneRepository(repositoryUrl, gitDirectory, authentication);

                using (Logger.IndentLog($"Normalizing git directory for branch '{targetBranch}'"))
                {
                    // Normalize (download branches) before using the branch
                    GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch, true);
                }

                return gitDirectory;
            }
        }

        private static void CloneRepository(string repositoryUrl, string gitDirectory, AuthenticationInfo authentication)
        {
            Credentials credentials = null;

            if (authentication != null)
            {
                if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
                {
                    Logger.Info($"Setting up credentials using name '{authentication.Username}'");

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = authentication.Username,
                        Password = authentication.Password
                    };
                }
            }


            try
            {
                using (Logger.IndentLog($"Cloning repository from url '{repositoryUrl}'"))
                {
                    var cloneOptions = new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    };

                    var returnedPath = Repository.Clone(repositoryUrl, gitDirectory, cloneOptions);
                    Logger.Info($"Returned path after repository clone: {returnedPath}");
                }
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new Exception("Unauthorised: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new Exception("Forbidden: Possbily Incorrect username/password");
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
