using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal class ConfigurationMigrationService(IConfigurationSerializer configurationSerializer) : IConfigurationMigrationService
{
    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();

    public string Migrate(string input)
    {
        var document = this.configurationSerializer.Deserialize<Dictionary<object, object?>>(input);
        return ConfigurationDocumentMapper.Detect(document) switch
        {
            ConfigurationDocumentKind.Empty or ConfigurationDocumentKind.V6 =>
                ConfigurationSerializer.SerializeDocument(ConfigurationDocumentMapper.Nest(document)),
            ConfigurationDocumentKind.V7 => ConfigurationSerializer.SerializeDocument(document),
            _ => throw new ConfigurationException(
                "The configuration document mixes the v6 flat configuration structure with the v7 'calculation'/'output' structure. " +
                "Use only one structure before migrating.")
        };
    }
}
