using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

public class LocalBuild : BuildAgentBase
{
    public LocalBuild(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public override bool IsDefault => true;

    protected override string EnvironmentVariable => string.Empty;
    public override bool CanApplyToCurrentContext() => true;
    public override string? GenerateSetVersionMessage(GitVersionVariables variables) => null;
    public override string[] GenerateSetParameterMessage(string name, string value) => Array.Empty<string>();
}
