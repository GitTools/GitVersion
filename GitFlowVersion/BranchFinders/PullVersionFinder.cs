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
                OlderThan = Commit.When(),
            };
            var versionFromMaster = versionOnMasterFinder
                .Execute();

            string suffix;
            if (TeamCity.IsBuildingAPullRequest())
            {
                suffix = TeamCity.CurrentPullRequestNo().ToString();
            }
            else
            {
                suffix = PullBranch.CanonicalName.Substring(PullBranch.CanonicalName.IndexOf("/pull/") + 6);
            }


            return new VersionAndBranch
            {
                BranchType = BranchType.PullRequest,
                BranchName = PullBranch.Name,
                Sha = Commit.Sha,
                Version = new SemanticVersion
                {
                    Major = versionFromMaster.Major,
                    Minor = versionFromMaster.Minor + 1,
                    Patch = 0,
                    Stability = Stability.Unstable,
                    PreReleasePartOne = 0,
                    Suffix = suffix
                }
            };
        }
    }
}