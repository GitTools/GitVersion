using System;
using System.IO;
using GitVersion.Model.Configuration;

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

        public string SelectConfigFilePath(GitVersionOptions gitVersionOptions)
        {
            var workingDirectory = gitVersionOptions.WorkingDirectory;
            var projectRootDirectory = gitVersionOptions.ProjectRootDirectory;

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

        public void Verify(GitVersionOptions gitVersionOptions)
        {
            if (!string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.TargetUrl))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitVersionOptions.WorkingDirectory;
            var projectRootDirectory = gitVersionOptions.ProjectRootDirectory;

            Verify(workingDirectory, projectRootDirectory);
        }
    }
}
