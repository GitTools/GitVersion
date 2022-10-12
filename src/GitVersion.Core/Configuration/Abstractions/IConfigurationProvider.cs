namespace GitVersion.Configuration;

public interface IConfigurationProvider
{
    GitVersionConfiguration Provide(GitVersionConfiguration? overrideConfiguration = null);
    GitVersionConfiguration Provide(string workingDirectory, GitVersionConfiguration? overrideConfiguration = null);
    void Init(string workingDirectory);
}
