namespace GitFlowVersion
{
    using GitFlowVersion.Integration;
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
            var versionFromMaster = versionOnMasterFinder.Execute();

            string suffix = null;

            var integrationManager = IntegrationManager.Default();
            foreach (var integration in integrationManager.Integrations)
            {
                if (integration.IsBuildingPullRequest())
                {
                    suffix = integration.CurrentPullRequestNo().ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(suffix))
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