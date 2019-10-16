using System.IO;
using GitVersion.Common;
using GitVersion.Log;

namespace GitVersion.Configuration
{
    public abstract class ConfigFileLocator : IConfigFileLocator
    {
        protected readonly IFileSystem FileSystem;
        protected readonly ILog Log;

        protected ConfigFileLocator(IFileSystem fileSystem, ILog log)
        {
            FileSystem = fileSystem;
            Log = log;
        }

        public static IConfigFileLocator GetLocator(IFileSystem fileSystem, ILog log, string filePath = null) =>
            !string.IsNullOrEmpty(filePath)
                ? (IConfigFileLocator) new NamedConfigFileLocator(filePath, fileSystem, log)
                : new DefaultConfigFileLocator(fileSystem, log);

        public abstract bool HasConfigFileAt(string workingDirectory);

        public abstract string GetConfigFilePath(string workingDirectory);

        public abstract void Verify(string workingDirectory, string projectRootDirectory);

        public string SelectConfigFilePath(GitPreparer gitPreparer)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
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

        public void Verify(GitPreparer gitPreparer)
        {
            if (!string.IsNullOrWhiteSpace(gitPreparer.TargetUrl))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            Verify(workingDirectory, projectRootDirectory);
        }
    }
}
