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

        var workflow = GetWorkflow(file, runtime);

        IConfigurationBuilder builder = workflow is null ? GitFlowConfigurationBuilder.New : ConfigurationBuilder.New;
        var workflowDocument = ResolveWorkflow(workflow);
        foreach (var item in new[] { workflowDocument, file, runtime }.OfType<IReadOnlyDictionary<object, object?>>())
        {
            builder.AddOverride(item);
        }

        return builder.Build();
    }

    private static string? GetWorkflow(params IReadOnlyDictionary<object, object?>?[] documents)
    {
        string? workflow = null;
        foreach (var item in documents)
        {
            if (item?.TryGetValue("workflow", out var value) == true && value is not null)
            {
                workflow = (string)value;
            }
        }

        return workflow;
    }

    private static IReadOnlyDictionary<object, object?>? ResolveWorkflow(string? workflow)
    {
        if (workflow is not null && string.IsNullOrWhiteSpace(workflow))
        {
            throw new ConfigurationValidationException("GVCONFIG_WORKFLOW", "workflow", "Expected an embedded workflow name or null for default configuration.");
        }
        try
        {
            var embedded = WorkflowManager.GetOverrideConfiguration(workflow);
            return embedded is null ? null : ConfigurationDocumentMapper.NormalizeInternal(embedded, "embedded workflow");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ConfigurationValidationException("GVCONFIG_WORKFLOW", "workflow", "Unknown workflow. Select an embedded workflow.");
        }
    }
}
