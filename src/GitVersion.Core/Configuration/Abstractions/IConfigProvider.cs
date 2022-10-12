using GitVersion.Model.Configuration;

namespace GitVersion.Configuration;

public interface IConfigProvider
{
    GitVersionConfiguration Provide(GitVersionConfiguration? overrideConfig = null);
    GitVersionConfiguration Provide(string workingDirectory, GitVersionConfiguration? overrideConfig = null);
    void Init(string workingDirectory);
}
