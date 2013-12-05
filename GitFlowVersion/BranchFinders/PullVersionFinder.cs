namespace GitFlowVersion
{
    using LibGit2Sharp;

    class PullVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch PullBranch;

        public VersionAndBranch FindVersion()
        {
            EnsurePullBranchShareACommonAncestorWithDevelop();

            var versionOnMasterFinder = new VersionOnMasterFinder
            {
                Repository = Repository,
                OlderThan = Commit.When(),
            };
            var versionFromMaster = versionOnMasterFinder
                .Execute();

            var suffix = ExtractIssueNumber();

            return new VersionAndBranch
            {
                BranchType = BranchType.PullRequest,
                BranchName = PullBranch.Name,
                Sha = Commit.Sha,
                Version = new SemanticVersion
                {
                    Major = versionFromMaster.Major,
                    Minor = versionFromMaster.Minor + 1,
                    Patch = 0,
                    Stability = Stability.Unstable,
                    PreReleasePartOne = 0,
                    Suffix = suffix
                }
            };
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

        void EnsurePullBranchShareACommonAncestorWithDevelop()
        {
            var ancestor = Repository.Commits.FindCommonAncestor(
                Repository.FindBranch("develop").Tip,
                PullBranch.Tip);

            if (ancestor != null)
            {
                return;
            }

            throw new ErrorException(
                "A pull request branch is expected to branch off of 'develop'. "
                + string.Format("However, branch 'develop' and '{0}' do not share a common ancestor."
                , PullBranch.Name));
        }
    }
}
