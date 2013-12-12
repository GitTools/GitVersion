namespace GitFlowVersion
{
    using LibGit2Sharp;

    class PullVersionFinder : DevelopBasedVersionFinderBase
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch PullBranch;

        public VersionAndBranch FindVersion()
        {
            var suffix = ExtractIssueNumber();

            var version = FindVersion(Repository, PullBranch, Commit, BranchType.PullRequest);
            version.Version.Suffix = suffix;

            return version;
        }

        string ExtractIssueNumber()
        {
            const string prefix = "/pull/";
            var start = PullBranch.CanonicalName.IndexOf(prefix, System.StringComparison.Ordinal);
            var end = PullBranch.CanonicalName.LastIndexOf("/merge", PullBranch.CanonicalName.Length - 1,
                System.StringComparison.Ordinal);

            string issueNumber = null;

            if (start != -1 && end != -1 && start + prefix.Length <= end)
            {
                start += prefix.Length;
                issueNumber = PullBranch.CanonicalName.Substring(start, end - start);
            }

            if (!LooksLikeAValidPullRequestNumber(issueNumber))
            {
                throw new ErrorException(string.Format("Unable to extract pull request number from '{0}'.",
                    PullBranch.CanonicalName));
            }

            return issueNumber;
        }

        bool LooksLikeAValidPullRequestNumber(string issueNumber)
        {
            if (string.IsNullOrEmpty(issueNumber))
            {
                return false;
            }

            uint res;
            if (!uint.TryParse(issueNumber, out res))
            {
                return false;
            }

            return true;
        }
    }
}
