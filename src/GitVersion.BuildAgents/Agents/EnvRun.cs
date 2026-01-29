using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class EnvRun(IEnvironment environment, ILogger<EnvRun> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    private const string EnvironmentVariableName = "ENVRUN_DATABASE";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext()
    {
        var envRunDatabasePath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (envRunDatabasePath.IsNullOrEmpty()) return false;
        if (this.FileSystem.File.Exists(envRunDatabasePath)) return true;
        this.Logger.LogError("The database file of EnvRun.exe was not found at {EnvRunDatabasePath}.", envRunDatabasePath);

        return false;
    }

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"@@envrun[set name='GitVersion_{name}' value='{value}']"
    ];
    public override bool PreventFetch() => true;
}
