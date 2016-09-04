namespace GitVersion
{
    using YamlDotNet.Serialization;

    /// <summary>
    /// Obsolete properties are added to this, so we can check to see if they are used and provide good error messages for migration
    /// </summary>
    public class LegacyConfig : Config
    {
        public string assemblyVersioningScheme { get; set; }

        [YamlMember(Alias = "develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlMember(Alias = "release-branch-tag")]
        public string ReleaseBranchTag { get; set; }
    }
}