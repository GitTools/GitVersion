using GitVersion.Model.Configuration;

namespace GitVersion.Configuration
{
    public interface IConfigProvider
    {
        Config Provide(bool applyDefaults = true, Config overrideConfig = null);
        Config Provide(string workingDirectory, bool applyDefaults = true, Config overrideConfig = null);
        void Init(string workingDirectory);
    }
}
