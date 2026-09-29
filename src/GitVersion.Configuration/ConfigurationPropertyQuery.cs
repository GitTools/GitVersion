namespace GitVersion.Configuration;

internal static class ConfigurationPropertyQuery
{
    public static object? GetValue(IGitVersionConfiguration configuration, string path)
    {
        if (ConfigurationVersionSelector.Resolve() != ConfigurationVersion.V7)
        {
            throw new ConfigurationException("Configuration property queries require v7 configuration. Set GITVERSION_CONFIGURATION_VERSION=v7.");
        }

        var segments = path.Split('.');
        if (segments.Any(segment => segment.Length == 0 || segment.Any(character => !char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character != '-')))
        {
            throw new ConfigurationException($"Invalid configuration property path '{path}'. Use dot-separated public configuration names.");
        }

        var offset = segments[0] is ConfigurationDocumentMapper.CalculationSectionName or ConfigurationDocumentMapper.OutputSectionName ? 1 : 0;
        if (offset == 1 && segments.Length == 1)
        {
            throw Unsupported(path);
        }

        if (offset == 0 && segments[0] != ConfigurationDocumentMapper.WorkflowPropertyName)
        {
            throw Unknown(path);
        }

        if (offset == 1)
        {
            var propertyName = segments[1];
            if (propertyName == ConfigurationDocumentMapper.BranchesPropertyName)
            {
                throw Unsupported(path);
            }

            var isOutput = ConfigurationDocumentMapper.IsOutputProperty(propertyName);
            if (propertyName == ConfigurationDocumentMapper.WorkflowPropertyName || isOutput != (segments[0] == ConfigurationDocumentMapper.OutputSectionName))
            {
                throw Unknown(path);
            }
        }

        // Traverse declared public properties rather than displayed YAML: serialization omits
        // nulls, and a mapping alone cannot distinguish a fixed object from a dynamic map.
        object? current = configuration;
        var type = configuration.GetType();
        for (var index = offset; index < segments.Length; index++)
        {
            var property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(candidate => candidate.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == segments[index]
                                              && candidate.GetCustomAttribute<JsonIgnoreAttribute>() is null);
            if (property is null)
            {
                throw Unknown(path);
            }

            type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            {
                throw Unsupported(path);
            }

            current = current is null ? null : property.GetValue(current);
            var isScalar = type == typeof(string) || type == typeof(bool) || type == typeof(int) || type.IsEnum;
            if (index == segments.Length - 1)
            {
                return isScalar ? current : throw Unsupported(path);
            }

            if (isScalar)
            {
                throw Unknown(path);
            }
        }

        throw Unknown(path);
    }

    private static ConfigurationException Unknown(string path) => new($"Unknown configuration property '{path}'. Use the public v7 configuration path.");

    private static ConfigurationException Unsupported(string path) => new($"Configuration property '{path}' is not supported: only scalar properties of fixed objects can be queried; maps and collections are not supported.");
}
