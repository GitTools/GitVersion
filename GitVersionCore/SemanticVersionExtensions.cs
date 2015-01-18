namespace GitVersion
{
    using LibGit2Sharp;

    public static class SemanticVersionExtensions
    {
        public static void OverrideVersionManuallyIfNeeded(this SemanticVersion version, IRepository repository, EffectiveConfiguration configuration)
        {
            SemanticVersion manualNextVersion;
            if (!string.IsNullOrEmpty(configuration.NextVersion) && SemanticVersion.TryParse(configuration.NextVersion, configuration.GitTagPrefix, out manualNextVersion))
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