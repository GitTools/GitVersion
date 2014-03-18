namespace GitVersion
{
    using System.IO;
    using LibGit2Sharp;

    public class GitPreparer
    {
        readonly Arguments _arguments;

        public GitPreparer(Arguments arguments)
        {
            _arguments = arguments;
        }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public string Prepare()
        {
            var gitPath = _arguments.TargetPath;

            if (!string.IsNullOrWhiteSpace(_arguments.TargetUrl))
            {
                gitPath = GetGitInfoFromUrl();
            }

            return GitDirFinder.TreeWalkForGitDir(gitPath);
        }

        private string GetGitInfoFromUrl()
        {
            var gitDirectory = Path.Combine(_arguments.TargetPath, "_dynamicrepository", ".git");
            if (Directory.Exists(gitDirectory))
            {
                Logger.WriteInfo(string.Format("Deleting existing .git folder from '{0}' to force new checkout from url", gitDirectory));

                DeleteHelper.DeleteGitRepository(gitDirectory);
            }

            Credentials credentials = null;
            if (!string.IsNullOrWhiteSpace(_arguments.Username) && !string.IsNullOrWhiteSpace(_arguments.Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", _arguments.Username));

                credentials = new Credentials()
                {
                    Username = _arguments.Username,
                    Password = _arguments.Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", _arguments.TargetUrl));

            Repository.Clone(_arguments.TargetUrl, gitDirectory, checkout: false, credentials: credentials);

            if (!string.IsNullOrWhiteSpace(_arguments.TargetBranch))
            {
                // Normalize (download branches) before using the branch
                GitHelper.NormalizeGitDirectory(gitDirectory, _arguments);

                using (var repository = new Repository(gitDirectory))
                {
                    var targetBranchName = string.Format("refs/heads/{0}", _arguments.TargetBranch);
                    if (!string.Equals(repository.Head.CanonicalName, targetBranchName))
                    {
                        Logger.WriteInfo(string.Format("Switching to branch '{0}'", _arguments.TargetBranch));

                        repository.Refs.UpdateTarget("HEAD", targetBranchName);
                    }
                }
            }

            DynamicGitRepositoryPath = gitDirectory;

            return gitDirectory;
        }
    }
}