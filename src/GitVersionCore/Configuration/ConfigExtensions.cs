using System.Collections.Generic;
using System.Linq;
using GitVersion.Extensions;
using GitVersion.VersioningModes;
using System;
using System.Text.RegularExpressions;

namespace GitVersion.Configuration
{
    public static class ConfigExtensions
    {
        public static void Reset(this Config config)
        {
            config.AssemblyVersioningScheme ??= AssemblyVersioningScheme.MajorMinorPatch;
            config.AssemblyFileVersioningScheme ??= AssemblyFileVersioningScheme.MajorMinorPatch;
            config.AssemblyInformationalFormat = config.AssemblyInformationalFormat;
            config.AssemblyVersioningFormat = config.AssemblyVersioningFormat;
            config.AssemblyFileVersioningFormat = config.AssemblyFileVersioningFormat;
            config.TagPrefix ??= Config.DefaultTagPrefix;
            config.VersioningMode ??= VersioningMode.ContinuousDelivery;
            config.ContinuousDeploymentFallbackTag ??= "ci";
            config.MajorVersionBumpMessage ??= IncrementStrategyFinder.DefaultMajorPattern;
            config.MinorVersionBumpMessage ??= IncrementStrategyFinder.DefaultMinorPattern;
            config.PatchVersionBumpMessage ??= IncrementStrategyFinder.DefaultPatchPattern;
            config.NoBumpMessage ??= IncrementStrategyFinder.DefaultNoBumpPattern;
            config.CommitMessageIncrementing ??= CommitMessageIncrementMode.Enabled;
            config.LegacySemVerPadding ??= 4;
            config.BuildMetaDataPadding ??= 4;
            config.CommitsSinceVersionSourcePadding ??= 4;
            config.CommitDateFormat ??= "yyyy-MM-dd";

            var configBranches = config.Branches.ToList();

            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.DevelopBranchKey), Config.DevelopBranchRegex,
                new List<string>(),
                defaultTag: "alpha",
                defaultIncrementStrategy: IncrementStrategy.Minor,
                defaultVersioningMode: config.VersioningMode == VersioningMode.Mainline ? VersioningMode.Mainline : VersioningMode.ContinuousDeployment,
                defaultTrackMergeTarget: true,
                tracksReleaseBranches: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.MasterBranchKey), Config.MasterBranchRegex,
                new List<string>
                    { "develop", "release" },
                defaultTag: string.Empty,
                defaultPreventIncrement: true,
                defaultIncrementStrategy: IncrementStrategy.Patch,
                isMainline: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.ReleaseBranchKey), Config.ReleaseBranchRegex,
                new List<string>
                    { "develop", "master", "support", "release" },
                defaultTag: "beta",
                defaultPreventIncrement: true,
                defaultIncrementStrategy: IncrementStrategy.None,
                isReleaseBranch: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.FeatureBranchKey), Config.FeatureBranchRegex,
                new List<string>
                    { "develop", "master", "release", "feature", "support", "hotfix" },
                defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.PullRequestBranchKey), Config.PullRequestRegex,
                new List<string>
                    { "develop", "master", "release", "feature", "support", "hotfix" },
                defaultTag: "PullRequest",
                defaultTagNumberPattern: @"[/-](?<number>\d+)",
                defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.HotfixBranchKey), Config.HotfixBranchRegex,
                new List<string>
                    { "develop", "master", "support" },
                defaultTag: "beta",
                defaultIncrementStrategy: IncrementStrategy.Patch);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, Config.SupportBranchKey), Config.SupportBranchRegex,
                new List<string>
                    { "master" },
                defaultTag: string.Empty,
                defaultPreventIncrement: true,
                defaultIncrementStrategy: IncrementStrategy.Patch,
                isMainline: true);

            // Any user defined branches should have other values defaulted after known branches filled in.
            // This allows users to override any of the value.
            foreach (var branchConfig in configBranches)
            {
                var regex = branchConfig.Value.Regex;
                if (regex == null)
                {
                    throw new GitVersionConfigurationException($"Branch configuration '{branchConfig.Key}' is missing required configuration 'regex'\n\n" +
                                                               "See http://gitversion.readthedocs.io/en/latest/configuration/ for more info");
                }

                var sourceBranches = branchConfig.Value.SourceBranches;
                if (sourceBranches == null)
                {
                    throw new GitVersionConfigurationException($"Branch configuration '{branchConfig.Key}' is missing required configuration 'source-branches'\n\n" +
                                                               "See http://gitversion.readthedocs.io/en/latest/configuration/ for more info");
                }

                ApplyBranchDefaults(config, branchConfig.Value, regex, sourceBranches);
            }

            // This is a second pass to add additional sources, it has to be another pass to prevent ordering issues
            foreach (var branchConfig in configBranches)
            {
                if (branchConfig.Value.IsSourceBranchFor == null) continue;
                foreach (var isSourceBranch in branchConfig.Value.IsSourceBranchFor)
                {
                    config.Branches[isSourceBranch].SourceBranches.Add(branchConfig.Key);
                }
            }
        }

        private static readonly Dictionary<string, int> DefaultPreReleaseWeight =
            new Dictionary<string, int>
            {
                { Config.DevelopBranchRegex, 0 },
                { Config.HotfixBranchRegex, 30000 },
                { Config.ReleaseBranchRegex, 30000 },
                { Config.FeatureBranchRegex, 30000 },
                { Config.PullRequestRegex, 30000 },
                { Config.SupportBranchRegex, 55000 },
                { Config.MasterBranchRegex, 55000 }
            };
        private const IncrementStrategy DefaultIncrementStrategy = IncrementStrategy.Inherit;

        public static void ApplyBranchDefaults(this Config config,
            BranchConfig branchConfig,
            string branchRegex,
            List<string> sourceBranches,
            string defaultTag = "useBranchName",
            IncrementStrategy? defaultIncrementStrategy = null, // Looked up from main config
            bool defaultPreventIncrement = false,
            VersioningMode? defaultVersioningMode = null, // Looked up from main config
            bool defaultTrackMergeTarget = false,
            string defaultTagNumberPattern = null,
            bool tracksReleaseBranches = false,
            bool isReleaseBranch = false,
            bool isMainline = false)
        {
            branchConfig.Regex = string.IsNullOrEmpty(branchConfig.Regex) ? branchRegex : branchConfig.Regex;
            branchConfig.SourceBranches = branchConfig.SourceBranches == null || !branchConfig.SourceBranches.Any()
                ? sourceBranches : branchConfig.SourceBranches;
            branchConfig.Tag ??= defaultTag;
            branchConfig.TagNumberPattern ??= defaultTagNumberPattern;
            branchConfig.Increment ??= defaultIncrementStrategy ?? config.Increment ?? DefaultIncrementStrategy;
            branchConfig.PreventIncrementOfMergedBranchVersion ??= defaultPreventIncrement;
            branchConfig.TrackMergeTarget ??= defaultTrackMergeTarget;
            branchConfig.VersioningMode ??= defaultVersioningMode ?? config.VersioningMode;
            branchConfig.TracksReleaseBranches ??= tracksReleaseBranches;
            branchConfig.IsReleaseBranch ??= isReleaseBranch;
            branchConfig.IsMainline ??= isMainline;
            DefaultPreReleaseWeight.TryGetValue(branchRegex, out var defaultPreReleaseNumber);
            branchConfig.PreReleaseWeight ??= defaultPreReleaseNumber;
        }

        public static void Verify(this Config readConfig)
        {
            // Verify no branches are set to mainline mode
            if (readConfig.Branches.Any(b => b.Value.VersioningMode == VersioningMode.Mainline))
            {
                throw new GitVersionConfigurationException(@"Mainline mode only works at the repository level, a single branch cannot be put into mainline mode

This is because mainline mode treats your entire git repository as an event source with each merge into the 'mainline' incrementing the version.

If the docs do not help you decide on the mode open an issue to discuss what you are trying to do.");
            }
        }

        public static void ApplyOverridesTo(this Config config, Config overrideConfig)
        {
            config.TagPrefix = string.IsNullOrWhiteSpace(overrideConfig.TagPrefix) ? config.TagPrefix : overrideConfig.TagPrefix;
        }

        public static BranchConfig GetConfigForBranch(this Config config, string branchName)
        {
            if (branchName == null) throw new ArgumentNullException(nameof(branchName));
            var matches = config.Branches
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
                var matchingConfigs = String.Concat(matches.Select(m => $"\n - {m.Key}"));
                var picked = matches
                    .Select(kvp => kvp.Value)
                    .First();

                // TODO check how to log this
                Console.WriteLine(
                    $"Multiple branch configurations match the current branch branchName of '{branchName}'. " +
                    $"Using the first matching configuration, '{picked.Name}'. Matching configurations include:'{matchingConfigs}'");

                return picked;
            }
        }

        public static bool IsReleaseBranch(this Config config, string branchName) => config.GetConfigForBranch(branchName)?.IsReleaseBranch ?? false;

        private static BranchConfig GetOrCreateBranchDefaults(this Config config, string branchKey)
        {
            if (!config.Branches.ContainsKey(branchKey))
            {
                var branchConfig = new BranchConfig { Name = branchKey };
                config.Branches.Add(branchKey, branchConfig);
                return branchConfig;
            }

            return config.Branches[branchKey];
        }
    }
}
