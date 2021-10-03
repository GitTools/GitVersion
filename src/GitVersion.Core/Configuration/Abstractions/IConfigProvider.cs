using GitVersion.Model.Configuration;

namespace GitVersion.Configuration;

public interface IConfigProvider
{
    Config Provide(Config? overrideConfig = null);
    Config Provide(string workingDirectory, Config? overrideConfig = null);
    void Init(string workingDirectory);
}
