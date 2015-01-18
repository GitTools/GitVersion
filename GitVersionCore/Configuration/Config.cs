namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class Config
    {
        Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();

        public Config()
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
            TagPrefix = "[vV]";
            VersioningMode = GitVersion.VersioningMode.ContinuousDelivery;

            Branches["master"] = new BranchConfig
            {
                Tag = string.Empty,
                Increment = IncrementStrategy.Patch,
            };
            Branches["release[/-]"] = new BranchConfig { Tag = "beta" };
            Branches["feature[/-]"] = new BranchConfig
            {
                Increment = IncrementStrategy.Inherit,
                Tag = "useBranchName"
            };
            Branches["hotfix[/-]"] = new BranchConfig { Tag = "beta" };
            Branches["develop"] = new BranchConfig
            {
                Tag = "unstable",
                Increment = IncrementStrategy.Minor,
                VersioningMode = GitVersion.VersioningMode.ContinuousDeployment
            };
            Branches[@"(pull|pull\-requests|pr)[/-]"] = new BranchConfig
            {
                Tag = "PullRequest",
                Increment = IncrementStrategy.Inherit
            };
        }

        [YamlAlias("assembly-versioning-scheme")]
        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlAlias("mode")]
        public VersioningMode? VersioningMode { get; set; }

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
    }
}