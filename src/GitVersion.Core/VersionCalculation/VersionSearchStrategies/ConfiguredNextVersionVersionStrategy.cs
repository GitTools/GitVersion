using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is from NextVersion (the configuration value), unless the current commit is tagged.
/// BaseVersionSource is null.
/// Does not increment.
/// </summary>
internal sealed class ConfiguredNextVersionVersionStrategy(Lazy<GitVersionContext> contextLazy) : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!this.Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.ConfiguredNextVersion))
            yield break;

        var nextVersion = this.Context.Configuration.NextVersion;
        if (!nextVersion.IsNullOrEmpty())
        {
            var semanticVersion = SemanticVersion.Parse(
                nextVersion, this.Context.Configuration.TagPrefixPattern, this.Context.Configuration.SemanticVersionFormat
            );
            var label = configuration.Value.GetBranchSpecificLabel(this.Context.CurrentBranch.Name, null);

            if (semanticVersion.IsMatchForBranchSpecificLabel(label))
            {
                BaseVersionOperator? operation = null;
                if (!semanticVersion.IsPreRelease || label is not null && semanticVersion.PreReleaseTag.Name != label)
                {
                    operation = new BaseVersionOperator
                    {
                        Increment = VersionField.None,
                        ForceIncrement = false,
                        Label = label
                    };
                }

                yield return new BaseVersion("NextVersion in GitVersion configuration file", semanticVersion, sourceType: VersionIncrementSourceType.NextVersionConfig)
                {
                    Operator = operation
                };
            }
        }
    }
}
