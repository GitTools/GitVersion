namespace GitVersion
{
    class ReleaseVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Release, "develop");
        }
    }
}
