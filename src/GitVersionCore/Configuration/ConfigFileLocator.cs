using System.IO;
using GitVersion.Helpers;

namespace GitVersion.Configuration
{
    public abstract class ConfigFileLocator
    {

        public static readonly ConfigFileLocator Default = new DefaultConfigFileLocator();

        public static ConfigFileLocator GetLocator(string filePath = null) =>
             !string.IsNullOrEmpty(filePath) ? new NamedConfigFileLocator(filePath) : Default;

        public abstract bool HasConfigFileAt(string workingDirectory, IFileSystem fileSystem);

        public abstract string GetConfigFilePath(string workingDirectory, IFileSystem fileSystem);

        public abstract void Verify(string workingDirectory, string projectRootDirectory, IFileSystem fileSystem);

        public string SelectConfigFilePath(GitPreparer gitPreparer, IFileSystem fileSystem)
        {
            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            if (HasConfigFileAt(workingDirectory, fileSystem))
            {
                return GetConfigFilePath(workingDirectory, fileSystem);
            }

            return GetConfigFilePath(projectRootDirectory, fileSystem);
        }

        public Config ReadConfig(string workingDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(workingDirectory, fileSystem);

            if (fileSystem.Exists(configFilePath))
            {
                var readAllText = fileSystem.ReadAllText(configFilePath);
                LegacyConfigNotifier.Notify(new StringReader(readAllText));
                return ConfigSerialiser.Read(new StringReader(readAllText));
            }

            return new Config();
        }

        public void Verify(GitPreparer gitPreparer, IFileSystem fileSystem)
        {
            if (!string.IsNullOrWhiteSpace(gitPreparer.TargetUrl))
            {
                // Assuming this is a dynamic repository. At this stage it's unsure whether we have
                // any .git info so we need to skip verification
                return;
            }

            var workingDirectory = gitPreparer.WorkingDirectory;
            var projectRootDirectory = gitPreparer.GetProjectRootDirectory();

            Verify(workingDirectory, projectRootDirectory, fileSystem);
        }
    }
}
