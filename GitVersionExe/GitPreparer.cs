namespace GitVersion
{
    using System;
    using System.IO;
    using System.Linq;
    using LibGit2Sharp;

    public class GitPreparer
    {
        Arguments arguments;

        public GitPreparer(Arguments arguments)
        {
            this.arguments = arguments;
        }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public void InitialiseDynamicRepositoryIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(arguments.TargetUrl)) return;

            var targetPath = CalculateTemporaryRepositoryPath(arguments.TargetUrl, arguments.DynamicRepositoryLocation);
            DynamicGitRepositoryPath = CreateDynamicRepository(targetPath, arguments.Authentication, arguments.TargetUrl, arguments.TargetBranch, arguments.NoFetch);
        }

        string CalculateTemporaryRepositoryPath(string targetUrl, string dynamicRepositoryLocation)
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

            return GitDirFinder.TreeWalkForDotGitDir(arguments.TargetPath);
        }

        static string CreateDynamicRepository(string targetPath, Authentication authentication, string repositoryUrl, string targetBranch, bool noFetch)
        {
            var gitDirectory = Path.Combine(targetPath, ".git");
            if (Directory.Exists(targetPath))
            {
                Logger.WriteInfo(string.Format("Git repository already exists at {0}", targetPath));
                GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch);
                Logger.WriteInfo(string.Format("Updating branch '{0}'", targetBranch));
                using (var repo = new Repository(targetPath))
                {
                    var targetGitBranch = repo.Branches[targetBranch];
                    var trackedBranch = targetGitBranch.TrackedBranch;
                    if (trackedBranch == null)
                        throw new InvalidOperationException(string.Format("Expecting {0} to have a remote tracking branch", targetBranch));
                    
                    targetGitBranch.Checkout();
                    repo.Reset(ResetMode.Hard, trackedBranch.Tip);
                }

                return gitDirectory;
            }

            Credentials credentials = null;
            if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", authentication.Username));

                credentials = new UsernamePasswordCredentials
                {
                    Username = authentication.Username,
                    Password = authentication.Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", repositoryUrl));

            Repository.Clone(repositoryUrl, gitDirectory,
                new CloneOptions
                {
                    Checkout = false,
                    CredentialsProvider = (url, usernameFromUrl, types) => credentials
                });

            // Normalize (download branches) before using the branch
            GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch);

            using (var repository = new Repository(gitDirectory))
            {
                if (string.IsNullOrWhiteSpace(targetBranch))
                {
                    targetBranch = repository.Head.Name;
                }

                Reference newHead = null;

                var localReference = GetLocalReference(repository, targetBranch);
                if (localReference != null)
                {
                    newHead = localReference;
                }

                if (newHead == null)
                {
                    var remoteReference = GetRemoteReference(repository, targetBranch, repositoryUrl);
                    if (remoteReference != null)
                    {
                        repository.Network.Fetch(repositoryUrl, new[]
                            {
                                string.Format("{0}:{1}", remoteReference.CanonicalName, targetBranch)
                            });

                        newHead = repository.Refs[string.Format("refs/heads/{0}", targetBranch)];
                    }
                }

                if (newHead != null)
                {
                    Logger.WriteInfo(string.Format("Switching to branch '{0}'", targetBranch));

                    repository.Refs.UpdateTarget(repository.Refs.Head, newHead);
                }
            }

            return gitDirectory;
        }

        private static Reference GetLocalReference(Repository repository, string branchName)
        {
            var targetBranchName = branchName.GetCanonicalBranchName();

            return repository.Refs.FirstOrDefault(localRef => string.Equals(localRef.CanonicalName, targetBranchName));
        }

        private static DirectReference GetRemoteReference(Repository repository, string branchName, string repositoryUrl)
        {
            var targetBranchName = branchName.GetCanonicalBranchName();
            var remoteReferences = repository.Network.ListReferences(repositoryUrl);

            return remoteReferences.FirstOrDefault(remoteRef => string.Equals(remoteRef.CanonicalName, targetBranchName));
        }
    }
}