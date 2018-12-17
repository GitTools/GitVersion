namespace GitTools.Git
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using LibGit2Sharp;
    using Logging;

    public static class DynamicRepositories
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(DynamicRepositories));

        /// <summary>
        /// Creates a dynamic repository based on the repository info
        /// </summary>
        /// <param name="repositoryInfo">The source repository information.</param>
        /// <param name="dynamicRepsitoryPath">The path to create the dynamic repository, NOT thread safe.</param>
        /// <param name="targetBranch"></param>
        /// <param name="targetCommit"></param>
        /// <returns>The git repository.</returns>
        public static DynamicRepository CreateOrOpen(RepositoryInfo repositoryInfo, string dynamicRepsitoryPath, string targetBranch, string targetCommit)
        {
            if (string.IsNullOrWhiteSpace(dynamicRepsitoryPath) || !Directory.Exists(dynamicRepsitoryPath))
                throw new GitToolsException(string.Format("Dynamic repository path {0} does not exist, ensure it is created before trying to create dynamic repository.", dynamicRepsitoryPath));
            if (string.IsNullOrWhiteSpace(targetBranch))
                throw new GitToolsException("Dynamic Git repositories must have a target branch");
            if (string.IsNullOrWhiteSpace(targetCommit))
                throw new GitToolsException("Dynamic Git repositories must have a target commit");

            var tempRepositoryPath = GetAndLockTemporaryRepositoryPath(repositoryInfo.Url, dynamicRepsitoryPath);
            var dynamicRepositoryPath = CreateDynamicRepository(tempRepositoryPath, repositoryInfo, targetBranch, targetCommit);

            return new DynamicRepository(new Repository(dynamicRepositoryPath), () => ReleaseDynamicRepoLock(tempRepositoryPath));
        }

        static void ReleaseDynamicRepoLock(string repoPath)
        {
            var lockFile = GetLockFile(repoPath);
            try
            {
                File.Delete(lockFile);
            }
            catch (Exception ex)
            {
                throw new GitToolsException(string.Format("Failed to delete dynamic repository lock file '{0}', this dynamic repository will not be used until the lock file is removed", lockFile), ex);
            }
        }

        static bool TakeDynamicRepoLock(string possibleDynamicRepoPath)
        {
            try
            {
                // Ensure directory exists
                try { Directory.CreateDirectory(possibleDynamicRepoPath); } catch (IOException) { }
                // Check if file exists and create lock file in a safe way
                using (new FileStream(GetLockFile(possibleDynamicRepoPath), FileMode.CreateNew)) { }
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        static string GetLockFile(string repoPath)
        {
            return Path.Combine(repoPath, "dynamicrepository.lock");
        }

        static string GetAndLockTemporaryRepositoryPath(string targetUrl, string dynamicRepositoryLocation)
        {
            var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
            var possiblePath = Path.Combine(dynamicRepositoryLocation, repositoryName);

            var i = 1;
            var originalPath = possiblePath;
            var possiblePathExists = Directory.Exists(possiblePath);
            if (VerifyDynamicRepositoryTarget(targetUrl, possiblePathExists, possiblePath)) return possiblePath;
            do
            {
                if (i > 10)
                {
                    throw new GitToolsException(string.Format(
                        "Failed to find a dynamic repository path at {0} -> {1}",
                        originalPath,
                        possiblePath));
                }
                possiblePath = string.Concat(originalPath, "_", i++.ToString());
                possiblePathExists = Directory.Exists(possiblePath);
            } while (!VerifyDynamicRepositoryTarget(targetUrl, possiblePathExists, possiblePath));

            return possiblePath;
        }

        static bool VerifyDynamicRepositoryTarget(string targetUrl, bool possiblePathExists, string possiblePath)
        {
            // First take a lock on that path
            var lockTaken = TakeDynamicRepoLock(possiblePath);
            if (!lockTaken) return false;

            if (!possiblePathExists) return true;

            // Then verify it's suitable
            if (!GitRepoHasMatchingRemote(possiblePath, targetUrl))
            {
                // Release lock if not suitable
                ReleaseDynamicRepoLock(possiblePath);
                return false;
            }
            return true;
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

        [SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
        [SuppressMessage("ReSharper", "ArgumentsStyleNamedExpression")]
        static string CreateDynamicRepository(string targetPath, RepositoryInfo repositoryInfo, string targetBranch, string targetCommit)
        {
            Log.Info(string.Format("Creating dynamic repository at '{0}'", targetPath));

            var gitDirectory = Path.Combine(targetPath, ".git");
            if (Directory.Exists(gitDirectory))
            {
                Log.Info("Git repository already exists");
                using (var repo = new Repository(gitDirectory))
                {
                    // We need to fetch before we can checkout the commit
                    var remote = GitRepositoryHelper.EnsureOnlyOneRemoteIsDefined(repo);
                    GitRepositoryHelper.Fetch(repositoryInfo.Authentication, remote, repo);
                    CheckoutCommit(repo, targetCommit);
                }
                GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, repositoryInfo.Authentication, noFetch: true, currentBranch: targetBranch);

                return gitDirectory;
            }

            CloneRepository(repositoryInfo.Url, gitDirectory, repositoryInfo.Authentication);

            using (var repo = new Repository(gitDirectory))
            {
                CheckoutCommit(repo, targetCommit);
            }

            // Normalize (download branches) before using the branch
            GitRepositoryHelper.NormalizeGitDirectory(gitDirectory, repositoryInfo.Authentication, noFetch: true, currentBranch: targetBranch);

            return gitDirectory;
        }

        static void CheckoutCommit(IRepository repo, string targetCommit)
        {
            Log.Info(string.Format("Checking out {0}", targetCommit));
            Commands.Checkout(repo, targetCommit);
        }

        static void CloneRepository(string repositoryUrl, string gitDirectory, AuthenticationInfo authentication)
        {
            Credentials credentials = null;

            if (authentication != null)
            {
                if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
                {
                    Log.Info(string.Format("Setting up credentials using name '{0}'", authentication.Username));

                    credentials = new UsernamePasswordCredentials
                    {
                        Username = authentication.Username,
                        Password = authentication.Password
                    };
                }
            }

            Log.Info(string.Format("Retrieving git info from url '{0}'", repositoryUrl));

            try
            {
                Repository.Clone(repositoryUrl, gitDirectory,
                    new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    });
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new GitToolsException("Unauthorised: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new GitToolsException("Forbidden: Possbily Incorrect username/password");
                }
                if (message.Contains("404"))
                {
                    throw new GitToolsException("Not found: The repository was not found");
                }

                throw new GitToolsException("There was an unknown problem with the Git repository you provided");
            }
        }
    }
}