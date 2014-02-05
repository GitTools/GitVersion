namespace GitFlowVersion
{
    class ReleaseVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Release, "develop");
        }
    }
}
