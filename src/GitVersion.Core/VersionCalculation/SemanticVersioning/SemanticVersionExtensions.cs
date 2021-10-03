using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion;

public static class SemanticVersionExtensions
{
    public static void OverrideVersionManuallyIfNeeded(this SemanticVersion version, EffectiveConfiguration configuration)
    {
        if (!configuration.NextVersion.IsNullOrEmpty() && SemanticVersion.TryParse(configuration.NextVersion, configuration.GitTagPrefix, out var manualNextVersion))
        {
            if (manualNextVersion > version)
            {
                version.Major = manualNextVersion.Major;
                version.Minor = manualNextVersion.Minor;
                version.Patch = manualNextVersion.Patch;
            }
        }
    }
}
