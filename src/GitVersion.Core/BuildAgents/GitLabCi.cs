using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public class GitLabCi : BuildAgentBase
{
    public const string EnvironmentVariableName = "GITLAB_CI";
    private string? file;

    public GitLabCi(IEnvironment environment, ILog log) : base(environment, log) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    protected override string EnvironmentVariable => EnvironmentVariableName;


    public override string GenerateSetVersionMessage(VersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string value) => new[]
    {
        $"GitVersion_{name}={value}"
    };

    // According to https://docs.gitlab.com/ee/ci/variables/predefined_variables.html
    // the CI_COMMIT_BRANCH environment variable must be used.
    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("CI_COMMIT_BRANCH");

    public override bool PreventFetch() => true;

    public override void WriteIntegration(Action<string?> writer, VersionVariables variables, bool updateBuildNumber = true)
    {
        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");
        WriteVariablesFile(variables);
    }

    private void WriteVariablesFile(VersionVariables variables) => File.WriteAllLines(this.file, GenerateBuildLogOutput(variables));
}
