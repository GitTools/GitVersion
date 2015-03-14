namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using LibGit2Sharp;

    public class GitFlowDevelopBranchBaseVersionStrategy : HighestTagBaseVersionStrategy
    {
        protected override bool IsValidTag(string branchName, Tag tag, Commit commit)
        {
            if (!string.IsNullOrWhiteSpace(branchName))
            {
                if (branchName.ToLower().EndsWith("/develop"))
                {
                    return tag.IsDirectMergeFromCommit(commit);
                }
            }

            return base.IsValidTag(branchName, tag, commit);
        }
    }
}
