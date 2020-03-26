using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public interface IConfigFileLocator
    {
        bool HasConfigFileAt(string workingDirectory);
        string GetConfigFilePath(string workingDirectory);
        void Verify(Arguments arguments);
        void Verify(string workingDirectory, string projectRootDirectory);
        string SelectConfigFilePath(Arguments arguments);
        Config ReadConfig(string workingDirectory);
    }
}
