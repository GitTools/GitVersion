namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;
    using GitVersion.GitRepoInformation;

    /// <summary>
    /// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
    /// BaseVersionSource is the tag's commit.
    /// Increments if the tag is not the current commit.
    /// </summary>
    public class TaggedCommitVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            return GetTaggedVersions(context, context.RepositoryMetadata.CurrentBranch);
        }

        public IEnumerable<BaseVersion> GetTaggedVersions(GitVersionContext context, MBranch currentBranch)
        {
            return currentBranch
                .Tags
                .Where(t => t.Version != null)
                .Select(t => CreateBaseVersion(context, t));
        }

        BaseVersion CreateBaseVersion(GitVersionContext context, MTag tag)
        {
            var shouldUpdateVersion = tag.Sha != context.CurrentCommit.Sha;
            return new BaseVersion(context, $"Git tag '{tag.Name}'", shouldUpdateVersion, tag.Version, context.Repository.Lookup<Commit>(tag.Sha), null);
        }
    }
}