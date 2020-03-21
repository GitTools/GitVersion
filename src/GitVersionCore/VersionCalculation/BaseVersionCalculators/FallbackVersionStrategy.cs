using System.Collections.Generic;
using GitVersion.Exceptions;
using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.VersionCalculation
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
                var contextRepository = context.Repository;
                baseVersionSource = contextRepository.GetBaseVersionSource(currentBranchTip);
            }
            catch (NotFoundException exception)
            {
                throw new GitVersionException($"Can't find commit {currentBranchTip.Sha}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
            }

            yield return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}
