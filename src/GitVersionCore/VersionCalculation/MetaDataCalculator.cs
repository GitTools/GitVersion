using System;
using System.Linq;
using LibGit2Sharp;
using GitVersion.Logging;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation
{
    public class MetaDataCalculator : IMetaDataCalculator
    {
        private readonly ILog log;

        public MetaDataCalculator(ILog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SemanticVersionBuildMetaData Create(Commit baseVersionSource, GitVersionContext context)
        {
            var commitLog = context.Repository.GetCommitLog(baseVersionSource, context.CurrentCommit);
            var commitsSinceTag = commitLog.Count();
            log.Info($"{commitsSinceTag} commits found between {baseVersionSource.Sha} and {context.CurrentCommit.Sha}");

            var shortSha = context.Repository.ObjectDatabase.ShortenObjectId(context.CurrentCommit);
            return new SemanticVersionBuildMetaData(
                baseVersionSource.Sha,
                commitsSinceTag,
                context.CurrentBranch.FriendlyName,
                context.CurrentCommit.Sha,
                shortSha,
                context.CurrentCommit.When());
        }
    }
}
