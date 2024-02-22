using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is from NextVersion (the configuration value), unless the current commit is tagged.
/// BaseVersionSource is null.
/// Does not increment.
/// </summary>
internal class ConfiguredNextVersionVersionStrategy(Lazy<GitVersionContext> versionContext) : VersionStrategyBase(versionContext)
{
    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.ConfiguredNextVersion))
            yield break;

        var contextConfiguration = Context.Configuration;
        var nextVersion = contextConfiguration.NextVersion;
        if (!nextVersion.IsNullOrEmpty())
        {
            var semanticVersion = SemanticVersion.Parse(nextVersion, contextConfiguration.TagPrefix, contextConfiguration.SemanticVersionFormat);
            yield return new("NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
        }
    }
}
