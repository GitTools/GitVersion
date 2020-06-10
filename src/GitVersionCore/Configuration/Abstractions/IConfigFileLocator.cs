using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public interface IConfigFileLocator
    {
        bool HasConfigFileAt(string workingDirectory);
        string GetConfigFilePath(string workingDirectory);
        void Verify(GitVersionOptions gitVersionOptions);
        void Verify(string workingDirectory, string projectRootDirectory);
        string SelectConfigFilePath(GitVersionOptions gitVersionOptions);
        Config ReadConfig(string workingDirectory);
    }
}
