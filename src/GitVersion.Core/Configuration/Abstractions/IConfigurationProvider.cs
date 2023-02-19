namespace GitVersion.Configuration;

public interface IConfigurationProvider
{
    GitVersionConfiguration Provide(GitVersionConfiguration? overrideConfiguration = null);
    void Init(string workingDirectory);
}
