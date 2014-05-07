namespace GitVersion
{
    using System.IO;
    using LibGit2Sharp;

    public class GitPreparer
    {
        readonly Arguments arguments;

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
            if (!string.IsNullOrWhiteSpace(arguments.Username) && !string.IsNullOrWhiteSpace(arguments.Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", arguments.Username));

                credentials = new UsernamePasswordCredentials
                    {
                    Username = arguments.Username,
                    Password = arguments.Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", arguments.TargetUrl));

            Repository.Clone(arguments.TargetUrl, gitDirectory, true, false, credentials: credentials);

            if (!string.IsNullOrWhiteSpace(arguments.TargetBranch))
            {
                // Normalize (download branches) before using the branch
                GitHelper.NormalizeGitDirectory(gitDirectory, arguments);

                using (var repository = new Repository(gitDirectory))
                {
                    var targetBranchName = string.Format("refs/heads/{0}", arguments.TargetBranch);
                    if (!string.Equals(repository.Head.CanonicalName, targetBranchName))
                    {
                        Logger.WriteInfo(string.Format("Switching to branch '{0}'", arguments.TargetBranch));

                        repository.Refs.UpdateTarget("HEAD", targetBranchName);
                    }
                }
            }

            DynamicGitRepositoryPath = gitDirectory;

            return gitDirectory;
        }
    }
}