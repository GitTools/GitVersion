using System.IO.Abstractions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class BuildKite(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public const string EnvironmentVariableName = "BUILDKITE";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override bool CanApplyToCurrentContext() => "true".Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), StringComparison.OrdinalIgnoreCase);

    public override string GenerateSetVersionMessage(GitVersionVariables variables) =>
        string.Empty; // There is no equivalent function in BuildKite.

    public override string[] GenerateSetParameterMessage(string name, string? value) =>
        []; // There is no equivalent function in BuildKite.

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var pullRequest = Environment.GetEnvironmentVariable("BUILDKITE_PULL_REQUEST");
        if (string.IsNullOrEmpty(pullRequest) || pullRequest == "false")
        {
            return Environment.GetEnvironmentVariable("BUILDKITE_BRANCH");
        }

        // For pull requests BUILDKITE_BRANCH refers to the head, so adjust the
        // branch name for pull request versioning to function as expected
        return $"refs/pull/{pullRequest}/head";
    }

    public override bool PreventFetch() => true;
}
