using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class EnvRun(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public const string EnvironmentVariableName = "ENVRUN_DATABASE";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext()
    {
        var envRunDatabasePath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!envRunDatabasePath.IsNullOrEmpty())
        {
            if (!File.Exists(envRunDatabasePath))
            {
                this.Log.Error($"The database file of EnvRun.exe was not found at {envRunDatabasePath}.");
                return false;
            }

            return true;
        }

        return false;
    }

    public override string GenerateSetVersionMessage(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string? value) =>
    [
        $"@@envrun[set name='GitVersion_{name}' value='{value}']"
    ];
    public override bool PreventFetch() => true;
}
