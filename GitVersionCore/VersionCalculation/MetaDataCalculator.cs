namespace GitVersion.VersionCalculation
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class MetaDataCalculator : IMetaDataCalculator
    {
        public SemanticVersionBuildMetaData Create(DateTimeOffset? baseVersionWhenFrom, GitVersionContext context)
        {
            var qf = new CommitFilter
            {
                Since = baseVersionWhenFrom,
                Until = context.CurrentCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return new SemanticVersionBuildMetaData(
                context.Repository.Commits.QueryBy(qf).Count(),
                context.CurrentBranch.Name,
                context.CurrentCommit.Sha,
                context.CurrentCommit.When());
        }
    }
}