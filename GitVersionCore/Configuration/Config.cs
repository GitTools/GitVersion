namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class Config
    {
        VersioningMode versioningMode;

        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            DevelopBranchTag = "unstable";
            ReleaseBranchTag = "beta";
            TagPrefix = "[vV]";
            Release = new BranchConfig();
            Develop = new BranchConfig();
            VersioningMode = VersioningMode.ContinuousDelivery;
        }

        [YamlAlias("assembly-versioning-scheme")]
        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlAlias("release-branch-tag")]
        public string ReleaseBranchTag { get; set; }

        [YamlAlias("mode")]
        public VersioningMode VersioningMode
        {
            get
            {
                return this.versioningMode;
            }
            set
            {
                Develop.VersioningMode = value;
                Release.VersioningMode = value;
                this.versioningMode = value;
            }
        }

        [YamlAlias("develop")]
        public BranchConfig Develop { get; set; }

        [YamlAlias("release*")]
        public BranchConfig Release { get; set; }


        [YamlAlias("tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlAlias("next-version")]
        public string NextVersion { get; set; }
    }
}