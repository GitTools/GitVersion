namespace GitVersion
{
    class PullVersionFinder : DevelopBasedVersionFinderBase
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var issueNumber = ExtractIssueNumber(context);

            var version = FindVersion(context, BranchType.PullRequest);
            version.PreReleaseTag = new SemanticVersionPreReleaseTag("PullRequest", int.Parse(issueNumber));
            //TODO version.Version.BuildMetaData = NumberOfCommitsOnBranchSinceCommit(context.CurrentBranch, commonAncestor);
            return version;
        }

        string ExtractIssueNumber(GitVersionContext context)
        {
            var issueNumber = GitHelper.ExtractIssueNumber(context.CurrentBranch.CanonicalName);

            if (!GitHelper.LooksLikeAValidPullRequestNumber(issueNumber))
            {
                throw new WarningException(string.Format("Unable to extract pull request number from '{0}'.", context.CurrentBranch.CanonicalName));
            }

            return issueNumber;
        }
    }
}
