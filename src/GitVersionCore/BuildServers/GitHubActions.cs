using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildServers
{
    public class GitHubActions: BuildServerBase
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
            // GITHUB_REF
            // The branch or tag ref that triggered the workflow.
            // For example, refs/heads/feature-branch-1. If neither a branch or
            // tag is available for the event type, the variable will not exist.

            var value = Environment.GetEnvironmentVariable("GITHUB_REF");
            if (!string.IsNullOrWhiteSpace(value))
            {
                const string refsHeads = "refs/heads/";

                if (value.StartsWith(refsHeads))
                {
                    value = value.Substring(refsHeads.Length);
                    return value;
                }
            }

            return base.GetCurrentBranch(usingDynamicRepos);
        }
    }
}
