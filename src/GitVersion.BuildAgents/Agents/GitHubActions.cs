using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class GitHubActions(IEnvironment environment, ILogger<GitHubActions> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    // https://help.github.com/en/actions/automating-your-workflow-with-github-actions/using-environment-variables#default-environment-variables

    public const string EnvironmentVariableName = "GITHUB_ACTIONS";
    public const string GitHubSetEnvTempFileEnvironmentVariableName = "GITHUB_ENV";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string SetBuildNumber(GitVersionVariables variables) =>
        string.Empty; // There is no equivalent function in GitHub Actions.

    public override string[] SetOutputVariables(string name, string? value) =>
        []; // There is no equivalent function in GitHub Actions.

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        base.WriteIntegration(writer, variables, updateBuildNumber);

        // https://docs.github.com/en/free-pro-team@latest/actions/reference/workflow-commands-for-github-actions#environment-files
        // The outgoing environment variables must be written to a temporary file (identified by the $GITHUB_ENV environment
        // variable, which changes for every step in a workflow) which is then parsed. That file must also be UTF-8, or it will fail.
        var gitHubSetEnvFilePath = this.Environment.GetEnvironmentVariable(GitHubSetEnvTempFileEnvironmentVariableName);

        if (gitHubSetEnvFilePath != null)
        {
            writer($"Writing version variables to $GITHUB_ENV file for '{GetType().Name}'.");
            using var streamWriter = this.FileSystem.File.AppendText(gitHubSetEnvFilePath);
            foreach (var (key, value) in variables)
            {
                if (!value.IsNullOrEmpty())
                {
                    streamWriter.WriteLine($"GitVersion_{key}={value}");
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
        // GITHUB_REF must be used only for "real" branches, not for tags.
        // Bug fix for https://github.com/GitTools/GitVersion/issues/2838

        var refType = Environment.GetEnvironmentVariable("GITHUB_REF_TYPE") ?? "";
        return refType.Equals("tag", StringComparison.OrdinalIgnoreCase) ? null : Environment.GetEnvironmentVariable("GITHUB_REF");
    }

    public override bool PreventFetch() => true;
}
