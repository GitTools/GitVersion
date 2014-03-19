using GitHubFlowVersion.AcceptanceTests.Helpers;
using Xunit;

namespace GitHubFlowVersion.AcceptanceTests
{
    using System;
    using ApprovalTests;
    using TestStack.BDDfy;

    public class NoTagsInRepositorySpecification : IUseFixture<RepositoryFixture>
    {
        private ExecutionResults _result;
        RepositoryFixture _data;

        public void GivenARepositoryWithCommitsButNoTags()
        {
            _data.Repository.MakeACommit();
            _data.Repository.MakeACommit();
            _data.Repository.MakeACommit();
        }

        public void WhenGitHubFlowVersionIsExecuted()
        {
            _result = GitVersionHelper.ExecuteIn(_data.RepositoryPath);
        }

        public void ThenAZeroExitCodeShouldOccur()
        {
            Assert.Equal(0, _result.ExitCode);
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
