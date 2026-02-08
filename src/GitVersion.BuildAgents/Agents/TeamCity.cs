using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class TeamCity(IEnvironment environment, ILogger<TeamCity> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "TEAMCITY_VERSION";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var branchName = Environment.GetEnvironmentVariable("Git_Branch");

        if (!branchName.IsNullOrEmpty()) return branchName;
        if (!usingDynamicRepos)
        {
            WriteBranchEnvVariableWarning();
        }

        return base.GetCurrentBranch(usingDynamicRepos);
    }

    private void WriteBranchEnvVariableWarning() => this.Logger.LogWarning("""
                                                                     TeamCity doesn't make the current branch available through environmental variables.
                                                                     Depending on your authentication and transport setup of your git VCS root things may work. In that case, ignore this warning.
                                                                     In your TeamCity build configuration, add a parameter called `env.Git_Branch` with value %teamcity.build.vcs.branch.<vcsid>%
                                                                     See https://gitversion.net/docs/reference/build-servers/teamcity for more info
                                                                     """);

    public override bool PreventFetch() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Git_Branch"));

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"##teamcity[setParameter name='GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']",
        $"##teamcity[setParameter name='system.GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']"
    ];

    public override string SetBuildNumber(GitVersionVariables variables) => $"##teamcity[buildNumber '{ServiceMessageEscapeHelper.EscapeValue(variables.FullSemVer)}']";
}
