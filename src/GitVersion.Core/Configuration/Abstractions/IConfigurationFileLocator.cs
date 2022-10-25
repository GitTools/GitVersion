namespace GitVersion.Configuration;

public interface IConfigurationFileLocator
{
    string FilePath { get; }
    bool HasConfigFileAt(string workingDirectory);
    string? GetConfigFilePath(string workingDirectory);
    void Verify(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
    void Verify(string workingDirectory, string projectRootDirectory);
    string? SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
    GitVersionConfiguration ReadConfig(string workingDirectory);
}
