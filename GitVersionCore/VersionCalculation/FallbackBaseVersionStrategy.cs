namespace GitVersion.VersionCalculation
{
    using System.Linq;
    using BaseVersionCalculators;
    using LibGit2Sharp;

    public class FallbackBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            var baseVersionSource = context.Repository.Commits.QueryBy(new CommitFilter
            {
                Since = context.CurrentBranch.Tip
            }).First(c => !c.Parents.Any());
            return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}