namespace GitVersion
{
    using Configuration.Init.Wizard;
    using GitVersion.Helpers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ConfigurationProvider
    {
        internal const string DefaultTagPrefix = "[vV]";

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
        public static Dictionary<string, int> DefaultPreReleaseWeight =
            new Dictionary<string, int>
            {
                { DevelopBranchRegex, 0 },
                { HotfixBranchRegex, 30000 },
                { ReleaseBranchRegex, 30000 },
                { FeatureBranchRegex, 30000 },
                { PullRequestRegex, 30000 },
                { SupportBranchRegex, 55000 },
                { MasterBranchRegex, 55000 }
            };

        private const IncrementStrategy DefaultIncrementStrategy = IncrementStrategy.Inherit;

        public static Config Provide(GitPreparer gitPreparer, IFileSystem fileSystem, ConfigFileLocator configFileLocator, bool applyDefaults = true, Config overrideConfig = null, string configFilePath = null)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (configFileLocator.HasConfigFileAt(workingDirectory, fileSystem))
            {
                return Provide(workingDirectory, fileSystem, configFileLocator, applyDefaults, overrideConfig);
            }

            return Provide(projectRootDirectory, fileSystem, configFileLocator, applyDefaults, overrideConfig);
        }

        public static Config Provide(string workingDirectory, IFileSystem fileSystem, ConfigFileLocator configFileLocator, bool applyDefaults = true, Config overrideConfig = null)
        {
            var readConfig = configFileLocator.ReadConfig(workingDirectory, fileSystem);
            VerifyConfiguration(readConfig);

            if (applyDefaults)
                ApplyDefaultsTo(readConfig);
            if (null != overrideConfig)
                ApplyOverridesTo(readConfig, overrideConfig);
            return readConfig;
        }

        static void VerifyConfiguration(Config readConfig)
        {
            // Verify no branches are set to mainline mode
            if (readConfig.Branches.Any(b => b.Value.VersioningMode == VersioningMode.Mainline))
            {
                throw new GitVersionConfigurationException(@"Mainline mode only works at the repository level, a single branch cannot be put into mainline mode

This is because mainline mode treats your entire git repository as an event source with each merge into the 'mainline' incrementing the version.

If the docs do not help you decide on the mode open an issue to discuss what you are trying to do.");
            }
        }

        public static void ApplyDefaultsTo(Config config)
        {
            config.AssemblyVersioningScheme = config.AssemblyVersioningScheme ?? AssemblyVersioningScheme.MajorMinorPatch;
            config.AssemblyFileVersioningScheme = config.AssemblyFileVersioningScheme ?? AssemblyFileVersioningScheme.MajorMinorPatch;
            config.AssemblyInformationalFormat = config.AssemblyInformationalFormat;
            config.AssemblyVersioningFormat = config.AssemblyVersioningFormat;
            config.AssemblyFileVersioningFormat = config.AssemblyFileVersioningFormat;
            config.TagPrefix = config.TagPrefix ?? DefaultTagPrefix;
            config.VersioningMode = config.VersioningMode ?? VersioningMode.ContinuousDelivery;
            config.ContinuousDeploymentFallbackTag = config.ContinuousDeploymentFallbackTag ?? "ci";
            config.MajorVersionBumpMessage = config.MajorVersionBumpMessage ?? IncrementStrategyFinder.DefaultMajorPattern;
            config.MinorVersionBumpMessage = config.MinorVersionBumpMessage ?? IncrementStrategyFinder.DefaultMinorPattern;
            config.PatchVersionBumpMessage = config.PatchVersionBumpMessage ?? IncrementStrategyFinder.DefaultPatchPattern;
            config.NoBumpMessage = config.NoBumpMessage ?? IncrementStrategyFinder.DefaultNoBumpPattern;
            config.CommitMessageIncrementing = config.CommitMessageIncrementing ?? CommitMessageIncrementMode.Enabled;
            config.LegacySemVerPadding = config.LegacySemVerPadding ?? 4;
            config.BuildMetaDataPadding = config.BuildMetaDataPadding ?? 4;
            config.CommitsSinceVersionSourcePadding = config.CommitsSinceVersionSourcePadding ?? 4;
            config.CommitDateFormat = config.CommitDateFormat ?? "yyyy-MM-dd";

            var configBranches = config.Branches.ToList();

            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, DevelopBranchKey),
                DevelopBranchRegex,
                new List<string>(),
                defaultTag: "alpha",
                defaultIncrementStrategy: IncrementStrategy.Minor,
                defaultVersioningMode: config.VersioningMode == VersioningMode.Mainline ? VersioningMode.Mainline : VersioningMode.ContinuousDeployment,
                defaultTrackMergeTarget: true,
                tracksReleaseBranches: true);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, MasterBranchKey),
                MasterBranchRegex,
                new List<string> { "develop", "release" },
                defaultTag: string.Empty,
                defaultPreventIncrement: true,
                defaultIncrementStrategy: IncrementStrategy.Patch,
                isMainline: true);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, ReleaseBranchKey),
                ReleaseBranchRegex,
                new List<string> { "develop", "master", "support", "release" },
                defaultTag: "beta",
                defaultPreventIncrement: true,
                defaultIncrementStrategy: IncrementStrategy.Patch,
                isReleaseBranch: true);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, FeatureBranchKey),
                FeatureBranchRegex,
                new List<string> { "develop", "master", "release", "feature", "support", "hotfix" },
                defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, PullRequestBranchKey),
                PullRequestRegex,
                new List<string> { "develop", "master", "release", "feature", "support", "hotfix" },
                defaultTag: "PullRequest",
                defaultTagNumberPattern: @"[/-](?<number>\d+)",
                defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, HotfixBranchKey),
                HotfixBranchRegex,
                new List<string> { "develop", "master", "support" },
                defaultTag: "beta",
                defaultIncrementStrategy: IncrementStrategy.Patch);
            ApplyBranchDefaults(config,
                GetOrCreateBranchDefaults(config, SupportBranchKey),
                SupportBranchRegex,
                new List<string> { "master" },
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

        static void ApplyOverridesTo(Config config, Config overrideConfig)
        {
            config.TagPrefix = string.IsNullOrWhiteSpace(overrideConfig.TagPrefix) ? config.TagPrefix : overrideConfig.TagPrefix;
        }

        static BranchConfig GetOrCreateBranchDefaults(Config config, string branchKey)
        {
            if (!config.Branches.ContainsKey(branchKey))
            {
                var branchConfig = new BranchConfig { Name = branchKey };
                config.Branches.Add(branchKey, branchConfig);
                return branchConfig;
            }

            return config.Branches[branchKey];
        }

        public static void ApplyBranchDefaults(Config config,
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
            branchConfig.Tag = branchConfig.Tag ?? defaultTag;
            branchConfig.TagNumberPattern = branchConfig.TagNumberPattern ?? defaultTagNumberPattern;
            branchConfig.Increment = branchConfig.Increment ?? defaultIncrementStrategy ?? config.Increment ?? DefaultIncrementStrategy;
            branchConfig.PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion ?? defaultPreventIncrement;
            branchConfig.TrackMergeTarget = branchConfig.TrackMergeTarget ?? defaultTrackMergeTarget;
            branchConfig.VersioningMode = branchConfig.VersioningMode ?? defaultVersioningMode ?? config.VersioningMode;
            branchConfig.TracksReleaseBranches = branchConfig.TracksReleaseBranches ?? tracksReleaseBranches;
            branchConfig.IsReleaseBranch = branchConfig.IsReleaseBranch ?? isReleaseBranch;
            branchConfig.IsMainline = branchConfig.IsMainline ?? isMainline;
            DefaultPreReleaseWeight.TryGetValue(branchRegex, out var defaultPreReleaseNumber);
            branchConfig.PreReleaseWeight = branchConfig.PreReleaseWeight ?? defaultPreReleaseNumber;
        }

        public static string GetEffectiveConfigAsString(string workingDirectory, IFileSystem fileSystem, ConfigFileLocator configFileLocator)
        {
            var config = Provide(workingDirectory, fileSystem, configFileLocator);
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerialiser.Write(config, stream);
                stream.Flush();
            }
            return stringBuilder.ToString();
        }

        public static void Init(string workingDirectory, IFileSystem fileSystem, IConsole console, ConfigFileLocator configFileLocator)
        {
            var configFilePath = configFileLocator.GetConfigFilePath(workingDirectory, fileSystem);
            var currentConfiguration = Provide(workingDirectory, fileSystem, applyDefaults: false, configFileLocator: configFileLocator);
            var config = new ConfigInitWizard(console, fileSystem).Run(currentConfiguration, workingDirectory);
            if (config == null) return;

            using (var stream = fileSystem.OpenWrite(configFilePath))
            using (var writer = new StreamWriter(stream))
            {
                Logger.WriteInfo("Saving config file");
                ConfigSerialiser.Write(config, writer);
                stream.Flush();
            }
        }
    }
}
