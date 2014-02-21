namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class BuildNumberCalculator
    {
        NextSemverCalculator nextSemverCalculator;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;
        IRepository gitRepo;

        public BuildNumberCalculator(
            NextSemverCalculator nextSemverCalculator,
            LastTaggedReleaseFinder lastTaggedReleaseFinder,
            IRepository gitRepo)
        {
            this.nextSemverCalculator = nextSemverCalculator;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            this.gitRepo = gitRepo;
        }

        public VersionAndBranch GetBuildNumber(GitVersionContext context)
        {
            var commit = lastTaggedReleaseFinder.GetVersion().Commit;
            var commitsSinceLastRelease = NumberOfCommitsOnBranchSinceCommit(gitRepo.Head, commit);
            var semanticVersion = nextSemverCalculator.NextVersion();
            semanticVersion.PreReleasePartTwo = commitsSinceLastRelease;
            if (context.CurrentBranch.IsPullRequest())
            {
                EnsurePullBranchShareACommonAncestorWithMaster(gitRepo, gitRepo.Head);
                var extractIssueNumber = ExtractIssueNumber(context);
                semanticVersion.Tag = "unstable" + extractIssueNumber;
                semanticVersion.Suffix = extractIssueNumber;
                return new VersionAndBranch
                {
                    BranchName = context.CurrentBranch.Name,
                    BranchType = BranchType.PullRequest,
                    Sha = context.CurrentBranch.Tip.Sha,
                    Version = semanticVersion
                };
            }
            
            return new VersionAndBranch
            {
                BranchName = context.CurrentBranch.Name,
                BranchType = BranchType.Master,
                Sha = context.CurrentBranch.Tip.Sha,
                Version = semanticVersion
            };
        }

        int NumberOfCommitsOnBranchSinceCommit(Branch branch, Commit commit)
        {
            return branch.Commits
                .TakeWhile(x => x != commit)
                .Count();
        }

        void EnsurePullBranchShareACommonAncestorWithMaster(IRepository repository, Branch pullBranch)
        {
            var masterTip = repository.FindBranch("master").Tip;
            var ancestor = repository.Commits.FindCommonAncestor(masterTip, pullBranch.Tip);

            if (ancestor != null)
            {
                return;
            }

            throw new Exception(
                "A pull request branch is expected to branch off of 'master'. "
                + string.Format("However, branch 'master' and '{0}' do not share a common ancestor."
                    , pullBranch.Name));
        }

        // TODO refactor to remove duplication
        string ExtractIssueNumber(GitVersionContext context)
        {
            const string prefix = "/pull/";
            var pullRequestBranch = context.CurrentBranch;

            var start = pullRequestBranch.CanonicalName.IndexOf(prefix, StringComparison.Ordinal);
            var end = pullRequestBranch.CanonicalName.LastIndexOf("/merge", pullRequestBranch.CanonicalName.Length - 1,
                StringComparison.Ordinal);

            string issueNumber = null;

            if (start != -1 && end != -1 && start + prefix.Length <= end)
            {
                start += prefix.Length;
                issueNumber = pullRequestBranch.CanonicalName.Substring(start, end - start);
            }

            if (!LooksLikeAValidPullRequestNumber(issueNumber))
            {
                throw new ErrorException(string.Format("Unable to extract pull request number from '{0}'.",
                    pullRequestBranch.CanonicalName));
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