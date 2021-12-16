using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class BuildKite : BuildAgentBase
{
    public BuildKite(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public const string EnvironmentVariableName = "BUILDKITE";

    protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

    public override bool CanApplyToCurrentContext() => Environment.GetEnvironmentVariable(EnvironmentVariable)?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    public override string GenerateSetVersionMessage(VersionVariables variables) =>
        string.Empty; // There is no equivalent function in BuildKite.

    public override string[] GenerateSetParameterMessage(string name, string value) =>
        Array.Empty<string>(); // There is no equivalent function in BuildKite.

    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("BUILDKITE_BRANCH");

    public override bool PreventFetch() => true;
}
