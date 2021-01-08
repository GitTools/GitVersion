using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Common;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
    /// BaseVersionSource is the tag's commit.
    /// Increments if the tag is not the current commit.
    /// </summary>
    public class TaggedCommitVersionStrategy : VersionStrategyBase
    {
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;

        public TaggedCommitVersionStrategy(IRepositoryMetadataProvider repositoryMetadataProvider, Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            return GetTaggedVersions(Context.CurrentBranch, Context.CurrentCommit.When());
        }

        internal IEnumerable<BaseVersion> GetTaggedVersions(IBranch currentBranch, DateTimeOffset? olderThan)
        {
            var allTags = repositoryMetadataProvider.GetValidVersionTags(Context.Configuration.GitTagPrefix, olderThan);

            var taggedCommits = currentBranch
                .Commits
                .SelectMany(commit => allTags.Where(t => IsValidTag(t.Item1, commit))).ToList();

            var taggedVersions = taggedCommits
                .Select(t =>
                {
                    var commit = t.Item1.PeeledTargetCommit();
                    return commit != null ? new VersionTaggedCommit(commit, t.Item2, t.Item1.FriendlyName) : null;
                })
                .Where(versionTaggedCommit => versionTaggedCommit != null)
                .Select(versionTaggedCommit => CreateBaseVersion(Context, versionTaggedCommit))
                .ToList();

            var taggedVersionsOnCurrentCommit = taggedVersions.Where(version => !version.ShouldIncrement).ToList();
            return taggedVersionsOnCurrentCommit.Any() ? taggedVersionsOnCurrentCommit : taggedVersions;
        }

        private BaseVersion CreateBaseVersion(GitVersionContext context, VersionTaggedCommit version)
        {
            var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
            var baseVersion = new BaseVersion(FormatSource(version), shouldUpdateVersion, version.SemVer, version.Commit, null);
            return baseVersion;
        }

        protected virtual string FormatSource(VersionTaggedCommit version)
        {
            return $"Git tag '{version.Tag}'";
        }

        protected virtual bool IsValidTag(ITag tag, ICommit commit)
        {
            var targetCommit = tag.PeeledTargetCommit();
            return targetCommit != null && Equals(targetCommit, commit);
        }

        protected class VersionTaggedCommit
        {
            public string Tag;
            public ICommit Commit;
            public SemanticVersion SemVer;

            public VersionTaggedCommit(ICommit commit, SemanticVersion semVer, string tag)
            {
                Tag = tag;
                Commit = commit;
                SemVer = semVer;
            }

            public override string ToString()
            {
                return $"{Tag} | {Commit} | {SemVer}";
            }
        }
    }
}
