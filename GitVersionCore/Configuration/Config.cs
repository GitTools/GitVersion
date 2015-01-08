namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using GitVersion.Configuration;

    using YamlDotNet.Serialization;

    public class Config
    {
        VersioningMode versioningMode;

        Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();

        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            TagPrefix = "[vV]";
            Branches["release[/-]"] = new BranchConfig
            {
                Tag = "beta",
                ReferenceBranch = "develop",
                IncrementType = IncrementType.PreReleaseTag,
                Prefixes = new List<string> { "release-", "release/" },
                IncrementOnTag = false
            };
            Branches["develop"] = new BranchConfig
            {
                Tag = "unstable",
                IncrementType = IncrementType.Minor,
                ReferenceBranch = "master",
                LastTagReferenceBranch = "master",
                IncrementOnTag = true,
                ForceBuildMetdataFromReferenceBranch = true
            };
            Branches["hotfix[/-]"] = new BranchConfig()
            {
                Tag = "beta",
                ReferenceBranch = "master",
                IncrementType = IncrementType.PreReleaseTag,
                Prefixes = new List<string> { "hotifx-", "hotfix/" },
                IncrementOnTag = false,
                ForceBuildMetdataFromReferenceBranch = true
            };
            Branches["master"] = new BranchConfig()
            {
                Tag = ""
            };
            Branches["support[/-]"] = new BranchConfig()
            {
                Tag = "",
                ReferenceBranch = "master"
            };
            Branches["feature[/-]"] = new BranchConfig()
            {
                Tag = "feature",
                ReferenceBranch = "develop"
            };
            Branches["(pull)|(pull-requests)\\/"] = new BranchConfig()
            {
                Tag = "PullRequest",
                IncrementType = IncrementType.Minor,
                ReferenceBranch = "master",
                LastTagReferenceBranch = "master",
                IncrementOnTag = true,
                ForceBuildMetdataFromReferenceBranch = true
            };
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
                value.ToList().ForEach(_ => branches[_.Key] = MergeObjects(branches[_.Key],  _.Value));
            }
        }

        private T MergeObjects<T>(T target, T source)
        {
            typeof(T).GetProperties()
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Select(_ => new {prop = _, value =_.GetValue(source, null) } )
                .Where(_ => _.value != null)
                .ToList()
                .ForEach(_ => _.prop.SetValue(target, _.value, null));
            return target;
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