using LibGit2Sharp;
using GitVersion.Configuration;

namespace GitVersion
{
    public static class SemanticVersionExtensions
    {
        public static void OverrideVersionManuallyIfNeeded(this SemanticVersion version, IRepository repository, EffectiveConfiguration configuration)
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