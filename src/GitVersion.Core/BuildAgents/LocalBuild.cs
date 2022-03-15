using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class LocalBuild : BuildAgentBase
{
    public LocalBuild(IEnvironment environment, ILog log)
        : base(environment, log)
    {
    }

    protected override string EnvironmentVariable => string.Empty;
    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("GIT_BRANCH");
    public override bool CanApplyToCurrentContext() => true;
    public override string? GenerateSetVersionMessage(VersionVariables variables) => null;
    public override string[] GenerateSetParameterMessage(string name, string value) => Array.Empty<string>();
}
