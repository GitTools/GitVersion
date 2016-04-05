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

            foreach (var version in base.GetVersions(context))
            {
                yield return version;
            }
        }

        protected override bool IsValidTag(Tag tag, Commit commit)
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
