namespace GitFlowVersion
{
    class HotfixVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Hotfix, "master");
        }
    }
}
