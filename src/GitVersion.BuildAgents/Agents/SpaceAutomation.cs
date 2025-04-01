using System.IO.Abstractions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class SpaceAutomation(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public const string EnvironmentVariableName = "JB_SPACE_PROJECT_KEY";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("JB_SPACE_GIT_BRANCH");

    public override string[] SetOutputVariables(string name, string? value) => [];

    public override string SetBuildNumber(GitVersionVariables variables) => string.Empty;
}
