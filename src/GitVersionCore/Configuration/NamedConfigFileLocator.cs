using System;
using System.IO;
using GitVersion.Common;
using GitVersion.Exceptions;
using GitVersion.Logging;

namespace GitVersion.Configuration
{
    public class NamedConfigFileLocator : ConfigFileLocator
    {
        public NamedConfigFileLocator(string filePath, IFileSystem fileSystem, ILog log) : base(fileSystem, log)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath), "Empty file path provided!");
            FilePath = filePath;
        }

        public string FilePath { get; }

        public override bool HasConfigFileAt(string workingDirectory) =>
            FileSystem.Exists(Path.Combine(workingDirectory, FilePath));

        public override string GetConfigFilePath(string workingDirectory) =>
            Path.Combine(workingDirectory, FilePath);

        public override void Verify(string workingDirectory, string projectRootDirectory)
        {
            if (!Path.IsPathRooted(FilePath))
            {
                WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
            }
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
