using System.IO.Abstractions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class GitLabCi : BuildAgentBase
{
    public const string EnvironmentVariableName = "GITLAB_CI";
    public const string CommitRefNameEnvironmentVariableName = "CI_COMMIT_REF_NAME";
    public const string CommitTagEnvironmentVariableName = "CI_COMMIT_TAG";
    public const string MergeRequestRefPathEnvironmentVariableName = "CI_MERGE_REQUEST_REF_PATH";

    private string? file;

    public GitLabCi(IEnvironment environment, ILog log, IFileSystem fileSystem) : base(environment, log, fileSystem) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string SetBuildNumber(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] SetOutputVariables(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    // CI_COMMIT_REF_NAME = branch/tag name. In MR pipelines, CI_MERGE_REQUEST_REF_PATH = refs/merge-requests/<iid>/head.
    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        if (!string.IsNullOrEmpty(this.Environment.GetEnvironmentVariable(CommitTagEnvironmentVariableName)))
            return null;
        var mrRef = this.Environment.GetEnvironmentVariable(MergeRequestRefPathEnvironmentVariableName);
        if (!string.IsNullOrEmpty(mrRef))
            return mrRef;
        return this.Environment.GetEnvironmentVariable(CommitRefNameEnvironmentVariableName);
    }

    public override bool PreventFetch() => true;

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (this.file is null)
            return;

        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");

        this.FileSystem.File.WriteAllLines(this.file, SetOutputVariables(variables));
    }
}
