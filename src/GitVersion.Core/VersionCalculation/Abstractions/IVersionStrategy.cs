using GitVersion.Configuration;

namespace GitVersion.VersionCalculation;

/// <summary>Implements a strategy for discovering candidate base versions from a specific source (e.g. tags, branch names, merge messages).</summary>
public interface IVersionStrategy
{
    /// <summary>Returns the candidate base versions found by this strategy for the given branch <paramref name="configuration"/>.</summary>
    IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration);
}
