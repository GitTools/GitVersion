namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class HighestTagBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            VersionTaggedCommit version;
            if (GetVersion(context, out version))
            {
                var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
                return new BaseVersion(string.Format("Git tag '{0}'", version.Tag), shouldUpdateVersion, version.SemVer, version.Commit, null);
            }

            return null;
        }

        protected virtual bool IsValidTag(string branchName, Tag tag, Commit commit)
        {
            return tag.PeeledTarget() == commit;
        }

        bool GetVersion(GitVersionContext context, out VersionTaggedCommit versionTaggedCommit)
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
                    return allTags.Where(t => IsValidTag(currentBranchName, t, commit));
                })
                .Select(t =>
                {
                    SemanticVersion version;
                    if (SemanticVersion.TryParse(t.Name, context.Configuration.GitTagPrefix, out version))
                    {
                        return new VersionTaggedCommit((Commit)t.PeeledTarget(), version, t.Name);
                    }
                    return null;
                })
                .Where(a => a != null)
                .ToList();

            if (tagsOnBranch.Count == 0)
            {
                versionTaggedCommit = null;
                return false;
            }
            if (tagsOnBranch.Count == 1)
            {
                versionTaggedCommit = tagsOnBranch[0];
                return true;
            }

            versionTaggedCommit = tagsOnBranch.Skip(1).Aggregate(tagsOnBranch[0], (t, t1) => t.SemVer > t1.SemVer ? t : t1);
            return true;
        }

        class VersionTaggedCommit
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