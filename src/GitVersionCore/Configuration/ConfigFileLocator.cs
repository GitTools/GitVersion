using System;
using System.IO;

namespace GitVersion.Configuration
{
    public abstract class ConfigFileLocator : IConfigFileLocator
    {
        protected readonly IFileSystem FileSystem;

        protected ConfigFileLocator(IFileSystem fileSystem)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public abstract bool HasConfigFileAt(string workingDirectory);

        public abstract string GetConfigFilePath(string workingDirectory);

        public abstract void Verify(string workingDirectory, string projectRootDirectory);

        public string SelectConfigFilePath(IGitPreparer gitPreparer)
        {
            var workingDirectory = gitPreparer.GetWorkingDirectory();
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            return GetConfigFilePath(HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory);
        }

        public Config ReadConfig(string workingDirectory)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);

            if (FileSystem.Exists(configFilePath))
            {
                var readAllText = FileSystem.ReadAllText(configFilePath);
                return ConfigSerializer.Read(new StringReader(readAllText));
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
