namespace GitFlowVersion
{
    class ReleaseVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitFlowVersionContext context)
        {
            return FindVersion(context, BranchType.Release, "develop");
        }
    }
}
