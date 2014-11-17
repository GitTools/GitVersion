namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class Config
    {
        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            DevelopBranchTag = "unstable";
            ReleaseBranchTag = "beta";
            TagPrefix = "[vV]";
        }

        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlAlias("release-branch-tag")]
        public string ReleaseBranchTag { get; set; }

        [YamlAlias("tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlAlias("next-version")]
        public string NextVersion { get; set; }
    }
}