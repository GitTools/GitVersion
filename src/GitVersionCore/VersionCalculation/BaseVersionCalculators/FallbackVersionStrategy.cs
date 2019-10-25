using System.Collections.Generic;
using System.Linq;
using GitVersion.Exceptions;
using LibGit2Sharp;

namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    /// <summary>
    /// Version is 0.1.0.
    /// BaseVersionSource is the "root" commit reachable from the current commit.
    /// Does not increment.
    /// </summary>
    public class FallbackVersionStrategy : IVersionStrategy
    {
        public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            Commit baseVersionSource;
            var currentBranchTip = context.CurrentBranch.Tip;

            try
            {
                baseVersionSource = context.Repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = currentBranchTip
                }).First(c => !c.Parents.Any());
            }
            catch (NotFoundException exception)
            {
                throw new GitVersionException($"Can't find commit {currentBranchTip.Sha}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
            }

            yield return new BaseVersion(context, "Fallback base version", false, new SemanticVersion(minor : 1), baseVersionSource, null);
        }
    }
}
