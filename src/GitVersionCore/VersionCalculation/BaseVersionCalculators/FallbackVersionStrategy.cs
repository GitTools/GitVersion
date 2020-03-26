using System.Collections.Generic;
using GitVersion.Extensions;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is 0.1.0.
    /// BaseVersionSource is the "root" commit reachable from the current commit.
    /// Does not increment.
    /// </summary>
    public class FallbackVersionStrategy : VersionStrategyBase
    {
        private readonly IRepository repository;

        public FallbackVersionStrategy(IRepository repository, IOptions<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repository = repository;
        }
        public override IEnumerable<BaseVersion> GetVersions()
        {
            Commit baseVersionSource;
            var currentBranchTip = Context.CurrentBranch.Tip;

            try
            {
                baseVersionSource = repository.GetBaseVersionSource(currentBranchTip);
            }
            catch (NotFoundException exception)
            {
                throw new GitVersionException($"Can't find commit {currentBranchTip.Sha}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
            }

            yield return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}
