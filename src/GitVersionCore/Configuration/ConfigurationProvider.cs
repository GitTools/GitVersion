using System.IO;
using System.Text;
using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly IConfigFileLocator configFileLocator;
        private readonly IGitPreparer gitPreparer;

        public ConfigurationProvider(IFileSystem fileSystem, ILog log, IConfigFileLocator configFileLocator, IGitPreparer gitPreparer)
        {
            this.fileSystem = fileSystem;
            this.log = log;
            this.configFileLocator = configFileLocator;
            this.gitPreparer = gitPreparer;
        }


        public Config Provide(bool applyDefaults = true, Config overrideConfig = null)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (configFileLocator.HasConfigFileAt(workingDirectory))
            {
                return Provide(workingDirectory, applyDefaults, overrideConfig);
            }

            return Provide(projectRootDirectory, applyDefaults, overrideConfig);
        }

        public Config Provide(string workingDirectory, bool applyDefaults = true, Config overrideConfig = null)
        {
            var readConfig = configFileLocator.ReadConfig(workingDirectory);
            ConfigurationUtils.VerifyConfiguration(readConfig);

            if (applyDefaults) ConfigurationUtils.ApplyDefaultsTo(readConfig);
            if (null != overrideConfig) ConfigurationUtils.ApplyOverridesTo(readConfig, overrideConfig);
            return readConfig;
        }

        public string GetEffectiveConfigAsString(string workingDirectory)
        {
            var config = Provide(workingDirectory);
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerialiser.Write(config, stream);
                stream.Flush();
            }
            return stringBuilder.ToString();
        }

        public void Init(string workingDirectory, IConsole console)
        {
            var configFilePath = configFileLocator.GetConfigFilePath(workingDirectory);
            var currentConfiguration = Provide(workingDirectory, false);
            var config = new ConfigInitWizard(console, fileSystem, log).Run(currentConfiguration, workingDirectory);
            if (config == null) return;

            using var stream = fileSystem.OpenWrite(configFilePath);
            using var writer = new StreamWriter(stream);
            log.Info("Saving config file");
            ConfigSerialiser.Write(config, writer);
            stream.Flush();
        }
    }
}
