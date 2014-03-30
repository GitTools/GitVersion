namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    using System.Collections.Generic;

    public class ExecutionResults
    {
        public ExecutionResults(int exitCode, Dictionary<string, string> output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; private set; }
        public Dictionary<string, string> Output { get; private set; }
    }
}