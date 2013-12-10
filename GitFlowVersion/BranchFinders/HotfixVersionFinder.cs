namespace GitFlowVersion
{
    using LibGit2Sharp;

    class HotfixVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public Commit Commit;
        public Branch HotfixBranch;
        public IRepository Repository;

        public VersionAndBranch FindVersion()
        {
            return FindVersion(Repository, HotfixBranch, Commit, BranchType.Hotfix, "master");
        }
    }
}
