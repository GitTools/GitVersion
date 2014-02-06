namespace GitFlowVersion
{
    class FeatureVersionFinder : DevelopBasedVersionFinderBase
    {
        public VersionAndBranch FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Feature);
        }
    }
}
