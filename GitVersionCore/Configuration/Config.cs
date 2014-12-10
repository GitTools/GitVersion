namespace GitVersion
{
    using System;

    using YamlDotNet.Serialization;

    public class Config
    {
        VersioningMode versioningMode;

        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            TagPrefix = "[vV]";
            Release = new BranchConfig { Tag = "beta" };
            Develop = new BranchConfig { Tag = "unstable" };
            VersioningMode = VersioningMode.ContinuousDelivery;
            Develop.VersioningMode = VersioningMode.ContinuousDeployment;
        }

        [YamlAlias("assembly-versioning-scheme")]
        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("develop-branch-tag")]
        [Obsolete]
        public string DevelopBranchTag
        {
            set
            {
                Develop.Tag = value;
            }
            get
            {
                return Develop.Tag;
            }
        }

        [YamlAlias("release-branch-tag")]
        [Obsolete]
        public string ReleaseBranchTag 
        {
            set
            {
                Release.Tag = value;
            }
            get
            {
                return Release.Tag;
            }
        }

        [YamlAlias("mode")]
        public VersioningMode VersioningMode
        {
            get
            {
                return versioningMode;
            }
            set
            {
                Develop.VersioningMode = value;
                Release.VersioningMode = value;
                versioningMode = value;
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