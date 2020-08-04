using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using JetBrains.Annotations;

namespace GitVersion.Configuration
{
    public class ConfigurationBuilder
    {
        private const int DefaultTagPreReleaseWeight = 60000;

        private readonly List<Config> _overrides = new List<Config>();

        public ConfigurationBuilder Add([NotNull] Config config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _overrides.Add(config);
            return this;
        }

        [NotNull]
        public Config Build()
        {
            var config = CreateDefaultConfiguration();

            foreach (var overrideConfig in _overrides)
            {
                ApplyOverrides(config, overrideConfig);
            }

            FinalizeConfiguration(config);
            ValidateConfiguration(config);

            return config;
        }

        private static void ApplyOverrides([NotNull] Config targetConfig, [NotNull] Config overrideConfig)
        {
            targetConfig.AssemblyVersioningScheme = overrideConfig.AssemblyVersioningScheme ?? targetConfig.AssemblyVersioningScheme;
            targetConfig.AssemblyFileVersioningScheme = overrideConfig.AssemblyFileVersioningScheme ?? targetConfig.AssemblyFileVersioningScheme;
            targetConfig.AssemblyInformationalFormat = overrideConfig.AssemblyInformationalFormat ?? targetConfig.AssemblyInformationalFormat;
            targetConfig.AssemblyVersioningFormat = overrideConfig.AssemblyVersioningFormat ?? targetConfig.AssemblyVersioningFormat;
            targetConfig.AssemblyFileVersioningFormat = overrideConfig.AssemblyFileVersioningFormat ?? targetConfig.AssemblyFileVersioningFormat;
            targetConfig.VersioningMode = overrideConfig.VersioningMode ?? targetConfig.VersioningMode;
            targetConfig.TagPrefix = overrideConfig.TagPrefix ?? targetConfig.TagPrefix;
            targetConfig.ContinuousDeploymentFallbackTag = overrideConfig.ContinuousDeploymentFallbackTag ?? targetConfig.ContinuousDeploymentFallbackTag;
            targetConfig.NextVersion = overrideConfig.NextVersion ?? targetConfig.NextVersion;
            targetConfig.MajorVersionBumpMessage = overrideConfig.MajorVersionBumpMessage ?? targetConfig.MajorVersionBumpMessage;
            targetConfig.MinorVersionBumpMessage = overrideConfig.MinorVersionBumpMessage ?? targetConfig.MinorVersionBumpMessage;
            targetConfig.PatchVersionBumpMessage = overrideConfig.PatchVersionBumpMessage ?? targetConfig.PatchVersionBumpMessage;
            targetConfig.NoBumpMessage = overrideConfig.NoBumpMessage ?? targetConfig.NoBumpMessage;
            targetConfig.LegacySemVerPadding = overrideConfig.LegacySemVerPadding ?? targetConfig.LegacySemVerPadding;
            targetConfig.BuildMetaDataPadding = overrideConfig.BuildMetaDataPadding ?? targetConfig.BuildMetaDataPadding;
            targetConfig.CommitsSinceVersionSourcePadding = overrideConfig.CommitsSinceVersionSourcePadding ?? targetConfig.CommitsSinceVersionSourcePadding;
            targetConfig.TagPreReleaseWeight = overrideConfig.TagPreReleaseWeight ?? targetConfig.TagPreReleaseWeight;
            targetConfig.CommitMessageIncrementing = overrideConfig.CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
            targetConfig.Increment = overrideConfig.Increment ?? targetConfig.Increment;
            targetConfig.CommitDateFormat = overrideConfig.CommitDateFormat ?? targetConfig.CommitDateFormat;
            targetConfig.MergeMessageFormats = overrideConfig.MergeMessageFormats.Any() ? overrideConfig.MergeMessageFormats : targetConfig.MergeMessageFormats;
            targetConfig.UpdateBuildNumber = overrideConfig.UpdateBuildNumber ?? targetConfig.UpdateBuildNumber;

            if (overrideConfig.Ignore != null && !overrideConfig.Ignore.IsEmpty)
            {
                targetConfig.Ignore = overrideConfig.Ignore;
            }

            ApplyBranchOverrides(targetConfig, overrideConfig);
        }

        private static void ApplyBranchOverrides(Config targetConfig, Config overrideConfig)
        {
            if (overrideConfig.Branches != null && overrideConfig.Branches.Count > 0)
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

                foreach (var (key, source) in overrideConfig.Branches)
                {
                    if (!targetConfigBranches.TryGetValue(key, out var target))
                    {
                        target = BranchConfig.CreateDefaultBranchConfig(key);
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

        private static void FinalizeConfiguration(Config config)
        {
            config.Ignore ??= new IgnoreConfig();

            foreach (var (name, branchConfig) in config.Branches)
            {
                FinalizeBranchConfiguration(config, name, branchConfig);
            }
        }

        private static void FinalizeBranchConfiguration(Config config, string name, BranchConfig branchConfig)
        {
            branchConfig.Name = name;
            branchConfig.Increment ??= config.Increment ?? IncrementStrategy.Inherit;

            if (branchConfig.VersioningMode == null)
            {
                if (name == Config.DevelopBranchKey)
                {
                    branchConfig.VersioningMode = config.VersioningMode == VersioningMode.Mainline ? VersioningMode.Mainline : VersioningMode.ContinuousDeployment;
                }
                else
                {
                    branchConfig.VersioningMode = config.VersioningMode;
                }
            }

            if (branchConfig.IsSourceBranchFor != null)
            {
                foreach (var targetBranchName in branchConfig.IsSourceBranchFor)
                {
                    var targetBranchConfig = config.Branches[targetBranchName];
                    targetBranchConfig.SourceBranches ??= new HashSet<string>();
                    targetBranchConfig.SourceBranches.Add(name);
                }
            }
        }

        private static void ValidateConfiguration(Config config)
        {
            foreach (var (name, branchConfig) in config.Branches)
            {
                var regex = branchConfig.Regex;
                if (regex == null)
                {
                    throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'regex'{System.Environment.NewLine}" + "See https://gitversion.net/docs/configuration/ for more info");
                }

                var sourceBranches = branchConfig.SourceBranches;
                if (sourceBranches == null)
                {
                    throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'source-branches'{System.Environment.NewLine}" + "See https://gitversion.net/docs/configuration/ for more info");
                }

                var missingSourceBranches = sourceBranches.Where(sb => !config.Branches.ContainsKey(sb)).ToArray();
                if (missingSourceBranches.Any())
                    throw new ConfigurationException($"Branch configuration '{name}' defines these 'source-branches' that are not configured: '[{string.Join(",", missingSourceBranches)}]'{System.Environment.NewLine}" + "See https://gitversion.net/docs/configuration/ for more info");
            }
        }

        private static Config CreateDefaultConfiguration()
        {
            var config = new Config
            {
                AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
                AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
                TagPrefix = Config.DefaultTagPrefix,
                VersioningMode = VersioningMode.ContinuousDelivery,
                ContinuousDeploymentFallbackTag = "ci",
                MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
                MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
                PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
                NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
                CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
                LegacySemVerPadding = 4,
                BuildMetaDataPadding = 4,
                CommitsSinceVersionSourcePadding = 4,
                CommitDateFormat = "yyyy-MM-dd",
                UpdateBuildNumber = true,
                TagPreReleaseWeight = DefaultTagPreReleaseWeight
            };

            AddBranchConfig(Config.DevelopBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.DevelopBranchRegex,
                                SourceBranches = new HashSet<string>(),
                                Tag = "alpha",
                                Increment = IncrementStrategy.Minor,
                                TrackMergeTarget = true,
                                TracksReleaseBranches = true,
                                PreReleaseWeight = 0,
                            });

            AddBranchConfig(Config.MasterBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.MasterBranchRegex,
                                SourceBranches = new HashSet<string> { Config.DevelopBranchKey, Config.ReleaseBranchKey },
                                Tag = string.Empty,
                                PreventIncrementOfMergedBranchVersion = true,
                                Increment = IncrementStrategy.Patch,
                                IsMainline = true,
                                PreReleaseWeight = 55000,
                            });

            AddBranchConfig(Config.ReleaseBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.ReleaseBranchRegex,
                                SourceBranches = new HashSet<string> { Config.DevelopBranchKey, Config.MasterBranchKey, Config.SupportBranchKey, Config.ReleaseBranchKey },
                                Tag = "beta",
                                PreventIncrementOfMergedBranchVersion = true,
                                Increment = IncrementStrategy.None,
                                IsReleaseBranch = true,
                                PreReleaseWeight = 30000,
                            });

            AddBranchConfig(Config.FeatureBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.FeatureBranchRegex,
                                SourceBranches = new HashSet<string> { Config.DevelopBranchKey, Config.MasterBranchKey, Config.ReleaseBranchKey, Config.FeatureBranchKey, Config.SupportBranchKey, Config.HotfixBranchKey },
                                Increment = IncrementStrategy.Inherit,
                                PreReleaseWeight = 30000,
                            });

            AddBranchConfig(Config.PullRequestBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.PullRequestRegex,
                                SourceBranches = new HashSet<string> { Config.DevelopBranchKey, Config.MasterBranchKey, Config.ReleaseBranchKey, Config.FeatureBranchKey, Config.SupportBranchKey, Config.HotfixBranchKey },
                                Tag = "PullRequest",
                                TagNumberPattern = @"[/-](?<number>\d+)",
                                Increment = IncrementStrategy.Inherit,
                                PreReleaseWeight = 30000,
                            });

            AddBranchConfig(Config.HotfixBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.HotfixBranchRegex,
                                SourceBranches = new HashSet<string> { Config.DevelopBranchKey, Config.MasterBranchKey, Config.SupportBranchKey },
                                Tag = "beta",
                                Increment = IncrementStrategy.Patch,
                                PreReleaseWeight = 30000,
                            });

            AddBranchConfig(Config.SupportBranchKey,
                            new BranchConfig
                            {
                                Regex = Config.SupportBranchRegex,
                                SourceBranches = new HashSet<string> { Config.MasterBranchKey },
                                Tag = string.Empty,
                                PreventIncrementOfMergedBranchVersion = true,
                                Increment = IncrementStrategy.Patch,
                                IsMainline = true,
                                PreReleaseWeight = 55000,
                            });

            return config;

            void AddBranchConfig(string name, BranchConfig overrides)
            {
                config.Branches[name] = BranchConfig.CreateDefaultBranchConfig(name).Apply(overrides);
            }
        }
    }
}
