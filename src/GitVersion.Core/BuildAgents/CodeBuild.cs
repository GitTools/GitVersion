using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents;

public sealed class CodeBuild : BuildAgentBase
{
    private string? file;
    public const string WebHookEnvironmentVariableName = "CODEBUILD_WEBHOOK_HEAD_REF";
    public const string SourceVersionEnvironmentVariableName = "CODEBUILD_SOURCE_VERSION";

    public CodeBuild(IEnvironment environment, ILog log) : base(environment, log) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    protected override string EnvironmentVariable => throw new NotSupportedException($"Accessing {nameof(EnvironmentVariable)} is not supported as {nameof(CodeBuild)} supports two environment variables for branch names.");

    public override string GenerateSetVersionMessage(VersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string value) => new[]
    {
        $"GitVersion_{name}={value}"
    };

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {

        var currentBranch = Environment.GetEnvironmentVariable(WebHookEnvironmentVariableName);

        if (currentBranch.IsNullOrEmpty())
        {
            return Environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName);
        }

        return currentBranch;
    }

    public override void WriteIntegration(Action<string?> writer, VersionVariables variables, bool updateBuildNumber = true)
    {
        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");
        File.WriteAllLines(this.file, GenerateBuildLogOutput(variables));
    }

    public override bool PreventFetch() => true;

    public override bool CanApplyToCurrentContext()
        => !Environment.GetEnvironmentVariable(WebHookEnvironmentVariableName).IsNullOrEmpty() || !Environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName).IsNullOrEmpty();
}
