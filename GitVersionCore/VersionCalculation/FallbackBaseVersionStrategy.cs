namespace GitVersion.VersionCalculation
{
    using System.Linq;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class FallbackBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), context.CurrentBranch.Commits.Last(), null);
        }
    }
}