namespace GitVersion.Configuration
{
    using YamlDotNet.Serialization;

    public class Config
    {
        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            DevelopBranchTag = "unstable";
            ReleaseBranchTag = "beta";
            TagPrefix = "v";
        }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlAlias("release-branch-tag")]
        public string ReleaseBranchTag { get; set; }

        [YamlAlias("tag-prefix")]
        public string TagPrefix { get; set; }
    }
}