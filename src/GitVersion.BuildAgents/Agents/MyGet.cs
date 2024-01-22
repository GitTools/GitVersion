using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class MyGet(IEnvironment environment, ILog log) : BuildAgentBase(environment, log)
{
    public const string EnvironmentVariableName = "BuildRunner";
    protected override string EnvironmentVariable => EnvironmentVariableName;
    public override bool CanApplyToCurrentContext()
    {
        var buildRunner = Environment.GetEnvironmentVariable(EnvironmentVariable);

        return !buildRunner.IsNullOrEmpty()
               && buildRunner.Equals("MyGet", StringComparison.InvariantCultureIgnoreCase);
    }

    public override string[] GenerateSetParameterMessage(string name, string? value)
    {
        var messages = new List<string>
        {
            $"##myget[setParameter name='GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']"
        };

        if (string.Equals(name, "SemVer", StringComparison.InvariantCultureIgnoreCase))
        {
            messages.Add($"##myget[buildNumber '{ServiceMessageEscapeHelper.EscapeValue(value)}']");
        }

        return messages.ToArray();
    }

    public override string? GenerateSetVersionMessage(GitVersionVariables variables) => null;

    public override bool PreventFetch() => false;
}
