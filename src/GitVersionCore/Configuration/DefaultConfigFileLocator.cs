using System.IO;
using GitVersion.Exceptions;
using GitVersion.Logging;

namespace GitVersion.Configuration
{
    public class DefaultConfigFileLocator : ConfigFileLocator
    {
        public DefaultConfigFileLocator(IFileSystem fileSystem, ILog log) : base(fileSystem, log)
        {
            
        }

        public const string DefaultFileName = "GitVersion.yml";

        public const string ObsoleteFileName = "GitVersionConfig.yaml";

        public override bool HasConfigFileAt(string workingDirectory)
        {
            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultFileName);
            if (FileSystem.Exists(defaultConfigFilePath))
            {
                return true;
            }

            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (FileSystem.Exists(deprecatedConfigFilePath))
            {
                return true;
            }

            return false;
        }

        public override string GetConfigFilePath(string workingDirectory)
        {
            var ymlPath = Path.Combine(workingDirectory, DefaultFileName);
            if (FileSystem.Exists(ymlPath))
            {
                return ymlPath;
            }

            var deprecatedPath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (FileSystem.Exists(deprecatedPath))
            {
                return deprecatedPath;
            }

            return ymlPath;
        }

        public override void Verify(string workingDirectory, string projectRootDirectory)
        {
            if (FileSystem.PathsEqual(workingDirectory, projectRootDirectory))
            {
                WarnAboutObsoleteConfigFile(workingDirectory);
                return;
            }

            WarnAboutObsoleteConfigFile(workingDirectory);
            WarnAboutObsoleteConfigFile(projectRootDirectory);

            WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
        }

        private void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory)
        {
            var workingConfigFile = GetConfigFilePath(workingDirectory);
            var projectRootConfigFile = GetConfigFilePath(projectRootDirectory);

            var hasConfigInWorkingDirectory = FileSystem.Exists(workingConfigFile);
            var hasConfigInProjectRootDirectory = FileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException($"Ambiguous config file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
            }
        }

        private void WarnAboutObsoleteConfigFile(string workingDirectory)
        {
            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (!FileSystem.Exists(deprecatedConfigFilePath))
            {
                return;
            }

            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultFileName);
            if (FileSystem.Exists(defaultConfigFilePath))
            {
                Log.Warning(string.Format("Ambiguous config files at '{0}': '{1}' (deprecated) and '{2}'. Will be used '{2}'", workingDirectory, ObsoleteFileName, DefaultFileName));
                return;
            }

            Log.Warning($"'{deprecatedConfigFilePath}' is deprecated, use '{DefaultFileName}' instead.");
        }

    }

}
