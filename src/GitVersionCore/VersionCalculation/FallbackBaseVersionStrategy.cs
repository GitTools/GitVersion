namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using BaseVersionCalculators;
    using LibGit2Sharp;

    /// <summary>
    /// Version is 0.1.0.
    /// BaseVersionSource is the "root" commit reachable from the current commit.
    /// Does not increment.
    /// </summary>
    public class FallbackBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var baseVersionSource = context.Repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = context.CurrentBranch.Tip
            }).First(c => !c.Parents.Any());
            yield return new BaseVersion(context, "Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}