namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public class TaggedCommitVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            string currentBranchName = null;
            var head = context.Repository.Head;
            if (head != null)
            {
                currentBranchName = head.CanonicalName;
            }

            var olderThan = context.CurrentCommit.When();
            var allTags = context.Repository.Tags
                .Where(tag => ((Commit)tag.PeeledTarget()).When() <= olderThan)
                .ToList();
            var tagsOnBranch = context.CurrentBranch
                .Commits
                .SelectMany(commit =>
                {
                    return allTags.Where(t => IsValidTag(context, currentBranchName, t, commit));
                })
                .Select(t =>
                {
                    SemanticVersion version;
                    if (SemanticVersion.TryParse(t.Name, context.Configuration.GitTagPrefix, out version))
                    {
                        var commit = t.PeeledTarget() as Commit;
                        if (commit != null)
                            return new VersionTaggedCommit(commit, version, t.Name);
                    }
                    return null;
                })
                .Where(a => a != null)
                .ToList();

            if (tagsOnBranch.Count == 0)
            {
                yield break;
            }
            if (tagsOnBranch.Count == 1)
            {
                yield return CreateBaseVersion(context, tagsOnBranch[0]);
            }

            foreach (var result in tagsOnBranch.Select(t => CreateBaseVersion(context, t)))
            {
                yield return result;
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

        protected virtual bool IsValidTag(GitVersionContext context, string branchName, Tag tag, Commit commit)
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