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

        public const string DefaultConfigFileName = "GitVersion.yml";
        public const string ObsoleteConfigFileName = "GitVersionConfig.yaml";

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

        public static Config Provide(GitPreparer gitPreparer, IFileSystem fileSystem, bool applyDefaults = true, Config overrideConfig = null)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (HasConfigFileAt(workingDirectory, fileSystem))
            {
                return Provide(workingDirectory, fileSystem, applyDefaults, overrideConfig);
            }

            return Provide(projectRootDirectory, fileSystem, applyDefaults, overrideConfig);
        }

        public static string SelectConfigFilePath(GitPreparer gitPreparer, IFileSystem fileSystem)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (HasConfigFileAt(workingDirectory, fileSystem))
            {
                return GetConfigFilePath(workingDirectory, fileSystem);
            }

            return GetConfigFilePath(projectRootDirectory, fileSystem);
        }

        public static Config Provide(string workingDirectory, IFileSystem fileSystem, bool applyDefaults = true, Config overrideConfig = null)
        {
            var readConfig = ReadConfig(workingDirectory, fileSystem);
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
                defaultVersioningMode: config.VersioningMode == VersioningMode.Mainline? VersioningMode.Mainline : VersioningMode.ContinuousDeployment,
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
                var branchConfig = new BranchConfig {Name = branchKey};
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
            int defaultPreReleaseNumber;
            DefaultPreReleaseWeight.TryGetValue(branchRegex, out defaultPreReleaseNumber);
            branchConfig.PreReleaseWeight = branchConfig.PreReleaseWeight ?? defaultPreReleaseNumber;
        }

        static Config ReadConfig(string workingDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(workingDirectory, fileSystem);

            if (fileSystem.Exists(configFilePath))
            {
                var readAllText = fileSystem.ReadAllText(configFilePath);
                LegacyConfigNotifier.Notify(new StringReader(readAllText));
                return ConfigSerialiser.Read(new StringReader(readAllText));
            }

            return new Config();
        }

        public static string GetEffectiveConfigAsString(string workingDirectory, IFileSystem fileSystem)
        {
            var config = Provide(workingDirectory, fileSystem);
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerialiser.Write(config, stream);
                stream.Flush();
            }
            return stringBuilder.ToString();
        }

        public static void Verify(GitPreparer gitPreparer, IFileSystem fileSystem)
        {
            if (!string.IsNullOrWhiteSpace(gitPreparer.TargetUrl))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            Verify(workingDirectory, projectRootDirectory, fileSystem);
        }

        public static void Verify(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
        {
            if (fileSystem.PathsEqual(workingDirectory, projectRootDirectory))
            {
                WarnAboutObsoleteConfigFile(workingDirectory, fileSystem);
                return;
            }

            WarnAboutObsoleteConfigFile(workingDirectory, fileSystem);
            WarnAboutObsoleteConfigFile(projectRootDirectory, fileSystem);

            WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory, fileSystem);
        }

        static void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
        {
            var workingConfigFile = GetConfigFilePath(workingDirectory, fileSystem);
            var projectRootConfigFile = GetConfigFilePath(projectRootDirectory, fileSystem);

            bool hasConfigInWorkingDirectory = fileSystem.Exists(workingConfigFile);
            bool hasConfigInProjectRootDirectory = fileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException(string.Format("Ambiguous config file selection from '{0}' and '{1}'", workingConfigFile, projectRootConfigFile));
            }
        }

        static string GetConfigFilePath(string workingDirectory, IFileSystem fileSystem)
        {
            var ymlPath = Path.Combine(workingDirectory, DefaultConfigFileName);
            if (fileSystem.Exists(ymlPath))
            {
                return ymlPath;
            }

            var deprecatedPath = Path.Combine(workingDirectory, ObsoleteConfigFileName);
            if (fileSystem.Exists(deprecatedPath))
            {
                return deprecatedPath;
            }

            return ymlPath;
        }

        static bool HasConfigFileAt(string workingDirectory, IFileSystem fileSystem)
        {
            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultConfigFileName);
            if (fileSystem.Exists(defaultConfigFilePath))
            {
                return true;
            }

            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteConfigFileName);
            if (fileSystem.Exists(deprecatedConfigFilePath))
            {
                return true;
            }

            return false;
        }

        static void WarnAboutObsoleteConfigFile(string workingDirectory, IFileSystem fileSystem)
        {
            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteConfigFileName);
            if (!fileSystem.Exists(deprecatedConfigFilePath))
            {
                return;
            }

            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultConfigFileName);
            if (fileSystem.Exists(defaultConfigFilePath))
            {
                Logger.WriteWarning(string.Format("Ambiguous config files at '{0}': '{1}' (deprecated) and '{2}'. Will be used '{2}'", workingDirectory, ObsoleteConfigFileName, DefaultConfigFileName));
                return;
            }

            Logger.WriteWarning(string.Format("'{0}' is deprecated, use '{1}' instead.", deprecatedConfigFilePath, DefaultConfigFileName));
        }

        public static void Init(string workingDirectory, IFileSystem fileSystem, IConsole console)
        {
            var configFilePath = GetConfigFilePath(workingDirectory, fileSystem);
            var currentConfiguration = Provide(workingDirectory, fileSystem, applyDefaults: false);
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
