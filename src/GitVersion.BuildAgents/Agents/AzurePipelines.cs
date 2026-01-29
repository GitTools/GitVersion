using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class AzurePipelines(IEnvironment environment, ILogger<AzurePipelines> logger, IFileSystem fileSystem) : BuildAgentBase(environment, logger, fileSystem)
{
    public const string EnvironmentVariableName = "TF_BUILD";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"##vso[task.setvariable variable=GitVersion.{name}]{value}",
        $"##vso[task.setvariable variable=GitVersion.{name};isOutput=true]{value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var gitBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
        if (gitBranch is not null)
            return gitBranch;

        var sourceBranch = Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");

        // https://learn.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
        // BUILD_SOURCEBRANCH must be used only for "real" branches, not for tags.
        // Azure Pipelines sets BUILD_SOURCEBRANCH to refs/tags/<tag> when the pipeline is triggered for a tag.
        return sourceBranch?.StartsWith("refs/tags", StringComparison.OrdinalIgnoreCase) == true ? null : sourceBranch;
    }

    public override bool PreventFetch() => true;

    public override string SetBuildNumber(GitVersionVariables variables)
    {
        // For AzurePipelines, we'll get the Build Number and insert GitVersion variables where
        // specified
        var buildNumberEnv = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER");
        if (buildNumberEnv.IsNullOrWhiteSpace())
            return variables.FullSemVer;

        var newBuildNumber = variables.OrderBy(x => x.Key).Aggregate(buildNumberEnv, ReplaceVariables);

        // If no variable substitution has happened, use FullSemVer
        if (buildNumberEnv != newBuildNumber) return $"##vso[build.updatebuildnumber]{newBuildNumber}";
        var buildNumber = variables.FullSemVer.EndsWith("+0")
            ? variables.FullSemVer[..^2]
            : variables.FullSemVer;

        return $"##vso[build.updatebuildnumber]{buildNumber}";
    }

    private static string ReplaceVariables(string buildNumberEnv, KeyValuePair<string, string?> variable)
    {
        var pattern = $@"\$\(GITVERSION[_\.]{variable.Key}\)";
        var replacement = variable.Value;
        return replacement switch
        {
            null => buildNumberEnv,
            _ => buildNumberEnv.RegexReplace(pattern, replacement)
        };
    }
}
