namespace GitFlowVersion
{
    class FeatureVersionFinder : DevelopBasedVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitFlowVersionContext context)
        {
            return FindVersion(context, BranchType.Feature);
        }
    }
}
