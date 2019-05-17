namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
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

        [YamlMember(Alias = "assembly-file-versioning-scheme")]
        public AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; set; }

        [YamlMember(Alias = "assembly-informational-format")]
        public string AssemblyInformationalFormat { get; set; }

        [YamlMember(Alias = "assembly-versioning-format")]
        public string AssemblyVersioningFormat { get; set; }

        [YamlMember(Alias = "assembly-file-versioning-format")]
        public string AssemblyFileVersioningFormat { get; set; }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        [YamlMember(Alias = "tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlMember(Alias = "continuous-delivery-fallback-tag")]
        public string ContinuousDeploymentFallbackTag { get; set; }

        [YamlMember(Alias = "next-version")]
        public string NextVersion
        {
            get { return nextVersion; }
            set
            {
                int major;
                nextVersion = int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out major)
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
                        branches.Add(_.Key, new BranchConfig {Name = _.Key});

                    branches[_.Key] = MergeObjects(branches[_.Key], _.Value);
                });
            }
        }

        public BranchConfig GetConfigForBranch(string branchName)
        {
            if (branchName == null) throw new ArgumentNullException(nameof(branchName));
            var matches = Branches
                .Where(b => Regex.IsMatch(branchName, b.Value.Regex, RegexOptions.IgnoreCase))
                .ToArray();

            try
            {
                return matches
                    .Select(kvp => kvp.Value)
                    .SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                var matchingConfigs = string.Join("\n - ", matches.Select(m => m.Key));
                var picked = matches
                    .Select(kvp => kvp.Value)
                    .First();

                Logger.WriteWarning(
                    $"Multiple branch configurations match the current branch branchName of '{branchName}'. " +
                    $"Using the first matching configuration, '{picked}'. Matching configurations include: '{matchingConfigs}'");

                return picked;
            }
        }

        T MergeObjects<T>(T target, T source)
        {
            typeof(T).GetProperties()
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Select(_ => new { prop = _, value = _.GetValue(source, null) })
                .Where(_ => _.value != null)
                .ToList()
                .ForEach(_ => _.prop.SetValue(target, _.value, null));
            return target;
        }

        public bool IsReleaseBranch(string branchName) => GetConfigForBranch(branchName)?.IsReleaseBranch ?? false;

        [YamlMember(Alias = "ignore")]
        public IgnoreConfig Ignore { get; set; }

        [YamlMember(Alias = "increment")]
        public IncrementStrategy? Increment { get; set; }

        [YamlMember(Alias = "commit-date-format")]
        public string CommitDateFormat { get; set; }

        [YamlMember(Alias = "merge-message-formats")]
        public Dictionary<string, string> MergeMessageFormats { get; set; } = new Dictionary<string, string>();
    }
}
