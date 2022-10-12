namespace GitVersion.Configuration;

public interface IConfigFileLocator
{
    string FilePath { get; }
    bool HasConfigFileAt(string workingDirectory);
    string? GetConfigFilePath(string workingDirectory);
    void Verify(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
    void Verify(string workingDirectory, string projectRootDirectory);
    string? SelectConfigFilePath(GitVersionOptions gitVersionOptions, IGitRepositoryInfo repositoryInfo);
    Model.Configuration.GitVersionConfiguration ReadConfig(string workingDirectory);
}
