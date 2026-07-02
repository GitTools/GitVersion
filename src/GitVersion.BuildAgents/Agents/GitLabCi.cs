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

    // CI_MERGE_REQUEST_REF_PATH is only available in merge request pipelines,
    // CI_COMMIT_REF_NAME can contain either the branch or the tag
    // See https://docs.gitlab.com/ee/ci/variables/predefined_variables.html
    // CI_COMMIT_TAG is only available in tag pipelines,
    // so we can exit if CI_COMMIT_REF_NAME would return the tag
    public override string? GetCurrentBranch(bool usingDynamicRepos)
    {
        if (!string.IsNullOrEmpty(this.environment.GetEnvironmentVariable(CommitTagEnvironmentVariableName)))
        {
            return null;
        }
        var mergeRequestRefPath = this.environment.GetEnvironmentVariable(MergeRequestRefPathEnvironmentVariableName);
        if (!string.IsNullOrEmpty(mergeRequestRefPath))
        {
            return mergeRequestRefPath;
        }
        return this.environment.GetEnvironmentVariable(CommitRefNameEnvironmentVariableName);
    }

    public override bool PreventFetch() => true;

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
}
