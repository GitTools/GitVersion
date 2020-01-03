using System;
using System.IO;
using GitVersion.Logging;

namespace GitVersion.Configuration
{
    public abstract class ConfigFileLocator : IConfigFileLocator
    {
        protected readonly IFileSystem FileSystem;
        protected readonly ILog Log;

        protected ConfigFileLocator(IFileSystem fileSystem, ILog log)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            Log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public abstract bool HasConfigFileAt(string workingDirectory);

        public abstract string GetConfigFilePath(string workingDirectory);

        public abstract void Verify(string workingDirectory, string projectRootDirectory);

        public string SelectConfigFilePath(IGitPreparer gitPreparer)
        {
            var workingDirectory = gitPreparer.GetWorkingDirectory();
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (HasConfigFileAt(workingDirectory))
            {
                return GetConfigFilePath(workingDirectory);
            }

            return GetConfigFilePath(projectRootDirectory);
        }

        public Config ReadConfig(string workingDirectory)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);

            if (FileSystem.Exists(configFilePath))
            {
                var readAllText = FileSystem.ReadAllText(configFilePath);
                LegacyConfigNotifier.Notify(new StringReader(readAllText));
                return ConfigSerialiser.Read(new StringReader(readAllText));
            }

            return new Config();
        }

        public void Verify(IGitPreparer gitPreparer)
        {
            if (!string.IsNullOrWhiteSpace(gitPreparer.GetTargetUrl()))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitPreparer.GetWorkingDirectory();
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            Verify(workingDirectory, projectRootDirectory);
        }
    }
}
