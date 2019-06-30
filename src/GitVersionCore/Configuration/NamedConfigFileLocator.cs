namespace GitVersion
{

    using System;

    using GitVersion.Helpers;
    using System.IO;

    public class NamedConfigFileLocator : ConfigFileLocator
    {

        public NamedConfigFileLocator(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath), "Empty file path provided!");
            FilePath = filePath;
        }

        public string FilePath { get; }

        public override bool HasConfigFileAt(string workingDirectory, IFileSystem fileSystem) =>
            fileSystem.Exists(Path.Combine(workingDirectory, FilePath));

        public override string GetConfigFilePath(string workingDirectory, IFileSystem fileSystem) =>
            Path.Combine(workingDirectory, FilePath);

        public override void Verify(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
        {
            if (!Path.IsPathRooted(FilePath))
            {
                WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory, fileSystem);
            }
        }

        private void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem)
        {
            var workingConfigFile = GetConfigFilePath(workingDirectory, fileSystem);
            var projectRootConfigFile = GetConfigFilePath(projectRootDirectory, fileSystem);

            var hasConfigInWorkingDirectory = fileSystem.Exists(workingConfigFile);
            var hasConfigInProjectRootDirectory = fileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException(string.Format("Ambiguous config file selection from '{0}' and '{1}'", workingConfigFile, projectRootConfigFile));
            }
        }
    }
}
