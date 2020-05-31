using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace GitVersion.Model.Configuration
{
    public class Config
    {
        private string nextVersion;

        public Config()
        {
            Branches = new Dictionary<string, BranchConfig>();
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

        [YamlMember(Alias = "tag-pre-release-weight")]
        public int? TagPreReleaseWeight { get; set; }

        [YamlMember(Alias = "commit-message-incrementing")]
        public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

        [YamlMember(Alias = "branches")]
        public Dictionary<string, BranchConfig> Branches { get; set; }

        [YamlMember(Alias = "ignore")]
        public IgnoreConfig Ignore { get; set; }

        [YamlMember(Alias = "increment")]
        public IncrementStrategy? Increment { get; set; }

        [YamlMember(Alias = "commit-date-format")]
        public string CommitDateFormat { get; set; }

        [YamlMember(Alias = "merge-message-formats")]
        public Dictionary<string, string> MergeMessageFormats { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "update-build-number")]
        public bool? UpdateBuildNumber { get; set; }

        public virtual void MergeTo([NotNull] Config targetConfig)
        {
            if (targetConfig == null) throw new ArgumentNullException(nameof(targetConfig));

            targetConfig.AssemblyVersioningScheme = this.AssemblyVersioningScheme ?? targetConfig.AssemblyVersioningScheme;
            targetConfig.AssemblyFileVersioningScheme = this.AssemblyFileVersioningScheme ?? targetConfig.AssemblyFileVersioningScheme;
            targetConfig.AssemblyInformationalFormat = this.AssemblyInformationalFormat ?? targetConfig.AssemblyInformationalFormat;
            targetConfig.AssemblyVersioningFormat = this.AssemblyVersioningFormat ?? targetConfig.AssemblyVersioningFormat;
            targetConfig.AssemblyFileVersioningFormat = this.AssemblyFileVersioningFormat ?? targetConfig.AssemblyFileVersioningFormat;
            targetConfig.VersioningMode = this.VersioningMode ?? targetConfig.VersioningMode;
            targetConfig.TagPrefix = this.TagPrefix ?? targetConfig.TagPrefix;
            targetConfig.ContinuousDeploymentFallbackTag = this.ContinuousDeploymentFallbackTag ?? targetConfig.ContinuousDeploymentFallbackTag;
            targetConfig.NextVersion = this.NextVersion ?? targetConfig.NextVersion;
            targetConfig.MajorVersionBumpMessage = this.MajorVersionBumpMessage ?? targetConfig.MajorVersionBumpMessage;
            targetConfig.MinorVersionBumpMessage = this.MinorVersionBumpMessage ?? targetConfig.MinorVersionBumpMessage;
            targetConfig.PatchVersionBumpMessage = this.PatchVersionBumpMessage ?? targetConfig.PatchVersionBumpMessage;
            targetConfig.NoBumpMessage = this.NoBumpMessage ?? targetConfig.NoBumpMessage;
            targetConfig.LegacySemVerPadding = this.LegacySemVerPadding ?? targetConfig.LegacySemVerPadding;
            targetConfig.BuildMetaDataPadding = this.BuildMetaDataPadding ?? targetConfig.BuildMetaDataPadding;
            targetConfig.CommitsSinceVersionSourcePadding = this.CommitsSinceVersionSourcePadding ?? targetConfig.CommitsSinceVersionSourcePadding;
            targetConfig.TagPreReleaseWeight = this.TagPreReleaseWeight ?? targetConfig.TagPreReleaseWeight;
            targetConfig.CommitMessageIncrementing = this.CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
            targetConfig.Increment = this.Increment ?? targetConfig.Increment;
            targetConfig.CommitDateFormat = this.CommitDateFormat ?? targetConfig.CommitDateFormat;
            targetConfig.MergeMessageFormats = this.MergeMessageFormats ?? targetConfig.MergeMessageFormats;
            targetConfig.UpdateBuildNumber = this.UpdateBuildNumber ?? targetConfig.UpdateBuildNumber;

            if (this.Ignore != null && !this.Ignore.IsEmpty)
            {
                targetConfig.Ignore = this.Ignore;
            }

            if (this.Branches != null && this.Branches.Count > 0)
            {
                // We can't just add new configs to the targetConfig.Branches, and have to create a new dictionary.
                // The reason is that GitVersion 5.3.x (and earlier) merges default configs into overrides. The new approach is opposite: we merge overrides into default config.
                // The important difference of these approaches is the order of branches in a dictionary (we should not rely on Dictionary's implementation details, but we already did that):
                // Old approach: { new-branch-1, new-branch-2, default-branch-1, default-branch-2, ... }
                // New approach: { default-branch-1, default-branch-2, ..., new-branch-1, new-branch-2 }
                // In case when several branch configurations match the current branch (by regex), we choose the first one.
                // So we have to add new branches to the beginning of a dictionary to preserve 5.3.x behavior.

                var newBranches = new Dictionary<string, BranchConfig>();

                var targetConfigBranches = targetConfig.Branches;

                foreach (var (key, source) in this.Branches)
                {
                    if (!targetConfigBranches.TryGetValue(key, out var target))
                    {
                        target = DefaultConfigProvider.CreateDefaultBranchConfig(key);
                    }

                    source.MergeTo(target);
                    newBranches[key] = target;
                }

                foreach (var (key, branchConfig) in targetConfigBranches)
                {
                    if (!newBranches.ContainsKey(key))
                    {
                        newBranches[key] = branchConfig;
                    }
                }

                targetConfig.Branches = newBranches;
            }
        }

        public Config FinalizeConfig()
        {
            Ignore ??= new IgnoreConfig();

            foreach (var (name, branchConfig) in Branches)
            {
                branchConfig.Increment ??= Increment ?? IncrementStrategy.Inherit;

                if (branchConfig.VersioningMode == null)
                {
                    if (name == DevelopBranchKey)
                    {
                        branchConfig.VersioningMode = VersioningMode == VersionCalculation.VersioningMode.Mainline
                                                          ? VersionCalculation.VersioningMode.Mainline
                                                          : VersionCalculation.VersioningMode.ContinuousDeployment;
                    }
                    else
                    {
                        branchConfig.VersioningMode = VersioningMode;
                    }
                }

                if (branchConfig.IsSourceBranchFor != null)
                {
                    foreach (var targetBranchName in branchConfig.IsSourceBranchFor)
                    {
                        var targetBranchConfig = Branches[targetBranchName];
                        targetBranchConfig.SourceBranches ??= new HashSet<string>();
                        targetBranchConfig.SourceBranches.Add(name);
                    }
                }
            }

            ValidateConfiguration();

            return this;
        }

        public void ValidateConfiguration()
        {
            foreach (var (name, branchConfig) in this.Branches)
            {
                var regex = branchConfig.Regex;
                if (regex == null)
                {
                    throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'regex'{System.Environment.NewLine}" +
                                                     "See https://gitversion.net/docs/configuration/ for more info");
                }

                var sourceBranches = branchConfig.SourceBranches;
                if (sourceBranches == null)
                {
                    throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'source-branches'{System.Environment.NewLine}" +
                                                     "See https://gitversion.net/docs/configuration/ for more info");
                }
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            using var stream = new StringWriter(stringBuilder);
            ConfigSerializer.Write(this, stream);
            stream.Flush();
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

        public Config Apply([NotNull] Config overrides)
        {
            if (overrides == null) throw new ArgumentNullException(nameof(overrides));

            overrides.MergeTo(this);
            return this;
        }
    }
}
