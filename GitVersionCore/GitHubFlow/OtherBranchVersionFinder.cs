namespace GitVersion
{
    using System;

    class OtherBranchVersionFinder : OptionallyTaggedBranchVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            try
            {
                return FindVersion(context, BranchType.Unknown, "master");
            }
            catch (Exception)
            {
                return new SemanticVersion();
            }
        }
    }
}