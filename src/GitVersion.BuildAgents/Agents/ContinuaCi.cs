using System.IO.Abstractions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class ContinuaCi(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
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
