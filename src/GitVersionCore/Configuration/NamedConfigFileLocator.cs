using System;
using System.IO;
using GitVersion.Exceptions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class NamedConfigFileLocator : ConfigFileLocator
    {
        private readonly IOptions<Arguments> options;

        public NamedConfigFileLocator(IFileSystem fileSystem, ILog log, IOptions<Arguments> options) : base(fileSystem, log)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string FilePath => options.Value.ConfigFile;

        public override bool HasConfigFileAt(string workingDirectory) =>
            FileSystem.Exists(Path.Combine(workingDirectory, FilePath));

        public override string GetConfigFilePath(string workingDirectory) =>
            Path.Combine(workingDirectory, FilePath);

        public override void Verify(string workingDirectory, string projectRootDirectory)
        {
            if (!Path.IsPathRooted(FilePath) && !FileSystem.PathsEqual(workingDirectory, projectRootDirectory))
            {
                WarnAboutAmbiguousConfigFileSelection(workingDirectory, projectRootDirectory);
            }
        }

        private void WarnAboutAmbiguousConfigFileSelection(string workingDirectory, string projectRootDirectory)
        {
            var workingConfigFile = GetConfigFilePath(workingDirectory);
            var projectRootConfigFile = GetConfigFilePath(projectRootDirectory);

            var fileSystemCasingComparer = System.Environment.OSVersion.Platform == PlatformID.Unix ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (Path.GetFullPath(workingConfigFile).Equals(Path.GetFullPath(projectRootConfigFile), fileSystemCasingComparer))
                return;

            var hasConfigInWorkingDirectory = FileSystem.Exists(workingConfigFile);
            var hasConfigInProjectRootDirectory = FileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException($"Ambiguous config file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
            }
        }
    }
}
