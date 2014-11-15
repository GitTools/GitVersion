namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class Config
    {
        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            DevelopBranchTag = "unstable";
            DevelopBranchName = "develop";
            ReleaseBranchTag = "beta";
            TagPrefix = "v";
        }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlAlias("develop-branch-name")]
        public string DevelopBranchName { get; set; }

        [YamlAlias("release-branch-tag")]
        public string ReleaseBranchTag { get; set; }

        [YamlAlias("tag-prefix")]
        public string TagPrefix { get; set; }
    }
}