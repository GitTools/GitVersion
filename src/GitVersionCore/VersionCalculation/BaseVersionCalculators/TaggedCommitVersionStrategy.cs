namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
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
            return GetTaggedVersions(context);
        }

        public IEnumerable<BaseVersion> GetTaggedVersions(GitVersionContext context)
        {
            foreach (var config in context.Configurations)
            {
                var olderThan = config.CurrentBranchInfo.FirstCommit.When();
                var allTagsForConfig = context.Repository.Tags
                    .Where(tag => ((Commit) tag.PeeledTarget()).When() <= olderThan)
                    .ToList();
                var tagsOnBranch = config.CurrentBranchInfo.RelevantCommits
                    .SelectMany(commit => allTagsForConfig.Where(tag => IsValidTag(tag, commit)))
                    .Select(tag =>
                    {
                        SemanticVersion version;
                        if (SemanticVersion.TryParse(tag.FriendlyName, config.GitTagPrefix, out version))
                        {
                            var commit = tag.PeeledTarget() as Commit;
                            if (commit != null)
                                return new VersionTaggedCommit(commit, version, tag.FriendlyName);
                        }
                        return null;
                    })
                    .Where(commit => commit != null)
                    .ToList();

                foreach (var version in tagsOnBranch.Select(t => CreateBaseVersion(context, t)))
                {
                    yield return version;
                }
            }
        }

        BaseVersion CreateBaseVersion(GitVersionContext context, VersionTaggedCommit version)
        {
            var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
            var baseVersion = new BaseVersion(FormatSource(version), shouldUpdateVersion, version.SemVer, version.Commit, null);
            return baseVersion;
        }

        protected virtual string FormatSource(VersionTaggedCommit version)
        {
            return string.Format("Git tag '{0}'", version.Tag);
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