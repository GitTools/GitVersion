using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class BuildKite : BuildAgentBase
{
    public BuildKite(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public const string EnvironmentVariableName = "BUILDKITE";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override bool CanApplyToCurrentContext() => "true".Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), StringComparison.OrdinalIgnoreCase);

    public override string GenerateSetVersionMessage(VersionVariables variables) =>
        string.Empty; // There is no equivalent function in BuildKite.

    public override string[] GenerateSetParameterMessage(string name, string value) =>
        Array.Empty<string>(); // There is no equivalent function in BuildKite.

    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("BUILDKITE_BRANCH");

    public override bool PreventFetch() => true;
}
