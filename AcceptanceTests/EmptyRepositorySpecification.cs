using GitHubFlowVersion.AcceptanceTests.Helpers;
using Xunit;

namespace GitHubFlowVersion.AcceptanceTests
{
    using System;
    using TestStack.BDDfy;

    public class EmptyRepositorySpecification : IUseFixture<RepositoryFixture>
    {
        private ExecutionResults _result;
        RepositoryFixture _data;

        public void GivenAnEmptyRepository() {}
        
        public void WhenGitHubFlowVersionIsExecuted()
        {
            _result = GitVersionHelper.ExecuteIn(_data.RepositoryPath);
        }

        public void ThenANonZeroExitCodeShouldOccur()
        {
            Assert.NotEqual(0, _result.ExitCode);
        }

        public void AndAnErrorAboutNotFindingMasterShouldBeShown()
        {
            Assert.Contains("No Tip found. Has repo been initialized?", _result.Output);
        }

        [Fact]
        public void RunSpecification()
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
