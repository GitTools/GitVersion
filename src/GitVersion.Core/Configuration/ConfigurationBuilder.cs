using GitVersion.Extensions;
using GitVersion.Model.Configurations;
using GitVersion.VersionCalculation;

namespace GitVersion.Configurations;

public class ConfigurationBuilder
{
    private const int DefaultTagPreReleaseWeight = 60000;

    private readonly List<Configuration> overrides = new();

    public ConfigurationBuilder Add(Configuration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        this.overrides.Add(configuration);
        return this;
    }

    public Configuration Build()
    {
        var config = CreateDefaultConfiguration();

        foreach (var overrideConfig in this.overrides)
        {
            ApplyOverrides(config, overrideConfig);
        }

        FinalizeConfiguration(config);
        ValidateConfiguration(config);

        return config;
    }

    private static void ApplyOverrides(Configuration targetConfig, Configuration overrideConfig)
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
        targetConfig.TagPreReleaseWeight = overrideConfig.TagPreReleaseWeight ?? targetConfig.TagPreReleaseWeight;
        targetConfig.CommitMessageIncrementing = overrideConfig.CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
        targetConfig.Increment = overrideConfig.Increment ?? targetConfig.Increment;
        targetConfig.CommitDateFormat = overrideConfig.CommitDateFormat ?? targetConfig.CommitDateFormat;
        targetConfig.MergeMessageFormats = overrideConfig.MergeMessageFormats.Any() ? overrideConfig.MergeMessageFormats : targetConfig.MergeMessageFormats;
        targetConfig.UpdateBuildNumber = overrideConfig.UpdateBuildNumber ?? targetConfig.UpdateBuildNumber;
        targetConfig.SemanticVersionFormat = overrideConfig.SemanticVersionFormat;

        if (overrideConfig.Ignore is { IsEmpty: false })
        {
            targetConfig.Ignore = overrideConfig.Ignore;
        }

        ApplyBranchOverrides(targetConfig, overrideConfig);
    }

    private static void ApplyBranchOverrides(Configuration targetConfig, Configuration overrideConfig)
    {
        if (overrideConfig.Branches is { Count: > 0 })
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

            foreach (var (name, branchConfig) in overrideConfig.Branches)
            {
                // for compatibility reason we check if it's master, we rename it to main
                var branchName = name == Configuration.MasterBranchKey ? Configuration.MainBranchKey : name;
                if (!targetConfigBranches.TryGetValue(branchName, out var target))
                {
                    target = new BranchConfig() { Name = branchName };
                }

                branchConfig.MergeTo(target);
                if (target.SourceBranches != null && target.SourceBranches.Contains(Configuration.MasterBranchKey))
                {
                    target.SourceBranches.Remove(Configuration.MasterBranchKey);
                    target.SourceBranches.Add(Configuration.MainBranchKey);
                }
                newBranches[branchName] = target;
            }

            foreach (var (name, branchConfig) in targetConfigBranches)
            {
                if (!newBranches.ContainsKey(name))
                {
                    newBranches[name] = branchConfig;
                }
            }

            targetConfig.Branches = newBranches;
        }
    }

    private static void FinalizeConfiguration(Configuration config)
    {
        foreach (var (name, branchConfig) in config.Branches)
        {
            FinalizeBranchConfiguration(config, name, branchConfig);
        }
    }

    private static void FinalizeBranchConfiguration(Configuration config, string name, BranchConfig branchConfig)
    {
        branchConfig.Name = name;
        branchConfig.Increment ??= config.Increment ?? IncrementStrategy.Inherit;

        if (branchConfig.VersioningMode == null)
        {
            if (name == Configuration.DevelopBranchKey)
            {
                // Why this applies only on develop branch? I'm surprised that the value not coming from configuration.
                branchConfig.VersioningMode = config.VersioningMode == VersioningMode.Mainline ? VersioningMode.Mainline : VersioningMode.ContinuousDeployment;
            }
            else
            {
                branchConfig.VersioningMode = config.VersioningMode;
            }
        }

        if (branchConfig.IsSourceBranchFor == null)
            return;

        foreach (var targetBranchName in branchConfig.IsSourceBranchFor)
        {
            var targetBranchConfig = config.Branches[targetBranchName];
            targetBranchConfig.SourceBranches ??= new HashSet<string>();
            targetBranchConfig.SourceBranches.Add(name);
        }
    }

    private static void ValidateConfiguration(Configuration config)
    {
        foreach (var (name, branchConfig) in config.Branches)
        {
            var regex = branchConfig?.Regex;
            var helpUrl = $"{System.Environment.NewLine}See https://gitversion.net/docs/reference/configuration for more info";

            if (regex == null)
            {
                throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'regex'{helpUrl}");
            }

            var sourceBranches = branchConfig?.SourceBranches;
            if (sourceBranches == null)
            {
                throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'source-branches'{helpUrl}");
            }

            var missingSourceBranches = sourceBranches.Where(sb => !config.Branches.ContainsKey(sb)).ToArray();
            if (missingSourceBranches.Any())
                throw new ConfigurationException($"Branch configuration '{name}' defines these 'source-branches' that are not configured: '[{string.Join(",", missingSourceBranches)}]'{helpUrl}");
        }
    }

    private static Configuration CreateDefaultConfiguration()
    {
        var config = new Configuration
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            TagPrefix = Configuration.DefaultTagPrefix,
            VersioningMode = VersioningMode.ContinuousDelivery,
            ContinuousDeploymentFallbackTag = "ci",
            MajorVersionBumpMessage = IncrementStrategyFinder.DefaultMajorPattern,
            MinorVersionBumpMessage = IncrementStrategyFinder.DefaultMinorPattern,
            PatchVersionBumpMessage = IncrementStrategyFinder.DefaultPatchPattern,
            NoBumpMessage = IncrementStrategyFinder.DefaultNoBumpPattern,
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            CommitDateFormat = "yyyy-MM-dd",
            UpdateBuildNumber = true,
            SemanticVersionFormat = SemanticVersionFormat.Strict,
            TagPreReleaseWeight = DefaultTagPreReleaseWeight,
            Increment = IncrementStrategy.Inherit
        };

        AddBranchConfig(Configuration.DevelopBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Minor,
                Regex = Configuration.DevelopBranchRegex,
                SourceBranches = new HashSet<string>(),
                Tag = "alpha",
                PreventIncrementOfMergedBranchVersion = false,
                TrackMergeTarget = true,
                TracksReleaseBranches = true,
                IsMainline = false,
                IsReleaseBranch = false,
                PreReleaseWeight = 0
            });

        AddBranchConfig(Configuration.MainBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Patch,
                Regex = Configuration.MainBranchRegex,
                SourceBranches = new HashSet<string> {
                    Configuration.DevelopBranchKey,
                    Configuration.ReleaseBranchKey
                },
                Tag = string.Empty,
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = true,
                IsReleaseBranch = false,
                PreReleaseWeight = 55000
            });

        AddBranchConfig(Configuration.ReleaseBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.None,
                Regex = Configuration.ReleaseBranchRegex,
                SourceBranches = new HashSet<string> {
                    Configuration.DevelopBranchKey,
                    Configuration.MainBranchKey,
                    Configuration.SupportBranchKey,
                    Configuration.ReleaseBranchKey
                },
                Tag = "beta",
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = false,
                IsReleaseBranch = true,
                PreReleaseWeight = 30000
            });

        AddBranchConfig(Configuration.FeatureBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Inherit,
                Regex = Configuration.FeatureBranchRegex,
                SourceBranches = new HashSet<string> {
                    Configuration.DevelopBranchKey,
                    Configuration.MainBranchKey,
                    Configuration.ReleaseBranchKey,
                    Configuration.FeatureBranchKey,
                    Configuration.SupportBranchKey,
                    Configuration.HotfixBranchKey
                },
                Tag = "{BranchName}",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(Configuration.PullRequestBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Inherit,
                Regex = Configuration.PullRequestRegex,
                SourceBranches = new HashSet<string> {
                    Configuration.DevelopBranchKey,
                    Configuration.MainBranchKey,
                    Configuration.ReleaseBranchKey,
                    Configuration.FeatureBranchKey,
                    Configuration.SupportBranchKey,
                    Configuration.HotfixBranchKey
                },
                Tag = "PullRequest",
                TagNumberPattern = @"[/-](?<number>\d+)",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(Configuration.HotfixBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Inherit,
                Regex = Configuration.HotfixBranchRegex,
                SourceBranches = new HashSet<string> {
                    Configuration.ReleaseBranchKey,
                    Configuration.MainBranchKey,
                    Configuration.SupportBranchKey,
                    Configuration.HotfixBranchKey
                },
                Tag = "beta",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(Configuration.SupportBranchKey,
            new BranchConfig
            {
                Increment = IncrementStrategy.Patch,
                Regex = Configuration.SupportBranchRegex,
                SourceBranches = new HashSet<string> { Configuration.MainBranchKey },
                Tag = string.Empty,
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = true,
                IsReleaseBranch = false,
                PreReleaseWeight = 55000
            });

        return config;

        void AddBranchConfig(string name, BranchConfig branchConfiguration)
        {
            branchConfiguration.Name = name;
            config.Branches[name] = branchConfiguration;
        }
    }
}
