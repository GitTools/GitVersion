using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Model.Configuration
{
    public class Config
    {
        private readonly Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();
        private string nextVersion;

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
            get => nextVersion;
            set =>
                nextVersion = Int32.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var major)
                    ? $"{major}.0"
                    : value;
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
            get => branches;
            set
            {
                value.ToList().ForEach(_ =>
                {
                    if (!branches.ContainsKey(_.Key))
                        branches.Add(_.Key, new BranchConfig { Name = _.Key });

                    branches[_.Key] = MergeObjects(branches[_.Key], _.Value);
                });
            }
        }

        private static T MergeObjects<T>(T target, T source)
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

        [YamlMember(Alias = "increment")]
        public IncrementStrategy? Increment { get; set; }

        [YamlMember(Alias = "commit-date-format")]
        public string CommitDateFormat { get; set; }

        [YamlMember(Alias = "merge-message-formats")]
        public Dictionary<string, string> MergeMessageFormats { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerializer.Write(this, stream);
                stream.Flush();
            }
            return stringBuilder.ToString();
        }

        public const string DefaultTagPrefix = "[vV]";
        public const string ReleaseBranchRegex = "^releases?[/-]";
        public const string FeatureBranchRegex = "^features?[/-]";
        public const string PullRequestRegex = @"^(pull|pull\-requests|pr)[/-]";
        public const string HotfixBranchRegex = "^hotfix(es)?[/-]";
        public const string SupportBranchRegex = "^support[/-]";
        public const string DevelopBranchRegex = "^dev(elop)?(ment)?$";
        public const string MasterBranchRegex = "^master$";
        public const string MasterBranchKey = "master";
        public const string ReleaseBranchKey = "release";
        public const string FeatureBranchKey = "feature";
        public const string PullRequestBranchKey = "pull-request";
        public const string HotfixBranchKey = "hotfix";
        public const string SupportBranchKey = "support";
        public const string DevelopBranchKey = "develop";
    }
}
