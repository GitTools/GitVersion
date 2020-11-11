using GitVersion.Logging;
using GitVersion.OutputVariables;
using System.IO;

namespace GitVersion.BuildAgents
{
    public class GitHubActions : BuildAgentBase
    {
        // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-environment-variables#default-environment-variables

        public GitHubActions(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "GITHUB_ACTIONS";
        public const string GitHubSetEnvTempFileEnvironmentVariableName = "GITHUB_ENV";

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            // There is no equivalent function in GitHub Actions.

            return string.Empty;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            // There is no equivalent function in GitHub Actions.

            return new string[0];
        }

        public override void WriteIntegration(System.Action<string> writer, VersionVariables variables, bool updateBuildNumber = true)
        {
            base.WriteIntegration(writer, variables, updateBuildNumber);

            // https://docs.github.com/en/free-pro-team@latest/actions/reference/workflow-commands-for-github-actions#environment-files
            // The outgoing environment variables must be written to a temporary file (identified by the $GITHUB_ENV environment
            // variable, which changes for every step in a workflow) which is then parsed. That file must also be UTF-8 or it will fail.

            if (writer == null || !updateBuildNumber)
            {
                return;
            }

            var gitHubSetEnvFilePath = this.Environment.GetEnvironmentVariable(GitHubSetEnvTempFileEnvironmentVariableName);

            if (gitHubSetEnvFilePath != null)
            {
                writer($"Writing version variables to $GITHUB_ENV file for '{GetType().Name}'.");
                using (var streamWriter = File.AppendText(gitHubSetEnvFilePath)) // Already uses UTF-8 as required by GitHub
                {
                    foreach (var variable in variables)
                    {
                        if (!string.IsNullOrEmpty(variable.Value))
                        {
                            streamWriter.WriteLine($"GitVersion_{variable.Key}={variable.Value}");
                        }
                    }
                }
            }
            else
            {
                writer($"Unable to write GitVersion variables to ${GitHubSetEnvTempFileEnvironmentVariableName} because the environment variable is not set.");
            }
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return Environment.GetEnvironmentVariable("GITHUB_REF");
        }

        public override bool PreventFetch() => true;
    }
}
