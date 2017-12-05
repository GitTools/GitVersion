namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using GitVersion.GitRepoInformation;
    using LibGit2Sharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Version is from NextVersion (the configuration value), unless the current commit is tagged.
    /// BaseVersionSource is null.
    /// Does not increment.
    /// </summary>
    public class ConfigNextVersionBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if (string.IsNullOrEmpty(context.Configuration.NextVersion) || context.IsCurrentCommitTagged)
            {
                yield break;
            }
            var semanticVersion = SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.GitTagPrefix);
            var configHistory = string.IsNullOrEmpty(context.FullConfiguration.Filename)
                ? null
                : context.Repository.Commits.QueryBy(context.FullConfiguration.Filename).FirstOrDefault();

            var sourceCommit = configHistory == null
                ? context.RepositoryMetadata.CurrentBranch.Tip
                : new MCommit(
                    configHistory.Commit,
                    new Lazy<int>(() => context.RepositoryMetadataProvider.GetCommitCount(
                        context.CurrentCommit,
                        context.Repository.Lookup<Commit>(configHistory.Commit.Sha))));
            var source = new BaseVersionSource(
                sourceCommit,
                "NextVersion in GitVersion configuration file");
            yield return new BaseVersion(context, false, semanticVersion, source, null);
        }
    }
}