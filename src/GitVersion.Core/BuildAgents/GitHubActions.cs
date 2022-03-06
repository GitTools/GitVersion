using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class GitHubActions : BuildAgentBase
{
    // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-environment-variables#default-environment-variables

    public GitHubActions(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public const string EnvironmentVariableName = "GITHUB_ACTIONS";
    public const string GitHubSetEnvTempFileEnvironmentVariableName = "GITHUB_ENV";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string GenerateSetVersionMessage(VersionVariables variables) =>
        string.Empty; // There is no equivalent function in GitHub Actions.

    public override string[] GenerateSetParameterMessage(string name, string value) =>
        Array.Empty<string>(); // There is no equivalent function in GitHub Actions.

    public override void WriteIntegration(Action<string?> writer, VersionVariables variables, bool updateBuildNumber = true)
    {
        base.WriteIntegration(writer, variables, updateBuildNumber);

        // https://docs.github.com/en/free-pro-team@latest/actions/reference/workflow-commands-for-github-actions#environment-files
        // The outgoing environment variables must be written to a temporary file (identified by the $GITHUB_ENV environment
        // variable, which changes for every step in a workflow) which is then parsed. That file must also be UTF-8 or it will fail.
        var gitHubSetEnvFilePath = this.Environment.GetEnvironmentVariable(GitHubSetEnvTempFileEnvironmentVariableName);

        if (gitHubSetEnvFilePath != null)
        {
            writer($"Writing version variables to $GITHUB_ENV file for '{GetType().Name}'.");
            using var streamWriter = File.AppendText(gitHubSetEnvFilePath);
            foreach (var variable in variables)
            {
                if (!variable.Value.IsNullOrEmpty())
                {
                    streamWriter.WriteLine($"GitVersion_{variable.Key}={variable.Value}");
                }
            }
        }
        else
        {
            writer($"Unable to write GitVersion variables to ${GitHubSetEnvTempFileEnvironmentVariableName} because the environment variable is not set.");
        }
    }

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
        // GITHUB_REF must be used only for "real" branches, not for tags and pull requests.
        // Bug fix for https://github.com/GitTools/GitVersion/issues/2838
        string? githubRef = Environment.GetEnvironmentVariable("GITHUB_REF");
        if (githubRef != null && githubRef.StartsWith("refs/heads/"))
        {
            return githubRef;
        }
        return null;
    }

    public override bool PreventFetch() => true;
}
