namespace GitVersion
{
    class FeatureVersionFinder : DevelopBasedVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Feature);
        }
    }
}
