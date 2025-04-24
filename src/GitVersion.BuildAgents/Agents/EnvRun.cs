using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class EnvRun(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    private const string EnvironmentVariableName = "ENVRUN_DATABASE";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext()
    {
        var envRunDatabasePath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (envRunDatabasePath.IsNullOrEmpty()) return false;
        if (this.FileSystem.File.Exists(envRunDatabasePath)) return true;
        this.Log.Error($"The database file of EnvRun.exe was not found at {envRunDatabasePath}.");

        return false;
    }

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"@@envrun[set name='GitVersion_{name}' value='{value}']"
    ];
    public override bool PreventFetch() => true;
}
