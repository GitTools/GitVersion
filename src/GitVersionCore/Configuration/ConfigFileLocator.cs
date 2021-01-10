using System;
using System.IO;
using System.Linq;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

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

        public string SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo)
        {
            var workingDirectory = gitVersionOptions.WorkingDirectory;
            var projectRootDirectory = repositoryInfo.ProjectRootDirectory;

            return GetConfigFilePath(HasConfigFileAt(workingDirectory) ? workingDirectory : projectRootDirectory);
        }

        public Config ReadConfig(string workingDirectory)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);

            if (FileSystem.Exists(configFilePath))
            {
                var readAllText = FileSystem.ReadAllText(configFilePath);
                var readConfig = ConfigSerializer.Read(new StringReader(readAllText));

                VerifyReadConfig(readConfig);

                return readConfig;
            }

            return new Config();
        }

        public static void VerifyReadConfig(Config config)
        {
            // Verify no branches are set to mainline mode
            if (config.Branches.Any(b => b.Value.VersioningMode == VersioningMode.Mainline))
            {
                throw new ConfigurationException(@"Mainline mode only works at the repository level, a single branch cannot be put into mainline mode

This is because mainline mode treats your entire git repository as an event source with each merge into the 'mainline' incrementing the version.

If the docs do not help you decide on the mode open an issue to discuss what you are trying to do.");
            }
        }

        public void Verify(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo)
        {
            if (!string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.TargetUrl))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitVersionOptions.WorkingDirectory;
            var projectRootDirectory = repositoryInfo.ProjectRootDirectory;

            Verify(workingDirectory, projectRootDirectory);
        }
    }
}
