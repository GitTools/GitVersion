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
            var gitDirectory = Path.Combine(arguments.TargetPath, "_dynamicrepository", ".git");
            if (Directory.Exists(gitDirectory))
            {
                Logger.WriteInfo(string.Format("Deleting existing .git folder from '{0}' to force new checkout from url", gitDirectory));

                DeleteHelper.DeleteGitRepository(gitDirectory);
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
                new CloneOptions { IsBare = true, Checkout = false, Credentials = credentials });

            if (!string.IsNullOrWhiteSpace(arguments.TargetBranch))
            {
                // Normalize (download branches) before using the branch
                GitHelper.NormalizeGitDirectory(gitDirectory, arguments.Authentication);

                using (var repository = new Repository(gitDirectory))
                {
                    var newHead = GetReference(repository, arguments.TargetBranch, arguments.TargetUrl);
                    if (newHead != null)
                    {
                        var directReference = newHead as DirectReference;
                        if (directReference != null)
                        {
                            repository.Network.Fetch(arguments.TargetUrl, new[]
                            {
                                string.Format("{0}:{1}", directReference.CanonicalName, arguments.TargetBranch) //refs/head/gitversion_dynamic")
                            });

                            newHead = repository.Refs[string.Format("refs/heads/{0}", arguments.TargetBranch)];
                        }

                        repository.Refs.UpdateTarget(repository.Refs.Head, newHead);
                    }

                    repository.CheckoutFilesIfExist("NextVersion.txt");
                }
            }

            DynamicGitRepositoryPath = gitDirectory;

            return gitDirectory;
        }

        private static Reference GetReference(Repository repository, string branchName, string repositoryUrl)
        {
            var targetBranchName = branchName.GetCanonicalBranchName();
            foreach (var localRef in repository.Refs)
            {
                if (string.Equals(localRef.CanonicalName, targetBranchName))
                {
                    return localRef;
                }
            }

            var remoteReferences = repository.Network.ListReferences(repositoryUrl);
            foreach (var remoteRef in remoteReferences)
            {
                if (string.Equals(remoteRef.CanonicalName, targetBranchName))
                {
                    return remoteRef;
                }
            }

            return null;
        }
    }
}