namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    /// <summary>
    /// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
    /// BaseVersionSource is the tag's commit.
    /// Increments if the tag is not the current commit.
    /// </summary>
    public class TaggedCommitVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            return GetTaggedVersions(context, context.CurrentBranch, context.CurrentCommit.When());
        }

        public IEnumerable<BaseVersion> GetTaggedVersions(GitVersionContext context, Branch currentBranch, DateTimeOffset? olderThan)
        {
            var allTags = GitRepoMetadataProvider.GetValidVersionTags(context.Repository, context.Configuration.GitTagPrefix, olderThan); 
                
            var tagsOnBranch = currentBranch
                .Commits
                .SelectMany(commit => { return allTags.Where(t => IsValidTag(t.Item1, commit)); })
                .Select(t =>
                {
                    var commit = t.Item1.PeeledTarget() as Commit;
                    if (commit != null)
                        return new VersionTaggedCommit(commit, t.Item2, t.Item1.FriendlyName);

                    return null;
                })
                .Where(a => a != null)
                .ToList();

            return tagsOnBranch.Select(t => CreateBaseVersion(context, t));
        }

        BaseVersion CreateBaseVersion(GitVersionContext context, VersionTaggedCommit version)
        {
            var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
            var baseVersion = new BaseVersion(context, FormatSource(version), shouldUpdateVersion, version.SemVer, version.Commit, null);
            return baseVersion;
        }

        protected virtual string FormatSource(VersionTaggedCommit version)
        {
            return $"Git tag '{version.Tag}'";
        }

        protected virtual bool IsValidTag(Tag tag, Commit commit)
        {
            return tag.PeeledTarget() == commit;
        }

        protected class VersionTaggedCommit
        {
            public string Tag;
            public Commit Commit;
            public SemanticVersion SemVer;

            public VersionTaggedCommit(Commit commit, SemanticVersion semVer, string tag)
            {
                Tag = tag;
                Commit = commit;
                SemVer = semVer;
            }
        }
    }
}