namespace GitVersion
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class Config
    {
        Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();
        string nextVersion;

        public Config()
        {
            Ignore = new IgnoreConfig();
        }

        [YamlMember(Alias = "assembly-versioning-scheme")]
        public AssemblyVersioningScheme? AssemblyVersioningScheme { get; set; }

        [YamlMember(Alias = "assembly-informational-format")]
        public string AssemblyInformationalFormat { get; set; }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        [YamlMember(Alias = "tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlMember(Alias = "continuous-delivery-fallback-tag")]
        public string ContinuousDeploymentFallbackTag { get; set; }

        [YamlMember(Alias = "next-version")]
        public string NextVersion
        {
            get { return this.nextVersion; }
            set
            {
                int major;
                this.nextVersion = int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out major)
                    ? string.Format("{0}.0", major)
                    : value;
            }
        }

        [YamlMember(Alias = "major-version-bump-message")]
        public string MajorVersionBumpMessage { get; set; }

        [YamlMember(Alias = "minor-version-bump-message")]
        public string MinorVersionBumpMessage { get; set; }

        [YamlMember(Alias = "patch-version-bump-message")]
        public string PatchVersionBumpMessage { get; set; }

        [YamlMember(Alias = "no-bump-message")]
        public string NoBumpMessage { get; set; }

        [YamlMember(Alias = "legacy-semver-padding")]
        public int? LegacySemVerPadding { get; set; }

        [YamlMember(Alias = "build-metadata-padding")]
        public int? BuildMetaDataPadding { get; set; }

        [YamlMember(Alias = "commits-since-version-source-padding")]
        public int? CommitsSinceVersionSourcePadding { get; set; }

        [YamlMember(Alias = "commit-message-incrementing")]
        public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

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
                .Select(_ => new { prop = _, value = _.GetValue(source, null) })
                .Where(_ => _.value != null)
                .ToList()
                .ForEach(_ => _.prop.SetValue(target, _.value, null));
            return target;
        }

        [YamlMember(Alias = "ignore")]
        public IgnoreConfig Ignore { get; set; }
    }
}