using GitHubFlowVersion.AcceptanceTests.Helpers;
using Xunit;

namespace GitHubFlowVersion.AcceptanceTests
{
    using System;
    using ApprovalTests;
    using TestStack.BDDfy;

    public class TagFollowedByCommitsWithNoNextVersionTxtSpecification : IUseFixture<RepositoryFixture>
    {
        private ExecutionResults _result;
        RepositoryFixture _data;

        public void GivenARepositoryWithASingleTagFollowedByCommits()
        {
            _data.Repository.MakeATaggedCommit("0.1.0");
            _data.Repository.MakeACommit();
        }

        public void AndGivenThereIsNoNextVersionTxtFile() {}

        public void WhenGitHubFlowVersionIsExecuted()
        {
            _result = GitVersionHelper.ExecuteIn(_data.RepositoryPath);
        }

        public void ThenNoErrorShouldOccur()
        {
            _result.AssertExitedSuccessfully();
        }

        public void AndTheCorrectVersionShouldBeOutput()
        {
            Approvals.Verify(_result.Output, Scrubbers.GuidScrubber);
        }

        [Fact]
        public virtual void RunSpecification()
        {
            // If we are actually running in teamcity, lets delete this environmental variable
            Environment.SetEnvironmentVariable("TEAMCITY_VERSION", null);
            this.BDDfy();
        }

        public void SetFixture(RepositoryFixture data)
        {
            _data = data;
        }
    }
}
