namespace GitFlowVersion
{
    using LibGit2Sharp;

    class PullVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch PullBranch;

        public VersionInformation FindVersion()
        {
            var versionFromMaster = Repository
                .MasterVersionPriorTo(Commit.When());

            var version = VersionInformation.FromMajorMinorPatch(versionFromMaster.Version);
            version.Minor++;
            version.Patch = 0;
            version.Stability = Stability.Unstable;
            version.BranchType = BranchType.PullRequest;


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
            version.BranchName = PullBranch.Name;
            version.Sha = Commit.Sha;

            return version;
        }
    }
}