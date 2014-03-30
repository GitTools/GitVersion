namespace GitVersion
{
    class HotfixVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            return FindVersion(context, BranchType.Hotfix, "master");
        }
    }
}
