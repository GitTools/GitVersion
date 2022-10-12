namespace GitVersion.Configurations;

public interface IConfigProvider
{
    Model.Configurations.Configuration Provide(Model.Configurations.Configuration? overrideConfig = null);
    Model.Configurations.Configuration Provide(string workingDirectory, Model.Configurations.Configuration? overrideConfig = null);
    void Init(string workingDirectory);
}
