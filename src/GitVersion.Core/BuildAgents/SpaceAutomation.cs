using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class SpaceAutomation : BuildAgentBase
{
    public SpaceAutomation(IEnvironment environment, ILog log) : base(environment, log)
    {
    }

    public const string EnvironmentVariableName = "JB_SPACE_PROJECT_KEY";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        string? branchName = Environment.GetEnvironmentVariable("JB_SPACE_GIT_BRANCH");
        if (branchName != null && branchName.StartsWith("refs/heads/"))
        {
            return branchName;
        }
        return null;
    }

    public override string[] GenerateSetParameterMessage(string name, string value) => Array.Empty<string>();

    public override string GenerateSetVersionMessage(VersionVariables variables) => string.Empty;
}
