using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.Core;

internal static class EffectiveConfigurationExtensions
{
    public static TaggedSemanticVersions GetTaggedSemanticVersion(this EffectiveConfiguration effectiveConfiguration)
    {
        effectiveConfiguration.NotNull();

        TaggedSemanticVersions taggedSemanticVersion = TaggedSemanticVersions.OfBranch;

        if (effectiveConfiguration.TrackMergeTarget)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
        }

        if (effectiveConfiguration.TrackMergeTarget)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfReleaseBranches;
        }

        if (!effectiveConfiguration.IsMainBranch && !effectiveConfiguration.IsReleaseBranch)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMainBranches;
        }
        return taggedSemanticVersion;
    }
}
