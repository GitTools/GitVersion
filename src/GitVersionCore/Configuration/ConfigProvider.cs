using System;
using System.IO;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class ConfigProvider : IConfigProvider
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IOptions<GitVersionOptions> options;
        private readonly IConfigInitWizard configInitWizard;

        public ConfigProvider(IFileSystem fileSystem, ILog log, IConfigFileLocator configFileLocator, IOptions<GitVersionOptions> options, IConfigInitWizard configInitWizard)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.configFileLocator = configFileLocator ?? throw new ArgumentNullException(nameof(configFileLocator));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.configInitWizard = configInitWizard ?? throw new ArgumentNullException(nameof(this.configInitWizard));
        }

        public Config Provide(bool applyDefaults = true, Config overrideConfig = null)
        {
            var gitVersionOptions = options.Value;
            var workingDirectory = gitVersionOptions.WorkingDirectory;
            var projectRootDirectory = gitVersionOptions.ProjectRootDirectory;

            var rootDirectory = configFileLocator.HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory;
            return Provide(rootDirectory, applyDefaults, overrideConfig);
        }

        public Config Provide(string workingDirectory, bool applyDefaults = true, Config overrideConfig = null)
        {
            var readConfig = configFileLocator.ReadConfig(workingDirectory);
            readConfig.Verify();

            if (applyDefaults) readConfig.Reset();
            if (null != overrideConfig) readConfig.ApplyOverridesTo(overrideConfig);
            return readConfig;
        }

        public void Init(string workingDirectory)
        {
            var configFilePath = configFileLocator.GetConfigFilePath(workingDirectory);
            var currentConfiguration = Provide(workingDirectory, false);

            var config = configInitWizard.Run(currentConfiguration, workingDirectory);
            if (config == null) return;

            using var stream = fileSystem.OpenWrite(configFilePath);
            using var writer = new StreamWriter(stream);
            log.Info("Saving config file");
            ConfigSerializer.Write(config, writer);
            stream.Flush();
        }
    }
}
