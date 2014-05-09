namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class BuildNumberCalculator
    {
        NextSemverCalculator nextSemverCalculator;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;
        IRepository gitRepo;

        public BuildNumberCalculator(NextSemverCalculator nextSemverCalculator, LastTaggedReleaseFinder lastTaggedReleaseFinder, IRepository gitRepo)
        {
            this.nextSemverCalculator = nextSemverCalculator;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            this.gitRepo = gitRepo;
        }

        public SemanticVersion GetBuildNumber(GitVersionContext context)
        {
            var commit = lastTaggedReleaseFinder.GetVersion().Commit;
            var commitsSinceLastRelease = NumberOfCommitsOnBranchSinceCommit(context, commit);
            var semanticVersion = nextSemverCalculator.NextVersion();

            var sha = context.CurrentBranch.Tip.Sha;
            var releaseDate = ReleaseDateFinder.Execute(context.Repository, sha, semanticVersion.Patch);

            // TODO Need a way of setting this in a cross cutting way
            semanticVersion.BuildMetaData = new SemanticVersionBuildMetaData(commitsSinceLastRelease, 
                context.CurrentBranch.Name, releaseDate);
            if (context.CurrentBranch.IsPullRequest())
            {
                EnsurePullBranchShareACommonAncestorWithMaster(gitRepo, gitRepo.Head);
                var extractIssueNumber = ExtractIssueNumber(context);
                semanticVersion.PreReleaseTag = "PullRequest" + extractIssueNumber;
                return semanticVersion;
            }

            return semanticVersion;
        }

        int NumberOfCommitsOnBranchSinceCommit(GitVersionContext context, Commit commit)
        {
            return context.Repository.Commits.QueryBy(new CommitFilter
            {
                Since = context.CurrentBranch,
                SortBy = CommitSortStrategies.Topological
            })
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

            var message = string.Format("A pull request branch is expected to branch off of 'master'. However, branch 'master' and '{0}' do not share a common ancestor.", pullBranch.Name);
            throw new Exception(message);
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
                var message = string.Format("Unable to extract pull request number from '{0}'.", pullRequestBranch.CanonicalName);
                throw new ErrorException(message);
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