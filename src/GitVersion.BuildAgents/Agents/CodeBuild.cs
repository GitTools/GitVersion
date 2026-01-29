using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal sealed class CodeBuild : BuildAgentBase
{
    private string? file;
    public const string WebHookEnvironmentVariableName = "CODEBUILD_WEBHOOK_HEAD_REF";
    public const string SourceVersionEnvironmentVariableName = "CODEBUILD_SOURCE_VERSION";

    public CodeBuild(IEnvironment environment, ILogger<CodeBuild> logger, IFileSystem fileSystem) : base(environment, logger, fileSystem) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    protected override string EnvironmentVariable => WebHookEnvironmentVariableName;

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        var currentBranch = this.environment.GetEnvironmentVariable(WebHookEnvironmentVariableName);

        return currentBranch.IsNullOrEmpty() ? this.environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName) : currentBranch;
    }

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (this.file is null)
        {
            return;
        }

        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");
        this.fileSystem.File.WriteAllLines(this.file, SetOutputVariables(variables));
    }

    public override bool PreventFetch() => true;

    public override bool CanApplyToCurrentContext() => !this.environment.GetEnvironmentVariable(WebHookEnvironmentVariableName).IsNullOrEmpty()
                                                       || !this.environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName).IsNullOrEmpty();
}
