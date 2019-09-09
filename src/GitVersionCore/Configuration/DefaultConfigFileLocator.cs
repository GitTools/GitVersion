using System.IO;
using GitVersion.Exceptions;
using GitVersion.Helpers;

namespace GitVersion.Configuration
{
    public class DefaultConfigFileLocator : ConfigFileLocator
    {

        public const string DefaultFileName = "GitVersion.yml";

        public const string ObsoleteFileName = "GitVersionConfig.yaml";

        public override bool HasConfigFileAt(string workingDirectory, IFileSystem fileSystem)
        {
            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultFileName);
            if (fileSystem.Exists(defaultConfigFilePath))
            {
                return true;
            }

            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (fileSystem.Exists(deprecatedConfigFilePath))
            {
                return true;
            }

            return false;
        }

        public override string GetConfigFilePath(string workingDirectory, IFileSystem fileSystem)
        {
            var ymlPath = Path.Combine(workingDirectory, DefaultFileName);
            if (fileSystem.Exists(ymlPath))
            {
                return ymlPath;
            }

            var deprecatedPath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (fileSystem.Exists(deprecatedPath))
            {
                return deprecatedPath;
            }

            return ymlPath;
        }

        public override void Verify(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
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

        private void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
        {
            var workingConfigFile = GetConfigFilePath(workingDirectory, fileSystem);
            var projectRootConfigFile = GetConfigFilePath(projectRootDirectory, fileSystem);

            bool hasConfigInWorkingDirectory = fileSystem.Exists(workingConfigFile);
            bool hasConfigInProjectRootDirectory = fileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException($"Ambiguous config file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
            }
        }

        private void WarnAboutObsoleteConfigFile(string workingDirectory, IFileSystem fileSystem)
        {
            var deprecatedConfigFilePath = Path.Combine(workingDirectory, ObsoleteFileName);
            if (!fileSystem.Exists(deprecatedConfigFilePath))
            {
                return;
            }

            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultFileName);
            if (fileSystem.Exists(defaultConfigFilePath))
            {
                Logger.WriteWarning(string.Format("Ambiguous config files at '{0}': '{1}' (deprecated) and '{2}'. Will be used '{2}'", workingDirectory, ObsoleteFileName, DefaultFileName));
                return;
            }

            Logger.WriteWarning($"'{deprecatedConfigFilePath}' is deprecated, use '{DefaultFileName}' instead.");
        }

    }

}
