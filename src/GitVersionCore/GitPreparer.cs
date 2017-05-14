namespace GitVersion
{
    using System;
    using System.IO;
    using System.Linq;
    using GitTools.Git;
    using GitTools.Logging;
    using LibGit2Sharp;

    public class GitPreparer
    {
        string targetUrl;
        string dynamicRepositoryLocation;
        AuthenticationInfo authentication;
        bool noFetch;
        string targetPath;

        public GitPreparer(string targetPath) : this(null, null, null, false, targetPath) { }
        public GitPreparer(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, bool noFetch, string targetPath)
        {
            this.targetUrl = targetUrl;
            this.dynamicRepositoryLocation = dynamicRepositoryLocation;
            this.authentication = authentication == null ?
                null :
                new AuthenticationInfo
                {
                    Username = authentication.Username,
                    Password = authentication.Password
                };
            this.noFetch = noFetch;
            this.targetPath = targetPath.TrimEnd('/', '\\');

            // GitTools has its own logging. So that it actually outputs something, it needs to be initialized.
            LogProvider.SetCurrentLogProvider(new LoggerWrapper());
        }

        public string TargetUrl
        {
            get { return targetUrl; }
        }

        public string WorkingDirectory
        {
            get { return targetPath; }
        }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public void Initialise(bool normaliseGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                if (normaliseGitDirectory)
                {
                    using (Logger.IndentLog(string.Format("Normalizing git directory for branch '{0}'", currentBranch)))
                    {
                        if (shouldCleanUpRemotes)
                        {
                            CleanupDuplicateOrigin();
                        }
                        GitRepositoryHelper.NormalizeGitDirectory(GetDotGitDirectory(), authentication, noFetch, currentBranch);
                    }
                }
                return;
            }

            var tempRepositoryPath = CalculateTemporaryRepositoryPath(targetUrl, dynamicRepositoryLocation);

            DynamicGitRepositoryPath = CreateDynamicRepository(tempRepositoryPath, authentication, targetUrl, currentBranch, noFetch);
        }

        private void CleanupDuplicateOrigin()
        {
            var repo = new Repository(GetDotGitDirectory());
            if (repo.Network.Remotes.Any(remote => remote.Name == "origin1"))
            {
                repo.Network.Remotes.Remove("origin1");
            }
        }

        public TResult WithRepository<TResult>(Func<IRepository, TResult> action)
        {
            using (IRepository repo = new Repository(GetDotGitDirectory()))
            {
                return action(repo);
            }
        }

        static string CalculateTemporaryRepositoryPath(string targetUrl, string dynamicRepositoryLocation)
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

        static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
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
            if (IsDynamicGitRepository)
                return DynamicGitRepositoryPath;

            var dotGitDirectory = Repository.Discover(targetPath);

            if (String.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + targetPath);

            dotGitDirectory = dotGitDirectory.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + targetPath);

            return dotGitDirectory;
        }

        public string GetProjectRootDirectory()
        {
            Logger.WriteInfo(string.Format("IsDynamicGitRepository: {0}", IsDynamicGitRepository));
            if (IsDynamicGitRepository)
            {
                Logger.WriteInfo(string.Format("Returning Project Root as {0}", targetPath));
                return targetPath;
            }

            var dotGetGitDirectory = GetDotGitDirectory();
            var result = Directory.GetParent(dotGetGitDirectory).FullName;
            Logger.WriteInfo(string.Format("Returning Project Root from DotGitDirectory: {0} - {1}", dotGetGitDirectory, result));
            return result;
        }

        static string CreateDynamicRepository(string targetPath, AuthenticationInfo authentication, string repositoryUrl, string targetBranch, bool noFetch)
        {
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }

            using (Logger.IndentLog(string.Format("Creating dynamic repository at '{0}'", targetPath)))
            {
                var gitDirectory = Path.Combine(targetPath, ".git");
                if (Directory.Exists(targetPath))
                {
                    Logger.WriteInfo("Git repository already exists");
                    using (Logger.IndentLog(string.Format("Normalizing git directory for branch '{0}'", targetBranch)))
                    {
                        GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch);
                    }

                    return gitDirectory;
                }

                CloneRepository(repositoryUrl, gitDirectory, authentication);

                using (Logger.IndentLog(string.Format("Normalizing git directory for branch '{0}'", targetBranch)))
                {
                    // Normalize (download branches) before using the branch
                    GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch);
                }

                return gitDirectory;
            }
        }

        static void CloneRepository(string repositoryUrl, string gitDirectory, AuthenticationInfo authentication)
        {
            Credentials credentials = null;

            if (authentication != null)
            {
                if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
                {
                    Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", authentication.Username));

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = authentication.Username,
                        Password = authentication.Password
                    };
                }
            }


            try
            {
                using (Logger.IndentLog(string.Format("Cloning repository from url '{0}'", repositoryUrl)))
                {
                    var cloneOptions = new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    };

                    var returnedPath = Repository.Clone(repositoryUrl, gitDirectory, cloneOptions);
                    Logger.WriteInfo(string.Format("Returned path after repository clone: {0}", returnedPath));
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