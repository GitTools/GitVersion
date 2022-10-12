using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public class ConfigurationBuilder
{
    private const int DefaultTagPreReleaseWeight = 60000;

    private readonly List<GitVersionConfiguration> overrides = new();

    public ConfigurationBuilder Add(GitVersionConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        this.overrides.Add(configuration);
        return this;
    }

    public GitVersionConfiguration Build()
    {
        var configuration = CreateDefaultConfiguration();

        foreach (var overrideConfiguration in this.overrides)
        {
            ApplyOverrides(configuration, overrideConfiguration);
        }

        FinalizeConfiguration(configuration);
        ValidateConfiguration(configuration);

        return configuration;
    }

    private static void ApplyOverrides(GitVersionConfiguration targetConfig, GitVersionConfiguration overrideConfiguration)
    {
        targetConfig.AssemblyVersioningScheme = overrideConfiguration.AssemblyVersioningScheme ?? targetConfig.AssemblyVersioningScheme;
        targetConfig.AssemblyFileVersioningScheme = overrideConfiguration.AssemblyFileVersioningScheme ?? targetConfig.AssemblyFileVersioningScheme;
        targetConfig.AssemblyInformationalFormat = overrideConfiguration.AssemblyInformationalFormat ?? targetConfig.AssemblyInformationalFormat;
        targetConfig.AssemblyVersioningFormat = overrideConfiguration.AssemblyVersioningFormat ?? targetConfig.AssemblyVersioningFormat;
        targetConfig.AssemblyFileVersioningFormat = overrideConfiguration.AssemblyFileVersioningFormat ?? targetConfig.AssemblyFileVersioningFormat;
        targetConfig.VersioningMode = overrideConfiguration.VersioningMode ?? targetConfig.VersioningMode;
        targetConfig.TagPrefix = overrideConfiguration.TagPrefix ?? targetConfig.TagPrefix;
        targetConfig.ContinuousDeploymentFallbackTag = overrideConfiguration.ContinuousDeploymentFallbackTag ?? targetConfig.ContinuousDeploymentFallbackTag;
        targetConfig.NextVersion = overrideConfiguration.NextVersion ?? targetConfig.NextVersion;
        targetConfig.MajorVersionBumpMessage = overrideConfiguration.MajorVersionBumpMessage ?? targetConfig.MajorVersionBumpMessage;
        targetConfig.MinorVersionBumpMessage = overrideConfiguration.MinorVersionBumpMessage ?? targetConfig.MinorVersionBumpMessage;
        targetConfig.PatchVersionBumpMessage = overrideConfiguration.PatchVersionBumpMessage ?? targetConfig.PatchVersionBumpMessage;
        targetConfig.NoBumpMessage = overrideConfiguration.NoBumpMessage ?? targetConfig.NoBumpMessage;
        targetConfig.TagPreReleaseWeight = overrideConfiguration.TagPreReleaseWeight ?? targetConfig.TagPreReleaseWeight;
        targetConfig.CommitMessageIncrementing = overrideConfiguration.CommitMessageIncrementing ?? targetConfig.CommitMessageIncrementing;
        targetConfig.Increment = overrideConfiguration.Increment ?? targetConfig.Increment;
        targetConfig.CommitDateFormat = overrideConfiguration.CommitDateFormat ?? targetConfig.CommitDateFormat;
        targetConfig.MergeMessageFormats = overrideConfiguration.MergeMessageFormats.Any() ? overrideConfiguration.MergeMessageFormats : targetConfig.MergeMessageFormats;
        targetConfig.UpdateBuildNumber = overrideConfiguration.UpdateBuildNumber ?? targetConfig.UpdateBuildNumber;
        targetConfig.SemanticVersionFormat = overrideConfiguration.SemanticVersionFormat;

        if (overrideConfiguration.Ignore is { IsEmpty: false })
        {
            targetConfig.Ignore = overrideConfiguration.Ignore;
        }

        ApplyBranchOverrides(targetConfig, overrideConfiguration);
    }

    private static void ApplyBranchOverrides(GitVersionConfiguration targetConfig, GitVersionConfiguration overrideConfiguration)
    {
        if (overrideConfiguration.Branches is { Count: > 0 })
        {
            // We can't just add new configs to the targetConfig.Branches, and have to create a new dictionary.
            // The reason is that GitVersion 5.3.x (and earlier) merges default configs into overrides. The new approach is opposite: we merge overrides into default configuration.
            // The important difference of these approaches is the order of branches in a dictionary (we should not rely on Dictionary's implementation details, but we already did that):
            // Old approach: { new-branch-1, new-branch-2, default-branch-1, default-branch-2, ... }
            // New approach: { default-branch-1, default-branch-2, ..., new-branch-1, new-branch-2 }
            // In case when several branch configurations match the current branch (by regex), we choose the first one.
            // So we have to add new branches to the beginning of a dictionary to preserve 5.3.x behavior.

            var newBranches = new Dictionary<string, BranchConfiguration>();

            var targetConfigBranches = targetConfig.Branches;

            foreach (var (name, branchConfiguration) in overrideConfiguration.Branches)
            {
                // for compatibility reason we check if it's master, we rename it to main
                var branchName = name == GitVersionConfiguration.MasterBranchKey ? GitVersionConfiguration.MainBranchKey : name;
                if (!targetConfigBranches.TryGetValue(branchName, out var target))
                {
                    target = new BranchConfiguration() { Name = branchName };
                }

                branchConfiguration.MergeTo(target);
                if (target.SourceBranches != null && target.SourceBranches.Contains(GitVersionConfiguration.MasterBranchKey))
                {
                    target.SourceBranches.Remove(GitVersionConfiguration.MasterBranchKey);
                    target.SourceBranches.Add(GitVersionConfiguration.MainBranchKey);
                }
                newBranches[branchName] = target;
            }

            foreach (var (name, branchConfiguration) in targetConfigBranches)
            {
                if (!newBranches.ContainsKey(name))
                {
                    newBranches[name] = branchConfiguration;
                }
            }

            targetConfig.Branches = newBranches;
        }
    }

    private static void FinalizeConfiguration(GitVersionConfiguration configuration)
    {
        foreach (var (name, branchConfiguration) in configuration.Branches)
        {
            FinalizeBranchConfiguration(configuration, name, branchConfiguration);
        }
    }

    private static void FinalizeBranchConfiguration(GitVersionConfiguration configuration, string name, BranchConfiguration branchConfiguration)
    {
        branchConfiguration.Name = name;
        branchConfiguration.Increment ??= configuration.Increment ?? IncrementStrategy.Inherit;

        if (branchConfiguration.VersioningMode == null)
        {
            if (name == GitVersionConfiguration.DevelopBranchKey)
            {
                // Why this applies only on develop branch? I'm surprised that the value not coming from configuration.
                branchConfiguration.VersioningMode = configuration.VersioningMode == VersioningMode.Mainline ? VersioningMode.Mainline : VersioningMode.ContinuousDeployment;
            }
            else
            {
                branchConfiguration.VersioningMode = configuration.VersioningMode;
            }
        }

        if (branchConfiguration.IsSourceBranchFor == null)
            return;

        foreach (var targetBranchName in branchConfiguration.IsSourceBranchFor)
        {
            var targetBranchConfig = configuration.Branches[targetBranchName];
            targetBranchConfig.SourceBranches ??= new HashSet<string>();
            targetBranchConfig.SourceBranches.Add(name);
        }
    }

    private static void ValidateConfiguration(GitVersionConfiguration configuration)
    {
        foreach (var (name, branchConfiguration) in configuration.Branches)
        {
            var regex = branchConfiguration?.Regex;
            var helpUrl = $"{System.Environment.NewLine}See https://gitversion.net/docs/reference/configuration for more info";

            if (regex == null)
            {
                throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'regex'{helpUrl}");
            }

            var sourceBranches = branchConfiguration?.SourceBranches;
            if (sourceBranches == null)
            {
                throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'source-branches'{helpUrl}");
            }

            var missingSourceBranches = sourceBranches.Where(sb => !configuration.Branches.ContainsKey(sb)).ToArray();
            if (missingSourceBranches.Any())
                throw new ConfigurationException($"Branch configuration '{name}' defines these 'source-branches' that are not configured: '[{string.Join(",", missingSourceBranches)}]'{helpUrl}");
        }
    }

    private static GitVersionConfiguration CreateDefaultConfiguration()
    {
        var configuration = new GitVersionConfiguration
        {
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            TagPrefix = GitVersionConfiguration.DefaultTagPrefix,
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

        AddBranchConfig(GitVersionConfiguration.DevelopBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Minor,
                Regex = GitVersionConfiguration.DevelopBranchRegex,
                SourceBranches = new HashSet<string>(),
                Tag = "alpha",
                PreventIncrementOfMergedBranchVersion = false,
                TrackMergeTarget = true,
                TracksReleaseBranches = true,
                IsMainline = false,
                IsReleaseBranch = false,
                PreReleaseWeight = 0
            });

        AddBranchConfig(GitVersionConfiguration.MainBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Patch,
                Regex = GitVersionConfiguration.MainBranchRegex,
                SourceBranches = new HashSet<string> {
                    GitVersionConfiguration.DevelopBranchKey,
                    GitVersionConfiguration.ReleaseBranchKey
                },
                Tag = string.Empty,
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = true,
                IsReleaseBranch = false,
                PreReleaseWeight = 55000
            });

        AddBranchConfig(GitVersionConfiguration.ReleaseBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.None,
                Regex = GitVersionConfiguration.ReleaseBranchRegex,
                SourceBranches = new HashSet<string> {
                    GitVersionConfiguration.DevelopBranchKey,
                    GitVersionConfiguration.MainBranchKey,
                    GitVersionConfiguration.SupportBranchKey,
                    GitVersionConfiguration.ReleaseBranchKey
                },
                Tag = "beta",
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = false,
                IsReleaseBranch = true,
                PreReleaseWeight = 30000
            });

        AddBranchConfig(GitVersionConfiguration.FeatureBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Inherit,
                Regex = GitVersionConfiguration.FeatureBranchRegex,
                SourceBranches = new HashSet<string> {
                    GitVersionConfiguration.DevelopBranchKey,
                    GitVersionConfiguration.MainBranchKey,
                    GitVersionConfiguration.ReleaseBranchKey,
                    GitVersionConfiguration.FeatureBranchKey,
                    GitVersionConfiguration.SupportBranchKey,
                    GitVersionConfiguration.HotfixBranchKey
                },
                Tag = "{BranchName}",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(GitVersionConfiguration.PullRequestBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Inherit,
                Regex = GitVersionConfiguration.PullRequestRegex,
                SourceBranches = new HashSet<string> {
                    GitVersionConfiguration.DevelopBranchKey,
                    GitVersionConfiguration.MainBranchKey,
                    GitVersionConfiguration.ReleaseBranchKey,
                    GitVersionConfiguration.FeatureBranchKey,
                    GitVersionConfiguration.SupportBranchKey,
                    GitVersionConfiguration.HotfixBranchKey
                },
                Tag = "PullRequest",
                TagNumberPattern = @"[/-](?<number>\d+)",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(GitVersionConfiguration.HotfixBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Inherit,
                Regex = GitVersionConfiguration.HotfixBranchRegex,
                SourceBranches = new HashSet<string> {
                    GitVersionConfiguration.ReleaseBranchKey,
                    GitVersionConfiguration.MainBranchKey,
                    GitVersionConfiguration.SupportBranchKey,
                    GitVersionConfiguration.HotfixBranchKey
                },
                Tag = "beta",
                PreReleaseWeight = 30000
            });

        AddBranchConfig(GitVersionConfiguration.SupportBranchKey,
            new BranchConfiguration
            {
                Increment = IncrementStrategy.Patch,
                Regex = GitVersionConfiguration.SupportBranchRegex,
                SourceBranches = new HashSet<string> { GitVersionConfiguration.MainBranchKey },
                Tag = string.Empty,
                PreventIncrementOfMergedBranchVersion = true,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsMainline = true,
                IsReleaseBranch = false,
                PreReleaseWeight = 55000
            });

        return configuration;

        void AddBranchConfig(string name, BranchConfiguration branchConfiguration)
        {
            branchConfiguration.Name = name;
            configuration.Branches[name] = branchConfiguration;
        }
    }
}
