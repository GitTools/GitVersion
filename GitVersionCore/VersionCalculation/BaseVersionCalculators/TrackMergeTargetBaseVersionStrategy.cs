namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class TrackMergeTargetBaseVersionStrategy : HighestTagBaseVersionStrategy
    {
        protected override bool IsValidTag(GitVersionContext context, string branchName, Tag tag, Commit commit)
        {
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                if (context.Configuration.TrackMergeTarget)
                {
                    return IsDirectMergeFromCommit(tag, commit);
                }
            }

            return false;
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
