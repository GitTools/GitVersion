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

            var sha = context.CurrentCommit.Sha;
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
            var qf = new CommitFilter
            {
                Since = context.CurrentCommit,
                Until = commit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return context.Repository.Commits.QueryBy(qf).Count();
        }

        void EnsurePullBranchShareACommonAncestorWithMaster(IRepository repository, Branch pullBranch)
        {
            var masterTip = repository.FindBranch("master").Tip;
            var ancestor = repository.Commits.FindMergeBase(masterTip, pullBranch.Tip);

            if (ancestor != null)
            {
                return;
            }

            var message = string.Format("A pull request branch is expected to branch off of 'master'. However, branch 'master' and '{0}' do not share a common ancestor.", pullBranch.Name);
            throw new Exception(message);
        }

        string ExtractIssueNumber(GitVersionContext context)
        {
            var issueNumber = GitHelper.ExtractIssueNumber(context.CurrentBranch.CanonicalName);

            if (!GitHelper.LooksLikeAValidPullRequestNumber(issueNumber))
            {
                var message = string.Format("Unable to extract pull request number from '{0}'.", context.CurrentBranch.CanonicalName);
                throw new WarningException(message);
            }

            return issueNumber;
        }
    }
}