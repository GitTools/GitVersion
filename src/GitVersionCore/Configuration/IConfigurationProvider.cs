namespace GitVersion.Configuration
{
    public interface IConfigurationProvider
    {
        Config Provide(bool applyDefaults = true, Config overrideConfig = null);
        Config Provide(string workingDirectory, bool applyDefaults = true, Config overrideConfig = null);
        string GetEffectiveConfigAsString(string workingDirectory);
        void Init(string workingDirectory);
    }
}
