using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class Jenkins : BuildAgentBase
{
    public const string EnvironmentVariableName = "JENKINS_URL";
    private string? file;
    protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

    public Jenkins(IEnvironment environment, ILog log) : base(environment, log) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    public override string GenerateSetVersionMessage(VersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string value) => new[]
    {
        $"GitVersion_{name}={value}"
    };

    public override string? GetCurrentBranch(bool usingDynamicRepos) => IsPipelineAsCode()
        ? Environment.GetEnvironmentVariable("BRANCH_NAME")
        : Environment.GetEnvironmentVariable("GIT_LOCAL_BRANCH") ?? Environment.GetEnvironmentVariable("GIT_BRANCH");

    private bool IsPipelineAsCode() => !Environment.GetEnvironmentVariable("BRANCH_NAME").IsNullOrEmpty();

    public override bool PreventFetch() => true;

    /// <summary>
    /// When Jenkins uses pipeline-as-code, it creates two remotes: "origin" and "origin1".
    /// This should be cleaned up, so that normizaling the Git repo will not fail.
    /// </summary>
    /// <returns></returns>
    public override bool ShouldCleanUpRemotes() => IsPipelineAsCode();

    public override void WriteIntegration(Action<string?> writer, VersionVariables variables, bool updateBuildNumber = true)
    {
        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");
        WriteVariablesFile(variables);
    }

    private void WriteVariablesFile(VersionVariables variables) => File.WriteAllLines(this.file, GenerateBuildLogOutput(variables));
}
