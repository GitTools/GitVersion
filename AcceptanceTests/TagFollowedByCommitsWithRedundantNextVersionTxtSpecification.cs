using GitHubFlowVersion.AcceptanceTests.Helpers;
using TestStack.BDDfy;

namespace GitHubFlowVersion.AcceptanceTests
{
    using ApprovalTests;
    using Xunit;

    public class TagFollowedByCommitsWithRedundantNextVersionTxtSpecification : IUseFixture<RepositoryFixture>
    {
        private ExecutionResults _result;
        private const string TaggedVersion = "1.0.3";
        private int _numCommitsToMake;
        RepositoryFixture _data;
        private const string NextVersionTxtVersion = "1.0.0";

        public void GivenARepositoryWithASingleTag()
        {
            _data.Repository.MakeATaggedCommit(TaggedVersion);
        }

        public void AndGivenRepositoryHasAnotherXCommits()
        {
            _data.Repository.MakeCommits(_numCommitsToMake);
        }

        public void AndGivenRepositoryHasARedundantNextVersionTxtFile()
        {
            _data.Repository.AddNextVersionTxtFile(NextVersionTxtVersion);
        }
        
        public void WhenGitHubFlowVersionIsExecuted()
        {
            _result = GitVersionHelper.ExecuteIn(_data.RepositoryPath);
        }

        public void ThenAZeroExitCodeShouldOccur()
        {
            _result.AssertExitedSuccessfully();
        }

        public void AndTheCorrectVersionShouldBeOutput()
        {
            Approvals.Verify(_result.Output, Scrubbers.GuidScrubber);
        }

        [Fact]
        public void WithOneCommit()
        {
            _numCommitsToMake = 1;
            this.BDDfy();
        }

        [Fact]
        public void WithTenCommit()
        {
            _numCommitsToMake = 10;
            this.BDDfy();
        }

        public void SetFixture(RepositoryFixture data)
        {
            _data = data;
        }
    }
}
