namespace GitVersion
{
    class MasterReleaseVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Release, "master");
        }
    }
}