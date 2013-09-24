namespace GitFlowVersion
{
    using LibGit2Sharp;

    class PullVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch PullBranch;

        public VersionAndBranch FindVersion()
        {
            var versionOnMasterFinder = new VersionOnMasterFinder
                                        {
                                            Repository = Repository,
                                          OlderThan  = Commit.When(),
                                        };
            var versionFromMaster = versionOnMasterFinder
                .Execute();

            var version = versionFromMaster.Version;
            version.Minor++;
            version.Patch = 0;
            version.Stability = Stability.Unstable;

            if (TeamCity.IsBuildingAPullRequest())
            {
                version.Suffix = TeamCity.CurrentPullRequestNo().ToString();
            }
            else
            {
                version.Suffix = PullBranch.CanonicalName
                                           .Substring(PullBranch.CanonicalName.IndexOf("/pull/") + 6);
            }
            version.PreReleaseNumber = 0;

            return new VersionAndBranch
                   {
                       BranchType = BranchType.PullRequest,
                       BranchName = PullBranch.Name,
                       Sha = Commit.Sha,
                       Version = version
                   };
        }
    }
}