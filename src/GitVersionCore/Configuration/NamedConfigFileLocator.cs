using System;
using System.IO;
using GitVersion.Exceptions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class NamedConfigFileLocator : ConfigFileLocator
    {
        public NamedConfigFileLocator(IFileSystem fileSystem, ILog log, IOptions<Arguments> options) : base(fileSystem, log)
        {
            var filePath = options.Value.ConfigFile;
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
