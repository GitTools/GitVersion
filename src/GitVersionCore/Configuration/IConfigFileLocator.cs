namespace GitVersion.Configuration
{
    public interface IConfigFileLocator
    {
        bool HasConfigFileAt(string workingDirectory);
        string GetConfigFilePath(string workingDirectory);
        void Verify(string workingDirectory, string projectRootDirectory);
        string SelectConfigFilePath(IGitPreparer gitPreparer);
        Config ReadConfig(string workingDirectory);
        void Verify(IGitPreparer gitPreparer);
    }
}
