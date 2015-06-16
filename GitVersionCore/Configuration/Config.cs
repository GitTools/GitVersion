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
            ContinuousDeploymentFallbackTag = "ci";

            Branches["master"] = new BranchConfig
            {
                Tag = string.Empty,
                Increment = IncrementStrategy.Patch,
                PreventIncrementOfMergedBranchVersion = true
            };
            Branches["release[/-]"] = new BranchConfig { Tag = "beta" };
            Branches["feature[/-]"] = new BranchConfig
            {
                Increment = IncrementStrategy.Inherit,
                Tag = "useBranchName"
            };
            Branches["hotfix[/-]"] = new BranchConfig { Tag = "beta" };
            Branches["support[/-]"] = new BranchConfig
            {
                Tag = string.Empty,
                Increment = IncrementStrategy.Patch,
                PreventIncrementOfMergedBranchVersion = true
            };
            Branches["develop"] = new BranchConfig
            {
                Tag = "unstable",
                Increment = IncrementStrategy.Minor,
                VersioningMode = GitVersion.VersioningMode.ContinuousDeployment,
                TrackMergeTarget = true
            };
            Branches[@"(pull|pull\-requests|pr)[/-]"] = new BranchConfig
            {
                Tag = "PullRequest",
                TagNumberPattern = @"[/-](?<number>\d+)[-/]",
                Increment = IncrementStrategy.Inherit
            };
        }

        [YamlMember(Alias = "assembly-versioning-scheme")]
        public AssemblyVersioningScheme AssemblyVersioningScheme { get; set; }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        [YamlMember(Alias = "tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlMember(Alias = "continuous-delivery-fallback-tag")]
        public string ContinuousDeploymentFallbackTag { get; set; }

        [YamlMember(Alias = "next-version")]
        public string NextVersion { get; set; }

        [YamlMember(Alias="commits-to-ignore")]
        public string[] CommitsToIgnore { get; set; }

        [YamlMember(Alias = "merge-messages-to-ignore")]
        public string[] MergeMessagesToIgnore { get; set; }

        [YamlMember(Alias = "branches")]
        public Dictionary<string, BranchConfig> Branches
        {
            get
            {
                return branches;
            }
            set
            {
                value.ToList().ForEach(_ =>
                {
                    if (!branches.ContainsKey(_.Key))
                        branches.Add(_.Key, new BranchConfig());

                    branches[_.Key] = MergeObjects(branches[_.Key], _.Value);
                });
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
    }
}