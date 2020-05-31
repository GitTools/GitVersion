using System.Collections.Generic;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration
{
    public class DefaultConfigProvider
    {
        private const int DefaultTagPreReleaseWeight = 60000;

        public static Config CreateDefaultConfig()
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
                config.Branches[name] = CreateDefaultBranchConfig(name).Apply(overrides);
            }
        }

        public static BranchConfig CreateDefaultBranchConfig(string name)
        {
            return new BranchConfig
            {
                Name = name,
                Tag = "useBranchName",
                PreventIncrementOfMergedBranchVersion = false,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsReleaseBranch = false,
                IsMainline = false,
            };
        }
    }
}
