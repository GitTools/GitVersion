using Xunit;

namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    public class ExecutionResults
    {
        public ExecutionResults(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; private set; }
        public string Output { get; private set; }

        public void AssertExitedSuccessfully()
        {
            Assert.Equal(0, ExitCode);
        }
    }
}