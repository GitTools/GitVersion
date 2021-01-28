using System.IO;

namespace GitVersion.Configuration
{
    public class DefaultConfigFileLocator : ConfigFileLocator
    {
        public DefaultConfigFileLocator(IFileSystem fileSystem) : base(fileSystem)
        {
        }

        public const string DefaultFileName = "GitVersion.yml";

        public override bool HasConfigFileAt(string workingDirectory)
        {
            var defaultConfigFilePath = Path.Combine(workingDirectory, DefaultFileName);
            if (FileSystem.Exists(defaultConfigFilePath))
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

            return ymlPath;
        }

        public override void Verify(string workingDirectory, string projectRootDirectory)
        {
            if (FileSystem.PathsEqual(workingDirectory, projectRootDirectory))
            {
                return;
            }

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
    }
}
