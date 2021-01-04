using System;
using System.Collections.Generic;
using GitVersion.Common;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is 0.1.0.
    /// BaseVersionSource is the "root" commit reachable from the current commit.
    /// Does not increment.
    /// </summary>
    public class FallbackVersionStrategy : VersionStrategyBase
    {
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;

        public FallbackVersionStrategy(IRepositoryMetadataProvider repositoryMetadataProvider, Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repositoryMetadataProvider = repositoryMetadataProvider;
        }
        public override IEnumerable<BaseVersion> GetVersions()
        {
            var currentBranchTip = Context.CurrentBranch.Tip;
            if (currentBranchTip == null)
            {
                throw new GitVersionException("No commits found on the current branch.");
            }

            var baseVersionSource = repositoryMetadataProvider.GetBaseVersionSource(currentBranchTip);

            yield return new BaseVersion("Fallback base version", false, new SemanticVersion(minor: 1), baseVersionSource, null);
        }
    }
}
