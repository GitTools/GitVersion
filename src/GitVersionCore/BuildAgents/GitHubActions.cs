using GitVersion.Logging;
using GitVersion.OutputVariables;
using System.IO;
using System.Text;

namespace GitVersion.BuildAgents
{
    public class GitHubActions : BuildAgentBase
    {
        // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-environment-variables#default-environment-variables

        public GitHubActions(IEnvironment environment, ILog log) : base(environment, log)
        {
            this.environment = environment;
        }

        public const string EnvironmentVariableName = "GITHUB_ACTIONS";
        public const string GitHubSetEnvTempFileEnvironmentVariableName = "GITHUB_ENV";
        private readonly IEnvironment environment;

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            // There is no equivalent function in GitHub Actions.

            return string.Empty;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            // https://docs.github.com/en/free-pro-team@latest/actions/reference/workflow-commands-for-github-actions#environment-files
            // However it's important that GitHub Actions does not parse the log output. The outgoing environment variables must be
            // written to a temporary file (identified by the $GITHUB_ENV environment variable, which changes for every step in a workflow)
            // which is then parsed. That file must also be UTF-8 or it will fail.

            if (!string.IsNullOrWhiteSpace(value))
            {
                var gitHubSetEnvFilePath = environment.GetEnvironmentVariable(GitHubSetEnvTempFileEnvironmentVariableName);
                var assignment = $"GitVersion_{name}={value}";

                if (gitHubSetEnvFilePath != null)
                {
                    using (var streamWriter = File.AppendText(gitHubSetEnvFilePath)) // Already uses UTF-8 as required by GitHub
                    {
                        streamWriter.WriteLine(assignment);
                    }

                    return new[]
                    {
                        $"Writing \"{assignment}\" to the file at ${GitHubSetEnvTempFileEnvironmentVariableName}"
                    };
                }

                return new[]
                {
                    $"Unable to write \"{assignment}\" to ${GitHubSetEnvTempFileEnvironmentVariableName} because the environment variable is not set."
                };
            }

            return new string[0];
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return Environment.GetEnvironmentVariable("GITHUB_REF");
        }

        public override bool PreventFetch() => true;
    }
}
