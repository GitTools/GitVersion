using System;
using System.IO;
using Microsoft.Extensions.Options;

namespace GitVersion.Configuration
{
    public class NamedConfigFileLocator : ConfigFileLocator
    {
        private readonly IOptions<GitVersionOptions> options;

        public NamedConfigFileLocator(IFileSystem fileSystem, IOptions<GitVersionOptions> options) : base(fileSystem)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string FilePath => options.Value.ConfigInfo.ConfigFile;

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

            var hasConfigInWorkingDirectory = FileSystem.Exists(workingConfigFile);
            var hasConfigInProjectRootDirectory = FileSystem.Exists(projectRootConfigFile);
            if (hasConfigInProjectRootDirectory && hasConfigInWorkingDirectory)
            {
                throw new WarningException($"Ambiguous config file selection from '{workingConfigFile}' and '{projectRootConfigFile}'");
            }

            if (!hasConfigInProjectRootDirectory && !hasConfigInWorkingDirectory)
            {
                throw new WarningException($"The configuration file was not found at '{workingConfigFile}' or '{projectRootConfigFile}'");
            }
        }
    }
}
