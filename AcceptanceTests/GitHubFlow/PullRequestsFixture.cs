using System;
using GitHubFlowVersion.AcceptanceTests.Helpers;
using LibGit2Sharp;
using Xunit;

namespace GitHubFlowVersion.AcceptanceTests
{
    public abstract class PullRequestsSpecification : RepositoryFixture
    {
        private ExecutionResults _result;
        private const string TaggedVersion = "1.0.3";
        protected abstract string PullRequestBranchName();

        public void GivenARemoteWithATagOnMaster()
        {
            RemoteRepositoryPath = PathHelper.GetTempPath();
            Repository.Init(RemoteRepositoryPath);
            RemoteRepository = new Repository(RemoteRepositoryPath);
            RemoteRepository.Config.Set("user.name", "Test");
            RemoteRepository.Config.Set("user.email", "test@email.com");
            RemoteReference = Repository.Network.Remotes.Add("origin", RemoteRepositoryPath);
            Console.WriteLine("Created git repository at {0}", RemoteRepositoryPath);
            RemoteRepository.MakeATaggedCommit(TaggedVersion);
        }

        protected Remote RemoteReference { get; private set; }
        protected Repository RemoteRepository { get; private set; }
        protected string RemoteRepositoryPath { get; private set; }
        protected string MergeCommitSha { get; private set; }

        public void AndGivenRunningInTeamCity()
        {
            Environment.SetEnvironmentVariable("TEAMCITY_VERSION", "8.0.4");
        }

        public void AndGivenARemoteFeatureBranchWithTwoCommits()
        {
            var branch = RemoteRepository.CreateBranch("FeatureBranch");
            RemoteRepository.Checkout(branch);
            RemoteRepository.MakeCommits(2);
        }

        public void AndGivenRemoteHasPullRefWithMergeCommit()
        {
            var pullRequestBranchName = PullRequestBranchName();
            RemoteRepository.Checkout(RemoteRepository.Head.Tip.Sha);
            //Emulate merge commit
            MergeCommitSha = RemoteRepository.MakeACommit().Sha;
            RemoteRepository.Checkout("master"); // HEAD cannot be pointing at the merge commit
            RemoteRepository.Refs.Add(pullRequestBranchName, new ObjectId(MergeCommitSha));
        }

        public void AndGivenTeamCityHasCheckedOutThePullMergeCommit()
        {
            Repository.Fetch("origin");
            Repository.Checkout(MergeCommitSha);
        }

        public void WhenGitHubFlowVersionIsExecuted()
        {
            _result = GitVersionHelper.ExecuteIn(RepositoryPath);
        }

        public void ThenItShouldExitWithoutError()
        {
            _result.AssertExitedSuccessfully();
        }

        public void AndTheCorrectVersionShouldBeOutput()
        {
            Assert.Contains("1.0.4-PullRequest5", _result.Output);
        }

        public class StashPullRequests : PullRequestsSpecification
        {
            protected override string PullRequestBranchName()
            {
                return "refs/pull-requests/5/merge-clean";
            }
        }

        public class GitHubPullRequests : PullRequestsSpecification
        {
            protected override string PullRequestBranchName()
            {
                return "refs/pull/5/merge";
            }
        }
    }
}