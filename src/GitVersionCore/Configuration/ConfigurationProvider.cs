namespace GitVersion
{
    using GitVersion.Configuration.Init.Wizard;
    using GitVersion.Helpers;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ConfigurationProvider
    {
        internal const string DefaultTagPrefix = "[vV]";

        public const string DefaultConfigFileName = "GitVersion.yml";
        public const string ObsoleteConfigFileName = "GitVersionConfig.yaml";

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
            if (applyDefaults)
                ApplyDefaultsTo(readConfig);
            if (null != overrideConfig)
                ApplyOverridesTo(readConfig, overrideConfig);
            return readConfig;
        }

        public static void ApplyDefaultsTo(Config config)
        {
            MigrateBranches(config);

            config.AssemblyVersioningScheme = config.AssemblyVersioningScheme ?? AssemblyVersioningScheme.MajorMinorPatch;
            config.AssemblyInformationalFormat = config.AssemblyInformationalFormat;
            config.TagPrefix = config.TagPrefix ?? DefaultTagPrefix;
            config.VersioningMode = config.VersioningMode ?? VersioningMode.ContinuousDelivery;
            config.ContinuousDeploymentFallbackTag = config.ContinuousDeploymentFallbackTag ?? "ci";
            config.MajorVersionBumpMessage = config.MajorVersionBumpMessage ?? IncrementStrategyFinder.DefaultMajorPattern;
            config.MinorVersionBumpMessage = config.MinorVersionBumpMessage ?? IncrementStrategyFinder.DefaultMinorPattern;
            config.PatchVersionBumpMessage = config.PatchVersionBumpMessage ?? IncrementStrategyFinder.DefaultPatchPattern;
            config.CommitMessageIncrementing = config.CommitMessageIncrementing ?? CommitMessageIncrementMode.Enabled;
            config.LegacySemVerPadding = config.LegacySemVerPadding ?? 4;
            config.BuildMetaDataPadding = config.BuildMetaDataPadding ?? 4;
            config.CommitsSinceVersionSourcePadding = config.CommitsSinceVersionSourcePadding ?? 4;

            var configBranches = config.Branches.ToList();

            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "master"), defaultTag: string.Empty, defaultPreventIncrement: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "releases?[/-]"), defaultTag: "beta", defaultPreventIncrement: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "features?[/-]"), defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, @"(pull|pull\-requests|pr)[/-]"),
                defaultTag: "PullRequest",
                defaultTagNumberPattern: @"[/-](?<number>\d+)[-/]",
                defaultIncrementStrategy: IncrementStrategy.Inherit);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "hotfix(es)?[/-]"), defaultTag: "beta");
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "support[/-]"), defaultTag: string.Empty, defaultPreventIncrement: true);
            ApplyBranchDefaults(config, GetOrCreateBranchDefaults(config, "dev(elop)?(ment)?$"),
                defaultTag: "unstable",
                defaultIncrementStrategy: IncrementStrategy.Minor,
                defaultVersioningMode: VersioningMode.ContinuousDeployment,
                defaultTrackMergeTarget: true);

            // Any user defined branches should have other values defaulted after known branches filled in
            // This allows users to override one value of 
            foreach (var branchConfig in configBranches)
            {
                ApplyBranchDefaults(config, branchConfig.Value);
            }
        }

        public static void ApplyOverridesTo(Config config, Config overrideConfig)
        {
            config.TagPrefix = string.IsNullOrWhiteSpace(overrideConfig.TagPrefix) ? config.TagPrefix : overrideConfig.TagPrefix;
        }

        static void MigrateBranches(Config config)
        {
            // Map of current names and previous names
            var dict = new Dictionary<string, string[]>
            {
                {"hotfix(es)?[/-]", new[]
                    {
                        "hotfix[/-]"
                    }
                },
                {"features?[/-]", new[]
                    {
                        "feature[/-]",
                        "feature(s)?[/-]"
                    }
                },
                {"releases?[/-]", new[]
                    {
                        "release[/-]"
                    }
                },
                {"dev(elop)?(ment)?$", new[]
                    {
                        "develop"
                    }
                }
            };

            foreach (var mapping in dict)
            {
                foreach (var source in mapping.Value)
                {
                    if (config.Branches.ContainsKey(source))
                    {
                        // found one, rename
                        var bc = config.Branches[source];
                        config.Branches.Remove(source);
                        config.Branches[mapping.Key] = bc; // re-add with new name
                    }
                }
            }
        }


        static BranchConfig GetOrCreateBranchDefaults(Config config, string branch)
        {
            if (!config.Branches.ContainsKey(branch))
            {
                var branchConfig = new BranchConfig();
                config.Branches.Add(branch, branchConfig);
                return branchConfig;
            }

            return config.Branches[branch];
        }

        public static void ApplyBranchDefaults(Config config,
            BranchConfig branchConfig,
            string defaultTag = "useBranchName",
            IncrementStrategy defaultIncrementStrategy = IncrementStrategy.Patch,
            bool defaultPreventIncrement = false,
            VersioningMode? defaultVersioningMode = null, // Looked up from main config
            bool defaultTrackMergeTarget = false,
            string defaultTagNumberPattern = null)
        {
            branchConfig.Tag = branchConfig.Tag ?? defaultTag;
            branchConfig.TagNumberPattern = branchConfig.TagNumberPattern ?? defaultTagNumberPattern;
            branchConfig.Increment = branchConfig.Increment ?? defaultIncrementStrategy;
            branchConfig.PreventIncrementOfMergedBranchVersion = branchConfig.PreventIncrementOfMergedBranchVersion ?? defaultPreventIncrement;
            branchConfig.TrackMergeTarget = branchConfig.TrackMergeTarget ?? defaultTrackMergeTarget;
            branchConfig.VersioningMode = branchConfig.VersioningMode ?? defaultVersioningMode ?? config.VersioningMode;
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

        private static void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
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

        public static string GetConfigFilePath(string workingDirectory, IFileSystem fileSystem)
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

        public static bool HasConfigFileAt(string workingDirectory, IFileSystem fileSystem)
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

        static bool WarnAboutObsoleteConfigFile(string workingDirectory, IFileSystem fileSystem)
        {
            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteConfigFileName);
            if (!fileSystem.Exists(deprecatedConfigFilePath))
            {
                return false;
            }

            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultConfigFileName);
            if (fileSystem.Exists(defaultConfigFilePath))
            {
                Logger.WriteWarning(string.Format("Ambiguous config files at '{0}': '{1}' (deprecated) and '{2}'. Will be used '{2}'", workingDirectory, ObsoleteConfigFileName, DefaultConfigFileName));
                return true;
            }

            Logger.WriteWarning(string.Format("'{0}' is deprecated, use '{1}' instead.", deprecatedConfigFilePath, DefaultConfigFileName));
            return true;
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
