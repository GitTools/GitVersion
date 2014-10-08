namespace GitVersion
{
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

        public string Prepare()
        {
            var gitPath = arguments.TargetPath;

            if (!string.IsNullOrWhiteSpace(arguments.TargetUrl))
            {
                gitPath = GetGitInfoFromUrl();
            }

            return GitDirFinder.TreeWalkForGitDir(gitPath);
        }

        string GetGitInfoFromUrl()
        {
            var gitRootDirectory = Path.Combine(arguments.TargetPath, "_dynamicrepository");
            var gitDirectory = Path.Combine(gitRootDirectory, ".git");
            if (Directory.Exists(gitRootDirectory))
            {
                Logger.WriteInfo(string.Format("Deleting existing .git folder from '{0}' to force new checkout from url", gitRootDirectory));

                DeleteHelper.DeleteGitRepository(gitRootDirectory);
            }

            Credentials credentials = null;
            var authentication = arguments.Authentication;
            if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", authentication.Username));

                credentials = new UsernamePasswordCredentials
                {
                    Username = authentication.Username,
                    Password = authentication.Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", arguments.TargetUrl));

            Repository.Clone(arguments.TargetUrl, gitDirectory,
                new CloneOptions
                {
                    IsBare = true, 
                    Checkout = false,
                    CredentialsProvider = (url, usernameFromUrl, types) => credentials
                });

            if (!string.IsNullOrWhiteSpace(arguments.TargetBranch))
            {
                // Normalize (download branches) before using the branch
                GitHelper.NormalizeGitDirectory(gitDirectory, arguments.Authentication);

                using (var repository = new Repository(gitDirectory))
                {
                    Reference newHead = null;

                    var localReference = GetLocalReference(repository, arguments.TargetBranch);
                    if (localReference != null)
                    {
                        newHead = localReference;
                    }

                    if (newHead == null)
                    {
                        var remoteReference = GetRemoteReference(repository, arguments.TargetBranch, arguments.TargetUrl);
                        if (remoteReference != null)
                        {
                            repository.Network.Fetch(arguments.TargetUrl, new[]
                            {
                                string.Format("{0}:{1}", remoteReference.CanonicalName, arguments.TargetBranch)
                            });

                            newHead = repository.Refs[string.Format("refs/heads/{0}", arguments.TargetBranch)];
                        }
                    }

                    if (newHead != null)
                    {
                        Logger.WriteInfo(string.Format("Switching to branch '{0}'", arguments.TargetBranch));

                        repository.Refs.UpdateTarget(repository.Refs.Head, newHead);
                    }

                    repository.CheckoutFilesIfExist("NextVersion.txt");
                }
            }

            DynamicGitRepositoryPath = gitDirectory;

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