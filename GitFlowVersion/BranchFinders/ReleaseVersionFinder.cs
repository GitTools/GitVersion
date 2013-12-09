namespace GitFlowVersion
{
    using LibGit2Sharp;

    class ReleaseVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch ReleaseBranch;

        public VersionAndBranch FindVersion()
        {
            return FindVersion(Repository, ReleaseBranch, Commit, BranchType.Release, "develop");
        }
    }
}
