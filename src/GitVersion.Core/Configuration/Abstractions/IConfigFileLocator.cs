using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public interface IConfigFileLocator
    {
        bool HasConfigFileAt(string workingDirectory);
        string GetConfigFilePath(string workingDirectory);
        void Verify(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
        void Verify(string workingDirectory, string projectRootDirectory);
        string SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
        Config ReadConfig(string workingDirectory);
    }
}
