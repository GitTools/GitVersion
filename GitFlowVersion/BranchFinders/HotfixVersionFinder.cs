namespace GitFlowVersion
{
    class HotfixVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitFlowVersionContext context)
        {
            return FindVersion(context, BranchType.Hotfix, "master");
        }
    }
}
