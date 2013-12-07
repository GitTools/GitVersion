namespace GitFlowVersion
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.AccessControl;
    using LibGit2Sharp;

    public class GitPreparer
    {
        public GitPreparer(string targetPath, string url, string branchName)
        {
            TargetPath = targetPath;
            Url = url;
            BranchName = branchName;
        }

        public string TargetPath { get; private set; }

        public string Url { get; private set; }

        public string BranchName { get; private set; }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public string Prepare()
        {
            if (!string.IsNullOrWhiteSpace(Url))
            {
                GetGitInfoFromUrl();
            }

            return GitDirFinder.TreeWalkForGitDir(TargetPath);
        }

        private void GetGitInfoFromUrl()
        {
            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", Url));

            string gitDirectory = Path.Combine(TargetPath, ".git");
            if (Directory.Exists(gitDirectory))
            {
                Logger.WriteInfo(string.Format("Deleting existing .git folder from '{0}' to force new checkout from url", gitDirectory));

                DeleteHelper.DeleteGitRepository(gitDirectory);
            }

            Repository.Clone(Url, TargetPath, checkout: false);

            DynamicGitRepositoryPath = gitDirectory;

            //var repositoryPath = Repository.Clone(Url, TargetPath, checkout: false);

            //if (!string.IsNullOrWhiteSpace(BranchName))
            //{
            //    using (var repository = new Repository(repositoryPath))
            //    {
            //        string targetBranchName = string.Format("refs/heads/{0}", BranchName);

            //        if (!string.Equals(repository.Head.CanonicalName, targetBranchName))
            //        {
            //            Logger.WriteInfo(string.Format("Switching to branch '{0}'", BranchName));

            //            var branch = repository.FindBranch(BranchName);
            //            if ((branch != null) && !branch.IsCurrentRepositoryHead)
            //            {
            //                repository.Refs.Add("HEAD", branch.UpstreamBranchCanonicalName, true);
            //                //var symRef = repository.Refs.Create("HEAD", string.Format("refs/heads/{0}", BranchName));
            //            }
            //        }
            //    }
            //}
        }
    }
}
