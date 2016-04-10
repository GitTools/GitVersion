namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using BaseVersionCalculators;
    using LibGit2Sharp;

    public class FallbackBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var baseVersionSource = context.Repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = context.CurrentBranch.Tip
            }).First(c => !c.Parents.Any());
            yield return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}