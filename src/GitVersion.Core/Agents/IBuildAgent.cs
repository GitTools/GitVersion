using GitVersion.OutputVariables;

namespace GitVersion.Agents;

public interface IBuildAgent
{
    bool IsDefault { get; }

    bool CanApplyToCurrentContext();
    void WriteIntegration(Action<string?> writer, VersionVariables variables, bool updateBuildNumber = true);
    string? GetCurrentBranch(bool usingDynamicRepos);
    bool PreventFetch();
    bool ShouldCleanUpRemotes();
}

public interface ICurrentBuildAgent : IBuildAgent { }
