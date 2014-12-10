namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    public class Config
    {
        VersioningMode versioningMode;

        Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();

        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            TagPrefix = "[vV]";
            Branches["release[/-]"] = new BranchConfig { Tag = "beta" };
            Branches["develop"] = new BranchConfig { Tag = "unstable" };
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
                Branches.ToList().ForEach(b => b.Value.VersioningMode = value);
                versioningMode = value;
            }
        }

        [YamlAlias("branches")]
        public Dictionary<string, BranchConfig> Branches
        {
            get
            {
                return branches;
            }
            set
            {
                value.ToList().ForEach(_ => branches[_.Key] = _.Value);
            }
        }

        [YamlAlias("tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlAlias("next-version")]
        public string NextVersion { get; set; }

        [Obsolete]
        public BranchConfig Develop
        {
            get
            {
                return Branches["develop"];
            }
        }

        [Obsolete]
        public BranchConfig Release
        {
            get
            {
                return Branches["release[/-]"];
            }
        }
    }
}