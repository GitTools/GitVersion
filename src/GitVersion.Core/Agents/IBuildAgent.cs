using GitVersion.OutputVariables;

namespace GitVersion.Agents;

/// <summary>
/// Represents a CI/CD build agent integration that GitVersion can write version information to.
/// </summary>
public interface IBuildAgent
{
    /// <summary>Gets a value indicating whether this agent is the default fallback when no specific agent is detected.</summary>
    bool IsDefault { get; }

    /// <summary>Determines whether this build agent is active in the current execution context.</summary>
    bool CanApplyToCurrentContext();

    /// <summary>Returns the name of the current branch as reported by the build agent environment.</summary>
    string? GetCurrentBranch(bool usingDynamicRepos);

    /// <summary>Indicates whether fetching from the remote should be suppressed in this build agent environment.</summary>
    bool PreventFetch();

    /// <summary>Indicates whether remote tracking branches should be cleaned up after fetching.</summary>
    bool ShouldCleanUpRemotes();

    /// <summary>Writes the computed version variables to the build agent's integration output (e.g. environment variables or build properties).</summary>
    void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true);
}

/// <summary>Marker interface for the build agent that is active in the current execution context.</summary>
public interface ICurrentBuildAgent : IBuildAgent;
