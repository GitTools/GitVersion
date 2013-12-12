namespace GitFlowVersion
{
    using LibGit2Sharp;

    class FeatureVersionFinder : DevelopBasedVersionFinderBase
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch FeatureBranch;

        public VersionAndBranch FindVersion()
        {
            return FindVersion(Repository, FeatureBranch, Commit, BranchType.Feature);
        }
    }
}
