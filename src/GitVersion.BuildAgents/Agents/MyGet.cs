using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class MyGet(IEnvironment environment, ILogger<MyGet> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "BuildRunner";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext()
    {
        var buildRunner = Environment.GetEnvironmentVariable(EnvironmentVariable);

        return !buildRunner.IsNullOrEmpty()
               && buildRunner.Equals("MyGet", StringComparison.InvariantCultureIgnoreCase);
    }

    public override string[] SetOutputVariables(string name, string? value)
    {
        var messages = new List<string>
        {
            $"##myget[setParameter name='GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']"
        };

        return [.. messages];
    }

    public override string SetBuildNumber(GitVersionVariables variables) =>
        $"##myget[buildNumber '{ServiceMessageEscapeHelper.EscapeValue(variables.FullSemVer)}']";

    public override bool PreventFetch() => false;
}
