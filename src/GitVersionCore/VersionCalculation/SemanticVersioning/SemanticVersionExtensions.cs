using GitVersion.Model.Configuration;

namespace GitVersion
{
    public static class SemanticVersionExtensions
    {
        public static void OverrideVersionManuallyIfNeeded(this SemanticVersion version, EffectiveConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(configuration.NextVersion) && SemanticVersion.TryParse(configuration.NextVersion, configuration.GitTagPrefix, out var manualNextVersion))
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
}
