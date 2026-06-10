using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Resolves the set of <see cref="EffectiveBranchConfiguration"/> instances that apply to a given branch.</summary>
public interface IEffectiveBranchConfigurationFinder
{
    /// <summary>Returns the effective branch configurations that match <paramref name="branch"/> under the given global <paramref name="configuration"/>.</summary>
    IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, IGitVersionConfiguration configuration);
}
