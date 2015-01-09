namespace GitVersion
{
    /// <summary>
    /// Configuration can be applied to different things, effective configuration is the result after applying the appropriate configuration
    /// </summary>
    public class EffectiveConfiguration
    {
        public EffectiveConfiguration(AssemblyVersioningScheme assemblyVersioningScheme, VersioningMode versioningMode, string gitTagPrefix, string tag, string nextVersion)
        {
            AssemblyVersioningScheme = assemblyVersioningScheme;
            VersioningMode = versioningMode;
            GitTagPrefix = gitTagPrefix;
            Tag = tag;
            NextVersion = nextVersion;
        }

        public VersioningMode VersioningMode { get; private set; }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; private set; }

        /// <summary>
        /// Git tag prefix
        /// </summary>
        public string GitTagPrefix { get; private set; }

        /// <summary>
        /// Tag to use when calculating SemVer
        /// </summary>
        public string Tag { get; private set; }

        public string NextVersion { get; private set; }
    }
}