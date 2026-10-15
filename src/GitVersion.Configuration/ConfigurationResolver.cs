using GitVersion.Configuration.Workflows;

namespace GitVersion.Configuration;

// This path deliberately has no repository, filesystem or version-calculation dependency.
internal sealed class ConfigurationResolver(IConfigurationSerializer serializer)
{
    public IGitVersionConfiguration Resolve(string? content, IReadOnlyDictionary<object, object?>? overrides = null)
    {
        var version = ConfigurationVersionSelector.Resolve();
        var document = content is null ? null : serializer.Deserialize<Dictionary<object, object?>>(content);
        var file = document is null ? null : ConfigurationDocumentMapper.Normalize(document, version, "configuration file");
        var runtime = overrides is null ? null : ConfigurationDocumentMapper.NormalizeInternal(overrides, "runtime override configuration");
        if (file is not null)
        {
            ConfigurationInputValidator.Validate(file, version);
        }

        if (runtime is not null)
        {
            ConfigurationInputValidator.Validate(runtime, version);
        }

        string? workflow = null;
        foreach (var item in new[] { file, runtime })
        {
            if (item?.TryGetValue("workflow", out var value) == true && value is not null)
            {
                workflow = (string)value;
            }
        }

        IConfigurationBuilder builder = workflow is null ? GitFlowConfigurationBuilder.New : ConfigurationBuilder.New;
        if (workflow is not null && string.IsNullOrWhiteSpace(workflow))
        {
            throw new ConfigurationValidationException("GVCONFIG_WORKFLOW", "workflow", "Expected an embedded workflow name or null for default configuration.");
        }
        IReadOnlyDictionary<object, object?>? workflowDocument;
        try
        {
            var embedded = WorkflowManager.GetOverrideConfiguration(workflow);
            workflowDocument = embedded is null ? null : ConfigurationDocumentMapper.NormalizeInternal(embedded, "embedded workflow");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ConfigurationValidationException("GVCONFIG_WORKFLOW", "workflow", "Unknown workflow. Select an embedded workflow.");
        }

        foreach (var item in new[] { workflowDocument, file, runtime }.OfType<IReadOnlyDictionary<object, object?>>())
        {
            builder.AddOverride(item);
        }

        return builder.Build();
    }
}
