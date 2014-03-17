namespace GitVersion
{
    using System.IO;
    using LibGit2Sharp;

    public class GitPreparer
    {
        public GitPreparer(string targetPath, string url, string branchName, string username, string password)
        {
            TargetPath = targetPath;
            Url = url;
            BranchName = branchName;
            Username = username;
            Password = password;
        }

        public string TargetPath { get; private set; }

        public string Url { get; private set; }

        public string BranchName { get; private set; }

        public string Username { get; private set; }

        public string Password { get; private set; }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public string Prepare()
        {
            var gitPath = TargetPath;

            if (!string.IsNullOrWhiteSpace(Url))
            {
                gitPath = GetGitInfoFromUrl();
            }

            return GitDirFinder.TreeWalkForGitDir(gitPath);
        }

        private string GetGitInfoFromUrl()
        {
            var gitDirectory = Path.Combine(TargetPath, "_dynamicrepository", ".git");
            if (Directory.Exists(gitDirectory))
            {
                Logger.WriteInfo(string.Format("Deleting existing .git folder from '{0}' to force new checkout from url", gitDirectory));

                DeleteHelper.DeleteGitRepository(gitDirectory);
            }

            Credentials credentials = null;
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", Username));

                credentials = new Credentials()
                {
                    Username = Username,
                    Password = Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", Url));

            Repository.Clone(Url, gitDirectory, checkout: false, credentials: credentials);

            if (!string.IsNullOrWhiteSpace(BranchName))
            {
                // Normalize (download branches) before using the branch
                GitHelper.NormalizeGitDirectory(gitDirectory);

                using (var repository = new Repository(gitDirectory))
                {
                    var targetBranchName = string.Format("refs/heads/{0}", BranchName);
                    if (!string.Equals(repository.Head.CanonicalName, targetBranchName))
                    {
                        Logger.WriteInfo(string.Format("Switching to branch '{0}'", BranchName));

                        var branch = repository.FindBranch(BranchName);
                        if ((branch != null) && !branch.IsCurrentRepositoryHead)
                        {
                            var finalName = string.Format("refs/heads/{0}", BranchName);
                            //repository.Refs.Add("HEAD", branch.UpstreamBranchCanonicalName, true);

                            repository.Refs.Add("HEAD", finalName, true);

                            //var symRef = repository.Refs.Create("HEAD", string.Format("refs/heads/{0}", BranchName));
                        }
                    }
                }
            }

            DynamicGitRepositoryPath = gitDirectory;

            return gitDirectory;
        }
    }
}