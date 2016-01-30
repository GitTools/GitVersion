namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public class TrackMergeTargetBaseVersionStrategy : TaggedCommitVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if(!context.Configuration.TrackMergeTarget)
            {
                yield break;
            }

            string currentBranchName = null;
            var head = context.Repository.Head;
            if (head != null)
            {
                currentBranchName = head.CanonicalName;
            }

            if (string.IsNullOrWhiteSpace(currentBranchName))
            {
                yield break;
            }
            var olderThan = context.CurrentCommit.When();
            var allTags = context.Repository.Tags
                .Where(tag => ((Commit)tag.PeeledTarget()).When() <= olderThan)
                .ToList();
            var tagsOnBranch = context.CurrentBranch
                .Commits
                .SelectMany(commit =>
                {
                    return allTags.Where(t => IsValidTag(context, t, commit));
                })
                .Select(t =>
                {
                    SemanticVersion version;
                    if (SemanticVersion.TryParse(t.Name, context.Configuration.GitTagPrefix, out version))
                    {
                        var commit = t.PeeledTarget() as Commit;
                        if (commit != null)
                            return new VersionTaggedCommit(commit, version, t);
                    }
                    return null;
                })
                .Where(a => a != null);

            foreach (var result in tagsOnBranch.Select(t => CreateBaseVersion(context, t)))
            {
                yield return result;
            }
        }

        static bool IsValidTag(GitVersionContext context, Tag tag, Commit commit)
        {
            return IsDirectMergeFromCommit(tag, commit);
        }

        protected override string FormatSource(VersionTaggedCommit version)
        {
            return string.Format("Merge target tagged '{0}'", version.Tag);
        }


        static bool IsDirectMergeFromCommit(Tag tag, Commit commit)
        {
            var targetCommit = tag.Target as Commit;
            if (targetCommit != null)
            {
                var parents = targetCommit.Parents;
                if (parents != null)
                {
                    return parents
                        .Where(parent => parent != null)
                        .Any(parent => string.Equals(parent.Id.Sha, commit.Id.Sha, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }
    }
}
