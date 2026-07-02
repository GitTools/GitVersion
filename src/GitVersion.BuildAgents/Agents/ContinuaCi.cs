using System.IO.Abstractions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class ContinuaCi(IEnvironment environment, ILogger<ContinuaCi> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "ContinuaCI.Version";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"@@continua[setVariable name='GitVersion_{name}' value='{value}' skipIfNotDefined='true']"
    ];

    public override string SetBuildNumber(GitVersionVariables variables) => $"@@continua[setBuildVersion value='{variables.FullSemVer}']";

    public override bool PreventFetch() => false;
}
