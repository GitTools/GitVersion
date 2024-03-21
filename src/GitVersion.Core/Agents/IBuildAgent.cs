using GitVersion.OutputVariables;

namespace GitVersion.Agents;

public interface IBuildAgent
{
    bool IsDefault { get; }

    bool CanApplyToCurrentContext();
    string? GetCurrentBranch(bool usingDynamicRepos);
    bool PreventFetch();
    bool ShouldCleanUpRemotes();

    void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true);
}

public interface ICurrentBuildAgent : IBuildAgent;
