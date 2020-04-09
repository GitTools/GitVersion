using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class GitHubActions : BuildAgentBase
    {
        // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-environment-variables#default-environment-variables

        public GitHubActions(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "GITHUB_ACTIONS";
        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            // There is no equivalent function in GitHub Actions.

            return string.Empty;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/development-tools-for-github-actions#set-an-environment-variable-set-env
            // Example
            // echo "::set-env name=action_state::yellow"

            if (!string.IsNullOrWhiteSpace(value))
            {
                var key = $"GitVersion_{name}";

                return new[]
                {
                    $"::set-env name={key}::{value}"
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
