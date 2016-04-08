namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public class TaggedCommitVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var olderThan = context.CurrentCommit.When();
            var tags = context.Repository.Tags
                .Where(tag => ((Commit)tag.PeeledTarget()).When() <= olderThan);
            
            return GetVersionsFromTags(context, tags);
        }

        private IEnumerable<BaseVersion> GetVersionsFromTags(GitVersionContext context, IEnumerable<Tag> tags)
        {
            var currentBranchName = context.CurrentBranch.CanonicalName;
            var versionedTags = tags.Where(t => IsTagInBranch(context, t.PeeledTarget() as Commit, currentBranchName))
                .Select(t =>
                {
                    SemanticVersion version;
                    if (SemanticVersion.TryParse(t.Name, context.Configuration.GitTagPrefix, out version))
                    {
                        return new VersionTaggedCommit((Commit)t.PeeledTarget(), version, t);
                    }
                    return null;
                }).Where(a => a != null);
            return versionedTags.Select(t => CreateBaseVersion(context, t));
        }

        static bool IsTagInBranch(GitVersionContext context, Commit tagCommit, string currentBranchName)
        {
            if(tagCommit == null)
            {
                return false;
            }
            var branches = tagCommit.GetBranchesContainingCommit(context.Repository, new List<Branch> { context.CurrentBranch }, false).ToList();
            return branches.Any(b =>b.CanonicalName == currentBranchName);
        }

        protected BaseVersion CreateBaseVersion(GitVersionContext context, VersionTaggedCommit version)
        {
            var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
            var baseVersion = new BaseVersion(FormatSource(version), shouldUpdateVersion, version.SemVer, version.Commit, null);
            return baseVersion;
        }

        protected virtual string FormatSource(VersionTaggedCommit version)
        {
            return string.Format("Git tag '{0}'", version.Tag);
        }

        protected class VersionTaggedCommit
        {
            public Tag Tag;
            public Commit Commit;
            public SemanticVersion SemVer;

            public VersionTaggedCommit(Commit commit, SemanticVersion semVer, Tag tag)
            {
                Tag = tag;
                Commit = commit;
                SemVer = semVer;
            }
        }
    }
}
