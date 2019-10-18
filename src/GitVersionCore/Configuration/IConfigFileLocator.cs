namespace GitVersion.Configuration
{
    public interface IConfigFileLocator
    {
        bool HasConfigFileAt(string workingDirectory);
        string GetConfigFilePath(string workingDirectory);
        void Verify(string workingDirectory, string projectRootDirectory);
        string SelectConfigFilePath(GitPreparer gitPreparer);
        Config ReadConfig(string workingDirectory);
        void Verify(GitPreparer gitPreparer);
    }
}
