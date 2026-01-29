using System.IO.Abstractions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class TravisCi(IEnvironment environment, ILogger<TravisCi> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "TRAVIS";
    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override bool CanApplyToCurrentContext() => "true".Equals(Environment.GetEnvironmentVariable(EnvironmentVariable)) && "true".Equals(Environment.GetEnvironmentVariable("CI"));

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    public override bool PreventFetch() => true;
}
