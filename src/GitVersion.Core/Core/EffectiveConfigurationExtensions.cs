using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.Core;

internal static class EffectiveConfigurationExtensions
{
    public static TaggedSemanticVersions GetTaggedSemanticVersion(this EffectiveConfiguration effectiveConfiguration)
    {
        effectiveConfiguration.NotNull();

        var taggedSemanticVersion = TaggedSemanticVersions.OfBranch;

        if (effectiveConfiguration.TrackMergeTarget)
        {
            taggedSemanticVersion |= TaggedSemanticVersions.OfMergeTargets;
        }

        if (effectiveConfiguration.TracksReleaseBranches)
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
