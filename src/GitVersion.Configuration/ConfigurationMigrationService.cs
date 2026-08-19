using GitVersion.Extensions;
using SharpYaml;

namespace GitVersion.Configuration;

internal class ConfigurationMigrationService(IConfigurationSerializer configurationSerializer) : IConfigurationMigrationService
{
    private static readonly YamlSerializerOptions ValidationOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = [VersionStrategiesConverter.Instance]
    };

    private readonly IConfigurationSerializer configurationSerializer = configurationSerializer.NotNull();

    public string Migrate(string input)
    {
        var document = this.configurationSerializer.Deserialize<Dictionary<object, object?>>(input);
        MoveDraftWorkflowToRoot(document);
        var normalized = ConfigurationDocumentMapper.NormalizeInternal(document, "configuration document");
        // Validate the effective types without serializing defaults back into the user's document.
        _ = YamlSerializer.Deserialize<GitVersionConfiguration>(ConfigurationSerializer.SerializeLegacy(normalized), ValidationOptions);

        return ConfigurationDocumentMapper.Detect(document) switch
        {
            ConfigurationDocumentKind.Empty or ConfigurationDocumentKind.Shared or ConfigurationDocumentKind.V6 =>
                ConfigurationSerializer.SerializeDocument(ConfigurationDocumentMapper.Nest(document)),
            ConfigurationDocumentKind.V7 => ConfigurationSerializer.SerializeDocument(document),
            _ => throw new ConfigurationException(
                "The configuration document mixes the v6 flat configuration structure with the v7 'calculation'/'output' structure. " +
                "Use only one structure before migrating.")
        };
    }

    private static void MoveDraftWorkflowToRoot(Dictionary<object, object?> document)
    {
        if (document.TryGetValue(ConfigurationDocumentMapper.OutputSectionName, out var outputValue)
            && outputValue is IReadOnlyDictionary<object, object?> output && output.ContainsKey(ConfigurationDocumentMapper.WorkflowPropertyName))
        {
            throw new ConfigurationException("Configuration property 'output.workflow' is not supported. Move 'workflow' to the document root before migrating.");
        }

        if (!document.TryGetValue(ConfigurationDocumentMapper.CalculationSectionName, out var calculationValue)
            || calculationValue is not IDictionary<object, object?> calculation
            || !calculation.TryGetValue(ConfigurationDocumentMapper.WorkflowPropertyName, out var workflow))
        {
            return;
        }

        if (document.ContainsKey(ConfigurationDocumentMapper.WorkflowPropertyName))
        {
            throw new ConfigurationException("Configuration property 'workflow' is defined at both the document root and 'calculation.workflow'. Keep only one before migrating.");
        }

        calculation.Remove(ConfigurationDocumentMapper.WorkflowPropertyName);
        document[ConfigurationDocumentMapper.WorkflowPropertyName] = workflow;
    }
}
